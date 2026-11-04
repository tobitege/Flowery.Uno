using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Uno.Photos.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>
    /// Loads downscaled thumbnails with bounded concurrency and a small LRU cache.
    /// Skia: SkiaSharp decodes the file (scaled where the codec allows it) and the pixels go into a WriteableBitmap.
    /// Formats SkiaSharp cannot decode (HEIC, AVIF, TIFF, JXR) have no thumbnail; the tile shows a placeholder.
    /// Must be called from the UI thread.
    /// </summary>
    public static class ThumbnailLoader
    {
        private const int MaxCacheEntries = 700;
        private static readonly SemaphoreSlim Gate = new(6);
        private static readonly Dictionary<string, LinkedListNode<(string Key, ImageSource Image)>> Cache = [];
        private static readonly LinkedList<(string Key, ImageSource Image)> Lru = new();

        private static readonly HashSet<string> DirectDecode = new(StringComparer.OrdinalIgnoreCase)
        {
            "JPG", "JPEG", "JPE", "JFIF", "PNG", "GIF", "BMP", "WEBP", "ICO"
        };

        public static int Bucket(double height)
        {
            if (height <= 120) return 120;
            if (height <= 200) return 200;
            if (height <= 300) return 300;
            if (height <= 440) return 440;
            if (height <= 640) return 640;
            return 960;
        }

        public static ImageSource? TryGetCached(PhotoItem item, double height)
        {
            var key = CreateKey(item, Bucket(height));
            if (!Cache.TryGetValue(key, out var node))
                return null;
            Lru.Remove(node);
            Lru.AddFirst(node);
            return node.Value.Image;
        }

        public static async Task<ImageSource?> LoadAsync(PhotoItem item, double height, CancellationToken token)
        {
            var bucket = Bucket(height);
            var key = CreateKey(item, bucket);
            if (Cache.TryGetValue(key, out var cached))
            {
                Lru.Remove(cached);
                Lru.AddFirst(cached);
                return cached.Value.Image;
            }

            await Gate.WaitAsync(token).ConfigureAwait(true);
            try
            {
                token.ThrowIfCancellationRequested();
                if (!CanDecode(item.Extension))
                    return null;
                var image = await DecodeAsync(item.Path, bucket, token).ConfigureAwait(true);
                if (image == null)
                    return null;

                var node = Lru.AddFirst((key, image));
                Cache[key] = node;
                while (Lru.Count > MaxCacheEntries && Lru.Last is { } last)
                {
                    Cache.Remove(last.Value.Key);
                    Lru.RemoveLast();
                }
                return image;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                Gate.Release();
            }
        }

        public static void Invalidate(PhotoItem item)
        {
            var prefix = item.Path + "|";
            foreach (var key in Cache.Keys.Where(k => k.StartsWith(prefix, PathRules.Comparison)).ToList())
            {
                Lru.Remove(Cache[key]);
                Cache.Remove(key);
            }
        }

        private static string CreateKey(PhotoItem item, int bucket) => $"{item.Path}|{item.Revision}|{bucket}";

        /// <summary>True for the formats SkiaSharp decodes; the others get a placeholder tile.</summary>
        public static bool CanDecode(string extension) => DirectDecode.Contains(extension);

        private static async Task<ImageSource?> DecodeAsync(string path, int bucket, CancellationToken token)
        {
            using var bitmap = await Task.Run(() => ThumbnailDiskCache.Default.GetOrCreate(path, bucket, ThumbnailDecoder.Decode), token).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            return bitmap == null ? null : ToImageSource(bitmap);
        }

        private static WriteableBitmap ToImageSource(SKBitmap bitmap)
        {
            var image = new WriteableBitmap(bitmap.Width, bitmap.Height);
            using (var stream = image.PixelBuffer.AsStream())
            {
                stream.Write(bitmap.GetPixelSpan());
            }
            image.Invalidate();
            return image;
        }
    }
}

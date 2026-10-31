using System.IO;
using System.Security.Cryptography;
using System.Text;
using SkiaSharp;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>
    /// Thumbnails kept on disk (WebP) so a restart does not decode every photo again. Skia: Windows had the shell
    /// thumbnail cache; PNG, BMP and ICO cannot decode scaled, so they are the expensive ones.
    /// A cached file belongs to one source path, size, modification time and bucket; changing the photo changes the key.
    /// Stored thumbnails already have their EXIF orientation applied, so readers must not apply it again.
    /// </summary>
    public sealed class ThumbnailDiskCache(string directory)
    {
        public const long DefaultMaxBytes = 256L * 1024 * 1024;
        private const int Quality = 85;

        private int _trimmed;

        public string Directory { get; } = directory;

        public static ThumbnailDiskCache Default => new(System.IO.Path.Combine(PhotosSettings.StateDirectory, "thumbnails"));

        /// <summary>Returns the cached thumbnail, or decodes it with <paramref name="decode"/> and stores the result.</summary>
        public SKBitmap? GetOrCreate(string sourcePath, int bucket, Func<string, int, SKBitmap?> decode)
        {
            var cached = TryRead(sourcePath, bucket);
            if (cached != null)
                return cached;

            var bitmap = decode(sourcePath, bucket);
            if (bitmap != null)
                Write(sourcePath, bucket, bitmap);
            return bitmap;
        }

        public SKBitmap? TryRead(string sourcePath, int bucket)
        {
            var file = GetCacheFile(sourcePath, bucket);
            if (file == null || !File.Exists(file))
                return null;

            try
            {
                using var raw = SKBitmap.Decode(file);
                if (raw == null)
                {
                    TryDelete(file);
                    return null;
                }

                // Same pixel layout as ThumbnailDecoder, whatever the platform default is.
                var info = new SKImageInfo(raw.Width, raw.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                var bitmap = new SKBitmap(info);
                using var image = SKImage.FromBitmap(raw);
                if (!image.ReadPixels(info, bitmap.GetPixels(), info.RowBytes, 0, 0))
                {
                    bitmap.Dispose();
                    TryDelete(file);
                    return null;
                }
                return bitmap;
            }
            catch
            {
                TryDelete(file);
                return null;
            }
        }

        public void Write(string sourcePath, int bucket, SKBitmap bitmap)
        {
            var file = GetCacheFile(sourcePath, bucket);
            if (file == null)
                return;

            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                using var data = bitmap.Encode(SKEncodedImageFormat.Webp, Quality);
                if (data == null)
                    return;

                // Written under another name first: up to six thumbnails load at once and a reader must never see half a file.
                var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
                using (var stream = File.Create(temp))
                    data.SaveTo(stream);
                File.Move(temp, file, overwrite: true);
            }
            catch
            {
                // The cache is optional.
                return;
            }

            // Keep the folder bounded; once per process is enough because thumbnails are small.
            if (Interlocked.Exchange(ref _trimmed, 1) == 0)
                _ = Task.Run(() => Trim(DefaultMaxBytes));
        }

        /// <summary>Deletes the oldest files until the cache is at most <paramref name="maxBytes"/>.</summary>
        public void Trim(long maxBytes)
        {
            try
            {
                if (!System.IO.Directory.Exists(Directory))
                    return;

                var files = new DirectoryInfo(Directory).EnumerateFiles("*.webp").OrderBy(f => f.LastWriteTimeUtc).ToList();
                var total = files.Sum(f => f.Length);
                foreach (var file in files)
                {
                    if (total <= maxBytes)
                        break;
                    total -= file.Length;
                    TryDelete(file.FullName);
                }
            }
            catch
            {
                // Trimming is best-effort.
            }
        }

        public void Clear()
        {
            try
            {
                if (System.IO.Directory.Exists(Directory))
                    System.IO.Directory.Delete(Directory, recursive: true);
            }
            catch
            {
                // Ignore.
            }
        }

        private string? GetCacheFile(string sourcePath, int bucket)
        {
            try
            {
                var info = new FileInfo(sourcePath);
                if (!info.Exists)
                    return null;

                var key = $"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}|{bucket}";
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..32];
                return System.IO.Path.Combine(Directory, hash + ".webp");
            }
            catch
            {
                return null;
            }
        }

        private static void TryDelete(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore.
            }
        }
    }
}

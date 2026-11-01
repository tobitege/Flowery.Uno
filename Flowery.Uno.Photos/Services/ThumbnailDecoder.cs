using SkiaSharp;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>
    /// Decodes a downscaled, orientation-corrected thumbnail with SkiaSharp. No UI types, so it can be tested on its own
    /// and reused by the viewer. Skia: replaces BitmapImage with DecodePixelHeight and the shell thumbnail cache.
    /// </summary>
    public static class ThumbnailDecoder
    {
        private static readonly SKSamplingOptions Resampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

        /// <summary>
        /// Decodes <paramref name="path"/> so that its oriented height is at most <paramref name="targetHeight"/>.
        /// Returns null for files SkiaSharp cannot decode (HEIC, AVIF, TIFF, JXR). The caller owns the bitmap.
        /// </summary>
        public static SKBitmap? Decode(string path, int targetHeight)
        {
            using var codec = SKCodec.Create(path);
            if (codec == null)
                return null;

            var origin = codec.EncodedOrigin;
            var stored = codec.Info;
            var orientedHeight = SwapsAxes(origin) ? stored.Width : stored.Height;

            // JPEG and WebP decode scaled; PNG, BMP and ICO ignore the scale (phase 0 spike), so those are resized below.
            var size = stored.Size;
            if (orientedHeight > targetHeight)
                size = codec.GetScaledDimensions((float)targetHeight / orientedHeight);

            var info = new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);
            var result = codec.GetPixels(info, bitmap.GetPixels());
            if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
            {
                bitmap.Dispose();
                return null;
            }

            return Finish(bitmap, origin, targetHeight);
        }

        /// <summary>Resizes to the target height when larger, then applies the EXIF origin. Takes ownership of <paramref name="bitmap"/>.</summary>
        public static SKBitmap Finish(SKBitmap bitmap, SKEncodedOrigin origin, int targetHeight)
        {
            var orientedHeight = SwapsAxes(origin) ? bitmap.Width : bitmap.Height;
            if (orientedHeight > targetHeight)
            {
                var scale = (double)targetHeight / orientedHeight;
                var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
                var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
                var resized = bitmap.Resize(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul), Resampling);
                if (resized != null)
                {
                    bitmap.Dispose();
                    bitmap = resized;
                }
            }

            return ApplyOrigin(bitmap, origin);
        }

        /// <summary>
        /// Draws the bitmap upright for the given EXIF origin. Skia: SKBitmap.Decode and SKCodec ignore the orientation
        /// (phase 0 spike), WinUI's BitmapImage applied it. Takes ownership of <paramref name="bitmap"/>.
        /// </summary>
        public static SKBitmap ApplyOrigin(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
                return bitmap;

            float w = bitmap.Width, h = bitmap.Height;
            var swap = SwapsAxes(origin);
            var target = new SKBitmap(new SKImageInfo(swap ? bitmap.Height : bitmap.Width, swap ? bitmap.Width : bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul));

            // Maps a source point (x, y) to its upright position: x' = a*x + b*y + tx, y' = c*x + d*y + ty.
            var matrix = origin switch
            {
                SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
                SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
                SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
                SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
                SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
                SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
                SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
                _ => SKMatrix.Identity
            };

            using (var canvas = new SKCanvas(target))
            using (var image = SKImage.FromBitmap(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.SetMatrix(matrix);
                canvas.DrawImage(image, 0, 0, new SKSamplingOptions());
            }

            bitmap.Dispose();
            return target;
        }

        public static bool SwapsAxes(SKEncodedOrigin origin) =>
            origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
    }
}

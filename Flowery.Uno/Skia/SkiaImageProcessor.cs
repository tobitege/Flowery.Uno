using System;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;

namespace Flowery.Skia
{
    /// <summary>
    /// Core SkiaSharp image processing infrastructure.
    /// Provides cross-platform image manipulation capabilities.
    /// </summary>
    public static class SkiaImageProcessor
    {
        /// <summary>
        /// Applies a Gaussian blur to the provided image bytes.
        /// </summary>
        /// <param name="imageBytes">Source image as PNG/JPEG bytes.</param>
        /// <param name="sigmaX">Horizontal blur sigma (higher = more blur).</param>
        /// <param name="sigmaY">Vertical blur sigma (higher = more blur).</param>
        /// <returns>Blurred image as PNG bytes, or null if processing failed.</returns>
        public static byte[]? ApplyBlur(byte[] imageBytes, float sigmaX, float sigmaY)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            using var bitmap = SKBitmap.Decode(imageBytes);
            if (bitmap == null)
                return null;

            return ApplyBlur(bitmap, sigmaX, sigmaY);
        }

        /// <summary>
        /// Applies a Gaussian blur to the provided SKBitmap.
        /// </summary>
        /// <param name="source">Source bitmap.</param>
        /// <param name="sigmaX">Horizontal blur sigma.</param>
        /// <param name="sigmaY">Vertical blur sigma.</param>
        /// <returns>Blurred image as PNG bytes, or null if processing failed.</returns>
        public static byte[]? ApplyBlur(SKBitmap source, float sigmaX, float sigmaY)
        {
            if (source == null)
                return null;

            using var surface = SKSurface.Create(new SKImageInfo(source.Width, source.Height));
            if (surface == null)
                return null;

            var canvas = surface.Canvas;

            using var blurFilter = SKImageFilter.CreateBlur(sigmaX, sigmaY, SKShaderTileMode.Clamp);
            using var paint = new SKPaint
            {
                ImageFilter = blurFilter,
                IsAntialias = true
            };

            canvas.DrawBitmap(source, 0, 0, paint);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);

            return data.ToArray();
        }

        /// <summary>
        /// Applies blur and saturation effects to an image.
        /// </summary>
        /// <param name="imageBytes">Source image bytes.</param>
        /// <param name="blurSigma">Blur amount (0 = no blur).</param>
        /// <param name="saturation">Saturation level (0.0 = grayscale, 1.0 = normal, >1.0 = oversaturated).</param>
        /// <returns>Processed image as PNG bytes.</returns>
        public static byte[]? ApplyBlurAndSaturation(byte[] imageBytes, float blurSigma, float saturation)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            using var bitmap = SKBitmap.Decode(imageBytes);
            if (bitmap == null)
                return null;

            return ApplyBlurAndSaturation(bitmap, blurSigma, saturation);
        }

        /// <summary>
        /// Applies blur and saturation effects to a bitmap.
        /// </summary>
        public static byte[]? ApplyBlurAndSaturation(SKBitmap source, float blurSigma, float saturation)
        {
            if (source == null)
                return null;

            using var surface = SKSurface.Create(new SKImageInfo(source.Width, source.Height));
            if (surface == null)
                return null;

            var canvas = surface.Canvas;

            // Create blur filter
            SKImageFilter? blurFilter = blurSigma > 0.1f
                ? SKImageFilter.CreateBlur(blurSigma, blurSigma, SKShaderTileMode.Clamp)
                : null;

            // Create saturation color filter if needed
            SKColorFilter? saturationFilter = null;
            if (Math.Abs(saturation - 1.0f) > 0.01f)
            {
                saturationFilter = CreateSaturationFilter(saturation);
            }

            // Combine filters
            SKImageFilter? combinedFilter = blurFilter;
            if (saturationFilter != null)
            {
                combinedFilter = SKImageFilter.CreateColorFilter(saturationFilter, blurFilter);
            }

            using var paint = new SKPaint
            {
                ImageFilter = combinedFilter,
                IsAntialias = true
            };

            canvas.DrawBitmap(source, 0, 0, paint);

            // Cleanup filters
            combinedFilter?.Dispose();
            if (combinedFilter != blurFilter)
                blurFilter?.Dispose();
            saturationFilter?.Dispose();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);

            return data.ToArray();
        }

        /// <summary>
        /// Applies blur, saturation, and tint overlay to create a glass effect.
        /// </summary>
        /// <param name="source">Source bitmap.</param>
        /// <param name="blurSigma">Blur amount.</param>
        /// <param name="saturation">Saturation level (0.0 = grayscale, 1.0 = normal).</param>
        /// <param name="tintColor">Tint overlay color.</param>
        /// <param name="tintOpacity">Tint opacity (0.0 - 1.0).</param>
        /// <param name="cornerRadius">Corner radius for rounded edges (0 = no rounding).</param>
        /// <returns>Glass effect image as PNG bytes.</returns>
        public static byte[]? ApplyGlassEffect(
            SKBitmap source,
            float blurSigma,
            float saturation,
            SKColor tintColor,
            float tintOpacity,
            float cornerRadius = 0)
        {
            if (source == null)
                return null;

            using var surface = SKSurface.Create(new SKImageInfo(source.Width, source.Height));
            if (surface == null)
                return null;

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var rect = new SKRect(0, 0, source.Width, source.Height);
            var roundRect = cornerRadius > 0
                ? new SKRoundRect(rect, cornerRadius)
                : null;

            // Clip to rounded rect if needed
            if (roundRect != null)
            {
                canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);
            }

            // Create blur filter
            SKImageFilter? blurFilter = blurSigma > 0.1f
                ? SKImageFilter.CreateBlur(blurSigma, blurSigma, SKShaderTileMode.Clamp)
                : null;

            // Create saturation filter if needed
            SKColorFilter? saturationFilter = null;
            if (Math.Abs(saturation - 1.0f) > 0.01f)
            {
                saturationFilter = CreateSaturationFilter(saturation);
            }

            // Combine filters
            SKImageFilter? combinedFilter = blurFilter;
            if (saturationFilter != null)
            {
                combinedFilter = SKImageFilter.CreateColorFilter(saturationFilter, blurFilter);
            }

            // Draw blurred background
            using (var paint = new SKPaint { ImageFilter = combinedFilter, IsAntialias = true })
            {
                canvas.DrawBitmap(source, 0, 0, paint);
            }

            // Draw tint overlay
            if (tintOpacity > 0.01f)
            {
                var tintWithOpacity = new SKColor(
                    tintColor.Red,
                    tintColor.Green,
                    tintColor.Blue,
                    (byte)(255 * tintOpacity));

                using var tintPaint = new SKPaint
                {
                    Color = tintWithOpacity,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                if (roundRect != null)
                    canvas.DrawRoundRect(roundRect, tintPaint);
                else
                    canvas.DrawRect(rect, tintPaint);
            }

            // Draw subtle highlight border (glass reflection effect)
            using var highlightPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };

            var highlightColors = new SKColor[]
            {
                new(255, 255, 255, 60),
                new(255, 255, 255, 0)
            };

            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, source.Height),
                highlightColors,
                null,
                SKShaderTileMode.Clamp);

            highlightPaint.Shader = shader;

            if (roundRect != null)
                canvas.DrawRoundRect(roundRect, highlightPaint);
            else
                canvas.DrawRect(rect, highlightPaint);

            // Cleanup
            combinedFilter?.Dispose();
            if (combinedFilter != blurFilter)
                blurFilter?.Dispose();
            saturationFilter?.Dispose();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);

            return data.ToArray();
        }

        /// <summary>
        /// Creates a saturation color filter using a color matrix.
        /// </summary>
        /// <param name="saturation">Saturation level (0.0 = grayscale, 1.0 = normal).</param>
        private static SKColorFilter CreateSaturationFilter(float saturation)
        {
            // Standard luminance weights (ITU-R BT.709)
            const float rWeight = 0.2126f;
            const float gWeight = 0.7152f;
            const float bWeight = 0.0722f;

            float invSat = 1.0f - saturation;
            float sr = invSat * rWeight;
            float sg = invSat * gWeight;
            float sb = invSat * bWeight;

            // Color matrix for saturation adjustment
            // Each row: [R, G, B, A, Offset]
            float[] matrix =
            [
                sr + saturation, sr, sr, 0, 0,
                sg, sg + saturation, sg, 0, 0,
                sb, sb, sb + saturation, 0, 0,
                0, 0, 0, 1, 0
            ];

            return SKColorFilter.CreateColorMatrix(matrix);
        }

        /// <summary>
        /// Converts raw BGRA pixel data to an SKBitmap.
        /// </summary>
        public static SKBitmap? FromPixelData(byte[] pixels, int width, int height)
        {
            if (pixels == null || width <= 0 || height <= 0)
                return null;

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);

            var ptr = bitmap.GetPixels();
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, ptr, pixels.Length);

            return bitmap;
        }

        /// <summary>
        /// Extracts raw BGRA pixel data from an SKBitmap.
        /// </summary>
        public static byte[]? ToPixelData(SKBitmap bitmap)
        {
            if (bitmap == null)
                return null;

            var pixels = new byte[bitmap.ByteCount];
            var ptr = bitmap.GetPixels();
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);

            return pixels;
        }
    }
}

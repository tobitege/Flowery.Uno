using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Windows.Storage.Streams;

namespace Flowery.Skia
{
    /// <summary>
    /// Captures backdrop content behind a UI element and processes it with SkiaSharp.
    /// Provides the bridge between WinUI rendering and Skia image processing.
    /// </summary>
    public static class SkiaBackdropCapture
    {
        /// <summary>
        /// Captures the area behind an element and applies glass effect.
        /// </summary>
        /// <param name="element">The element to capture behind.</param>
        /// <param name="blurRadius">Blur amount (0-100, will be converted to sigma).</param>
        /// <param name="saturation">Saturation level (0.0 = grayscale, 1.0 = normal).</param>
        /// <param name="tintColor">Tint overlay color.</param>
        /// <param name="tintOpacity">Tint opacity (0.0 - 1.0).</param>
        /// <param name="cornerRadius">Corner radius for the glass effect.</param>
        /// <returns>An ImageSource containing the glass effect, or null if capture failed.</returns>
        public static async Task<ImageSource?> CaptureAndApplyGlassEffectAsync(
            FrameworkElement element,
            double blurRadius,
            double saturation,
            Windows.UI.Color tintColor,
            double tintOpacity,
            double cornerRadius = 0)
        {
            try
            {
                // Find the background source (parent with content)
                var backgroundSource = FindBackgroundSource(element);
                if (backgroundSource == null)
                    return null;

                // Get element bounds
                double width = element.ActualWidth;
                double height = element.ActualHeight;
                if (width <= 0 || height <= 0)
                    return null;

                // Get position relative to background source
                var transform = element.TransformToVisual(backgroundSource);
                var topLeft = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

                // Calculate capture area with blur padding
                double blurPadding = Math.Min(blurRadius, 20);
                double captureX = Math.Max(0, topLeft.X - blurPadding);
                double captureY = Math.Max(0, topLeft.Y - blurPadding);
                double captureWidth = Math.Min(width + blurPadding * 2, backgroundSource.ActualWidth - captureX);
                double captureHeight = Math.Min(height + blurPadding * 2, backgroundSource.ActualHeight - captureY);

                if (captureWidth <= 0 || captureHeight <= 0)
                    return null;

                // Temporarily hide the element during capture
                double originalOpacity = element.Opacity;
                element.Opacity = 0;

                try
                {
                    // Capture the background source
                    var bitmap = await CaptureElementAsync(backgroundSource, captureX, captureY, captureWidth, captureHeight);
                    if (bitmap == null)
                        return null;

                    // Apply glass effect with Skia
                    float blurSigma = (float)(blurRadius / 10.0); // Convert to sigma
                    var skTintColor = new SKColor(tintColor.R, tintColor.G, tintColor.B);

                    byte[]? glassBytes = SkiaImageProcessor.ApplyGlassEffect(
                        bitmap,
                        blurSigma,
                        (float)saturation,
                        skTintColor,
                        (float)tintOpacity,
                        (float)cornerRadius);

                    bitmap.Dispose();

                    if (glassBytes == null)
                        return null;

                    // Convert back to ImageSource
                    return await BytesToImageSourceAsync(glassBytes);
                }
                finally
                {
                    element.Opacity = originalOpacity;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkiaBackdropCapture] Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Captures a region of a UI element as an SKBitmap.
        /// </summary>
        public static async Task<SKBitmap?> CaptureElementAsync(
            FrameworkElement element,
            double x,
            double y,
            double width,
            double height)
        {
            try
            {
                // Render at reduced resolution for performance
                double scale = 0.75;
                int requestedWidth = (int)Math.Ceiling(element.ActualWidth * scale);
                int requestedHeight = (int)Math.Ceiling(element.ActualHeight * scale);

                if (requestedWidth <= 0 || requestedHeight <= 0)
                    return null;

                var renderTarget = new RenderTargetBitmap();
                await renderTarget.RenderAsync(element, requestedWidth, requestedHeight);

                // Use actual rendered dimensions, not requested dimensions
                int actualWidth = renderTarget.PixelWidth;
                int actualHeight = renderTarget.PixelHeight;

                if (actualWidth <= 0 || actualHeight <= 0)
                    return null;

                var pixelBuffer = await renderTarget.GetPixelsAsync();
                var pixels = pixelBuffer.ToArray();

                // Verify pixel buffer size matches expected dimensions
                int expectedSize = actualWidth * actualHeight * 4; // BGRA = 4 bytes per pixel
                if (pixels.Length != expectedSize)
                {
                    System.Diagnostics.Debug.WriteLine($"[SkiaBackdropCapture] Pixel buffer size mismatch: expected {expectedSize}, got {pixels.Length}");
                    return null;
                }

                // Create full bitmap with actual dimensions
                var fullBitmap = SkiaImageProcessor.FromPixelData(pixels, actualWidth, actualHeight);
                if (fullBitmap == null)
                    return null;

                // Calculate scale ratio for crop coordinates
                double actualScale = (double)actualWidth / element.ActualWidth;

                // If we need to crop to a specific region
                int cropX = (int)(x * actualScale);
                int cropY = (int)(y * actualScale);
                int cropWidth = (int)(width * actualScale);
                int cropHeight = (int)(height * actualScale);

                // Clamp to valid bounds
                cropX = Math.Max(0, Math.Min(cropX, fullBitmap.Width - 1));
                cropY = Math.Max(0, Math.Min(cropY, fullBitmap.Height - 1));
                cropWidth = Math.Min(cropWidth, fullBitmap.Width - cropX);
                cropHeight = Math.Min(cropHeight, fullBitmap.Height - cropY);

                if (cropWidth <= 0 || cropHeight <= 0)
                {
                    fullBitmap.Dispose();
                    return null;
                }

                // If crop region is the full bitmap, return as-is
                if (cropX == 0 && cropY == 0 && cropWidth == fullBitmap.Width && cropHeight == fullBitmap.Height)
                {
                    return fullBitmap;
                }

                // Extract the region
                var cropRect = new SKRectI(cropX, cropY, cropX + cropWidth, cropY + cropHeight);
                var croppedBitmap = new SKBitmap(cropWidth, cropHeight);

                if (!fullBitmap.ExtractSubset(croppedBitmap, cropRect))
                {
                    fullBitmap.Dispose();
                    croppedBitmap.Dispose();
                    return null;
                }

                fullBitmap.Dispose();
                return croppedBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkiaBackdropCapture] CaptureElement error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds the appropriate background source element (parent with background).
        /// </summary>
        private static FrameworkElement? FindBackgroundSource(FrameworkElement element)
        {
            DependencyObject? current = VisualTreeHelper.GetParent(element);

            while (current != null)
            {
                // Stop at Page level (top of visual tree)
                if (current is Page)
                    break;

                // Check for background on Border or Panel
                if (current is Border { Background: not null } border)
                    return border;

                if (current is Panel { Background: not null } panel)
                    return panel;

                // Stop if we've reached the root (no more parents)
                var parent = VisualTreeHelper.GetParent(current);
                if (parent == null)
                    break;

                current = parent;
            }

            // Fallback: immediate parent
            return VisualTreeHelper.GetParent(element) as FrameworkElement;
        }

        /// <summary>
        /// Converts PNG bytes to a WinUI ImageSource.
        /// </summary>
        private static async Task<ImageSource?> BytesToImageSourceAsync(byte[] pngBytes)
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(pngBytes.AsBuffer());
                stream.Seek(0);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);

                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkiaBackdropCapture] BytesToImageSource error: {ex.Message}");
                return null;
            }
        }
    }
}

using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace Flowery.Controls.ColorPicker
{
    internal static class ColorPickerRendering
    {
        public static WriteableBitmap CreateCheckerboardBitmap(int width, int height, int checkSize, Color light, Color dark)
        {
            if (width <= 0 || height <= 0)
            {
                return new WriteableBitmap(1, 1);
            }

            var bitmap = new WriteableBitmap(width, height);
            var pixels = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isLight = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                    var color = isLight ? light : dark;
                    int offset = (y * width + x) * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                    pixels[offset + 3] = color.A;
                }
            }

            WritePixels(bitmap, pixels);
            return bitmap;
        }

        public static void WritePixels(WriteableBitmap bitmap, byte[] pixels)
        {
            using var stream = bitmap.PixelBuffer.AsStream();
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(pixels, 0, pixels.Length);
            bitmap.Invalidate();
        }
    }
}

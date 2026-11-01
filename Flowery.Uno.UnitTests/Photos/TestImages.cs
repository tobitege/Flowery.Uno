using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    /// <summary>Builds small images in memory so the tests need no fixture files.</summary>
    internal static class TestImages
    {
        public static SKBitmap Gradient(int width, int height)
        {
            var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                    bitmap.SetPixel(x, y, new SKColor((byte)(x * 255 / Math.Max(1, width - 1)), (byte)(y * 255 / Math.Max(1, height - 1)), 128));
            }
            return bitmap;
        }

        public static byte[] Encode(int width, int height, SKEncodedImageFormat format)
        {
            using var bitmap = Gradient(width, height);
            using var data = bitmap.Encode(format, 90);
            return data.ToArray();
        }

        public static string Write(string path, int width, int height, SKEncodedImageFormat? format = null)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            format ??= ext switch
            {
                ".png" => SKEncodedImageFormat.Png,
                ".webp" => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg
            };
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, Encode(width, height, format.Value));
            return path;
        }

        /// <summary>A JPEG with an EXIF segment (little endian) holding orientation, make, model and capture date.</summary>
        public static byte[] JpegWithExif(int width, int height, int orientation, string make, string model, string dateTaken)
        {
            var jpeg = Encode(width, height, SKEncodedImageFormat.Jpeg);

            var makeBytes = Encoding.ASCII.GetBytes(make + "\0");
            var modelBytes = Encoding.ASCII.GetBytes(model + "\0");
            var dateBytes = Encoding.ASCII.GetBytes(dateTaken + "\0");

            const int ifd0Size = 2 + 4 * 12 + 4;
            var makeOffset = 8 + ifd0Size;
            var modelOffset = makeOffset + makeBytes.Length;
            var exifIfdOffset = modelOffset + modelBytes.Length;
            var dateOffset = exifIfdOffset + 2 + 12 + 4;

            using var tiff = new MemoryStream();
            using (var w = new BinaryWriter(tiff, Encoding.ASCII, leaveOpen: true))
            {
                w.Write("II"u8);
                w.Write((ushort)42);
                w.Write((uint)8);

                w.Write((ushort)4);
                WriteEntry(w, 0x010F, 2, (uint)makeBytes.Length, (uint)makeOffset);
                WriteEntry(w, 0x0110, 2, (uint)modelBytes.Length, (uint)modelOffset);
                WriteEntry(w, 0x0112, 3, 1, (uint)orientation);
                WriteEntry(w, 0x8769, 4, 1, (uint)exifIfdOffset);
                w.Write((uint)0);

                w.Write(makeBytes);
                w.Write(modelBytes);

                w.Write((ushort)1);
                WriteEntry(w, 0x9003, 2, (uint)dateBytes.Length, (uint)dateOffset);
                w.Write((uint)0);
                w.Write(dateBytes);
            }

            var payload = new List<byte>();
            payload.AddRange("Exif\0\0"u8.ToArray());
            payload.AddRange(tiff.ToArray());

            var segment = new byte[4 + payload.Count];
            segment[0] = 0xFF;
            segment[1] = 0xE1;
            BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(payload.Count + 2));
            payload.CopyTo(segment, 4);

            // Insert right after the SOI marker.
            var result = new byte[jpeg.Length + segment.Length];
            jpeg.AsSpan(0, 2).CopyTo(result);
            segment.CopyTo(result, 2);
            jpeg.AsSpan(2).CopyTo(result.AsSpan(2 + segment.Length));
            return result;
        }

        private static void WriteEntry(BinaryWriter w, ushort tag, ushort type, uint count, uint value)
        {
            w.Write(tag);
            w.Write(type);
            w.Write(count);
            w.Write(value);
        }

        public static string TempDirectory(string prefix) => Directory.CreateTempSubdirectory(prefix).FullName;

        public static void DeleteQuietly(string directory)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // Temp folders are cleaned by the OS eventually.
            }
        }
    }
}

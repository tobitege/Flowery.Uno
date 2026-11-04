using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Flowery.Uno.Photos.Services;
using Flowery.Uno.Photos.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_ExifDetails
    {
        private string _dir = null!;

        [TestInitialize]
        public void Create() => _dir = TestImages.TempDirectory("photos-exif-");

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        public void When_TiffHasCameraExposureAndGps_AllFieldsAreRead()
        {
            var exif = ExifDetails.Parse(BuildTiff(latRef: "N", lonRef: "E"))!;

            Assert.AreEqual(new DateTime(2024, 5, 17, 18, 30, 5), exif.DateTaken);
            Assert.AreEqual("Acme Cam 9", exif.Camera);
            Assert.AreEqual("Acme 24-70mm", exif.Lens);
            Assert.AreEqual(2.8, exif.FNumber!.Value, 1e-9);
            Assert.AreEqual(1.0 / 250, exif.ExposureSeconds!.Value, 1e-9);
            Assert.AreEqual(400, exif.Iso);
            Assert.AreEqual(35.0, exif.FocalLengthMm!.Value, 1e-9);
            // 45° 30' 36" = 45.51
            Assert.AreEqual(45.51, exif.Latitude!.Value, 1e-9);
            Assert.AreEqual(9.25, exif.Longitude!.Value, 1e-9);
        }

        [TestMethod]
        public void When_GpsReferenceIsSouthAndWest_CoordinatesAreNegative()
        {
            var exif = ExifDetails.Parse(BuildTiff(latRef: "S", lonRef: "W"))!;

            Assert.AreEqual(-45.51, exif.Latitude!.Value, 1e-9);
            Assert.AreEqual(-9.25, exif.Longitude!.Value, 1e-9);
        }

        [TestMethod]
        public void When_ModelStartsWithMake_CameraIsNotDuplicated()
        {
            var exif = ExifDetails.Parse(BuildTiff(make: "Acme", model: "Acme Pro", latRef: "N", lonRef: "E"))!;

            Assert.AreEqual("Acme Pro", exif.Camera);
        }

        [TestMethod]
        public void When_Truncated_ParseDoesNotThrow()
        {
            var tiff = BuildTiff(latRef: "N", lonRef: "E");

            for (var length = 0; length < tiff.Length; length += 7)
                _ = ExifDetails.Parse(tiff.AsSpan(0, length));
        }

        [TestMethod]
        public void When_ByteOrderMarkIsWrong_ParseReturnsNull()
        {
            Assert.IsNull(ExifDetails.Parse("XX\0\0\0\0\0\0"u8));
        }

        [TestMethod]
        public void When_FileIsAJpegWithExif_ReadFindsTheSegment()
        {
            var path = Path.Combine(_dir, "photo.jpg");
            File.WriteAllBytes(path, TestImages.JpegWithExif(40, 30, 1, "Acme", "Camera", "2024:01:02 03:04:05"));

            var exif = ExifDetails.Read(path)!;

            Assert.AreEqual("Acme Camera", exif.Camera);
            Assert.AreEqual(new DateTime(2024, 1, 2, 3, 4, 5), exif.DateTaken);
            Assert.IsNull(exif.FNumber);
            Assert.IsNull(exif.Latitude);
        }

        [TestMethod]
        public void When_FileHasNoExifOrIsNotAJpeg_ReadReturnsNull()
        {
            var png = TestImages.Write(Path.Combine(_dir, "a.png"), 10, 10, SkiaSharp.SKEncodedImageFormat.Png);
            var plainJpeg = TestImages.Write(Path.Combine(_dir, "b.jpg"), 10, 10, SkiaSharp.SKEncodedImageFormat.Jpeg);

            Assert.IsNull(ExifDetails.Read(png));
            Assert.IsNull(ExifDetails.Read(plainJpeg));
            Assert.IsNull(ExifDetails.Read(Path.Combine(_dir, "missing.jpg")));
        }

        [TestMethod]
        public void When_ExposureIsFormatted_ItFollowsTheWinUiOrder()
        {
            var exif = new ExifInfo(null, null, null, 2.8, 1.0 / 250, 400, 35, null, null);

            Assert.AreEqual("ƒ/2.8  ·  1/250 s  ·  ISO 400  ·  35 mm", ViewerFormat.Exposure(exif, CultureInfo.InvariantCulture));
            Assert.AreEqual("2 s", ViewerFormat.Exposure(new ExifInfo(null, null, null, null, 2, null, null, null, null), CultureInfo.InvariantCulture));
            Assert.AreEqual(string.Empty, ViewerFormat.Exposure(new ExifInfo(null, null, null, null, null, null, null, null, null), CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Little endian TIFF: IFD0 (make, model, Exif and GPS pointers), the Exif IFD (exposure, aperture, ISO, date, focal length, lens)
        /// and the GPS IFD (45° 30' 36" and 9° 15' 0").
        /// </summary>
        private static byte[] BuildTiff(string make = "Acme", string model = "Cam 9", string latRef = "N", string lonRef = "E")
        {
            const int ifd0 = 8;
            const int exifIfd = ifd0 + 2 + 4 * 12 + 4;
            const int gpsIfd = exifIfd + 2 + 6 * 12 + 4;
            const int data = gpsIfd + 2 + 4 * 12 + 4;

            using var tiff = new MemoryStream();
            using var w = new BinaryWriter(tiff, Encoding.ASCII, leaveOpen: true);
            var blob = new MemoryStream();
            using var blobWriter = new BinaryWriter(blob, Encoding.ASCII, leaveOpen: true);

            uint Alloc(byte[] bytes)
            {
                var offset = (uint)(data + blob.Length);
                blobWriter.Write(bytes);
                return offset;
            }

            static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text + "\0");

            static byte[] Rationals(params (uint N, uint D)[] values)
            {
                var bytes = new byte[values.Length * 8];
                for (var i = 0; i < values.Length; i++)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 8), values[i].N);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 8 + 4), values[i].D);
                }
                return bytes;
            }

            static uint Inline(string text) => BitConverter.ToUInt32([.. Encoding.ASCII.GetBytes(text + "\0\0\0\0").Take(4)]);

            void Entry(ushort tag, ushort type, uint count, uint value)
            {
                w.Write(tag);
                w.Write(type);
                w.Write(count);
                w.Write(value);
            }

            var makeBytes = Ascii(make);
            var modelBytes = Ascii(model);
            var dateBytes = Ascii("2024:05:17 18:30:05");
            var lensBytes = Ascii("Acme 24-70mm");

            var makeOffset = Alloc(makeBytes);
            var modelOffset = Alloc(modelBytes);
            var exposureOffset = Alloc(Rationals((1, 250)));
            var fNumberOffset = Alloc(Rationals((28, 10)));
            var dateOffset = Alloc(dateBytes);
            var focalOffset = Alloc(Rationals((35, 1)));
            var lensOffset = Alloc(lensBytes);
            var latOffset = Alloc(Rationals((45, 1), (30, 1), (36, 1)));
            var lonOffset = Alloc(Rationals((9, 1), (15, 1), (0, 1)));

            w.Write("II"u8);
            w.Write((ushort)42);
            w.Write((uint)ifd0);

            w.Write((ushort)4);
            Entry(0x010F, 2, (uint)makeBytes.Length, makeOffset);
            Entry(0x0110, 2, (uint)modelBytes.Length, modelOffset);
            Entry(0x8769, 4, 1, exifIfd);
            Entry(0x8825, 4, 1, gpsIfd);
            w.Write((uint)0);

            w.Write((ushort)6);
            Entry(0x829A, 5, 1, exposureOffset);
            Entry(0x829D, 5, 1, fNumberOffset);
            Entry(0x8827, 3, 1, 400);
            Entry(0x9003, 2, (uint)dateBytes.Length, dateOffset);
            Entry(0x920A, 5, 1, focalOffset);
            Entry(0xA434, 2, (uint)lensBytes.Length, lensOffset);
            w.Write((uint)0);

            w.Write((ushort)4);
            Entry(1, 2, 2, Inline(latRef));
            Entry(2, 5, 3, latOffset);
            Entry(3, 2, 2, Inline(lonRef));
            Entry(4, 5, 3, lonOffset);
            w.Write((uint)0);

            w.Write(blob.ToArray());
            return tiff.ToArray();
        }
    }
}

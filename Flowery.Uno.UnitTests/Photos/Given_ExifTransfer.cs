using System.Buffers.Binary;
using Flowery.Uno.Photos.Editing;
using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_ExifTransfer
    {
        private string _dir = null!;

        [TestInitialize]
        public void Create() => _dir = TestImages.TempDirectory("photos-exif-transfer-");

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        public void When_AJpegEditIsSaved_CameraAndDateStayAndThePhotoIsUpright()
        {
            // A portrait photo stored sideways (orientation 6); the edit is already turned upright and cropped.
            var source = Path.Combine(_dir, "photo.jpg");
            File.WriteAllBytes(source, TestImages.JpegWithExif(60, 40, 6, "Acme", "Cam 9", "2024:05:01 10:20:30"));
            var destination = Path.Combine(_dir, "photo (edited).jpg");

            File.WriteAllBytes(destination, ExifTransfer.CarryOver(source, destination, EncodeJpeg(30, 50), 30, 50));

            var exif = ExifDetails.Read(destination);
            Assert.IsNotNull(exif);
            Assert.AreEqual(new DateTime(2024, 5, 1, 10, 20, 30), exif.DateTaken);
            StringAssert.Contains(exif.Camera!, "Cam 9");
            var probe = ImageMetadataReader.ProbeFast(destination);
            Assert.AreEqual(1, probe?.Orientation, "the pixels are upright, so the copy must not turn them again");
            using var decoded = SKImage.FromEncodedData(destination);
            Assert.AreEqual(30, decoded.Width);
            Assert.AreEqual(50, decoded.Height);
        }

        [TestMethod]
        public void When_ThePhotoHasGps_TheLocationStays()
        {
            var source = Path.Combine(_dir, "gps.jpg");
            File.WriteAllBytes(source, ExifTransfer.InsertExif(EncodeJpeg(40, 30), Given_ExifDetails.BuildTiff()));
            var destination = Path.Combine(_dir, "gps (edited).jpg");

            File.WriteAllBytes(destination, ExifTransfer.CarryOver(source, destination, EncodeJpeg(40, 30), 40, 30));

            var exif = ExifDetails.Read(destination);
            Assert.AreEqual(45.51, exif!.Latitude!.Value, 1e-9);
            Assert.AreEqual(9.25, exif.Longitude!.Value, 1e-9);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void When_TheBlockIsPrepared_OrientationSizeAndThumbnailLinkAreUpdated(bool little)
        {
            var tiff = BuildTiff(little, orientation: 8, width: 4000, height: 3000, thumbnailIfd: 200);

            var prepared = ExifTransfer.PrepareTiff(tiff, 1200, 900)!;

            Assert.AreEqual(1, U16(prepared, 8 + 2 + 8, little), "orientation");
            Assert.AreEqual(0u, U32(prepared, 8 + 2 + 2 * 12, little), "the link to the old thumbnail is gone");
            const int exifIfd = 8 + 2 + 2 * 12 + 4;
            Assert.AreEqual(1200u, U32(prepared, exifIfd + 2 + 8, little), "PixelXDimension (LONG)");
            Assert.AreEqual(900, U16(prepared, exifIfd + 2 + 12 + 8, little), "PixelYDimension (SHORT)");
            Assert.AreEqual(tiff.Length, prepared.Length, "nothing moves, so offsets inside the block stay valid");
        }

        [TestMethod]
        [DataRow(0xFFFFFFFFu)]
        [DataRow(0xFFFFFFFEu)]
        [DataRow(5000u)]
        public void When_TheExifBlockPointsOutside_TheEditIsSavedWithoutItAndTheInfoPanelStaysEmpty(uint ifd0)
        {
            // An IFD offset near uint.MaxValue wrapped around in the bounds checks and then threw.
            var tiff = new byte[64];
            tiff[0] = tiff[1] = (byte)'I';
            BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(2), 42);
            BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), ifd0);
            var source = Path.Combine(_dir, "broken.jpg");
            File.WriteAllBytes(source, ExifTransfer.InsertExif(EncodeJpeg(20, 20), tiff));
            var encoded = EncodeJpeg(20, 20);

            Assert.AreSame(encoded, ExifTransfer.CarryOver(source, Path.Combine(_dir, "broken (edited).jpg"), encoded, 20, 20));
            Assert.IsNull(ExifDetails.Read(source)?.Camera);
        }

        [TestMethod]
        public void When_TheExifIfdPointsOutside_OrientationIsStillReset()
        {
            var tiff = BuildTiff(little: true, orientation: 6, width: 400, height: 300, thumbnailIfd: 0);
            BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(8 + 2 + 12 + 8), 0xFFFFFFFE);

            var prepared = ExifTransfer.PrepareTiff(tiff, 40, 30)!;

            Assert.AreEqual(1, U16(prepared, 8 + 2 + 8, little: true));
        }

        [TestMethod]
        public void When_TheEditIsNotAJpeg_NoExifIsAdded()
        {
            var source = Path.Combine(_dir, "photo.jpg");
            File.WriteAllBytes(source, TestImages.JpegWithExif(60, 40, 1, "Acme", "Cam 9", "2024:05:01 10:20:30"));
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

            Assert.AreSame(png, ExifTransfer.CarryOver(source, Path.Combine(_dir, "photo.png"), png, 60, 40));
        }

        [TestMethod]
        public void When_TheSourceHasNoExif_TheEditIsSavedAsEncoded()
        {
            var source = Path.Combine(_dir, "plain.jpg");
            File.WriteAllBytes(source, EncodeJpeg(20, 20));
            var encoded = EncodeJpeg(10, 10);

            Assert.AreSame(encoded, ExifTransfer.CarryOver(source, Path.Combine(_dir, "plain (edited).jpg"), encoded, 10, 10));
        }

        [TestMethod]
        public void When_SkiaWritesAJfifHeader_TheExifSegmentFollowsIt()
        {
            var jpeg = EncodeJpeg(10, 10);
            var withExif = ExifTransfer.InsertExif(jpeg, Given_ExifDetails.BuildTiff());

            var at = jpeg[3] == 0xE0 ? 4 + BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(4)) : 2;
            Assert.AreEqual(0xFF, withExif[at]);
            Assert.AreEqual(0xE1, withExif[at + 1]);
            using var decoded = SKImage.FromEncodedData(withExif);
            Assert.AreEqual(10, decoded.Width, "the file still decodes");
        }

        private static byte[] EncodeJpeg(int width, int height)
        {
            using var bitmap = new SKBitmap(width, height);
            bitmap.Erase(new SKColor(90, 140, 200));
            using var image = SKImage.FromBitmap(bitmap);
            return EditRenderer.Encode(image, "a.jpg", 90);
        }

        /// <summary>
        /// IFD0 (orientation SHORT, Exif pointer) linking to a thumbnail IFD, then the Exif IFD (PixelXDimension LONG,
        /// PixelYDimension SHORT).
        /// </summary>
        private static byte[] BuildTiff(bool little, int orientation, uint width, int height, uint thumbnailIfd)
        {
            const int exifIfd = 8 + 2 + 2 * 12 + 4;
            var tiff = new byte[exifIfd + 2 + 2 * 12 + 4 + 64];
            var at = 0;
            void U16At(int value) { Write16(tiff, at, (ushort)value, little); at += 2; }
            void U32At(uint value) { Write32(tiff, at, value, little); at += 4; }
            void Entry(ushort tag, ushort type, uint value)
            {
                U16At(tag);
                U16At(type);
                U32At(1);
                if (type == 3)
                {
                    U16At((int)value);
                    U16At(0);
                }
                else
                {
                    U32At(value);
                }
            }

            tiff[0] = tiff[1] = (byte)(little ? 'I' : 'M');
            at = 2;
            U16At(42);
            U32At(8);
            U16At(2);
            Entry(0x0112, 3, (uint)orientation);
            Entry(0x8769, 4, exifIfd);
            U32At(thumbnailIfd);
            U16At(2);
            Entry(0xA002, 4, width);
            Entry(0xA003, 3, (uint)height);
            U32At(0);
            return tiff;
        }

        private static void Write16(byte[] data, int offset, ushort value, bool little)
        {
            if (little)
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);
            else
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset), value);
        }

        private static void Write32(byte[] data, int offset, uint value, bool little)
        {
            if (little)
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
            else
                BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset), value);
        }

        private static int U16(byte[] data, int offset, bool little) =>
            little ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)) : BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset));

        private static uint U32(byte[] data, int offset, bool little) =>
            little ? BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)) : BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
    }
}

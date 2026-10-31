using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_ImageMetadataReader
    {
        private string _dir = null!;

        [TestInitialize]
        public void Create() => _dir = TestImages.TempDirectory("photos-meta-");

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        [DataRow("a.jpg", 120, 80)]
        [DataRow("a.png", 64, 200)]
        [DataRow("a.webp", 90, 45)]
        public void When_FormatIsParsedByHeader_SizeIsRead(string name, int width, int height)
        {
            var path = TestImages.Write(Path.Combine(_dir, name), width, height);

            var probe = ImageMetadataReader.ProbeFast(path);

            Assert.IsNotNull(probe);
            Assert.AreEqual(width, probe.Value.Width);
            Assert.AreEqual(height, probe.Value.Height);
            Assert.IsNull(probe.Value.DateTaken);
        }

        [TestMethod]
        public void When_FormatIsGifOrBmp_SizeIsRead()
        {
            var gif = Path.Combine(_dir, "a.gif");
            File.WriteAllBytes(gif, [.. "GIF89a"u8.ToArray(), 40, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
            var bmp = Path.Combine(_dir, "a.bmp");
            using (var bitmap = TestImages.Gradient(33, 17))
            using (var data = bitmap.Encode(SKEncodedImageFormat.Bmp, 100))
            {
                File.WriteAllBytes(bmp, data?.ToArray() ?? BuildBmp(33, 17));
            }

            var gifProbe = ImageMetadataReader.ProbeFast(gif);
            var bmpProbe = ImageMetadataReader.ProbeFast(bmp);

            Assert.AreEqual((40, 30), (gifProbe!.Value.Width, gifProbe.Value.Height));
            Assert.AreEqual((33, 17), (bmpProbe!.Value.Width, bmpProbe.Value.Height));
        }

        [TestMethod]
        public void When_JpegHasExif_DateCameraAndOrientationAreRead()
        {
            var path = Path.Combine(_dir, "exif.jpg");
            File.WriteAllBytes(path, TestImages.JpegWithExif(200, 100, orientation: 6, "Acme", "Cam 1", "2024:03:05 14:30:00"));

            var probe = ImageMetadataReader.ProbeFast(path);

            Assert.IsNotNull(probe);
            Assert.AreEqual(6, probe.Value.Orientation);
            // Orientation 6 rotates by 90 degrees, so the reported size is the displayed one.
            Assert.AreEqual(100, probe.Value.Width);
            Assert.AreEqual(200, probe.Value.Height);
            Assert.AreEqual(new DateTime(2024, 3, 5, 14, 30, 0), probe.Value.DateTaken);
            Assert.AreEqual("Acme Cam 1", probe.Value.CameraModel);
        }

        [TestMethod]
        public void When_ModelRepeatsTheMake_CameraIsNotDuplicated()
        {
            var path = Path.Combine(_dir, "exif.jpg");
            File.WriteAllBytes(path, TestImages.JpegWithExif(40, 40, orientation: 1, "Acme", "Acme Pro", "2020:01:02 03:04:05"));

            var probe = ImageMetadataReader.ProbeFast(path);

            Assert.AreEqual("Acme Pro", probe!.Value.CameraModel);
            Assert.AreEqual(40, probe.Value.Width);
        }

        [TestMethod]
        public void When_FileIsNotAnImage_ProbeFastReturnsNull()
        {
            var path = Path.Combine(_dir, "notes.jpg");
            File.WriteAllText(path, "this is not an image, just a sentence long enough to be read");

            Assert.IsNull(ImageMetadataReader.ProbeFast(path));
        }

        [TestMethod]
        public async Task When_HeaderParsingFails_ProbeAsyncUsesTheCodecOrGivesUp()
        {
            var png = TestImages.Write(Path.Combine(_dir, "a.png"), 50, 20);
            var junk = Path.Combine(_dir, "junk.heic");
            File.WriteAllBytes(junk, new byte[64]);

            Assert.IsNotNull(ImageMetadataReader.ProbeWithCodec(png));
            Assert.AreEqual(50, ImageMetadataReader.ProbeWithCodec(png)!.Value.Width);
            Assert.IsNull(await ImageMetadataReader.ProbeAsync(junk));
        }

        [TestMethod]
        public void When_ExtensionIsKnown_IsSupportedIgnoresCase()
        {
            Assert.IsTrue(ImageMetadataReader.IsSupported("/x/Photo.JPG"));
            Assert.IsTrue(ImageMetadataReader.IsSupported("/x/photo.heic"));
            Assert.IsFalse(ImageMetadataReader.IsSupported("/x/notes.txt"));
        }

        private static byte[] BuildBmp(int width, int height)
        {
            var bytes = new byte[54];
            bytes[0] = (byte)'B';
            bytes[1] = (byte)'M';
            BitConverter.GetBytes(width).CopyTo(bytes, 18);
            BitConverter.GetBytes(height).CopyTo(bytes, 22);
            return bytes;
        }
    }
}

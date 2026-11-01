using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_ThumbnailDecoder
    {
        private string _dir = null!;

        [TestInitialize]
        public void Create() => _dir = TestImages.TempDirectory("photos-thumb-");

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        [DataRow("a.jpg")]
        [DataRow("a.png")]
        [DataRow("a.webp")]
        public void When_ImageIsLarger_HeightIsCappedAndAspectKept(string name)
        {
            var path = TestImages.Write(Path.Combine(_dir, name), 800, 400);

            using var bitmap = ThumbnailDecoder.Decode(path, 100);

            Assert.IsNotNull(bitmap);
            Assert.AreEqual(100, bitmap.Height);
            Assert.AreEqual(200, bitmap.Width, 2);
        }

        [TestMethod]
        public void When_ImageIsSmaller_ItIsNotUpscaled()
        {
            var path = TestImages.Write(Path.Combine(_dir, "small.png"), 60, 30);

            using var bitmap = ThumbnailDecoder.Decode(path, 300);

            Assert.IsNotNull(bitmap);
            Assert.AreEqual((60, 30), (bitmap.Width, bitmap.Height));
        }

        [TestMethod]
        public void When_JpegHasExifOrientation_ThumbnailIsUpright()
        {
            var path = Path.Combine(_dir, "o6.jpg");
            File.WriteAllBytes(path, TestImages.JpegWithExif(400, 200, orientation: 6, "Acme", "Cam", "2024:01:01 00:00:00"));

            using var bitmap = ThumbnailDecoder.Decode(path, 100);

            Assert.IsNotNull(bitmap);
            // Stored 400x200, displayed rotated: portrait, height capped at 100 means width 50.
            Assert.AreEqual(100, bitmap.Height);
            Assert.AreEqual(50, bitmap.Width, 2);
        }

        [TestMethod]
        public void When_FormatCannotBeDecoded_ResultIsNull()
        {
            var path = Path.Combine(_dir, "a.heic");
            File.WriteAllBytes(path, new byte[256]);

            Assert.IsNull(ThumbnailDecoder.Decode(path, 100));
        }

        [TestMethod]
        [DataRow(SKEncodedOrigin.TopLeft)]
        [DataRow(SKEncodedOrigin.TopRight)]
        [DataRow(SKEncodedOrigin.BottomRight)]
        [DataRow(SKEncodedOrigin.BottomLeft)]
        [DataRow(SKEncodedOrigin.LeftTop)]
        [DataRow(SKEncodedOrigin.RightTop)]
        [DataRow(SKEncodedOrigin.RightBottom)]
        [DataRow(SKEncodedOrigin.LeftBottom)]
        public void When_OriginIsApplied_EveryPixelMovesToItsUprightPosition(SKEncodedOrigin origin)
        {
            const int w = 3, h = 2;
            var source = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                    source.SetPixel(x, y, new SKColor((byte)(10 + x * 40), (byte)(10 + y * 100), 200));
            }

            using var result = ThumbnailDecoder.ApplyOrigin(source, origin);

            var swap = ThumbnailDecoder.SwapsAxes(origin);
            Assert.AreEqual(swap ? h : w, result.Width);
            Assert.AreEqual(swap ? w : h, result.Height);

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var (ux, uy) = origin switch
                    {
                        SKEncodedOrigin.TopRight => (w - 1 - x, y),
                        SKEncodedOrigin.BottomRight => (w - 1 - x, h - 1 - y),
                        SKEncodedOrigin.BottomLeft => (x, h - 1 - y),
                        SKEncodedOrigin.LeftTop => (y, x),
                        SKEncodedOrigin.RightTop => (h - 1 - y, x),
                        SKEncodedOrigin.RightBottom => (h - 1 - y, w - 1 - x),
                        SKEncodedOrigin.LeftBottom => (y, w - 1 - x),
                        _ => (x, y)
                    };
                    var expected = new SKColor((byte)(10 + x * 40), (byte)(10 + y * 100), 200);
                    Assert.AreEqual(expected, result.GetPixel(ux, uy), $"{origin}: pixel ({x},{y}) should land at ({ux},{uy})");
                }
            }
        }

        [TestMethod]
        public void When_OriginIsRightTop_TheStoredTopLeftEndsUpTopRight()
        {
            // EXIF 6: the camera was rotated, the stored top-left corner is the displayed top-right corner.
            var source = new SKBitmap(new SKImageInfo(4, 2, SKColorType.Bgra8888, SKAlphaType.Premul));
            source.Erase(SKColors.Blue);
            source.SetPixel(0, 0, SKColors.Red);

            using var result = ThumbnailDecoder.ApplyOrigin(source, SKEncodedOrigin.RightTop);

            Assert.AreEqual((2, 4), (result.Width, result.Height));
            Assert.AreEqual(SKColors.Red, result.GetPixel(1, 0));
        }
    }
}

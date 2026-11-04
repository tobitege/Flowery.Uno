using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    /// <summary>
    /// One decode path (ThumbnailDecoder) bakes the EXIF orientation into the pixels; the header probe, the thumbnail and
    /// the disk cache must all agree, and nothing may rotate a second time. SKBitmap.Decode ignores the orientation,
    /// SKImage.FromEncodedData applies it (phase 0 spike).
    /// </summary>
    [TestClass]
    public class Given_ThumbnailOrientation
    {
        private string _dir = null!;
        private string _photo = null!;

        // Stored 120x60: red grows to the right, green grows downwards (see TestImages.Gradient).
        private const int StoredWidth = 120, StoredHeight = 60;

        [TestInitialize]
        public void Create()
        {
            _dir = TestImages.TempDirectory("photos-orient-");
            _photo = Path.Combine(_dir, "o6.jpg");
            File.WriteAllBytes(_photo, TestImages.JpegWithExif(StoredWidth, StoredHeight, orientation: 6, "Acme", "Cam", "2024:01:01 00:00:00"));
        }

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        public void When_OrientationIs6_ProbeAndThumbnailAgreeOnTheUprightSize()
        {
            var probe = ImageMetadataReader.ProbeFast(_photo)!.Value;

            using var thumb = ThumbnailDecoder.Decode(_photo, 960);

            Assert.IsNotNull(thumb);
            Assert.AreEqual((StoredHeight, StoredWidth), (probe.Width, probe.Height));
            Assert.AreEqual((probe.Width, probe.Height), (thumb.Width, thumb.Height));
        }

        [TestMethod]
        public void When_OrientationIs6_ThumbnailCornersAreRotatedClockwise()
        {
            using var thumb = ThumbnailDecoder.Decode(_photo, 960)!;

            // Stored (x, y) lands at (StoredHeight - 1 - y, x).
            AssertCorner(thumb, 2, 2, expectedRed: false, expectedGreen: true, "stored bottom-left becomes top-left");
            AssertCorner(thumb, thumb.Width - 3, 2, expectedRed: false, expectedGreen: false, "stored top-left becomes top-right");
            AssertCorner(thumb, 2, thumb.Height - 3, expectedRed: true, expectedGreen: true, "stored bottom-right becomes bottom-left");
            AssertCorner(thumb, thumb.Width - 3, thumb.Height - 3, expectedRed: true, expectedGreen: false, "stored top-right becomes bottom-right");
        }

        [TestMethod]
        public void When_ThumbnailComesFromTheDiskCache_ItIsNotRotatedAgain()
        {
            var cache = new ThumbnailDiskCache(Path.Combine(_dir, "thumbs"));
            using var first = cache.GetOrCreate(_photo, 960, ThumbnailDecoder.Decode)!;

            using var second = cache.GetOrCreate(_photo, 960, (_, _) => throw new AssertFailedException("served from disk"))!;

            Assert.AreEqual((first.Width, first.Height), (second.Width, second.Height));
            AssertCorner(second, 2, 2, expectedRed: false, expectedGreen: true, "top-left after the cache round trip");
            AssertCorner(second, second.Width - 3, second.Height - 3, expectedRed: true, expectedGreen: false, "bottom-right after the cache round trip");
        }

        [TestMethod]
        public void When_ThumbnailIsScaledDown_OrientationStillApplies()
        {
            using var thumb = ThumbnailDecoder.Decode(_photo, 60)!;

            Assert.AreEqual(60, thumb.Height);
            Assert.IsTrue(thumb.Height > thumb.Width, "portrait after rotation");
            AssertCorner(thumb, 1, 1, expectedRed: false, expectedGreen: true, "top-left");
        }

        private static void AssertCorner(SKBitmap bitmap, int x, int y, bool expectedRed, bool expectedGreen, string what)
        {
            var color = bitmap.GetPixel(x, y);
            // The gradient runs 0..255 and JPEG is lossy, so only the high and low ends are compared.
            Assert.AreEqual(expectedRed, color.Red > 128, $"{what}: red {color.Red}");
            Assert.AreEqual(expectedGreen, color.Green > 128, $"{what}: green {color.Green}");
        }
    }
}

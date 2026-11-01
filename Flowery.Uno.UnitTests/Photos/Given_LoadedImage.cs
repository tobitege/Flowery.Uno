using Flowery.Uno.Photos.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_LoadedImage
    {
        private string _dir = null!;

        [TestInitialize]
        public void Create() => _dir = TestImages.TempDirectory("photos-loaded-");

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        public void When_PhotoIsSmall_PreviewIsTheFullImage()
        {
            var path = TestImages.Write(Path.Combine(_dir, "small.jpg"), 200, 100);

            using var image = LoadedImage.Load(path);

            Assert.AreEqual((200, 100), (image.Width, image.Height));
            Assert.IsFalse(image.HasSeparatePreview);
            Assert.AreSame(image.Full, image.Preview);
        }

        [TestMethod]
        public void When_PhotoIsLargerThanThePreview_PreviewIsScaledDownAndKeepsTheAspect()
        {
            var path = TestImages.Write(Path.Combine(_dir, "large.png"), 3200, 1600, SKEncodedImageFormat.Png);

            using var image = LoadedImage.Load(path);

            Assert.AreEqual((3200, 1600), (image.Width, image.Height));
            Assert.IsTrue(image.HasSeparatePreview);
            Assert.AreEqual(EditRenderer.PreviewMaxEdge, image.Preview.Width);
            Assert.AreEqual(1280, image.Preview.Height);
        }

        [TestMethod]
        public void When_OrientationIs6_FullImageIsUpright()
        {
            var path = Path.Combine(_dir, "o6.jpg");
            File.WriteAllBytes(path, TestImages.JpegWithExif(120, 60, orientation: 6, "Acme", "Cam", "2024:01:01 00:00:00"));

            using var image = LoadedImage.Load(path);

            // The viewer, the thumbnails and the cache share one decode path: width and height are swapped once.
            Assert.AreEqual((60, 120), (image.Width, image.Height));
        }

        [TestMethod]
        public void When_FileCannotBeDecoded_LoadThrowsInvalidData()
        {
            var path = Path.Combine(_dir, "photo.heic");
            File.WriteAllBytes(path, [0, 0, 0, 24, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63]);

            Assert.ThrowsExactly<InvalidDataException>(() => LoadedImage.Load(path));
        }

        [TestMethod]
        public void When_DisposedTwice_NothingThrows()
        {
            var path = TestImages.Write(Path.Combine(_dir, "p.png"), 50, 50, SKEncodedImageFormat.Png);
            var image = LoadedImage.Load(path);

            image.Dispose();
            image.Dispose();
        }

        [TestMethod]
        public void When_OutputSizeIsRequested_RotationSwapsAxesAndCropScales()
        {
            var size = new Windows.Foundation.Size(400, 200);

            var rotated = EditRenderer.GetOutputSize(size, EditState.Original with { Rotation = 1 });
            var cropped = EditRenderer.GetOutputSize(size, EditState.Original with { Crop = new Windows.Foundation.Rect(0.25, 0, 0.5, 0.5) });

            Assert.AreEqual((200d, 400d), (rotated.Width, rotated.Height));
            Assert.AreEqual((200d, 100d), (cropped.Width, cropped.Height));
        }
    }
}

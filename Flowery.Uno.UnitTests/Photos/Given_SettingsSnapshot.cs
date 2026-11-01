using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_SettingsSnapshot
    {
        private string _dir = null!;

        [TestInitialize]
        public void Create()
        {
            _dir = TestImages.TempDirectory("photos-snapshot-");
            PhotosSettings.Initialize(_dir);
            PhotosSettings.ConfirmDelete = true;
            PhotosSettings.WheelNavigates = true;
            PhotosSettings.SlideshowSeconds = 5;
            PhotosSettings.JpegQuality = 92;
        }

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        public void When_OptionsChangeAfterCapture_RestoreBringsThemBackAndPersistsThem()
        {
            var snapshot = SettingsSnapshot.Capture();

            PhotosSettings.ConfirmDelete = false;
            PhotosSettings.WheelNavigates = false;
            PhotosSettings.SlideshowSeconds = 12;
            PhotosSettings.JpegQuality = 50;
            PhotosSettings.SaveOptions();

            snapshot.Restore();

            Assert.IsTrue(PhotosSettings.ConfirmDelete);
            Assert.IsTrue(PhotosSettings.WheelNavigates);
            Assert.AreEqual(5, PhotosSettings.SlideshowSeconds);
            Assert.AreEqual(92, PhotosSettings.JpegQuality);

            // The restored values are on disk, not only in memory.
            PhotosSettings.ConfirmDelete = false;
            PhotosSettings.Initialize(_dir);
            Assert.IsTrue(PhotosSettings.ConfirmDelete);
            Assert.AreEqual(92, PhotosSettings.JpegQuality);
        }

        [TestMethod]
        public void When_Captured_FolderListMatchesTheLibrary()
        {
            var snapshot = SettingsSnapshot.Capture();

            CollectionAssert.AreEqual(PhotoLibrary.Instance.Folders.ToArray(), snapshot.Folders.ToArray());
        }
    }
}

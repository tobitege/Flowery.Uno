using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_FileOperations
    {
        [TestMethod]
        public void When_PickedPathHasNoExtension_AddsIt()
        {
            Assert.AreEqual("/home/me/palette.gpl", FileOperations.EnsureExtension("/home/me/palette", ".gpl"));
            Assert.AreEqual("/home/me/palette.gpl", FileOperations.EnsureExtension("/home/me/palette.gpl", ".gpl"));
            Assert.AreEqual("/home/me.v2/palette.gpl", FileOperations.EnsureExtension("/home/me.v2/palette", ".gpl"));
        }

        [TestMethod]
        public void When_FileExists_GetUniquePathCountsUp()
        {
            var dir = Directory.CreateTempSubdirectory("photos-unique-").FullName;
            try
            {
                var path = Path.Combine(dir, "a.jpg");
                Assert.AreEqual(path, FileOperations.GetUniquePath(path));

                File.WriteAllText(path, "x");
                Assert.AreEqual(Path.Combine(dir, "a (1).jpg"), FileOperations.GetUniquePath(path));

                File.WriteAllText(Path.Combine(dir, "a (1).jpg"), "x");
                Assert.AreEqual(Path.Combine(dir, "a (2).jpg"), FileOperations.GetUniquePath(path));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [TestMethod]
        public async Task When_SavingSucceeds_OnlyThePhotoIsLeft()
        {
            var dir = Directory.CreateTempSubdirectory("photos-save-").FullName;
            try
            {
                var path = Path.Combine(dir, "a.jpg");
                File.WriteAllText(path, "old");

                await FileOperations.WriteFileSafelyAsync(path, [1, 2, 3]);

                CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(path));
                Assert.AreEqual(1, Directory.GetFiles(dir).Length);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [TestMethod]
        public async Task When_SavingFails_NoTemporaryFileIsLeft()
        {
            var dir = Directory.CreateTempSubdirectory("photos-save-").FullName;
            try
            {
                // A folder where the photo should go: the write succeeds, the move into place fails.
                var path = Path.Combine(dir, "a.jpg");
                Directory.CreateDirectory(path);

                // Windows reports UnauthorizedAccessException, Linux and macOS an IOException.
                Exception? error = null;
                try
                {
                    await FileOperations.WriteFileSafelyAsync(path, [1, 2, 3]);
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                Assert.IsNotNull(error, "the failed save is reported");
                Assert.AreEqual(0, Directory.GetFiles(dir).Length, "the .saving file is gone");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}

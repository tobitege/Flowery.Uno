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
    }
}

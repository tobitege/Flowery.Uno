using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_PathRules
    {
        private static readonly char Sep = Path.DirectorySeparatorChar;

        [TestMethod]
        public void When_PathEndsWithSeparators_TrimEndRemovesThem()
        {
            Assert.AreEqual($"{Sep}pics{Sep}2024", PathRules.TrimEnd($"{Sep}pics{Sep}2024{Sep}"));
        }

        [TestMethod]
        public void When_PathIsTheRoot_TrimEndKeepsIt()
        {
            var root = Path.GetPathRoot(Path.GetTempPath())!;

            Assert.IsFalse(string.IsNullOrEmpty(PathRules.TrimEnd(root)));
        }

        [TestMethod]
        public void When_PathIsInsideFolder_IsUnderIsTrue()
        {
            var folder = Path.Combine(Path.GetTempPath(), "pics");

            Assert.IsTrue(PathRules.IsUnder(Path.Combine(folder, "a.jpg"), folder));
            Assert.IsTrue(PathRules.IsUnder(Path.Combine(folder, "sub", "a.jpg"), folder + Sep));
        }

        [TestMethod]
        public void When_PathOnlySharesThePrefix_IsUnderIsFalse()
        {
            var folder = Path.Combine(Path.GetTempPath(), "pics");

            Assert.IsFalse(PathRules.IsUnder(folder + "2" + Sep + "a.jpg", folder));
            Assert.IsFalse(PathRules.IsUnder(folder, folder));
        }

        [TestMethod]
        public void When_FolderHasTrailingSeparator_FolderNameIsTheLastSegment()
        {
            Assert.AreEqual("2024", PathRules.FolderName($"{Sep}pics{Sep}2024{Sep}"));
        }

        [TestMethod]
        public void When_CaseDiffers_ComparerFollowsTheOs()
        {
            var sameFile = PathRules.Comparer.Equals("/Pics/A.JPG", "/pics/a.jpg");

            Assert.AreEqual(!OperatingSystem.IsLinux(), sameFile);
        }
    }
}

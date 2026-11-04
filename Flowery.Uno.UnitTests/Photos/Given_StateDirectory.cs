using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_StateDirectory
    {
        private const string Variable = "FLOWERY_STATE_ROOT";
        private string? _saved;

        [TestInitialize]
        public void Save() => _saved = Environment.GetEnvironmentVariable(Variable);

        [TestCleanup]
        public void Restore() => Environment.SetEnvironmentVariable(Variable, _saved);

        [TestMethod]
        public void When_RootIsSet_ItWinsAndIsNormalized()
        {
            var root = Path.Combine(Path.GetTempPath(), "photos-state", "..", "photos-state-2");
            Environment.SetEnvironmentVariable(Variable, root);

            Assert.AreEqual(Path.GetFullPath(root), StateDirectory.Resolve());
        }

        [TestMethod]
        public void When_RootIsRelative_Throws()
        {
            Environment.SetEnvironmentVariable(Variable, Path.Combine("relative", "state"));

            Assert.ThrowsExactly<ArgumentException>(() => StateDirectory.Resolve());
        }

        [TestMethod]
        public void When_RootIsNotSet_UsesTheLocalAppDataFolder()
        {
            Environment.SetEnvironmentVariable(Variable, null);

            var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flowery.Uno.Photos");
            Assert.AreEqual(expected, StateDirectory.Resolve());
        }
    }
}

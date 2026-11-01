using Flowery.Uno.Photos.Services.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_MacPlatformServices
    {
        private sealed class FakeMacNative : IMacNative
        {
            public List<string> Trashed { get; } = [];

            public bool TrashItem(string path)
            {
                Trashed.Add(path);
                return true;
            }

            public bool SetDesktopImage(string path) => path.EndsWith(".jpg", StringComparison.Ordinal);

            public IReadOnlyList<OpenWithApp> GetAppsToOpen(string path) => [new("Preview", "/System/Applications/Preview.app")];

            public IReadOnlyList<DisplayRect> GetDisplays() => [new(0, 0, 1512, 982, true)];
        }

        [TestMethod]
        public async Task When_CopyingFiles_ReturnsFalse()
        {
            var services = new MacPlatformServices(new FakeCommandRunner(), new FakeMacNative());

            Assert.IsFalse(await services.CopyFilesToClipboardAsync(["/a.jpg"]));
        }

        [TestMethod]
        public void When_Revealing_UsesOpenWithTheRevealFlag()
        {
            var runner = new FakeCommandRunner();

            new MacPlatformServices(runner, new FakeMacNative()).RevealInFileManager("/Users/me/a b.jpg");

            Assert.AreEqual("/usr/bin/open", runner.Started.Single().File);
            CollectionAssert.AreEqual(new[] { "-R", "/Users/me/a b.jpg" }, runner.Started.Single().Args);
        }

        [TestMethod]
        public async Task When_OpeningWith_StartsOpenDashAWithTheAppBundle()
        {
            var runner = new FakeCommandRunner();
            var services = new MacPlatformServices(runner, new FakeMacNative());

            var apps = await services.GetOpenWithAppsAsync("/a.jpg");
            Assert.IsTrue(await services.OpenWithAsync("/a.jpg", apps[0]));
            CollectionAssert.AreEqual(new[] { "-a", "/System/Applications/Preview.app", "/a.jpg" }, runner.Started.Single().Args);

            Assert.IsFalse(await services.OpenWithAsync("/a.jpg"), "macOS has no system chooser");
        }

        [TestMethod]
        public void When_Trashing_NeedsAnExistingFile()
        {
            var native = new FakeMacNative();
            var services = new MacPlatformServices(new FakeCommandRunner(), native);

            Assert.IsFalse(services.MoveToTrash(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
            Assert.AreEqual(0, native.Trashed.Count);
        }

        [TestMethod]
        [DataRow("-1\n", 0x8C, 0x8C, 0x8C)]
        [DataRow("0", 0xFF, 0x52, 0x57)]
        [DataRow("3", 0x62, 0xBA, 0x46)]
        [DataRow("5", 0xA5, 0x50, 0xA7)]
        [DataRow("4", 0x00, 0x7A, 0xFF)]
        public void When_ParsingAccentSetting_ReturnsTheMatchingColor(string output, int r, int g, int b)
        {
            Assert.AreEqual(new SystemAccent((byte)r, (byte)g, (byte)b), MacPlatformServices.ParseAccent(output));
        }

        [TestMethod]
        public void When_AccentSettingIsAbsent_IsTheDefaultBlue()
        {
            var runner = new FakeCommandRunner().Returns("/usr/bin/defaults read -g AppleAccentColor", 1, "The domain/default pair does not exist");

            Assert.AreEqual(new SystemAccent(0x00, 0x7A, 0xFF), new MacPlatformServices(runner, new FakeMacNative()).GetSystemAccent());
        }
    }
}

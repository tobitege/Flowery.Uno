#pragma warning disable CA1416 // The pieces under test are pure and run on every OS.

using System.Text;
using Flowery.Uno.Photos.Services.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_WindowsPlatformServices
    {
        [TestMethod]
        public void When_Revealing_QuotesThePathAfterSelect()
        {
            Assert.AreEqual("explorer.exe /select,\"C:\\My Pictures\\a.jpg\"", WindowsPlatformServices.Commands.Reveal("C:\\My Pictures\\a.jpg").ToString());
        }

        [TestMethod]
        public void When_OpeningWith_UsesTheShell32DialogWithTheUnquotedPath()
        {
            Assert.AreEqual("rundll32.exe shell32.dll,OpenAs_RunDLL C:\\My Pictures\\a.jpg", WindowsPlatformServices.Commands.OpenWith("C:\\My Pictures\\a.jpg").ToString());
        }

        [TestMethod]
        public async Task When_OpeningWithAMissingFile_DoesNotStartAnything()
        {
            var runner = new FakeCommandRunner();

            Assert.IsFalse(await new WindowsPlatformServices(runner).OpenWithAsync("C:\\nope\\" + Guid.NewGuid().ToString("N") + ".jpg"));
            Assert.AreEqual(0, runner.Started.Count);
        }

        [TestMethod]
        public void When_ReadingDwmAccent_ChannelsAreReversedFromAbgr()
        {
            Assert.AreEqual(new SystemAccent(0x11, 0x22, 0x33), WindowsPlatformServices.ParseAccent(0xFF332211));
        }

        [TestMethod]
        public void When_BuildingDropFiles_HeaderPointsAtADoubleNullTerminatedWideList()
        {
            var data = WindowsPlatformServices.DropFiles.Build(["C:\\a.jpg", "C:\\b c.png"]);

            Assert.AreEqual(20, BitConverter.ToInt32(data, 0), "pFiles");
            Assert.AreEqual(1, BitConverter.ToInt32(data, 16), "fWide");
            var list = Encoding.Unicode.GetString(data, 20, data.Length - 20);
            Assert.AreEqual("C:\\a.jpg\0C:\\b c.png\0\0", list);
        }
    }
}

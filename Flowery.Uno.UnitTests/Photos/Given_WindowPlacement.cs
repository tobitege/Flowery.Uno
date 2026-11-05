using Flowery.Uno.Photos.Services;
using Flowery.Uno.Photos.Services.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_WindowPlacement
    {
        // A 1920 x 1080 monitor with a 48 pixel taskbar at the bottom.
        private static readonly DisplayRect WorkArea = new(0, 0, 1920, 1032, IsPrimary: true);

        [TestMethod]
        public void When_TheSavedWindowFits_ItReopensWithItsSize()
        {
            Assert.AreEqual(new RestoredSize(1200, 800), WindowPlacement.Restore(new WindowBounds(300, 100, 1200, 800), WorkArea));
        }

        [TestMethod]
        public void When_TheSavedWindowIsLargerThanTheWorkArea_ItShrinksToIt()
        {
            Assert.AreEqual(new RestoredSize(1920, 1032), WindowPlacement.Restore(new WindowBounds(-10, -10, 2500, 1500), WorkArea));
        }

        [TestMethod]
        public void When_TheSavedWindowIsTiny_ItGetsTheMinimumSize()
        {
            Assert.AreEqual(new RestoredSize(WindowPlacement.MinimumWidth, WindowPlacement.MinimumHeight),
                WindowPlacement.Restore(new WindowBounds(100, 100, 300, 200), WorkArea));
        }

        [TestMethod]
        public void When_TheSystemHasNoWorkArea_TheSavedSizeIsKept()
        {
            Assert.AreEqual(new RestoredSize(2500, 1500), WindowPlacement.Restore(new WindowBounds(300, 100, 2500, 1500), null));
        }

        [TestMethod]
        public void When_NothingWasSaved_TheDefaultSizeIsUsed()
        {
            Assert.IsNull(WindowPlacement.Restore(null, WorkArea));
        }
    }
}

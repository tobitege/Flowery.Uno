using Flowery.Uno.Photos.Services.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_ScreenOverhang
    {
        private static readonly DisplayRect Monitor = new(0, 0, 3840, 2160, true);

        [TestMethod]
        public void When_FullScreenWindowKeepsItsResizeBorder_EachSideReachesPastTheMonitor()
        {
            // Measured on Windows 11 at 125 %: the full screen window is (-9,-9)-(3849,2169) on a 3840 x 2160 monitor.
            var window = new DisplayRect(-9, -9, 3858, 2178, false);

            Assert.AreEqual(new ScreenOverhang(9, 9, 9, 9), ScreenOverhang.Between(window, Monitor));
        }

        [TestMethod]
        public void When_WindowFitsTheMonitor_ThereIsNoOverhang()
        {
            var overhang = ScreenOverhang.Between(new DisplayRect(0, 0, 3840, 2160, false), Monitor);

            Assert.IsTrue(overhang.IsEmpty);
        }

        [TestMethod]
        public void When_WindowIsInsideTheMonitor_NoSideIsNegative()
        {
            Assert.IsTrue(ScreenOverhang.Between(new DisplayRect(100, 200, 1280, 800, false), Monitor).IsEmpty);
        }

        [TestMethod]
        public void When_MonitorIsLeftOfThePrimary_OverhangUsesItsOwnOrigin()
        {
            var left = new DisplayRect(-1440, 0, 1440, 2560, false);
            var window = new DisplayRect(-1448, -8, 1456, 2576, false);

            Assert.AreEqual(new ScreenOverhang(8, 8, 8, 8), ScreenOverhang.Between(window, left));
        }
    }
}

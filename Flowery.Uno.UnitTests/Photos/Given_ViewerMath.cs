using Flowery.Uno.Photos.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_ViewerMath
    {
        [TestMethod]
        public void When_ZoomingInFromFit_StepsLandOnRoundPercentages()
        {
            // 1 % grid below 10 %, 10 % grid below 100 %, then 25 % grid.
            Assert.AreEqual(6, ViewerMath.StepZoomPercent(5, 1));
            Assert.AreEqual(10, ViewerMath.StepZoomPercent(9.4, 1));
            Assert.AreEqual(40, ViewerMath.StepZoomPercent(33.3, 1));
            Assert.AreEqual(100, ViewerMath.StepZoomPercent(95, 1));
            Assert.AreEqual(125, ViewerMath.StepZoomPercent(100, 1));
            Assert.AreEqual(150, ViewerMath.StepZoomPercent(125, 1));
        }

        [TestMethod]
        public void When_ZoomingOut_StepsLandOnRoundPercentages()
        {
            Assert.AreEqual(90, ViewerMath.StepZoomPercent(100, -1), "100 % goes below 100 %");
            Assert.AreEqual(30, ViewerMath.StepZoomPercent(33.3, -1));
            Assert.AreEqual(1, ViewerMath.StepZoomPercent(1, -1), "never below 1 %");
            Assert.AreEqual(100, ViewerMath.StepZoomPercent(125, -1));
        }

        [TestMethod]
        public void When_StepsAreRepeated_ZoomAlwaysMoves()
        {
            var percent = 5.0;
            for (var i = 0; i < 40; i++)
            {
                var next = ViewerMath.StepZoomPercent(percent, 1);
                Assert.IsTrue(next > percent, $"{percent} -> {next}");
                percent = next;
            }

            for (var i = 0; i < 60 && percent > 1; i++)
            {
                var next = ViewerMath.StepZoomPercent(percent, -1);
                Assert.IsTrue(next < percent, $"{percent} -> {next}");
                percent = next;
            }
        }

        [TestMethod]
        public void When_RulerStepIsChosen_ItIsOneTwoOrFiveTimesAPowerOfTen()
        {
            Assert.AreEqual(1, ViewerMath.NiceStep(0.7));
            Assert.AreEqual(2, ViewerMath.NiceStep(1.5));
            Assert.AreEqual(5, ViewerMath.NiceStep(3));
            Assert.AreEqual(10, ViewerMath.NiceStep(7));
            Assert.AreEqual(200, ViewerMath.NiceStep(130));
            Assert.AreEqual(100, ViewerMath.NiceStep(0), "invalid input falls back to 100");
            Assert.AreEqual(100, ViewerMath.NiceStep(double.NaN));
        }
    }
}

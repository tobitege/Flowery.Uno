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

        [TestMethod]
        public void When_DividerIsPressedInTheMarkupTab_TheDividerMovesInsteadOfDrawing()
        {
            var press = new CanvasPressInput(Left: true, Right: false, Middle: false, Eyedropper: false, OnNavigator: false,
                CropMode: false, OnSplitHandle: true, MarkupMode: true, ImageLargerThanView: false);

            Assert.AreEqual(CanvasPress.Split, ViewerMath.ChoosePress(press));
            Assert.AreEqual(CanvasPress.Markup, ViewerMath.ChoosePress(press with { OnSplitHandle = false }), "away from the divider it draws");
            Assert.AreEqual(CanvasPress.Markup, ViewerMath.ChoosePress(press with { Left = false, Right = true }),
                "the right button draws with the secondary color, also on the divider");
        }

        [TestMethod]
        public void When_PressingTheCanvas_ThePickerNavigatorAndCropComeFirst()
        {
            var all = new CanvasPressInput(Left: true, Right: false, Middle: false, Eyedropper: true, OnNavigator: true,
                CropMode: true, OnSplitHandle: true, MarkupMode: true, ImageLargerThanView: true);

            Assert.AreEqual(CanvasPress.PickColor, ViewerMath.ChoosePress(all));
            Assert.AreEqual(CanvasPress.Navigator, ViewerMath.ChoosePress(all with { Eyedropper = false }));
            Assert.AreEqual(CanvasPress.Crop, ViewerMath.ChoosePress(all with { Eyedropper = false, OnNavigator = false }));
            Assert.AreEqual(CanvasPress.Pan, ViewerMath.ChoosePress(all with
            {
                Left = false, Middle = true, Eyedropper = false, OnNavigator = false, CropMode = false, MarkupMode = false
            }));
            Assert.AreEqual(CanvasPress.None, ViewerMath.ChoosePress(default));
        }

        [TestMethod]
        public void When_PointerIsNearTheDivider_ItIsOnTheHandle()
        {
            // Image from x = 100, 400 wide, split in the middle: the divider is at x = 300.
            Assert.IsTrue(ViewerMath.IsOnSplitHandle(100, 400, 0.5, 285));
            Assert.IsTrue(ViewerMath.IsOnSplitHandle(100, 400, 0.5, 319));
            Assert.IsFalse(ViewerMath.IsOnSplitHandle(100, 400, 0.5, 321));
        }
    }
}

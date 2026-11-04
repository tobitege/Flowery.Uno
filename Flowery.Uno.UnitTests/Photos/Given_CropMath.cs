using System.Collections.Immutable;
using System.Numerics;
using Flowery.Uno.Photos.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_CropMath
    {
        private static readonly Rect Screen = new(100, 100, 400, 200);
        private static readonly Rect Half = new(0.25, 0.25, 0.5, 0.5);
        private static readonly Size Landscape = new(2000, 1000);

        private static void AssertRect(Rect expected, Rect actual)
        {
            Assert.AreEqual(expected.X, actual.X, 1e-6, "x");
            Assert.AreEqual(expected.Y, actual.Y, 1e-6, "y");
            Assert.AreEqual(expected.Width, actual.Width, 1e-6, "width");
            Assert.AreEqual(expected.Height, actual.Height, 1e-6, "height");
        }

        [TestMethod]
        public void When_PointIsOnACorner_TheCornerHandleIsHit()
        {
            Assert.AreEqual(CropHandle.TopLeft, CropMath.HitTest(Screen, Half, new Point(200, 150)));
            Assert.AreEqual(CropHandle.BottomRight, CropMath.HitTest(Screen, Half, new Point(400, 250)));
        }

        [TestMethod]
        public void When_PointIsOnAnEdge_TheEdgeHandleIsHit()
        {
            Assert.AreEqual(CropHandle.Top, CropMath.HitTest(Screen, Half, new Point(300, 150)));
            Assert.AreEqual(CropHandle.Right, CropMath.HitTest(Screen, Half, new Point(400, 200)));
        }

        [TestMethod]
        public void When_PointIsInsideOrOutside_MoveOrNothingIsHit()
        {
            Assert.AreEqual(CropHandle.Move, CropMath.HitTest(Screen, Half, new Point(300, 200)));
            Assert.AreEqual(CropHandle.None, CropMath.HitTest(Screen, Half, new Point(110, 110)));
        }

        [TestMethod]
        public void When_Moved_TheSizeStaysAndTheCropStaysInside()
        {
            var moved = CropMath.Resize(Half, CropHandle.Move, 0.9, -0.9, Landscape, null);

            AssertRect(new Rect(0.5, 0, 0.5, 0.5), moved);
        }

        [TestMethod]
        public void When_CornerIsDragged_TheOppositeCornerStays()
        {
            var resized = CropMath.Resize(Half, CropHandle.TopLeft, -0.1, -0.1, Landscape, null);

            AssertRect(new Rect(0.15, 0.15, 0.6, 0.6), resized);
        }

        [TestMethod]
        public void When_EdgeIsDraggedPastTheOtherEdge_TheMinimumSizeStays()
        {
            var resized = CropMath.Resize(Half, CropHandle.Right, -2, 0, Landscape, null);

            Assert.AreEqual(0.04, resized.Width, 1e-6);
        }

        [TestMethod]
        public void When_AspectIsLocked_TheResultHasThatPixelAspect()
        {
            var resized = CropMath.Resize(Half, CropHandle.BottomRight, 0.1, 0, Landscape, 1);

            var aspect = resized.Width * Landscape.Width / (resized.Height * Landscape.Height);
            Assert.AreEqual(1, aspect, 1e-6);
            Assert.AreEqual(Half.X, resized.X, 1e-6);
            Assert.AreEqual(Half.Y, resized.Y, 1e-6);
        }

        [TestMethod]
        public void When_AspectCropIsCreated_ItIsCenteredInsideTheCurrentCrop()
        {
            var crop = CropMath.CreateAspectCrop(EditState.FullCrop, 1, Landscape);

            AssertRect(new Rect(0.25, 0, 0.5, 1), crop);
        }

        [TestMethod]
        public void When_Mirrored_TheCropMovesToTheOtherSide()
        {
            var crop = new Rect(0.1, 0.2, 0.3, 0.4);

            AssertRect(new Rect(0.6, 0.2, 0.3, 0.4), CropMath.Mirror(crop, true));
            AssertRect(new Rect(0.1, 0.4, 0.3, 0.4), CropMath.Mirror(crop, false));
        }

        [TestMethod]
        public void When_RotatedClockwise_CropAndStrokesFollowTheContent()
        {
            var stroke = new MarkupStroke(Color.FromArgb(255, 1, 2, 3), 0.01f, false, [new Vector2(0.1f, 0.2f)]);
            var state = EditState.Original with { Crop = new Rect(0.1, 0.2, 0.3, 0.4), Strokes = ImmutableList.Create(stroke) };

            var rotated = CropMath.Rotate(state, 1);

            Assert.AreEqual(1, rotated.Rotation);
            AssertRect(new Rect(0.4, 0.1, 0.4, 0.3), rotated.Crop);
            Assert.AreEqual(0.8f, rotated.Strokes[0].Points[0].X, 1e-5f);
            Assert.AreEqual(0.1f, rotated.Strokes[0].Points[0].Y, 1e-5f);
        }

        [TestMethod]
        public void When_RotatedBothWays_TheStateComesBack()
        {
            var state = EditState.Original with { Crop = new Rect(0.1, 0.2, 0.3, 0.4) };

            var back = CropMath.Rotate(CropMath.Rotate(state, 1), -1);

            Assert.AreEqual(0, back.Rotation);
            AssertRect(state.Crop, back.Crop);
        }

        [TestMethod]
        public void When_RotatedAntiClockwiseFromZero_RotationWrapsToThree()
        {
            Assert.AreEqual(3, CropMath.Rotate(EditState.Original, -1).Rotation);
        }
    }
}

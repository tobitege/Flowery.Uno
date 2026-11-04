using Flowery.Uno.Photos.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_AutoEnhance
    {
        /// <summary>A horizontal grey ramp between two levels.</summary>
        private static SKImage Ramp(byte from, byte to)
        {
            using var surface = SKSurface.Create(new SKImageInfo(256, 64, SKColorType.Bgra8888, SKAlphaType.Premul));
            using var paint = new SKPaint();
            for (var x = 0; x < 256; x++)
            {
                var v = (byte)(from + (to - from) * x / 255);
                paint.Color = new SKColor(v, v, v);
                surface.Canvas.DrawRect(x, 0, 1, 64, paint);
            }
            return surface.Snapshot();
        }

        [TestMethod]
        public void When_PhotoIsDark_ExposureGoesUp()
        {
            using var image = Ramp(5, 60);

            var state = AutoEnhance.Apply(image, EditState.Original);

            Assert.IsGreaterThan(0, state.Exposure);
            Assert.IsGreaterThan(0, state.Shadows);
        }

        [TestMethod]
        public void When_PhotoIsBright_ExposureGoesDownAndHighlightsAreLowered()
        {
            using var image = Ramp(200, 255);

            var state = AutoEnhance.Apply(image, EditState.Original);

            Assert.IsLessThan(0, state.Exposure);
            Assert.IsLessThan(0, state.Highlights);
        }

        [TestMethod]
        public void When_PhotoIsFlat_ContrastGoesUp()
        {
            using var image = Ramp(110, 140);

            var state = AutoEnhance.Apply(image, EditState.Original);

            Assert.IsGreaterThan(0, state.Contrast);
        }

        [TestMethod]
        public void When_PhotoIsBlue_WarmthGoesUp()
        {
            using var surface = SKSurface.Create(new SKImageInfo(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul));
            surface.Canvas.Clear(new SKColor(80, 100, 170));
            using var image = surface.Snapshot();

            var state = AutoEnhance.Apply(image, EditState.Original);

            Assert.IsGreaterThan(0, state.Warmth);
        }

        [TestMethod]
        public void When_StateHasGeometry_ItIsKept()
        {
            using var image = Ramp(5, 60);
            var start = EditState.Original with { Rotation = 1, FlipH = true };

            var state = AutoEnhance.Apply(image, start);

            Assert.AreEqual((1, true), (state.Rotation, state.FlipH));
        }
    }
}

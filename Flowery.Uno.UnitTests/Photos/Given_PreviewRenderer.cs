using Flowery.Uno.Photos.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_PreviewRenderer
    {
        private static SKImage Flat()
        {
            using var surface = SKSurface.Create(new SKImageInfo(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul));
            surface.Canvas.Clear(new SKColor(100, 100, 100));
            return surface.Snapshot();
        }

        [TestMethod]
        public async Task When_ManyRequestsArrive_TheLastOneIsRendered()
        {
            using var source = Flat();
            var renderer = new PreviewRenderer();
            var done = new TaskCompletionSource<PreviewRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
            var last = new PreviewRequest(source, EditState.Original with { Exposure = 50 }, true, 1);
            renderer.Rendered += (request, image) =>
            {
                image.Dispose();
                if (request == last)
                    done.TrySetResult(request);
            };

            for (var i = 1; i < 50; i++)
                renderer.Request(new PreviewRequest(source, EditState.Original with { Exposure = i - 50 }, true, 1), SKImage.FromPixelCopy(source.PeekPixels()));
            renderer.Request(last, SKImage.FromPixelCopy(source.PeekPixels()));

            var finished = await Task.WhenAny(done.Task, Task.Delay(10000));
            Assert.AreSame(done.Task, finished);
        }

        [TestMethod]
        public void When_TwoRequestsDescribeTheSamePicture_TheyAreEqual()
        {
            using var source = Flat();
            var state = EditState.Original with { Exposure = 10 };

            Assert.AreEqual(new PreviewRequest(source, state, true, 0.5), new PreviewRequest(source, state with { }, true, 0.5));
            Assert.AreNotEqual(new PreviewRequest(source, state, true, 0.5), new PreviewRequest(source, state, true, 0.55));
        }

        [TestMethod]
        [DataRow(0.61, 0.65)]
        [DataRow(0.6, 0.6)]
        [DataRow(3.0, 1.0)]
        [DataRow(0.001, 0.05)]
        public void When_DisplayScaleIsGiven_PreviewScaleRoundsUp(double display, double expected)
        {
            Assert.AreEqual(expected, EditRenderer.GetPreviewScale(display), 1e-9);
        }
    }
}

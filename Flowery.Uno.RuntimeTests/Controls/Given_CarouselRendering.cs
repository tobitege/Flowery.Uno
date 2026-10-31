using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Flowery.Controls;
using Flowery.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_CarouselRendering
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        // Only a safeguard: the macOS runner, which renders Skia in software, can hold back a frame for more than five
        // seconds.
        private static readonly TimeSpan FrameTimeout = TimeSpan.FromSeconds(30);

        [TestMethod]
        public async Task When_Checkerboard_Transitions_MaskChangesDifferentRegions()
        {
            var outgoing = new Border { Background = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)) };
            var incoming = new Border { Background = new SolidColorBrush(Color.FromArgb(255, 255, 0, 255)) };
            var host = new Grid
            {
                Width = 240,
                Height = 160,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Children = { incoming, outgoing }
            };
            var completed = new TaskCompletionSource();
            var started = false;
            var largestRegionDifference = 0;

            RuntimeTestHelpers.AttachToHost(host);
            try
            {
                await RuntimeTestHelpers.EnsureLoadedAsync(host).WaitAsync(Timeout);
                await NextFrameAsync();
                var bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(host).AsTask().WaitAsync(Timeout);
                var initialPixels = (await bitmap.GetPixelsAsync().AsTask().WaitAsync(Timeout)).ToArray();
                Assert.IsTrue(initialPixels.Length > 0, "The capture must contain rendered pixels.");
                Assert.AreEqual((byte)255, initialPixels[((bitmap.PixelHeight / 2 * bitmap.PixelWidth) + bitmap.PixelWidth / 2) * 4 + 1],
                    "The outgoing green slide must be rendered before starting the transition.");

                FlowerySlideTransitionHelpers.ApplyTransition(host, outgoing, incoming, FlowerySlideTransition.Checkerboard,
                    new FlowerySlideTransitionParams
                    {
                        Duration = TimeSpan.FromSeconds(1.2),
                        CheckerboardSize = 4,
                        SliceStaggerMs = 150
                    }, () => completed.TrySetResult());
                started = true;

                var elapsed = Stopwatch.StartNew();
                while (!completed.Task.IsCompleted && elapsed.Elapsed < Timeout && largestRegionDifference < 80)
                {
                    await NextFrameAsync();
                    if (completed.Task.IsCompleted)
                        break;

                    await bitmap.RenderAsync(host).AsTask().WaitAsync(Timeout);
                    var pixels = (await bitmap.GetPixelsAsync().AsTask().WaitAsync(Timeout)).ToArray();
                    largestRegionDifference = Math.Max(largestRegionDifference,
                        GetCheckerboardRegionDifference(pixels, bitmap.PixelWidth, bitmap.PixelHeight));
                }
            }
            finally
            {
                try
                {
                    if (started)
                        await completed.Task.WaitAsync(Timeout);
                }
                finally
                {
                    RuntimeTestHelpers.GetHostPanel().Children.Remove(host);
                }
            }

            Assert.IsTrue(largestRegionDifference >= 80,
                $"The mask must reveal different regions, not a uniform crossfade. Largest region difference: {largestRegionDifference}.");
        }

        [TestMethod]
        public async Task When_Leaving_AnimatedSlide_OutgoingImagePoseStaysFrozen()
        {
            var source = new WriteableBitmap(2, 2);
            using (var stream = source.PixelBuffer.AsStream())
            {
                stream.Write(new byte[] { 0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255 });
            }
            source.Invalidate();
            var image = new Image { Source = source, Stretch = Stretch.UniformToFill };
            var carousel = new DaisyCarousel
            {
                Width = 240,
                Height = 160,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                ShowNavigation = false,
                Mode = FlowerySlideshowMode.Manual,
                SlideInterval = 4,
                SlideEffect = FlowerySlideEffect.PanLeft,
                PanDistance = 80,
                ZoomIntensity = 0.4,
                SlideTransition = FlowerySlideTransition.Fade,
                // Long enough that the three checked frames stay inside the transition on a slow CI runner, where a
                // frame of a software-rendered Skia window can take longer than a second.
                TransitionDuration = 4,
                Content = new StackPanel
                {
                    Children = { new Border { Child = image }, new Border { Background = new SolidColorBrush(Microsoft.UI.Colors.Magenta) } }
                }
            };
            var completed = new TaskCompletionSource();
            EventHandler<FlowerySlideChangedEventArgs> onChanged = (_, _) => completed.TrySetResult();
            var started = false;
            RuntimeTestHelpers.AttachToHost(carousel);
            try
            {
                await RuntimeTestHelpers.EnsureLoadedAsync(carousel).WaitAsync(Timeout);
                var elapsed = Stopwatch.StartNew();
                while (image.RenderTransform is not CompositeTransform { TranslateX: < -2, ScaleX: > 1.005 } && elapsed.Elapsed < Timeout)
                    await NextFrameAsync();

                Assert.IsInstanceOfType<CompositeTransform>(image.RenderTransform);
                var transform = (CompositeTransform)image.RenderTransform;
                Assert.IsTrue(transform.TranslateX < -2 && transform.ScaleX > 1.005,
                    "The outgoing image must have actually moved and scaled before the freeze is tested.");
                var pose = CapturePose(image);
                carousel.SlideChanged += onChanged;
                var transition = Stopwatch.StartNew();
                carousel.Next();
                started = true;

                Assert.AreEqual(pose, CapturePose(image), "Starting the transition must preserve the outgoing image pose.");
                for (var frame = 0; frame < 3; frame++)
                {
                    await NextFrameAsync();
                    if (completed.Task.IsCompleted)
                    {
                        // A transition that ends early is a fault; a runner too slow to render inside it is not.
                        Assert.IsGreaterThanOrEqualTo(carousel.TransitionDuration, transition.Elapsed.TotalSeconds,
                            "The transition ended before its duration.");
                        Assert.Inconclusive($"The runner rendered {frame + 1} frames in {transition.Elapsed.TotalSeconds:F1} s, " +
                            $"so the {carousel.TransitionDuration} s transition ended before the pose could be checked.");
                    }

                    Assert.AreEqual(pose, CapturePose(image), "The outgoing image pose must remain frozen during the transition.");
                }
            }
            finally
            {
                try
                {
                    if (started)
                        await completed.Task.WaitAsync(Timeout + TimeSpan.FromSeconds(carousel.TransitionDuration));
                }
                finally
                {
                    carousel.SlideChanged -= onChanged;
                    carousel.SlideEffect = FlowerySlideEffect.None;
                    FlowerySlideEffects.StopEffect(image);
                    RuntimeTestHelpers.GetHostPanel().Children.Remove(carousel);
                }
            }
        }

        private static (double CenterX, double CenterY, double ScaleX, double ScaleY, double SkewX, double SkewY,
            double Rotation, double TranslateX, double TranslateY) CapturePose(UIElement element)
        {
            Assert.IsInstanceOfType<CompositeTransform>(element.RenderTransform);
            var transform = (CompositeTransform)element.RenderTransform;
            return (transform.CenterX, transform.CenterY, transform.ScaleX, transform.ScaleY, transform.SkewX, transform.SkewY,
                transform.Rotation, transform.TranslateX, transform.TranslateY);
        }

        private static int GetCheckerboardRegionDifference(byte[] pixels, int width, int height)
        {
            var minimum = 255;
            var maximum = 0;
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    var x = (column * 2 + 1) * width / 8;
                    var y = (row * 2 + 1) * height / 8;
                    var blue = pixels[(y * width + x) * 4];
                    minimum = Math.Min(minimum, blue);
                    maximum = Math.Max(maximum, blue);
                }
            }
            return maximum - minimum;
        }

        private static async Task NextFrameAsync()
        {
            var rendered = new TaskCompletionSource();
            EventHandler<object> onRendering = (_, _) => rendered.TrySetResult();
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += onRendering;
            try
            {
                await rendered.Task.WaitAsync(FrameTimeout);
            }
            finally
            {
                Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= onRendering;
            }
        }
    }
}

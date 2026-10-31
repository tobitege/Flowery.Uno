using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Enums;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_AnimatedControls
    {
        // Longer than the longest staggered start delay of the animated controls.
        private static readonly TimeSpan StartDelaySettle = TimeSpan.FromSeconds(2.5);

        [TestMethod]
        public async Task When_Removed_Right_After_Loading_No_Animation_Keeps_Running()
        {
            var panel = new StackPanel();
            foreach (var variant in Enum.GetValues<DaisyLoadingVariant>())
                panel.Children.Add(new DaisyLoading { Variant = variant });
            foreach (var variant in Enum.GetValues<DaisyStatusIndicatorVariant>())
                panel.Children.Add(new DaisyStatusIndicator { Variant = variant });
            panel.Children.Add(new DaisySkeleton());
            panel.Children.Add(new DaisyProgress { Value = 50, Shimmer = true });
            panel.Children.Add(new DaisyRadialProgress { Value = 50, Shimmer = true });
            panel.Children.Add(new DaisyDivider { DividerStyle = DaisyDividerStyle.Glow });

            // Only Uno's compositor lets the test see what still runs; native WinUI still loads and unloads every variant.
            var running = GetRunningAnimations(panel);
            var rendering = typeof(CompositionTarget).GetField("_rendering", BindingFlags.Static | BindingFlags.NonPublic);
            if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
            {
                Assert.IsNotNull(running, "Uno's Compositor no longer has _runningAnimations: update the test.");
                Assert.IsNotNull(rendering, "Uno's CompositionTarget no longer has _rendering: update the test.");
            }

            // Earlier tests may leave animations that end while this one runs, so only new ones count.
            var animationsBefore = running?.Keys.Cast<object>().ToHashSet() ?? [];
            var framesBefore = GetRenderingSubscribers(rendering);

            RuntimeTestHelpers.AttachToHost(panel);
            await RuntimeTestHelpers.EnsureLoadedAsync(panel);
            if (running != null && rendering != null)
            {
                Assert.IsTrue(running.Keys.Cast<object>().Any(key => !animationsBefore.Contains(key)), "The loaded controls animate.");
                Assert.IsTrue(GetRenderingSubscribers(rendering).Except(framesBefore).Any(),
                    "Storyboards and the manual rotation on Skia animate per frame.");
            }

            // An animation that outlives its control keeps the compositor animating, and with it the control's
            // whole visual tree, for the rest of the app's life.
            var unloaded = new TaskCompletionSource();
            panel.Unloaded += (_, _) => unloaded.TrySetResult();
            RuntimeTestHelpers.AttachToHost(new Border());
            await unloaded.Task;
            await Task.Delay(StartDelaySettle);

            if (running == null || rendering == null)
            {
                Assert.Inconclusive("Loaded and unloaded every variant; native WinUI does not expose its running animations.");
            }

            var leaked = running.Keys.Cast<object>().Where(key => !animationsBefore.Contains(key)).Select(Describe).ToList();
            Assert.IsEmpty(leaked, $"Animations still running: {string.Join(", ", leaked)}");

            // Any handler on CompositionTarget.Rendering makes the app render every frame, even when nothing moves.
            var frameHandlers = GetRenderingSubscribers(rendering).Except(framesBefore)
                .Select(handler => $"{handler.Method.DeclaringType?.Name}.{handler.Method.Name}").ToList();
            Assert.IsEmpty(frameHandlers, $"Still called every frame: {string.Join(", ", frameHandlers)}");
        }

        // Uno keeps the Rendering handlers in a private static field; the test reads it to see what still runs per frame.
        private static Delegate[] GetRenderingSubscribers(FieldInfo? rendering) =>
            (rendering?.GetValue(null) as Delegate)?.GetInvocationList() ?? [];

        // Uno's Compositor has no public list of its running animations, so the test reads the internal one.
        private static IDictionary? GetRunningAnimations(UIElement element)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
            return compositor.GetType().GetField("_runningAnimations", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(compositor) as IDictionary;
        }

        private static string Describe(object animation)
        {
            var duration = animation.GetType().GetProperty("Duration")?.GetValue(animation);
            var target = animation.GetType().GetProperty("AnimationTargetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?.GetValue(animation) as Microsoft.UI.Composition.Visual;
            return $"{animation.GetType().Name} {duration} on {target?.GetType().Name} {target?.Size}";
        }
    }
}

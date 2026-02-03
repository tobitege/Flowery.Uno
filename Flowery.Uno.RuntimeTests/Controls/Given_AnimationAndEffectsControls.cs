using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Colors = Microsoft.UI.Colors;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_AnimationAndEffectsControls
    {
        [TestMethod]
        public async Task When_Loading_DefaultsHold()
        {
            var loading = new DaisyLoading();

            RuntimeTestHelpers.AttachToHost(loading);
            await RuntimeTestHelpers.EnsureLoadedAsync(loading);

            Assert.AreEqual(DaisyLoadingVariant.Spinner, loading.Variant);
            Assert.AreEqual(DaisySize.Small, loading.Size);
            Assert.AreEqual(DaisyColor.Default, loading.Color);
            Assert.IsTrue(double.IsNaN(loading.PixelSize));

            loading.Variant = DaisyLoadingVariant.Dots;
            Assert.AreEqual(DaisyLoadingVariant.Dots, loading.Variant);
        }

        [TestMethod]
        public async Task When_Skeleton_TogglesTextMode_NoExceptions()
        {
            var skeleton = new DaisySkeleton
            {
                Content = new TextBlock { Text = "Loading" }
            };

            RuntimeTestHelpers.AttachToHost(skeleton);
            await RuntimeTestHelpers.EnsureLoadedAsync(skeleton);

            Assert.IsFalse(skeleton.IsTextMode);
            Assert.AreEqual(4, skeleton.CornerRadius.TopLeft);

            skeleton.IsTextMode = true;
            Assert.IsTrue(skeleton.IsTextMode);
        }

        [TestMethod]
        public async Task When_AnimatedNumber_ValuesSet_DefaultsHold()
        {
            var animated = new DaisyAnimatedNumber();

            RuntimeTestHelpers.AttachToHost(animated);
            await RuntimeTestHelpers.EnsureLoadedAsync(animated);

            Assert.AreEqual(0, animated.Value);
            Assert.AreEqual(0, animated.MinDigits);
            Assert.AreEqual(TimeSpan.FromMilliseconds(250), animated.Duration);
            Assert.AreEqual(18.0, animated.SlideDistance);
            Assert.AreEqual(DaisySize.Small, animated.Size);

            animated.MinDigits = 3;
            animated.Value = 7;
            Assert.AreEqual(7, animated.Value);
        }

        [TestMethod]
        public async Task When_TextRotate_ItemsSet_DefaultsHold()
        {
            var rotate = new DaisyTextRotate();
            rotate.Items.Add("One");
            rotate.Items.Add("Two");

            RuntimeTestHelpers.AttachToHost(rotate);
            await RuntimeTestHelpers.EnsureLoadedAsync(rotate);

            Assert.AreEqual(10000.0, rotate.Duration);
            Assert.AreEqual(500.0, rotate.TransitionDuration);
            Assert.AreEqual(0, rotate.CurrentIndex);
            Assert.IsFalse(rotate.IsPaused);
            Assert.IsTrue(rotate.PauseOnHover);

            rotate.IsPaused = true;
            Assert.IsTrue(rotate.IsPaused);
        }

        [TestMethod]
        public async Task When_Glass_DefaultsHold()
        {
            var glass = new DaisyGlass();

            RuntimeTestHelpers.AttachToHost(glass);
            await RuntimeTestHelpers.EnsureLoadedAsync(glass);

            Assert.AreEqual(GlassBlurMode.Simulated, glass.BlurMode);
            Assert.IsFalse(glass.EnableBackdropBlur);
            Assert.AreEqual(40.0, glass.GlassBlur);
            Assert.AreEqual(1.0, glass.GlassSaturation);
            Assert.AreEqual(0.25, glass.GlassOpacity);
            Assert.AreEqual(Colors.White, glass.GlassTint);
            Assert.AreEqual(0.5, glass.GlassTintOpacity);
            Assert.AreEqual(0.2, glass.GlassBorderOpacity);
            Assert.AreEqual(0.1, glass.GlassReflectOpacity);
            Assert.AreEqual(16, glass.CornerRadius.TopLeft);
        }

        [TestMethod]
        public async Task When_NeumorphicScope_Toggles_EffectivelyEnabled()
        {
            var host = new Grid();
            DaisyBaseContentControl.SetNeumorphicScopeEnabled(host, false);

            var control = new TestNeumorphicControl
            {
                NeumorphicMode = DaisyNeumorphicMode.Raised
            };
            host.Children.Add(control);

            RuntimeTestHelpers.AttachToHost(host);
            await RuntimeTestHelpers.EnsureLoadedAsync(control);

            Assert.IsFalse(control.IsEffectivelyNeumorphicEnabled());

            DaisyBaseContentControl.SetNeumorphicScopeEnabled(host, true);
            Assert.IsTrue(control.IsEffectivelyNeumorphicEnabled());
        }

        private sealed class TestNeumorphicControl : DaisyBaseContentControl
        {
            public bool IsEffectivelyNeumorphicEnabled() => IsNeumorphicEffectivelyEnabled();
        }
    }
}

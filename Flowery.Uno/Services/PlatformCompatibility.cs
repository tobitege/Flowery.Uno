using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Foundation.Metadata;

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable IDE0130 // Namespace does not match folder structure
#pragma warning disable CA1416 // Validate platform compatibility (we intentionally catch NotImplementedException)
#pragma warning disable UNO0001 // Uno Platform: property is not implemented (we intentionally catch at runtime)

namespace Flowery.Services
{
    /// <summary>
    /// Helpers that bridge platform gaps by providing safe fallbacks for unsupported APIs.
    /// </summary>
    internal static class PlatformCompatibility
    {
        private enum ApiSupportState
        {
            Unknown = 0,
            Supported = 1,
            NotSupported = 2
        }

        private static ApiSupportState _strokeStartLineCapSupport = ApiSupportState.Unknown;
        private static ApiSupportState _strokeEndLineCapSupport = ApiSupportState.Unknown;
        private static ApiSupportState _strokeLineJoinSupport = ApiSupportState.Unknown;

        public static bool IsSkiaBackend { get; } = DetectSkiaDesktop();
        public static bool IsWasmBackend { get; } = DetectWasmBackend();
        public static bool IsWindows { get; } = DetectWindows();
        public static bool IsAndroid { get; } = OperatingSystem.IsAndroid();
        public static bool IsIOS { get; } = OperatingSystem.IsIOS();
        public static bool IsMobile { get; } = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        public static bool UseManualCompositionAnimations { get; set; } =
            OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Safely sets StrokeStartLineCap, StrokeEndLineCap, and StrokeLineJoin to Round on a Shape.
        /// These properties are not implemented in Uno Platform's Skia backend, so we catch exceptions.
        /// </summary>
        /// <param name="shape">The shape to configure.</param>
        /// <param name="setStartCap">If true, sets StrokeStartLineCap to Round.</param>
        /// <param name="setEndCap">If true, sets StrokeEndLineCap to Round.</param>
        /// <param name="setLineJoin">If true, sets StrokeLineJoin to Round.</param>
        public static void SafeSetRoundedStroke(Shape? shape, bool setStartCap = true, bool setEndCap = true, bool setLineJoin = false)
        {
            if (shape is null) return;

            if (setStartCap)
            {
                TrySetStrokeStartLineCap(shape);
            }

            if (setEndCap)
            {
                TrySetStrokeEndLineCap(shape);
            }

            if (setLineJoin)
            {
                TrySetStrokeLineJoin(shape);
            }
        }

        private static void TrySetStrokeStartLineCap(Shape shape)
        {
            if (_strokeStartLineCapSupport == ApiSupportState.NotSupported)
            {
                return;
            }

            try
            {
                shape.StrokeStartLineCap = PenLineCap.Round;
                _strokeStartLineCapSupport = ApiSupportState.Supported;
            }
            catch (NotImplementedException)
            {
                _strokeStartLineCapSupport = ApiSupportState.NotSupported;
                // Unsupported on this runtime; skip future assignments to avoid repeated first-chance exceptions.
            }
        }

        private static void TrySetStrokeEndLineCap(Shape shape)
        {
            if (_strokeEndLineCapSupport == ApiSupportState.NotSupported)
            {
                return;
            }

            try
            {
                shape.StrokeEndLineCap = PenLineCap.Round;
                _strokeEndLineCapSupport = ApiSupportState.Supported;
            }
            catch (NotImplementedException)
            {
                _strokeEndLineCapSupport = ApiSupportState.NotSupported;
                // Unsupported on this runtime; skip future assignments to avoid repeated first-chance exceptions.
            }
        }

        private static void TrySetStrokeLineJoin(Shape shape)
        {
            if (_strokeLineJoinSupport == ApiSupportState.NotSupported)
            {
                return;
            }

            try
            {
                shape.StrokeLineJoin = PenLineJoin.Round;
                _strokeLineJoinSupport = ApiSupportState.Supported;
            }
            catch (NotImplementedException)
            {
                _strokeLineJoinSupport = ApiSupportState.NotSupported;
                // Unsupported on this runtime; skip future assignments to avoid repeated first-chance exceptions.
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2075:DynamicallyAccessedMembers",
            Justification = "DelayTime property access is optional and fails gracefully if not available.")]
        public static void TrySetAnimationDelay(KeyFrameAnimation animation, TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)
                return;

            try
            {
                var delayProperty = animation.GetType().GetProperty("DelayTime");
                delayProperty?.SetValue(animation, delay);
            }
            catch
            {
                // DelayTime not supported on this backend; ignore.
            }
        }

        public static void TrySetIsTranslationEnabled(UIElement element, bool value)
        {
            if (!ApiInformation.IsMethodPresent("Microsoft.UI.Xaml.Hosting.ElementCompositionPreview", "SetIsTranslationEnabled", 2))
            {
                return;
            }

            try
            {
                ElementCompositionPreview.SetIsTranslationEnabled(element, value);
            }
            catch
            {
                // SetIsTranslationEnabled not supported on this backend; ignore.
            }
        }

        public static void StartAnimation(Visual visual, string property, KeyFrameAnimation animation)
        {
            StartAnimation(visual, property, animation, TimeSpan.Zero);
        }

        public static void StartAnimation(Visual visual, string property, KeyFrameAnimation animation, TimeSpan delay)
        {
            if (IsWasmBackend || UseManualCompositionAnimations)
            {
                property = MapWasmAnimationProperty(property);
            }

            if (delay <= TimeSpan.Zero)
            {
                StartAnimationCore(visual, property, animation);
                return;
            }

            if (!IsSkiaBackend && !IsWasmBackend && !UseManualCompositionAnimations)
            {
                TrySetAnimationDelay(animation, delay);
                StartAnimationCore(visual, property, animation);
                return;
            }

            var queue = DispatcherQueue.GetForCurrentThread();
            if (queue == null)
            {
                StartAnimationCore(visual, property, animation);
                return;
            }

            _ = StartAnimationDelayed(queue, visual, property, animation, delay);
        }

        private static void StartAnimationCore(Visual visual, string property, KeyFrameAnimation animation)
        {
            if (IsWasmBackend || UseManualCompositionAnimations)
            {
                WasmCompositionAnimationScheduler.Register(animation, visual, property);
            }

            visual.StartAnimation(property, animation);
        }

        private static string MapWasmAnimationProperty(string property)
        {
            const string translationProperty = "Translation";
            if (property.StartsWith(translationProperty, StringComparison.Ordinal))
            {
                if (property.Length == translationProperty.Length)
                {
                    return "Offset";
                }

                if (property.Length > translationProperty.Length && property[translationProperty.Length] == '.')
                {
                    return "Offset" + property[translationProperty.Length..];
                }
            }

            return property;
        }

        private static async Task StartAnimationDelayed(DispatcherQueue queue, Visual visual, string property, KeyFrameAnimation animation, TimeSpan delay)
        {
            try
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            queue.TryEnqueue(() => StartAnimationCore(visual, property, animation));
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        public static float DegreesToRadians(float degrees) => degrees * (float)(Math.PI / 180.0);

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        public static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);

        /// <summary>
        /// Creates a scalar keyframe animation for rotation using degree values.
        /// The animation is configured to animate "RotationAngle" (radians) with values converted from degrees.
        /// </summary>
        /// <param name="compositor">The compositor to create the animation.</param>
        /// <param name="keyframes">Array of (progress, angleDegrees) tuples.</param>
        /// <param name="duration">Animation duration.</param>
        /// <param name="iterationBehavior">Animation iteration behavior.</param>
        /// <returns>A configured ScalarKeyFrameAnimation.</returns>
        public static ScalarKeyFrameAnimation CreateRotationAnimation(
            Compositor compositor,
            (float progress, float angleDegrees)[] keyframes,
            TimeSpan duration,
            AnimationIterationBehavior iterationBehavior = AnimationIterationBehavior.Forever)
        {
            var animation = compositor.CreateScalarKeyFrameAnimation();
            foreach (var (progress, angleDegrees) in keyframes)
            {
                animation.InsertKeyFrame(progress, DegreesToRadians(angleDegrees));
            }
            animation.Duration = duration;
            animation.IterationBehavior = iterationBehavior;
            return animation;
        }

        /// <summary>
        /// Starts a rotation animation on a visual using degree values.
        /// Automatically handles conversion to radians and uses the correct "RotationAngle" property.
        /// </summary>
        /// <param name="visual">The visual to animate.</param>
        /// <param name="keyframes">Array of (progress, angleDegrees) tuples.</param>
        /// <param name="duration">Animation duration.</param>
        /// <param name="delay">Optional delay before starting.</param>
        public static void StartRotationAnimationInDegrees(
            Visual visual,
            (float progress, float angleDegrees)[] keyframes,
            TimeSpan duration,
            TimeSpan delay = default)
        {
            var animation = CreateRotationAnimation(visual.Compositor, keyframes, duration);
            StartAnimation(visual, "RotationAngle", animation, delay);
        }

        public static Storyboard? StartRotationAnimation(UIElement element, Visual visual, double durationSeconds)
        {
            // Both Skia and WASM backends use manual rotation via CompositionTarget.Rendering
            // because Storyboard animations targeting dynamically-created RotateTransform objects
            // don't work reliably in Uno Platform's non-Windows backends.
            if (IsSkiaBackend || IsWasmBackend)
            {
                StartManualRotation(element, durationSeconds);
                return null;
            }

            StartCompositionRotation(visual, durationSeconds);
            return null;
        }

        public static void StopRotationAnimation(UIElement? element, Visual? visual, Storyboard? storyboard)
        {
            storyboard?.Stop();

            // Both Skia and WASM use manual rotation, so clean up the same way
            if (IsSkiaBackend || IsWasmBackend)
            {
                if (element != null)
                {
                    StopManualRotation(element);
                    var rotate = TryGetRotateTransform(element);
                    if (rotate != null)
                    {
                        rotate.Angle = 0;
                    }
                }
                return;
            }

            if (visual != null)
            {
                visual.StopAnimation("RotationAngle");
                visual.RotationAngle = 0;
            }
        }

        public static Storyboard? StartRotationKeyframes(UIElement element, (double progress, double angle)[] keyframes, TimeSpan duration)
        {
            if ((!IsSkiaBackend && !IsWasmBackend) || keyframes.Length == 0)
                return null;

            if (element.RenderTransformOrigin == default)
            {
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            var rotate = EnsureRotateTransform(element);
            rotate.Angle = keyframes[0].angle;

            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(duration),
                RepeatBehavior = RepeatBehavior.Forever,
                EnableDependentAnimation = true
            };

            foreach (var (progress, angle) in keyframes)
            {
                var clamped = progress < 0 ? 0 : progress > 1 ? 1 : progress;
                var keyTime = TimeSpan.FromTicks((long)(duration.Ticks * clamped));
                animation.KeyFrames.Add(new LinearDoubleKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(keyTime),
                    Value = angle
                });
            }

            Storyboard.SetTarget(animation, rotate);
            Storyboard.SetTargetProperty(animation, "Angle");

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
            return storyboard;
        }

        private sealed class RotationTicker
        {
            public DateTimeOffset Start { get; init; }
            public double DurationSeconds { get; init; }
            public RotateTransform Rotate { get; init; } = null!;
            public EventHandler<object>? Handler { get; set; }
        }

        private static readonly DependencyProperty RotationStateProperty =
            DependencyProperty.RegisterAttached(
                "RotationState",
                typeof(RotationTicker),
                typeof(PlatformCompatibility),
                new PropertyMetadata(null));

        private static void StartManualRotation(UIElement element, double durationSeconds)
        {
            StopManualRotation(element);

            if (element.RenderTransformOrigin == default)
            {
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            var rotate = EnsureRotateTransform(element);
            rotate.Angle = 0;

            var state = new RotationTicker
            {
                Start = DateTimeOffset.Now,
                DurationSeconds = Math.Max(0.01, durationSeconds),
                Rotate = rotate
            };

            void OnRendering(object? sender, object e)
            {
                var elapsed = (DateTimeOffset.Now - state.Start).TotalSeconds;
                var angle = (elapsed / state.DurationSeconds) * 360.0;
                state.Rotate.Angle = angle;
            }

            state.Handler = OnRendering;
            element.SetValue(RotationStateProperty, state);
            CompositionTarget.Rendering += OnRendering;
        }

        private static void StopManualRotation(UIElement element)
        {
            if (element.GetValue(RotationStateProperty) is RotationTicker { Handler: not null } state)
            {
                CompositionTarget.Rendering -= state.Handler;
                element.ClearValue(RotationStateProperty);
            }
        }

        private static void StartCompositionRotation(Visual visual, double durationSeconds)
        {
            // Note: Uno's Visual only supports animating "RotationAngle" (in radians), not "RotationAngleInDegrees"
            var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
            var linear = visual.Compositor.CreateLinearEasingFunction();
            animation.InsertKeyFrame(0f, 0f, linear);
            // End just shy of 360 degrees to avoid a visible hitch when the loop wraps.
            animation.InsertKeyFrame(1f, DegreesToRadians(359.99f), linear);
            animation.Duration = TimeSpan.FromSeconds(durationSeconds);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            StartAnimation(visual, "RotationAngle", animation);
        }

        private static RotateTransform EnsureRotateTransform(UIElement element)
        {
            var existing = element.RenderTransform;
            if (existing is RotateTransform rotateTransform)
                return rotateTransform;

            if (existing is TransformGroup group)
            {
                foreach (var child in group.Children)
                {
                    if (child is RotateTransform childRotate)
                        return childRotate;
                }

                var newRotate = new RotateTransform();
                group.Children.Add(newRotate);
                return newRotate;
            }

            var newGroup = new TransformGroup();
            if (existing != null)
            {
                newGroup.Children.Add(existing);
            }

            var rotate = new RotateTransform();
            newGroup.Children.Add(rotate);
            element.RenderTransform = newGroup;
            return rotate;
        }

        private static RotateTransform? TryGetRotateTransform(UIElement element)
        {
            var existing = element.RenderTransform;
            if (existing is RotateTransform rotateTransform)
                return rotateTransform;

            if (existing is TransformGroup group)
            {
                foreach (var child in group.Children)
                {
                    if (child is RotateTransform childRotate)
                        return childRotate;
                }
            }

            return null;
        }

        private static bool DetectSkiaDesktop()
        {
            // Uno Platform defines different symbols depending on SDK version:
            // - HAS_UNO_SKIA: Newer Uno SDK 5.x+
            // - __DESKTOP__: net9.0-desktop target
            // - __SKIA__: Legacy/alternative symbol
#if HAS_UNO_SKIA || __DESKTOP__ || __SKIA__
            return true;
#else
            return false;
#endif
        }

        private static bool DetectWasmBackend()
        {
#if __WASM__ || HAS_UNO_WASM
            return true;
#else
            // Fallback: Runtime detection for WASM (matches Uno.Foundation.Runtime.WebAssembly)
            return DetectWasmAtRuntime();
#endif
        }

        private static bool? _isWasmRuntimeDetected;
        private static bool DetectWasmAtRuntime()
        {
            if (_isWasmRuntimeDetected.HasValue)
                return _isWasmRuntimeDetected.Value;

            try
            {
                // Uno's official runtime detection uses OSPlatform "WEBASSEMBLY" / "BROWSER"
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                        System.Runtime.InteropServices.OSPlatform.Create("WEBASSEMBLY")) ||
                    System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                        System.Runtime.InteropServices.OSPlatform.Create("BROWSER")))
                {
                    _isWasmRuntimeDetected = true;
                    return true;
                }

                // Check for WASM-specific types
                var wasmType = Type.GetType("Uno.Foundation.WebAssemblyRuntime, Uno.Foundation.Runtime.WebAssembly");
                if (wasmType != null)
                {
                    _isWasmRuntimeDetected = true;
                    return true;
                }
            }
            catch
            {
                // Ignore detection errors
            }

            _isWasmRuntimeDetected = false;
            return false;
        }
        private static bool DetectWindows()
        {
#if WINDOWS
            return true;
#else
            return OperatingSystem.IsWindows() && !DetectSkiaDesktop();
            // Note: OperatingSystem.IsWindows() can return true for net9.0-windows based Skia targets too (e.g. WPF),
            // but usually we distinguish 'Native Windows App SDK' (WINDOWS) vs 'Skia on Windows' (DetectSkiaDesktop).
            // This flag is intended for "Native OneCore/UWP/WinUI" path.
#endif
        }
    }
}

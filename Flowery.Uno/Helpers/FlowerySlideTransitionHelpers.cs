using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using System.Numerics;

namespace Flowery.Helpers
{
    #region Slide Transition Helpers

    /// <summary>
    /// Static helper class for creating slide transition animations between UI elements.
    /// Transitions animate the change from one slide to another (unlike SlideEffects which
    /// animate a single slide during display).
    /// </summary>
    /// <remarks>
    /// Transition architecture:
    /// - Both old and new elements must be in the visual tree during the transition
    /// - Transitions use a dedicated overlay Grid that contains both elements
    /// - The caller is responsible for providing the container and managing element lifecycle
    /// </remarks>
    public static class FlowerySlideTransitionHelpers
    {
        private static readonly Random _random = new();

        // Track active transitions to prevent overlapping animations
        private static readonly Dictionary<UIElement, Storyboard> _activeTransitions = [];

        // Track containers that need background cleanup after FadeThrough transitions
        private static readonly Dictionary<UIElement, Grid> _fadeThroughContainers = [];

        // Track resources (clips, brushes, effects) for cleanup after transitions
        private static readonly Dictionary<UIElement, object> _transitionResources = [];

        private sealed class PixelateSpriteResources
        {
            private readonly SpriteVisual _oldSprite;
            private readonly SpriteVisual _newSprite;
            private readonly Grid _container;

            public PixelateSpriteResources(SpriteVisual oldSprite, SpriteVisual newSprite, Grid container)
            {
                _oldSprite = oldSprite;
                _newSprite = newSprite;
                _container = container;
            }

            public void Restore()
            {
                if (ElementCompositionPreview.GetElementVisual(_container) is ContainerVisual containerVisual)
                {
                    containerVisual.Children.Remove(_newSprite);
                    containerVisual.Children.Remove(_oldSprite);
                }
            }
        }

        #region Public API

        /// <summary>
        /// Applies a slide transition between two elements within a container.
        /// </summary>
        /// <param name="container">The parent container (Grid) holding both elements.</param>
        /// <param name="oldElement">The element transitioning out (can be null for first load).</param>
        /// <param name="newElement">The element transitioning in.</param>
        /// <param name="transition">The type of transition to apply.</param>
        /// <param name="transitionParams">Optional parameters for the transition.</param>
        /// <param name="onComplete">Callback when transition completes (for cleanup).</param>
        /// <returns>The resolved transition that was applied (important for Random).</returns>
        public static FlowerySlideTransition ApplyTransition(
            Grid container,
            UIElement? oldElement,
            UIElement newElement,
            FlowerySlideTransition transition,
            FlowerySlideTransitionParams? transitionParams = null,
            Action? onComplete = null)
        {
            var resolvedTransition = transition;
            var @params = transitionParams ?? new FlowerySlideTransitionParams();

            // Handle Random transition selection
            if (transition == FlowerySlideTransition.Random)
            {
                // Default to Tier 1 (Transform) transitions for cross-platform compatibility
                resolvedTransition = FlowerySlideTransitionParser.PickRandom(FloweryTransitionTier.Transform);
            }

            resolvedTransition = NormalizeTransitionForPlatform(resolvedTransition);

            // None = instant swap, no animation
            if (resolvedTransition == FlowerySlideTransition.None)
            {
                if (oldElement != null)
                    oldElement.Visibility = Visibility.Collapsed;
                newElement.Visibility = Visibility.Visible;
                onComplete?.Invoke();
                return resolvedTransition;
            }

            // Stop any existing transition on the new element
            StopTransition(newElement);

            // Ensure both elements are visible for the transition
            if (oldElement != null)
                oldElement.Visibility = Visibility.Visible;
            newElement.Visibility = Visibility.Visible;
            newElement.Opacity = 1;

            // Create the transition storyboard
            var storyboard = FloweryAnimationHelpers.CreateStoryboard();

            // Apply the specific transition
            bool applied;
            try
            {
                applied = ApplyTransitionCore(container, oldElement, newElement, resolvedTransition, @params, storyboard);
            }
            catch (Exception ex) when (ex is NotImplementedException or PlatformNotSupportedException or NotSupportedException)
            {
                applied = false;
            }

            if (!applied)
            {
                ResetElementTransforms(oldElement);
                ResetElementTransforms(newElement);

                // Fallback to instant show if transition could not be applied
                if (oldElement != null)
                    oldElement.Visibility = Visibility.Collapsed;
                newElement.Visibility = Visibility.Visible;
                onComplete?.Invoke();
                return resolvedTransition;
            }

            // Track and start the transition
            _activeTransitions[newElement] = storyboard;

            storyboard.Completed += (s, e) =>
            {
                _activeTransitions.Remove(newElement);

                // Clean up FadeThrough container background
                if (_fadeThroughContainers.TryGetValue(newElement, out var fadeThroughContainer))
                {
                    // Clear the through color background
                    fadeThroughContainer.Background = null;
                    _fadeThroughContainers.Remove(newElement);
                }

                // Clean up transition resources (clips, masks, proxy visuals)
                if (_transitionResources.TryGetValue(newElement, out var resource))
                {
                    if (resource is RectangleGeometry)
                    {
                        newElement.Clip = null;
                    }
                    else if (resource is SpriteVisual sprite)
                    {
                        // Remove proxy visual from container
                        if (ElementCompositionPreview.GetElementVisual(container) is ContainerVisual containerVisual)
                        {
                            containerVisual.Children.Remove(sprite);
                        }

                        // Restore original element visibility
                        newElement.Opacity = 1;
                    }
                    else if (resource is PixelateSpriteResources pixelateResources)
                    {
                        pixelateResources.Restore();
                    }

                    _transitionResources.Remove(newElement);
                }

                // Hide old element after transition
                if (oldElement != null)
                    oldElement.Visibility = Visibility.Collapsed;

                // Reset transforms
                ResetElementTransforms(oldElement);
                ResetElementTransforms(newElement);

                onComplete?.Invoke();
            };

            storyboard.Begin();
            return resolvedTransition;
        }

        /// <summary>
        /// Stops any active transition on an element.
        /// </summary>
        public static void StopTransition(UIElement element)
        {
            if (_activeTransitions.TryGetValue(element, out var storyboard))
            {
                storyboard.Stop();
                _activeTransitions.Remove(element);
                ResetElementTransforms(element);
            }
        }

        /// <summary>
        /// Checks if a transition is currently animating on an element.
        /// </summary>
        public static bool IsTransitioning(UIElement element) =>
            _activeTransitions.ContainsKey(element);

        private static FlowerySlideTransition NormalizeTransitionForPlatform(FlowerySlideTransition transition)
        {
            if (!PlatformCompatibility.IsWasmBackend)
            {
                return transition;
            }

            // Allow WASM transitions (Skia browser runtime handles these now).
            return transition;
        }

        public static bool IsWasmCompositionTransition(FlowerySlideTransition transition)
        {
            return transition is FlowerySlideTransition.BlindsHorizontal
                or FlowerySlideTransition.BlindsVertical
                or FlowerySlideTransition.SlicesHorizontal
                or FlowerySlideTransition.SlicesVertical
                or FlowerySlideTransition.Checkerboard
                or FlowerySlideTransition.Spiral
                or FlowerySlideTransition.MatrixRain
                or FlowerySlideTransition.Wormhole
                or FlowerySlideTransition.Dissolve
                or FlowerySlideTransition.Pixelate;
        }

        #endregion

        #region Transition Implementation

        private static bool ApplyTransitionCore(
            Grid container,
            UIElement? oldElement,
            UIElement newElement,
            FlowerySlideTransition transition,
            FlowerySlideTransitionParams @params,
            Storyboard storyboard)
        {
            var tier = FlowerySlideTransitionParser.GetTier(transition);

            return tier switch
            {
                FloweryTransitionTier.Transform => ApplyTransformTransition(container, oldElement, newElement, transition, @params, storyboard),
                FloweryTransitionTier.Clip => ApplyClipTransition(container, oldElement, newElement, transition, @params, storyboard),
                FloweryTransitionTier.Skia => ApplySkiaTransition(container, oldElement, newElement, transition, @params, storyboard),
                _ => false
            };
        }

        /// <summary>
        /// Applies Tier 1 (Transform-based) transitions: Fade, Slide, Push, Zoom, Flip, Cover, Reveal
        /// </summary>
        private static bool ApplyTransformTransition(
            Grid container,
            UIElement? oldElement,
            UIElement newElement,
            FlowerySlideTransition transition,
            FlowerySlideTransitionParams @params,
            Storyboard storyboard)
        {
            var duration = @params.Duration;
            var easing = CreateEasing(@params.EasingMode);

            // Get container dimensions for slide/push calculations
            var width = container.ActualWidth > 0 ? container.ActualWidth : 400;
            var height = container.ActualHeight > 0 ? container.ActualHeight : 300;

            return transition switch
            {
                // Fade family
                FlowerySlideTransition.Fade => ApplyFadeTransition(oldElement, newElement, duration, easing, storyboard),
                FlowerySlideTransition.FadeThroughBlack => ApplyFadeThroughTransition(container, oldElement, newElement, duration, easing, storyboard, Windows.UI.Color.FromArgb(255, 0, 0, 0)),
                FlowerySlideTransition.FadeThroughWhite => ApplyFadeThroughTransition(container, oldElement, newElement, duration, easing, storyboard, Windows.UI.Color.FromArgb(255, 255, 255, 255)),

                // Slide family
                FlowerySlideTransition.SlideLeft => ApplySlideTransition(oldElement, newElement, duration, easing, storyboard, -width, 0, width, 0),
                FlowerySlideTransition.SlideRight => ApplySlideTransition(oldElement, newElement, duration, easing, storyboard, width, 0, -width, 0),
                FlowerySlideTransition.SlideUp => ApplySlideTransition(oldElement, newElement, duration, easing, storyboard, 0, -height, 0, height),
                FlowerySlideTransition.SlideDown => ApplySlideTransition(oldElement, newElement, duration, easing, storyboard, 0, height, 0, -height),

                // Push family
                FlowerySlideTransition.PushLeft => ApplyPushTransition(oldElement, newElement, duration, easing, storyboard, -width, 0),
                FlowerySlideTransition.PushRight => ApplyPushTransition(oldElement, newElement, duration, easing, storyboard, width, 0),
                FlowerySlideTransition.PushUp => ApplyPushTransition(oldElement, newElement, duration, easing, storyboard, 0, -height),
                FlowerySlideTransition.PushDown => ApplyPushTransition(oldElement, newElement, duration, easing, storyboard, 0, height),

                // Zoom family
                FlowerySlideTransition.ZoomIn => ApplyZoomInTransition(oldElement, newElement, duration, easing, storyboard, @params.ZoomScale),
                FlowerySlideTransition.ZoomOut => ApplyZoomOutTransition(oldElement, newElement, duration, easing, storyboard, @params.ZoomScale),
                FlowerySlideTransition.ZoomCross => ApplyZoomCrossTransition(oldElement, newElement, duration, easing, storyboard, @params.ZoomScale),

                // Flip family (3D)
                FlowerySlideTransition.FlipHorizontal => ApplyFlipTransition(oldElement, newElement, duration, easing, storyboard, true, @params.FlipAngle),
                FlowerySlideTransition.FlipVertical => ApplyFlipTransition(oldElement, newElement, duration, easing, storyboard, false, @params.FlipAngle),
                FlowerySlideTransition.CubeLeft => ApplyCubeTransition(oldElement, newElement, duration, easing, storyboard, true, width),
                FlowerySlideTransition.CubeRight => ApplyCubeTransition(oldElement, newElement, duration, easing, storyboard, false, width),

                // Cover/Reveal family
                FlowerySlideTransition.CoverLeft => ApplyCoverTransition(oldElement, newElement, duration, easing, storyboard, -width, 0),
                FlowerySlideTransition.CoverRight => ApplyCoverTransition(oldElement, newElement, duration, easing, storyboard, width, 0),
                FlowerySlideTransition.CoverUp => ApplyCoverTransition(oldElement, newElement, duration, easing, storyboard, 0, -height),
                FlowerySlideTransition.CoverDown => ApplyCoverTransition(oldElement, newElement, duration, easing, storyboard, 0, height),
                FlowerySlideTransition.RevealLeft => ApplyRevealTransition(oldElement, newElement, duration, easing, storyboard, -width, 0),
                FlowerySlideTransition.RevealRight => ApplyRevealTransition(oldElement, newElement, duration, easing, storyboard, width, 0),

                _ => false
            };
        }

        /// <summary>
        /// Applies Tier 2 (Clip-based) transitions: Wipe, Blinds, Slices, Checkerboard
        /// </summary>
        private static bool ApplyClipTransition(
            Grid container,
            UIElement? oldElement,
            UIElement newElement,
            FlowerySlideTransition transition,
            FlowerySlideTransitionParams @params,
            Storyboard storyboard)
        {
            var duration = @params.Duration;
            var easing = CreateEasing(@params.EasingMode);
            var width = container.ActualWidth > 0 ? container.ActualWidth : 400;
            var height = container.ActualHeight > 0 ? container.ActualHeight : 300;

            return transition switch
            {
                // Wipe family
                FlowerySlideTransition.WipeLeft => ApplyWipeTransition(oldElement, newElement, duration, easing, storyboard, width, height, WipeDirection.Left),
                FlowerySlideTransition.WipeRight => ApplyWipeTransition(oldElement, newElement, duration, easing, storyboard, width, height, WipeDirection.Right),
                FlowerySlideTransition.WipeUp => ApplyWipeTransition(oldElement, newElement, duration, easing, storyboard, width, height, WipeDirection.Up),
                FlowerySlideTransition.WipeDown => ApplyWipeTransition(oldElement, newElement, duration, easing, storyboard, width, height, WipeDirection.Down),

                // Blinds family
                FlowerySlideTransition.BlindsHorizontal => ApplyBlindsTransition(oldElement, newElement, duration, easing, storyboard, width, height, horizontal: true, @params.SliceCount, @params.StaggerSlices, @params.SliceStaggerMs),
                FlowerySlideTransition.BlindsVertical => ApplyBlindsTransition(oldElement, newElement, duration, easing, storyboard, width, height, horizontal: false, @params.SliceCount, @params.StaggerSlices, @params.SliceStaggerMs),

                // Slices family
                FlowerySlideTransition.SlicesHorizontal => ApplySlicesTransition(oldElement, newElement, duration, easing, storyboard, width, height, horizontal: true, @params.SliceCount, @params.SliceStaggerMs),
                FlowerySlideTransition.SlicesVertical => ApplySlicesTransition(oldElement, newElement, duration, easing, storyboard, width, height, horizontal: false, @params.SliceCount, @params.SliceStaggerMs),
                FlowerySlideTransition.Checkerboard => ApplyCheckerboardTransition(oldElement, newElement, duration, easing, storyboard, width, height, @params.CheckerboardSize, @params.SliceStaggerMs),
                FlowerySlideTransition.Spiral => ApplySpiralTransition(oldElement, newElement, duration, easing, storyboard, width, height, @params.CheckerboardSize, @params.SliceStaggerMs),
                FlowerySlideTransition.MatrixRain => ApplyMatrixRainTransition(oldElement, newElement, duration, easing, storyboard, width, height, @params.CheckerboardSize, @params.SliceStaggerMs),
                FlowerySlideTransition.Wormhole => ApplyWormholeTransition(oldElement, newElement, duration, easing, storyboard, width, height),

                _ => ApplyFadeTransition(oldElement, newElement, duration, easing, storyboard)
            };
        }

        private enum WipeDirection { Left, Right, Up, Down }

        private static bool ApplySkiaTransition(
            Grid container,
            UIElement? oldElement,
            UIElement newElement,
            FlowerySlideTransition transition,
            FlowerySlideTransitionParams @params,
            Storyboard storyboard)
        {
            var duration = @params.Duration;
            var easing = CreateEasing(@params.EasingMode);
            var width = container.ActualWidth > 0 ? container.ActualWidth : 400;
            var height = container.ActualHeight > 0 ? container.ActualHeight : 300;

            // All Tier 3 transitions start with a clean slate
            newElement.Opacity = 1;
            Canvas.SetZIndex(newElement, 1);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            if (transition == FlowerySlideTransition.Dissolve)
            {
                return ApplyDissolveTransition(oldElement, newElement, duration, easing, storyboard, width, height, @params.DissolveDensity);
            }

            if (transition == FlowerySlideTransition.Pixelate)
            {
                return ApplyPixelateTransition(oldElement, newElement, duration, easing, storyboard, width, height, @params.PixelateSize);
            }

            return ApplyFadeTransition(oldElement, newElement, duration, easing, storyboard);
        }

        #endregion

        #region Tier 1: Transform Transition Implementations

        private static bool ApplyFadeTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard)
        {
            // New element starts transparent, fades to opaque
            newElement.Opacity = 0;
            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, duration, easing);
            storyboard.Children.Add(fadeIn);

            // Old element fades out
            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);
                storyboard.Children.Add(fadeOut);
            }

            return true;
        }

        private static bool ApplyFadeThroughTransition(Grid container, UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, Windows.UI.Color throughColor)
        {
            // Save original background and set the through color
            container.Background = new SolidColorBrush(throughColor);

            var halfDuration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / 2);

            // New element starts transparent, begins fading in at half-time
            newElement.Opacity = 0;
            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, halfDuration, easing);
            fadeIn.BeginTime = halfDuration;
            storyboard.Children.Add(fadeIn);

            // Old element fades out in first half
            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, halfDuration, easing);
                storyboard.Children.Add(fadeOut);
            }

            // Track container for background cleanup
            _fadeThroughContainers[newElement] = container;

            return true;
        }

        private static bool ApplySlideTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, double oldExitX, double oldExitY, double newEnterX, double newEnterY)
        {
            // Slide: Both elements move independently with crossfade for smoother look
            // New element slides in from offscreen
            var newTransform = FloweryAnimationHelpers.EnsureCompositeTransform(newElement);
            newTransform.TranslateX = newEnterX;
            newTransform.TranslateY = newEnterY;
            newElement.Opacity = 0;

            var slideInX = CreateDoubleAnimation(newElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)", newEnterX, 0, duration, easing);
            var slideInY = CreateDoubleAnimation(newElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)", newEnterY, 0, duration, easing);
            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, duration, easing);
            storyboard.Children.Add(slideInX);
            storyboard.Children.Add(slideInY);
            storyboard.Children.Add(fadeIn);

            // Old element slides out with fade
            if (oldElement != null)
            {
                _ = FloweryAnimationHelpers.EnsureCompositeTransform(oldElement);
                var slideOutX = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)", 0, oldExitX, duration, easing);
                var slideOutY = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)", 0, oldExitY, duration, easing);
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);
                storyboard.Children.Add(slideOutX);
                storyboard.Children.Add(slideOutY);
                storyboard.Children.Add(fadeOut);
            }

            return true;
        }

        private static bool ApplyPushTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, double pushX, double pushY)
        {
            // Both elements move together - new pushes old
            var newTransform = FloweryAnimationHelpers.EnsureCompositeTransform(newElement);
            newTransform.TranslateX = -pushX; // Start from opposite side
            newTransform.TranslateY = -pushY;

            var slideIn = CreateDoubleAnimation(newElement, pushX != 0 ? "(UIElement.RenderTransform).(CompositeTransform.TranslateX)" : "(UIElement.RenderTransform).(CompositeTransform.TranslateY)", pushX != 0 ? -pushX : -pushY, 0, duration, easing);
            storyboard.Children.Add(slideIn);

            if (oldElement != null)
            {
                _ = FloweryAnimationHelpers.EnsureCompositeTransform(oldElement);
                var slideOut = CreateDoubleAnimation(oldElement, pushX != 0 ? "(UIElement.RenderTransform).(CompositeTransform.TranslateX)" : "(UIElement.RenderTransform).(CompositeTransform.TranslateY)", 0, pushX != 0 ? pushX : pushY, duration, easing);
                storyboard.Children.Add(slideOut);
            }

            return true;
        }

        private static bool ApplyZoomInTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, double startScale)
        {
            // New element zooms in from small
            var newTransform = FloweryAnimationHelpers.EnsureCompositeTransform(newElement);
            newTransform.ScaleX = startScale;
            newTransform.ScaleY = startScale;
            newElement.Opacity = 0;

            var zoomX = CreateDoubleAnimation(newElement, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", startScale, 1, duration, easing);
            var zoomY = CreateDoubleAnimation(newElement, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", startScale, 1, duration, easing);
            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, duration, easing);
            storyboard.Children.Add(zoomX);
            storyboard.Children.Add(zoomY);
            storyboard.Children.Add(fadeIn);

            // Old element fades out
            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);
                storyboard.Children.Add(fadeOut);
            }

            return true;
        }

        private static bool ApplyZoomOutTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, double endScale)
        {
            // Old element zooms out to small
            if (oldElement != null)
            {
                _ = FloweryAnimationHelpers.EnsureCompositeTransform(oldElement);
                var zoomX = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", 1, endScale, duration, easing);
                var zoomY = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", 1, endScale, duration, easing);
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);
                storyboard.Children.Add(zoomX);
                storyboard.Children.Add(zoomY);
                storyboard.Children.Add(fadeOut);
            }

            // New element fades in
            newElement.Opacity = 0;
            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, duration, easing);
            storyboard.Children.Add(fadeIn);

            return true;
        }

        private static bool ApplyZoomCrossTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, double scale)
        {
            // Both zoom simultaneously - old zooms out, new zooms in
            ApplyZoomInTransition(oldElement, newElement, duration, easing, storyboard, scale);
            if (oldElement != null)
            {
                _ = FloweryAnimationHelpers.EnsureCompositeTransform(oldElement);
                var zoomOutX = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", 1, 1 + (1 - scale), duration, easing);
                var zoomOutY = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", 1, 1 + (1 - scale), duration, easing);
                storyboard.Children.Add(zoomOutX);
                storyboard.Children.Add(zoomOutY);
            }
            return true;
        }

        private static bool ApplyFlipTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, bool horizontal, double angle)
        {
            // PlaneProjection is not implemented on Skia/WASM, so use a 2D fallback.
            if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
            {
                return ApplyFlatFlipTransition(oldElement, newElement, duration, easing, storyboard, horizontal);
            }

            // Set up PlaneProjection for new element
            var newProj = new PlaneProjection { CenterOfRotationX = 0.5, CenterOfRotationY = 0.5 };
            newElement.Projection = newProj;
            newElement.Opacity = 0;

            if (horizontal)
            {
                // Flip on Horizontal axis (Rotate around X)
                newProj.RotationX = -angle;
                var animIn = new DoubleAnimation { From = -angle, To = 0, Duration = new Duration(duration), EasingFunction = easing };
                Storyboard.SetTarget(animIn, newProj);
                Storyboard.SetTargetProperty(animIn, "RotationX");
                storyboard.Children.Add(animIn);

                if (oldElement != null)
                {
                    var oldProj = new PlaneProjection { CenterOfRotationX = 0.5, CenterOfRotationY = 0.5 };
                    oldElement.Projection = oldProj;
                    var animOut = new DoubleAnimation { From = 0, To = angle, Duration = new Duration(duration), EasingFunction = easing };
                    Storyboard.SetTarget(animOut, oldProj);
                    Storyboard.SetTargetProperty(animOut, "RotationX");
                    storyboard.Children.Add(animOut);
                }
            }
            else
            {
                // Flip on Vertical axis (Rotate around Y)
                newProj.RotationY = -angle;
                var animIn = new DoubleAnimation { From = -angle, To = 0, Duration = new Duration(duration), EasingFunction = easing };
                Storyboard.SetTarget(animIn, newProj);
                Storyboard.SetTargetProperty(animIn, "RotationY");
                storyboard.Children.Add(animIn);

                if (oldElement != null)
                {
                    var oldProj = new PlaneProjection { CenterOfRotationX = 0.5, CenterOfRotationY = 0.5 };
                    oldElement.Projection = oldProj;
                    var animOut = new DoubleAnimation { From = 0, To = angle, Duration = new Duration(duration), EasingFunction = easing };
                    Storyboard.SetTarget(animOut, oldProj);
                    Storyboard.SetTargetProperty(animOut, "RotationY");
                    storyboard.Children.Add(animOut);
                }
            }

            // Crossfade for a smoother look
            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, duration, easing);
            storyboard.Children.Add(fadeIn);

            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);
                storyboard.Children.Add(fadeOut);
            }

            return true;
        }

        private static bool ApplyCubeTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, bool left, double width)
        {
            // PlaneProjection is not implemented on Skia/WASM, so use a 2D fallback.
            if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
            {
                return ApplyFlatCubeTransition(oldElement, newElement, duration, easing, storyboard, left, width);
            }

            var direction = left ? -1 : 1;
            double angle = 90 * direction;

            // Perspective distance for cube feel
            double depth = width / 2;

            // New Slide: Rotates from -90 to 0
            var newProj = new PlaneProjection { CenterOfRotationX = 0.5, CenterOfRotationY = 0.5, GlobalOffsetZ = depth };
            newElement.Projection = newProj;
            newElement.Opacity = 0;

            var newRot = new DoubleAnimation { From = -angle, To = 0, Duration = new Duration(duration), EasingFunction = easing };
            Storyboard.SetTarget(newRot, newProj);
            Storyboard.SetTargetProperty(newRot, "RotationY");

            var newZ = new DoubleAnimation { From = depth, To = 0, Duration = new Duration(duration), EasingFunction = easing };
            Storyboard.SetTarget(newZ, newProj);
            Storyboard.SetTargetProperty(newZ, "GlobalOffsetZ");

            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, duration, easing);

            storyboard.Children.Add(newRot);
            storyboard.Children.Add(newZ);
            storyboard.Children.Add(fadeIn);

            if (oldElement != null)
            {
                // Old Slide: Rotates from 0 to 90
                var oldProj = new PlaneProjection { CenterOfRotationX = 0.5, CenterOfRotationY = 0.5, GlobalOffsetZ = 0 };
                oldElement.Projection = oldProj;

                var oldRot = new DoubleAnimation { From = 0, To = angle, Duration = new Duration(duration), EasingFunction = easing };
                Storyboard.SetTarget(oldRot, oldProj);
                Storyboard.SetTargetProperty(oldRot, "RotationY");

                var oldZ = new DoubleAnimation { From = 0, To = depth, Duration = new Duration(duration), EasingFunction = easing };
                Storyboard.SetTarget(oldZ, oldProj);
                Storyboard.SetTargetProperty(oldZ, "GlobalOffsetZ");

                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);

                storyboard.Children.Add(oldRot);
                storyboard.Children.Add(oldZ);
                storyboard.Children.Add(fadeOut);
            }

            return true;
        }

        private static bool ApplyFlatFlipTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, bool horizontal)
        {
            const double minScale = 0.05;
            var halfDuration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / 2.0);
            var scaleProperty = horizontal
                ? "(UIElement.RenderTransform).(CompositeTransform.ScaleY)"
                : "(UIElement.RenderTransform).(CompositeTransform.ScaleX)";

            var newTransform = FloweryAnimationHelpers.EnsureCompositeTransform(newElement);
            if (horizontal)
            {
                newTransform.ScaleY = minScale;
            }
            else
            {
                newTransform.ScaleX = minScale;
            }
            newElement.Opacity = 0;

            var scaleIn = CreateDoubleAnimation(newElement, scaleProperty, minScale, 1, halfDuration, easing);
            scaleIn.BeginTime = halfDuration;
            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, halfDuration, easing);
            fadeIn.BeginTime = halfDuration;
            storyboard.Children.Add(scaleIn);
            storyboard.Children.Add(fadeIn);

            if (oldElement != null)
            {
                _ = FloweryAnimationHelpers.EnsureCompositeTransform(oldElement);
                var scaleOut = CreateDoubleAnimation(oldElement, scaleProperty, 1, minScale, halfDuration, easing);
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, halfDuration, easing);
                storyboard.Children.Add(scaleOut);
                storyboard.Children.Add(fadeOut);
            }

            return true;
        }

        private static bool ApplyFlatCubeTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, bool left, double width)
        {
            const double minScale = 0.85;
            const double translateFactor = 0.35;
            var enterX = (left ? 1 : -1) * width * translateFactor;
            var exitX = -enterX;

            var newTransform = FloweryAnimationHelpers.EnsureCompositeTransform(newElement);
            newTransform.TranslateX = enterX;
            newTransform.ScaleX = minScale;
            newElement.Opacity = 0;

            var slideIn = CreateDoubleAnimation(newElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)", enterX, 0, duration, easing);
            var scaleIn = CreateDoubleAnimation(newElement, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", minScale, 1, duration, easing);
            var fadeIn = CreateOpacityAnimation(newElement, 0, 1, duration, easing);
            storyboard.Children.Add(slideIn);
            storyboard.Children.Add(scaleIn);
            storyboard.Children.Add(fadeIn);

            if (oldElement != null)
            {
                _ = FloweryAnimationHelpers.EnsureCompositeTransform(oldElement);
                var slideOut = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)", 0, exitX, duration, easing);
                var scaleOut = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", 1, minScale, duration, easing);
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);
                storyboard.Children.Add(slideOut);
                storyboard.Children.Add(scaleOut);
                storyboard.Children.Add(fadeOut);
            }

            return true;
        }

        private static bool ApplyCoverTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, double enterX, double enterY)
        {
            // Cover: New slides in ON TOP of old, old stays stationary
            // Ensure new element is on top using Canvas.ZIndex
            Canvas.SetZIndex(newElement, 1);
            if (oldElement != null)
                Canvas.SetZIndex(oldElement, 0);

            var newTransform = FloweryAnimationHelpers.EnsureCompositeTransform(newElement);
            // enterX/Y is the direction, so negate it for starting position
            newTransform.TranslateX = -enterX;
            newTransform.TranslateY = -enterY;

            var slideInX = CreateDoubleAnimation(newElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)", -enterX, 0, duration, easing);
            var slideInY = CreateDoubleAnimation(newElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)", -enterY, 0, duration, easing);
            storyboard.Children.Add(slideInX);
            storyboard.Children.Add(slideInY);

            return true;
        }

        private static bool ApplyRevealTransition(UIElement? oldElement, UIElement newElement, TimeSpan duration, EasingFunctionBase easing, Storyboard storyboard, double exitX, double exitY)
        {
            // Reveal: Old slides out ON TOP, revealing new underneath (new is stationary)
            // Ensure old element is on top using Canvas.ZIndex
            Canvas.SetZIndex(newElement, 0);
            if (oldElement != null)
            {
                Canvas.SetZIndex(oldElement, 1);
                _ = FloweryAnimationHelpers.EnsureCompositeTransform(oldElement);
                var slideOutX = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)", 0, exitX, duration, easing);
                var slideOutY = CreateDoubleAnimation(oldElement, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)", 0, exitY, duration, easing);
                storyboard.Children.Add(slideOutX);
                storyboard.Children.Add(slideOutY);
            }

            return true;
        }

        #endregion

        #region Tier 2: Clip Transition Implementations

        private static bool ApplyWipeTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            WipeDirection direction)
        {
            // Create a RectangleGeometry for clipping
            var clipGeometry = new RectangleGeometry();
            Windows.Foundation.Rect endRect = new(0, 0, width, height);

            // Set initial and target clip based on direction
            var startRect = direction switch
            {
                WipeDirection.Left => new Windows.Foundation.Rect(width, 0, 0, height),
                WipeDirection.Right => new Windows.Foundation.Rect(0, 0, 0, height),
                WipeDirection.Up => new Windows.Foundation.Rect(0, height, width, 0),
                _ => new Windows.Foundation.Rect(0, 0, width, 0),
            };
            clipGeometry.Rect = startRect;
            newElement.Clip = clipGeometry;

            // Ensure Visibility and Z-Order
            newElement.Opacity = 1;
            Canvas.SetZIndex(newElement, 1);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            // Set initial and target clip based on direction
            var rectAnimation = new ObjectAnimationUsingKeyFrames();
            Storyboard.SetTarget(rectAnimation, clipGeometry);
            Storyboard.SetTargetProperty(rectAnimation, "Rect");

            // Create keyframes for smooth animation
            int steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                var t = (double)i / steps;
                var easedT = ApplyEasing(t, easing);

                var interpolatedRect = new Windows.Foundation.Rect(
                    Lerp(startRect.X, endRect.X, easedT),
                    Lerp(startRect.Y, endRect.Y, easedT),
                    Lerp(startRect.Width, endRect.Width, easedT),
                    Lerp(startRect.Height, endRect.Height, easedT));

                var keyFrame = new DiscreteObjectKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * t)),
                    Value = interpolatedRect
                };
                rectAnimation.KeyFrames.Add(keyFrame);
            }

            storyboard.Children.Add(rectAnimation);

            // Fade out old element
            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);
                storyboard.Children.Add(fadeOut);
            }

            // Track for clip cleanup
            _transitionResources[newElement] = clipGeometry;

            return true;
        }

        private static bool ApplyBlindsTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            bool horizontal,
            int sliceCount,
            bool stagger,
            double staggerMs)
        {
            // Blinds uses sequential reveal (Top to Bottom or Left to Right)
            return ApplySlicesTransition(oldElement, newElement, duration, easing, storyboard, width, height, horizontal, sliceCount, stagger ? staggerMs : 0, randomize: false);
        }

        private static bool ApplySlicesTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            bool horizontal,
            int sliceCount,
            double staggerMs,
            bool randomize = true)
        {
            // For robust cross-platform behavior, we implement Slices using a visual mask grid
            newElement.Opacity = 1;
            var visual = ElementCompositionPreview.GetElementVisual(newElement);
            var compositor = visual.Compositor;

            var maskRoot = compositor.CreateContainerVisual();
            maskRoot.Size = new Vector2((float)width, (float)height);

            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)width, (float)height);

            var surface = compositor.CreateVisualSurface();
            surface.SourceVisual = visual;
            surface.SourceSize = new Vector2((float)width, (float)height);
            var surfaceBrush = compositor.CreateSurfaceBrush(surface);

            var maskSurface = compositor.CreateVisualSurface();
            maskSurface.SourceVisual = maskRoot;
            maskSurface.SourceSize = new Vector2((float)width, (float)height);
            var maskSurfaceBrush = compositor.CreateSurfaceBrush(maskSurface);

            float sliceSize = horizontal ? (float)height / sliceCount : (float)width / sliceCount;
            var order = Enumerable.Range(0, sliceCount).ToArray();
            if (randomize) order = [.. order.OrderBy(_ => _random.Next())];

            for (int orderIndex = 0; orderIndex < sliceCount; orderIndex++)
            {
                int i = order[orderIndex];
                var cell = compositor.CreateSpriteVisual();
                if (horizontal)
                {
                    cell.Size = new Vector2((float)width, sliceSize);
                    cell.Offset = new Vector3(0, i * sliceSize, 0);
                    cell.Scale = new Vector3(1, 0, 1);
                }
                else
                {
                    cell.Size = new Vector2(sliceSize, (float)height);
                    cell.Offset = new Vector3(i * sliceSize, 0, 0);
                    cell.Scale = new Vector3(0, 1, 1);
                }

                cell.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
                cell.Opacity = 0;
                cell.CenterPoint = new Vector3(cell.Size.X / 2, cell.Size.Y / 2, 0);
                maskRoot.Children.InsertAtTop(cell);

                var delay = TimeSpan.FromMilliseconds(orderIndex * staggerMs);
                var cellDuration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * 0.6);

                var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
                scaleAnim.Duration = cellDuration;
                scaleAnim.InsertKeyFrame(1f, new Vector3(1, 1, 1));
                PlatformCompatibility.StartAnimation(cell, "Scale", scaleAnim, delay);

                var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
                opacityAnim.Duration = cellDuration;
                opacityAnim.InsertKeyFrame(1f, 1f);
                PlatformCompatibility.StartAnimation(cell, "Opacity", opacityAnim, delay);
            }

            var maskBrush = compositor.CreateMaskBrush();
            maskBrush.Source = surfaceBrush;
            maskBrush.Mask = maskSurfaceBrush;
            sprite.Brush = maskBrush;

            // Tier 2 Fix: Source element MUST be Opacity=1 to render to surface.
            // We hide the 'ghost' by placing it behind background elements.
            newElement.Opacity = 1;
            Canvas.SetZIndex(newElement, -10);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            if (VisualTreeHelper.GetParent(newElement) is Grid container &&
                ElementCompositionPreview.GetElementVisual(container) is ContainerVisual containerVisual)
            {
                containerVisual.Children.InsertAtTop(sprite);
            }

            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, duration, easing);
                storyboard.Children.Add(fadeOut);
            }

            _transitionResources[newElement] = sprite;
            return true;
        }

        private static bool ApplyCheckerboardTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            int gridSize,
            double staggerMs)
        {
            var visual = ElementCompositionPreview.GetElementVisual(newElement);
            var compositor = visual.Compositor;

            newElement.Opacity = 1;

            // 1. Create a container for the mask cells
            var maskRoot = compositor.CreateContainerVisual();
            maskRoot.Size = new Vector2((float)width, (float)height);

            // 2. Create the proxy sprite for the new slide
            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)width, (float)height);

            var surface = compositor.CreateVisualSurface();
            surface.SourceVisual = visual;
            surface.SourceSize = new Vector2((float)width, (float)height);
            var surfaceBrush = compositor.CreateSurfaceBrush(surface);

            // 3. Create the mask surface from our mask visual tree
            var maskSurface = compositor.CreateVisualSurface();
            maskSurface.SourceVisual = maskRoot;
            maskSurface.SourceSize = new Vector2((float)width, (float)height);
            var maskSurfaceBrush = compositor.CreateSurfaceBrush(maskSurface);

            // 4. Create and animate cells in a structured checkerboard pattern
            float cellW = (float)width / gridSize;
            float cellH = (float)height / gridSize;

            // Cinematic Override: Checkerboard needs enough time for the wave to finish.
            // We calculate the absolute end time of the last cell reveal.
            double maxDiagonal = (gridSize - 1) * 2;

            // Sanity Check: Total reveal delay shouldn't exceed a reasonable window (e.g. 2s or 2.5x duration)
            double targetMaxDelay = Math.Min(2000, duration.TotalMilliseconds * 2.5);
            double effectiveStagger = Math.Min(staggerMs, targetMaxDelay / Math.Max(1, maxDiagonal));

            double maxDelay = (maxDiagonal * effectiveStagger) + (effectiveStagger / 2.0);
            var cellDuration = TimeSpan.FromMilliseconds(Math.Max(duration.TotalMilliseconds * 0.8, 400.0));
            var totalRequiredDuration = TimeSpan.FromMilliseconds(maxDelay + cellDuration.TotalMilliseconds);

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    var cell = compositor.CreateSpriteVisual();
                    cell.Size = new Vector2(cellW, cellH);
                    cell.Offset = new Vector3(col * cellW, row * cellH, 0);
                    cell.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

                    // Start hidden
                    cell.Scale = Vector3.Zero;
                    cell.CenterPoint = new Vector3(cellW / 2, cellH / 2, 0);
                    cell.Opacity = 0;

                    maskRoot.Children.InsertAtBottom(cell);

                    int diagonal = row + col;
                    bool isEven = (row + col) % 2 == 0;

                    // Use the sanitized effectiveStagger for the diagonal sweep
                    double baseDelay = diagonal * effectiveStagger;
                    if (!isEven) baseDelay += (effectiveStagger / 2.0);

                    var delay = TimeSpan.FromMilliseconds(baseDelay);

                    // Animate scale
                    var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
                    scaleAnim.Duration = cellDuration;
                    scaleAnim.InsertKeyFrame(1f, new Vector3(1, 1, 1), compositor.CreateCubicBezierEasingFunction(new Vector2(0.3f, 1.0f), new Vector2(0.5f, 1.0f)));
                    PlatformCompatibility.StartAnimation(cell, "Scale", scaleAnim, delay);

                    // Animate opacity
                    var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnim.Duration = cellDuration;
                    opacityAnim.InsertKeyFrame(1f, 1f);
                    PlatformCompatibility.StartAnimation(cell, "Opacity", opacityAnim, delay);
                }
            }

            // 5. Apply the mask
            var maskBrush = compositor.CreateMaskBrush();
            maskBrush.Source = surfaceBrush;
            maskBrush.Mask = maskSurfaceBrush;
            sprite.Brush = maskBrush;

            // 6. Inject proxy into container
            // Tier 2 Fix: Source element MUST be Opacity=1 to render to surface.
            newElement.Opacity = 1;
            Canvas.SetZIndex(newElement, -10);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            if (VisualTreeHelper.GetParent(newElement) is Grid container &&
                ElementCompositionPreview.GetElementVisual(container) is ContainerVisual containerVisual)
            {
                containerVisual.Children.InsertAtTop(sprite);
            }

            // 7. KEEP STORYBOARD ALIVE
            // IMPORTANT: Since Checkerboard uses detached Composition animations,
            // we must add a 'Ghost' animation to the XAML Storyboard that matches
            // the full total duration, otherwise the cleanup fires too early.
            // Tier 2 Fix: Must be 1 to 1 to preserve surface rendering.
            var storyboardHold = CreateOpacityAnimation(newElement, 1, 1, totalRequiredDuration, null);
            storyboard.Children.Add(storyboardHold);

            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, totalRequiredDuration, easing);
                storyboard.Children.Add(fadeOut);
            }

            _transitionResources[newElement] = sprite;
            return true;
        }

        private static bool ApplySpiralTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            int gridSize,
            double staggerMs)
        {
            var visual = ElementCompositionPreview.GetElementVisual(newElement);
            var compositor = visual.Compositor;

            newElement.Opacity = 1;

            // 1. Create a container for the mask cells
            var maskRoot = compositor.CreateContainerVisual();
            maskRoot.Size = new Vector2((float)width, (float)height);

            // 2. Create the proxy sprite for the new slide
            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)width, (float)height);

            var surface = compositor.CreateVisualSurface();
            surface.SourceVisual = visual;
            surface.SourceSize = new Vector2((float)width, (float)height);
            var surfaceBrush = compositor.CreateSurfaceBrush(surface);

            // 3. Create the mask surface from our mask visual tree
            var maskSurface = compositor.CreateVisualSurface();
            maskSurface.SourceVisual = maskRoot;
            maskSurface.SourceSize = new Vector2((float)width, (float)height);
            var maskSurfaceBrush = compositor.CreateSurfaceBrush(maskSurface);

            // 4. Generate spiral sequence (Outer-to-Inner walking)
            var spiralSequence = new List<(int row, int col)>();
            int rMin = 0, rMax = gridSize - 1;
            int cMin = 0, cMax = gridSize - 1;

            while (rMin <= rMax && cMin <= cMax)
            {
                // Top row
                for (int c = cMin; c <= cMax; c++) spiralSequence.Add((rMin, c));
                rMin++;
                // Right col
                for (int r = rMin; r <= rMax; r++) spiralSequence.Add((r, cMax));
                cMax--;
                // Bottom row
                if (rMin <= rMax)
                {
                    for (int c = cMax; c >= cMin; c--) spiralSequence.Add((rMax, c));
                    rMax--;
                }
                // Left col
                if (cMin <= cMax)
                {
                    for (int r = rMax; r >= rMin; r--) spiralSequence.Add((r, cMin));
                    cMin++;
                }
            }

            // Reverse for center-out effect (more cinematic)
            spiralSequence.Reverse();

            // 5. Calculate cinematic overrides
            // Sanity Check: Spiral is sequential (cell-by-cell), so it grows very fast.
            // We must cap the total reveal time to prevent the carousel from feeling 'defunct/hung'.
            double targetMaxDelay = Math.Min(2000, duration.TotalMilliseconds * 3.0);
            double cellStagger = Math.Min(staggerMs, targetMaxDelay / Math.Max(1, spiralSequence.Count));

            var cellDuration = TimeSpan.FromMilliseconds(Math.Max(duration.TotalMilliseconds * 0.8, 400.0));
            var totalRequiredDuration = TimeSpan.FromMilliseconds((spiralSequence.Count * cellStagger) + cellDuration.TotalMilliseconds);

            // 6. Create and animate spiral cells
            float cellW = (float)width / gridSize;
            float cellH = (float)height / gridSize;

            for (int i = 0; i < spiralSequence.Count; i++)
            {
                var (row, col) = spiralSequence[i];
                var cell = compositor.CreateSpriteVisual();
                cell.Size = new Vector2(cellW, cellH);
                cell.Offset = new Vector3(col * cellW, row * cellH, 0);
                cell.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

                cell.Scale = Vector3.Zero;
                cell.CenterPoint = new Vector3(cellW / 2, cellH / 2, 0);
                cell.Opacity = 0;

                maskRoot.Children.InsertAtBottom(cell);

                var delay = TimeSpan.FromMilliseconds(i * cellStagger);

                // Animate scale (Cubic-Out for pop)
                var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
                scaleAnim.Duration = cellDuration;
                scaleAnim.InsertKeyFrame(1f, new Vector3(1, 1, 1), compositor.CreateCubicBezierEasingFunction(new Vector2(0.3f, 1.0f), new Vector2(0.5f, 1.0f)));
                PlatformCompatibility.StartAnimation(cell, "Scale", scaleAnim, delay);

                // Animate opacity
                var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
                opacityAnim.Duration = cellDuration;
                opacityAnim.InsertKeyFrame(1f, 1f);
                PlatformCompatibility.StartAnimation(cell, "Opacity", opacityAnim, delay);
            }

            // 7. Apply the mask
            var maskBrush = compositor.CreateMaskBrush();
            maskBrush.Source = surfaceBrush;
            maskBrush.Mask = maskSurfaceBrush;
            sprite.Brush = maskBrush;

            // 8. Inject and Lifecycle management
            // Tier 2 Fix: Source element MUST be Opacity=1 to render to surface.
            newElement.Opacity = 1;
            Canvas.SetZIndex(newElement, -10);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            // 8. Inject and Lifecycle management
            // Tier 2 Fix: Source element MUST be Opacity=1 to render to surface.
            newElement.Opacity = 1;
            Canvas.SetZIndex(newElement, -10);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            if (VisualTreeHelper.GetParent(newElement) is Grid container &&
                ElementCompositionPreview.GetElementVisual(container) is ContainerVisual containerVisual)
            {
                containerVisual.Children.InsertAtTop(sprite);
            }

            // Tier 2 Fix: Hold must be 1 to 1 to preserve surface.
            var storyboardHold = CreateOpacityAnimation(newElement, 1, 1, totalRequiredDuration, null);
            storyboard.Children.Add(storyboardHold);

            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, totalRequiredDuration, easing);
                storyboard.Children.Add(fadeOut);
            }

            _transitionResources[newElement] = sprite;
            return true;
        }

        private static bool ApplyMatrixRainTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            int gridSize,
            double staggerMs)
        {
            var visual = ElementCompositionPreview.GetElementVisual(newElement);
            var compositor = visual.Compositor;

            newElement.Opacity = 1;

            var maskRoot = compositor.CreateContainerVisual();
            maskRoot.Size = new Vector2((float)width, (float)height);

            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)width, (float)height);

            var surface = compositor.CreateVisualSurface();
            surface.SourceVisual = visual;
            surface.SourceSize = new Vector2((float)width, (float)height);
            var surfaceBrush = compositor.CreateSurfaceBrush(surface);

            var maskSurface = compositor.CreateVisualSurface();
            maskSurface.SourceVisual = maskRoot;
            maskSurface.SourceSize = new Vector2((float)width, (float)height);
            var maskSurfaceBrush = compositor.CreateSurfaceBrush(maskSurface);

            float cellW = (float)width / gridSize;
            float cellH = (float)height / gridSize;

            double maxDelayFound = 0;
            // Cinematic Override: Slower pop-in for better visibility
            var cellDuration = TimeSpan.FromMilliseconds(400);

            for (int col = 0; col < gridSize; col++)
            {
                // Each column starts at a random time, up to 125% of the base duration
                double colStartDelay = _random.Next(0, (int)(duration.TotalMilliseconds * 1.25));

                for (int row = 0; row < gridSize; row++)
                {
                    var cell = compositor.CreateSpriteVisual();
                    cell.Size = new Vector2(cellW, cellH);
                    cell.Offset = new Vector3(col * cellW, row * cellH, 0);
                    cell.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

                    cell.Scale = new Vector3(1, 0, 1);
                    cell.CenterPoint = new Vector3(cellW / 2, 0, 0);
                    cell.Opacity = 0;

                    maskRoot.Children.InsertAtBottom(cell);

                    // Use user-defined staggerMs for the 'falling' speed
                    double delayMs = colStartDelay + (row * staggerMs);
                    maxDelayFound = Math.Max(maxDelayFound, delayMs);
                    var delay = TimeSpan.FromMilliseconds(delayMs);

                    var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
                    scaleAnim.Duration = cellDuration;
                    scaleAnim.InsertKeyFrame(1f, new Vector3(1, 1, 1), compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.8f), new Vector2(0.4f, 1.0f)));
                    PlatformCompatibility.StartAnimation(cell, "Scale", scaleAnim, delay);

                    var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnim.Duration = cellDuration;
                    opacityAnim.InsertKeyFrame(1f, 1f);
                    PlatformCompatibility.StartAnimation(cell, "Opacity", opacityAnim, delay);
                }
            }

            // Sanity Check: Ensure Matrix Rain finishes in a reasonable time
            double targetMaxDelay = Math.Min(2500, duration.TotalMilliseconds * 3.5);
            double maxRevealTime = maxDelayFound + cellDuration.TotalMilliseconds;

            // If it exceeds target, we scale all delays down
            if (maxRevealTime > targetMaxDelay && maxDelayFound > 0)
            {
                // Since animations already started, we just adjust the 'hold' duration
                // for the cleanup to fire sooner, though it's better to prevent this
                // at the start. For now, we cap the hold at 3 seconds max.
                maxRevealTime = Math.Min(maxRevealTime, 3000);
            }

            var totalRequiredDuration = TimeSpan.FromMilliseconds(maxRevealTime);

            var maskBrush = compositor.CreateMaskBrush();
            maskBrush.Source = surfaceBrush;
            maskBrush.Mask = maskSurfaceBrush;
            sprite.Brush = maskBrush;

            // 4. Inject and Lifecycle management
            // Tier 2 Fix: Source element MUST be Opacity=1 to render to surface.
            newElement.Opacity = 1;
            Canvas.SetZIndex(newElement, -10);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            if (VisualTreeHelper.GetParent(newElement) is Grid containerView &&
                ElementCompositionPreview.GetElementVisual(containerView) is ContainerVisual containerVisual)
            {
                containerVisual.Children.InsertAtTop(sprite);
            }

            // Tier 2 Fix: Hold must be 1 to 1 to preserve surface.
            var storyboardHold = CreateOpacityAnimation(newElement, 1, 1, totalRequiredDuration, null);
            storyboard.Children.Add(storyboardHold);

            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, totalRequiredDuration, easing);
                storyboard.Children.Add(fadeOut);
            }

            _transitionResources[newElement] = sprite;
            return true;
        }

        private static bool ApplyWormholeTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height)
        {
            var visual = ElementCompositionPreview.GetElementVisual(newElement);
            var compositor = visual.Compositor;

            newElement.Opacity = 1;

            var maskRoot = compositor.CreateContainerVisual();
            maskRoot.Size = new Vector2((float)width, (float)height);

            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)width, (float)height);

            var surface = compositor.CreateVisualSurface();
            surface.SourceVisual = visual;
            surface.SourceSize = new Vector2((float)width, (float)height);
            var surfaceBrush = compositor.CreateSurfaceBrush(surface);

            float centerX = (float)width / 2;
            float centerY = (float)height / 2;

            // Use a denser grid for smoother circular motion
            int ringSteps = 10;
            int radialSteps = 24;
            double maxDelayFound = 0;

            // Wormhole specific: Slower for drama
            var cellDuration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * 0.8);

            for (int ring = 0; ring < ringSteps; ring++)
            {
                // Radius increases ring by ring
                float r = (ring + 0.5f) * ((float)Math.Max(width, height) / ringSteps);

                for (int step = 0; step < radialSteps; step++)
                {
                    float angle = (float)(step * 2 * Math.PI / radialSteps);
                    float x = centerX + r * (float)Math.Cos(angle);
                    float y = centerY + r * (float)Math.Sin(angle);

                    // Cells are sized to be slightly overlapping
                    float cellSize = (r * (float)Math.PI * 2 / radialSteps) * 1.5f;
                    cellSize = Math.Max(cellSize, 40f); // Minimum size

                    var cell = compositor.CreateSpriteVisual();
                    cell.Size = new Vector2(cellSize, cellSize);
                    cell.Offset = new Vector3(x - cellSize / 2, y - cellSize / 2, 0);
                    cell.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

                    cell.CenterPoint = new Vector3(cellSize / 2, cellSize / 2, 0);
                    cell.Scale = Vector3.Zero;
                    cell.Opacity = 0;

                    maskRoot.Children.InsertAtBottom(cell);

                    // Spiral Stagger: Combinatorial delay based on outward distance + angular rotation
                    // This creates the "outward swirl" effect
                    double normAngle = (double)step / radialSteps;
                    double delayMs = (ring * 80.0) + (normAngle * 400.0);
                    maxDelayFound = Math.Max(maxDelayFound, delayMs);
                    var delay = TimeSpan.FromMilliseconds(delayMs);

                    // 1. Scale with spaghettification (radially stretched entrance)
                    var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
                    scaleAnim.Duration = cellDuration;
                    // Start stretched out
                    scaleAnim.InsertKeyFrame(0f, new Vector3(0.1f, 4f, 1f));
                    scaleAnim.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
                    PlatformCompatibility.StartAnimation(cell, "Scale", scaleAnim, delay);

                    // 2. High-speed Vortex Rotation (720 degrees)
                    var rotationAnim = compositor.CreateScalarKeyFrameAnimation();
                    rotationAnim.Duration = cellDuration;
                    rotationAnim.InsertKeyFrame(1f, PlatformCompatibility.DegreesToRadians(720f));
                    PlatformCompatibility.StartAnimation(cell, "RotationAngle", rotationAnim, delay);

                    // 3. Opacity
                    var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnim.Duration = cellDuration;
                    opacityAnim.InsertKeyFrame(1f, 1f);
                    PlatformCompatibility.StartAnimation(cell, "Opacity", opacityAnim, delay);
                }
            }

            // Ensure the mask fully reaches opaque at the end (avoid residual center artifact).
            var finalFill = compositor.CreateSpriteVisual();
            finalFill.Size = new Vector2((float)width, (float)height);
            finalFill.Offset = Vector3.Zero;
            finalFill.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
            finalFill.Opacity = 0;
            maskRoot.Children.InsertAtTop(finalFill);

            var maskSurface = compositor.CreateVisualSurface();
            maskSurface.SourceVisual = maskRoot;
            maskSurface.SourceSize = new Vector2((float)width, (float)height);

            var maskBrush = compositor.CreateMaskBrush();
            maskBrush.Source = surfaceBrush;
            maskBrush.Mask = compositor.CreateSurfaceBrush(maskSurface);
            sprite.Brush = maskBrush;

            // Tier 2 Fix: Source element MUST be Opacity=1 to render to surface.
            newElement.Opacity = 1;
            Canvas.SetZIndex(newElement, -10);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            if (VisualTreeHelper.GetParent(newElement) is Grid containerView &&
                ElementCompositionPreview.GetElementVisual(containerView) is ContainerVisual containerVisual)
            {
                containerVisual.Children.InsertAtTop(sprite);
            }

            var totalTime = TimeSpan.FromMilliseconds(maxDelayFound + cellDuration.TotalMilliseconds);
            var fillDurationMs = Math.Max(80.0, cellDuration.TotalMilliseconds * 0.4);
            var fillDelayMs = Math.Max(0.0, totalTime.TotalMilliseconds - fillDurationMs);
            var fillDuration = TimeSpan.FromMilliseconds(fillDurationMs);
            var fillDelay = TimeSpan.FromMilliseconds(fillDelayMs);
            var fillOpacity = compositor.CreateScalarKeyFrameAnimation();
            fillOpacity.Duration = fillDuration;
            fillOpacity.InsertKeyFrame(1f, 1f);
            PlatformCompatibility.StartAnimation(finalFill, "Opacity", fillOpacity, fillDelay);
            // Tier 2 Fix: Hold must be 1 to 1 to preserve surface.
            var hold = CreateOpacityAnimation(newElement, 1, 1, totalTime, null);
            storyboard.Children.Add(hold);

            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, totalTime, easing);
                storyboard.Children.Add(fadeOut);
            }

            _transitionResources[newElement] = sprite;
            return true;
        }

        private static bool ApplyDissolveTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            double density)
        {
            // 1. Calculate Performance Budget (Sanity Limit)
            // Gut feeling: ~5,000 elements max for 400ms duration.
            // We scale the budget slightly with duration (more time = we can afford more setup overhead).
            double budgetFactor = Math.Clamp(duration.TotalMilliseconds / 400.0, 0.5, 2.0);
            int cellBudget = (int)(5000 * budgetFactor);
            int maxGridSize = (int)Math.Sqrt(cellBudget); // ~70x70

            // 2. Map density to grid size
            // Base range 20-50, but capped by performance budget
            int gridSize = (int)(20 + (density * 30));
            gridSize = Math.Min(gridSize, maxGridSize);

            double totalStaggerMs = duration.TotalMilliseconds * 0.7;
            double staggerMs = totalStaggerMs / (gridSize * gridSize);

            return ApplyRandomGridTransition(oldElement, newElement, duration, easing, storyboard, width, height, gridSize, staggerMs);
        }

        private static bool ApplyPixelateTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            int pixelateSize)
        {
            // 1. Performance Guard: Don't exceed ~5,000 elements.
            // We calculate the minimum pixel size needed to stay within budget for this specific area.
            double budgetFactor = Math.Clamp(duration.TotalMilliseconds / 400.0, 0.5, 2.0);
            int cellBudget = (int)(5000 * budgetFactor);

            // For a grid where gridSize = width / P, total cells = gridSize * gridSize.
            // We want gridSize * gridSize <= cellBudget.
            int maxGridSize = (int)Math.Sqrt(cellBudget);

            // 2. Calculate requested gridSize
            int gridSize = pixelateSize > 0 ? (int)(width / pixelateSize) : 10;

            // 3. Apply the Sanity Limit
            // If the user requested very small pixels on a large screen, the grid size
            // will be capped here, effectively 'keeping the pixelsize up' as requested.
            gridSize = Math.Clamp(gridSize, 2, maxGridSize);

            double halfMs = duration.TotalMilliseconds / 2;
            var halfDuration = TimeSpan.FromMilliseconds(halfMs);
            double totalStaggerMs = halfMs * 0.6;
            double staggerMs = totalStaggerMs / (gridSize * gridSize);

            if (oldElement == null)
            {
                return ApplyRandomGridTransition(oldElement, newElement, duration, easing, storyboard, width, height, gridSize, staggerMs);
            }

            if (VisualTreeHelper.GetParent(newElement) is not Grid container ||
                ElementCompositionPreview.GetElementVisual(container) is not ContainerVisual containerVisual)
            {
                return false;
            }

            var oldVisual = ElementCompositionPreview.GetElementVisual(oldElement);
            var newVisual = ElementCompositionPreview.GetElementVisual(newElement);
            var compositor = newVisual.Compositor;

            newElement.Opacity = 1;
            oldElement.Opacity = 1;
            Canvas.SetZIndex(newElement, -10);
            Canvas.SetZIndex(oldElement, -10);

            var oldMaskRoot = compositor.CreateContainerVisual();
            oldMaskRoot.Size = new Vector2((float)width, (float)height);

            var newMaskRoot = compositor.CreateContainerVisual();
            newMaskRoot.Size = new Vector2((float)width, (float)height);

            var oldSprite = compositor.CreateSpriteVisual();
            oldSprite.Size = new Vector2((float)width, (float)height);

            var newSprite = compositor.CreateSpriteVisual();
            newSprite.Size = new Vector2((float)width, (float)height);

            var oldSurface = compositor.CreateVisualSurface();
            oldSurface.SourceVisual = oldVisual;
            oldSurface.SourceSize = new Vector2((float)width, (float)height);
            var oldSurfaceBrush = compositor.CreateSurfaceBrush(oldSurface);

            var newSurface = compositor.CreateVisualSurface();
            newSurface.SourceVisual = newVisual;
            newSurface.SourceSize = new Vector2((float)width, (float)height);
            var newSurfaceBrush = compositor.CreateSurfaceBrush(newSurface);

            var oldMaskSurface = compositor.CreateVisualSurface();
            oldMaskSurface.SourceVisual = oldMaskRoot;
            oldMaskSurface.SourceSize = new Vector2((float)width, (float)height);
            var oldMaskBrushSurface = compositor.CreateSurfaceBrush(oldMaskSurface);

            var newMaskSurface = compositor.CreateVisualSurface();
            newMaskSurface.SourceVisual = newMaskRoot;
            newMaskSurface.SourceSize = new Vector2((float)width, (float)height);
            var newMaskBrushSurface = compositor.CreateSurfaceBrush(newMaskSurface);

            var oldMaskBrush = compositor.CreateMaskBrush();
            oldMaskBrush.Source = oldSurfaceBrush;
            oldMaskBrush.Mask = oldMaskBrushSurface;
            oldSprite.Brush = oldMaskBrush;

            var newMaskBrush = compositor.CreateMaskBrush();
            newMaskBrush.Source = newSurfaceBrush;
            newMaskBrush.Mask = newMaskBrushSurface;
            newSprite.Brush = newMaskBrush;

            float cellW = (float)width / gridSize;
            float cellH = (float)height / gridSize;
            var totalCells = gridSize * gridSize;
            var order = Enumerable.Range(0, totalCells).OrderBy(_ => _random.Next()).ToArray();
            var cellDuration = TimeSpan.FromMilliseconds(halfMs * 0.6);
            double maxDelay = 0;

            for (int i = 0; i < totalCells; i++)
            {
                int index = order[i];
                int r = index / gridSize;
                int c = index % gridSize;

                var oldCell = compositor.CreateSpriteVisual();
                oldCell.Size = new Vector2(cellW, cellH);
                oldCell.Offset = new Vector3(c * cellW, r * cellH, 0);
                oldCell.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
                oldCell.Opacity = 1;
                oldMaskRoot.Children.InsertAtBottom(oldCell);

                var newCell = compositor.CreateSpriteVisual();
                newCell.Size = new Vector2(cellW, cellH);
                newCell.Offset = new Vector3(c * cellW, r * cellH, 0);
                newCell.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
                newCell.Opacity = 0;
                newMaskRoot.Children.InsertAtBottom(newCell);

                double delayMs = i * staggerMs;
                maxDelay = Math.Max(maxDelay, delayMs);
                var delay = TimeSpan.FromMilliseconds(delayMs);

                var oldOpacityAnim = compositor.CreateScalarKeyFrameAnimation();
                oldOpacityAnim.Duration = cellDuration;
                oldOpacityAnim.InsertKeyFrame(1f, 0f);
                PlatformCompatibility.StartAnimation(oldCell, "Opacity", oldOpacityAnim, delay);

                var newOpacityAnim = compositor.CreateScalarKeyFrameAnimation();
                newOpacityAnim.Duration = cellDuration;
                newOpacityAnim.InsertKeyFrame(1f, 1f);
                PlatformCompatibility.StartAnimation(newCell, "Opacity", newOpacityAnim, delay + halfDuration);
            }

            var totalTime = TimeSpan.FromMilliseconds(maxDelay + cellDuration.TotalMilliseconds + halfMs);

            containerVisual.Children.InsertAtTop(oldSprite);
            containerVisual.Children.InsertAtTop(newSprite);

            var hold = CreateOpacityAnimation(newElement, 1, 1, totalTime, null);
            storyboard.Children.Add(hold);

            _transitionResources[newElement] = new PixelateSpriteResources(oldSprite, newSprite, container);
            return true;
        }

        private static bool ApplyRandomGridTransition(
            UIElement? oldElement,
            UIElement newElement,
            TimeSpan duration,
            EasingFunctionBase easing,
            Storyboard storyboard,
            double width,
            double height,
            int gridSize,
            double staggerMs)
        {
            var visual = ElementCompositionPreview.GetElementVisual(newElement);
            var compositor = visual.Compositor;

            newElement.Opacity = 1;

            var maskRoot = compositor.CreateContainerVisual();
            maskRoot.Size = new Vector2((float)width, (float)height);

            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)width, (float)height);

            var surface = compositor.CreateVisualSurface();
            surface.SourceVisual = visual;
            surface.SourceSize = new Vector2((float)width, (float)height);
            var surfaceBrush = compositor.CreateSurfaceBrush(surface);

            var maskSurface = compositor.CreateVisualSurface();
            maskSurface.SourceVisual = maskRoot;
            maskSurface.SourceSize = new Vector2((float)width, (float)height);
            var maskSurfaceBrush = compositor.CreateSurfaceBrush(maskSurface);

            float cellW = (float)width / gridSize;
            float cellH = (float)height / gridSize;
            var totalCells = gridSize * gridSize;
            var order = Enumerable.Range(0, totalCells).OrderBy(_ => _random.Next()).ToArray();

            var cellDuration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * 0.4);
            double maxDelay = 0;

            for (int i = 0; i < totalCells; i++)
            {
                int index = order[i];
                int r = index / gridSize;
                int c = index % gridSize;

                var cell = compositor.CreateSpriteVisual();
                cell.Size = new Vector2(cellW, cellH);
                cell.Offset = new Vector3(c * cellW, r * cellH, 0);
                cell.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

                cell.Opacity = 0;
                maskRoot.Children.InsertAtBottom(cell);

                double delayMs = i * staggerMs;
                maxDelay = Math.Max(maxDelay, delayMs);
                var delay = TimeSpan.FromMilliseconds(delayMs);

                var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
                opacityAnim.Duration = cellDuration;
                opacityAnim.InsertKeyFrame(1f, 1f);
                PlatformCompatibility.StartAnimation(cell, "Opacity", opacityAnim, delay);
            }

            var totalTime = TimeSpan.FromMilliseconds(maxDelay + cellDuration.TotalMilliseconds);

            var maskBrush = compositor.CreateMaskBrush();
            maskBrush.Source = surfaceBrush;
            maskBrush.Mask = maskSurfaceBrush;
            sprite.Brush = maskBrush;

            if (VisualTreeHelper.GetParent(newElement) is not Grid container ||
                ElementCompositionPreview.GetElementVisual(container) is not ContainerVisual containerVisual)
            {
                return false;
            }

            newElement.Opacity = 0;
            Canvas.SetZIndex(newElement, -1);
            if (oldElement != null) Canvas.SetZIndex(oldElement, 0);

            containerVisual.Children.InsertAtTop(sprite);

            var storyboardHold = CreateOpacityAnimation(newElement, 0, 0, totalTime, null);
            storyboard.Children.Add(storyboardHold);

            if (oldElement != null)
            {
                var fadeOut = CreateOpacityAnimation(oldElement, 1, 0, totalTime, easing);
                storyboard.Children.Add(fadeOut);
            }

            _transitionResources[newElement] = sprite;
            return true;
        }

        private static DoubleAnimation CreateOpacityAnimation(UIElement target, double from, double to, TimeSpan duration, EasingFunctionBase? easing)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                EasingFunction = easing
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, "Opacity");
            return animation;
        }

        private static DoubleAnimation CreateDoubleAnimation(UIElement target, string propertyPath, double from, double to, TimeSpan duration, EasingFunctionBase? easing)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                EasingFunction = easing
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, propertyPath);
            return animation;
        }

        private static CubicEase CreateEasing(EasingMode mode)
        {
            return new CubicEase { EasingMode = mode };
        }

        private static void ResetElementTransforms(UIElement? element)
        {
            if (element == null) return;

            element.Opacity = 1;
            Canvas.SetZIndex(element, 0); // Reset z-order
            if (element.RenderTransform is CompositeTransform transform)
            {
                FloweryAnimationHelpers.ResetTransform(transform);
            }
        }

        /// <summary>
        /// Linear interpolation between two values.
        /// </summary>
        private static double Lerp(double start, double end, double t)
        {
            return start + (end - start) * t;
        }

        /// <summary>
        /// Manually applies easing function to a normalized t value (0 to 1).
        /// Since ObjectAnimationUsingKeyFrames doesn't support easing, we calculate eased values ourselves.
        /// </summary>
        private static double ApplyEasing(double t, EasingFunctionBase? easing)
        {
            if (easing == null) return t;

            // CubicEase implementation
            if (easing is CubicEase cubicEase)
            {
                return cubicEase.EasingMode switch
                {
                    EasingMode.EaseIn => t * t * t,
                    EasingMode.EaseOut => 1 - Math.Pow(1 - t, 3),
                    EasingMode.EaseInOut => t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2,
                    _ => t
                };
            }

            // Fallback to linear for other easing types
            return t;
        }

        #endregion
    }

    #endregion
}

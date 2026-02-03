using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Flowery.Helpers
{
    /// <summary>
    /// Attached properties for applying slide effects to any UIElement.
    /// Usage in XAML: &lt;Image services:FlowerySlideEffects.Effect="PanAndZoom" /&gt;
    /// </summary>
    public static class FlowerySlideEffects
    {
        // Track active storyboards per element
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UIElement, Storyboard> _storyboards = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UIElement, UIElement> _effectTargets = new();

        #region Effect Attached Property

        public static readonly DependencyProperty EffectProperty =
            DependencyProperty.RegisterAttached(
                "Effect",
                typeof(FlowerySlideEffect),
                typeof(FlowerySlideEffects),
                new PropertyMetadata(FlowerySlideEffect.None, OnEffectChanged));

        public static FlowerySlideEffect GetEffect(UIElement element)
        {
            return (FlowerySlideEffect)element.GetValue(EffectProperty);
        }

        public static void SetEffect(UIElement element, FlowerySlideEffect value)
        {
            element.SetValue(EffectProperty, value);
        }

        #endregion

        #region Duration Attached Property

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached(
                "Duration",
                typeof(double),
                typeof(FlowerySlideEffects),
                new PropertyMetadata(3.0, OnEffectParameterChanged));

        /// <summary>Gets the effect duration in seconds.</summary>
        public static double GetDuration(UIElement element)
        {
            return (double)element.GetValue(DurationProperty);
        }

        /// <summary>Sets the effect duration in seconds.</summary>
        public static void SetDuration(UIElement element, double value)
        {
            element.SetValue(DurationProperty, value);
        }

        #endregion

        #region ZoomIntensity Attached Property

        public static readonly DependencyProperty ZoomIntensityProperty =
            DependencyProperty.RegisterAttached(
                "ZoomIntensity",
                typeof(double),
                typeof(FlowerySlideEffects),
                new PropertyMetadata(0.15, OnEffectParameterChanged));

        public static double GetZoomIntensity(UIElement element)
        {
            return (double)element.GetValue(ZoomIntensityProperty);
        }

        public static void SetZoomIntensity(UIElement element, double value)
        {
            element.SetValue(ZoomIntensityProperty, value);
        }

        #endregion

        #region PanDistance Attached Property

        public static readonly DependencyProperty PanDistanceProperty =
            DependencyProperty.RegisterAttached(
                "PanDistance",
                typeof(double),
                typeof(FlowerySlideEffects),
                new PropertyMetadata(50.0, OnEffectParameterChanged));

        public static double GetPanDistance(UIElement element)
        {
            return (double)element.GetValue(PanDistanceProperty);
        }

        public static void SetPanDistance(UIElement element, double value)
        {
            element.SetValue(PanDistanceProperty, value);
        }

        #endregion

        #region PulseIntensity Attached Property

        public static readonly DependencyProperty PulseIntensityProperty =
            DependencyProperty.RegisterAttached(
                "PulseIntensity",
                typeof(double),
                typeof(FlowerySlideEffects),
                new PropertyMetadata(0.08, OnEffectParameterChanged));

        public static double GetPulseIntensity(UIElement element)
        {
            return (double)element.GetValue(PulseIntensityProperty);
        }

        public static void SetPulseIntensity(UIElement element, double value)
        {
            element.SetValue(PulseIntensityProperty, value);
        }

        #endregion

        #region PanAndZoomLockZoom Attached Property

        public static readonly DependencyProperty PanAndZoomLockZoomProperty =
            DependencyProperty.RegisterAttached(
                "PanAndZoomLockZoom",
                typeof(bool),
                typeof(FlowerySlideEffects),
                new PropertyMetadata(false, OnEffectParameterChanged));

        public static bool GetPanAndZoomLockZoom(UIElement element)
        {
            return (bool)element.GetValue(PanAndZoomLockZoomProperty);
        }

        public static void SetPanAndZoomLockZoom(UIElement element, bool value)
        {
            element.SetValue(PanAndZoomLockZoomProperty, value);
        }

        #endregion

        #region PanAndZoomPanSpeed Attached Property

        public static readonly DependencyProperty PanAndZoomPanSpeedProperty =
            DependencyProperty.RegisterAttached(
                "PanAndZoomPanSpeed",
                typeof(double),
                typeof(FlowerySlideEffects),
                new PropertyMetadata(4.0, OnEffectParameterChanged));

        public static double GetPanAndZoomPanSpeed(UIElement element)
        {
            return (double)element.GetValue(PanAndZoomPanSpeedProperty);
        }

        public static void SetPanAndZoomPanSpeed(UIElement element, double value)
        {
            element.SetValue(PanAndZoomPanSpeedProperty, Math.Max(0.0, value));
        }

        #endregion

        #region AutoStart Attached Property

        public static readonly DependencyProperty AutoStartProperty =
            DependencyProperty.RegisterAttached(
                "AutoStart",
                typeof(bool),
                typeof(FlowerySlideEffects),
                new PropertyMetadata(true, OnEffectChanged));

        /// <summary>Gets whether the effect starts automatically when the element loads. Default true.</summary>
        public static bool GetAutoStart(UIElement element)
        {
            return (bool)element.GetValue(AutoStartProperty);
        }

        /// <summary>Sets whether the effect starts automatically when the element loads.</summary>
        public static void SetAutoStart(UIElement element, bool value)
        {
            element.SetValue(AutoStartProperty, value);
        }

        #endregion

        #region Effect Management

        private static void OnEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
            {
                return;
            }

            // Stop any existing effect
            StopEffect(element);

            FlowerySlideEffect effect = GetEffect(element);
            if (effect == FlowerySlideEffect.None)
            {
                return;
            }

            if (GetAutoStart(element) && element is FrameworkElement frameworkElement)
            {
                // Start effect immediately if element is loaded, otherwise wait for Loaded event
                if (frameworkElement.IsLoaded)
                {
                    _ = StartEffect(element);
                }
                else
                {
                    frameworkElement.Loaded += OnElementLoaded;
                }
            }
        }

        private static void OnEffectParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
            {
                return;
            }

            // Restart effect if it's running
            if (GetEffect(element) != FlowerySlideEffect.None && GetAutoStart(element))
            {
                StopEffect(element);
                _ = StartEffect(element);
            }
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            element.Loaded -= OnElementLoaded;
            _ = StartEffect(element);

            // Clean up when element is unloaded
            element.Unloaded += OnElementUnloaded;
        }

        private static void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            element.Unloaded -= OnElementUnloaded;
            StopEffect(element);
        }

        /// <summary>
        /// Starts the slide effect on the specified element.
        /// Returns the actual effect applied (may differ from configured effect for Drift which picks random direction).
        /// </summary>
        public static FlowerySlideEffect StartEffect(UIElement element)
        {
            FlowerySlideEffect effect = GetEffect(element);
            if (effect == FlowerySlideEffect.None)
            {
                return FlowerySlideEffect.None;
            }

            // Stop any existing effect first
            StopEffect(element);

            UIElement target = ResolveEffectTarget(element);
            _effectTargets.AddOrUpdate(element, target);
            CompositeTransform transform = FloweryAnimationHelpers.EnsureCompositeTransform(target);
            TimeSpan duration = TimeSpan.FromSeconds(Math.Max(0.5, GetDuration(element)));
            Storyboard storyboard = FloweryAnimationHelpers.CreateStoryboard();

            FlowerySlideEffectParams effectParams = new FlowerySlideEffectParams
            {
                ZoomIntensity = GetZoomIntensity(element),
                PanDistance = GetPanDistance(element),
                PulseIntensity = GetPulseIntensity(element),
                PanAndZoomLockZoom = GetPanAndZoomLockZoom(element),
                PanAndZoomPanSpeed = GetPanAndZoomPanSpeed(element)
            };

            FlowerySlideEffect resolvedEffect = FloweryAnimationHelpers.ApplySlideEffect(storyboard, transform, effect, duration, effectParams, target);

            _storyboards.AddOrUpdate(element, storyboard);
            storyboard.Begin();

            return resolvedEffect;
        }

        /// <summary>
        /// Stops and clears the slide effect on the specified element.
        /// </summary>
        /// <param name="element">The element to stop the effect on.</param>
        /// <param name="preserveTransform">If true, the transform is preserved at its current state (for smooth transitions).</param>
        public static void StopEffect(UIElement element, bool preserveTransform = false)
        {
            UIElement target = element;
            if (_effectTargets.TryGetValue(element, out UIElement? storedTarget))
            {
                target = storedTarget;
            }

            // Capture current transform values BEFORE stopping storyboard
            // (Stop() releases animated values back to original state)
            double scaleX = 1, scaleY = 1, translateX = 0, translateY = 0, rotation = 0;
            if (preserveTransform && target.RenderTransform is CompositeTransform currentTransform)
            {
                scaleX = currentTransform.ScaleX;
                scaleY = currentTransform.ScaleY;
                translateX = currentTransform.TranslateX;
                translateY = currentTransform.TranslateY;
                rotation = currentTransform.Rotation;
            }

            if (_storyboards.TryGetValue(element, out Storyboard? storyboard))
            {
                FloweryAnimationHelpers.StopPanAndZoom(storyboard, preserveTransform);
                FloweryAnimationHelpers.StopAndClear(ref storyboard);
                _ = _storyboards.Remove(element);
            }

            _ = _effectTargets.Remove(element);

            // Either reset transform or restore captured values
            if (target.RenderTransform is CompositeTransform transform)
            {
                if (preserveTransform)
                {
                    // Restore captured values after Stop() released them
                    transform.ScaleX = scaleX;
                    transform.ScaleY = scaleY;
                    transform.TranslateX = translateX;
                    transform.TranslateY = translateY;
                    transform.Rotation = rotation;
                }
                else
                {
                    FloweryAnimationHelpers.ResetTransform(transform);
                }
            }

            if (!preserveTransform)
            {
                FloweryAnimationHelpers.RestorePanAndZoomSizingSnapshot(target);
            }
        }

        /// <summary>
        /// Stops the slide effect while preserving the current transform state.
        /// Useful when transitioning between slides to avoid visual snapping.
        /// </summary>
        public static void StopEffectPreservingTransform(UIElement element)
        {
            StopEffect(element, preserveTransform: true);
        }

        /// <summary>
        /// Resets the transform on an element's effect target.
        /// Use this to ensure a slide is clean before applying new effects.
        /// Resets transforms at all levels of the hierarchy.
        /// </summary>
        public static void ResetSlideTransform(UIElement element)
        {
            // Reset transform on the element itself
            ResetElementTransform(element);

            // Also reset on the resolved target (in case of nested structure)
            UIElement target = ResolveEffectTarget(element);
            if (target != element)
            {
                ResetElementTransform(target);
            }

            FloweryAnimationHelpers.RestorePanAndZoomSizingSnapshot(target);
        }

        private static void ResetElementTransform(UIElement element)
        {
            if (element.RenderTransform is CompositeTransform existing)
            {
                FloweryAnimationHelpers.ResetTransform(existing);
            }
            else
            {
                // Force a fresh transform to ensure clean state
                element.RenderTransform = new CompositeTransform();
            }
        }

        #endregion

        /// <summary>
        /// Resolves the actual target element for effects (finds nested Image inside Border/Panel).
        /// Recursively unwraps containers until reaching the actual content element.
        /// </summary>
        public static UIElement ResolveEffectTarget(UIElement element)
        {
            UIElement current = element;
            UIElement? next;

            // Keep unwrapping until we reach an element that doesn't unwrap further
            while ((next = TryUnwrap(current)) != current)
            {
                current = next;
            }

            return current;

            static UIElement TryUnwrap(UIElement el)
            {
                return el switch
                {
                    Border { Child: UIElement borderChild } => borderChild,
                    ContentControl { Content: UIElement content } => content,
                    ContentPresenter { Content: UIElement presenterContent } => presenterContent,
                    Panel { Children: [var onlyChild] } => onlyChild,
                    _ => el
                };
            }
        }

    }
}

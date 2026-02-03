using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Flowery.Effects
{
    /// <summary>
    /// Reveals an element with fade-in and slide/scale animation when it is loaded.
    /// Works on any <see cref="FrameworkElement"/> via attached properties.
    /// </summary>
    public static class RevealBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(RevealBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(RevealMode),
                typeof(RevealBehavior),
                new PropertyMetadata(RevealMode.FadeReveal));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached(
                "Duration",
                typeof(TimeSpan),
                typeof(RevealBehavior),
                new PropertyMetadata(TimeSpan.FromMilliseconds(500)));

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.RegisterAttached(
                "Direction",
                typeof(RevealDirection),
                typeof(RevealBehavior),
                new PropertyMetadata(RevealDirection.Bottom));

        public static readonly DependencyProperty DistanceProperty =
            DependencyProperty.RegisterAttached(
                "Distance",
                typeof(double),
                typeof(RevealBehavior),
                new PropertyMetadata(30d));

        /// <summary>
        /// When true, the reveal animation will not auto-trigger on load.
        /// Use <see cref="TriggerReveal"/> to manually start the animation.
        /// </summary>
        public static readonly DependencyProperty ManualTriggerOnlyProperty =
            DependencyProperty.RegisterAttached(
                "ManualTriggerOnly",
                typeof(bool),
                typeof(RevealBehavior),
                new PropertyMetadata(false));

        private static readonly DependencyProperty ActiveStoryboardProperty =
            DependencyProperty.RegisterAttached(
                "ActiveStoryboard",
                typeof(Storyboard),
                typeof(RevealBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static RevealMode GetMode(DependencyObject element) => (RevealMode)element.GetValue(ModeProperty);
        public static void SetMode(DependencyObject element, RevealMode value) => element.SetValue(ModeProperty, value);

        public static TimeSpan GetDuration(DependencyObject element) => (TimeSpan)element.GetValue(DurationProperty);
        public static void SetDuration(DependencyObject element, TimeSpan value) => element.SetValue(DurationProperty, value);

        public static RevealDirection GetDirection(DependencyObject element) => (RevealDirection)element.GetValue(DirectionProperty);
        public static void SetDirection(DependencyObject element, RevealDirection value) => element.SetValue(DirectionProperty, value);

        public static double GetDistance(DependencyObject element) => (double)element.GetValue(DistanceProperty);
        public static void SetDistance(DependencyObject element, double value) => element.SetValue(DistanceProperty, value);

        public static bool GetManualTriggerOnly(DependencyObject element) => (bool)element.GetValue(ManualTriggerOnlyProperty);
        public static void SetManualTriggerOnly(DependencyObject element, bool value) => element.SetValue(ManualTriggerOnlyProperty, value);

        /// <summary>
        /// Manually triggers the reveal animation for the specified element.
        /// </summary>
        public static void TriggerReveal(FrameworkElement element)
        {
            if (element == null)
                return;

            if (!GetIsEnabled(element))
                return;

            StopActiveStoryboard(element);

            var mode = GetMode(element);
            var duration = GetDuration(element);
            var direction = GetDirection(element);
            var distance = GetDistance(element);

            var hasTranslate = mode is RevealMode.FadeReveal or RevealMode.SlideIn or RevealMode.ScaleSlide;
            var hasScale = mode is RevealMode.Scale or RevealMode.ScaleSlide;
            var hasFade = mode is RevealMode.FadeReveal or RevealMode.FadeOnly or RevealMode.Scale or RevealMode.ScaleSlide;

            double startX = 0d;
            double startY = 0d;
            if (hasTranslate)
            {
                switch (direction)
                {
                    case RevealDirection.Top:
                        startY = -distance;
                        break;
                    case RevealDirection.Bottom:
                        startY = distance;
                        break;
                    case RevealDirection.Left:
                        startX = -distance;
                        break;
                    case RevealDirection.Right:
                        startX = distance;
                        break;
                    default:
                        startY = distance;
                        break;
                }
            }

            if (element.RenderTransform is not CompositeTransform transform)
            {
                transform = new CompositeTransform();
                element.RenderTransform = transform;
            }
            element.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

            transform.TranslateX = startX;
            transform.TranslateY = startY;
            transform.ScaleX = hasScale ? 0.8 : 1.0;
            transform.ScaleY = hasScale ? 0.8 : 1.0;
            element.Opacity = hasFade ? 0.0 : 1.0;

            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            var sb = new Storyboard();

            if (hasFade)
            {
                var opacityAnim = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(duration),
                    EasingFunction = easing,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(opacityAnim, element);
                Storyboard.SetTargetProperty(opacityAnim, "Opacity");
                sb.Children.Add(opacityAnim);
            }

            if (hasTranslate)
            {
                var xAnim = new DoubleAnimation
                {
                    From = startX,
                    To = 0,
                    Duration = new Duration(duration),
                    EasingFunction = easing,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(xAnim, element);
                Storyboard.SetTargetProperty(xAnim, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
                sb.Children.Add(xAnim);

                var yAnim = new DoubleAnimation
                {
                    From = startY,
                    To = 0,
                    Duration = new Duration(duration),
                    EasingFunction = easing,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(yAnim, element);
                Storyboard.SetTargetProperty(yAnim, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
                sb.Children.Add(yAnim);
            }

            if (hasScale)
            {
                var sxAnim = new DoubleAnimation
                {
                    From = 0.8,
                    To = 1.0,
                    Duration = new Duration(duration),
                    EasingFunction = easing,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(sxAnim, element);
                Storyboard.SetTargetProperty(sxAnim, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
                sb.Children.Add(sxAnim);

                var syAnim = new DoubleAnimation
                {
                    From = 0.8,
                    To = 1.0,
                    Duration = new Duration(duration),
                    EasingFunction = easing,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(syAnim, element);
                Storyboard.SetTargetProperty(syAnim, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");
                sb.Children.Add(syAnim);
            }

            element.SetValue(ActiveStoryboardProperty, sb);
            sb.Completed += (_, _) =>
            {
                element.ClearValue(ActiveStoryboardProperty);
                element.Opacity = 1.0;
                transform.TranslateX = 0;
                transform.TranslateY = 0;
                transform.ScaleX = 1.0;
                transform.ScaleY = 1.0;
            };

            sb.Begin();
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            var enabled = e.NewValue is true;
            if (enabled)
            {
                element.Loaded += OnLoaded;
                element.Unloaded += OnUnloaded;

                // If IsEnabled is set after load, we still want it to run.
                element.DispatcherQueue?.TryEnqueue(() =>
                {
                    if (!GetManualTriggerOnly(element))
                        TriggerReveal(element);
                });
            }
            else
            {
                element.Loaded -= OnLoaded;
                element.Unloaded -= OnUnloaded;
                StopActiveStoryboard(element);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            if (!GetIsEnabled(element))
                return;

            if (GetManualTriggerOnly(element))
                return;

            TriggerReveal(element);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                StopActiveStoryboard(element);
            }
        }

        private static void StopActiveStoryboard(FrameworkElement element)
        {
            if (element.GetValue(ActiveStoryboardProperty) is Storyboard sb)
            {
                try
                {
                    sb.Stop();
                }
                catch
                {
                }
                element.ClearValue(ActiveStoryboardProperty);
            }
        }
    }

    /// <summary>
    /// Animation mode for reveal effect.
    /// </summary>
    public enum RevealMode
    {
        /// <summary>
        /// Fades in opacity while sliding into position (default).
        /// </summary>
        FadeReveal,

        /// <summary>
        /// Slides in from an offset while staying fully visible (no fade).
        /// </summary>
        SlideIn,

        /// <summary>
        /// Pure fade-in with no movement.
        /// </summary>
        FadeOnly,

        /// <summary>
        /// Scales up from center while fading in.
        /// </summary>
        Scale,

        /// <summary>
        /// Scales up while sliding into position with fade.
        /// </summary>
        ScaleSlide
    }

    /// <summary>
    /// Direction from which the reveal animation originates.
    /// </summary>
    public enum RevealDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }
}

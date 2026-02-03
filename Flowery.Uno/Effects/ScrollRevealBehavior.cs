using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Flowery.Effects
{
    /// <summary>
    /// Triggers <see cref="RevealBehavior"/> when an element becomes visible within a <see cref="ScrollViewer"/>.
    /// </summary>
    public static class ScrollRevealBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ScrollRevealBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly DependencyProperty WasTriggeredProperty =
            DependencyProperty.RegisterAttached(
                "WasTriggered",
                typeof(bool),
                typeof(ScrollRevealBehavior),
                new PropertyMetadata(false));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        private static bool GetWasTriggered(UIElement element) => (bool)element.GetValue(WasTriggeredProperty);
        private static void SetWasTriggered(UIElement element, bool value) => element.SetValue(WasTriggeredProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            var enabled = e.NewValue is true;
            if (enabled)
            {
                element.Loaded += OnLoaded;
                element.Unloaded += OnUnloaded;
                element.DispatcherQueue?.TryEnqueue(() => Attach(element));
            }
            else
            {
                element.Loaded -= OnLoaded;
                element.Unloaded -= OnUnloaded;
                SetWasTriggered(element, false);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                Attach(element);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // No-op: event handlers are attached to the ScrollViewer instance and are short-lived in examples.
        }

        private static void Attach(FrameworkElement element)
        {
            if (!GetIsEnabled(element))
                return;

            // Hide initially if RevealBehavior is set to manual trigger only.
            if (RevealBehavior.GetIsEnabled(element) && RevealBehavior.GetManualTriggerOnly(element))
                element.Opacity = 0;

            var scrollViewer = FindScrollViewerAncestor(element);
            if (scrollViewer == null)
                return;

            scrollViewer.ViewChanged += (_, _) => CheckVisibility(element, scrollViewer);
            element.DispatcherQueue?.TryEnqueue(() => CheckVisibility(element, scrollViewer));
        }

        private static ScrollViewer? FindScrollViewerAncestor(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is ScrollViewer sv)
                    return sv;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static void CheckVisibility(FrameworkElement element, ScrollViewer scrollViewer)
        {
            if (!GetIsEnabled(element) || GetWasTriggered(element))
                return;

            try
            {
                if (scrollViewer.Content is not UIElement content)
                    return;

                var transform = element.TransformToVisual(content);
                var bounds = transform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                if (Intersects(viewport, bounds))
                {
                    SetWasTriggered(element, true);
                    if (RevealBehavior.GetIsEnabled(element))
                        RevealBehavior.TriggerReveal(element);
                }
            }
            catch
            {
            }
        }

        private static bool Intersects(Rect a, Rect b)
        {
            return a.Left < b.Right &&
                   a.Right > b.Left &&
                   a.Top < b.Bottom &&
                   a.Bottom > b.Top;
        }
    }
}

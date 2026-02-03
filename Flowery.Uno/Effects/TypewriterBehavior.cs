using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Effects
{
    /// <summary>
    /// Creates a typewriter animation effect on <see cref="TextBlock"/> controls.
    /// </summary>
    public static class TypewriterBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(TypewriterBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.RegisterAttached(
                "Speed",
                typeof(TimeSpan),
                typeof(TypewriterBehavior),
                new PropertyMetadata(TimeSpan.FromMilliseconds(50), OnSpeedChanged));

        private static readonly DependencyProperty FullTextProperty =
            DependencyProperty.RegisterAttached(
                "FullText",
                typeof(string),
                typeof(TypewriterBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached(
                "Timer",
                typeof(DispatcherTimer),
                typeof(TypewriterBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty IndexProperty =
            DependencyProperty.RegisterAttached(
                "Index",
                typeof(int),
                typeof(TypewriterBehavior),
                new PropertyMetadata(0));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static TimeSpan GetSpeed(DependencyObject element) => (TimeSpan)element.GetValue(SpeedProperty);
        public static void SetSpeed(DependencyObject element, TimeSpan value) => element.SetValue(SpeedProperty, value);

        /// <summary>
        /// Restarts the typewriter animation from the beginning.
        /// </summary>
        public static void Restart(TextBlock textBlock)
        {
            if (textBlock == null)
                return;

            StopTypewriter(textBlock, restoreText: true);
            if (GetIsEnabled(textBlock))
                StartTypewriter(textBlock);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
                return;

            var enabled = e.NewValue is true;
            if (enabled)
            {
                textBlock.Loaded += OnLoaded;
                textBlock.Unloaded += OnUnloaded;
                textBlock.DispatcherQueue?.TryEnqueue(() => StartTypewriter(textBlock));
            }
            else
            {
                textBlock.Loaded -= OnLoaded;
                textBlock.Unloaded -= OnUnloaded;
                StopTypewriter(textBlock, restoreText: true);
            }
        }

        private static void OnSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock tb && tb.GetValue(TimerProperty) is DispatcherTimer timer)
            {
                timer.Interval = GetSpeed(tb);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && GetIsEnabled(tb))
                StartTypewriter(tb);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb)
                StopTypewriter(tb, restoreText: false);
        }

        private static void StartTypewriter(TextBlock textBlock)
        {
            if (!GetIsEnabled(textBlock))
                return;

            if (textBlock.GetValue(TimerProperty) is DispatcherTimer)
                return;

            var stored = textBlock.GetValue(FullTextProperty) as string;
            var text = stored ?? textBlock.Text;
            if (string.IsNullOrEmpty(text))
                return;

            textBlock.SetValue(FullTextProperty, text);
            textBlock.SetValue(IndexProperty, 0);
            textBlock.Text = string.Empty;

            var timer = new DispatcherTimer { Interval = ClampInterval(GetSpeed(textBlock)) };
            timer.Tick += (_, _) =>
            {
                var fullText = textBlock.GetValue(FullTextProperty) as string;
                if (string.IsNullOrEmpty(fullText))
                {
                    StopTypewriter(textBlock, restoreText: false);
                    return;
                }

                var idx = (int)textBlock.GetValue(IndexProperty);
                idx++;

                if (idx >= fullText.Length)
                {
                    textBlock.Text = fullText;
                    StopTypewriter(textBlock, restoreText: false);
                    return;
                }

                textBlock.Text = fullText[..idx];
                textBlock.SetValue(IndexProperty, idx);
            };

            textBlock.SetValue(TimerProperty, timer);
            timer.Start();
        }

        private static void StopTypewriter(TextBlock textBlock, bool restoreText)
        {
            if (textBlock.GetValue(TimerProperty) is DispatcherTimer timer)
            {
                timer.Stop();
                textBlock.ClearValue(TimerProperty);
            }

            if (restoreText)
            {
                var fullText = textBlock.GetValue(FullTextProperty) as string;
                if (fullText is not null)
                {
                    textBlock.Text = fullText;
                }
            }

            textBlock.ClearValue(IndexProperty);
        }

        private static TimeSpan ClampInterval(TimeSpan speed)
        {
            if (speed <= TimeSpan.Zero)
                return TimeSpan.FromMilliseconds(1);

            return speed;
        }
    }
}

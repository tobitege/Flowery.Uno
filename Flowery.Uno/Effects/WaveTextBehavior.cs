using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Effects
{
    /// <summary>
    /// Creates a wave animation effect on text by animating vertical translation.
    /// Works on <see cref="TextBlock"/> controls via attached properties.
    /// </summary>
    public static class WaveTextBehavior
    {
        // NOTE: Per-character wave is disabled on Uno; always fall back to whole-text wave.

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(WaveTextBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty IsPerCharacterProperty =
            DependencyProperty.RegisterAttached(
                "IsPerCharacter",
                typeof(bool),
                typeof(WaveTextBehavior),
                new PropertyMetadata(false, OnVisualSettingsChanged));

        public static readonly DependencyProperty AmplitudeProperty =
            DependencyProperty.RegisterAttached(
                "Amplitude",
                typeof(double),
                typeof(WaveTextBehavior),
                new PropertyMetadata(5d));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached(
                "Duration",
                typeof(TimeSpan),
                typeof(WaveTextBehavior),
                new PropertyMetadata(TimeSpan.FromMilliseconds(1000)));

        public static readonly DependencyProperty StaggerDelayProperty =
            DependencyProperty.RegisterAttached(
                "StaggerDelay",
                typeof(TimeSpan),
                typeof(WaveTextBehavior),
                new PropertyMetadata(TimeSpan.FromMilliseconds(50)));

        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached(
                "Timer",
                typeof(DispatcherTimer),
                typeof(WaveTextBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.RegisterAttached(
                "StartTime",
                typeof(DateTimeOffset),
                typeof(WaveTextBehavior),
                new PropertyMetadata(default(DateTimeOffset)));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static bool GetIsPerCharacter(DependencyObject element) => (bool)element.GetValue(IsPerCharacterProperty);
        public static void SetIsPerCharacter(DependencyObject element, bool value) => element.SetValue(IsPerCharacterProperty, value);

        public static double GetAmplitude(DependencyObject element) => (double)element.GetValue(AmplitudeProperty);
        public static void SetAmplitude(DependencyObject element, double value) => element.SetValue(AmplitudeProperty, value);

        public static TimeSpan GetDuration(DependencyObject element) => (TimeSpan)element.GetValue(DurationProperty);
        public static void SetDuration(DependencyObject element, TimeSpan value) => element.SetValue(DurationProperty, value);

        public static TimeSpan GetStaggerDelay(DependencyObject element) => (TimeSpan)element.GetValue(StaggerDelayProperty);
        public static void SetStaggerDelay(DependencyObject element, TimeSpan value) => element.SetValue(StaggerDelayProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
                return;

            var enabled = e.NewValue is true;
            if (enabled)
            {
                textBlock.Loaded += OnLoaded;
                textBlock.Unloaded += OnUnloaded;
                textBlock.DispatcherQueue?.TryEnqueue(() => StartWave(textBlock));
            }
            else
            {
                textBlock.Loaded -= OnLoaded;
                textBlock.Unloaded -= OnUnloaded;
                StopWave(textBlock);
            }
        }

        private static void OnVisualSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock tb && GetIsEnabled(tb))
            {
                StopWave(tb);
                StartWave(tb);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && GetIsEnabled(tb))
                StartWave(tb);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb)
                StopWave(tb);
        }

        private static void StartWave(TextBlock textBlock)
        {
            if (!GetIsEnabled(textBlock))
                return;

            StopWave(textBlock);

            var text = textBlock.Text ?? string.Empty;
            if (string.IsNullOrEmpty(text))
                return;

            textBlock.SetValue(StartTimeProperty, DateTimeOffset.Now);

            if (textBlock.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                textBlock.RenderTransform = transform;
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (_, _) => Tick(textBlock);
            textBlock.SetValue(TimerProperty, timer);
            timer.Start();
        }

        private static void Tick(TextBlock textBlock)
        {
            var amplitude = GetAmplitude(textBlock);
            var duration = GetDuration(textBlock);

            var durationMs = Math.Max(1, duration.TotalMilliseconds);
            var elapsedMs = (DateTimeOffset.Now - (DateTimeOffset)textBlock.GetValue(StartTimeProperty)).TotalMilliseconds;

            if (textBlock.RenderTransform is TranslateTransform transform)
            {
                transform.Y = -amplitude * Math.Sin((elapsedMs / durationMs) * Math.PI * 2);
            }
        }

        private static void StopWave(TextBlock textBlock)
        {
            if (textBlock.GetValue(TimerProperty) is DispatcherTimer timer)
            {
                timer.Stop();
                textBlock.ClearValue(TimerProperty);
            }

            if (textBlock.RenderTransform is TranslateTransform transform)
            {
                transform.Y = 0;
            }
        }
    }
}

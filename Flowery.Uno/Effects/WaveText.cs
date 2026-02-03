using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Flowery.Effects
{
    /// <summary>
    /// A lightweight text control that can animate a wave effect per-character (or whole-text).
    /// This exists because some Uno/WinUI targets don't support <see cref="Microsoft.UI.Xaml.Documents.InlineUIContainer"/>
    /// for <see cref="TextBlock.Inlines"/>.
    /// </summary>
    public sealed partial class WaveText : ContentControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(WaveText),
                new PropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty IsPerCharacterProperty =
            DependencyProperty.Register(
                nameof(IsPerCharacter),
                typeof(bool),
                typeof(WaveText),
                new PropertyMetadata(false, OnVisualPropertyChanged));

        public bool IsPerCharacter
        {
            get => (bool)GetValue(IsPerCharacterProperty);
            set => SetValue(IsPerCharacterProperty, value);
        }

        public static readonly DependencyProperty AmplitudeProperty =
            DependencyProperty.Register(
                nameof(Amplitude),
                typeof(double),
                typeof(WaveText),
                new PropertyMetadata(5d));

        public double Amplitude
        {
            get => (double)GetValue(AmplitudeProperty);
            set => SetValue(AmplitudeProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(TimeSpan),
                typeof(WaveText),
                new PropertyMetadata(TimeSpan.FromMilliseconds(1000)));

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty StaggerDelayProperty =
            DependencyProperty.Register(
                nameof(StaggerDelay),
                typeof(TimeSpan),
                typeof(WaveText),
                new PropertyMetadata(TimeSpan.FromMilliseconds(50)));

        public TimeSpan StaggerDelay
        {
            get => (TimeSpan)GetValue(StaggerDelayProperty);
            set => SetValue(StaggerDelayProperty, value);
        }

        private DispatcherTimer? _timer;
        private DateTimeOffset _start;
        private readonly List<TextBlock> _charBlocks = [];
        private TranslateTransform? _wholeTransform;

        public WaveText()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WaveText wt && wt.IsLoaded)
            {
                wt.RebuildVisual();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RebuildVisual();
            Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void RebuildVisual()
        {
            Stop();

            _charBlocks.Clear();
            _wholeTransform = null;

            var text = Text ?? string.Empty;

            if (IsPerCharacter)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 0
                };

                for (var i = 0; i < text.Length; i++)
                {
                    var c = text[i];
                    var tb = new TextBlock
                    {
                        Text = c == ' ' ? "\u00A0" : c.ToString(),
                        RenderTransform = new TranslateTransform()
                    };

                    // Bind styling to this control so theme switching / font changes flow through.
                    tb.SetBinding(TextBlock.FontSizeProperty, new Binding { Source = this, Path = new PropertyPath("FontSize") });
                    tb.SetBinding(TextBlock.FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath("FontFamily") });
                    tb.SetBinding(TextBlock.FontStyleProperty, new Binding { Source = this, Path = new PropertyPath("FontStyle") });
                    tb.SetBinding(TextBlock.FontWeightProperty, new Binding { Source = this, Path = new PropertyPath("FontWeight") });
                    tb.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = this, Path = new PropertyPath("Foreground") });
                    tb.SetBinding(TextBlock.OpacityProperty, new Binding { Source = this, Path = new PropertyPath("Opacity") });

                    panel.Children.Add(tb);
                    _charBlocks.Add(tb);
                }

                Content = panel;
            }
            else
            {
                var tb = new TextBlock { Text = text };
                tb.SetBinding(TextBlock.FontSizeProperty, new Binding { Source = this, Path = new PropertyPath("FontSize") });
                tb.SetBinding(TextBlock.FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath("FontFamily") });
                tb.SetBinding(TextBlock.FontStyleProperty, new Binding { Source = this, Path = new PropertyPath("FontStyle") });
                tb.SetBinding(TextBlock.FontWeightProperty, new Binding { Source = this, Path = new PropertyPath("FontWeight") });
                tb.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = this, Path = new PropertyPath("Foreground") });
                tb.SetBinding(TextBlock.OpacityProperty, new Binding { Source = this, Path = new PropertyPath("Opacity") });

                _wholeTransform = new TranslateTransform();
                tb.RenderTransform = _wholeTransform;
                Content = tb;
            }
        }

        private void Start()
        {
            _start = DateTimeOffset.Now;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            foreach (var tb in _charBlocks)
            {
                if (tb.RenderTransform is TranslateTransform t)
                    t.Y = 0;
            }

            if (_wholeTransform != null)
                _wholeTransform.Y = 0;
        }

        private void Timer_Tick(object? sender, object e)
        {
            var amplitude = Amplitude;
            var durationMs = Math.Max(1, Duration.TotalMilliseconds);
            var elapsedMs = (DateTimeOffset.Now - _start).TotalMilliseconds;
            var staggerMs = StaggerDelay.TotalMilliseconds;

            if (IsPerCharacter && _charBlocks.Count > 0)
            {
                for (var i = 0; i < _charBlocks.Count; i++)
                {
                    var localMs = elapsedMs - (staggerMs * i);
                    var y = localMs <= 0 ? 0 : -amplitude * Math.Sin((localMs / durationMs) * Math.PI * 2);
                    if (_charBlocks[i].RenderTransform is TranslateTransform t)
                        t.Y = y;
                }
                return;
            }

            if (_wholeTransform != null)
            {
                _wholeTransform.Y = -amplitude * Math.Sin((elapsedMs / durationMs) * Math.PI * 2);
            }
        }
    }
}

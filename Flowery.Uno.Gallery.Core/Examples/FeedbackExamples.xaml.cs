using System;
using System.Collections.Generic;
using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class FeedbackExamples : ScrollableExamplePage
    {
        private readonly List<RandomLoaderState> _randomLoaderStates = [];
        private DispatcherTimer? _randomLoadersTimer;
        private DateTimeOffset _randomLoadersLastTick;

        private static readonly DaisyColor[] RandomCycleColors =
        [
            DaisyColor.Primary,
            DaisyColor.Secondary,
            DaisyColor.Accent,
            DaisyColor.Info,
            DaisyColor.Success,
            DaisyColor.Warning,
            DaisyColor.Error
        ];

        public FeedbackExamples()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildRandomLoaders();
            StartRandomLoadersTimer();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopRandomLoadersTimer();
        }

        private void BuildRandomLoaders()
        {
            if (RandomLoadersPanel == null) return;

            RandomLoadersPanel.Children.Clear();
            _randomLoaderStates.Clear();

            var variants = Enum.GetValues<DaisyLoadingVariant>();
            var random = new Random();

            var variantPool = new List<DaisyLoadingVariant>(variants);
            Shuffle(variantPool, random);

            var initialColorOffset = random.Next(RandomCycleColors.Length);
            var background = GetBaseBackgroundColor();

            var count = Math.Min(10, variantPool.Count);
            for (int i = 0; i < count; i++)
            {
                var variant = variantPool[i];
                var periodMs = GetVariantDurationMs(variant) * 2;
                var colorIndex = FindNextVisibleColorIndex((initialColorOffset + i) % RandomCycleColors.Length, background);

                var loader = new DaisyLoading
                {
                    Variant = variant,
                    Color = RandomCycleColors[colorIndex]
                };

                RandomLoadersPanel.Children.Add(loader);
                _randomLoaderStates.Add(new RandomLoaderState(loader, periodMs, periodMs, colorIndex));
            }
        }

        private void StartRandomLoadersTimer()
        {
            StopRandomLoadersTimer();

            _randomLoadersLastTick = DateTimeOffset.UtcNow;
            _randomLoadersTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _randomLoadersTimer.Tick += OnRandomLoadersTick;
            _randomLoadersTimer.Start();
        }

        private void StopRandomLoadersTimer()
        {
            if (_randomLoadersTimer == null) return;

            _randomLoadersTimer.Stop();
            _randomLoadersTimer.Tick -= OnRandomLoadersTick;
            _randomLoadersTimer = null;
        }

        private void OnRandomLoadersTick(object? sender, object e)
        {
            var now = DateTimeOffset.UtcNow;
            var elapsedMs = (now - _randomLoadersLastTick).TotalMilliseconds;
            _randomLoadersLastTick = now;

            if (elapsedMs <= 0) return;

            var background = GetBaseBackgroundColor();

            for (int i = 0; i < _randomLoaderStates.Count; i++)
            {
                var state = _randomLoaderStates[i];
                state.RemainingMs -= elapsedMs;
                while (state.RemainingMs <= 0)
                {
                    state.ColorIndex = FindNextVisibleColorIndex((state.ColorIndex + 1) % RandomCycleColors.Length, background);
                    state.Loader.Color = RandomCycleColors[state.ColorIndex];
                    state.RemainingMs += state.PeriodMs;
                }
            }
        }

        private static Color GetBaseBackgroundColor()
        {
            if (TryGetResourceColor("DaisyBase100Brush", out var color))
                return color;

            if (TryGetResourceColor("DaisyBase200Brush", out color))
                return color;

            return Color.FromArgb(255, 0, 0, 0);
        }

        private static bool TryGetResourceColor(string key, out Color color)
        {
            if (Application.Current.Resources.TryGetValue(key, out var brushObj) &&
                brushObj is SolidColorBrush solid)
            {
                color = solid.Color;
                return true;
            }

            color = default;
            return false;
        }

        private static int FindNextVisibleColorIndex(int startIndex, Color background)
        {
            for (int i = 0; i < RandomCycleColors.Length; i++)
            {
                var idx = (startIndex + i) % RandomCycleColors.Length;
                if (IsColorVisible(RandomCycleColors[idx], background))
                    return idx;
            }

            return startIndex;
        }

        private static bool IsColorVisible(DaisyColor color, Color background)
        {
            var key = color switch
            {
                DaisyColor.Primary => "DaisyPrimaryBrush",
                DaisyColor.Secondary => "DaisySecondaryBrush",
                DaisyColor.Accent => "DaisyAccentBrush",
                DaisyColor.Info => "DaisyInfoBrush",
                DaisyColor.Success => "DaisySuccessBrush",
                DaisyColor.Warning => "DaisyWarningBrush",
                DaisyColor.Error => "DaisyErrorBrush",
                _ => null
            };

            if (key == null || !TryGetResourceColor(key, out var fg))
                return false;

            return ContrastRatio(fg, background) >= 3.0;
        }

        private static double ContrastRatio(Color a, Color b)
        {
            var la = RelativeLuminance(a);
            var lb = RelativeLuminance(b);
            var lighter = Math.Max(la, lb);
            var darker = Math.Min(la, lb);
            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double RelativeLuminance(Color c)
        {
            static double Channel(double v)
            {
                v /= 255.0;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }

            var r = Channel(c.R);
            var g = Channel(c.G);
            var bl = Channel(c.B);
            return (0.2126 * r) + (0.7152 * g) + (0.0722 * bl);
        }

        private static void Shuffle<T>(List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static double GetVariantDurationMs(DaisyLoadingVariant variant)
        {
            return variant switch
            {
                DaisyLoadingVariant.Spinner or DaisyLoadingVariant.Ring => 750,
                DaisyLoadingVariant.Dots or DaisyLoadingVariant.Ball => 600,
                DaisyLoadingVariant.Bars => 800,
                DaisyLoadingVariant.Infinity or DaisyLoadingVariant.Orbit => 1200,
                DaisyLoadingVariant.Snake => 1600,
                DaisyLoadingVariant.Pulse => 1500,
                DaisyLoadingVariant.Wave => 1000,
                DaisyLoadingVariant.Bounce => 1600,
                DaisyLoadingVariant.Matrix => 1800,
                DaisyLoadingVariant.MatrixInward or DaisyLoadingVariant.MatrixOutward => 1200,
                DaisyLoadingVariant.MatrixVertical => 1000,
                DaisyLoadingVariant.MatrixRain => 1000,
                DaisyLoadingVariant.Hourglass => 2000,
                DaisyLoadingVariant.SignalSweep => 1200,
                DaisyLoadingVariant.BitFlip => 1600,
                DaisyLoadingVariant.PacketBurst => 1200,
                DaisyLoadingVariant.CometTrail => 1500,
                DaisyLoadingVariant.Heartbeat => 1500,
                DaisyLoadingVariant.TunnelZoom => 1500,
                DaisyLoadingVariant.GlitchReveal => 2000,
                DaisyLoadingVariant.RippleMatrix => 1200,
                DaisyLoadingVariant.CursorBlink => 1000,
                DaisyLoadingVariant.CountdownSpinner => 1200,
                _ => 1200
            };
        }

        private sealed class RandomLoaderState(DaisyLoading loader, double periodMs, double remainingMs, int colorIndex)
        {
            public DaisyLoading Loader { get; } = loader;
            public double PeriodMs { get; } = periodMs;
            public double RemainingMs { get; set; } = remainingMs;
            public int ColorIndex { get; set; } = colorIndex;
        }
    }
}


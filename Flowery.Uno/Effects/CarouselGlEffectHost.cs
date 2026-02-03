using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Effects
{
    public sealed partial class CarouselGlEffectHost : ContentControl
    {
        public static readonly DependencyProperty EffectProperty =
            DependencyProperty.Register(
                nameof(Effect),
                typeof(CarouselGlEffectKind),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(CarouselGlEffectKind.None, OnEffectChanged));

        public static readonly DependencyProperty IntervalSecondsProperty =
            DependencyProperty.Register(
                nameof(IntervalSeconds),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(0d, OnIntervalSecondsChanged));

        public static readonly DependencyProperty TransitionSecondsProperty =
            DependencyProperty.Register(
                nameof(TransitionSeconds),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(2.4d, OnTransitionSecondsChanged));

        public static readonly DependencyProperty OverlayEffectProperty =
            DependencyProperty.Register(
                nameof(OverlayEffect),
                typeof(CarouselGlEffectKind),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(CarouselGlEffectKind.None, OnOverlayEffectChanged));

        public static readonly DependencyProperty TransitionSliceCountProperty =
            DependencyProperty.Register(
                nameof(TransitionSliceCount),
                typeof(int),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(8, OnTransitionParamsChanged));

        public static readonly DependencyProperty TransitionStaggerSlicesProperty =
            DependencyProperty.Register(
                nameof(TransitionStaggerSlices),
                typeof(bool),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(true, OnTransitionParamsChanged));

        public static readonly DependencyProperty TransitionSliceStaggerMsProperty =
            DependencyProperty.Register(
                nameof(TransitionSliceStaggerMs),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(50.0, OnTransitionParamsChanged));

        public static readonly DependencyProperty TransitionPixelateSizeProperty =
            DependencyProperty.Register(
                nameof(TransitionPixelateSize),
                typeof(int),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(20, OnTransitionParamsChanged));

        public static readonly DependencyProperty TransitionDissolveDensityProperty =
            DependencyProperty.Register(
                nameof(TransitionDissolveDensity),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(0.5, OnTransitionParamsChanged));

        public static readonly DependencyProperty TransitionFlipAngleProperty =
            DependencyProperty.Register(
                nameof(TransitionFlipAngle),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(90.0, OnTransitionParamsChanged));

        public CarouselGlEffectHost()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            LayoutUpdated += OnLayoutUpdated;
        }

        public CarouselGlEffectKind Effect
        {
            get => (CarouselGlEffectKind)GetValue(EffectProperty);
            set => SetValue(EffectProperty, value);
        }

        public double IntervalSeconds
        {
            get => (double)GetValue(IntervalSecondsProperty);
            set => SetValue(IntervalSecondsProperty, value);
        }

        public double TransitionSeconds
        {
            get => (double)GetValue(TransitionSecondsProperty);
            set => SetValue(TransitionSecondsProperty, value);
        }

        public CarouselGlEffectKind OverlayEffect
        {
            get => (CarouselGlEffectKind)GetValue(OverlayEffectProperty);
            set => SetValue(OverlayEffectProperty, value);
        }

        public int TransitionSliceCount
        {
            get => (int)GetValue(TransitionSliceCountProperty);
            set => SetValue(TransitionSliceCountProperty, value);
        }

        public bool TransitionStaggerSlices
        {
            get => (bool)GetValue(TransitionStaggerSlicesProperty);
            set => SetValue(TransitionStaggerSlicesProperty, value);
        }

        public double TransitionSliceStaggerMs
        {
            get => (double)GetValue(TransitionSliceStaggerMsProperty);
            set => SetValue(TransitionSliceStaggerMsProperty, value);
        }

        public int TransitionPixelateSize
        {
            get => (int)GetValue(TransitionPixelateSizeProperty);
            set => SetValue(TransitionPixelateSizeProperty, value);
        }

        public double TransitionDissolveDensity
        {
            get => (double)GetValue(TransitionDissolveDensityProperty);
            set => SetValue(TransitionDissolveDensityProperty, value);
        }

        public double TransitionFlipAngle
        {
            get => (double)GetValue(TransitionFlipAngleProperty);
            set => SetValue(TransitionFlipAngleProperty, value);
        }

        private static void OnEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CarouselGlEffectHost host)
            {
                return;
            }

            if (!host.IsLoaded)
            {
                return;
            }

            CarouselGlRuntime.Current.Attach(host, host.Effect, host.OverlayEffect);
            CarouselGlRuntime.Current.SetIntervalSeconds(host, host.IntervalSeconds);
        }

        private static void OnOverlayEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CarouselGlEffectHost host)
            {
                return;
            }

            if (!host.IsLoaded)
            {
                return;
            }

            CarouselGlRuntime.Current.Attach(host, host.Effect, host.OverlayEffect);
            CarouselGlRuntime.Current.SetIntervalSeconds(host, host.IntervalSeconds);
        }

        private static void OnIntervalSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CarouselGlEffectHost host)
            {
                return;
            }

            if (!host.IsLoaded)
            {
                return;
            }

            CarouselGlRuntime.Current.SetIntervalSeconds(host, host.IntervalSeconds);
        }

        private static void OnTransitionSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CarouselGlEffectHost host)
            {
                return;
            }

            if (!host.IsLoaded)
            {
                return;
            }

            CarouselGlRuntime.Current.SetTransitionSeconds(host, host.TransitionSeconds);
        }

        private static void OnTransitionParamsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CarouselGlEffectHost host)
            {
                return;
            }

            if (!host.IsLoaded)
            {
                return;
            }

            CarouselGlRuntime.Current.SetTransitionSeconds(host, host.TransitionSeconds);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CarouselGlRuntime.Current.Attach(this, Effect, OverlayEffect);
            CarouselGlRuntime.Current.SetIntervalSeconds(this, IntervalSeconds);
            CarouselGlRuntime.Current.SetTransitionSeconds(this, TransitionSeconds);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CarouselGlRuntime.Current.Detach(this);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CarouselGlRuntime.Current.NotifyLayoutChanged(this);
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            CarouselGlRuntime.Current.NotifyLayoutChanged(this);
        }

    }
}

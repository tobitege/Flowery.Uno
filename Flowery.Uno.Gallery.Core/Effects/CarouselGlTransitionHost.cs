using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Effects
{
    public sealed partial class CarouselGlTransitionHost : ContentControl
    {
        public static readonly DependencyProperty TransitionNameProperty =
            DependencyProperty.Register(
                nameof(TransitionName),
                typeof(string),
                typeof(CarouselGlTransitionHost),
                new PropertyMetadata(string.Empty, OnTransitionNameChanged));

        public static readonly DependencyProperty IntervalSecondsProperty =
            DependencyProperty.Register(
                nameof(IntervalSeconds),
                typeof(double),
                typeof(CarouselGlTransitionHost),
                new PropertyMetadata(0d, OnIntervalSecondsChanged));

        public static readonly DependencyProperty TransitionSecondsProperty =
            DependencyProperty.Register(
                nameof(TransitionSeconds),
                typeof(double),
                typeof(CarouselGlTransitionHost),
                new PropertyMetadata(0.6d, OnTransitionSecondsChanged));

        public CarouselGlTransitionHost()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            LayoutUpdated += OnLayoutUpdated;
        }

#if __ANDROID__
        public new string TransitionName
#else
        public string TransitionName
#endif
        {
            get => (string)GetValue(TransitionNameProperty);
            set => SetValue(TransitionNameProperty, value);
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

        private static void OnTransitionNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CarouselGlTransitionHost host)
            {
                return;
            }

            if (!host.IsLoaded)
            {
                return;
            }

            CarouselGlTransitionOverlayService.Instance.Attach(host, host.TransitionName);
            CarouselGlTransitionOverlayService.Instance.SetIntervalSeconds(host, host.IntervalSeconds);
            CarouselGlTransitionOverlayService.Instance.SetTransitionSeconds(host, host.TransitionSeconds);
        }

        private static void OnIntervalSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CarouselGlTransitionHost host)
            {
                return;
            }

            if (!host.IsLoaded)
            {
                return;
            }

            CarouselGlTransitionOverlayService.Instance.SetIntervalSeconds(host, host.IntervalSeconds);
        }

        private static void OnTransitionSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CarouselGlTransitionHost host)
            {
                return;
            }

            if (!host.IsLoaded)
            {
                return;
            }

            CarouselGlTransitionOverlayService.Instance.SetTransitionSeconds(host, host.TransitionSeconds);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CarouselGlTransitionOverlayService.Instance.Attach(this, TransitionName);
            CarouselGlTransitionOverlayService.Instance.SetIntervalSeconds(this, IntervalSeconds);
            CarouselGlTransitionOverlayService.Instance.SetTransitionSeconds(this, TransitionSeconds);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CarouselGlTransitionOverlayService.Instance.Detach(this);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CarouselGlTransitionOverlayService.Instance.NotifyLayoutChanged(this);
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            CarouselGlTransitionOverlayService.Instance.NotifyLayoutChanged(this);
        }
    }
}

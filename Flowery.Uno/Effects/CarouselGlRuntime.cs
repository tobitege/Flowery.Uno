using System;
using Microsoft.UI.Xaml;

namespace Flowery.Effects
{
    internal interface ICarouselGlRuntime
    {
        void Attach(FrameworkElement host, CarouselGlEffectKind effect, CarouselGlEffectKind overlayEffect);
        void SetIntervalSeconds(FrameworkElement host, double intervalSeconds);
        void SetTransitionSeconds(FrameworkElement host, double transitionSeconds);
        void Detach(FrameworkElement host);
        void NotifyLayoutChanged(FrameworkElement host);
    }

    internal static class CarouselGlRuntime
    {
        public static ICarouselGlRuntime Current { get; } = CreateRuntime();

        private static ICarouselGlRuntime CreateRuntime()
        {
            if (OperatingSystem.IsBrowser())
            {
                return new CarouselGlOverlayRuntime();
            }

            return new CarouselGlNoOpRuntime();
        }
    }

    internal sealed class CarouselGlOverlayRuntime : ICarouselGlRuntime
    {
        public void Attach(FrameworkElement host, CarouselGlEffectKind effect, CarouselGlEffectKind overlayEffect)
        {
            CarouselGlOverlayService.Instance.Attach(host, effect, overlayEffect);
        }

        public void SetIntervalSeconds(FrameworkElement host, double intervalSeconds)
        {
            CarouselGlOverlayService.Instance.SetIntervalSeconds(host, intervalSeconds);
        }

        public void SetTransitionSeconds(FrameworkElement host, double transitionSeconds)
        {
            CarouselGlOverlayService.Instance.SetTransitionSeconds(host, transitionSeconds);
        }

        public void Detach(FrameworkElement host)
        {
            CarouselGlOverlayService.Instance.Detach(host);
        }

        public void NotifyLayoutChanged(FrameworkElement host)
        {
            CarouselGlOverlayService.Instance.NotifyLayoutChanged(host);
        }
    }

    internal sealed class CarouselGlNoOpRuntime : ICarouselGlRuntime
    {
        public void Attach(FrameworkElement host, CarouselGlEffectKind effect, CarouselGlEffectKind overlayEffect)
        {
            _ = host;
            _ = effect;
            _ = overlayEffect;
        }

        public void SetIntervalSeconds(FrameworkElement host, double intervalSeconds)
        {
            _ = host;
            _ = intervalSeconds;
        }

        public void SetTransitionSeconds(FrameworkElement host, double transitionSeconds)
        {
            _ = host;
            _ = transitionSeconds;
        }

        public void Detach(FrameworkElement host)
        {
            _ = host;
        }

        public void NotifyLayoutChanged(FrameworkElement host)
        {
            _ = host;
        }
    }
}

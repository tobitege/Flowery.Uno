#if __WASM__ // && (HAS_UNO_SKIA || __UNO_SKIA__ || __SKIA__ || HAS_UNO_SKIA_WEBASSEMBLY_BROWSER || __UNO_SKIA_WEBASSEMBLY_BROWSER__)
using System;
using System.Collections.Generic;
using System.Linq;
using Flowery.Controls;
using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Foundation;

namespace Flowery.Effects
{
    internal sealed class CarouselGlSkiaRuntime : ICarouselGlRuntime
    {
        private readonly Dictionary<FrameworkElement, SkiaEntry> _entries = new();

        public void Attach(FrameworkElement host, CarouselGlEffectKind effect, CarouselGlEffectKind overlayEffect)
        {
            if (!OperatingSystem.IsBrowser())
            {
                return;
            }

            if (effect == CarouselGlEffectKind.None && overlayEffect != CarouselGlEffectKind.None)
            {
                effect = overlayEffect;
                overlayEffect = CarouselGlEffectKind.None;
            }

            if (effect == CarouselGlEffectKind.None)
            {
                Detach(host);
                return;
            }

            if (!CarouselGlEffectRegistry.TryGet(effect, out var definition) || definition is null)
            {
                Detach(host);
                return;
            }

            var entry = GetOrCreateEntry(host);
            UpdateEntry(entry, definition, effect, overlayEffect);
        }

        public void SetIntervalSeconds(FrameworkElement host, double intervalSeconds)
        {
            _ = intervalSeconds;
            if (_entries.TryGetValue(host, out var entry) && entry.Carousel != null)
            {
                ApplyCarouselSettings(entry, entry.Carousel, entry.Definition, entry.EffectKind, entry.OverlayEffectKind);
            }
        }

        public void SetTransitionSeconds(FrameworkElement host, double transitionSeconds)
        {
            _ = transitionSeconds;
            if (_entries.TryGetValue(host, out var entry) && entry.Carousel != null)
            {
                ApplyCarouselSettings(entry, entry.Carousel, entry.Definition, entry.EffectKind, entry.OverlayEffectKind);
            }
        }

        public void Detach(FrameworkElement host)
        {
            if (!OperatingSystem.IsBrowser())
            {
                return;
            }

            if (!_entries.Remove(host, out var entry))
            {
                return;
            }

            if (host is ContentControl contentControl && entry.Carousel != null)
            {
                if (ReferenceEquals(contentControl.Content, entry.Carousel))
                {
                    contentControl.Content = null;
                }
            }
        }

        public void NotifyLayoutChanged(FrameworkElement host)
        {
            _ = host;
        }

        private SkiaEntry GetOrCreateEntry(FrameworkElement host)
        {
            if (_entries.TryGetValue(host, out var entry))
            {
                return entry;
            }

            entry = new SkiaEntry(host);
            _entries[host] = entry;
            return entry;
        }

        private void UpdateEntry(
            SkiaEntry entry,
            CarouselGlEffectDefinition definition,
            CarouselGlEffectKind effect,
            CarouselGlEffectKind overlayEffect)
        {
            entry.Definition = definition;
            entry.EffectKind = effect;
            entry.OverlayEffectKind = overlayEffect;

            var assetsKey = BuildAssetsKey(definition.Assets);
            var mode = definition.Mode;
            bool needsRebuild = entry.Carousel == null
                || !string.Equals(entry.AssetsKey, assetsKey, StringComparison.Ordinal)
                || !string.Equals(entry.Mode, mode, StringComparison.Ordinal);

            if (needsRebuild)
            {
                entry.Carousel = BuildCarousel(entry.Host, definition);
                entry.AssetsKey = assetsKey;
                entry.Mode = mode;
                AttachCarousel(entry);
            }

            if (entry.Carousel != null)
            {
                ApplyCarouselSettings(entry, entry.Carousel, definition, effect, overlayEffect);
            }
        }

        private void AttachCarousel(SkiaEntry entry)
        {
            if (entry.Host is not ContentControl host)
            {
                return;
            }

            if (entry.Carousel == null)
            {
                return;
            }

            EnsureHostPanel(entry);
            if (entry.HostPanel == null)
            {
                return;
            }

            if (host.Content is UIElement existing && !ReferenceEquals(existing, entry.HostPanel))
            {
                host.Content = null;
            }

            host.Content = entry.HostPanel;
        }

        private static DaisyCarousel BuildCarousel(FrameworkElement host, CarouselGlEffectDefinition definition)
        {
            var panel = new StackPanel();
            foreach (var slide in BuildSlides(definition))
            {
                panel.Children.Add(slide);
            }

            var carousel = new DaisyCarousel
            {
                Content = panel,
                ShowNavigation = false,
                WrapAround = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            return carousel;
        }

        private static IEnumerable<UIElement> BuildSlides(CarouselGlEffectDefinition definition)
        {
            var assets = definition.Assets;
            return definition.Mode switch
            {
                "mask" => BuildImagePairSlides(assets, "maskA", "maskB"),
                "flip" => BuildImagePairSlides(assets, "flipA", "flipB"),
                "transition" => BuildImagePairSlides(assets, "transitionA", "transitionB"),
                "effect" => BuildSingleImageSlide(assets, "effect"),
                "text" => BuildTextFillSlides(assets),
                _ => BuildFallbackSlides()
            };
        }

        private static IEnumerable<UIElement> BuildImagePairSlides(
            IReadOnlyDictionary<string, string> assets,
            string firstKey,
            string secondKey)
        {
            if (assets.TryGetValue(firstKey, out var first) && assets.TryGetValue(secondKey, out var second))
            {
                yield return CreateImageSlide(first);
                yield return CreateImageSlide(second);
                yield break;
            }

            foreach (var fallback in BuildFallbackSlides())
            {
                yield return fallback;
            }
        }

        private static IEnumerable<UIElement> BuildSingleImageSlide(IReadOnlyDictionary<string, string> assets, string key)
        {
            if (assets.TryGetValue(key, out var path))
            {
                yield return CreateImageSlide(path);
                yield break;
            }

            foreach (var fallback in BuildFallbackSlides())
            {
                yield return fallback;
            }
        }

        private static IEnumerable<UIElement> BuildTextFillSlides(IReadOnlyDictionary<string, string> assets)
        {
            if (assets.TryGetValue("text", out var path))
            {
                yield return CreateTextFillSlide(path);
                yield break;
            }

            foreach (var fallback in BuildFallbackSlides())
            {
                yield return fallback;
            }
        }

        private static IEnumerable<UIElement> BuildFallbackSlides()
        {
            yield return CreatePlaceholderSlide("Carousel GL assets missing");
        }

        private static UIElement CreateImageSlide(string assetPath)
        {
            var image = new Image
            {
                Source = CreateImageSource(assetPath),
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            return new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Children = { image }
            };
        }

        private static UIElement CreateTextFillSlide(string assetPath)
        {
            var textBlock = new TextBlock
            {
                Text = "Flowery",
                FontSize = 72,
                FontWeight = FontWeights.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            FloweryTextFillEffects.SetImageSource(textBlock, CreateImageSource(assetPath));
            FloweryTextFillEffects.SetAnimate(textBlock, true);
            FloweryTextFillEffects.SetAutoReverse(textBlock, true);
            FloweryTextFillEffects.SetDuration(textBlock, 6);
            FloweryTextFillEffects.SetPanX(textBlock, 0.25);
            FloweryTextFillEffects.SetPanY(textBlock, 0.1);
            FloweryTextFillEffects.SetAutoStart(textBlock, true);

            return new Border
            {
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                Child = textBlock,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        private static UIElement CreatePlaceholderSlide(string text)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };

            return new Border
            {
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                Child = textBlock,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        private static ImageSource CreateImageSource(string assetPath)
        {
            if (IsWebUri(assetPath))
            {
                return new BitmapImage(new Uri(assetPath, UriKind.Absolute));
            }

            if (OperatingSystem.IsBrowser() && TryBuildBrowserAssetUri(assetPath, out var browserUri))
            {
                return new BitmapImage(browserUri);
            }

            if (assetPath.StartsWith("ms-appx:///", StringComparison.Ordinal))
            {
                return new BitmapImage(new Uri(assetPath, UriKind.Absolute));
            }

            if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                var uri = new Uri($"ms-appx:///Flowery.Uno.Gallery/{assetPath}", UriKind.Absolute);
                return new BitmapImage(uri);
            }

            var fallbackUri = new Uri($"ms-appx:///{assetPath}", UriKind.Absolute);
            return new BitmapImage(fallbackUri);
        }

        private static bool IsWebUri(string assetPath)
        {
            return assetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryBuildBrowserAssetUri(string assetPath, out Uri uri)
        {
            uri = null!;
            if (assetPath.StartsWith("ms-appx:///", StringComparison.Ordinal))
            {
                var trimmed = assetPath["ms-appx:///".Length..];
                return TryBuildBrowserUriFromRelative(trimmed, out uri);
            }

            if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return TryBuildBrowserUriFromRelative(assetPath, out uri);
            }

            return false;
        }

        private static bool TryBuildBrowserUriFromRelative(string assetPath, out Uri uri)
        {
            uri = null!;
            var origin = GetBrowserOrigin();
            if (string.IsNullOrEmpty(origin))
            {
                return false;
            }

            var trimmed = assetPath.TrimStart('/');
            uri = new Uri($"{origin}/_framework/{trimmed}", UriKind.Absolute);
            return true;
        }

        private static string? _browserOrigin;

        private static string? GetBrowserOrigin()
        {
            if (!OperatingSystem.IsBrowser())
            {
                return null;
            }

            if (_browserOrigin != null)
            {
                return _browserOrigin;
            }

            try
            {
                var origin = WebAssemblyRuntime.InvokeJS("window.location.origin");
                if (string.IsNullOrWhiteSpace(origin) || string.Equals(origin, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                _browserOrigin = origin.Trim().TrimEnd('/');
                return _browserOrigin;
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureHostPanel(SkiaEntry entry)
        {
            if (entry.Carousel == null)
            {
                return;
            }

            if (entry.HostPanel == null)
            {
                entry.HostPanel = BuildHostPanel(entry.Carousel, entry);
                return;
            }

            if (!entry.HostPanel.Children.Contains(entry.Carousel))
            {
                entry.HostPanel.Children.Clear();
                entry.HostPanel.Children.Add(entry.Carousel);
                entry.HostPanel.Children.Add(BuildDebugOverlay(entry));
            }
            else if (entry.DebugLabel != null)
            {
                entry.DebugLabel.Text = BuildDebugText(entry);
            }
        }

        private static Grid BuildHostPanel(DaisyCarousel carousel, SkiaEntry entry)
        {
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            grid.Children.Add(carousel);
            grid.Children.Add(BuildDebugOverlay(entry));
            return grid;
        }

        private static UIElement BuildDebugOverlay(SkiaEntry entry)
        {
            var textBlock = new TextBlock
            {
                Text = BuildDebugText(entry),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            entry.DebugLabel = textBlock;

            return new Border
            {
                Background = new SolidColorBrush(Colors.Black),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(8),
                Opacity = 0.65,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false,
                Child = textBlock
            };
        }

        private static string BuildDebugText(SkiaEntry entry)
        {
            return $"Skia runtime attached ({entry.EffectKind})";
        }

        private static void ApplyCarouselSettings(
            SkiaEntry entry,
            DaisyCarousel carousel,
            CarouselGlEffectDefinition definition,
            CarouselGlEffectKind effect,
            CarouselGlEffectKind overlayEffect)
        {
            var host = entry.Host;
            if (host is not CarouselGlEffectHost glHost)
            {
                return;
            }

            var transition = ResolveTransition(effect, definition.Mode);
            var slideEffect = ResolveSlideEffect(effect, overlayEffect, definition.Mode);

            carousel.SlideTransition = transition;
            carousel.SlideEffect = slideEffect;
            carousel.TransitionDuration = Math.Max(0.1, glHost.TransitionSeconds);
            carousel.SlideInterval = Math.Max(0.5, glHost.IntervalSeconds);
            carousel.Mode = glHost.IntervalSeconds > 0
                ? FlowerySlideshowMode.Slideshow
                : FlowerySlideshowMode.Manual;

            carousel.TransitionSliceCount = glHost.TransitionSliceCount;
            carousel.TransitionStaggerSlices = glHost.TransitionStaggerSlices;
            carousel.TransitionSliceStaggerMs = glHost.TransitionSliceStaggerMs;
            carousel.TransitionPixelateSize = glHost.TransitionPixelateSize;
            carousel.TransitionDissolveDensity = glHost.TransitionDissolveDensity;
            carousel.TransitionFlipAngle = glHost.TransitionFlipAngle;
        }

        private static FlowerySlideTransition ResolveTransition(CarouselGlEffectKind effect, string mode)
        {
            if (mode == "text" || IsEffectKind(effect))
            {
                return FlowerySlideTransition.None;
            }

            return effect switch
            {
                CarouselGlEffectKind.MaskTransition => FlowerySlideTransition.Checkerboard,
                CarouselGlEffectKind.BlindsHorizontal => FlowerySlideTransition.BlindsHorizontal,
                CarouselGlEffectKind.BlindsVertical => FlowerySlideTransition.BlindsVertical,
                CarouselGlEffectKind.SlicesHorizontal => FlowerySlideTransition.SlicesHorizontal,
                CarouselGlEffectKind.SlicesVertical => FlowerySlideTransition.SlicesVertical,
                CarouselGlEffectKind.Checkerboard => FlowerySlideTransition.Checkerboard,
                CarouselGlEffectKind.Spiral => FlowerySlideTransition.Spiral,
                CarouselGlEffectKind.MatrixRain => FlowerySlideTransition.MatrixRain,
                CarouselGlEffectKind.Wormhole => FlowerySlideTransition.Wormhole,
                CarouselGlEffectKind.Dissolve => FlowerySlideTransition.Dissolve,
                CarouselGlEffectKind.Pixelate => FlowerySlideTransition.Pixelate,
                CarouselGlEffectKind.FlipPlane => FlowerySlideTransition.FlipVertical,
                CarouselGlEffectKind.FlipHorizontal => FlowerySlideTransition.FlipHorizontal,
                CarouselGlEffectKind.FlipVertical => FlowerySlideTransition.FlipVertical,
                CarouselGlEffectKind.CubeLeft => FlowerySlideTransition.CubeLeft,
                CarouselGlEffectKind.CubeRight => FlowerySlideTransition.CubeRight,
                CarouselGlEffectKind.Fade => FlowerySlideTransition.Fade,
                CarouselGlEffectKind.FadeThroughBlack => FlowerySlideTransition.FadeThroughBlack,
                CarouselGlEffectKind.FadeThroughWhite => FlowerySlideTransition.FadeThroughWhite,
                CarouselGlEffectKind.SlideLeft => FlowerySlideTransition.SlideLeft,
                CarouselGlEffectKind.SlideRight => FlowerySlideTransition.SlideRight,
                CarouselGlEffectKind.SlideUp => FlowerySlideTransition.SlideUp,
                CarouselGlEffectKind.SlideDown => FlowerySlideTransition.SlideDown,
                CarouselGlEffectKind.PushLeft => FlowerySlideTransition.PushLeft,
                CarouselGlEffectKind.PushRight => FlowerySlideTransition.PushRight,
                CarouselGlEffectKind.PushUp => FlowerySlideTransition.PushUp,
                CarouselGlEffectKind.PushDown => FlowerySlideTransition.PushDown,
                CarouselGlEffectKind.ZoomIn => FlowerySlideTransition.ZoomIn,
                CarouselGlEffectKind.ZoomOut => FlowerySlideTransition.ZoomOut,
                CarouselGlEffectKind.ZoomCross => FlowerySlideTransition.ZoomCross,
                CarouselGlEffectKind.CoverLeft => FlowerySlideTransition.CoverLeft,
                CarouselGlEffectKind.CoverRight => FlowerySlideTransition.CoverRight,
                CarouselGlEffectKind.RevealLeft => FlowerySlideTransition.RevealLeft,
                CarouselGlEffectKind.RevealRight => FlowerySlideTransition.RevealRight,
                CarouselGlEffectKind.WipeLeft => FlowerySlideTransition.WipeLeft,
                CarouselGlEffectKind.WipeRight => FlowerySlideTransition.WipeRight,
                CarouselGlEffectKind.WipeUp => FlowerySlideTransition.WipeUp,
                CarouselGlEffectKind.WipeDown => FlowerySlideTransition.WipeDown,
                CarouselGlEffectKind.Random => FlowerySlideTransition.Random,
                _ => FlowerySlideTransition.None
            };
        }

        private static FlowerySlideEffect ResolveSlideEffect(
            CarouselGlEffectKind effect,
            CarouselGlEffectKind overlayEffect,
            string mode)
        {
            if (mode == "text")
            {
                return FlowerySlideEffect.None;
            }

            if (!IsEffectKind(effect) && IsEffectKind(overlayEffect))
            {
                return MapEffectKind(overlayEffect);
            }

            return MapEffectKind(effect);
        }

        private static FlowerySlideEffect MapEffectKind(CarouselGlEffectKind kind)
        {
            return kind switch
            {
                CarouselGlEffectKind.EffectPanAndZoom => FlowerySlideEffect.PanAndZoom,
                CarouselGlEffectKind.EffectZoomIn => FlowerySlideEffect.ZoomIn,
                CarouselGlEffectKind.EffectZoomOut => FlowerySlideEffect.ZoomOut,
                CarouselGlEffectKind.EffectPanLeft => FlowerySlideEffect.PanLeft,
                CarouselGlEffectKind.EffectPanRight => FlowerySlideEffect.PanRight,
                CarouselGlEffectKind.EffectPanUp => FlowerySlideEffect.PanUp,
                CarouselGlEffectKind.EffectPanDown => FlowerySlideEffect.PanDown,
                CarouselGlEffectKind.EffectDrift => FlowerySlideEffect.Drift,
                CarouselGlEffectKind.EffectPulse => FlowerySlideEffect.Pulse,
                CarouselGlEffectKind.EffectBreath => FlowerySlideEffect.Breath,
                CarouselGlEffectKind.EffectThrow => FlowerySlideEffect.Throw,
                _ => FlowerySlideEffect.None
            };
        }

        private static bool IsEffectKind(CarouselGlEffectKind kind)
        {
            return kind is CarouselGlEffectKind.TextFill
                or CarouselGlEffectKind.EffectPanAndZoom
                or CarouselGlEffectKind.EffectZoomIn
                or CarouselGlEffectKind.EffectZoomOut
                or CarouselGlEffectKind.EffectPanLeft
                or CarouselGlEffectKind.EffectPanRight
                or CarouselGlEffectKind.EffectPanUp
                or CarouselGlEffectKind.EffectPanDown
                or CarouselGlEffectKind.EffectDrift
                or CarouselGlEffectKind.EffectPulse
                or CarouselGlEffectKind.EffectBreath
                or CarouselGlEffectKind.EffectThrow;
        }

        private static string BuildAssetsKey(IReadOnlyDictionary<string, string> assets)
        {
            if (assets.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("|", assets.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        }

        private sealed class SkiaEntry
        {
            public SkiaEntry(FrameworkElement host)
            {
                Host = host;
            }

            public FrameworkElement Host { get; }
            public DaisyCarousel? Carousel { get; set; }
            public Grid? HostPanel { get; set; }
            public TextBlock? DebugLabel { get; set; }
            public CarouselGlEffectDefinition Definition { get; set; } = null!;
            public CarouselGlEffectKind EffectKind { get; set; }
            public CarouselGlEffectKind OverlayEffectKind { get; set; }
            public string AssetsKey { get; set; } = string.Empty;
            public string Mode { get; set; } = string.Empty;
        }
    }
}
#endif

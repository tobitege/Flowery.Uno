using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
#if __WASM__ || HAS_UNO_WASM
using Uno.Foundation;
#endif

namespace Flowery.Effects
{
    public sealed class CarouselGlOverlayService
    {
        private static readonly Lazy<CarouselGlOverlayService> InstanceValue = new(() => new CarouselGlOverlayService());
        private readonly Dictionary<FrameworkElement, OverlayEntry> _entries = new();
        private readonly Dictionary<ScrollViewer, int> _scrollViewers = new();
        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(33);
        private DispatcherQueueTimer? _updateTimer;
        private bool _updatePending;

        public static CarouselGlOverlayService Instance => InstanceValue.Value;

        private CarouselGlOverlayService()
        {
        }

        public void Attach(FrameworkElement host, CarouselGlEffectKind effect)
        {
            Attach(host, effect, CarouselGlEffectKind.None);
        }

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

            var overlayVariant = ResolveOverlayVariant(definition, overlayEffect);
            var assetsJson = SerializeAssets(definition.Assets);

            if (_entries.TryGetValue(host, out var entry))
            {
                if (entry.Definition.Kind != definition.Kind
                    || !string.Equals(entry.AssetsJson, assetsJson, StringComparison.Ordinal)
                    || !string.Equals(entry.Variant, definition.Variant, StringComparison.Ordinal)
                    || !string.Equals(entry.OverlayVariant, overlayVariant, StringComparison.Ordinal))
                {
                    DetachOverlay(entry);
                    entry.LastRect = null;
                    entry.IsActive = false;
                }

                entry.Definition = definition;
                entry.AssetsJson = assetsJson;
                entry.Variant = definition.Variant;
                entry.OverlayVariant = overlayVariant;
            }
            else
            {
                entry = new OverlayEntry(host, definition)
                {
                    AssetsJson = assetsJson,
                    Variant = definition.Variant,
                    OverlayVariant = overlayVariant
                };
                _entries[host] = entry;
                AttachScrollViewer(entry);
            }

            if (host is CarouselGlEffectHost glHost)
            {
                entry.TransitionSeconds = glHost.TransitionSeconds;
                entry.PixelateSize = glHost.TransitionPixelateSize;
                entry.SliceCount = glHost.TransitionSliceCount;
                entry.StaggerSlices = glHost.TransitionStaggerSlices;
                entry.StaggerMs = glHost.TransitionSliceStaggerMs;
                entry.DissolveDensity = glHost.TransitionDissolveDensity;
                entry.FlipAngle = glHost.TransitionFlipAngle;
            }

            EnsureUpdateTimer(host);
            RequestUpdate();
        }

        public void SetIntervalSeconds(FrameworkElement host, double intervalSeconds)
        {
            if (!OperatingSystem.IsBrowser())
            {
                return;
            }

            if (!_entries.TryGetValue(host, out var entry))
            {
                return;
            }

            entry.IntervalSeconds = intervalSeconds;
            ApplyOverlayTiming(entry);
        }

        public void SetTransitionSeconds(FrameworkElement host, double transitionSeconds)
        {
            if (!OperatingSystem.IsBrowser())
            {
                return;
            }

            if (!_entries.TryGetValue(host, out var entry))
            {
                return;
            }

            entry.TransitionSeconds = transitionSeconds;
            if (host is CarouselGlEffectHost glHost)
            {
                entry.PixelateSize = glHost.TransitionPixelateSize;
                entry.SliceCount = glHost.TransitionSliceCount;
                entry.StaggerSlices = glHost.TransitionStaggerSlices;
                entry.StaggerMs = glHost.TransitionSliceStaggerMs;
                entry.DissolveDensity = glHost.TransitionDissolveDensity;
                entry.FlipAngle = glHost.TransitionFlipAngle;
            }
            ApplyOverlayTransitionSeconds(entry);
            ApplyOverlayTransitionParams(entry);
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

            DetachOverlay(entry);
            DetachScrollViewer(entry);
            RequestUpdate();
        }

        public void NotifyLayoutChanged(FrameworkElement host)
        {
            if (!OperatingSystem.IsBrowser())
            {
                return;
            }

            if (_entries.ContainsKey(host))
            {
                EnsureUpdateTimer(host);
                RequestUpdate();
            }
        }

        private void RequestUpdate()
        {
            if (_updateTimer == null)
            {
                UpdateOverlays();
                return;
            }

            _updatePending = true;
            if (!_updateTimer.IsRunning)
            {
                _updateTimer.Start();
            }
        }

        private void EnsureUpdateTimer(FrameworkElement host)
        {
            if (_updateTimer != null)
            {
                return;
            }

            var queue = host.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread() ?? Window.Current?.DispatcherQueue;
            if (queue == null)
            {
                return;
            }

            _updateTimer = queue.CreateTimer();
            _updateTimer.Interval = _updateInterval;
            _updateTimer.Tick += OnUpdateTick;
        }

        private void OnUpdateTick(DispatcherQueueTimer sender, object args)
        {
            if (_entries.Count == 0)
            {
                _updatePending = false;
                sender.Stop();
                return;
            }

            if (!_updatePending)
            {
                sender.Stop();
                return;
            }

            _updatePending = false;
            UpdateOverlays();
        }

        private void UpdateOverlays()
        {
            foreach (var entry in _entries.Values)
            {
                if (!entry.Host.IsLoaded)
                {
                    continue;
                }

                if (!TryGetElementRect(entry.Host, out var rect))
                {
                    SetOverlayActive(entry, false);
                    continue;
                }

                var hasViewport = TryGetViewportRect(entry.Host, out var viewport);
                var isVisible = !hasViewport || Intersects(viewport, rect);

                if (isVisible && (HasRectChanged(rect, entry.LastRect) || !entry.IsActive))
                {
                    AttachOverlay(entry, rect);
                    entry.LastRect = rect;
                }

                SetOverlayActive(entry, isVisible);
            }
        }

        private void AttachOverlay(OverlayEntry entry, Rect rect)
        {
            var safeKey = EscapeJsString(entry.OverlayId);
            var safeMode = EscapeJsString(entry.Definition.Mode);
            var assetsJson = entry.AssetsJson;
            var variantArg = entry.Variant != null
                ? $"'{EscapeJsString(entry.Variant)}'"
                : "null";
            var overlayVariantArg = entry.OverlayVariant != null
                ? $"'{EscapeJsString(entry.OverlayVariant)}'"
                : "null";
            var transitionSecondsArg = entry.TransitionSeconds > 0
                ? FormattableString.Invariant($"{entry.TransitionSeconds:0.###}")
                : "0";
            var pixelateSizeArg = entry.PixelateSize > 0
                ? entry.PixelateSize.ToString(CultureInfo.InvariantCulture)
                : "20";
            var sliceCountArg = entry.SliceCount > 0
                ? entry.SliceCount.ToString(CultureInfo.InvariantCulture)
                : "0";
            var staggerArg = entry.StaggerSlices ? "true" : "false";
            var staggerMsArg = entry.StaggerMs > 0
                ? entry.StaggerMs.ToString(CultureInfo.InvariantCulture)
                : "0";
            var dissolveDensityArg = entry.DissolveDensity.ToString(CultureInfo.InvariantCulture);
            var flipAngleArg = entry.FlipAngle.ToString(CultureInfo.InvariantCulture);
            var script = FormattableString.Invariant(
                $"if (window.FloweryCarouselGL && window.FloweryCarouselGL.attachOverlay) {{ window.FloweryCarouselGL.attachOverlay('{safeKey}', '{safeMode}', {rect.X:0.###}, {rect.Y:0.###}, {rect.Width:0.###}, {rect.Height:0.###}, {assetsJson}, {variantArg}, {overlayVariantArg}, {transitionSecondsArg}, {sliceCountArg}, {staggerArg}, {staggerMsArg}, {pixelateSizeArg}, {dissolveDensityArg}, {flipAngleArg}); }}");
            InvokeJs(script, "attach overlay");
            ApplyOverlayTiming(entry);
            ApplyOverlayTransitionSeconds(entry);
            ApplyOverlayTransitionParams(entry);
        }

        private static string? ResolveOverlayVariant(CarouselGlEffectDefinition primary, CarouselGlEffectKind overlayEffect)
        {
            if (overlayEffect == CarouselGlEffectKind.None)
            {
                return null;
            }

            if (string.Equals(primary.Mode, "effect", StringComparison.Ordinal)
                || string.Equals(primary.Mode, "text", StringComparison.Ordinal))
            {
                return null;
            }

            if (!CarouselGlEffectRegistry.TryGet(overlayEffect, out var overlayDefinition)
                || overlayDefinition is null)
            {
                return null;
            }

            return string.Equals(overlayDefinition.Mode, "effect", StringComparison.Ordinal)
                ? overlayDefinition.Variant
                : null;
        }

        private void SetOverlayActive(OverlayEntry entry, bool isActive)
        {
            if (entry.IsActive == isActive)
            {
                return;
            }

            entry.IsActive = isActive;
            var safeKey = EscapeJsString(entry.OverlayId);
            var activeFlag = isActive ? "true" : "false";
            var script = $"if (window.FloweryCarouselGL && window.FloweryCarouselGL.setOverlayActive) {{ window.FloweryCarouselGL.setOverlayActive('{safeKey}', {activeFlag}); }}";
            InvokeJs(script, "set overlay active");
        }

        private void DetachOverlay(OverlayEntry entry)
        {
            var safeKey = EscapeJsString(entry.OverlayId);
            var script = $"if (window.FloweryCarouselGL && window.FloweryCarouselGL.detachOverlay) {{ window.FloweryCarouselGL.detachOverlay('{safeKey}'); }}";
            InvokeJs(script, "detach overlay");
        }

        private static void ApplyOverlayTiming(OverlayEntry entry)
        {
            var safeKey = EscapeJsString(entry.OverlayId);
            var intervalSeconds = entry.IntervalSeconds;
            var script = FormattableString.Invariant(
                $"if (window.FloweryCarouselGL && window.FloweryCarouselGL.setOverlayTiming) {{ window.FloweryCarouselGL.setOverlayTiming('{safeKey}', {intervalSeconds:0.###}); }}");
            InvokeJs(script, "set overlay timing");
        }

        private static void ApplyOverlayTransitionSeconds(OverlayEntry entry)
        {
            var safeKey = EscapeJsString(entry.OverlayId);
            var transitionSeconds = entry.TransitionSeconds > 0 ? entry.TransitionSeconds : 0;
            var script = FormattableString.Invariant(
                $"if (window.FloweryCarouselGL && window.FloweryCarouselGL.setOverlayTransitionDuration) {{ window.FloweryCarouselGL.setOverlayTransitionDuration('{safeKey}', {transitionSeconds:0.###}); }}");
            InvokeJs(script, "set overlay transition");
        }

        private static void ApplyOverlayTransitionParams(OverlayEntry entry)
        {
            var safeKey = EscapeJsString(entry.OverlayId);
            var sliceCount = entry.SliceCount > 0 ? entry.SliceCount : 0;
            var stagger = entry.StaggerSlices ? "true" : "false";
            var staggerMs = entry.StaggerMs > 0 ? entry.StaggerMs : 0;
            var pixelateSize = entry.PixelateSize > 0 ? entry.PixelateSize : 20;
            var dissolveDensity = entry.DissolveDensity;
            var flipAngle = entry.FlipAngle;
            var script = FormattableString.Invariant(
                $"if (window.FloweryCarouselGL && window.FloweryCarouselGL.setOverlayTransitionParams) {{ window.FloweryCarouselGL.setOverlayTransitionParams('{safeKey}', {sliceCount}, {stagger}, {staggerMs:0.###}, {pixelateSize}, {dissolveDensity:0.###}, {flipAngle:0.###}); }}");
            InvokeJs(script, "set overlay transition params");
        }

        private void AttachScrollViewer(OverlayEntry entry)
        {
            var scrollViewer = FindScrollViewerAncestor(entry.Host);
            entry.ScrollViewer = scrollViewer;
            if (scrollViewer == null)
            {
                return;
            }

            if (_scrollViewers.TryGetValue(scrollViewer, out var count))
            {
                _scrollViewers[scrollViewer] = count + 1;
                return;
            }

            _scrollViewers[scrollViewer] = 1;
            scrollViewer.ViewChanged += OnScrollViewerViewChanged;
        }

        private void DetachScrollViewer(OverlayEntry entry)
        {
            if (entry.ScrollViewer == null)
            {
                return;
            }

            if (!_scrollViewers.TryGetValue(entry.ScrollViewer, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                entry.ScrollViewer.ViewChanged -= OnScrollViewerViewChanged;
                _scrollViewers.Remove(entry.ScrollViewer);
                return;
            }

            _scrollViewers[entry.ScrollViewer] = count - 1;
        }

        private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            RequestUpdate();
        }

        private static ScrollViewer? FindScrollViewerAncestor(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is ScrollViewer sv)
                {
                    return sv;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static bool TryGetElementRect(FrameworkElement element, out Rect rect)
        {
            rect = default;
            if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
            {
                return false;
            }

            var root = Window.Current?.Content;
            var transform = root != null ? element.TransformToVisual(root) : element.TransformToVisual(null);
            var origin = transform.TransformPoint(new Point(0, 0));
            rect = new Rect(origin, new Size(element.ActualWidth, element.ActualHeight));
            if (double.IsNaN(rect.X) || double.IsNaN(rect.Y) || double.IsInfinity(rect.X) || double.IsInfinity(rect.Y))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetViewportRect(FrameworkElement element, out Rect viewport)
        {
            viewport = default;
            var xamlRoot = element.XamlRoot;
            if (xamlRoot != null)
            {
                var size = xamlRoot.Size;
                if (size.Width > 0 && size.Height > 0)
                {
                    viewport = new Rect(0, 0, size.Width, size.Height);
                    return true;
                }
            }

            var bounds = Window.Current?.Bounds ?? default;
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                viewport = new Rect(0, 0, bounds.Width, bounds.Height);
                return true;
            }

            return false;
        }

        private static bool Intersects(Rect a, Rect b)
        {
            return a.Left < b.Right &&
                   a.Right > b.Left &&
                   a.Top < b.Bottom &&
                   a.Bottom > b.Top;
        }

        private static bool HasRectChanged(Rect rect, Rect? lastRect)
        {
            if (lastRect == null)
            {
                return true;
            }

            var previous = lastRect.Value;
            const double tolerance = 0.5;
            return Math.Abs(rect.X - previous.X) > tolerance ||
                   Math.Abs(rect.Y - previous.Y) > tolerance ||
                   Math.Abs(rect.Width - previous.Width) > tolerance ||
                   Math.Abs(rect.Height - previous.Height) > tolerance;
        }

        private static string EscapeJsString(string value)
        {
            return value.Replace("'", "\\'", StringComparison.Ordinal);
        }

        private static string SerializeAssets(IReadOnlyDictionary<string, string> assets)
        {
            if (assets.Count == 0)
            {
                return "null";
            }

            var builder = new StringBuilder();
            builder.Append('{');
            var first = true;
            foreach (var pair in assets)
            {
                if (!first)
                {
                    builder.Append(',');
                }
                first = false;
                builder.Append('"');
                builder.Append(EscapeJsonString(pair.Key));
                builder.Append("\":\"");
                builder.Append(EscapeJsonString(pair.Value));
                builder.Append('"');
            }
            builder.Append('}');
            return builder.ToString();
        }

        private static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length + 8);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    default:
                        if (ch < 32)
                        {
                            builder.Append("\\u00");
                            builder.Append(((int)ch).ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(ch);
                        }
                        break;
                }
            }

            return builder.ToString();
        }

        private static void InvokeJs(string script, string context)
        {
#if __WASM__ || HAS_UNO_WASM
            try
            {
                WebAssemblyRuntime.InvokeJS(script);
            }
            catch (Exception ex)
            {
                var text = $"[CarouselGL] InvokeJS failed ({context}): {ex.Message}";
                System.Diagnostics.Debug.WriteLine(text);
                Console.Error.WriteLine(text);
            }
#else
            _ = script;
            _ = context;
#endif
        }

        private sealed class OverlayEntry
        {
            public OverlayEntry(FrameworkElement host, CarouselGlEffectDefinition definition)
            {
                Host = host;
                Definition = definition;
                OverlayId = "carousel-gl-" + Guid.NewGuid().ToString("N");
            }

            public FrameworkElement Host { get; }

            public CarouselGlEffectDefinition Definition { get; set; }

            public string OverlayId { get; }

            public ScrollViewer? ScrollViewer { get; set; }

            public Rect? LastRect { get; set; }

            public bool IsActive { get; set; }

            public double IntervalSeconds { get; set; }

            public double TransitionSeconds { get; set; }

            public int PixelateSize { get; set; } = 20;

            public int SliceCount { get; set; }

            public bool StaggerSlices { get; set; } = true;

            public double StaggerMs { get; set; } = 50;

            public double DissolveDensity { get; set; } = 0.5;

            public double FlipAngle { get; set; } = 180;

            public string AssetsJson { get; set; } = "null";

            public string? Variant { get; set; }

            public string? OverlayVariant { get; set; }
        }
    }
}

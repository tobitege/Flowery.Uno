using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
#if __WASM__ || HAS_UNO_WASM
using Uno.Foundation;
#endif

namespace Flowery.Uno.Gallery.Effects
{
    public sealed class CarouselGlTransitionOverlayService
    {
        private static readonly Lazy<CarouselGlTransitionOverlayService> InstanceValue =
            new(() => new CarouselGlTransitionOverlayService());

        private readonly Dictionary<FrameworkElement, OverlayEntry> _entries = new();
        private readonly Dictionary<ScrollViewer, int> _scrollViewers = new();
        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(33);
        private DispatcherQueueTimer? _updateTimer;
        private bool _updatePending;

        public static CarouselGlTransitionOverlayService Instance => InstanceValue.Value;

        private CarouselGlTransitionOverlayService()
        {
        }

        public void Attach(FrameworkElement host, string? transitionName)
        {
            if (!OperatingSystem.IsBrowser())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(transitionName))
            {
                Detach(host);
                return;
            }

            if (_entries.TryGetValue(host, out var entry))
            {
                if (!string.Equals(entry.TransitionName, transitionName, StringComparison.Ordinal))
                {
                    entry.LastRect = null;
                    entry.IsActive = false;
                }

                entry.TransitionName = transitionName;
            }
            else
            {
                entry = new OverlayEntry(host)
                {
                    TransitionName = transitionName
                };
                _entries[host] = entry;
                AttachScrollViewer(entry);
            }

            if (host is CarouselGlTransitionHost transitionHost)
            {
                entry.IntervalSeconds = transitionHost.IntervalSeconds;
                entry.TransitionSeconds = transitionHost.TransitionSeconds;
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
            ApplyOverlayTransitionSeconds(entry);
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
            var transitionArg = !string.IsNullOrWhiteSpace(entry.TransitionName)
                ? $"'{EscapeJsString(entry.TransitionName)}'"
                : "null";
            var transitionSecondsArg = entry.TransitionSeconds > 0
                ? FormattableString.Invariant($"{entry.TransitionSeconds:0.###}")
                : "0";
            var intervalSecondsArg = entry.IntervalSeconds > 0
                ? FormattableString.Invariant($"{entry.IntervalSeconds:0.###}")
                : "0";

            var script = FormattableString.Invariant(
                $"if (window.FloweryCarouselGLTransitions && window.FloweryCarouselGLTransitions.attachOverlay) {{ window.FloweryCarouselGLTransitions.attachOverlay('{safeKey}', {rect.X:0.###}, {rect.Y:0.###}, {rect.Width:0.###}, {rect.Height:0.###}, null, {transitionArg}, {transitionSecondsArg}, {intervalSecondsArg}); }}");
            InvokeJs(script, "attach transition overlay");
            ApplyOverlayTiming(entry);
            ApplyOverlayTransitionSeconds(entry);
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
            var script = $"if (window.FloweryCarouselGLTransitions && window.FloweryCarouselGLTransitions.setOverlayActive) {{ window.FloweryCarouselGLTransitions.setOverlayActive('{safeKey}', {activeFlag}); }}";
            InvokeJs(script, "set transition overlay active");
        }

        private void DetachOverlay(OverlayEntry entry)
        {
            var safeKey = EscapeJsString(entry.OverlayId);
            var script = $"if (window.FloweryCarouselGLTransitions && window.FloweryCarouselGLTransitions.detachOverlay) {{ window.FloweryCarouselGLTransitions.detachOverlay('{safeKey}'); }}";
            InvokeJs(script, "detach transition overlay");
        }

        private static void ApplyOverlayTiming(OverlayEntry entry)
        {
            var safeKey = EscapeJsString(entry.OverlayId);
            var intervalSeconds = entry.IntervalSeconds;
            var script = FormattableString.Invariant(
                $"if (window.FloweryCarouselGLTransitions && window.FloweryCarouselGLTransitions.setOverlayTiming) {{ window.FloweryCarouselGLTransitions.setOverlayTiming('{safeKey}', {intervalSeconds:0.###}); }}");
            InvokeJs(script, "set transition overlay timing");
        }

        private static void ApplyOverlayTransitionSeconds(OverlayEntry entry)
        {
            var safeKey = EscapeJsString(entry.OverlayId);
            var transitionSeconds = entry.TransitionSeconds > 0 ? entry.TransitionSeconds : 0;
            var script = FormattableString.Invariant(
                $"if (window.FloweryCarouselGLTransitions && window.FloweryCarouselGLTransitions.setOverlayTransitionDuration) {{ window.FloweryCarouselGLTransitions.setOverlayTransitionDuration('{safeKey}', {transitionSeconds:0.###}); }}");
            InvokeJs(script, "set transition overlay duration");
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

        private static void InvokeJs(string script, string context)
        {
#if FLOWERY_GL_TRANSITIONS && (__WASM__ || HAS_UNO_WASM)
            try
            {
                WebAssemblyRuntime.InvokeJS(script);
            }
            catch (Exception ex)
            {
                var text = $"[CarouselGLTransitions] InvokeJS failed ({context}): {ex.Message}";
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
            public OverlayEntry(FrameworkElement host)
            {
                Host = host;
                OverlayId = "carousel-gl-transition-" + Guid.NewGuid().ToString("N");
            }

            public FrameworkElement Host { get; }

            public string OverlayId { get; }

            public string TransitionName { get; set; } = string.Empty;

            public ScrollViewer? ScrollViewer { get; set; }

            public Rect? LastRect { get; set; }

            public bool IsActive { get; set; }

            public double IntervalSeconds { get; set; }

            public double TransitionSeconds { get; set; }
        }
    }
}

// CsWinRT1028: Generic collections implementing WinRT interfaces - inherent to Uno Platform, not fixable via code
#pragma warning disable CsWinRT1028

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Services
{
    /// <summary>
    /// Provides opt-in automatic font scaling for Daisy controls based on window size.
    /// Set EnableScaling="True" on any container to make all child Daisy controls auto-scale.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;UserControl services:FloweryScaleManager.EnableScaling="True"&gt;
    ///     &lt;!-- All Daisy controls will auto-scale their text --&gt;
    ///     &lt;controls:DaisyInput Label="Street" /&gt;
    /// &lt;/UserControl&gt;
    /// </code>
    /// </example>
    public static class FloweryScaleManager
    {
        internal const double MaxSanityScaleFactor = 5.0;

        // Track windows and their scale factors
        private static readonly Dictionary<Window, WindowScaleTracker> _windowTrackers = [];

        private static bool _isEnabled = false;
        private static double? _overrideScaleFactor;

        /// <summary>
        /// Gets or sets whether global scaling is enabled. When disabled, scale factor defaults to 1.0.
        /// This can be toggled at runtime via the sidebar or programmatically.
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;

                _isEnabled = value;
                IsEnabledChanged?.Invoke(null, value);

                // Recalculate scale for all tracked windows
                foreach (var tracker in _windowTrackers.Values)
                {
                    tracker.ForceRecalculate();
                }
            }
        }

        /// <summary>
        /// Event raised when the IsEnabled property changes.
        /// </summary>
        public static event EventHandler<bool>? IsEnabledChanged;

        #region Attached Properties

        /// <summary>
        /// Enables automatic font scaling for all Daisy controls within this container.
        /// </summary>
        public static readonly DependencyProperty EnableScalingProperty =
            DependencyProperty.RegisterAttached(
                "EnableScaling",
                typeof(bool),
                typeof(FloweryScaleManager),
                new PropertyMetadata(false, OnEnableScalingChanged));

        /// <summary>
        /// The current scale factor (0.5 to 1.0) based on window size.
        /// This is automatically calculated and set when EnableScaling is true.
        /// </summary>
        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.RegisterAttached(
                "ScaleFactor",
                typeof(double),
                typeof(FloweryScaleManager),
                new PropertyMetadata(1.0));

        /// <summary>
        /// Event raised when the scale factor changes for any window.
        /// </summary>
        public static event EventHandler<ScaleChangedEventArgs>? ScaleFactorChanged;

        /// <summary>
        /// Event raised when scale configuration values change (e.g. MaxScaleFactor).
        /// This can be used by bindings (like ScaleExtension) to refresh without requiring a resize.
        /// </summary>
        public static event EventHandler? ScaleConfigChanged;

        #endregion

        #region Property Accessors

        public static bool GetEnableScaling(DependencyObject obj) => (bool)obj.GetValue(EnableScalingProperty);
        public static void SetEnableScaling(DependencyObject obj, bool value) => obj.SetValue(EnableScalingProperty, value);

        public static double GetScaleFactor(DependencyObject obj) => (double)obj.GetValue(ScaleFactorProperty);
        internal static void SetScaleFactor(DependencyObject obj, double value) => obj.SetValue(ScaleFactorProperty, value);

        #endregion

        #region Configuration (delegates to FloweryScaleConfig)

        /// <summary>
        /// Gets or sets the reference width for scale calculations (default: 1920).
        /// </summary>
        public static double ReferenceWidth
        {
            get => FloweryScaleConfig.ReferenceWidth;
            set => FloweryScaleConfig.ReferenceWidth = value;
        }

        /// <summary>
        /// Gets or sets the reference height for scale calculations (default: 1080).
        /// </summary>
        public static double ReferenceHeight
        {
            get => FloweryScaleConfig.ReferenceHeight;
            set => FloweryScaleConfig.ReferenceHeight = value;
        }

        /// <summary>
        /// Gets or sets the minimum scale factor (default: 0.6).
        /// </summary>
        public static double MinScaleFactor
        {
            get => FloweryScaleConfig.MinScaleFactor;
            set => FloweryScaleConfig.MinScaleFactor = value;
        }

        /// <summary>
        /// Gets or sets the maximum scale factor (default: 1.0).
        /// </summary>
        public static double MaxScaleFactor
        {
            get => FloweryScaleConfig.MaxScaleFactor;
            set
            {
                var clamped = value;
                if (double.IsNaN(clamped) || double.IsInfinity(clamped) || clamped <= 0)
                    clamped = 1.0;

                // Sanity cap: prevent extreme values (e.g. accidental 5000%).
                if (clamped > MaxSanityScaleFactor)
                    clamped = MaxSanityScaleFactor;

                if (Math.Abs(FloweryScaleConfig.MaxScaleFactor - clamped) < 0.001)
                    return;

                FloweryScaleConfig.MaxScaleFactor = clamped;
                ScaleConfigChanged?.Invoke(null, EventArgs.Empty);

                // Recalculate scale for all tracked windows.
                foreach (var tracker in _windowTrackers.Values)
                {
                    tracker.ForceRecalculate();
                }
            }
        }

        /// <summary>
        /// Gets or sets a manual scale factor override.
        /// When set, window-size based scaling is bypassed and this value is used instead.
        /// Set to null to use automatic scaling.
        /// </summary>
        public static double? OverrideScaleFactor
        {
            get => _overrideScaleFactor;
            set
            {
                double? clamped = value;
                if (clamped.HasValue)
                {
                    var v = clamped.Value;
                    if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0)
                        v = 1.0;

                    // Sanity cap: prevent extreme values (e.g. accidental 5000%).
                    if (v > MaxSanityScaleFactor)
                        v = MaxSanityScaleFactor;

                    clamped = v;
                }

                if (_overrideScaleFactor == clamped)
                    return;

                _overrideScaleFactor = clamped;
                ScaleConfigChanged?.Invoke(null, EventArgs.Empty);

                // Recalculate scale for all tracked windows.
                foreach (var tracker in _windowTrackers.Values)
                {
                    tracker.ForceRecalculate();
                }
            }
        }

        #endregion

        #region Event Handlers

        private static void OnEnableScalingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            if (e.NewValue is true)
            {
                // Subscribe to loaded event to find the window
                element.Loaded += OnElementLoaded;

                // If already loaded, set up tracking immediately
                if (element.IsLoaded)
                {
                    SetupWindowTracking(element);
                }
            }
            else
            {
                element.Loaded -= OnElementLoaded;
            }
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                SetupWindowTracking(element);
            }
        }

        #endregion

        #region Window Tracking

        /// <summary>
        /// The main window reference for scale tracking.
        /// Set this in your App.xaml.cs after creating the window.
        /// </summary>
        public static Window? MainWindow { get; set; }

        private static void SetupWindowTracking(FrameworkElement element)
        {
            // Try to find the parent window
            var window = GetParentWindow(element);
            if (window == null)
                return;

            // Check if we already have a tracker for this window
            if (!_windowTrackers.TryGetValue(window, out var tracker))
            {
                tracker = new WindowScaleTracker(window);
                _windowTrackers[window] = tracker;

                // Clean up when window closes
                window.Closed += (s, e) =>
                {
                    if (s is Window w && _windowTrackers.Remove(w, out var tracker))
                    {
                        tracker.Dispose();
                    }
                };
            }

            // Apply initial scale factor to this control and all its children
            var scaleFactor = tracker.CurrentScaleFactor;
            ApplyScaleToControlTree(element, scaleFactor);
        }

        /// <summary>
        /// Applies scale factor to a control and recursively to all children that implement IScalableControl.
        /// Only applies to controls within an EnableScaling region.
        /// </summary>
        private static void ApplyScaleToControlTree(DependencyObject visual, double scaleFactor)
        {
            if (visual is FrameworkElement element && GetEnableScaling(element))
            {
                SetScaleFactor(element, scaleFactor);

                // Call ApplyScaleFactor on controls that implement IScalableControl
                if (element is IScalableControl scalable)
                {
                    scalable.ApplyScaleFactor(scaleFactor);
                }
            }

            // Recurse to children
            var count = VisualTreeHelper.GetChildrenCount(visual);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(visual, i);
                ApplyScaleToControlTree(child, scaleFactor);
            }
        }

        private static Window? GetParentWindow(FrameworkElement element)
        {
            // In WinUI/Uno, Window is NOT part of the visual tree.
            // The Window contains the root element but isn't a DependencyObject ancestor.
            // We can try to get the Window from the element's XamlRoot, but the safest 
            // approach is to use MainWindow which should be set in App.xaml.cs.
            
            // Try to find the window via XamlRoot (works in some scenarios)
            try
            {
                var xamlRoot = element.XamlRoot;
                if (xamlRoot != null && MainWindow?.Content?.XamlRoot == xamlRoot)
                {
                    return MainWindow;
                }
            }
            catch
            {
                // XamlRoot access may fail in some scenarios
            }

            // Fallback to MainWindow
            return MainWindow;
        }

        /// <summary>
        /// Checks if a control or any of its ancestors has EnableScaling set to true.
        /// </summary>
        public static bool IsScalingEnabled(DependencyObject control)
        {
            var current = control;
            while (current != null)
            {
                if (GetEnableScaling(current))
                    return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        #endregion

        #region Scale Calculation

        /// <summary>
        /// Calculates the scale factor for a given window size.
        /// </summary>
        public static double CalculateScaleFactor(double width, double height)
        {
            if (width <= 0 || height <= 0)
                return 1.0;

            if (_overrideScaleFactor.HasValue)
            {
                var manual = SanitizeScaleFactor(_overrideScaleFactor.Value);
                if (manual > FloweryScaleConfig.MaxScaleFactor)
                    manual = FloweryScaleConfig.MaxScaleFactor;

                return manual;
            }

            var widthScale = width / FloweryScaleConfig.ReferenceWidth;
            var heightScale = height / FloweryScaleConfig.ReferenceHeight;

            // Use the smaller scale to ensure content fits
            var scale = Math.Min(widthScale, heightScale);

            return Math.Max(FloweryScaleConfig.MinScaleFactor, Math.Min(scale, FloweryScaleConfig.MaxScaleFactor));
        }

        internal static double CalculateScaleFactor(double width, double height, double renderScaling)
        {
            if (width <= 0 || height <= 0)
                return 1.0;

            var widthScale = width / FloweryScaleConfig.ReferenceWidth;
            var heightScale = height / FloweryScaleConfig.ReferenceHeight;

            var min = FloweryScaleConfig.MinScaleFactor;
            var maxPhysical = FloweryScaleConfig.MaxScaleFactor;
            var max = maxPhysical;

            // Treat MaxScaleFactor as a physical max. Window sizing/rendering uses DIPs, so convert the max to DIPs
            // to keep the physical cap consistent across Windows DPI scaling.
            if (renderScaling > 0)
            {
                max /= renderScaling;
            }

            if (_overrideScaleFactor.HasValue)
            {
                var manualPhysical = SanitizeScaleFactor(_overrideScaleFactor.Value);

                // Keep the manual override within the current physical max.
                if (manualPhysical > maxPhysical)
                    manualPhysical = maxPhysical;

                var manual = manualPhysical;
                if (renderScaling > 0)
                {
                    manual /= renderScaling;
                }

                return Math.Min(manual, max);
            }

            // Default: fit to the smaller dimension (keeps content from exploding on cramped windows).
            var fitScale = Math.Min(widthScale, heightScale);

            // When scaling-up is enabled, allow upscaling even if only one dimension exceeds the reference
            // by using the larger dimension for scale-up. This avoids "stuck at 1.0" behavior in browsers
            // where height is often limited by browser chrome.
            var scale = fitScale;
            if (maxPhysical > 1.0)
            {
                var upScale = Math.Max(widthScale, heightScale);
                if (upScale > 1.0)
                    scale = upScale;
            }

            return Math.Max(min, Math.Min(scale, max));
        }

        /// <summary>
        /// Applies a scale factor to a base value.
        /// </summary>
        public static double ApplyScale(double baseValue, double scaleFactor)
        {
            scaleFactor = SanitizeScaleFactor(scaleFactor);
            return baseValue * scaleFactor;
        }

        /// <summary>
        /// Applies a scale factor with a minimum value.
        /// </summary>
        public static double ApplyScale(double baseValue, double minValue, double scaleFactor)
        {
            scaleFactor = SanitizeScaleFactor(scaleFactor);
            var scaled = baseValue * scaleFactor;
            return Math.Max(scaled, minValue);
        }

        /// <summary>
        /// Gets a scaled value for a preset.
        /// </summary>
        public static double GetScaledValue(FloweryScalePreset preset, double scaleFactor)
        {
            var (baseValue, minValue) = FloweryScaleConfig.GetPresetValues(preset);
            if (minValue.HasValue)
                return ApplyScale(baseValue, minValue.Value, scaleFactor);
            return ApplyScale(baseValue, scaleFactor);
        }

        internal static double SanitizeScaleFactor(double scaleFactor)
        {
            if (double.IsNaN(scaleFactor) || double.IsInfinity(scaleFactor) || scaleFactor <= 0)
                return 1.0;

            return scaleFactor > MaxSanityScaleFactor ? MaxSanityScaleFactor : scaleFactor;
        }

        internal static void RaiseScaleFactorChanged(Window window, double scaleFactor)
        {
            ScaleFactorChanged?.Invoke(null, new ScaleChangedEventArgs(window, scaleFactor));
        }

        #endregion

        #region Window Scale Tracker

        private partial class WindowScaleTracker : IDisposable
        {
            private readonly Window _window;
            private bool _isDisposed;
            private DispatcherTimer? _debounceTimer;
            private double _pendingScaleFactor;

            // Debounce delay in milliseconds - prevents "snap back" during rapid resize events
            private const int DebounceDelayMs = 50;

            public double CurrentScaleFactor { get; private set; } = 1.0;

            public WindowScaleTracker(Window window)
            {
                _window = window;

                // Subscribe to size changes
                _window.SizeChanged += OnWindowSizeChanged;

                // Calculate initial scale (no debounce for initial)
                UpdateScaleFactorImmediate();
            }

            private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
            {
                if (_isDisposed) return;

                // Debounce rapid resize events
                ScheduleDebouncedUpdate(e.Size.Width, e.Size.Height);
            }

            private void ScheduleDebouncedUpdate(double width, double height)
            {
                // Get DPI scaling factor (XamlRoot.RasterizationScale)
                var rasterizationScale = _window.Content?.XamlRoot?.RasterizationScale ?? 1.0;
                _pendingScaleFactor = CalculateScaleFactor(width, height, rasterizationScale);

                if (_debounceTimer == null)
                {
                    _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DebounceDelayMs) };
                    _debounceTimer.Tick += OnDebounceTimerTick;
                }

                // Reset the timer on each event
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }

            private void OnDebounceTimerTick(object? sender, object e)
            {
                _debounceTimer?.Stop();
                if (_isDisposed) return;

                ApplyScaleFactor(_pendingScaleFactor);
            }

            /// <summary>
            /// Forces a recalculation of the scale factor (used when IsEnabled changes).
            /// </summary>
            public void ForceRecalculate()
            {
                if (_isDisposed) return;

                // Reset current to force update
                CurrentScaleFactor = -1;
                UpdateScaleFactorImmediate();
            }

            private void UpdateScaleFactorImmediate()
            {
                double newFactor;

                if (!IsEnabled)
                {
                    // When scaling is disabled, use factor of 1.0
                    newFactor = 1.0;
                }
                else
                {
                    // Get DPI scaling factor (XamlRoot.RasterizationScale)
                    var rasterizationScale = _window.Content?.XamlRoot?.RasterizationScale ?? 1.0;

                    // Get window bounds - use AppWindow if available
                    double width = 0, height = 0;
                    bool isPhysical = false;

                    try
                    {
                        var appWindow = _window.AppWindow;
                        if (appWindow != null)
                        {
                            width = appWindow.Size.Width;
                            height = appWindow.Size.Height;
                            isPhysical = true;
                        }
                    }
                    catch
                    {
                        // Fallback - try to get from content
                    }

                    if (width <= 0 || height <= 0)
                    {
                        if (_window.Content is FrameworkElement content)
                        {
                            width = content.ActualWidth;
                            height = content.ActualHeight;
                            isPhysical = false; // ActualWidth/Height are already logical
                        }
                    }

                    if (width <= 0 || height <= 0)
                    {
                        newFactor = 1.0;
                    }
                    else
                    {
                        // Normalize to logical pixels for consistent calculation
                        if (isPhysical && rasterizationScale > 0)
                        {
                            width /= rasterizationScale;
                            height /= rasterizationScale;
                        }

                        newFactor = CalculateScaleFactor(width, height, rasterizationScale);
                    }
                }

                ApplyScaleFactor(newFactor);
            }

            private void ApplyScaleFactor(double newFactor)
            {
                if (Math.Abs(newFactor - CurrentScaleFactor) > 0.001)
                {
                    CurrentScaleFactor = newFactor;

                    // Update all controls with EnableScaling in this window
                    _window.DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_window.Content is FrameworkElement root)
                        {
                            PropagateScaleFactor(root, newFactor);
                        }
                        RaiseScaleFactorChanged(_window, newFactor);
                    });
                }
            }

            private static void PropagateScaleFactor(DependencyObject visual, double scaleFactor)
            {
                if (visual is FrameworkElement element)
                {
                    // Only apply scaling to controls within an EnableScaling region
                    if (GetEnableScaling(element))
                    {
                        // Set the scale factor on all controls (for binding/styling purposes)
                        SetScaleFactor(element, scaleFactor);

                        // Call ApplyScaleFactor on controls that implement IScalableControl
                        if (element is IScalableControl scalable)
                        {
                            scalable.ApplyScaleFactor(scaleFactor);
                        }
                    }
                }

                // Recurse to children
                var count = VisualTreeHelper.GetChildrenCount(visual);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(visual, i);
                    PropagateScaleFactor(child, scaleFactor);
                }
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                _window.SizeChanged -= OnWindowSizeChanged;

                if (_debounceTimer != null)
                {
                    _debounceTimer.Stop();
                    _debounceTimer.Tick -= OnDebounceTimerTick;
                    _debounceTimer = null;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Event args for scale factor changes.
    /// </summary>
    public class ScaleChangedEventArgs(Window window, double scaleFactor) : EventArgs
    {
        public Window Window { get; } = window;
        public double ScaleFactor { get; } = scaleFactor;
    }
}

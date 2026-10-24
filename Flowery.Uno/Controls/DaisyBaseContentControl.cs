using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// Base class for Daisy controls that provides centralized lifecycle management,
    /// theme integration, size system hookup, and optional neumorphic visual effects.
    /// </summary>
    /// <remarks>
    /// <para><b>Benefits of inheriting from this class:</b></para>
    /// <list type="bullet">
    ///   <item>Automatic theme change subscription/unsubscription</item>
    ///   <item>Loaded-control registration for resolved size refreshes</item>
    ///   <item>Optional neumorphic effects via Composition API (no XAML)</item>
    ///   <item>Reduced boilerplate in derived controls</item>
    /// </list>
    /// <para><b>Migration:</b> Change <c>: ContentControl</c> to <c>: DaisyBaseContentControl</c>
    /// and remove manual Loaded/Unloaded event wiring for theme/size changes.</para>
    /// </remarks>
    public abstract partial class DaisyBaseContentControl : ContentControl, IDaisyNeumorphicTarget, IDaisySizeScope
    {
        private const string LogCategory = "Controls";
        private static bool _globalNeumorphicEnabled;
        private static DaisyNeumorphicMode _globalNeumorphicMode = DaisyNeumorphicMode.None;
        private static double _globalNeumorphicIntensity = 0.45;
        private static Color? _globalNeumorphicDarkShadowColor;
        private static Color? _globalNeumorphicLightShadowColor;
        private static DaisyNeumorphicMode _lastNonNoneMode = DaisyNeumorphicMode.Raised; // Remember last mode for toggle

        #region Static Configuration

        /// <summary>
        /// When true, all DaisyBaseContentControl-derived controls will have neumorphic effects enabled by default.
        /// Individual controls can still override via the <see cref="DaisyNeumorphic.IsEnabledProperty"/> attached property.
        /// </summary>
        public static bool GlobalNeumorphicEnabled
        {
            get => _globalNeumorphicEnabled;
            set
            {
                if (_globalNeumorphicEnabled == value)
                    return;

                _globalNeumorphicEnabled = value;

                // Enabled=true but Mode=None is a no-op. Restore the last remembered mode.
                // Set backing field directly to avoid double event firing.
                if (value && _globalNeumorphicMode == DaisyNeumorphicMode.None)
                {
                    _globalNeumorphicMode = _lastNonNoneMode;
                }

                GlobalNeumorphicChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Fired when global neumorphic settings change.
        /// </summary>
        public static event EventHandler? GlobalNeumorphicChanged;

        /// <summary>
        /// Default neumorphic mode when <see cref="GlobalNeumorphicEnabled"/> is true.
        /// </summary>
        public static DaisyNeumorphicMode GlobalNeumorphicMode
        {
            get => _globalNeumorphicMode;
            set
            {
                if (_globalNeumorphicMode == value)
                    return;

                // Remember the last non-None mode so we can restore it when re-enabling
                if (value != DaisyNeumorphicMode.None)
                {
                    _lastNonNoneMode = value;
                }

                _globalNeumorphicMode = value;

                // Sync enabled state: non-None implies enabled, None implies disabled.
                _globalNeumorphicEnabled = value != DaisyNeumorphicMode.None;

                GlobalNeumorphicChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Default neumorphic intensity (0.0 to 1.0) when <see cref="GlobalNeumorphicEnabled"/> is true.
        /// </summary>
        public static double GlobalNeumorphicIntensity
        {
            get => _globalNeumorphicIntensity;
            set
            {
                if (Math.Abs(_globalNeumorphicIntensity - value) < 0.001)
                    return;

                _globalNeumorphicIntensity = value;
                GlobalNeumorphicChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Default dark shadow color for neumorphic effects. <c>null</c> (the default) derives it from the
        /// current theme: its Base300 color, darkened by 50% for light themes and 70% for dark themes.
        /// A value set here survives theme changes.
        /// </summary>
        public static Color? GlobalNeumorphicDarkShadowColor
        {
            get => _globalNeumorphicDarkShadowColor;
            set
            {
                if (_globalNeumorphicDarkShadowColor == value)
                    return;

                _globalNeumorphicDarkShadowColor = value;
                GlobalNeumorphicChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Default light shadow color for neumorphic effects. <c>null</c> (the default) uses white.
        /// A value set here survives theme changes.
        /// </summary>
        public static Color? GlobalNeumorphicLightShadowColor
        {
            get => _globalNeumorphicLightShadowColor;
            set
            {
                if (_globalNeumorphicLightShadowColor == value)
                    return;

                _globalNeumorphicLightShadowColor = value;
                GlobalNeumorphicChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// When true, debug overlay visuals can be enabled by derived controls.
        /// </summary>
        public static bool EnableDebugOverlay { get; set; }

        // TEMP: Disable automatic neumorphic refresh to isolate hangs/freezes.
        internal static bool DisableNeumorphicAutoRefresh { get; set; } = false;

        /// <summary>
        /// Identifies the NeumorphicScopeEnabled attached property.
        /// When set on a parent element, it enables or disables neumorphic effects for the subtree.
        /// The nearest value wins over a control's own <see cref="NeumorphicEnabled"/>, but never enables
        /// rows, cells or segments that their container keeps flat.
        /// </summary>
        public static readonly DependencyProperty NeumorphicScopeEnabledProperty =
            DependencyProperty.RegisterAttached(
                "NeumorphicScopeEnabled",
                typeof(bool?),
                typeof(DaisyBaseContentControl),
                new PropertyMetadata(null, OnNeumorphicScopeChanged));

        public static void SetNeumorphicScopeEnabled(DependencyObject element, bool? value)
        {
            element.SetValue(NeumorphicScopeEnabledProperty, value);
        }

        public static bool? GetNeumorphicScopeEnabled(DependencyObject element)
        {
            return (bool?)element.GetValue(NeumorphicScopeEnabledProperty);
        }

        /// <summary>
        /// Identifies the NeumorphicScopeMode attached property.
        /// When set on a parent element, it sets the neumorphic mode for the subtree.
        /// The nearest value wins; a control's own <see cref="NeumorphicMode"/> still takes precedence.
        /// </summary>
        public static readonly DependencyProperty NeumorphicScopeModeProperty =
            DependencyProperty.RegisterAttached(
                "NeumorphicScopeMode",
                typeof(DaisyNeumorphicMode?),
                typeof(DaisyBaseContentControl),
                new PropertyMetadata(null, OnNeumorphicScopeChanged));

        public static void SetNeumorphicScopeMode(DependencyObject element, DaisyNeumorphicMode? value)
        {
            element.SetValue(NeumorphicScopeModeProperty, value);
        }

        public static DaisyNeumorphicMode? GetNeumorphicScopeMode(DependencyObject element)
        {
            return (DaisyNeumorphicMode?)element.GetValue(NeumorphicScopeModeProperty);
        }

        /// <summary>
        /// Identifies the NeumorphicScopeIntensity attached property.
        /// When set on a parent element, it sets the neumorphic intensity (0.0 to 1.0) for the subtree.
        /// The nearest value wins; a control's own <see cref="NeumorphicIntensity"/> still takes precedence.
        /// </summary>
        public static readonly DependencyProperty NeumorphicScopeIntensityProperty =
            DependencyProperty.RegisterAttached(
                "NeumorphicScopeIntensity",
                typeof(double?),
                typeof(DaisyBaseContentControl),
                new PropertyMetadata(null, OnNeumorphicScopeChanged));

        public static void SetNeumorphicScopeIntensity(DependencyObject element, double? value)
        {
            element.SetValue(NeumorphicScopeIntensityProperty, value);
        }

        public static double? GetNeumorphicScopeIntensity(DependencyObject element)
        {
            return (double?)element.GetValue(NeumorphicScopeIntensityProperty);
        }

        private static void OnNeumorphicScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RefreshNeumorphicInTree(d);
        }

        private static void RefreshNeumorphicInTree(DependencyObject root)
        {
            DaisyNeumorphicRefreshHelper.RefreshNeumorphicInTree(root);
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Click"/> event. Using 'new' to avoid conflict with Android View.Click.
        /// </summary>
#if __ANDROID__
        public new event EventHandler? Click;
#else
        public event EventHandler? Click;
#endif

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(System.Windows.Input.ICommand),
                typeof(DaisyBaseContentControl),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command to invoke when the control is clicked.
        /// </summary>
        public System.Windows.Input.ICommand? Command
        {
            get => (System.Windows.Input.ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(DaisyBaseContentControl),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="Command"/>.
        /// </summary>
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets whether neumorphic effects are enabled for this control.
        /// </summary>
        public bool? NeumorphicEnabled
        {
            get => DaisyNeumorphic.GetIsEnabled(this);
            set => DaisyNeumorphic.SetIsEnabled(this, value);
        }

        /// <summary>
        /// Gets or sets the neumorphic effect mode.
        /// </summary>
        public DaisyNeumorphicMode? NeumorphicMode
        {
            get => DaisyNeumorphic.GetMode(this);
            set => DaisyNeumorphic.SetMode(this, value);
        }

        /// <summary>
        /// Gets or sets the neumorphic effect intensity (0.0 to 1.0).
        /// </summary>
        public double? NeumorphicIntensity
        {
            get => DaisyNeumorphic.GetIntensity(this);
            set => DaisyNeumorphic.SetIntensity(this, value);
        }

        /// <summary>
        /// Gets or sets the blur radius for neumorphic shadows.
        /// </summary>
        public double NeumorphicBlurRadius
        {
            get => DaisyNeumorphic.GetBlurRadius(this);
            set => DaisyNeumorphic.SetBlurRadius(this, value);
        }

        /// <summary>
        /// Gets or sets the offset distance for neumorphic shadows.
        /// </summary>
        public double NeumorphicOffset
        {
            get => DaisyNeumorphic.GetOffset(this);
            set => DaisyNeumorphic.SetOffset(this, value);
        }

        /// <summary>
        /// Gets or sets the dark shadow color for neumorphic effects.
        /// </summary>
        public Color? NeumorphicDarkShadowColor
        {
            get => DaisyNeumorphic.GetDarkShadowColor(this);
            set => DaisyNeumorphic.SetDarkShadowColor(this, value);
        }

        /// <summary>
        /// Gets or sets the light shadow color for neumorphic effects.
        /// </summary>
        public Color? NeumorphicLightShadowColor
        {
            get => DaisyNeumorphic.GetLightShadowColor(this);
            set => DaisyNeumorphic.SetLightShadowColor(this, value);
        }

        #endregion

        #region State

        private bool _isLoaded;
        private bool _themeSubscribed;
        private readonly DaisyNeumorphicSurfaceSet _neumorphicSurfaces;
        private DaisyNeumorphicResolution? _neumorphicResolution;
        private bool _neumorphicTransparencyApplied;
        private object? _neumorphicOriginalBackgroundValue;
        private Brush? _neumorphicSurfaceBrush;
        private SolidColorBrush? _neumorphicTransparentBrush;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DaisyBaseContentControl"/> class.
        /// </summary>
        protected DaisyBaseContentControl()
        {
            _neumorphicSurfaces = new DaisyNeumorphicSurfaceSet(this);
            DaisyNeumorphicLifecycle.Attach(this, refreshOnThemeChange: true);
            if (UsesNeumorphicTransparencyProtocol)
            {
                RegisterPropertyChangedCallback(BackgroundProperty, OnBackgroundChangedForNeumorphic);
            }

            Loaded += OnBaseLoaded;
            Unloaded += OnBaseUnloaded;
            SizeChanged += OnBaseSizeChanged;
            FlowerySizeManager.Register(this, RefreshResolvedSize, GetResolvedSize);

            // Basic interaction logic for button-like behavior
            PointerPressed += (s, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) _isBasePressed = true; };
            PointerReleased += (s, e) => { if (_isBasePressed) { _isBasePressed = false; OnBaseClick(); } };
            PointerCaptureLost += (s, e) => _isBasePressed = false;
        }

        private bool _isBasePressed;

        protected virtual void OnBaseClick()
        {
            Click?.Invoke(this, EventArgs.Empty);

            if (Command != null && Command.CanExecute(CommandParameter))
            {
                Command.Execute(CommandParameter);
            }
        }

        #endregion

        #region Lifecycle Events

        private void OnBaseLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            if (!_themeSubscribed)
            {
                DaisyThemeManager.ThemeChanged += OnBaseThemeChanged;
                _themeSubscribed = true;
            }

            OnLoaded();

            // The neumorphic effect is applied after the first layout pass by DaisyNeumorphicLifecycle.
        }

        private void OnBaseUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            if (_themeSubscribed)
            {
                DaisyThemeManager.ThemeChanged -= OnBaseThemeChanged;
                _themeSubscribed = false;
            }

            RestoreNeumorphicHostBackground();

            _neumorphicSurfaces.Suspend();

            OnUnloaded();
        }

        private void OnBaseSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Handled by helper
        }

        private void OnBaseThemeChanged(object? sender, string themeName)
        {
            OnThemeChanged(themeName);
        }

        #endregion

        #region Virtual Hooks for Derived Classes

        /// <summary>
        /// Called when the control is loaded. Override to perform initialization.
        /// Theme subscriptions are already set up. ResolvedSize reflects the current sizing policy.
        /// </summary>
        protected virtual void OnLoaded()
        {
            OnControlLoaded();
        }

        /// <summary>
        /// Called when the control is unloaded. Override to perform cleanup.
        /// Theme subscriptions are already torn down. Size registration ends with the Unloaded event.
        /// </summary>
        protected virtual void OnUnloaded()
        {
            OnControlUnloaded();
        }

        /// <summary>
        /// Legacy hook kept for backward compatibility with existing derived controls.
        /// </summary>
        protected virtual void OnControlLoaded()
        {
        }

        /// <summary>
        /// Legacy hook kept for backward compatibility with existing derived controls.
        /// </summary>
        protected virtual void OnControlUnloaded()
        {
        }

        /// <summary>
        /// Called when the application theme changes.
        /// Override to update control colors/appearance.
        /// </summary>
        /// <param name="themeName">The new theme name.</param>
        protected virtual void OnThemeChanged(string themeName)
        {
        }

        /// <summary>
        /// Called when the manager refreshes the effective size.
        /// Override to update control dimensions.
        /// </summary>
        protected virtual void OnResolvedSizeChanged()
        {
            RequestNeumorphicRefresh();
        }

        /// <summary>
        /// Gets the control's current Size property value, if it has one.
        /// Override in derived classes to return the control's Size.
        /// The authored value remains unchanged when the global size changes.
        /// </summary>
        /// <returns>The current size, or null if the control doesn't have a Size property.</returns>
        protected virtual DaisySize? GetControlSize() => null;

        /// <summary>
        /// Gets the fallback size when Auto is opted out of global sizing.
        /// </summary>
        protected virtual DaisySize DefaultSize => DaisySize.Medium;

        // Composite controls can supply a default without assigning their children's Size.
        internal virtual DaisySize? ChildSizeOverride => null;

        DaisySize? IDaisySizeScope.ChildSizeOverride => ChildSizeOverride;

        internal virtual DaisySize? ChildSizeFallback => null;

        DaisySize? IDaisySizeScope.ChildSizeFallback => ChildSizeFallback;

        internal void RefreshResolvedSize() => OnResolvedSizeChanged();

        #endregion

        #region Neumorphic Effect Implementation

        /// <summary>
        /// Gets the element that draws the neumorphic effect for the default surface.
        /// Override to return the element that renders the control's visible surface.
        /// </summary>
        protected virtual FrameworkElement? GetNeumorphicHostElement()
        {
            return this;
        }

        /// <summary>
        /// Gets the surfaces that draw the neumorphic effect. The default is one surface on
        /// <see cref="GetNeumorphicHostElement"/>, which draws nothing without a visible background.
        /// Override for controls with several surfaces or surfaces with a fixed mode
        /// (e.g. an inset track with a raised handle).
        /// </summary>
        protected virtual IReadOnlyList<DaisyNeumorphicSurface> GetNeumorphicSurfaces()
        {
            return [new DaisyNeumorphicSurface(GetNeumorphicHostElement() ?? this)];
        }

        /// <summary>
        /// Called after every neumorphic refresh with whether the control draws the effect.
        /// Override to switch appearance details that depend on it (e.g. a transparent track in neumorphic mode).
        /// </summary>
        protected virtual void OnNeumorphicRefreshed(bool isEnabled)
        {
        }

        public void RefreshNeumorphicEffect()
        {
            if (DisableNeumorphicAutoRefresh)
            {
                return;
            }

            if (!_isLoaded) return;

            var surfaces = GetNeumorphicSurfaces();

            // One walk up the tree serves all three hooks; overrides still take part.
            bool isEnabled;
            DaisyNeumorphicMode mode;
            double intensity;
            _neumorphicResolution = DaisyNeumorphic.Resolve(this);
            try
            {
                isEnabled = IsNeumorphicEffectivelyEnabled();
                mode = GetEffectiveNeumorphicMode();
                intensity = GetEffectiveNeumorphicIntensity();
            }
            finally
            {
                _neumorphicResolution = null;
            }

            var elevation = DaisyResourceLookup.GetDefaultElevation(GetResolvedSize());

            // Apply transparency protocol only on Windows (Composition shadows need it),
            // and only when the control is its own surface and paints one.
            var useTransparentBackground = isEnabled
                && UsesNeumorphicTransparencyProtocol
                && surfaces.Any(surface => ReferenceEquals(surface.Host, this))
                && (_neumorphicTransparencyApplied || Background is not (null or SolidColorBrush { Color.A: 0 }));
            if (useTransparentBackground)
            {
                ApplyTransparentBackground();
            }
            else
            {
                RestoreNeumorphicHostBackground();
            }

            _neumorphicSurfaces.Update(
                surfaces,
                isEnabled,
                mode,
                intensity,
                elevation,
                _neumorphicTransparencyApplied ? _neumorphicSurfaceBrush : null);

            OnNeumorphicRefreshed(isEnabled);
        }

        internal void RequestNeumorphicRefresh()
        {
            if (DisableNeumorphicAutoRefresh || !_isLoaded)
                return;
            DaisyNeumorphicRefreshHelper.QueueRefresh(this);
        }

        void IDaisyNeumorphicTarget.RequestNeumorphicRefresh() => RequestNeumorphicRefresh();

        private DaisyNeumorphicResolution ResolveNeumorphic() => _neumorphicResolution ?? DaisyNeumorphic.Resolve(this);

        public void RebuildNeumorphicEffect()
        {
            if (!_isLoaded) return;

            _neumorphicSurfaces.Clear();
            RefreshNeumorphicEffect();
        }

        protected virtual bool IsNeumorphicEffectivelyEnabled()
        {
            return ResolveNeumorphic().IsEnabled;
        }

        /// <summary>
        /// Gets the effective neumorphic mode for this control.
        /// </summary>
        protected virtual DaisyNeumorphicMode GetEffectiveNeumorphicMode()
        {
            return ResolveNeumorphic().Mode;
        }

        /// <summary>
        /// Gets the effective neumorphic intensity for this control.
        /// </summary>
        protected virtual double GetEffectiveNeumorphicIntensity()
        {
            return ResolveNeumorphic().Intensity;
        }

        /// <summary>
        /// Windows (non-Skia) Composition shadows need the control that is its own surface to paint a transparent
        /// background; the surface brush is drawn behind it instead.
        /// </summary>
        private static bool UsesNeumorphicTransparencyProtocol =>
            PlatformCompatibility.IsWindows && !PlatformCompatibility.IsSkiaBackend;

        private void OnBackgroundChangedForNeumorphic(DependencyObject sender, DependencyProperty dp)
        {
            // A new background from the control or the app replaces the remembered surface brush.
            if (!ReferenceEquals(Background, _neumorphicTransparentBrush))
            {
                RequestNeumorphicRefresh();
            }
        }

        private void ApplyTransparentBackground()
        {
            if (_neumorphicTransparencyApplied && ReferenceEquals(Background, _neumorphicTransparentBrush))
            {
                return;
            }

            // First application, or the control replaced the transparent brush (e.g. on a theme change):
            // remember the background it paints now as the neumorphic surface.
            _neumorphicSurfaceBrush = Background;
            _neumorphicOriginalBackgroundValue = ReadLocalValue(BackgroundProperty);
            _neumorphicTransparentBrush ??= new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            SetValue(BackgroundProperty, _neumorphicTransparentBrush);
            _neumorphicTransparencyApplied = true;
        }

        private void RestoreNeumorphicHostBackground()
        {
            if (!_neumorphicTransparencyApplied)
            {
                return;
            }

            // A background the control set after the transparent one stays in place.
            if (ReferenceEquals(Background, _neumorphicTransparentBrush))
            {
                if (_neumorphicOriginalBackgroundValue == DependencyProperty.UnsetValue)
                {
                    ClearValue(BackgroundProperty);
                }
                else
                {
                    SetValue(BackgroundProperty, _neumorphicOriginalBackgroundValue);
                }
            }

            _neumorphicOriginalBackgroundValue = null;
            _neumorphicSurfaceBrush = null;
            _neumorphicTransparencyApplied = false;
        }

        #endregion

        #region Size System Integration

        /// <summary>
        /// Gets the effective size without changing the authored Size property.
        /// </summary>
        public DaisySize ResolvedSize => GetResolvedSize();

        protected DaisySize GetResolvedSize() =>
            FlowerySizeManager.ResolveSize(this, GetControlSize() ?? DaisySize.Auto, DefaultSize);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a themed brush from the current application resources.
        /// </summary>
        /// <param name="key">The resource key (e.g., "DaisyPrimaryBrush").</param>
        /// <param name="fallback">Fallback brush if not found.</param>
        /// <returns>The brush from resources, or the fallback.</returns>
        protected static Brush GetThemedBrush(string key, Brush? fallback = null)
        {
            return DaisyResourceLookup.GetBrush(key, fallback);
        }

        /// <summary>
        /// Gets a control-specific brush with lightweight styling support.
        /// </summary>
        /// <param name="controlName">The control name prefix (e.g., "DaisyButton").</param>
        /// <param name="suffix">The resource suffix (e.g., "Background").</param>
        /// <returns>The brush if found via lightweight styling, or null.</returns>
        protected Brush? TryGetControlBrush(string controlName, string suffix)
        {
            return DaisyResourceLookup.TryGetControlBrush(this, controlName, suffix);
        }

        /// <summary>
        /// Creates a themed border for derived controls.
        /// </summary>
        /// <param name="cornerRadius">The border corner radius.</param>
        /// <param name="borderThickness">The border thickness.</param>
        /// <returns>A themed Border instance.</returns>
        protected static Border CreateThemedBorder(CornerRadius cornerRadius, Thickness borderThickness)
        {
            return new Border
            {
                CornerRadius = cornerRadius,
                BorderThickness = borderThickness,
                Background = GetThemedBrush("DaisyBase200Brush"),
                BorderBrush = GetThemedBrush("DaisyBase300Brush")
            };
        }

        /// <summary>
        /// Applies a simple hover effect to a UIElement by switching its background brush.
        /// </summary>
        /// <param name="element">The element to apply the hover effect to.</param>
        /// <param name="normal">The normal background brush.</param>
        /// <param name="hover">The hover background brush.</param>
        protected void ApplyHoverEffect(UIElement element, Brush normal, Brush hover)
        {
            _ = new HoverEffectSubscription(element, normal, hover);
        }

        /// <summary>
        /// Wraps a UIElement with padding inside a Grid.
        /// </summary>
        /// <param name="element">The element to wrap.</param>
        /// <param name="padding">Padding to apply around the element.</param>
        /// <returns>A Grid containing the padded element.</returns>
        protected static Grid WrapWithPadding(UIElement element, Thickness padding)
        {
            var grid = new Grid();
            var border = new Border
            {
                Padding = padding,
                Child = element
            };
            grid.Children.Add(border);
            return grid;
        }

        private static void SetElementBackground(UIElement element, Brush brush)
        {
            switch (element)
            {
                case Control control:
                    control.Background = brush;
                    break;
                case Panel panel:
                    panel.Background = brush;
                    break;
                case Border border:
                    border.Background = brush;
                    break;
            }
        }

        private sealed class HoverEffectSubscription
        {
            private readonly UIElement _element;
            private readonly Brush _normal;
            private readonly Brush _hover;

            public HoverEffectSubscription(UIElement element, Brush normal, Brush hover)
            {
                _element = element;
                _normal = normal;
                _hover = hover;

                SetElementBackground(_element, _normal);

                _element.PointerEntered += OnPointerEntered;
                _element.PointerExited += OnPointerExited;
            }

            private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
            {
                SetElementBackground(_element, _hover);
            }

            private void OnPointerExited(object sender, PointerRoutedEventArgs e)
            {
                SetElementBackground(_element, _normal);
            }
        }

        #endregion
    }
}

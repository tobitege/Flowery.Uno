using System;
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
    ///   <item>Automatic global size change subscription/unsubscription</item>
    ///   <item>Optional neumorphic effects via Composition API (no XAML)</item>
    ///   <item>Reduced boilerplate in derived controls</item>
    /// </list>
    /// <para><b>Migration:</b> Change <c>: ContentControl</c> to <c>: DaisyBaseContentControl</c>
    /// and remove manual Loaded/Unloaded event wiring for theme/size changes.</para>
    /// </remarks>
    public abstract partial class DaisyBaseContentControl : ContentControl
    {
        private const string LogCategory = "Controls";
        private static bool _globalNeumorphicEnabled;
        private static DaisyNeumorphicMode _globalNeumorphicMode = DaisyNeumorphicMode.None;
        private static double _globalNeumorphicIntensity = 0.45;
        private static bool _globalNeumorphicRimLightEnabled = false;
        private static bool _globalNeumorphicSurfaceGradientEnabled = false;
        private static Color _globalNeumorphicDarkShadowColor = Color.FromArgb(255, 13, 39, 80); // #0D2750-ish
        private static Color _globalNeumorphicLightShadowColor = Color.FromArgb(255, 255, 255, 255); // #FFFFFF
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
        /// Default dark shadow color for neumorphic effects.
        /// </summary>
        public static Color GlobalNeumorphicDarkShadowColor
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
        /// Default light shadow color for neumorphic effects.
        /// </summary>
        public static Color GlobalNeumorphicLightShadowColor
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
        /// When true, a subtle rim light (inner highlight) is added to neumorphic effects.
        /// </summary>
        public static bool GlobalNeumorphicRimLightEnabled
        {
            get => _globalNeumorphicRimLightEnabled;
            set
            {
                if (_globalNeumorphicRimLightEnabled == value)
                    return;

                _globalNeumorphicRimLightEnabled = value;
                GlobalNeumorphicChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// When true, a subtle gradient is applied to the surface of neumorphic elements to enhance the 3D look.
        /// </summary>
        public static bool GlobalNeumorphicSurfaceGradientEnabled
        {
            get => _globalNeumorphicSurfaceGradientEnabled;
            set
            {
                if (_globalNeumorphicSurfaceGradientEnabled == value)
                    return;

                _globalNeumorphicSurfaceGradientEnabled = value;
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

        /// <summary>
        /// Gets or sets whether rim lighting is enabled for this control's neumorphic effect.
        /// </summary>
        public bool? NeumorphicRimLightEnabled
        {
            get => DaisyNeumorphic.GetRimLightEnabled(this);
            set => DaisyNeumorphic.SetRimLightEnabled(this, value);
        }

        /// <summary>
        /// Gets or sets whether a surface gradient is enabled for this control's neumorphic effect.
        /// </summary>
        public bool? NeumorphicSurfaceGradientEnabled
        {
            get => DaisyNeumorphic.GetSurfaceGradientEnabled(this);
            set => DaisyNeumorphic.SetSurfaceGradientEnabled(this, value);
        }

        #endregion

        #region Composition API Fields
        protected DaisyNeumorphicHelper? _neumorphicHelper;
        #endregion

        #region State

        private bool _isLoaded;
        private bool _themeSubscribed;
        private bool _sizeSubscribed;
        private bool _neumorphicSubscribed;
        private bool _neumorphicTransparencyApplied;
        private object? _neumorphicOriginalBackgroundValue;
        private FrameworkElement? _neumorphicHostElement;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DaisyBaseContentControl"/> class.
        /// </summary>
        protected DaisyBaseContentControl()
        {
            Loaded += OnBaseLoaded;
            Unloaded += OnBaseUnloaded;
            SizeChanged += OnBaseSizeChanged;

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

            if (!_sizeSubscribed && !FlowerySizeManager.ShouldIgnoreGlobalSize(this))
            {
                FlowerySizeManager.SizeChanged += OnBaseGlobalSizeChanged;
                _sizeSubscribed = true;

                if (FlowerySizeManager.UseGlobalSizeByDefault)
                {
                    ApplyGlobalSizeIfNotExplicit();
                }
            }

            if (!_neumorphicSubscribed)
            {
                GlobalNeumorphicChanged += OnBaseNeumorphicChanged;
                _neumorphicSubscribed = true;
            }

            OnLoaded();

            // Defer neumorphic attachment until the visual tree is fully laid out.
            // When Content is set in OnLoaded(), the ContentPresenter doesn't process
            // it immediately - it happens on the next layout pass. If we call
            // RefreshNeumorphicEffect() immediately, the helper will find an empty
            // visual tree and fall back to attaching to the ContentPresenter itself,
            // causing the composition surface to render ON TOP of the content.
            LayoutUpdated += OnFirstLayoutForNeumorphic;
        }

        private void OnFirstLayoutForNeumorphic(object? sender, object e)
        {
            LayoutUpdated -= OnFirstLayoutForNeumorphic;
            if (_isLoaded)
            {
                RequestNeumorphicRefresh();
            }
        }

        private void OnBaseUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            LayoutUpdated -= OnFirstLayoutForNeumorphic;

            if (_themeSubscribed)
            {
                DaisyThemeManager.ThemeChanged -= OnBaseThemeChanged;
                _themeSubscribed = false;
            }

            if (_sizeSubscribed)
            {
                FlowerySizeManager.SizeChanged -= OnBaseGlobalSizeChanged;
                _sizeSubscribed = false;
            }

            if (_neumorphicSubscribed)
            {
                GlobalNeumorphicChanged -= OnBaseNeumorphicChanged;
                _neumorphicSubscribed = false;
            }

            RestoreNeumorphicHostBackground();

            _neumorphicHelper?.Suspend();

            OnUnloaded();
        }

        private void OnBaseSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Handled by helper
        }

        private void OnBaseThemeChanged(object? sender, string themeName)
        {
            RequestNeumorphicRefresh();
            OnThemeChanged(themeName);
        }

        private void OnBaseNeumorphicChanged(object? sender, EventArgs e)
        {
            RequestNeumorphicRefresh();
        }

        private void OnBaseGlobalSizeChanged(object? sender, DaisySize size)
        {
            if (FlowerySizeManager.ShouldIgnoreGlobalSize(this))
                return;

            if (FlowerySizeManager.UseGlobalSizeByDefault)
            {
                OnGlobalSizeChanged(size);
            }
        }

        #endregion

        #region Virtual Hooks for Derived Classes

        /// <summary>
        /// Called when the control is loaded. Override to perform initialization.
        /// Theme and size subscriptions are already set up at this point.
        /// </summary>
        protected virtual void OnLoaded()
        {
            OnControlLoaded();
        }

        /// <summary>
        /// Called when the control is unloaded. Override to perform cleanup.
        /// Theme and size subscriptions are already torn down at this point.
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
        /// Called when the global size changes.
        /// Override to update control dimensions.
        /// </summary>
        /// <param name="size">The new global size.</param>
        protected virtual void OnGlobalSizeChanged(DaisySize size)
        {
        }

        /// <summary>
        /// Gets the control's current Size property value, if it has one.
        /// Override in derived classes to return the control's Size.
        /// Used for global size synchronization.
        /// </summary>
        /// <returns>The current size, or null if the control doesn't have a Size property.</returns>
        protected virtual DaisySize? GetControlSize() => null;

        /// <summary>
        /// Sets the control's Size property value, if it has one.
        /// Override in derived classes to set the control's Size.
        /// Used for global size synchronization.
        /// </summary>
        /// <param name="size">The size to set.</param>
        protected virtual void SetControlSize(DaisySize size)
        {
        }

        #endregion

        #region Neumorphic Effect Implementation

        protected virtual FrameworkElement? GetNeumorphicHostElement()
        {
            return this;
        }

        public void RefreshNeumorphicEffect()
        {
            if (DisableNeumorphicAutoRefresh)
            {
                return;
            }

            if (!_isLoaded) return;

            var hostElement = GetNeumorphicHostElement() ?? this;
            if (!ReferenceEquals(hostElement, _neumorphicHostElement))
            {
                RestoreNeumorphicHostBackground();
                _neumorphicHelper?.Dispose();
                _neumorphicHelper = null;
                _neumorphicHostElement = hostElement;
            }

            bool isEnabled = IsNeumorphicEffectivelyEnabled();
            var mode = GetEffectiveNeumorphicMode();

            // Apply transparency protocol only on Windows (Composition shadows need it)
            var useTransparentBackground = PlatformCompatibility.IsWindows
                && !PlatformCompatibility.IsSkiaBackend
                && ReferenceEquals(hostElement, this);
            if (isEnabled && useTransparentBackground)
            {
                ApplyTransparentBackground(hostElement);
            }
            else if (_neumorphicTransparencyApplied)
            {
                RestoreNeumorphicHostBackground();
            }

            if (!isEnabled && _neumorphicHelper == null)
            {
                return;
            }

            _neumorphicHelper ??= new DaisyNeumorphicHelper(hostElement);

            if (!ReferenceEquals(hostElement, this))
            {
                SyncNeumorphicOverrides(hostElement);
            }

            _neumorphicHelper.Update(
                isEnabled,
                mode,
                GetEffectiveNeumorphicIntensity(),
                DaisyResourceLookup.GetDefaultElevation(GetResolvedSize())
            );
        }

        internal void RequestNeumorphicRefresh()
        {
            if (DisableNeumorphicAutoRefresh || !_isLoaded)
                return;
            DaisyNeumorphicRefreshHelper.QueueRefresh(this);
        }

        public void RebuildNeumorphicEffect()
        {
            if (!_isLoaded) return;

            _neumorphicHelper?.Dispose();
            _neumorphicHelper = null;
            RefreshNeumorphicEffect();
        }


        protected virtual bool IsNeumorphicEffectivelyEnabled()
        {
            var scope = DaisyNeumorphic.GetScopeEnabled(this);
            bool isActuallyEnabled = (scope ?? NeumorphicEnabled ?? _globalNeumorphicEnabled);

            return isActuallyEnabled && GetEffectiveNeumorphicMode() != DaisyNeumorphicMode.None;
        }

        protected virtual bool IsNeumorphicRimLightEffectivelyEnabled()
        {
            return NeumorphicRimLightEnabled == true && GlobalNeumorphicRimLightEnabled;
        }

        /// <summary>
        /// Gets whether surface gradient is effectively enabled for this control.
        /// </summary>
        protected virtual bool IsNeumorphicSurfaceGradientEffectivelyEnabled()
        {
            return NeumorphicSurfaceGradientEnabled == true && GlobalNeumorphicSurfaceGradientEnabled;
        }

        /// <summary>
        /// Gets the effective neumorphic mode for this control.
        /// </summary>
        protected virtual DaisyNeumorphicMode GetEffectiveNeumorphicMode()
        {
            return NeumorphicMode ?? GlobalNeumorphicMode;
        }

        /// <summary>
        /// Gets the effective neumorphic intensity for this control.
        /// </summary>
        protected virtual double GetEffectiveNeumorphicIntensity()
        {
            return Math.Clamp(NeumorphicIntensity ?? GlobalNeumorphicIntensity, 0.0, 1.0);
        }

        private void SyncNeumorphicOverrides(FrameworkElement hostElement)
        {
            SyncNeumorphicOverride(hostElement, DaisyNeumorphic.OffsetProperty, DaisyNeumorphic.GetOffset(this));
            SyncNeumorphicOverride(hostElement, DaisyNeumorphic.BlurRadiusProperty, DaisyNeumorphic.GetBlurRadius(this));
            SyncNeumorphicOverride(hostElement, DaisyNeumorphic.DarkShadowColorProperty, DaisyNeumorphic.GetDarkShadowColor(this));
            SyncNeumorphicOverride(hostElement, DaisyNeumorphic.LightShadowColorProperty, DaisyNeumorphic.GetLightShadowColor(this));
        }

        private void SyncNeumorphicOverride(FrameworkElement hostElement, DependencyProperty property, object? value)
        {
            if (ShouldApplyNeumorphicOverride(property, value))
            {
                hostElement.SetValue(property, value);
            }
            else
            {
                hostElement.ClearValue(property);
            }
        }

        private bool ShouldApplyNeumorphicOverride(DependencyProperty property, object? value)
        {
            if (ReadLocalValue(property) != DependencyProperty.UnsetValue)
            {
                return true;
            }

            var metadata = property.GetMetadata(GetType());
            var defaultValue = metadata?.DefaultValue;
            if (defaultValue == null)
            {
                return value != null;
            }

            return !Equals(value, defaultValue);
        }

        private void ApplyTransparentBackground(FrameworkElement hostElement)
        {
            if (_neumorphicTransparencyApplied)
            {
                return;
            }

            var backgroundProperty = GetBackgroundProperty(hostElement);
            if (backgroundProperty == null)
            {
                return;
            }

            _neumorphicOriginalBackgroundValue = hostElement.ReadLocalValue(backgroundProperty);
            hostElement.SetValue(backgroundProperty, new SolidColorBrush(Microsoft.UI.Colors.Transparent));
            _neumorphicTransparencyApplied = true;
        }

        private void RestoreNeumorphicHostBackground()
        {
            if (!_neumorphicTransparencyApplied || _neumorphicHostElement == null)
            {
                return;
            }

            var backgroundProperty = GetBackgroundProperty(_neumorphicHostElement);
            if (backgroundProperty == null)
            {
                _neumorphicOriginalBackgroundValue = null;
                _neumorphicTransparencyApplied = false;
                return;
            }

            if (_neumorphicOriginalBackgroundValue == DependencyProperty.UnsetValue)
            {
                _neumorphicHostElement.ClearValue(backgroundProperty);
            }
            else
            {
                _neumorphicHostElement.SetValue(backgroundProperty, _neumorphicOriginalBackgroundValue);
            }

            _neumorphicOriginalBackgroundValue = null;
            _neumorphicTransparencyApplied = false;
        }

        private static DependencyProperty? GetBackgroundProperty(FrameworkElement element)
        {
            return element switch
            {
                Control => Control.BackgroundProperty,
                Panel => Panel.BackgroundProperty,
                Border => Border.BackgroundProperty,
                _ => null
            };
        }

        #endregion

        #region Size System Integration

        /// <summary>
        /// Applies the global size to this control if no explicit Size was set via XAML.
        /// </summary>
        private void ApplyGlobalSizeIfNotExplicit()
        {
            var currentSize = GetControlSize();
            if (currentSize == null)
                return;

            var sizeProperty = TryGetSizeProperty();
            if (sizeProperty != null && ReadLocalValue(sizeProperty) != DependencyProperty.UnsetValue)
                return;

            SetControlSize(FlowerySizeManager.CurrentSize);
        }

        /// <summary>
        /// Gets the resolved size for this control, respecting the IgnoreGlobalSize attached property.
        /// </summary>
        public DaisySize ResolvedSize => GetResolvedSize();

        protected DaisySize GetResolvedSize()
        {
            var controlSize = GetControlSize();
            return controlSize ?? FlowerySizeManager.CurrentSize;
        }

        /// <summary>
        /// Attempts to get the SizeProperty DependencyProperty from the derived type.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
            "Trimming",
            "IL2075",
            Justification = "SizeProperty lookup is optional and guarded; controls without it fallback safely.")]
        private DependencyProperty? TryGetSizeProperty()
        {
            return GetType().GetField(
                    "SizeProperty",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
                ?.GetValue(null) as DependencyProperty;
        }

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

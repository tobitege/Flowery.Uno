using System;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace Flowery.Controls
{
    /// <summary>
    /// A toggle control styled after DaisyUI's Toggle component (Uno/WinUI).
    /// Features neutral track + moving knob; knob color varies by Variant when checked.
    /// </summary>
    public partial class DaisyToggle : ToggleButton
    {
        private Border? _switchArea;
        private Ellipse? _knob;
        private TranslateTransform? _knobTransform;

        private bool _isSyncing;
        private bool _isApplyingForeground;
        private bool _hasForegroundOverride;
        private bool _foregroundOverrideInitialized;
        private long _foregroundCallbackToken;
        private readonly DaisyControlLifecycle _lifecycle;

        public DaisyToggle()
        {
            DefaultStyleKey = typeof(DaisyToggle);

            _lifecycle = new DaisyControlLifecycle(
                this,
                () => ApplyAll(animateKnob: false),
                () => Size,
                s => Size = s,
                handleLifecycleEvents: false,
                subscribeSizeChanges: false);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Checked += OnCheckedChanged;
            Unchecked += OnCheckedChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _foregroundCallbackToken = RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundChanged);
            EnsureForegroundOverrideInitialized();

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            SyncIsCheckedFromIsOn();
            _lifecycle.HandleLoaded();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleUnloaded();

            if (_foregroundCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(ForegroundProperty, _foregroundCallbackToken);
                _foregroundCallbackToken = 0;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _switchArea = GetTemplateChild("SwitchArea") as Border;
            _knob = GetTemplateChild("Knob") as Ellipse;
            _knobTransform = GetTemplateChild("KnobTransform") as TranslateTransform;

            EnsureForegroundOverrideInitialized();
            ApplyAll(animateKnob: false);
        }

        private void OnCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing)
                return;

            SyncIsOnFromIsChecked();
            ApplyAll(animateKnob: true);

            Toggled?.Invoke(this, e);
        }

        #region IsOn
        public static readonly DependencyProperty IsOnProperty =
            DependencyProperty.Register(
                nameof(IsOn),
                typeof(bool),
                typeof(DaisyToggle),
                new PropertyMetadata(false, OnIsOnChanged));

        /// <summary>
        /// Whether the toggle is on.
        /// </summary>
        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyToggle toggle)
            {
                if (toggle._isSyncing)
                    return;

                toggle.SyncIsCheckedFromIsOn();
                toggle.ApplyAll(animateKnob: false);
            }
        }
        #endregion

        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyToggleVariant),
                typeof(DaisyToggle),
                new PropertyMetadata(DaisyToggleVariant.Default, OnAppearanceChanged));

        /// <summary>
        /// The color variant of the toggle.
        /// </summary>
        public DaisyToggleVariant Variant
        {
            get => (DaisyToggleVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyToggle),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// The size of the toggle.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region TogglePadding
        public static readonly DependencyProperty TogglePaddingProperty =
            DependencyProperty.Register(
                nameof(TogglePadding),
                typeof(double),
                typeof(DaisyToggle),
                new PropertyMetadata(2.0, OnAppearanceChanged));

        /// <summary>
        /// The internal padding of the toggle knob area.
        /// </summary>
        public double TogglePadding
        {
            get => (double)GetValue(TogglePaddingProperty);
            set => SetValue(TogglePaddingProperty, value);
        }
        #endregion

        #region Header
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(DaisyToggle),
                new PropertyMetadata(null));

        /// <summary>
        /// Header content for the toggle.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
        #endregion

        #region OnContent
        public static readonly DependencyProperty OnContentProperty =
            DependencyProperty.Register(
                nameof(OnContent),
                typeof(object),
                typeof(DaisyToggle),
                new PropertyMetadata(null));

        public object? OnContent
        {
            get => GetValue(OnContentProperty);
            set => SetValue(OnContentProperty, value);
        }
        #endregion

        #region OffContent
        public static readonly DependencyProperty OffContentProperty =
            DependencyProperty.Register(
                nameof(OffContent),
                typeof(object),
                typeof(DaisyToggle),
                new PropertyMetadata(null));

        public object? OffContent
        {
            get => GetValue(OffContentProperty);
            set => SetValue(OffContentProperty, value);
        }
        #endregion

        /// <summary>
        /// Raised when the toggle state changes (compat with ToggleSwitch semantics).
        /// </summary>
        public event RoutedEventHandler? Toggled;

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyToggle toggle)
                toggle.ApplyAll(animateKnob: false);
        }

        private void SyncIsCheckedFromIsOn()
        {
            _isSyncing = true;
            try
            {
                IsChecked = IsOn;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void SyncIsOnFromIsChecked()
        {
            _isSyncing = true;
            try
            {
                SetValue(IsOnProperty, IsChecked == true);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void ApplyAll(bool animateKnob)
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            ApplySizing(resources);
            ApplyTheme(resources);
            ApplyKnobPosition(animateKnob);
        }

        private void ApplySizing(ResourceDictionary? resources)
        {
            if (_switchArea == null || _knob == null)
                return;

            _switchArea.Width = DaisyResourceLookup.GetSizeDouble(resources, "DaisyToggle", Size, "Width", fallback: 48d);
            _switchArea.Height = DaisyResourceLookup.GetSizeDouble(resources, "DaisyToggle", Size, "Height", fallback: 24d);
            _switchArea.CornerRadius = new CornerRadius(_switchArea.Height / 2);

            _knob.Width = DaisyResourceLookup.GetSizeDouble(resources, "DaisyToggleKnob", Size, "Size", fallback: 20d);
            _knob.Height = _knob.Width;
            // No margin needed - vertical centering via VerticalAlignment="Center" in XAML,
            // horizontal positioning via TranslateTransform.X

            // Apply font size to toggle (affects header via ContentPresenter)
            FontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
        }

        private void ApplyTheme(ResourceDictionary? resources)
        {
            if (_switchArea == null || _knob == null)
                return;

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyToggleVariant.Primary => "Primary",
                DaisyToggleVariant.Secondary => "Secondary",
                DaisyToggleVariant.Accent => "Accent",
                DaisyToggleVariant.Success => "Success",
                DaisyToggleVariant.Warning => "Warning",
                DaisyToggleVariant.Info => "Info",
                DaisyToggleVariant.Error => "Error",
                _ => ""
            };

            // Check for lightweight styling overrides
            var trackBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyToggle", "TrackBackground");
            var trackBorderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyToggle", "TrackBorderBrush");

            // Track uses Base300 for background, BaseContent for border to ensure visibility
            _switchArea.Background = trackBgOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", fallback: new SolidColorBrush(Microsoft.UI.Colors.Gray));
            _switchArea.BorderBrush = trackBorderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.Gray));
            _switchArea.BorderThickness = new Thickness(1);

            if (IsChecked == true)
            {
                // Check for knob brush override (variant-specific or generic)
                var knobOverride = !string.IsNullOrEmpty(variantName)
                    ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyToggle", $"{variantName}KnobBrush")
                    : null;
                knobOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyToggle", "KnobBrush");

                if (knobOverride != null)
                {
                    _knob.Fill = knobOverride;
                }
                else
                {
                    _knob.Fill = Variant switch
                    {
                        DaisyToggleVariant.Primary => DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                        DaisyToggleVariant.Secondary => DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                        DaisyToggleVariant.Accent => DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                        DaisyToggleVariant.Success => DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                        DaisyToggleVariant.Warning => DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                        DaisyToggleVariant.Info => DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                        DaisyToggleVariant.Error => DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                        _ => DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.Black))
                    };
                }
            }
            else
            {
                // Unchecked knob: check for override, else use Base100
                var uncheckedKnobOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyToggle", "KnobBrushUnchecked");
                _knob.Fill = uncheckedKnobOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White));
            }

            var labelBrush = _hasForegroundOverride
                ? Foreground
                : GetAppBrush("DaisyBaseContentBrush");
            if (!_hasForegroundOverride)
            {
                SetForeground(labelBrush);
            }
        }

        private void ApplyKnobPosition(bool animate)
        {
            if (_switchArea == null || _knob == null || _knobTransform == null)
                return;

            var padding = GetEffectivePadding();
            // Unchecked: knob left edge at padding offset from left
            // Checked: knob right edge at padding offset from right
            var targetX = IsChecked == true
                ? Math.Max(0, _switchArea.Width - _knob.Width - padding)
                : padding;

            if (!animate)
            {
                _knobTransform.X = targetX;
                return;
            }

            // Simple animation (matches Avalonia ~200ms knob movement)
            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            var anim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                To = targetX,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(anim, _knobTransform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(anim, "X");
            storyboard.Children.Add(anim);
            storyboard.Begin();
        }

        private double GetEffectivePadding()
        {
            // Size-specific padding for Large while still allowing explicit overrides.
            if (ReadLocalValue(TogglePaddingProperty) == DependencyProperty.UnsetValue)
            {
                return Size == DaisySize.Large ? 3.0 : 2.0;
            }

            return TogglePadding;
        }


        private void OnForegroundChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_isApplyingForeground)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            ApplyAll(animateKnob: false);
        }

        private void EnsureForegroundOverrideInitialized()
        {
            if (_foregroundOverrideInitialized)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            _foregroundOverrideInitialized = true;
        }

        private void SetForeground(Brush brush)
        {
            if (ReferenceEquals(Foreground, brush))
                return;

            _isApplyingForeground = true;
            try
            {
                Foreground = brush;
            }
            finally
            {
                _isApplyingForeground = false;
            }
        }

        private static Brush GetAppBrush(string key)
        {
            var resources = Application.Current?.Resources;
            if (resources != null && resources.TryGetValue(key, out var value) && value is Brush brush)
                return brush;

            return DaisyResourceLookup.GetBrush(key);
        }
    }
}

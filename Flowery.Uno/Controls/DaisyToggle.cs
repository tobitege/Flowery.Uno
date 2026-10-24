using System;
using Flowery.Helpers;
using Flowery.Theming;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Flowery.Controls
{
    /// <summary>
    /// A toggle control styled after DaisyUI's Toggle component (Uno/WinUI).
    /// Features neutral track + moving knob; knob color varies by Variant when checked.
    /// </summary>
    public partial class DaisyToggle : ToggleButton, IDaisyNeumorphicTarget
    {
        // A clearly visible CIE76 color difference. A luminance ratio such as WCAG 3:1 would change most variant
        // knobs in the bundled themes, because their colors share the track's lightness and differ in hue.
        private const double MinimumTrackDifference = 20;
        private const double KnobOutlineOpacity = 0.5;

        private Border? _switchArea;
        private Grid? _trackSurface;
        private Border? _knob;
        private TranslateTransform? _knobTransform;

        private bool _isSyncing;
        private bool _isApplyingForeground;
        private bool _hasForegroundOverride;
        private bool _foregroundOverrideInitialized;
        private bool _hasFontSizeOverride;
        private bool _fontSizeOverrideInitialized;
        private bool _isApplyingFontSize;
        private long _foregroundCallbackToken;
        private long _fontSizeCallbackToken;
        private readonly DaisyControlLifecycle _lifecycle;
        private readonly DaisyNeumorphicParticipant _neumorphic;

        public DaisyToggle()
        {
            DefaultStyleKey = typeof(DaisyToggle);

            _neumorphic = DaisyNeumorphicParticipant.Create(this, GetNeumorphicSurfaces, () => ResolvedSize);

            _lifecycle = new DaisyControlLifecycle(
                this,
                () => ApplyAll(animateKnob: false),
                handleLifecycleEvents: false);
            FlowerySizeManager.Register(this, () => ApplyAll(animateKnob: false), () => ResolvedSize);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Checked += OnCheckedChanged;
            Unchecked += OnCheckedChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _foregroundCallbackToken = RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundChanged);
            _fontSizeCallbackToken = RegisterPropertyChangedCallback(FontSizeProperty, OnFontSizeChanged);
            EnsureForegroundOverrideInitialized();
            EnsureFontSizeOverrideInitialized();

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

            if (_fontSizeCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(FontSizeProperty, _fontSizeCallbackToken);
                _fontSizeCallbackToken = 0;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _switchArea = GetTemplateChild("SwitchArea") as Border;
            _trackSurface = GetTemplateChild("PART_TrackSurface") as Grid;
            _knob = GetTemplateChild("Knob") as Border;
            _knobTransform = GetTemplateChild("KnobTransform") as TranslateTransform;

            EnsureForegroundOverrideInitialized();
            EnsureFontSizeOverrideInitialized();
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
                new PropertyMetadata(DaisySize.Auto, OnAppearanceChanged));

        /// <summary>
        /// The size of the toggle.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public DaisySize ResolvedSize => FlowerySizeManager.ResolveSize(this, Size);
        #endregion

        #region TogglePadding
        public static readonly DependencyProperty TogglePaddingProperty =
            DependencyProperty.Register(
                nameof(TogglePadding),
                typeof(double),
                typeof(DaisyToggle),
                new PropertyMetadata(double.NaN, OnAppearanceChanged));

        /// <summary>
        /// The horizontal gap between the knob and the track border.
        /// <see cref="double.NaN"/> (the default) centers the knob with the same gap on every side.
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
            _neumorphic.RequestRefresh();
        }

        private void ApplySizing(ResourceDictionary? resources)
        {
            if (_switchArea == null || _knob == null)
                return;

            _switchArea.Width = DaisyResourceLookup.GetSizeDouble(resources, "DaisyToggle", ResolvedSize, "Width", fallback: 40d);
            _switchArea.Height = DaisyResourceLookup.GetSizeDouble(resources, "DaisyToggle", ResolvedSize, "Height", fallback: 20d);
            _switchArea.CornerRadius = new CornerRadius(_switchArea.Height / 2);

            _knob.Width = DaisyResourceLookup.GetSizeDouble(resources, "DaisyToggleKnob", ResolvedSize, "Size", fallback: 12d);
            _knob.Height = _knob.Width;
            _knob.CornerRadius = new CornerRadius(_knob.Width / 2);

            // The inset layer fills the area inside the track's 1 px border.
            if (_trackSurface != null)
            {
                _trackSurface.CornerRadius = new CornerRadius(Math.Max(0, _switchArea.Height / 2 - 1));
            }
            // No margin needed - vertical centering via VerticalAlignment="Center" in XAML,
            // horizontal positioning via TranslateTransform.X

            if (!_hasFontSizeOverride)
            {
                SetFontSize(DaisyResourceLookup.GetSizeDouble(resources, "DaisyToggle", ResolvedSize, "FontSize",
                    DaisyResourceLookup.GetDefaultFontSize(ResolvedSize)));
            }

            // The track has an explicit size, so the toggle already measures at least the track. A minimum
            // written here becomes a local value and would stick after the first size (#70). The 44 px touch
            // target on mobile does not depend on Size, so setting it once is correct there.
            if (PlatformCompatibility.IsMobile)
            {
                if (ReadLocalValue(MinHeightProperty) == DependencyProperty.UnsetValue)
                    MinHeight = 44d;

                if (ReadLocalValue(MinWidthProperty) == DependencyProperty.UnsetValue)
                    MinWidth = 44d;
            }
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

            // Track uses Base300 for background and half of BaseContent for the border, like DaisyUI's --input-color
            var trackBrush = trackBgOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", fallback: new SolidColorBrush(Microsoft.UI.Colors.Gray));
            var contentBrush = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.Gray));
            var halfContentBrush = contentBrush is SolidColorBrush solidContent
                ? new SolidColorBrush(solidContent.Color) { Opacity = 0.5 }
                : contentBrush;
            _switchArea.Background = trackBrush;
            _switchArea.BorderThickness = new Thickness(1);

            var variantBrush = Variant switch
            {
                DaisyToggleVariant.Primary => DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                DaisyToggleVariant.Secondary => DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                DaisyToggleVariant.Accent => DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                DaisyToggleVariant.Success => DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                DaisyToggleVariant.Warning => DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                DaisyToggleVariant.Info => DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                DaisyToggleVariant.Error => DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White)),
                _ => null
            };

            if (IsChecked == true)
            {
                // Check for knob brush override (variant-specific or generic)
                var knobOverride = !string.IsNullOrEmpty(variantName)
                    ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyToggle", $"{variantName}KnobBrush")
                    : null;
                knobOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyToggle", "KnobBrush");

                // A theme color that barely differs from the track is darkened (or lightened) in its own hue;
                // a knob brush set by the app is used as given.
                _knob.Background = knobOverride
                    ?? EnsureKnobContrast(variantBrush ?? contentBrush, trackBrush);

                // The checked track border takes the variant color, like DaisyUI, where that color stands out
                // from the track; otherwise the neutral border stays.
                _switchArea.BorderBrush = trackBorderOverride ?? WhenDistinctFromTrack(variantBrush, trackBrush) ?? halfContentBrush;
            }
            else
            {
                // Unchecked knob: check for override, else use Base100
                var uncheckedKnobOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyToggle", "KnobBrushUnchecked");
                _knob.Background = uncheckedKnobOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush", fallback: new SolidColorBrush(Microsoft.UI.Colors.White));
                _switchArea.BorderBrush = trackBorderOverride ?? halfContentBrush;
            }

            // A thin outline keeps the knob visible on any track, also where knob and track colors are close.
            _knob.BorderBrush = contentBrush is SolidColorBrush outlineContent
                ? new SolidColorBrush(outlineContent.Color) { Opacity = KnobOutlineOpacity }
                : contentBrush;
            _knob.BorderThickness = new Thickness(1);

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

            // The knob moves inside the track border, so the travel excludes the border thickness.
            // Unchecked: knob left edge at padding offset from left
            // Checked: knob right edge at padding offset from right
            var border = _switchArea.BorderThickness;
            var innerWidth = _switchArea.Width - border.Left - border.Right;
            var innerHeight = _switchArea.Height - border.Top - border.Bottom;
            var padding = GetEffectivePadding(innerHeight, _knob.Width);
            var targetX = IsChecked == true
                ? Math.Max(padding, innerWidth - _knob.Width - padding)
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

        private double GetEffectivePadding(double innerHeight, double knobSize)
        {
            if (!double.IsNaN(TogglePadding))
                return TogglePadding;

            // Automatic: the same gap beside the knob as above and below it.
            return Math.Max(0, (innerHeight - knobSize) / 2);
        }

        // Returns null where the brush color is too close to the track to be seen.
        private static Brush? WhenDistinctFromTrack(Brush? brush, Brush track)
        {
            if (brush is SolidColorBrush solid && track is SolidColorBrush trackSolid
                && FloweryColorHelpers.GetColorDifference(solid.Color, trackSolid.Color) < MinimumTrackDifference)
                return null;

            return brush;
        }

        /// <summary>
        /// Returns <paramref name="knob"/>, darkened on a light track or lightened on a dark one in 10 % steps
        /// (at most 60 %) until it differs from the track by <see cref="MinimumTrackDifference"/> (CIE76 Delta E).
        /// The steps keep the knob's hue, so a variant knob stays recognizable.
        /// </summary>
        private static Brush EnsureKnobContrast(Brush knob, Brush track)
        {
            if (knob is not SolidColorBrush knobSolid || track is not SolidColorBrush trackSolid)
                return knob;

            var darken = !FloweryColorHelpers.IsDark(trackSolid.Color);
            var color = knobSolid.Color;
            for (var step = 1; step <= 6 && FloweryColorHelpers.GetColorDifference(color, trackSolid.Color) < MinimumTrackDifference; step++)
            {
                color = darken
                    ? FloweryColorHelpers.Darken(knobSolid.Color, step * 0.1)
                    : FloweryColorHelpers.Lighten(knobSolid.Color, step * 0.1);
            }

            return color == knobSolid.Color ? knob : new SolidColorBrush(color);
        }


        private void OnForegroundChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_isApplyingForeground)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            ApplyAll(animateKnob: false);
        }

        private void OnFontSizeChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_isApplyingFontSize)
                return;

            _hasFontSizeOverride = ReadLocalValue(FontSizeProperty) != DependencyProperty.UnsetValue;
        }

        private void EnsureForegroundOverrideInitialized()
        {
            if (_foregroundOverrideInitialized)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            _foregroundOverrideInitialized = true;
        }

        private void EnsureFontSizeOverrideInitialized()
        {
            if (_fontSizeOverrideInitialized)
                return;

            _hasFontSizeOverride = ReadLocalValue(FontSizeProperty) != DependencyProperty.UnsetValue;
            _fontSizeOverrideInitialized = true;
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

        private void SetFontSize(double fontSize)
        {
            if (Math.Abs(FontSize - fontSize) < 0.001)
                return;

            _isApplyingFontSize = true;
            try
            {
                FontSize = fontSize;
            }
            finally
            {
                _isApplyingFontSize = false;
            }
        }

        private static Brush GetAppBrush(string key)
        {
            var resources = Application.Current?.Resources;
            if (resources != null && resources.TryGetValue(key, out var value) && value is Brush brush)
                return brush;

            return DaisyResourceLookup.GetBrush(key);
        }

        #region Neumorphic

        void IDaisyNeumorphicTarget.RefreshNeumorphicEffect() => _neumorphic.Refresh();

        void IDaisyNeumorphicTarget.RequestNeumorphicRefresh() => _neumorphic.RequestRefresh();

        /// <summary>
        /// The track is always inset and the knob always raised, whatever the effective mode.
        /// </summary>
        private IReadOnlyList<DaisyNeumorphicSurface> GetNeumorphicSurfaces()
        {
            if (_trackSurface == null || _knob == null)
                return [];

            return
            [
                new DaisyNeumorphicSurface(_trackSurface, DaisyNeumorphicMode.Inset),
                new DaisyNeumorphicSurface(_knob, DaisyNeumorphicMode.Raised)
            ];
        }

        #endregion
    }
}

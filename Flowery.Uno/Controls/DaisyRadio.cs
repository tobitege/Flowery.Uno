using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A RadioButton control styled after DaisyUI's Radio component.
    /// Supports variant colors and multiple sizes.
    /// </summary>
    public partial class DaisyRadio : RadioButton
    {
        private Border? _outerBorder;
        private Ellipse? _innerDot;
        private readonly DaisyControlLifecycle _lifecycle;
        private long _foregroundCallbackToken;
        private bool _hasForegroundOverride;
        private bool _foregroundOverrideInitialized;
        private bool _isApplyingForeground;

        public DaisyRadio()
        {
            DefaultStyleKey = typeof(DaisyRadio);

            _lifecycle = new DaisyControlLifecycle(
                this,
                ApplyAll,
                () => Size,
                s => Size = s,
                handleLifecycleEvents: false,
                subscribeSizeChanges: false);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Checked += (s, e) => ApplyAll();
            Unchecked += (s, e) => ApplyAll();
        }

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyRadioVariant),
                typeof(DaisyRadio),
                new PropertyMetadata(DaisyRadioVariant.Default, OnAppearanceChanged));

        public DaisyRadioVariant Variant
        {
            get => (DaisyRadioVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyRadio),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRadio radio)
            {
                radio.ApplyAll();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _foregroundCallbackToken = RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundChanged);
            EnsureForegroundOverrideInitialized();
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

        #endregion

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _outerBorder = GetTemplateChild("PART_OuterBorder") as Border;
            _innerDot = GetTemplateChild("PART_InnerDot") as Ellipse;

            EnsureForegroundOverrideInitialized();
            ApplyAll();
        }

        #region Apply Styling

        private void ApplyAll()
        {
            if (_outerBorder == null || _innerDot == null)
                return;

            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            if (_outerBorder == null || _innerDot == null)
                return;

            // Get sizing from tokens (guaranteed by EnsureDefaults)
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            double boxSize = DaisyResourceLookup.GetDouble($"DaisyCheckbox{sizeKey}Size", 18);
            double dotSize = DaisyResourceLookup.GetDouble($"DaisyRadioDot{sizeKey}Size", 10);

            _outerBorder.Width = boxSize;
            _outerBorder.Height = boxSize;
            _outerBorder.CornerRadius = new CornerRadius(boxSize / 2);

            _innerDot.Width = dotSize;
            _innerDot.Height = dotSize;

            FontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
        }

        private void ApplyColors()
        {
            if (_outerBorder == null || _innerDot == null)
                return;

            bool isChecked = IsChecked == true;

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyRadioVariant.Primary => "Primary",
                DaisyRadioVariant.Secondary => "Secondary",
                DaisyRadioVariant.Accent => "Accent",
                DaisyRadioVariant.Success => "Success",
                DaisyRadioVariant.Warning => "Warning",
                DaisyRadioVariant.Info => "Info",
                DaisyRadioVariant.Error => "Error",
                _ => ""
            };

            // Get variant color from palette
            var accentBrushKey = Variant switch
            {
                DaisyRadioVariant.Primary => "DaisyPrimaryBrush",
                DaisyRadioVariant.Secondary => "DaisySecondaryBrush",
                DaisyRadioVariant.Accent => "DaisyAccentBrush",
                DaisyRadioVariant.Success => "DaisySuccessBrush",
                DaisyRadioVariant.Warning => "DaisyWarningBrush",
                DaisyRadioVariant.Info => "DaisyInfoBrush",
                DaisyRadioVariant.Error => "DaisyErrorBrush",
                _ => "DaisyBaseContentBrush"
            };

            // Check for lightweight styling overrides
            var accentOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyRadio", $"{variantName}AccentBrush")
                : null;
            accentOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyRadio", "AccentBrush");

            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyRadio", "BorderBrush");

            var accentBrush = accentOverride ?? DaisyResourceLookup.GetBrush(accentBrushKey);
            var borderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            var labelBrush = _hasForegroundOverride
                ? Foreground
                : GetAppBrush("DaisyBaseContentBrush");

            if (isChecked)
            {
                _outerBorder.BorderBrush = accentBrush;
                _innerDot.Fill = accentBrush;
                _innerDot.Visibility = Visibility.Visible;
            }
            else
            {
                _outerBorder.BorderBrush = borderBrush;
                _innerDot.Visibility = Visibility.Collapsed;
            }

            // Background is always transparent for radio
            _outerBorder.Background = new SolidColorBrush(Colors.Transparent);
            if (!_hasForegroundOverride)
            {
                SetForeground(labelBrush);
            }
        }

        #endregion

        private void OnForegroundChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_isApplyingForeground)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            ApplyAll();
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

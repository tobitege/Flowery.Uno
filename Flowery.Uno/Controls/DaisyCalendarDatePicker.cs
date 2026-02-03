using System;
using Flowery.Enums;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// A CalendarDatePicker control styled to match Daisy input surfaces.
    /// </summary>
    public partial class DaisyCalendarDatePicker : DaisyBaseContentControl
    {
        private Border? _inputBorder;
        private Grid? _inputHost;
        private CalendarDatePicker? _calendarDatePicker;
        private bool _isPointerOverInput;
        private Brush? _baseInputBackground;
        private Brush? _baseInputBorderBrush;
        private bool _isUpdatingDate;
        private long _paddingChangedToken;
        private long _verticalContentAlignmentChangedToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="DaisyCalendarDatePicker"/> class.
        /// </summary>
        public DaisyCalendarDatePicker()
        {
            DefaultStyleKey = typeof(DaisyCalendarDatePicker);
            IsTabStop = false;

            // Apply global size early if allowed and not explicitly ignored.
            if (FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.GetIgnoreGlobalSize(this))
            {
                Size = FlowerySizeManager.CurrentSize;
            }
        }

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Date"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(
                nameof(Date),
                typeof(DateTimeOffset?),
                typeof(DaisyCalendarDatePicker),
                new PropertyMetadata(null, OnDateChanged));

        /// <summary>
        /// Gets or sets the selected date.
        /// </summary>
        public DateTimeOffset? Date
        {
            get => (DateTimeOffset?)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(DaisyCalendarDatePicker),
                new PropertyMetadata(null, OnPlaceholderTextChanged));

        /// <summary>
        /// Gets or sets the placeholder text displayed when no date is selected.
        /// </summary>
        public string? PlaceholderText
        {
            get => (string?)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Variant"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyInputVariant),
                typeof(DaisyCalendarDatePicker),
                new PropertyMetadata(DaisyInputVariant.Bordered, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the visual variant for the input surface.
        /// </summary>
        public DaisyInputVariant Variant
        {
            get => (DaisyInputVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyCalendarDatePicker),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of the date picker.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BorderRingBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderRingBrushProperty =
            DependencyProperty.Register(
                nameof(BorderRingBrush),
                typeof(Brush),
                typeof(DaisyCalendarDatePicker),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the focus border brush override.
        /// </summary>
        public Brush? BorderRingBrush
        {
            get => (Brush?)GetValue(BorderRingBrushProperty);
            set => SetValue(BorderRingBrushProperty, value);
        }

        #endregion

        /// <summary>
        /// Occurs when the date selection changes.
        /// </summary>
        public event TypedEventHandler<DaisyCalendarDatePicker, CalendarDatePickerDateChangedEventArgs>? DateChanged;

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCalendarDatePicker picker)
            {
                picker.ApplyAll();
            }
        }

        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCalendarDatePicker picker)
            {
                picker.SyncDateFromProperty();
            }
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCalendarDatePicker picker)
            {
                picker.UpdatePlaceholder();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_inputBorder == null)
            {
                BuildVisualTree();
            }

            ApplyAll();

            if (_paddingChangedToken == 0)
            {
                _paddingChangedToken = RegisterPropertyChangedCallback(Control.PaddingProperty, OnPaddingChanged);
            }

            if (_verticalContentAlignmentChangedToken == 0)
            {
                _verticalContentAlignmentChangedToken = RegisterPropertyChangedCallback(Control.VerticalContentAlignmentProperty, OnVerticalContentAlignmentChanged);
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            if (_paddingChangedToken != 0)
            {
                UnregisterPropertyChangedCallback(Control.PaddingProperty, _paddingChangedToken);
                _paddingChangedToken = 0;
            }

            if (_verticalContentAlignmentChangedToken != 0)
            {
                UnregisterPropertyChangedCallback(Control.VerticalContentAlignmentProperty, _verticalContentAlignmentChangedToken);
                _verticalContentAlignmentChangedToken = 0;
            }
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _inputBorder ?? base.GetNeumorphicHostElement();
        }

        private void OnPaddingChanged(DependencyObject sender, DependencyProperty dp)
        {
            ApplySizing(Application.Current?.Resources);
        }

        private void OnVerticalContentAlignmentChanged(DependencyObject sender, DependencyProperty dp)
        {
            ApplySizing(Application.Current?.Resources);
        }

        private void BuildVisualTree()
        {
            _inputBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            _inputHost = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _calendarDatePicker = new CalendarDatePicker
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _calendarDatePicker.SetBinding(Control.IsEnabledProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(IsEnabled))
            });

            _calendarDatePicker.DateChanged += OnInnerDateChanged;
            _calendarDatePicker.GotFocus += OnInnerGotFocus;
            _calendarDatePicker.LostFocus += OnInnerLostFocus;

            _inputHost.PointerEntered += OnInputPointerEntered;
            _inputHost.PointerExited += OnInputPointerExited;

            _inputHost.Children.Add(_calendarDatePicker);
            _inputBorder.Child = _inputHost;
            Content = _inputBorder;
        }

        private void OnInnerDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_isUpdatingDate)
                return;

            if (Equals(Date, sender.Date))
            {
                DateChanged?.Invoke(this, args);
                return;
            }

            _isUpdatingDate = true;
            try
            {
                Date = sender.Date;
            }
            finally
            {
                _isUpdatingDate = false;
            }

            DateChanged?.Invoke(this, args);
        }

        private void OnInnerGotFocus(object sender, RoutedEventArgs e)
        {
            ApplyInputBorderInteractionState();
        }

        private void OnInnerLostFocus(object sender, RoutedEventArgs e)
        {
            ApplyInputBorderInteractionState();
        }

        private void OnInputPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverInput = true;
            ApplyInputBorderInteractionState();
        }

        private void OnInputPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverInput = false;
            ApplyInputBorderInteractionState();
        }

        private void ApplyAll()
        {
            if (_inputBorder == null || _calendarDatePicker == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
            {
                DaisyTokenDefaults.EnsureDefaults(resources);
            }

            ApplySizing(resources);
            ApplyTheme(resources);
            ApplyContent();
            ApplyInputBorderInteractionState();
        }

        private void ApplySizing(ResourceDictionary? resources)
        {
            if (_inputBorder == null || _calendarDatePicker == null)
                return;

            var controlHeight = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "Height",
                DaisyResourceLookup.GetDefaultHeight(Size));
            var fontSize = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "FontSize",
                DaisyResourceLookup.GetDefaultFontSize(Size));
            var defaultPadding = DaisyResourceLookup.GetSizeThickness(resources, "DaisyInput", Size, "Padding",
                DaisyResourceLookup.GetDefaultPadding(Size));

            Height = controlHeight;
            FontSize = fontSize;

            _inputBorder.Height = controlHeight;

            var hasLocalPadding = ReadLocalValue(Control.PaddingProperty) != DependencyProperty.UnsetValue;
            _calendarDatePicker.Padding = hasLocalPadding ? Padding : defaultPadding;
            _calendarDatePicker.FontSize = fontSize;

            var hasLocalVerticalAlignment = ReadLocalValue(Control.VerticalContentAlignmentProperty) != DependencyProperty.UnsetValue;
            _calendarDatePicker.VerticalContentAlignment = hasLocalVerticalAlignment ? VerticalContentAlignment : VerticalAlignment.Center;

            var radius = resources != null
                ? DaisyResourceLookup.GetCornerRadius(resources, "DaisyRoundedBtn", new CornerRadius(8))
                : new CornerRadius(8);
            _inputBorder.CornerRadius = radius;
        }

        private void ApplyTheme(ResourceDictionary? resources)
        {
            if (_inputBorder == null || _calendarDatePicker == null)
                return;

            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            var base100 = DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush", new SolidColorBrush(Colors.White));
            var base200 = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush", new SolidColorBrush(Colors.LightGray));
            var base300 = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Colors.Gray));
            var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.Black));
            var neutral = DaisyResourceLookup.GetBrush(resources, "DaisyNeutralBrush", base300);
            var error = DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", new SolidColorBrush(Colors.IndianRed));

            var variantName = Variant switch
            {
                DaisyInputVariant.Primary => "Primary",
                DaisyInputVariant.Secondary => "Secondary",
                DaisyInputVariant.Accent => "Accent",
                DaisyInputVariant.Info => "Info",
                DaisyInputVariant.Success => "Success",
                DaisyInputVariant.Warning => "Warning",
                DaisyInputVariant.Error => "Error",
                DaisyInputVariant.Ghost => "Ghost",
                DaisyInputVariant.Filled => "Filled",
                _ => "Bordered"
            };

            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyCalendarDatePicker", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyCalendarDatePicker", $"{variantName}BorderBrush")
                ?? DaisyResourceLookup.TryGetControlBrush(this, "DaisyCalendarDatePicker", "BorderBrush");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyCalendarDatePicker", "Foreground");

            Brush background;
            Brush border;
            Thickness borderThickness;
            var cornerRadius = new CornerRadius(8);

            switch (Variant)
            {
                case DaisyInputVariant.Primary:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Colors.Blue));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Secondary:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", new SolidColorBrush(Colors.HotPink));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Accent:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", new SolidColorBrush(Colors.Teal));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Info:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", new SolidColorBrush(Colors.DeepSkyBlue));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Success:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", new SolidColorBrush(Colors.LimeGreen));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Warning:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", new SolidColorBrush(Colors.Gold));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Error:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? error;
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Ghost:
                    background = bgOverride ?? transparent;
                    border = borderOverride ?? transparent;
                    borderThickness = new Thickness(0);
                    break;
                case DaisyInputVariant.Filled:
                    background = bgOverride ?? base200;
                    border = borderOverride ?? neutral;
                    borderThickness = new Thickness(0, 0, 0, 2);
                    cornerRadius = new CornerRadius(8, 8, 0, 0);
                    break;
                default:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? neutral;
                    borderThickness = new Thickness(1);
                    break;
            }

            _inputBorder.Background = background;
            _inputBorder.BorderBrush = border;
            _inputBorder.BorderThickness = borderThickness;
            _inputBorder.CornerRadius = cornerRadius;

            _baseInputBackground = background;
            _baseInputBorderBrush = border;

            var textForeground = fgOverride ?? baseContent;
            _calendarDatePicker.Foreground = textForeground;
            _calendarDatePicker.Background = background;

            _calendarDatePicker.Resources["TextControlForegroundPointerOver"] = textForeground;
            _calendarDatePicker.Resources["TextControlForegroundFocused"] = textForeground;
            _calendarDatePicker.Resources["TextControlForegroundDisabled"] = textForeground;
            _calendarDatePicker.Resources["TextControlButtonForeground"] = textForeground;
            _calendarDatePicker.Resources["TextControlButtonForegroundPointerOver"] = textForeground;
            _calendarDatePicker.Resources["TextControlButtonForegroundPressed"] = textForeground;

            _calendarDatePicker.Resources["CalendarDatePickerForeground"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerForegroundDisabled"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerTextForeground"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerTextForegroundPointerOver"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerTextForegroundPressed"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerTextForegroundDisabled"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerTextForegroundSelected"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerCalendarGlyphForeground"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerCalendarGlyphForegroundPointerOver"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerCalendarGlyphForegroundPressed"] = textForeground;
            _calendarDatePicker.Resources["CalendarDatePickerCalendarGlyphForegroundDisabled"] = textForeground;

            var pointerOverBackground = Variant == DaisyInputVariant.Filled
                ? DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Colors.Gray))
                : background;
            _calendarDatePicker.Resources["CalendarDatePickerBackground"] = background;
            _calendarDatePicker.Resources["CalendarDatePickerBackgroundPointerOver"] = pointerOverBackground;
            _calendarDatePicker.Resources["CalendarDatePickerBackgroundPressed"] = background;
            _calendarDatePicker.Resources["CalendarDatePickerBackgroundDisabled"] = background;
            _calendarDatePicker.Resources["CalendarDatePickerBackgroundFocused"] = background;

            if (textForeground is SolidColorBrush scb)
            {
                var placeholderColor = Color.FromArgb(153, scb.Color.R, scb.Color.G, scb.Color.B);
                var placeholderBrush = new SolidColorBrush(placeholderColor);
                _calendarDatePicker.Resources["TextControlPlaceholderForeground"] = placeholderBrush;
                _calendarDatePicker.Resources["TextControlPlaceholderForegroundPointerOver"] = placeholderBrush;
                _calendarDatePicker.Resources["TextControlPlaceholderForegroundFocused"] = placeholderBrush;
                _calendarDatePicker.Resources["TextControlPlaceholderForegroundDisabled"] = placeholderBrush;
            }
        }

        private void ApplyContent()
        {
            if (_calendarDatePicker == null)
                return;

            SyncDateFromProperty();
            UpdatePlaceholder();
        }

        private void SyncDateFromProperty()
        {
            if (_calendarDatePicker == null || _isUpdatingDate)
                return;

            var date = Date;
            if (Equals(_calendarDatePicker.Date, date))
                return;

            _isUpdatingDate = true;
            try
            {
                _calendarDatePicker.Date = date;
            }
            finally
            {
                _isUpdatingDate = false;
            }
        }

        private void UpdatePlaceholder()
        {
            if (_calendarDatePicker == null)
                return;

            if (PlaceholderText == null)
            {
                _calendarDatePicker.ClearValue(CalendarDatePicker.PlaceholderTextProperty);
            }
            else
            {
                _calendarDatePicker.PlaceholderText = PlaceholderText;
            }
        }

        private void ApplyInputBorderInteractionState()
        {
            if (_inputBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
            {
                DaisyTokenDefaults.EnsureDefaults(resources);
            }

            var isFocused = _calendarDatePicker != null && _calendarDatePicker.FocusState != FocusState.Unfocused;
            if (isFocused)
            {
                _inputBorder.BorderBrush = GetVariantFocusBrush(resources);
                ApplyInnerBackground(_baseInputBackground);
                return;
            }

            if (_isPointerOverInput && (Variant == DaisyInputVariant.Bordered || Variant == DaisyInputVariant.Filled))
            {
                var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.Black));
                _inputBorder.BorderBrush = baseContent;

                if (Variant == DaisyInputVariant.Filled)
                {
                    var base300 = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Colors.Gray));
                    _inputBorder.Background = base300;
                    ApplyInnerBackground(base300);
                }
                else
                {
                    ApplyInnerBackground(_baseInputBackground);
                }

                return;
            }

            if (_baseInputBorderBrush != null)
            {
                _inputBorder.BorderBrush = _baseInputBorderBrush;
            }

            if (_baseInputBackground != null)
            {
                _inputBorder.Background = _baseInputBackground;
                ApplyInnerBackground(_baseInputBackground);
            }
        }

        private void ApplyInnerBackground(Brush? background)
        {
            if (_calendarDatePicker == null || background == null)
                return;

            _calendarDatePicker.Background = background;
        }

        private Brush GetVariantFocusBrush(ResourceDictionary? resources)
        {
            if (BorderRingBrush != null)
                return BorderRingBrush;

            var primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Colors.Blue));

            return Variant switch
            {
                DaisyInputVariant.Primary => primary,
                DaisyInputVariant.Secondary => DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", primary),
                DaisyInputVariant.Accent => DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", primary),
                DaisyInputVariant.Info => DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", primary),
                DaisyInputVariant.Success => DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", primary),
                DaisyInputVariant.Warning => DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", primary),
                DaisyInputVariant.Error => DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", primary),
                _ => primary
            };
        }
    }
}

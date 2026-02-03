namespace Flowery.Controls.Weather
{
    /// <summary>
    /// Displays current weather conditions including temperature, feels-like, condition icon, and date/time info.
    /// </summary>
    public partial class DaisyWeatherCurrent : DaisyBaseContentControl
    {
        [GeneratedRegex(@"[yY]+")]
        private static partial Regex YearPatternRegex();

        private Path? _sunriseIcon;
        private Path? _sunsetIcon;
        private bool _pendingSizeUpdate;

        // Cache detected palette name (background color doesn't change, only theme brushes do)
        private string? _detectedBackgroundPalette;

        public DaisyWeatherCurrent()
        {
            DefaultStyleKey = typeof(DaisyWeatherCurrent);
            LayoutUpdated += OnLayoutUpdated;
            UpdateFormattedDate();
            IsTabStop = false;

        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            ApplyAll();
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

        private void ApplyAll()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Detect which palette the Background belongs to (once, then cached)
            // This handles cases where XAML usage sets explicit Background
            if (_detectedBackgroundPalette == null)
            {
                var bgColor = (Background as SolidColorBrush)?.Color;
                if (bgColor != null)
                {
                    _detectedBackgroundPalette = DaisyResourceLookup.GetPaletteNameForColor(bgColor.Value);
                }
            }

            // Get fresh brushes from current theme
            if (_detectedBackgroundPalette != null)
            {
                var (freshBackground, freshForeground) = DaisyResourceLookup.GetPaletteBrushes(_detectedBackgroundPalette);
                if (freshBackground != null)
                    Background = freshBackground;
                if (freshForeground != null)
                    Foreground = freshForeground;
            }
            else
            {
                // No explicit background - just refresh foreground (style uses BaseContent)
                var freshForeground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
                if (freshForeground != null)
                    Foreground = freshForeground;
            }

            UpdateSizeVisualState();
        }

        #region Size

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(DaisySize.Medium, OnSizeChanged));

        /// <summary>
        /// The size of the weather display.
        /// When <see cref="FlowerySizeManager.UseGlobalSizeByDefault"/> is enabled,
        /// this will follow <see cref="FlowerySizeManager.CurrentSize"/> unless IgnoreGlobalSize is set.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherCurrent control)
                control.UpdateSizeVisualState();
        }

        #endregion

        private void OnLayoutUpdated(object? sender, object e)
        {
            if (_pendingSizeUpdate && ActualWidth > 0)
            {
                _pendingSizeUpdate = false;
                UpdateSizeVisualState();
            }
        }

        private void UpdateSizeVisualState()
        {
            var stateName = Size switch
            {
                DaisySize.ExtraSmall => "ExtraSmall",
                DaisySize.Small => "Small",
                DaisySize.Medium => "Medium",
                DaisySize.Large => "Large",
                DaisySize.ExtraLarge => "ExtraLarge",
                _ => "Medium"
            };

            VisualStateManager.GoToState(this, stateName, true);
        }

        public static readonly DependencyProperty TemperatureProperty =
            DependencyProperty.Register(
                nameof(Temperature),
                typeof(double),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(0d));

        /// <summary>
        /// Current temperature value.
        /// </summary>
        public double Temperature
        {
            get => (double)GetValue(TemperatureProperty);
            set => SetValue(TemperatureProperty, value);
        }

        public static readonly DependencyProperty FeelsLikeProperty =
            DependencyProperty.Register(
                nameof(FeelsLike),
                typeof(double),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(0d));

        /// <summary>
        /// "Feels like" temperature value.
        /// </summary>
        public double FeelsLike
        {
            get => (double)GetValue(FeelsLikeProperty);
            set => SetValue(FeelsLikeProperty, value);
        }

        public static readonly DependencyProperty FeelsLikeLabelProperty =
            DependencyProperty.Register(
                nameof(FeelsLikeLabel),
                typeof(string),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("Weather_FeelsLike", "Feels Like")));

        /// <summary>
        /// The label text for "Feels like".
        /// </summary>
        public string FeelsLikeLabel
        {
            get => (string)GetValue(FeelsLikeLabelProperty);
            set => SetValue(FeelsLikeLabelProperty, value);
        }

        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register(
                nameof(Condition),
                typeof(WeatherCondition),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(WeatherCondition.Unknown));

        /// <summary>
        /// Current weather condition for icon display.
        /// </summary>
        public WeatherCondition Condition
        {
            get => (WeatherCondition)GetValue(ConditionProperty);
            set => SetValue(ConditionProperty, value);
        }

        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(
                nameof(Date),
                typeof(DateTime),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(DateTime.Now, OnDateChanged));

        /// <summary>
        /// Date and time of the weather reading.
        /// </summary>
        public DateTime Date
        {
            get => (DateTime)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherCurrent control)
                control.UpdateFormattedDate();
        }

        public static readonly DependencyProperty SunriseProperty =
            DependencyProperty.Register(
                nameof(Sunrise),
                typeof(TimeSpan),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(default(TimeSpan)));

        /// <summary>
        /// Sunrise time.
        /// </summary>
        public TimeSpan Sunrise
        {
            get => (TimeSpan)GetValue(SunriseProperty);
            set => SetValue(SunriseProperty, value);
        }

        public static readonly DependencyProperty SunsetProperty =
            DependencyProperty.Register(
                nameof(Sunset),
                typeof(TimeSpan),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(default(TimeSpan)));

        /// <summary>
        /// Sunset time.
        /// </summary>
        public TimeSpan Sunset
        {
            get => (TimeSpan)GetValue(SunsetProperty);
            set => SetValue(SunsetProperty, value);
        }

        public static readonly DependencyProperty TemperatureUnitProperty =
            DependencyProperty.Register(
                nameof(TemperatureUnit),
                typeof(string),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata("C"));

        /// <summary>
        /// Temperature unit (C or F).
        /// </summary>
        public string TemperatureUnit
        {
            get => (string)GetValue(TemperatureUnitProperty);
            set => SetValue(TemperatureUnitProperty, value);
        }

        public static readonly DependencyProperty ShowSunTimesProperty =
            DependencyProperty.Register(
                nameof(ShowSunTimes),
                typeof(bool),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(true));

        /// <summary>
        /// Whether to show sunrise/sunset times.
        /// </summary>
        public bool ShowSunTimes
        {
            get => (bool)GetValue(ShowSunTimesProperty);
            set => SetValue(ShowSunTimesProperty, value);
        }

        public static readonly DependencyProperty DateFormatProperty =
            DependencyProperty.Register(
                nameof(DateFormat),
                typeof(string),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata(null, OnDateFormatChanged));

        /// <summary>
        /// Date format string for the header display.
        /// Default is null, which automatically selects a compact locale-aware format (e.g., "ddd, dd.MM." or "ddd, MM/dd").
        /// Use shorter formats like "ddd, d MMM" for constrained layouts.
        /// </summary>
        public string DateFormat
        {
            get => (string)GetValue(DateFormatProperty);
            set => SetValue(DateFormatProperty, value);
        }

        public static readonly DependencyProperty FormattedDateProperty =
            DependencyProperty.Register(
                nameof(FormattedDate),
                typeof(string),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata("--"));

        public string FormattedDate
        {
            get => (string)GetValue(FormattedDateProperty);
            private set => SetValue(FormattedDateProperty, value);
        }

        public static readonly DependencyProperty FormattedDayProperty =
            DependencyProperty.Register(
                nameof(FormattedDay),
                typeof(string),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata("--"));

        public string FormattedDay
        {
            get => (string)GetValue(FormattedDayProperty);
            private set => SetValue(FormattedDayProperty, value);
        }

        public static readonly DependencyProperty FormattedMonthDateProperty =
            DependencyProperty.Register(
                nameof(FormattedMonthDate),
                typeof(string),
                typeof(DaisyWeatherCurrent),
                new PropertyMetadata("--"));

        public string FormattedMonthDate
        {
            get => (string)GetValue(FormattedMonthDateProperty);
            private set => SetValue(FormattedMonthDateProperty, value);
        }

        private static void OnDateFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherCurrent control)
                control.UpdateFormattedDate();
        }

        private void UpdateFormattedDate()
        {
            var format = DateFormat;
            var pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            
            // 1. Calculate the numeric part (stripped of year)
            string numericPart;
            try
            {
                // Remove year digits, then strip leading separators and trailing slashes/dashes
                // We KEEP the trailing dot because it's an ordinal marker in many European locales (e.g., "15.07.")
                numericPart = YearPatternRegex().Replace(pattern, "");
                numericPart = numericPart.TrimStart('/', '-', '.', ' ', '(', ')'); 
                numericPart = numericPart.TrimEnd('/', '-', ' ', '(', ')');
            }
            catch
            {
                numericPart = pattern;
            }

            // 2. Populate the split properties
            try 
            {
                FormattedDay = Date.ToString("ddd", CultureInfo.CurrentCulture);
                FormattedMonthDate = Date.ToString(numericPart, CultureInfo.CurrentCulture);
            }
            catch
            {
                FormattedDay = Date.ToString("ddd", CultureInfo.CurrentCulture);
                FormattedMonthDate = Date.ToShortDateString();
            }

            // 3. Populate the combined FormattedDate (respecting custom format if provided)
            if (string.IsNullOrWhiteSpace(format))
            {
                format = $"ddd, {numericPart}";
            }

            try
            {
                FormattedDate = Date.ToString(format, CultureInfo.CurrentCulture);
            }
            catch
            {
                FormattedDate = FormattedDay + ", " + FormattedMonthDate;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _sunriseIcon = GetTemplateChild("PART_SunriseIcon") as Path;
            _sunsetIcon = GetTemplateChild("PART_SunsetIcon") as Path;

            FloweryPathHelpers.TrySetPathData(
                _sunriseIcon,
                () => FloweryPathHelpers.GetWeatherIconGeometry("WeatherIconSunrise"),
                () => FloweryPathHelpers.CreateSunriseGeometry());

            FloweryPathHelpers.TrySetPathData(
                _sunsetIcon,
                () => FloweryPathHelpers.GetWeatherIconGeometry("WeatherIconSunset"),
                () => FloweryPathHelpers.CreateSunsetGeometry());

            _pendingSizeUpdate = true;
        }
    }
}

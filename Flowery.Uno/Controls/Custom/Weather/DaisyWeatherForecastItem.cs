using Flowery.Controls.Weather.Models;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Controls.Weather
{
    /// <summary>
    /// Represents a single day in the weather forecast strip.
    /// This control supports Size-based visual states for responsive sizing.
    /// </summary>
    public partial class DaisyWeatherForecastItem : DaisyBaseContentControl
    {
        private Border? _itemBorder;
        private bool _pendingSizeUpdate;

        public DaisyWeatherForecastItem()
        {
            DefaultStyleKey = typeof(DaisyWeatherForecastItem);
            LayoutUpdated += OnLayoutUpdated;
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

            // Refresh brushes from current theme (style uses Base100)
            var (freshBackground, freshForeground) = DaisyResourceLookup.GetPaletteBrushes("Base100");
            if (freshBackground != null)
                Background = freshBackground;
            if (freshForeground != null)
                Foreground = freshForeground;

            ApplySizing();
        }

        #region Size

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata(DaisySize.Medium, OnSizeChanged));

        /// <summary>
        /// The size of the forecast item.
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
            if (d is DaisyWeatherForecastItem control)
                control.ApplySizing();
        }

        #endregion

        #region ItemWidth

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(
                nameof(ItemWidth),
                typeof(double),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata(double.NaN, OnItemWidthChanged));

        /// <summary>
        /// When set (non-NaN), specifies the exact width for this item.
        /// When NaN (default), the item uses visual state-based sizing.
        /// This is typically set by the parent DaisyWeatherForecast when AutoFitItems is enabled.
        /// </summary>
        public double ItemWidth
        {
            get => (double)GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        private static void OnItemWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherForecastItem control)
                control.ApplyItemWidth();
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty DayNameProperty =
            DependencyProperty.Register(
                nameof(DayName),
                typeof(string),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Name of the day (e.g., "Mon", "Tue").
        /// </summary>
        public string DayName
        {
            get => (string)GetValue(DayNameProperty);
            set => SetValue(DayNameProperty, value);
        }

        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register(
                nameof(Condition),
                typeof(WeatherCondition),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata(WeatherCondition.Unknown));

        /// <summary>
        /// Weather condition for icon display.
        /// </summary>
        public WeatherCondition Condition
        {
            get => (WeatherCondition)GetValue(ConditionProperty);
            set => SetValue(ConditionProperty, value);
        }

        public static readonly DependencyProperty HighTemperatureProperty =
            DependencyProperty.Register(
                nameof(HighTemperature),
                typeof(double),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata(0d));

        /// <summary>
        /// High temperature for the day.
        /// </summary>
        public double HighTemperature
        {
            get => (double)GetValue(HighTemperatureProperty);
            set => SetValue(HighTemperatureProperty, value);
        }

        public static readonly DependencyProperty LowTemperatureProperty =
            DependencyProperty.Register(
                nameof(LowTemperature),
                typeof(double),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata(0d));

        /// <summary>
        /// Low temperature for the day.
        /// </summary>
        public double LowTemperature
        {
            get => (double)GetValue(LowTemperatureProperty);
            set => SetValue(LowTemperatureProperty, value);
        }

        public static readonly DependencyProperty TemperatureUnitProperty =
            DependencyProperty.Register(
                nameof(TemperatureUnit),
                typeof(string),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata("C"));

        /// <summary>
        /// Temperature unit (C or F).
        /// </summary>
        public string TemperatureUnit
        {
            get => (string)GetValue(TemperatureUnitProperty);
            set => SetValue(TemperatureUnitProperty, value);
        }

        public static readonly DependencyProperty PrecipitationChanceProperty =
            DependencyProperty.Register(
                nameof(PrecipitationChance),
                typeof(double),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata(0d));

        /// <summary>
        /// Chance of precipitation (0-100).
        /// </summary>
        public double PrecipitationChance
        {
            get => (double)GetValue(PrecipitationChanceProperty);
            set => SetValue(PrecipitationChanceProperty, value);
        }

        public static readonly DependencyProperty ShowPrecipitationProperty =
            DependencyProperty.Register(
                nameof(ShowPrecipitation),
                typeof(bool),
                typeof(DaisyWeatherForecastItem),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether to show precipitation chance.
        /// </summary>
        public bool ShowPrecipitation
        {
            get => (bool)GetValue(ShowPrecipitationProperty);
            set => SetValue(ShowPrecipitationProperty, value);
        }

        #endregion

        private void OnLayoutUpdated(object? sender, object e)
        {
            if (_pendingSizeUpdate && ActualWidth > 0)
            {
                _pendingSizeUpdate = false;
                ApplySizing();
            }
        }

        private void ApplySizing()
        {
            UpdateSizeVisualState();
            ApplyItemWidth();
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

        private void ApplyItemWidth()
        {
            if (_itemBorder == null)
                return;

            if (!double.IsNaN(ItemWidth) && ItemWidth > 0)
            {
                // Use explicit width from parent's AutoFitItems calculation
                _itemBorder.Width = ItemWidth;
                _itemBorder.MinWidth = 0; // Clear MinWidth when using explicit width
            }
            else
            {
                // Clear explicit width to let visual state sizing take effect
                _itemBorder.Width = double.NaN;
                // MinWidth will be set by visual state
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _itemBorder = GetTemplateChild("ItemBorder") as Border;

            _pendingSizeUpdate = true;
        }
    }
}

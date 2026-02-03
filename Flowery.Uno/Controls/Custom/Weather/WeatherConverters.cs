using System;
using System.Globalization;
using Flowery.Controls.Weather.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls.Weather
{
    /// <summary>
    /// Converts WeatherCondition enum to the appropriate icon Geometry resource.
    /// </summary>
    public sealed partial class WeatherConditionToIconConverter : IValueConverter
    {
        public static readonly WeatherConditionToIconConverter Instance = new();

        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            var key = GetIconKey(value);
            if (Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Geometry geometry)
            {
                return geometry;
            }

            return null;
        }

        private static string GetIconKey(object value)
        {
            if (value is WeatherCondition condition)
            {
                return condition switch
                {
                    WeatherCondition.Sunny => "WeatherIconSunny",
                    WeatherCondition.Clear => "WeatherIconClear",
                    WeatherCondition.PartlyCloudy => "WeatherIconPartlyCloudy",
                    WeatherCondition.Cloudy => "WeatherIconCloudy",
                    WeatherCondition.Overcast => "WeatherIconOvercast",
                    WeatherCondition.Mist => "WeatherIconMist",
                    WeatherCondition.Fog => "WeatherIconFog",
                    WeatherCondition.LightRain => "WeatherIconCloudy",
                    WeatherCondition.Rain => "WeatherIconCloudy",
                    WeatherCondition.HeavyRain => "WeatherIconCloudy",
                    WeatherCondition.Drizzle => "WeatherIconCloudy",
                    WeatherCondition.Showers => "WeatherIconCloudy",
                    WeatherCondition.Thunderstorm => "WeatherIconThunderstorm",
                    WeatherCondition.LightSnow => "WeatherIconCloudy",
                    WeatherCondition.Snow => "WeatherIconCloudy",
                    WeatherCondition.HeavySnow => "WeatherIconCloudy",
                    WeatherCondition.Sleet => "WeatherIconCloudy",
                    WeatherCondition.FreezingRain => "WeatherIconCloudy",
                    WeatherCondition.Hail => "WeatherIconCloudy",
                    WeatherCondition.Windy => "WeatherIconWindy",
                    _ => "WeatherIconUnknown"
                };
            }

            return "WeatherIconUnknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts TimeSpan to time string (HH:mm format).
    /// </summary>
    public sealed partial class TimeSpanToStringConverter : IValueConverter
    {
        public static readonly TimeSpanToStringConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan time)
            {
                return time.ToString(@"hh\:mm", CultureInfo.CurrentCulture);
            }

            return "--:--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts DateTime to full date format.
    /// </summary>
    public sealed partial class DateToFullStringConverter : IValueConverter
    {
        public static readonly DateToFullStringConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime date)
            {
                var format = parameter as string ?? "dddd, d MMMM";
                return date.ToString(format, CultureInfo.CurrentCulture);
            }

            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts non-empty strings to Visible, otherwise Collapsed.
    /// </summary>
    public sealed partial class StringNotNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public static readonly StringNotNullOrEmptyToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var text = value as string;
            return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts boolean values to Visible/Collapsed.
    /// </summary>
    public sealed partial class BooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var isVisible = value is bool flag && flag;

            if (parameter is string param
                && string.Equals(param, "Invert", StringComparison.OrdinalIgnoreCase))
            {
                isVisible = !isVisible;
            }

            if (isVisible)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Formats numeric values with optional degree suffix.
    /// </summary>
    public sealed partial class DoubleToIntStringConverter : IValueConverter
    {
        public static readonly DoubleToIntStringConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
                return string.Empty;

            if (!double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out var numeric))
                return value.ToString() ?? string.Empty;

            var text = numeric.ToString("0", CultureInfo.CurrentCulture);
            if (parameter is string suffix && string.Equals(suffix, "deg", StringComparison.OrdinalIgnoreCase))
            {
                text += "\u00B0";
            }

            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Formats temperature unit strings with degree prefix.
    /// </summary>
    public sealed partial class TemperatureUnitToDegreeConverter : IValueConverter
    {
        public static readonly TemperatureUnitToDegreeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var unit = value?.ToString();
            if (string.IsNullOrWhiteSpace(unit))
                return "\u00B0";

            return "\u00B0" + unit.Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}

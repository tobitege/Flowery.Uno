using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Flowery.Services
{
    /// <summary>
    /// Converter that applies scale factor to a base value.
    /// Use with ScaleHelper attached properties or directly in bindings.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;TextBlock FontSize="{Binding ElementName=ScalingRoot, 
    ///     Path=(services:FloweryScaleManager.ScaleFactor),
    ///     Converter={StaticResource ScaleConverter},
    ///     ConverterParameter='FontTitle'}" /&gt;
    /// </code>
    /// </example>
    public partial class ScaleConverter : IValueConverter
    {
        /// <summary>
        /// Shared instance for use in XAML resources.
        /// </summary>
        public static ScaleConverter Instance { get; } = new();

        /// <summary>
        /// Converts a scale factor to a scaled value.
        /// </summary>
        /// <param name="value">The scale factor (double)</param>
        /// <param name="targetType">Target type (double or Thickness)</param>
        /// <param name="parameter">Preset name (e.g., "FontTitle") or "baseValue" or "baseValue,minValue"</param>
        /// <param name="language">Culture info</param>
        /// <returns>Scaled value</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var scaleFactor = value is double sf ? sf : 1.0;
            var (baseValue, minValue) = ResolveValues(parameter?.ToString() ?? "");

            if (baseValue <= 0)
                return 0.0;

            double result;
            if (minValue.HasValue)
            {
                result = FloweryScaleManager.ApplyScale(baseValue, minValue.Value, scaleFactor);
            }
            else
            {
                result = FloweryScaleManager.ApplyScale(baseValue, scaleFactor);
            }

            // Handle Thickness target type
            if (targetType == typeof(Thickness))
            {
                return new Thickness(result);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private static (double baseValue, double? minValue) ResolveValues(string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
                return (0, null);

            // Try to parse as preset first
            if (Enum.TryParse<FloweryScalePreset>(parameter, true, out var preset))
            {
                return FloweryScaleConfig.GetPresetValues(preset);
            }

            // Try to parse as "baseValue" or "baseValue,minValue"
            var parts = parameter.Split(',');
            if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var bv))
            {
                double? mv = null;
                if (parts.Length > 1 && double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMin))
                {
                    mv = parsedMin;
                }
                return (bv, mv);
            }

            return (0, null);
        }
    }

    /// <summary>
    /// Converter that converts double to Thickness (uniform on all sides).
    /// </summary>
    public partial class DoubleToThicknessConverter : IValueConverter
    {
        public static DoubleToThicknessConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double d)
            {
                return new Thickness(d);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Static helper class providing scaled values for use in code-behind.
    /// Equivalent to the ScaleExtension markup extension from Avalonia.
    /// </summary>
    /// <example>
    /// <code>
    /// // In code-behind:
    /// myTextBlock.FontSize = ScaleHelper.GetScaledPreset(FloweryScalePreset.FontTitle, scaleFactor);
    /// myBorder.Padding = new Thickness(ScaleHelper.GetScaledPreset(FloweryScalePreset.SpacingMedium, scaleFactor));
    /// </code>
    /// </example>
    public static class ScaleHelper
    {
        /// <summary>
        /// Gets the scaled value for a preset with the given scale factor.
        /// </summary>
        public static double GetScaledPreset(FloweryScalePreset preset, double scaleFactor)
        {
            var (baseValue, minValue) = FloweryScaleConfig.GetPresetValues(preset);
            if (minValue.HasValue)
            {
                return FloweryScaleManager.ApplyScale(baseValue, minValue.Value, scaleFactor);
            }
            return FloweryScaleManager.ApplyScale(baseValue, scaleFactor);
        }

        /// <summary>
        /// Gets the scaled value for custom base/min values with the given scale factor.
        /// </summary>
        public static double GetScaledValue(double baseValue, double? minValue, double scaleFactor)
        {
            if (minValue.HasValue)
            {
                return FloweryScaleManager.ApplyScale(baseValue, minValue.Value, scaleFactor);
            }
            return FloweryScaleManager.ApplyScale(baseValue, scaleFactor);
        }

        /// <summary>
        /// Gets Thickness (uniform) for a preset with the given scale factor.
        /// </summary>
        public static Thickness GetScaledThickness(FloweryScalePreset preset, double scaleFactor)
        {
            return new Thickness(GetScaledPreset(preset, scaleFactor));
        }

        /// <summary>
        /// Gets the current scale factor for an element (reads from attached property or calculates).
        /// </summary>
        public static double GetCurrentScaleFactor(DependencyObject element)
        {
            // First check if the element has a scale factor set
            var scaleFactor = FloweryScaleManager.GetScaleFactor(element);
            if (scaleFactor > 0 && Math.Abs(scaleFactor - 1.0) > 0.001)
            {
                return scaleFactor;
            }

            // Walk up the tree to find a parent with EnableScaling
            var current = element;
            while (current != null)
            {
                if (FloweryScaleManager.GetEnableScaling(current))
                {
                    var parentFactor = FloweryScaleManager.GetScaleFactor(current);
                    if (parentFactor > 0)
                        return parentFactor;
                }
                current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            }

            return 1.0;
        }

        #region Preset Value Accessors (convenience methods)

        // Font presets
        public static double FontDisplay(double scaleFactor) => GetScaledPreset(FloweryScalePreset.FontDisplay, scaleFactor);
        public static double FontTitle(double scaleFactor) => GetScaledPreset(FloweryScalePreset.FontTitle, scaleFactor);
        public static double FontHeading(double scaleFactor) => GetScaledPreset(FloweryScalePreset.FontHeading, scaleFactor);
        public static double FontSubheading(double scaleFactor) => GetScaledPreset(FloweryScalePreset.FontSubheading, scaleFactor);
        public static double FontBody(double scaleFactor) => GetScaledPreset(FloweryScalePreset.FontBody, scaleFactor);
        public static double FontCaption(double scaleFactor) => GetScaledPreset(FloweryScalePreset.FontCaption, scaleFactor);
        public static double FontSmall(double scaleFactor) => GetScaledPreset(FloweryScalePreset.FontSmall, scaleFactor);

        // Spacing presets
        public static double SpacingXL(double scaleFactor) => GetScaledPreset(FloweryScalePreset.SpacingXL, scaleFactor);
        public static double SpacingLarge(double scaleFactor) => GetScaledPreset(FloweryScalePreset.SpacingLarge, scaleFactor);
        public static double SpacingMedium(double scaleFactor) => GetScaledPreset(FloweryScalePreset.SpacingMedium, scaleFactor);
        public static double SpacingSmall(double scaleFactor) => GetScaledPreset(FloweryScalePreset.SpacingSmall, scaleFactor);
        public static double SpacingXS(double scaleFactor) => GetScaledPreset(FloweryScalePreset.SpacingXS, scaleFactor);

        // Dimension presets
        public static double CardWidth(double scaleFactor) => GetScaledPreset(FloweryScalePreset.CardWidth, scaleFactor);
        public static double CardHeight(double scaleFactor) => GetScaledPreset(FloweryScalePreset.CardHeight, scaleFactor);
        public static double IconSmall(double scaleFactor) => GetScaledPreset(FloweryScalePreset.IconSmall, scaleFactor);
        public static double IconMedium(double scaleFactor) => GetScaledPreset(FloweryScalePreset.IconMedium, scaleFactor);
        public static double IconLarge(double scaleFactor) => GetScaledPreset(FloweryScalePreset.IconLarge, scaleFactor);
        public static double IconContainerSmall(double scaleFactor) => GetScaledPreset(FloweryScalePreset.IconContainerSmall, scaleFactor);
        public static double IconContainerMedium(double scaleFactor) => GetScaledPreset(FloweryScalePreset.IconContainerMedium, scaleFactor);
        public static double IconContainerLarge(double scaleFactor) => GetScaledPreset(FloweryScalePreset.IconContainerLarge, scaleFactor);

        #endregion
    }
}

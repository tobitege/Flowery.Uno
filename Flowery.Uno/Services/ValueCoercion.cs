using System;
using Microsoft.UI.Xaml;

namespace Flowery.Services
{
    /// <summary>
    /// Provides reusable value coercion helpers for dependency properties.
    /// </summary>
    public static class ValueCoercion
    {
        /// <summary>
        /// Coerces a double value to be within the specified range.
        /// </summary>
        public static double Clamp(double value, double min, double max)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Coerces an int value to be within the specified range.
        /// </summary>
        public static int Clamp(int value, int min, int max)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Coerces a decimal value to be within the specified range.
        /// </summary>
        public static decimal Clamp(decimal value, decimal min, decimal max)
            => Math.Clamp(value, min, max);

        // ────────────────────────────────────────────────────────────────
        // Common presets with sensible defaults
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Coerces a pixel size value (e.g., CellSize, IconSize). Min: 4px, Max: 512px.
        /// </summary>
        public static double PixelSize(double value, double min = 4.0, double max = 512.0)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Coerces a percentage value. Min: 0%, Max: configurable (default 1000% for zoom/scale use cases).
        /// </summary>
        public static double Percent(double value, double min = 0.0, double max = 1000.0)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Coerces a strict percentage value (0-100) for progress bars, completion, etc.
        /// </summary>
        public static double Percent100(double value)
            => Math.Clamp(value, 0.0, 100.0);

        /// <summary>
        /// Coerces a normalized ratio value (0-1).
        /// </summary>
        public static double Ratio(double value)
            => Math.Clamp(value, 0.0, 1.0);

        /// <summary>
        /// Coerces an opacity value (0-1).
        /// </summary>
        public static double Opacity(double value)
            => Math.Clamp(value, 0.0, 1.0);

        /// <summary>
        /// Coerces a corner radius value. Min: 0, Max: half of size (or 999 if no size context).
        /// </summary>
        public static double CornerRadius(double value, double maxRadius = 999.0)
            => Math.Clamp(value, 0.0, maxRadius);

        /// <summary>
        /// Coerces a spacing/margin value. Min: 0, Max: 256px.
        /// </summary>
        public static double Spacing(double value, double min = 0.0, double max = 256.0)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Coerces an index value to be within valid bounds.
        /// </summary>
        public static int Index(int value, int count)
            => count <= 0 ? 0 : Math.Clamp(value, 0, count - 1);

        /// <summary>
        /// Coerces a count/length value. Min: 1, Max: specified maximum.
        /// </summary>
        public static int Count(int value, int min = 1, int max = 1000)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Coerces a duration in milliseconds. Min: 0, Max: 60000 (1 minute).
        /// </summary>
        public static double DurationMs(double value, double min = 0.0, double max = 60000.0)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Coerces a font size value. Min: 6px, Max: 200px.
        /// </summary>
        public static double FontSize(double value, double min = 6.0, double max = 200.0)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Coerces a stroke/border thickness value. Min: 0, Max: 32px.
        /// </summary>
        public static double StrokeThickness(double value, double min = 0.0, double max = 32.0)
            => Math.Clamp(value, min, max);

        /// <summary>
        /// Ensures a value is non-negative.
        /// </summary>
        public static double NonNegative(double value)
            => Math.Max(0.0, value);

        /// <summary>
        /// Ensures a value is positive (greater than zero).
        /// </summary>
        public static double Positive(double value, double fallback = 1.0)
            => value > 0 ? value : fallback;

        // ────────────────────────────────────────────────────────────────
        // Complex type coercion
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Coerces a Thickness to ensure all values are within the specified range.
        /// </summary>
        public static Thickness Thickness(Thickness value, double min = 0.0, double max = 64.0)
            => new(
                Math.Clamp(value.Left, min, max),
                Math.Clamp(value.Top, min, max),
                Math.Clamp(value.Right, min, max),
                Math.Clamp(value.Bottom, min, max));

        /// <summary>
        /// Coerces a CornerRadius to ensure all values are within the specified range.
        /// </summary>
        public static CornerRadius CornerRadius(CornerRadius value, double min = 0.0, double max = 999.0)
            => new(
                Math.Clamp(value.TopLeft, min, max),
                Math.Clamp(value.TopRight, min, max),
                Math.Clamp(value.BottomRight, min, max),
                Math.Clamp(value.BottomLeft, min, max));
    }
}

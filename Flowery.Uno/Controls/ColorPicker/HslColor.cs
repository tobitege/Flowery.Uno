using System;
using Windows.UI;

namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// Represents a color in HSL (Hue, Saturation, Lightness) color space.
    /// Provides conversion methods to and from RGB color space.
    /// </summary>
    [Serializable]
    public struct HslColor : IEquatable<HslColor>
    {
        /// <summary>
        /// Represents an empty HSL color.
        /// </summary>
        public static readonly HslColor Empty = new() { IsEmpty = true };

        private int _alpha;
        private double _hue;
        private bool _isEmpty;
        private double _lightness;
        private double _saturation;

        /// <summary>
        /// Initializes a new instance of the <see cref="HslColor"/> struct with specified HSL values.
        /// </summary>
        /// <param name="hue">The hue component (0-359).</param>
        /// <param name="saturation">The saturation component (0-1).</param>
        /// <param name="lightness">The lightness component (0-1).</param>
        public HslColor(double hue, double saturation, double lightness)
            : this(255, hue, saturation, lightness)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HslColor"/> struct with specified alpha and HSL values.
        /// </summary>
        /// <param name="alpha">The alpha component (0-255).</param>
        /// <param name="hue">The hue component (0-359).</param>
        /// <param name="saturation">The saturation component (0-1).</param>
        /// <param name="lightness">The lightness component (0-1).</param>
        public HslColor(int alpha, double hue, double saturation, double lightness)
        {
            _hue = Math.Min(359, Math.Max(0, hue));
            _saturation = Math.Min(1, Math.Max(0, saturation));
            _lightness = Math.Min(1, Math.Max(0, lightness));
            _alpha = Math.Min(255, Math.Max(0, alpha));
            _isEmpty = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HslColor"/> struct from a Color.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        public HslColor(Color color)
        {
            _alpha = color.A;
            RgbToHsl(color.R, color.G, color.B, out _hue, out _saturation, out _lightness);
            _isEmpty = false;
        }

        /// <summary>
        /// Gets or sets the alpha component (0-255).
        /// </summary>
        public int A
        {
            readonly get => _alpha;
            set => _alpha = Math.Min(255, Math.Max(0, value));
        }

        /// <summary>
        /// Gets or sets the hue component (0-359).
        /// </summary>
        public double H
        {
            readonly get => _hue;
            set
            {
                _hue = value;
                if (_hue > 359) _hue = 0;
                if (_hue < 0) _hue = 359;
            }
        }

        /// <summary>
        /// Gets or sets whether this color is empty.
        /// </summary>
        public bool IsEmpty
        {
            readonly get => _isEmpty;
            internal set => _isEmpty = value;
        }

        /// <summary>
        /// Gets or sets the lightness component (0-1).
        /// </summary>
        public double L
        {
            readonly get => _lightness;
            set => _lightness = Math.Min(1, Math.Max(0, value));
        }

        /// <summary>
        /// Gets or sets the saturation component (0-1).
        /// </summary>
        public double S
        {
            readonly get => _saturation;
            set => _saturation = Math.Min(1, Math.Max(0, value));
        }

        /// <summary>
        /// Implicitly converts an HslColor to a Color.
        /// </summary>
        public static implicit operator Color(HslColor color)
        {
            return color.ToRgbColor();
        }

        /// <summary>
        /// Implicitly converts a Color to an HslColor.
        /// </summary>
        public static implicit operator HslColor(Color color)
        {
            return new HslColor(color);
        }

        public static bool operator !=(HslColor a, HslColor b)
        {
            return !(a == b);
        }

        public static bool operator ==(HslColor a, HslColor b)
        {
            return Math.Abs(a.H - b.H) < 0.001 &&
                   Math.Abs(a.L - b.L) < 0.001 &&
                   Math.Abs(a.S - b.S) < 0.001 &&
                   a.A == b.A;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is HslColor color && this == color;
        }

        public readonly bool Equals(HslColor other)
        {
            return this == other;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(_hue, _saturation, _lightness, _alpha);
        }

        /// <summary>
        /// Converts this HSL color to a Color.
        /// </summary>
        public readonly Color ToRgbColor()
        {
            return ToRgbColor(A);
        }

        /// <summary>
        /// Converts this HSL color to a Color with the specified alpha.
        /// </summary>
        public readonly Color ToRgbColor(int alpha)
        {
            return HslToRgb(alpha, _hue, _saturation, _lightness);
        }

        public override readonly string ToString()
        {
            return $"HslColor [H={H:F1}, S={S:F3}, L={L:F3}, A={A}]";
        }

        /// <summary>
        /// Converts HSL values to a Color.
        /// </summary>
        internal static Color HslToRgb(double h, double s, double l)
        {
            return HslToRgb(255, h, s, l);
        }

        /// <summary>
        /// Converts HSL values to a Color with specified alpha.
        /// </summary>
        internal static Color HslToRgb(int alpha, double h, double s, double l)
        {
            byte r;
            byte g;
            byte b;

            if (Math.Abs(s) < double.Epsilon)
            {
                r = g = b = (byte)(l * 255);
            }
            else
            {
                double v2 = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double v1 = 2 * l - v2;
                double hue = h / 360;

                r = Clamp(255 * HueToRgb(v1, v2, hue + 1.0 / 3));
                g = Clamp(255 * HueToRgb(v1, v2, hue));
                b = Clamp(255 * HueToRgb(v1, v2, hue - 1.0 / 3));
            }

            return Color.FromArgb((byte)alpha, r, g, b);
        }

        /// <summary>
        /// Converts RGB values to HSL.
        /// </summary>
        internal static void RgbToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            l = (max + min) / 2;

            if (Math.Abs(delta) < double.Epsilon)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = l < 0.5 ? delta / (max + min) : delta / (2 - max - min);

                if (Math.Abs(max - rd) < double.Epsilon)
                {
                    h = ((gd - bd) / delta) % 6;
                }
                else if (Math.Abs(max - gd) < double.Epsilon)
                {
                    h = (bd - rd) / delta + 2;
                }
                else
                {
                    h = (rd - gd) / delta + 4;
                }

                h *= 60;
                if (h < 0) h += 360;
            }
        }

        private static byte Clamp(double v)
        {
            if (v < 0) v = 0;
            if (v > 255) v = 255;
            return (byte)Math.Round(v);
        }

        private static double HueToRgb(double v1, double v2, double vH)
        {
            if (vH < 0) vH++;
            if (vH > 1) vH--;

            if (6 * vH < 1) return v1 + (v2 - v1) * 6 * vH;
            if (2 * vH < 1) return v2;
            if (3 * vH < 2) return v1 + (v2 - v1) * (2.0 / 3 - vH) * 6;
            return v1;
        }
    }
}

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Flowery.Controls;

namespace Flowery.Theming
{
    /// <summary>
    /// Provides centralized resource lookup utilities for Daisy controls.
    /// Supports both sizing tokens and lightweight styling (per-control color customization).
    /// </summary>
    /// <remarks>
    /// <para><b>Resource Naming Convention:</b></para>
    /// <para>Control-specific resources follow the pattern: <c>Daisy{Control}{Variant?}{Part?}{State?}{Property}</c></para>
    /// <para>Examples:</para>
    /// <list type="bullet">
    ///   <item><c>DaisyButtonBackground</c> - Button background (base)</item>
    ///   <item><c>DaisyButtonPrimaryBackground</c> - Primary variant background</item>
    ///   <item><c>DaisyToggleKnobBrush</c> - Toggle switch knob color</item>
    ///   <item><c>DaisyTableRowHoverBackground</c> - Table row hover state</item>
    /// </list>
    /// <para><b>Lookup Priority:</b></para>
    /// <list type="number">
    ///   <item>Control.Resources (instance-level override, highest priority)</item>
    ///   <item>Application.Current.Resources (app-level override)</item>
    ///   <item>Fallback to control's internal palette logic</item>
    /// </list>
    /// <para><b>Usage Example:</b></para>
    /// <code>
    /// // In a control's ApplyColors method:
    /// var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "Background");
    /// _rootBorder.Background = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
    /// </code>
    /// </remarks>
    public static class DaisyResourceLookup
    {
        /// <summary>
        /// Ensures default sizing tokens are available in application resources.
        /// Call this early (e.g. app startup) if you need token values before any Daisy control loads.
        /// </summary>
        public static void EnsureTokens(ResourceDictionary? resources = null)
        {
            var target = resources ?? Application.Current?.Resources;
            if (target == null)
                return;

            DaisyTokenDefaults.EnsureDefaults(target);
        }

        public static bool TryGetResource(ResourceDictionary? resources, string key, out object? value)
        {
            value = null;
            if (resources == null)
                return false;

            if (resources.TryGetValue(key, out var found))
            {
                value = found;
                return true;
            }

            // WinUI/Uno does not consistently surface merged dictionary values via TryGetValue
            // across platforms/versions. Search merged dictionaries recursively.
            if (resources.MergedDictionaries != null)
            {
                foreach (var merged in resources.MergedDictionaries)
                {
                    if (merged != null && TryGetResource(merged, key, out value))
                        return true;
                }
            }

            return false;
        }

        public static double GetDouble(ResourceDictionary? resources, string key, double fallback)
        {
            if (!TryGetResource(resources, key, out var value) || value == null)
                return fallback;

            if (value is double d)
                return d;

            if (value is float f)
                return f;

            if (value is int i)
                return i;

            return fallback;
        }

        public static Thickness GetThickness(ResourceDictionary? resources, string key, Thickness fallback)
        {
            if (!TryGetResource(resources, key, out var value) || value == null)
                return fallback;

            if (value is Thickness t)
                return t;

            return fallback;
        }

        public static CornerRadius GetCornerRadius(ResourceDictionary? resources, string key, CornerRadius fallback)
        {
            if (!TryGetResource(resources, key, out var value) || value == null)
                return fallback;

            if (value is CornerRadius r)
                return r;

            return fallback;
        }

        public static Brush GetBrush(ResourceDictionary? resources, string key, Brush fallback)
        {
            if (!TryGetResource(resources, key, out var value) || value == null)
                return fallback;

            if (value is Brush b)
                return b;

            return fallback;
        }

        public static Brush GetBrush(ResourceDictionary? resources, string key)
        {
            return GetBrush(resources, key, new SolidColorBrush(Microsoft.UI.Colors.Transparent));
        }

        public static Brush? WithOpacity(Brush? brush, double opacity)
        {
            if (brush is SolidColorBrush scb)
            {
                var c = scb.Color;
                var a = (byte)Math.Clamp((int)Math.Round(255 * opacity), 0, 255);
                return new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B));
            }

            return brush;
        }

        // ---- Convenience overloads that use Application.Current.Resources ----

        public static double GetDouble(string key, double fallback = 0)
        {
            return GetDouble(Application.Current.Resources, key, fallback);
        }

        public static Thickness GetThickness(string key, Thickness fallback = default)
        {
            return GetThickness(Application.Current.Resources, key, fallback);
        }

        public static CornerRadius GetCornerRadius(string key, CornerRadius fallback = default)
        {
            return GetCornerRadius(Application.Current.Resources, key, fallback);
        }

        public static Brush GetBrush(string key, Brush? fallback = null)
        {
            return GetBrush(Application.Current.Resources, key, fallback ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent));
        }

        // ---- Lightweight Styling Helpers ----

        /// <summary>
        /// Attempts to get a control-specific brush resource for lightweight styling.
        /// </summary>
        /// <param name="control">The control to check local resources from.</param>
        /// <param name="controlName">The control name prefix (e.g., "DaisyButton", "DaisyModal").</param>
        /// <param name="suffix">The resource suffix (e.g., "Background", "PrimaryBackground", "Foreground").</param>
        /// <returns>The brush if found, or <c>null</c> to signal fallback to default palette logic.</returns>
        /// <remarks>
        /// <para><b>Lookup Priority:</b></para>
        /// <list type="number">
        ///   <item>Control.Resources (instance-level lightweight styling)</item>
        ///   <item>Application.Current.Resources (app-level overrides)</item>
        ///   <item>Returns <c>null</c> → caller falls back to palette-based logic</item>
        /// </list>
        /// <para><b>XAML Usage (Instance-Level):</b></para>
        /// <code language="xml">
        /// &lt;daisy:DaisyButton Content="Custom"&gt;
        ///     &lt;daisy:DaisyButton.Resources&gt;
        ///         &lt;SolidColorBrush x:Key="DaisyButtonBackground" Color="Coral"/&gt;
        ///     &lt;/daisy:DaisyButton.Resources&gt;
        /// &lt;/daisy:DaisyButton&gt;
        /// </code>
        /// <para><b>XAML Usage (App-Level):</b></para>
        /// <code language="xml">
        /// &lt;Application.Resources&gt;
        ///     &lt;SolidColorBrush x:Key="DaisyButtonBackground" Color="MediumSlateBlue"/&gt;
        /// &lt;/Application.Resources&gt;
        /// </code>
        /// <para><b>Common Resource Keys:</b> Background, Foreground, BorderBrush,
        /// PrimaryBackground, PrimaryForeground, ActiveBackground, HoverBackground, etc.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In control's ApplyColors method:
        /// var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "Background");
        /// var variantBg = DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "PrimaryBackground");
        /// _rootBorder.Background = variantBg ?? bgOverride ?? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
        /// </code>
        /// </example>
        public static Brush? TryGetControlBrush(FrameworkElement control, string controlName, string suffix)
        {
            var key = $"{controlName}{suffix}";

            // 1. Check control-level resources first (lightweight styling on specific instance)
            if (control.Resources != null && control.Resources.TryGetValue(key, out var local) && local is Brush localBrush)
                return localBrush;

            // 2. Check application resources (app-level overrides)
            if (TryGetResource(Application.Current.Resources, key, out var app) && app is Brush appBrush)
                return appBrush;

            return null;
        }

        /// <summary>
        /// Attempts to get a control-specific <see cref="CornerRadius"/> resource.
        /// </summary>
        /// <param name="control">The control to check local resources from.</param>
        /// <param name="controlName">The control name prefix (e.g., "DaisyCard", "DaisyModal").</param>
        /// <param name="suffix">The resource suffix (e.g., "CornerRadius").</param>
        /// <returns>The corner radius if found, or <c>null</c> to signal fallback.</returns>
        /// <example>
        /// <code>
        /// var radiusOverride = DaisyResourceLookup.TryGetControlCornerRadius(this, "DaisyCard", "CornerRadius");
        /// _rootBorder.CornerRadius = radiusOverride ?? new CornerRadius(12);
        /// </code>
        /// </example>
        public static CornerRadius? TryGetControlCornerRadius(FrameworkElement control, string controlName, string suffix)
        {
            var key = $"{controlName}{suffix}";

            if (control.Resources != null && control.Resources.TryGetValue(key, out var local) && local is CornerRadius localRadius)
                return localRadius;

            if (TryGetResource(Application.Current.Resources, key, out var app) && app is CornerRadius appRadius)
                return appRadius;

            return null;
        }

        /// <summary>
        /// Attempts to get a control-specific <see cref="double"/> resource.
        /// </summary>
        /// <param name="control">The control to check local resources from.</param>
        /// <param name="controlName">The control name prefix (e.g., "DaisyProgress", "DaisyRange").</param>
        /// <param name="suffix">The resource suffix (e.g., "Height", "TrackHeight", "ThumbSize").</param>
        /// <returns>The double value if found, or <c>null</c> to signal fallback.</returns>
        /// <remarks>Accepts <c>double</c>, <c>float</c>, or <c>int</c> resource values.</remarks>
        /// <example>
        /// <code>
        /// var heightOverride = DaisyResourceLookup.TryGetControlDouble(this, "DaisyRange", "TrackHeight");
        /// _track.Height = heightOverride ?? DaisyResourceLookup.GetDefaultRangeTrackHeight(Size);
        /// </code>
        /// </example>
        public static double? TryGetControlDouble(FrameworkElement control, string controlName, string suffix)
        {
            var key = $"{controlName}{suffix}";

            if (control.Resources != null && control.Resources.TryGetValue(key, out var local))
            {
                if (local is double d) return d;
                if (local is float f) return f;
                if (local is int i) return i;
            }

            if (TryGetResource(Application.Current.Resources, key, out var app))
            {
                if (app is double d) return d;
                if (app is float f) return f;
                if (app is int i) return i;
            }

            return null;
        }

        /// <summary>
        /// Attempts to get a control-specific <see cref="Thickness"/> resource.
        /// </summary>
        /// <param name="control">The control to check local resources from.</param>
        /// <param name="controlName">The control name prefix (e.g., "DaisyButton", "DaisyInput").</param>
        /// <param name="suffix">The resource suffix (e.g., "Padding", "BorderThickness").</param>
        /// <returns>The thickness if found, or <c>null</c> to signal fallback.</returns>
        /// <example>
        /// <code>
        /// var paddingOverride = DaisyResourceLookup.TryGetControlThickness(this, "DaisyInput", "Padding");
        /// _innerContent.Padding = paddingOverride ?? DaisyResourceLookup.GetDefaultPadding(Size);
        /// </code>
        /// </example>
        public static Thickness? TryGetControlThickness(FrameworkElement control, string controlName, string suffix)
        {
            var key = $"{controlName}{suffix}";

            if (control.Resources != null && control.Resources.TryGetValue(key, out var local) && local is Thickness localThickness)
                return localThickness;

            if (TryGetResource(Application.Current.Resources, key, out var app) && app is Thickness appThickness)
                return appThickness;

            return null;
        }

        // ---- Size Key Helpers (centralized to avoid duplication across 22+ controls) ----

        /// <summary>
        /// Converts a DaisySize enum to its abbreviated string key for resource lookups.
        /// Example: DaisySize.ExtraSmall -> "XS", DaisySize.Medium -> "M"
        /// Use this for resources like "DaisyBadgeXSHeight".
        /// </summary>
        public static string GetSizeKey(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => "XS",
            DaisySize.Small => "S",
            DaisySize.Medium => "M",
            DaisySize.Large => "L",
            DaisySize.ExtraLarge => "XL",
            _ => "M"
        };

        /// <summary>
        /// Converts a DaisySize enum to its full string key for resource lookups.
        /// Example: DaisySize.ExtraSmall -> "ExtraSmall", DaisySize.Medium -> "Medium"
        /// Use this for resources like "DaisySizeExtraSmallHeight".
        /// </summary>
        public static string GetSizeKeyFull(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => "ExtraSmall",
            DaisySize.Small => "Small",
            DaisySize.Medium => "Medium",
            DaisySize.Large => "Large",
            DaisySize.ExtraLarge => "ExtraLarge",
            _ => "Medium"
        };

        // ---- Size Resource Helpers ----

        public static double GetSizeDouble(ResourceDictionary? resources, string prefix, DaisySize size, string suffix, double fallback)
        {
            var key = prefix + GetSizeKeyFull(size) + suffix;
            return GetDouble(resources, key, fallback);
        }

        public static Thickness GetSizeThickness(ResourceDictionary? resources, string prefix, DaisySize size, string suffix, Thickness fallback)
        {
            var key = prefix + GetSizeKeyFull(size) + suffix;
            return GetThickness(resources, key, fallback);
        }

        public static CornerRadius GetSizeCornerRadius(ResourceDictionary? resources, string prefix, DaisySize size, string suffix, CornerRadius fallback)
        {
            var key = prefix + GetSizeKeyFull(size) + suffix;
            return GetCornerRadius(resources, key, fallback);
        }

        /// <summary>
        /// Gets the default icon size in pixels for a given DaisySize.
        /// </summary>
        public static double GetDefaultIconSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 8,
            DaisySize.Small => 10,
            DaisySize.Medium => 14,
            DaisySize.Large => 16,
            DaisySize.ExtraLarge => 18,
            _ => 14
        };

        /// <summary>
        /// Gets the default icon-to-text spacing for a given DaisySize.
        /// </summary>
        public static double GetDefaultIconSpacing(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 3,
            DaisySize.Small => 4,
            DaisySize.Medium => 6,
            DaisySize.Large => 6,
            DaisySize.ExtraLarge => 8,
            _ => 6
        };

        /// <summary>
        /// Gets the standard design token spacing for a given DaisySize.
        /// </summary>
        public static double GetSpacing(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => GetDouble("DaisySpacingXS", 3),
            DaisySize.Small => GetDouble("DaisySpacingSmall", 6),
            DaisySize.Medium => GetDouble("DaisySpacingMedium", 10),
            DaisySize.Large => GetDouble("DaisySpacingLarge", 14),
            DaisySize.ExtraLarge => GetDouble("DaisySpacingXL", 20),
            _ => GetDouble("DaisySpacingMedium", 10)
        };

        // ---- Default Control Sizing Helpers ----

        /// <summary>
        /// Gets the default control height for a given DaisySize.
        /// </summary>
        public static double GetDefaultHeight(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 22,
            DaisySize.Small => 24,
            DaisySize.Medium => 28,
            DaisySize.Large => 32,
            DaisySize.ExtraLarge => 36,
            _ => 28
        };

        /// <summary>
        /// Gets the default elevation (shadow depth) for neumorphic effects based on DaisySize.
        /// Values are roughly 40-45% of the control height.
        /// </summary>
        public static double GetDefaultElevation(DaisySize size) => size switch
        {
            // DaisySize.ExtraSmall => 10,
            // DaisySize.Small => 11,
            // DaisySize.Medium => 12,
            // DaisySize.Large => 14,
            // DaisySize.ExtraLarge => 16,
            // _ => 12
            DaisySize.ExtraSmall => 20,
            DaisySize.Small => 21,
            DaisySize.Medium => 22,
            DaisySize.Large => 24,
            DaisySize.ExtraLarge => 26,
            _ => 22
        };

        /// <summary>
        /// Gets the default floating input height (taller inputs like NumericUpDown) for a given DaisySize.
        /// </summary>
        public static double GetDefaultFloatingInputHeight(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 28,
            DaisySize.Small => 32,
            DaisySize.Medium => 36,
            DaisySize.Large => 40,
            DaisySize.ExtraLarge => 44,
            _ => 36
        };

        /// <summary>
        /// Gets the spinner button padding for NumericUpDown controls.
        /// </summary>
        public static double GetDefaultSpinnerButtonPadding(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 1,
            DaisySize.Small => 2,
            DaisySize.Medium => 3,
            DaisySize.Large => 3,
            DaisySize.ExtraLarge => 4,
            _ => 3
        };

        /// <summary>
        /// Gets the spinner buttons grid width for NumericUpDown controls.
        /// </summary>
        public static double GetDefaultSpinnerGridWidth(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 20,
            DaisySize.Small => 24,
            DaisySize.Medium => 28,
            DaisySize.Large => 32,
            DaisySize.ExtraLarge => 36,
            _ => 28
        };

        /// <summary>
        /// Gets the spinner icon size for NumericUpDown/spinner controls.
        /// These are smaller than general icons since two are stacked vertically.
        /// Calculation: (floatingHeight / 2) - (2 * buttonPadding) - safety margin
        /// </summary>
        public static double GetDefaultSpinnerIconSize(DaisySize size) => size switch
        {
            // ExtraSmall: row=14, padding=1 → available=12, use 8
            DaisySize.ExtraSmall => 8,
            // Small: row=16, padding=2 → available=12, use 10
            DaisySize.Small => 10,
            // Medium: row=18, padding=3 → available=12, use 10
            DaisySize.Medium => 10,
            // Large: row=20, padding=3 → available=14, use 12
            DaisySize.Large => 12,
            // ExtraLarge: row=22, padding=4 → available=14, use 12
            DaisySize.ExtraLarge => 12,
            _ => 10
        };

        /// <summary>
        /// Gets the default font size for a given DaisySize.
        /// </summary>
        public static double GetDefaultFontSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 8,
            DaisySize.Small => 10,
            DaisySize.Medium => 12,
            DaisySize.Large => 14,
            DaisySize.ExtraLarge => 16,
            _ => 12
        };

        /// <summary>
        /// Gets the header font size for a given DaisySize (larger, for countdown/animated numbers).
        /// </summary>
        public static double GetDefaultHeaderFontSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 14,
            DaisySize.Small => 16,
            DaisySize.Medium => 20,
            DaisySize.Large => 24,
            DaisySize.ExtraLarge => 28,
            _ => 20
        };

        /// <summary>
        /// Gets the radial progress size for a given DaisySize (avatar-like circular dimensions).
        /// </summary>
        public static double GetDefaultRadialProgressSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 24,
            DaisySize.Small => 32,
            DaisySize.Medium => 48,
            DaisySize.Large => 96,
            DaisySize.ExtraLarge => 128,
            _ => 48
        };

        /// <summary>
        /// Gets the radial progress stroke thickness for a given DaisySize.
        /// </summary>
        public static double GetDefaultRadialProgressThickness(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 2,
            DaisySize.Small => 3,
            DaisySize.Medium => 4,
            DaisySize.Large => 6,
            DaisySize.ExtraLarge => 8,
            _ => 4
        };

        /// <summary>
        /// Gets the radial progress font size for a given DaisySize.
        /// </summary>
        public static double GetDefaultRadialProgressFontSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 7,
            DaisySize.Small => 9,
            DaisySize.Medium => 12,
            DaisySize.Large => 20,
            DaisySize.ExtraLarge => 28,
            _ => 12
        };

        /// <summary>
        /// Gets the range track height for a given DaisySize.
        /// </summary>
        public static double GetDefaultRangeTrackHeight(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 4,
            DaisySize.Small => 6,
            DaisySize.Medium => 8,
            DaisySize.Large => 12,
            DaisySize.ExtraLarge => 16,
            _ => 8
        };

        /// <summary>
        /// Gets the range thumb size for a given DaisySize.
        /// </summary>
        public static double GetDefaultRangeThumbSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 12,
            DaisySize.Small => 16,
            DaisySize.Medium => 20,
            DaisySize.Large => 28,
            DaisySize.ExtraLarge => 36,
            _ => 20
        };

        /// <summary>
        /// Gets the rating star size for a given DaisySize.
        /// </summary>
        public static double GetDefaultRatingStarSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 16,
            DaisySize.Small => 20,
            DaisySize.Medium => 24,
            DaisySize.Large => 32,
            DaisySize.ExtraLarge => 40,
            _ => 24
        };

        /// <summary>
        /// Gets the rating star spacing for a given DaisySize.
        /// </summary>
        public static double GetDefaultRatingSpacing(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 2,
            DaisySize.Small => 3,
            DaisySize.Medium => 4,
            DaisySize.Large => 6,
            DaisySize.ExtraLarge => 8,
            _ => 4
        };

        /// <summary>
        /// Gets the menu/list row padding for a given DaisySize.
        /// </summary>
        public static Thickness GetDefaultMenuPadding(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => new Thickness(8, 4, 8, 4),
            DaisySize.Small => new Thickness(10, 6, 10, 6),
            DaisySize.Medium => new Thickness(10, 6, 10, 6),
            DaisySize.Large => new Thickness(12, 8, 12, 8),
            DaisySize.ExtraLarge => new Thickness(12, 8, 12, 8),
            _ => new Thickness(10, 6, 10, 6)
        };

        /// <summary>
        /// Gets the tag/chip padding for a given DaisySize.
        /// </summary>
        public static Thickness GetDefaultTagPadding(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => new Thickness(4, 2, 4, 2),
            DaisySize.Small => new Thickness(6, 3, 6, 3),
            DaisySize.Medium => new Thickness(8, 4, 8, 4),
            DaisySize.Large => new Thickness(10, 5, 10, 5),
            DaisySize.ExtraLarge => new Thickness(12, 6, 12, 6),
            _ => new Thickness(8, 4, 8, 4)
        };

        /// <summary>
        /// Gets the OTP slot size for a given DaisySize.
        /// </summary>
        public static double GetDefaultOtpSlotSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 32,
            DaisySize.Small => 40,
            DaisySize.Medium => 48,
            DaisySize.Large => 56,
            DaisySize.ExtraLarge => 64,
            _ => 48
        };

        /// <summary>
        /// Gets the OTP slot spacing for a given DaisySize.
        /// </summary>
        public static double GetDefaultOtpSpacing(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 4,
            DaisySize.Small => 5,
            DaisySize.Medium => 6,
            DaisySize.Large => 8,
            DaisySize.ExtraLarge => 10,
            _ => 6
        };

        /// <summary>
        /// Gets the color swatch size for theme/color pickers for a given DaisySize.
        /// </summary>
        public static double GetDefaultSwatchSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 6,
            DaisySize.Small => 8,
            DaisySize.Medium => 10,
            DaisySize.Large => 12,
            DaisySize.ExtraLarge => 14,
            _ => 10
        };

        /// <summary>
        /// Gets the default padding for a given DaisySize.
        /// </summary>
        public static Thickness GetDefaultPadding(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => new Thickness(6, 0, 6, 0),
            DaisySize.Small => new Thickness(8, 0, 8, 0),
            DaisySize.Medium => new Thickness(10, 0, 10, 0),
            DaisySize.Large => new Thickness(12, 0, 12, 0),
            DaisySize.ExtraLarge => new Thickness(14, 0, 14, 0),
            _ => new Thickness(10, 0, 10, 0)
        };

        /// <summary>
        /// Gets the default corner radius for a given DaisySize.
        /// </summary>
        public static CornerRadius GetDefaultCornerRadius(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => new CornerRadius(2),
            DaisySize.Small => new CornerRadius(4),
            DaisySize.Medium => new CornerRadius(6),
            DaisySize.Large => new CornerRadius(8),
            DaisySize.ExtraLarge => new CornerRadius(10),
            _ => new CornerRadius(6)
        };

        /// <summary>
        /// Gets the default size value (width/height) for controls that use simple pixel sizing.
        /// Used by DaisyLoading, DaisyStatusIndicator, etc.
        /// </summary>
        public static double GetSizeValue(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 14,
            DaisySize.Small => 20,
            DaisySize.Medium => 26,
            DaisySize.Large => 38,
            DaisySize.ExtraLarge => 52,
            _ => 26
        };

        // ---- Status Indicator Sizing Helpers ----

        /// <summary>
        /// Gets the base padding for a status indicator size.
        /// </summary>
        public static double GetStatusIndicatorPadding(DaisySize size)
        {
            var key = $"DaisyStatusIndicator{GetSizeKeyFull(size)}Padding";
            return GetDouble(key, size switch
            {
                DaisySize.ExtraSmall => 2,
                DaisySize.Small => 3,
                DaisySize.Medium => 4,
                DaisySize.Large => 6,
                DaisySize.ExtraLarge => 8,
                _ => 4
            });
        }

        /// <summary>
        /// Gets the extra padding for status indicator variants that extend beyond the dot.
        /// </summary>
        public static double GetStatusIndicatorEffectPadding(DaisySize size)
        {
            var key = $"DaisyStatusIndicator{GetSizeKeyFull(size)}EffectPadding";
            return GetDouble(key, size switch
            {
                DaisySize.ExtraSmall => 8,
                DaisySize.Small => 10,
                DaisySize.Medium => 12,
                DaisySize.Large => 15,
                DaisySize.ExtraLarge => 20,
                _ => 12
            });
        }

        // ---- Avatar-specific Sizing Helpers ----

        /// <summary>
        /// Gets the avatar size (width/height) for a given DaisySize.
        /// </summary>
        public static double GetAvatarSize(DaisySize size)
        {
            var key = $"DaisyAvatar{GetSizeKeyFull(size)}Size";
            return GetDouble(key, size switch
            {
                DaisySize.ExtraSmall => 22,
                DaisySize.Small => 28,
                DaisySize.Medium => 40,
                DaisySize.Large => 56,
                DaisySize.ExtraLarge => 80,
                _ => 40
            });
        }

        /// <summary>
        /// Gets the avatar status indicator size for a given DaisySize.
        /// </summary>
        public static double GetAvatarStatusSize(DaisySize size)
        {
            var key = $"DaisyAvatarStatus{GetSizeKeyFull(size)}Size";
            return GetDouble(key, size switch
            {
                DaisySize.ExtraSmall => 5,
                DaisySize.Small => 7,
                DaisySize.Medium => 10,
                DaisySize.Large => 14,
                DaisySize.ExtraLarge => 16,
                _ => 10
            });
        }

        /// <summary>
        /// Gets the avatar placeholder font size for a given DaisySize.
        /// </summary>
        public static double GetAvatarFontSize(DaisySize size)
        {
            var key = $"DaisyAvatarFont{GetSizeKeyFull(size)}Size";
            return GetDouble(key, size switch
            {
                DaisySize.ExtraSmall => 7,
                DaisySize.Small => 9,
                DaisySize.Medium => 12,
                DaisySize.Large => 16,
                DaisySize.ExtraLarge => 20,
                _ => 12
            });
        }

        /// <summary>
        /// Gets the avatar icon size for a given DaisySize.
        /// </summary>
        public static double GetAvatarIconSize(DaisySize size)
        {
            var key = $"DaisyAvatarIcon{GetSizeKeyFull(size)}Size";
            return GetDouble(key, size switch
            {
                DaisySize.ExtraSmall => 10,
                DaisySize.Small => 14,
                DaisySize.Medium => 18,
                DaisySize.Large => 24,
                DaisySize.ExtraLarge => 34,
                _ => 18
            });
        }

        // ---- Battery Status Indicator Sizing Tokens ----

        /// <summary>
        /// Gets the battery bar width for a given DaisySize.
        /// </summary>
        public static double GetBatteryBarWidth(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 2,
            DaisySize.Small => 3,
            DaisySize.Medium => 4,
            DaisySize.Large => 5,
            DaisySize.ExtraLarge => 7,
            _ => 4
        };

        /// <summary>
        /// Gets the battery bar gap (spacing between bars) for a given DaisySize.
        /// </summary>
        public static double GetBatteryBarGap(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 1,
            DaisySize.Small => 1,
            DaisySize.Medium => 1,
            DaisySize.Large => 3,
            DaisySize.ExtraLarge => 4,
            _ => 1
        };

        /// <summary>
        /// Gets the battery stroke thickness for a given DaisySize.
        /// </summary>
        public static double GetBatteryStroke(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 1,
            DaisySize.Small => 1,
            DaisySize.Medium => 1,
            DaisySize.Large => 2,
            DaisySize.ExtraLarge => 2,
            _ => 1
        };

        /// <summary>
        /// Gets the battery bar corner radius for a given DaisySize.
        /// </summary>
        public static double GetBatteryBarCornerRadius(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 1,
            DaisySize.Small => 1,
            DaisySize.Medium => 1,
            DaisySize.Large => 2,
            DaisySize.ExtraLarge => 2,
            _ => 1
        };

        /// <summary>
        /// Gets the battery shell corner radius for a given DaisySize.
        /// </summary>
        public static double GetBatteryShellCornerRadius(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 2,
            DaisySize.Small => 2,
            DaisySize.Medium => 3,
            DaisySize.Large => 4,
            DaisySize.ExtraLarge => 5,
            _ => 3
        };

        /// <summary>
        /// Gets the battery tip width for a given DaisySize.
        /// </summary>
        public static double GetBatteryTipWidth(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 2,
            DaisySize.Small => 2,
            DaisySize.Medium => 2,
            DaisySize.Large => 3,
            DaisySize.ExtraLarge => 4,
            _ => 2
        };

        /// <summary>
        /// Gets the battery tip corner radius for a given DaisySize.
        /// </summary>
        public static double GetBatteryTipCornerRadius(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 1,
            DaisySize.Small => 1,
            DaisySize.Medium => 1,
            DaisySize.Large => 2,
            DaisySize.ExtraLarge => 2,
            _ => 1
        };

        /// <summary>
        /// Gets the internal padding around the battery bars for a given DaisySize.
        /// </summary>
        public static double GetBatteryInternalPadding(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 0,
            DaisySize.Small => 1,
            DaisySize.Medium => 2,
            DaisySize.Large => 2,
            DaisySize.ExtraLarge => 3,
            _ => 2
        };

        // ---- Palette Detection Helpers ----

        /// <summary>
        /// All known Daisy palette names used for background/content brush pairs.
        /// </summary>
        private static readonly string[] PaletteNames =
        [
            "Primary", "Secondary", "Accent", "Neutral",
            "Info", "Success", "Warning", "Error",
            "Base100", "Base200", "Base300"
        ];

        /// <summary>
        /// Cache of color values to palette names. Invalidated on theme change.
        /// </summary>
        private static readonly Dictionary<Windows.UI.Color, string> _colorToPaletteCache = new();
        private static string? _cachedThemeName;

        /// <summary>
        /// Gets the Daisy palette name (e.g., "Primary", "Secondary") for a given color.
        /// Returns null if the color doesn't match any known palette brush.
        /// </summary>
        /// <param name="color">The color to match against current theme palette brushes.</param>
        /// <returns>The palette name if found, or null if no match.</returns>
        /// <remarks>
        /// Results are cached per theme. Cache is automatically invalidated when
        /// a different theme is detected.
        /// </remarks>
        public static string? GetPaletteNameForColor(Windows.UI.Color color)
        {
            // Check if theme changed - invalidate cache
            var currentTheme = DaisyThemeManager.CurrentThemeName;
            if (_cachedThemeName != currentTheme)
            {
                _colorToPaletteCache.Clear();
                _cachedThemeName = currentTheme;
            }

            // Check cache first
            if (_colorToPaletteCache.TryGetValue(color, out var cached))
                return cached;

            // Look up fresh from current theme
            foreach (var name in PaletteNames)
            {
                var paletteBrush = GetBrush($"Daisy{name}Brush") as SolidColorBrush;
                if (paletteBrush != null && paletteBrush.Color == color)
                {
                    _colorToPaletteCache[color] = name;
                    return name;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the background and content brushes for a given palette name.
        /// </summary>
        /// <param name="paletteName">The palette name (e.g., "Primary", "Secondary", "Base200").</param>
        /// <returns>A tuple containing the background brush and content brush for the palette.</returns>
        /// <remarks>
        /// For Base100/Base200/Base300 palettes, uses DaisyBaseContentBrush as the content brush.
        /// For all other palettes, uses Daisy{Name}ContentBrush.
        /// </remarks>
        public static (Brush? background, Brush? content) GetPaletteBrushes(string? paletteName)
        {
            if (string.IsNullOrEmpty(paletteName))
                return (null, null);

            var backgroundBrush = GetBrush($"Daisy{paletteName}Brush");

            // Base100/Base200/Base300 all use BaseContent
            var contentBrushName = paletteName.StartsWith("Base")
                ? "DaisyBaseContentBrush"
                : $"Daisy{paletteName}ContentBrush";
            var contentBrush = GetBrush(contentBrushName);

            return (backgroundBrush, contentBrush);
        }

        /// <summary>
        /// Gets the background and content brushes for a given color by detecting its palette.
        /// </summary>
        /// <param name="color">The color to match against current theme palette brushes.</param>
        /// <returns>A tuple containing the background brush and content brush, or (null, null) if not found.</returns>
        /// <remarks>
        /// Combines GetPaletteNameForColor and GetPaletteBrushes for convenience.
        /// </remarks>
        public static (Brush? background, Brush? content) GetPaletteBrushesForColor(Windows.UI.Color color)
        {
            var paletteName = GetPaletteNameForColor(color);
            return GetPaletteBrushes(paletteName);
        }

        // ---- Kanban-specific Design Token Helpers ----

        /// <summary>
        /// Gets the Kanban column width for a given DaisySize.
        /// </summary>
        public static double GetKanbanColumnWidth(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 200,
            DaisySize.Small => 240,
            DaisySize.Medium => 280,
            DaisySize.Large => 320,
            DaisySize.ExtraLarge => 380,
            _ => 280
        };

        /// <summary>
        /// Gets the Kanban card spacing for a given DaisySize.
        /// </summary>
        public static double GetKanbanCardSpacing(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 4,
            DaisySize.Small => 6,
            DaisySize.Medium => 8,
            DaisySize.Large => 10,
            DaisySize.ExtraLarge => 12,
            _ => 8
        };

        /// <summary>
        /// Gets the Kanban card padding for a given DaisySize.
        /// </summary>
        public static Thickness GetKanbanCardPadding(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => new Thickness(6),
            DaisySize.Small => new Thickness(8),
            DaisySize.Medium => new Thickness(12),
            DaisySize.Large => new Thickness(14),
            DaisySize.ExtraLarge => new Thickness(16),
            _ => new Thickness(12)
        };

        /// <summary>
        /// Gets the Kanban column padding for a given DaisySize.
        /// </summary>
        public static Thickness GetKanbanColumnPadding(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => new Thickness(6),
            DaisySize.Small => new Thickness(8),
            DaisySize.Medium => new Thickness(12),
            DaisySize.Large => new Thickness(14),
            DaisySize.ExtraLarge => new Thickness(16),
            _ => new Thickness(12)
        };

        /// <summary>
        /// Gets the Kanban card title font size for a given DaisySize.
        /// </summary>
        public static double GetKanbanCardTitleFontSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 10,
            DaisySize.Small => 12,
            DaisySize.Medium => 14,
            DaisySize.Large => 16,
            DaisySize.ExtraLarge => 18,
            _ => 14
        };

        /// <summary>
        /// Gets the Kanban card description font size for a given DaisySize.
        /// </summary>
        public static double GetKanbanCardDescriptionFontSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 8,
            DaisySize.Small => 10,
            DaisySize.Medium => 12,
            DaisySize.Large => 14,
            DaisySize.ExtraLarge => 16,
            _ => 12
        };

        /// <summary>
        /// Gets the Kanban column header font size for a given DaisySize.
        /// </summary>
        public static double GetKanbanColumnHeaderFontSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 12,
            DaisySize.Small => 14,
            DaisySize.Medium => 18,
            DaisySize.Large => 20,
            DaisySize.ExtraLarge => 24,
            _ => 18
        };

        /// <summary>
        /// Gets the Kanban card corner radius for a given DaisySize.
        /// </summary>
        public static CornerRadius GetKanbanCardCornerRadius(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => new CornerRadius(4),
            DaisySize.Small => new CornerRadius(6),
            DaisySize.Medium => new CornerRadius(8),
            DaisySize.Large => new CornerRadius(10),
            DaisySize.ExtraLarge => new CornerRadius(12),
            _ => new CornerRadius(8)
        };

        /// <summary>
        /// Gets the Kanban column corner radius for a given DaisySize.
        /// </summary>
        public static CornerRadius GetKanbanColumnCornerRadius(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => new CornerRadius(6),
            DaisySize.Small => new CornerRadius(8),
            DaisySize.Medium => new CornerRadius(12),
            DaisySize.Large => new CornerRadius(14),
            DaisySize.ExtraLarge => new CornerRadius(16),
            _ => new CornerRadius(12)
        };

        /// <summary>
        /// Gets the Kanban close button size for a given DaisySize.
        /// </summary>
        public static double GetKanbanCloseButtonSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 16,
            DaisySize.Small => 20,
            DaisySize.Medium => 24,
            DaisySize.Large => 28,
            DaisySize.ExtraLarge => 32,
            _ => 24
        };
    }
}

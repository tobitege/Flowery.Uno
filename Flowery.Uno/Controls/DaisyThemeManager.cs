using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// Information about a DaisyUI theme.
    /// </summary>
    public partial class DaisyThemeInfo(string name, bool isDark)
    {
        public string Name { get; } = name;
        public bool IsDark { get; } = isDark;
    }

    /// <summary>
    /// Centralized theme manager for DaisyUI themes (Uno/WinUI).
    /// Swaps a single palette ResourceDictionary into Application.Resources.MergedDictionaries
    /// and raises ThemeChanged.
    /// </summary>
    public static class DaisyThemeManager
    {
        private sealed class ThemeDefinition(DaisyThemeInfo info, Func<ResourceDictionary> paletteFactory)
        {
            public DaisyThemeInfo Info { get; } = info;
            public Func<ResourceDictionary> PaletteFactory { get; } = paletteFactory;
        }

        private static readonly Dictionary<string, ThemeDefinition> ThemesByName =
            new(StringComparer.OrdinalIgnoreCase);

        private static ResourceDictionary? _currentPalette;
        private static string? _currentThemeName;
        private static string _baseThemeName = "Dark";

        /// <summary>
        /// When true, ApplyTheme calls only update internal state without actually applying the theme.
        /// </summary>
        public static bool SuppressThemeApplication { get; set; }

        /// <summary>
        /// Optional custom theme applicator. When set, this delegate is called
        /// instead of the default MergedDictionaries approach.
        /// </summary>
        public static Func<string, bool>? CustomThemeApplicator { get; set; }

        /// <summary>
        /// All available DaisyUI themes.
        /// </summary>
        public static ReadOnlyCollection<DaisyThemeInfo> AvailableThemes { get; private set; } =
            new([]);

        static DaisyThemeManager()
        {
            // Register all themes (DaisyUI + extras)
            RegisterTheme(new DaisyThemeInfo("Abyss", isDark: true), () => DaisyPaletteFactory.Create("Abyss"));
            RegisterTheme(new DaisyThemeInfo("Acid", isDark: false), () => DaisyPaletteFactory.Create("Acid"));
            RegisterTheme(new DaisyThemeInfo("Aqua", isDark: true), () => DaisyPaletteFactory.Create("Aqua"));
            RegisterTheme(new DaisyThemeInfo("Autumn", isDark: false), () => DaisyPaletteFactory.Create("Autumn"));
            RegisterTheme(new DaisyThemeInfo("Black", isDark: true), () => DaisyPaletteFactory.Create("Black"));
            RegisterTheme(new DaisyThemeInfo("Bumblebee", isDark: false), () => DaisyPaletteFactory.Create("Bumblebee"));
            RegisterTheme(new DaisyThemeInfo("Business", isDark: true), () => DaisyPaletteFactory.Create("Business"));
            RegisterTheme(new DaisyThemeInfo("Caramellatte", isDark: false), () => DaisyPaletteFactory.Create("Caramellatte"));
            RegisterTheme(new DaisyThemeInfo("Cmyk", isDark: false), () => DaisyPaletteFactory.Create("Cmyk"));
            RegisterTheme(new DaisyThemeInfo("Coffee", isDark: true), () => DaisyPaletteFactory.Create("Coffee"));
            RegisterTheme(new DaisyThemeInfo("Corporate", isDark: false), () => DaisyPaletteFactory.Create("Corporate"));
            RegisterTheme(new DaisyThemeInfo("Cupcake", isDark: false), () => DaisyPaletteFactory.Create("Cupcake"));
            RegisterTheme(new DaisyThemeInfo("Cyberpunk", isDark: false), () => DaisyPaletteFactory.Create("Cyberpunk"));
            RegisterTheme(new DaisyThemeInfo("Dark", isDark: true), () => DaisyPaletteFactory.Create("Dark"));
            RegisterTheme(new DaisyThemeInfo("Dim", isDark: true), () => DaisyPaletteFactory.Create("Dim"));
            RegisterTheme(new DaisyThemeInfo("Dracula", isDark: true), () => DaisyPaletteFactory.Create("Dracula"));
            RegisterTheme(new DaisyThemeInfo("Emerald", isDark: false), () => DaisyPaletteFactory.Create("Emerald"));
            RegisterTheme(new DaisyThemeInfo("Fantasy", isDark: false), () => DaisyPaletteFactory.Create("Fantasy"));
            RegisterTheme(new DaisyThemeInfo("Forest", isDark: true), () => DaisyPaletteFactory.Create("Forest"));
            RegisterTheme(new DaisyThemeInfo("Garden", isDark: false), () => DaisyPaletteFactory.Create("Garden"));
            RegisterTheme(new DaisyThemeInfo("Halloween", isDark: true), () => DaisyPaletteFactory.Create("Halloween"));
            RegisterTheme(new DaisyThemeInfo("Lemonade", isDark: false), () => DaisyPaletteFactory.Create("Lemonade"));
            RegisterTheme(new DaisyThemeInfo("Light", isDark: false), () => DaisyPaletteFactory.Create("Light"));
            RegisterTheme(new DaisyThemeInfo("Lofi", isDark: false), () => DaisyPaletteFactory.Create("Lofi"));
            RegisterTheme(new DaisyThemeInfo("Luxury", isDark: true), () => DaisyPaletteFactory.Create("Luxury"));
            RegisterTheme(new DaisyThemeInfo("Night", isDark: true), () => DaisyPaletteFactory.Create("Night"));
            RegisterTheme(new DaisyThemeInfo("Nord", isDark: false), () => DaisyPaletteFactory.Create("Nord"));
            RegisterTheme(new DaisyThemeInfo("Pastel", isDark: false), () => DaisyPaletteFactory.Create("Pastel"));
            RegisterTheme(new DaisyThemeInfo("Retro", isDark: false), () => DaisyPaletteFactory.Create("Retro"));
            RegisterTheme(new DaisyThemeInfo("Silk", isDark: false), () => DaisyPaletteFactory.Create("Silk"));
            RegisterTheme(new DaisyThemeInfo("Smooth", isDark: true), () => DaisyPaletteFactory.Create("Smooth"));
            RegisterTheme(new DaisyThemeInfo("Sunset", isDark: true), () => DaisyPaletteFactory.Create("Sunset"));
            RegisterTheme(new DaisyThemeInfo("Synthwave", isDark: true), () => DaisyPaletteFactory.Create("Synthwave"));
            RegisterTheme(new DaisyThemeInfo("Valentine", isDark: false), () => DaisyPaletteFactory.Create("Valentine"));
            RegisterTheme(new DaisyThemeInfo("Winter", isDark: false), () => DaisyPaletteFactory.Create("Winter"));
            RegisterTheme(new DaisyThemeInfo("Wireframe", isDark: false), () => DaisyPaletteFactory.Create("Wireframe"));

            // Happy Hues (https://www.happyhues.co/)
            RegisterTheme(new DaisyThemeInfo("HappyHues01", isDark: false), () => DaisyPaletteFactory.Create("HappyHues01"));
            RegisterTheme(new DaisyThemeInfo("HappyHues02", isDark: false), () => DaisyPaletteFactory.Create("HappyHues02"));
            RegisterTheme(new DaisyThemeInfo("HappyHues03", isDark: false), () => DaisyPaletteFactory.Create("HappyHues03"));
            RegisterTheme(new DaisyThemeInfo("HappyHues04", isDark: true), () => DaisyPaletteFactory.Create("HappyHues04"));
            RegisterTheme(new DaisyThemeInfo("HappyHues05", isDark: false), () => DaisyPaletteFactory.Create("HappyHues05"));
            RegisterTheme(new DaisyThemeInfo("HappyHues06", isDark: false), () => DaisyPaletteFactory.Create("HappyHues06"));
            RegisterTheme(new DaisyThemeInfo("HappyHues07", isDark: false), () => DaisyPaletteFactory.Create("HappyHues07"));
            RegisterTheme(new DaisyThemeInfo("HappyHues08", isDark: false), () => DaisyPaletteFactory.Create("HappyHues08"));
            RegisterTheme(new DaisyThemeInfo("HappyHues09", isDark: false), () => DaisyPaletteFactory.Create("HappyHues09"));
            RegisterTheme(new DaisyThemeInfo("HappyHues10", isDark: true), () => DaisyPaletteFactory.Create("HappyHues10"));
            RegisterTheme(new DaisyThemeInfo("HappyHues11", isDark: false), () => DaisyPaletteFactory.Create("HappyHues11"));
            RegisterTheme(new DaisyThemeInfo("HappyHues12", isDark: true), () => DaisyPaletteFactory.Create("HappyHues12"));
            RegisterTheme(new DaisyThemeInfo("HappyHues13", isDark: true), () => DaisyPaletteFactory.Create("HappyHues13"));
            RegisterTheme(new DaisyThemeInfo("HappyHues14", isDark: false), () => DaisyPaletteFactory.Create("HappyHues14"));
            RegisterTheme(new DaisyThemeInfo("HappyHues15", isDark: false), () => DaisyPaletteFactory.Create("HappyHues15"));
            RegisterTheme(new DaisyThemeInfo("HappyHues16", isDark: true), () => DaisyPaletteFactory.Create("HappyHues16"));
            RegisterTheme(new DaisyThemeInfo("HappyHues17", isDark: false), () => DaisyPaletteFactory.Create("HappyHues17"));
        }

        /// <summary>
        /// Event raised when the theme changes.
        /// </summary>
        public static event EventHandler<string>? ThemeChanged;

        /// <summary>
        /// Gets the currently active theme name.
        /// </summary>
        public static string? CurrentThemeName => _currentThemeName;

        /// <summary>
        /// Gets or sets the base/default theme name (used by theme controllers as the "unchecked" theme).
        /// </summary>
        public static string BaseThemeName
        {
            get => _baseThemeName;
            set => _baseThemeName = value ?? "Light";
        }

        /// <summary>
        /// Gets the "alternate" theme - the current theme if it's not the base theme,
        /// otherwise returns "Dark" as a fallback.
        /// </summary>
        public static string AlternateThemeName
        {
            get
            {
                if (_currentThemeName != null &&
                    !string.Equals(_currentThemeName, _baseThemeName, StringComparison.OrdinalIgnoreCase))
                {
                    return _currentThemeName;
                }

                return "Dark";
            }
        }

        /// <summary>
        /// Registers a theme with a palette factory. Call this to add more DaisyUI themes over time.
        /// </summary>
        public static void RegisterTheme(DaisyThemeInfo info, Func<ResourceDictionary> paletteFactory)
        {
            ArgumentNullException.ThrowIfNull(info);
            ArgumentNullException.ThrowIfNull(paletteFactory);
            if (string.IsNullOrWhiteSpace(info.Name))
                throw new ArgumentException("Theme name cannot be empty.", nameof(info));

            ThemesByName[info.Name] = new ThemeDefinition(info, paletteFactory);
            RebuildThemeList();
        }

        private static void RebuildThemeList()
        {
            var list = new List<DaisyThemeInfo>();
            foreach (var def in ThemesByName.Values)
                list.Add(def.Info);

            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            AvailableThemes = new ReadOnlyCollection<DaisyThemeInfo>(list);
        }

        /// <summary>
        /// Gets theme info by name, or null if not found.
        /// </summary>
        public static DaisyThemeInfo? GetThemeInfo(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                return null;

            return ThemesByName.TryGetValue(themeName, out var def) ? def.Info : null;
        }

        /// <summary>
        /// Apply a theme by name.
        /// </summary>
        public static bool ApplyTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                return false;

            if (!ThemesByName.TryGetValue(themeName, out var def))
            {
                return false;
            }

            // Skip if already applied
            if (string.Equals(_currentThemeName, def.Info.Name, StringComparison.OrdinalIgnoreCase))
                return true;

            // When suppressed, only update internal state without applying
            if (SuppressThemeApplication)
            {
                _currentThemeName = def.Info.Name;
                return true;
            }

            // Use custom applicator if set
            if (CustomThemeApplicator != null)
            {
                var result = CustomThemeApplicator(themeName);
                if (result)
                {
                    SetCurrentTheme(def.Info.Name);
                }
                return result;
            }

            var app = Application.Current;
            if (app == null)
            {
                return false;
            }

            // Ensure token defaults are present (apps can override by defining the keys first).
            DaisyTokenDefaults.EnsureDefaults(app.Resources);

            try
            {
                var newPalette = def.PaletteFactory();
                if (_currentPalette == null)
                {
                    app.Resources.MergedDictionaries.Add(newPalette);
                    _currentPalette = newPalette;
                }
                else
                {
                    if (!app.Resources.MergedDictionaries.Contains(_currentPalette))
                    {
                        app.Resources.MergedDictionaries.Add(_currentPalette);
                    }

                    ApplyPaletteToResourceDictionary(_currentPalette, newPalette);
                }
                _currentThemeName = def.Info.Name;

                // NOTE:
                // Many parts of the Gallery (and likely consumer apps) use {ThemeResource Daisy...Brush} or {StaticResource Daisy...Brush}.
                // Swapping MergedDictionaries alone does NOT reliably refresh those bindings unless the app theme flips Light<->Dark.
                // To make theme switches always update (even Dark->Dark), we keep stable SolidColorBrush instances in Application.Resources
                // and only mutate their Color values on each apply.
                ApplyPaletteToApplicationResources(app.Resources, newPalette);

                // Update neumorphic shadow colors based on theme
                UpdateNeumorphicShadowColors(app.Resources, def.Info.IsDark);

                // Optional WinUI mappings:
                // Some resource keys / theme properties can throw COMException on certain Uno backends.
                // Theme switching should still work even if these mappings fail, so we apply them best-effort.
                if (app.Resources.TryGetValue("DaisyBase100Brush", out var base100Obj) && base100Obj is Brush base100Brush)
                    TrySetAppResource(app.Resources, "ApplicationPageBackgroundThemeBrush", base100Brush);

                // NOTE: We'll avoid overriding WinUI system foreground keys here, since those were the primary
                // suspects for COMException and they aren't needed for Daisy-styled controls.

                TrySetRequestedTheme(app, def.Info.IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light);

                ThemeChanged?.Invoke(null, def.Info.Name);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void TrySetAppResource(ResourceDictionary appResources, string key, object value)
        {
            try
            {
                appResources[key] = value;
            }
            catch (Exception)
            {
            }
        }

        private static void TrySetRequestedTheme(Application app, ApplicationTheme theme)
        {
            // URGENT:
            // Do NOT early-return for ApplicationTheme.Light.
            //
            // We intentionally flip Application.RequestedTheme both ways (Light <-> Dark) because:
            // - WinUI system controls (notably ScrollViewer/ScrollBar) take colors from the active theme.
            //   If we ever set RequestedTheme=Dark but never allow switching back to Light, scrollbars can
            //   remain in "Dark" styling while the Daisy palette is a light theme, making the thumb nearly
            //   invisible on light backgrounds (this shows up especially on Desktop/Skia).
            // - Some ThemeResource updates only refresh reliably when the app theme actually flips.

            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue is null || !dispatcherQueue.HasThreadAccess)
            {
                return;
            }

            try
            {
                if (app.RequestedTheme == theme)
                    return;

                app.RequestedTheme = theme;
            }
            catch { }
        }

        private static void ApplyPaletteToApplicationResources(ResourceDictionary appResources, ResourceDictionary palette)
        {
            // Do NOT enumerate the ResourceDictionary here.
            // Uno/WinUI ResourceDictionary enumeration can be platform-dependent; instead, copy known keys.
            // We keep stable SolidColorBrush instances in Application.Resources and only mutate Color.
            var prefixes = new[]
            {
                "DaisyPrimary", "DaisyPrimaryFocus", "DaisyPrimaryContent",
                "DaisySecondary", "DaisySecondaryFocus", "DaisySecondaryContent",
                "DaisyAccent", "DaisyAccentFocus", "DaisyAccentContent",
                "DaisyNeutral", "DaisyNeutralFocus", "DaisyNeutralContent",
                "DaisyBase100", "DaisyBase200", "DaisyBase300", "DaisyBaseContent",
                "DaisyInfo", "DaisyInfoContent",
                "DaisySuccess", "DaisySuccessContent",
                "DaisyWarning", "DaisyWarningContent",
                "DaisyError", "DaisyErrorContent"
            };

            foreach (var prefix in prefixes)
            {
                var colorKey = prefix + "Color";
                var brushKey = prefix + "Brush";

                // Prefer the Color entry (value type) as source-of-truth.
                if (palette.TryGetValue(colorKey, out var colorObj) && colorObj is Windows.UI.Color color)
                {
                    appResources[colorKey] = color;

                    if (DaisyResourceLookup.TryGetResource(appResources, brushKey, out var existingBrushObj)
                        && existingBrushObj is SolidColorBrush existingBrush)
                    {
                        existingBrush.Color = color;
                    }
                    else
                    {
                        appResources[brushKey] = new SolidColorBrush(color);
                    }

                    continue;
                }

                // Fallback: if only a brush exists in the palette, clone/mutate from it.
                if (palette.TryGetValue(brushKey, out var brushObj) && brushObj is SolidColorBrush paletteBrush)
                {
                    if (DaisyResourceLookup.TryGetResource(appResources, brushKey, out var existingBrushObj)
                        && existingBrushObj is SolidColorBrush existingBrush)
                    {
                        existingBrush.Color = paletteBrush.Color;
                    }
                    else
                    {
                        appResources[brushKey] = new SolidColorBrush(paletteBrush.Color);
                    }
                }
            }
        }

        private static void ApplyPaletteToResourceDictionary(ResourceDictionary target, ResourceDictionary palette)
        {
            var prefixes = new[]
            {
                "DaisyPrimary", "DaisyPrimaryFocus", "DaisyPrimaryContent",
                "DaisySecondary", "DaisySecondaryFocus", "DaisySecondaryContent",
                "DaisyAccent", "DaisyAccentFocus", "DaisyAccentContent",
                "DaisyNeutral", "DaisyNeutralFocus", "DaisyNeutralContent",
                "DaisyBase100", "DaisyBase200", "DaisyBase300", "DaisyBaseContent",
                "DaisyInfo", "DaisyInfoContent",
                "DaisySuccess", "DaisySuccessContent",
                "DaisyWarning", "DaisyWarningContent",
                "DaisyError", "DaisyErrorContent"
            };

            foreach (var prefix in prefixes)
            {
                var colorKey = prefix + "Color";
                var brushKey = prefix + "Brush";

                if (palette.TryGetValue(colorKey, out var colorObj) && colorObj is Windows.UI.Color color)
                {
                    target[colorKey] = color;

                    if (target.TryGetValue(brushKey, out var existingBrushObj) && existingBrushObj is SolidColorBrush existingBrush)
                    {
                        existingBrush.Color = color;
                    }
                    else
                    {
                        target[brushKey] = new SolidColorBrush(color);
                    }

                    continue;
                }

                if (palette.TryGetValue(brushKey, out var brushObj) && brushObj is SolidColorBrush paletteBrush)
                {
                    if (target.TryGetValue(brushKey, out var existingBrushObj) && existingBrushObj is SolidColorBrush existingBrush)
                    {
                        existingBrush.Color = paletteBrush.Color;
                    }
                    else
                    {
                        target[brushKey] = new SolidColorBrush(paletteBrush.Color);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the current theme name and fires the ThemeChanged event.
        /// Used by custom theme applicators to update internal state after applying a theme.
        /// </summary>
        public static void SetCurrentTheme(string themeName)
        {
            if (string.Equals(_currentThemeName, themeName, StringComparison.OrdinalIgnoreCase))
                return;

            _currentThemeName = themeName;
            ThemeChanged?.Invoke(null, themeName);
        }

        /// <summary>
        /// Check if a theme is a dark theme.
        /// </summary>
        public static bool IsDarkTheme(string themeName)
        {
            var info = GetThemeInfo(themeName);
            return info?.IsDark ?? false;
        }

        /// <summary>
        /// Updates the global neumorphic shadow colors based on the current theme's base colors.
        /// </summary>
        private static void UpdateNeumorphicShadowColors(ResourceDictionary appResources, bool isDark)
        {
            try
            {
                // Get Base300 (darkest surface) from theme for shadow derivation
                Windows.UI.Color base300 = Windows.UI.Color.FromArgb(255, 128, 128, 128); // Fallback gray

                if (appResources.TryGetValue("DaisyBase300Color", out var base300Obj) && base300Obj is Windows.UI.Color c300)
                    base300 = c300;

                // Derive shadow colors based on theme brightness
                Windows.UI.Color darkShadow;
                Windows.UI.Color lightShadow;

                if (isDark)
                {
                    // Dark theme: use a deep dark color (not pure black, which blends into dark backgrounds)
                    // Darken Base300 significantly for contrast against dark surfaces
                    darkShadow = DarkenColor(base300, 0.7);
                    lightShadow = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                }
                else
                {
                    // Light theme: dark shadow is darkened Base300, light shadow is white
                    darkShadow = DarkenColor(base300, 0.5);
                    lightShadow = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                }

                // Update global neumorphic settings
                DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor = darkShadow;
                DaisyBaseContentControl.GlobalNeumorphicLightShadowColor = lightShadow;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Darkens a color by a specified factor (0.0 = no change, 1.0 = black).
        /// Always returns a fully opaque color (A=255).
        /// </summary>
        private static Windows.UI.Color DarkenColor(Windows.UI.Color color, double factor)
        {
            factor = Math.Clamp(factor, 0.0, 1.0);
            return Windows.UI.Color.FromArgb(
                255, // Always fully opaque
                (byte)(color.R * (1.0 - factor)),
                (byte)(color.G * (1.0 - factor)),
                (byte)(color.B * (1.0 - factor)));
        }
    }
}

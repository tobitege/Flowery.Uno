using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Flowery.Helpers;
using Flowery.Services;
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

        private static readonly HashSet<string> InternalThemeNames =
            new(StringComparer.OrdinalIgnoreCase);

        private static ResourceDictionary? _currentPalette;
        private static string? _currentThemeName;
        private static string _baseThemeName = "Dark";
        private static HashSet<string> _excludedThemes = new(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> _preferredThemes = new(StringComparer.OrdinalIgnoreCase);
        internal sealed record ThemeEntry(string Name, string DisplayName, bool Preferred, ProductPalette? ProductPalette);
        private static readonly Dictionary<string, ThemeEntry> AddedThemes = new(StringComparer.OrdinalIgnoreCase);
        internal static IReadOnlyDictionary<string, ThemeEntry> ThemeOverrides => AddedThemes;

        /// <summary>
        /// Adds or updates a theme in both dropdowns globally. Local dropdown overrides take
        /// precedence. Product themes are registered without applying their palette.
        /// Call before creating controls or on the UI thread at runtime.
        /// </summary>
        /// <param name="originalName">Original registered or product theme name, matched case-insensitively.</param>
        /// <param name="displayName">Default text displayed by the dropdowns.</param>
        /// <param name="preferred">Whether to place the theme in the preferred group, sorted by display name.</param>
        public static void AddTheme(string originalName, string displayName, bool preferred)
        {
            var entry = CreateThemeEntry(originalName, displayName, preferred);
            if (AddedThemes.TryGetValue(entry.Name, out var existing) && existing == entry) return;
            AddedThemes[entry.Name] = entry;
            if (GetThemeInfo(entry.Name) == null && entry.ProductPalette is { } product)
                EnsureProductThemeRegistered(product);
            else
                NotifyThemeListChanged();
        }

        /// <summary>
        /// Removes global display and ordering overrides. Registered themes remain registered.
        /// Call on the UI thread once controls exist. Returns true when an override was removed.
        /// </summary>
        public static bool RemoveThemeOverride(string originalName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(originalName);
            if (!AddedThemes.Remove(originalName)) return false;
            NotifyThemeListChanged();
            return true;
        }

        internal static ThemeEntry CreateThemeEntry(string originalName, string displayName, bool preferred)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(originalName);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            var registered = GetThemeInfo(originalName);
            var product = ProductPaletteFactory.FindByName(originalName);
            var name = registered?.Name ?? product?.Name
                ?? throw new ArgumentException($"Unknown theme '{originalName}'.", nameof(originalName));
            return new ThemeEntry(name, displayName, preferred, product);
        }

        /// <summary>
        /// Theme names hidden from both theme dropdowns. Assign before constructing the UI,
        /// or replace on the UI thread at runtime. The getter returns a read-only snapshot.
        /// Names are case-insensitive and may refer to themes not yet registered.
        /// This does not prevent <see cref="ApplyTheme"/> from applying a hidden theme.
        /// </summary>
        public static IReadOnlyCollection<string> ExcludedThemes
        {
            get => _excludedThemes.ToList().AsReadOnly();
            set
            {
                var names = ToNameSet(value);
                if (_excludedThemes.SetEquals(names)) return;
                _excludedThemes = names;
                NotifyThemeListChanged();
            }
        }

        /// <summary>
        /// Theme names shown in the preferred group, sorted by display text.
        /// An empty collection clears these defaults. Explicit AddTheme flags override them.
        /// Names are case-insensitive; the getter returns a read-only snapshot.
        /// Exclusion takes precedence. This neither selects nor applies a theme.
        /// Set before constructing the UI or on the UI thread at runtime.
        /// </summary>
        public static IReadOnlyCollection<string> PreferredThemes
        {
            get => _preferredThemes.ToList().AsReadOnly();
            set
            {
                var names = ToNameSet(value);
                if (_preferredThemes.SetEquals(names)) return;
                _preferredThemes = names;
                NotifyThemeListChanged();
            }
        }

        internal static bool IsThemePreferred(string themeName) => _preferredThemes.Contains(themeName);

        /// <summary>
        /// Hides a theme from both dropdowns, including themes registered later.
        /// Call on the UI thread once the UI exists. Returns true if the exclusion changed.
        /// </summary>
        public static bool HideTheme(string themeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(themeName);
            if (!_excludedThemes.Add(themeName)) return false;
            NotifyThemeListChanged();
            return true;
        }

        /// <summary>
        /// Removes a theme exclusion. Call on the UI thread once the UI exists.
        /// Returns true if the exclusion changed; this does not register unknown themes.
        /// </summary>
        public static bool ShowTheme(string themeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(themeName);
            if (!_excludedThemes.Remove(themeName)) return false;
            NotifyThemeListChanged();
            return true;
        }

        /// <summary>
        /// Whether a name is eligible for display, independently of theme registration.
        /// </summary>
        public static bool IsThemeVisible(string themeName) =>
            !string.IsNullOrWhiteSpace(themeName) && !_excludedThemes.Contains(themeName);

        private static HashSet<string> ToNameSet(IReadOnlyCollection<string> value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in value)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);
                names.Add(name);
            }

            return names;
        }

        private static void NotifyThemeListChanged()
        {
            RebuildThemeList();
            AvailableThemesChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// When true, ApplyTheme calls only update internal state without actually applying the theme.
        /// </summary>
        public static bool SuppressThemeApplication { get; set; }

        /// <summary>
        /// When true, <see cref="RegisterTheme"/> also raises <see cref="AvailableThemesChanged"/>
        /// for built-in themes. Default is false.
        /// </summary>
        public static bool NotifyForInternalThemesChanged { get; set; }

        /// <summary>
        /// Optional custom theme applicator. When set, this delegate is called
        /// instead of the default MergedDictionaries approach.
        /// </summary>
        public static Func<string, bool>? CustomThemeApplicator { get; set; }

        /// <summary>
        /// Visible registered themes, with preferred themes first and the rest sorted by name.
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

            InternalThemeNames.UnionWith(ThemesByName.Keys);
        }

        /// <summary>
        /// Event raised when the theme changes.
        /// </summary>
        public static event EventHandler<string>? ThemeChanged;

        /// <summary>
        /// Event raised when <see cref="RegisterTheme"/> adds a new theme name.
        /// Built-in themes raise this event only when <see cref="NotifyForInternalThemesChanged"/> is true.
        /// </summary>
        public static event EventHandler? AvailableThemesChanged;

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

            var isNewTheme = !ThemesByName.ContainsKey(info.Name);
            var isInternalTheme = InternalThemeNames.Contains(info.Name);
            ThemesByName[info.Name] = new ThemeDefinition(info, paletteFactory);
            RebuildThemeList();

            var notifyInternal = isInternalTheme && NotifyForInternalThemesChanged;
            var notifyNew = isNewTheme && !isInternalTheme;
            if (notifyInternal || notifyNew)
            {
                AvailableThemesChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        internal static void EnsureProductThemeRegistered(ProductPalette palette)
        {
            if (GetThemeInfo(palette.Name) != null)
                return;

            var precompiled = ProductPalettes.Get(palette.Name);
            if (precompiled != null)
            {
                var info = new DaisyThemeInfo(palette.Name, FloweryColorHelpers.IsDark(precompiled.Base100));
                RegisterTheme(info, () => DaisyPaletteFactory.Create(precompiled));
                return;
            }

            var fallbackInfo = new DaisyThemeInfo(palette.Name, FloweryColorHelpers.IsDark(palette.Background));
            RegisterTheme(fallbackInfo, () => ProductPaletteFactory.CreateResourceDictionary(palette));
        }

        private static void RebuildThemeList()
        {
            var list = ThemeListResolver.AvailableThemes(ThemesByName.Values.Select(static definition => definition.Info));
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
        /// Gets the palette factory registered for a theme, or null if the theme is unknown.
        /// </summary>
        public static Func<ResourceDictionary>? GetPaletteFactory(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                return null;

            return ThemesByName.TryGetValue(themeName, out var def) ? def.PaletteFactory : null;
        }

        /// <summary>
        /// Creates a palette <see cref="ResourceDictionary"/> from the factory registered for a theme.
        /// </summary>
        /// <param name="themeName">The registered theme name.</param>
        /// <param name="palette">The created palette when the method returns true.</param>
        /// <returns>True when a palette was created; otherwise false.</returns>
        public static bool TryCreatePalette(string themeName, out ResourceDictionary? palette)
        {
            palette = null;
            var factory = GetPaletteFactory(themeName);
            if (factory == null)
                return false;

            try
            {
                palette = factory();
                return palette != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TryCreatePalette: failed for '{themeName}': {ex.Message}");
                palette = null;
                return false;
            }
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
                EnsureDerivedPaletteKeys(newPalette, app.Resources);
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

                // Neumorphic shadow colors are derived from the new theme on next use
                DaisyNeumorphic.InvalidateThemeShadowColors();

                // Optional WinUI mappings:
                // Some resource keys / theme properties can throw COMException on certain Uno backends.
                // Theme switching should still work even if these mappings fail, so we apply them best-effort.
                if (app.Resources.TryGetValue("DaisyBase100Brush", out var base100Obj) && base100Obj is Brush base100Brush)
                    TrySetAppResource(app.Resources, "ApplicationPageBackgroundThemeBrush", base100Brush);

                // NOTE: We'll avoid overriding WinUI system foreground keys here, since those were the primary
                // suspects for COMException and they aren't needed for Daisy-styled controls.

                TrySetRequestedTheme(app, def.Info.IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light);

                FloweryEventRaiser.Raise(ThemeChanged, null, def.Info.Name, nameof(ThemeChanged));
                return true;
            }
            catch (Exception ex)
            {
                FloweryLogging.Log(FloweryLogLevel.Error, "Theming", $"[DaisyThemeManager] Applying '{themeName}' failed: {ex.Message}", ex);
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

        // Variant foregrounds a palette may set; when missing they follow the base hue.
        private static readonly (string Prefix, string Source)[] DerivedForegroundPrefixes =
        [
            ("DaisyPrimaryForeground", "DaisyPrimary"),
            ("DaisySecondaryForeground", "DaisySecondary"),
            ("DaisyAccentForeground", "DaisyAccent"),
            ("DaisyButtonPrimaryForeground", "DaisyPrimary")
        ];

        // Optional keys that are removed when the next palette doesn't define them.
        private static readonly string[] OptionalPrefixes = ["DaisyButtonPrimaryDisabled"];

        private static readonly string[] PalettePrefixes =
        [
            "DaisyPrimary", "DaisyPrimaryFocus", "DaisyPrimaryContent",
            "DaisySecondary", "DaisySecondaryFocus", "DaisySecondaryContent",
            "DaisyAccent", "DaisyAccentFocus", "DaisyAccentContent",
            "DaisyNeutral", "DaisyNeutralFocus", "DaisyNeutralContent",
            "DaisyBase100", "DaisyBase200", "DaisyBase300", "DaisyBaseContent",
            "DaisyInfo", "DaisyInfoContent",
            "DaisySuccess", "DaisySuccessContent",
            "DaisyWarning", "DaisyWarningContent",
            "DaisyError", "DaisyErrorContent",
            "DaisyPrimaryForeground", "DaisySecondaryForeground", "DaisyAccentForeground",
            "DaisyButtonPrimaryForeground", "DaisyButtonPrimaryDisabled",
            "DaisyBaseContentMuted", "DaisyBaseContentSubtle", "DaisyBaseContentDisabled",
            "DaisyButtonPrimarySoft", "DaisyButtonPrimarySoftHover", "DaisyButtonPrimarySoftBorder",
            "DaisyButtonSecondarySoft", "DaisyButtonSecondarySoftHover", "DaisyButtonSecondarySoftBorder",
            "DaisyButtonAccentSoft", "DaisyButtonAccentSoftHover", "DaisyButtonAccentSoftBorder",
            "DaisyButtonInfoSoft", "DaisyButtonInfoSoftHover", "DaisyButtonInfoSoftBorder",
            "DaisyButtonSuccessSoft", "DaisyButtonSuccessSoftHover", "DaisyButtonSuccessSoftBorder",
            "DaisyButtonWarningSoft", "DaisyButtonWarningSoftHover", "DaisyButtonWarningSoftBorder",
            "DaisyButtonErrorSoft", "DaisyButtonErrorSoftHover", "DaisyButtonErrorSoftBorder"
        ];

        /// <summary>
        /// Adds the variant foreground keys to palettes that don't define them (custom factories,
        /// CSS themes), so a theme switch never keeps the previous theme's foregrounds.
        /// </summary>
        private static void EnsureDerivedPaletteKeys(ResourceDictionary palette, ResourceDictionary fallbackResources)
        {
            foreach (var (prefix, source) in DerivedForegroundPrefixes)
            {
                if (palette.ContainsKey(prefix + "Color") || palette.ContainsKey(prefix + "Brush"))
                    continue;

                if (palette.TryGetValue(source + "Color", out var colorObj) && colorObj is Windows.UI.Color color)
                {
                    palette[prefix + "Color"] = color;
                    palette[prefix + "Brush"] = new SolidColorBrush(color);
                }
                else if (palette.TryGetValue(source + "Brush", out var brushObj) && brushObj is SolidColorBrush brush)
                {
                    palette[prefix + "Color"] = brush.Color;
                    palette[prefix + "Brush"] = new SolidColorBrush(brush.Color);
                }
            }
            DaisyPaletteFactory.EnsureSemanticBrushes(palette, fallbackResources);
        }

        private static void RemoveMissingOptionalKeys(ResourceDictionary target, ResourceDictionary palette)
        {
            foreach (var prefix in OptionalPrefixes)
            {
                if (palette.ContainsKey(prefix + "Color") || palette.ContainsKey(prefix + "Brush"))
                    continue;

                target.Remove(prefix + "Color");
                target.Remove(prefix + "Brush");
            }
        }

        private static void ApplyPaletteToApplicationResources(ResourceDictionary appResources, ResourceDictionary palette)
        {
            RemoveMissingOptionalKeys(appResources, palette);

            // Do NOT enumerate the ResourceDictionary here.
            // Uno/WinUI ResourceDictionary enumeration can be platform-dependent; instead, copy known keys.
            // We keep stable SolidColorBrush instances in Application.Resources and only mutate Color.
            foreach (var prefix in PalettePrefixes)
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
            RemoveMissingOptionalKeys(target, palette);

            foreach (var prefix in PalettePrefixes)
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
            DaisyNeumorphic.InvalidateThemeShadowColors();
            FloweryEventRaiser.Raise(ThemeChanged, null, themeName, nameof(ThemeChanged));
        }

        /// <summary>
        /// Check if a theme is a dark theme.
        /// </summary>
        public static bool IsDarkTheme(string themeName)
        {
            var info = GetThemeInfo(themeName);
            return info?.IsDark ?? false;
        }
    }
}

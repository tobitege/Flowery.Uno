using System;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Helpers
{
    /// <summary>
    /// Helper for applying Daisy theme colors to ComboBox controls.
    /// Uses persistent brush instances so {ThemeResource} bindings update correctly on theme change.
    /// </summary>
    public static class ComboBoxThemeHelper
    {
        private const string ThemeDictionaryKey = "__FloweryComboBoxThemeDictionary";
        private const string ThemeDictionaryThreadKey = "__FloweryComboBoxThemeDictionaryThreadId";
        private const string ThemeDictionaryOwnedBrushKeys = "__FloweryComboBoxThemeDictionaryOwnedBrushKeys";

        /// <summary>
        /// Applies Daisy theme colors to a ComboBox's ThemeDictionaries.
        /// Uses persistent brush instances that are updated in-place to ensure {ThemeResource} bindings refresh.
        /// </summary>
        /// <param name="comboBox">The ComboBox to theme.</param>
        /// <param name="appResources">Application resources containing Daisy theme colors.</param>
        /// <param name="transparentItemBackgrounds">If true, item backgrounds are transparent (for items with their own themed content).</param>
        /// <param name="controlHeight">Optional control height to override ComboBoxMinHeight (fixes dropdown glyph clipping at small sizes).</param>
        public static void ApplyTheme(ComboBox comboBox, ResourceDictionary? appResources, bool transparentItemBackgrounds = false, double? controlHeight = null)
        {
            if (appResources == null)
                return;

            var dispatcherQueue = comboBox.DispatcherQueue;
            if (dispatcherQueue is null)
                return;

            if (!dispatcherQueue.HasThreadAccess)
            {
                dispatcherQueue.TryEnqueue(() => ApplyTheme(comboBox, appResources, transparentItemBackgrounds, controlHeight));
                return;
            }

            var currentThreadId = Environment.CurrentManagedThreadId;

            // Get current theme colors
            var transparentColor = Color.FromArgb(0, 0, 0, 0);
            var base100Color = GetColor(appResources, "DaisyBase100Brush", Color.FromArgb(255, 255, 255, 255));
            var base200Color = GetColor(appResources, "DaisyBase200Brush", Color.FromArgb(255, 211, 211, 211));
            var base300Color = GetColor(appResources, "DaisyBase300Brush", Color.FromArgb(255, 128, 128, 128));
            var baseContentColor = GetColor(appResources, "DaisyBaseContentBrush", Color.FromArgb(255, 0, 0, 0));
            var primaryColor = GetColor(appResources, "DaisyPrimaryBrush", Color.FromArgb(255, 0, 0, 255));
            var primaryContentColor = GetColor(appResources, "DaisyPrimaryContentBrush", Color.FromArgb(255, 255, 255, 255));

            var borderOverride = comboBox.BorderBrush;
            var foregroundOverride = comboBox.Foreground;

            // Use solid base200 for hover/pressed to match DaisySelect behavior
            var hoverColor = base200Color;
            var pressedColor = base200Color;

            // Keep a dedicated dictionary per ComboBox to avoid touching sealed theme dictionaries.
            ResourceDictionary? themeDictionary = null;
            for (int i = 0; i < comboBox.Resources.MergedDictionaries.Count; i++)
            {
                var dict = comboBox.Resources.MergedDictionaries[i];
                if (!dict.ContainsKey(ThemeDictionaryKey))
                    continue;

                if (!dict.TryGetValue(ThemeDictionaryThreadKey, out var threadObj) ||
                    threadObj is not int threadId ||
                    threadId != currentThreadId)
                {
                    comboBox.Resources.MergedDictionaries.RemoveAt(i);
                    break;
                }

                themeDictionary = dict;
                break;
            }

            if (themeDictionary == null)
            {
                themeDictionary = new ResourceDictionary
                {
                    [ThemeDictionaryKey] = true,
                    [ThemeDictionaryThreadKey] = currentThreadId
                };
                comboBox.Resources.MergedDictionaries.Add(themeDictionary);
            }

            var ownedBrushKeys = GetOwnedBrushKeys(themeDictionary);

            // Update existing brushes in-place to preserve template bindings,
            // or create new if this is the first call
            // Note: Don't set ComboBoxBackground - the control's Background property handles default state
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxBackgroundPointerOver", hoverColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxBackgroundPressed", pressedColor);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxBorderBrush", borderOverride, base300Color);
            SetHoverBorderBrush(themeDictionary, ownedBrushKeys, "ComboBoxBorderBrushPointerOver", borderOverride, base200Color, 0.2);
            SetHoverBorderBrush(themeDictionary, ownedBrushKeys, "ComboBoxBorderBrushPressed", borderOverride, base200Color, 0.1);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxForeground", foregroundOverride, baseContentColor);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxForegroundPointerOver", foregroundOverride, baseContentColor);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxForegroundPressed", foregroundOverride, baseContentColor);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxForegroundFocused", foregroundOverride, baseContentColor);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxForegroundDisabled", foregroundOverride, baseContentColor);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxPlaceHolderForeground", foregroundOverride, baseContentColor);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxPlaceHolderForegroundPointerOver", foregroundOverride, baseContentColor);
            SetBrushOrColor(themeDictionary, ownedBrushKeys, "ComboBoxPlaceHolderForegroundPressed", foregroundOverride, baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxDropDownGlyphForeground", baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxDropDownGlyphForegroundPointerOver", baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxDropDownGlyphForegroundPressed", baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxDropDownBackground", base100Color);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxDropDownBorderBrush", base300Color);
            themeDictionary["ComboBoxDropDownBorderThickness"] = DaisyResourceLookup.GetThickness(appResources, "DaisyBorderThicknessThin", new Thickness(1));
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBackground", transparentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBackgroundPointerOver", transparentItemBackgrounds ? transparentColor : hoverColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBackgroundPressed", transparentItemBackgrounds ? transparentColor : pressedColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBackgroundSelected", primaryColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBackgroundSelectedUnfocused", primaryColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBackgroundSelectedPointerOver", primaryColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBackgroundSelectedPressed", primaryColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemForeground", baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemForegroundPointerOver", baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemForegroundPressed", baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemForegroundSelected", primaryContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemForegroundSelectedUnfocused", primaryContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemForegroundSelectedPointerOver", primaryContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemForegroundSelectedPressed", primaryContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBorderBrush", transparentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBorderBrushPointerOver", transparentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBorderBrushPressed", transparentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBorderBrushSelected", transparentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemBorderBrushSelectedPointerOver", transparentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemPointerOverBackground", transparentItemBackgrounds ? transparentColor : hoverColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemPointerOverForeground", baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemPressedBackground", transparentItemBackgrounds ? transparentColor : pressedColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemPressedForeground", baseContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemSelectedBackground", primaryColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemSelectedForeground", primaryContentColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemSelectedPointerOverBackground", primaryColor);
            UpdateOrCreateBrush(themeDictionary, ownedBrushKeys, "ComboBoxItemSelectedPointerOverForeground", primaryContentColor);
            themeDictionary["ComboBoxItemBorderThickness"] = new Thickness(0);
            themeDictionary["ComboBoxItemPadding"] = new Thickness(10, 6, 10, 6);

            // Override ComboBoxMinHeight to match control height (fixes dropdown glyph clipping)
            if (controlHeight.HasValue)
            {
                themeDictionary["ComboBoxMinHeight"] = controlHeight.Value;
            }

            if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
            {
                // Uno Skia ComboBox templates use SystemControl* ThemeResources.
                ApplySkiaThemeOverrides(themeDictionary, ownedBrushKeys, base100Color, base200Color, base300Color, baseContentColor, primaryColor, primaryContentColor);
            }

            // Force visual refresh to ensure template picks up updated colors
            comboBox.InvalidateArrange();
        }

        /// <summary>
        /// Updates an owned brush's color in-place, or replaces it if not owned.
        /// In-place updates are only attempted for brushes created by this helper.
        /// </summary>
        private static void UpdateOrCreateBrush(ResourceDictionary dict, ISet<string> ownedBrushKeys, string key, Color color)
        {
            if (dict.TryGetValue(key, out var existing) &&
                existing is SolidColorBrush brush &&
                ownedBrushKeys.Contains(key))
            {
                // Update existing brush in-place
                try
                {
                    brush.Color = color;
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // Fall back to replacing the brush if in-place updates are blocked.
                }
            }

            // Create new brush
            dict[key] = new SolidColorBrush(color);
            ownedBrushKeys.Add(key);
        }

        /// <summary>
        /// Creates a standard ItemContainerStyle for ComboBox items with Daisy theme colors.
        /// </summary>
        public static Style CreateItemContainerStyle(ResourceDictionary? appResources)
        {
            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            var baseContent = appResources != null
                ? DaisyResourceLookup.GetBrush(appResources, "DaisyBaseContentBrush", new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)))
                : new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

            var style = new Style(typeof(ComboBoxItem));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 6, 10, 6)));
            style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, baseContent));
            style.Setters.Add(new Setter(Control.BackgroundProperty, transparent));
            return style;
        }

        private static Color GetColor(ResourceDictionary resources, string key, Color fallback)
        {
            if (resources.TryGetValue(key, out var value) && value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return fallback;
        }

        private static void SetBrushOrColor(ResourceDictionary dict, ISet<string> ownedBrushKeys, string key, Brush? overrideBrush, Color fallback)
        {
            if (overrideBrush is SolidColorBrush solid)
            {
                UpdateOrCreateBrush(dict, ownedBrushKeys, key, solid.Color);
                return;
            }

            if (overrideBrush != null)
            {
                dict[key] = overrideBrush;
                ownedBrushKeys.Remove(key);
                return;
            }

            UpdateOrCreateBrush(dict, ownedBrushKeys, key, fallback);
        }

        private static void SetHoverBorderBrush(ResourceDictionary dict, ISet<string> ownedBrushKeys, string key, Brush? overrideBrush, Color fallback, double lightenFactor)
        {
            if (overrideBrush is SolidColorBrush solid)
            {
                UpdateOrCreateBrush(dict, ownedBrushKeys, key, FloweryColorHelpers.Lighten(solid.Color, lightenFactor));
                return;
            }

            if (overrideBrush != null)
            {
                dict[key] = overrideBrush;
                ownedBrushKeys.Remove(key);
                return;
            }

            UpdateOrCreateBrush(dict, ownedBrushKeys, key, fallback);
        }

        private static HashSet<string> GetOwnedBrushKeys(ResourceDictionary dict)
        {
            if (dict.TryGetValue(ThemeDictionaryOwnedBrushKeys, out var value) &&
                value is HashSet<string> keys)
            {
                return keys;
            }

            var ownedKeys = new HashSet<string>(StringComparer.Ordinal);
            dict[ThemeDictionaryOwnedBrushKeys] = ownedKeys;
            return ownedKeys;
        }

        internal static void ApplySkiaThemeOverrides(ResourceDictionary target, ResourceDictionary appResources)
        {
            var base100Color = GetColor(appResources, "DaisyBase100Brush", Color.FromArgb(255, 255, 255, 255));
            var base200Color = GetColor(appResources, "DaisyBase200Brush", Color.FromArgb(255, 211, 211, 211));
            var base300Color = GetColor(appResources, "DaisyBase300Brush", Color.FromArgb(255, 128, 128, 128));
            var baseContentColor = GetColor(appResources, "DaisyBaseContentBrush", Color.FromArgb(255, 0, 0, 0));
            var primaryColor = GetColor(appResources, "DaisyPrimaryBrush", Color.FromArgb(255, 0, 0, 255));
            var primaryContentColor = GetColor(appResources, "DaisyPrimaryContentBrush", Color.FromArgb(255, 255, 255, 255));

            var ownedBrushKeys = GetOwnedBrushKeys(target);
            ApplySkiaThemeOverrides(target, ownedBrushKeys, base100Color, base200Color, base300Color, baseContentColor, primaryColor, primaryContentColor);
        }

        private static void ApplySkiaThemeOverrides(
            ResourceDictionary dict,
            ISet<string> ownedBrushKeys,
            Color base100Color,
            Color base200Color,
            Color base300Color,
            Color baseContentColor,
            Color primaryColor,
            Color primaryContentColor)
        {
            var disabledForeground = FloweryColorHelpers.WithOpacity(baseContentColor, 0.5);
            var disabledBorder = FloweryColorHelpers.WithOpacity(base300Color, 0.5);
            var disabledBackground = FloweryColorHelpers.WithOpacity(base200Color, 0.6);

            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlForegroundBaseHighBrush", baseContentColor);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlForegroundBaseMediumHighBrush", baseContentColor);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlForegroundBaseMediumLowBrush", base300Color);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlForegroundChromeHighBrush", base300Color);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlForegroundChromeBlackMediumBrush", baseContentColor);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlPageTextBaseHighBrush", baseContentColor);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlDescriptionTextForegroundBrush", baseContentColor);

            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlBackgroundAltMediumLowBrush", base200Color);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlPageBackgroundAltMediumBrush", base200Color);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlBackgroundListMediumBrush", base200Color);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightListLowBrush", base200Color);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightListMediumBrush", base200Color);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlBackgroundChromeMediumLowBrush", base100Color);

            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightBaseMediumBrush", base300Color);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightBaseMediumLowBrush", base300Color);

            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightAltBaseHighBrush", primaryContentColor);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightAltBaseMediumHighBrush", baseContentColor);

            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightListAccentLowBrush", primaryColor);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightListAccentMediumBrush", primaryColor);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlHighlightListAccentHighBrush", primaryColor);

            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlBackgroundBaseLowBrush", disabledBackground);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlDisabledBaseLowBrush", disabledBorder);
            UpdateOrCreateBrush(dict, ownedBrushKeys, "SystemControlDisabledBaseMediumLowBrush", disabledForeground);
        }

    }
}

using System;
using System.Collections.Generic;
using Flowery.Theming;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// A dropdown for selecting product themes with visual color previews.
    /// Uses the pre-compiled ProductPalettes instead of DaisyUI themes.
    /// </summary>
    public partial class DaisyProductThemeDropdown : DaisyThemeDropdown
    {
        private static ThemePreviewInfo[]? _cachedProductThemes;

        #region ApplyOnSelection
        public static readonly DependencyProperty ApplyOnSelectionProperty =
            DependencyProperty.Register(
                nameof(ApplyOnSelection),
                typeof(bool),
                typeof(DaisyProductThemeDropdown),
                new PropertyMetadata(true));

        /// <summary>
        /// When true (default), selecting a theme immediately applies it globally.
        /// Set to false to only update SelectedTheme without applying.
        /// </summary>
        public bool ApplyOnSelection
        {
            get => (bool)GetValue(ApplyOnSelectionProperty);
            set => SetValue(ApplyOnSelectionProperty, value);
        }
        #endregion

        /// <summary>
        /// Raised when a product theme is selected.
        /// </summary>
        public event EventHandler<string>? ProductThemeSelected;

        public DaisyProductThemeDropdown()
        {
            MinWidth = 200;
        }

        /// <summary>
        /// Gets the list of product theme infos from ProductPalettes.
        /// </summary>
        protected override ThemePreviewInfo[] GetThemeInfos()
        {
            if (_cachedProductThemes != null) return CloneThemeInfos(_cachedProductThemes);

            List<ThemePreviewInfo> themeData = [];

            foreach (var name in ProductPalettes.GetAllNames())
            {
                var palette = ProductPalettes.Get(name);
                if (palette == null) continue;

                var isDark = FloweryColorHelpers.IsDark(palette.Base100);

                var preview = new ThemePreviewInfo
                {
                    Name = name,
                    IsDark = isDark,
                    Primary = new SolidColorBrush(FloweryColorHelpers.ColorFromHex(palette.Primary)),
                    Secondary = new SolidColorBrush(FloweryColorHelpers.ColorFromHex(palette.Secondary)),
                    Accent = new SolidColorBrush(FloweryColorHelpers.ColorFromHex(palette.Accent)),
                    Neutral = new SolidColorBrush(FloweryColorHelpers.ColorFromHex(palette.Neutral)),
                    Base100 = new SolidColorBrush(FloweryColorHelpers.ColorFromHex(palette.Base100)),
                    BaseContent = new SolidColorBrush(FloweryColorHelpers.ColorFromHex(palette.BaseContent))
                };

                themeData.Add(preview);
            }

            _cachedProductThemes = [.. themeData];
            return CloneThemeInfos(_cachedProductThemes);
        }

        /// <summary>
        /// Gets the default product theme name.
        /// </summary>
        protected override string GetDefaultThemeName()
        {
            return "SaaS";
        }

        /// <summary>
        /// Applies the selected product theme.
        /// </summary>
        protected override void OnThemeSelected(ThemePreviewInfo themeInfo)
        {
            ProductThemeSelected?.Invoke(this, themeInfo.Name);

            if (!ApplyOnSelection)
                return;

            var palette = ProductPalettes.Get(themeInfo.Name);
            if (palette == null) return;

            // Register this product theme with DaisyThemeManager and apply it
            // This ensures consistent theme application across the app
            var isDark = FloweryColorHelpers.IsDark(palette.Base100);
            DaisyThemeManager.RegisterTheme(
                new DaisyThemeInfo(themeInfo.Name, isDark),
                () => DaisyPaletteFactory.Create(palette));

            // Apply via the standard theme manager to ensure all resources are updated correctly
            DaisyThemeManager.ApplyTheme(themeInfo.Name);
        }

        /// <summary>
        /// Product themes don't sync with DaisyThemeManager's current theme.
        /// </summary>
        protected override void SyncWithCurrentTheme()
        {
            // Don't sync - product themes are independent of DaisyUI themes
        }

        /// <summary>
        /// Invalidates the cached product theme list.
        /// </summary>
        public static new void InvalidateThemeCache()
        {
            _cachedProductThemes = null;
        }

        private ThemePreviewInfo[] CloneThemeInfos(ThemePreviewInfo[] source)
        {
            var swatchSize = DaisyResourceLookup.GetDefaultSwatchSize(Size);
            var textMargin = new Thickness(Size == DaisySize.ExtraSmall ? 4 : 8, 0, 0, 0);
            var result = new ThemePreviewInfo[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var item = source[i];
                result[i] = new ThemePreviewInfo
                {
                    Name = item.Name,
                    IsDark = item.IsDark,
                    Base100 = item.Base100,
                    BaseContent = item.BaseContent,
                    Primary = item.Primary,
                    Secondary = item.Secondary,
                    Accent = item.Accent,
                    Neutral = item.Neutral,
                    SwatchSize = swatchSize,
                    TextMargin = textMargin
                };
            }

            return result;
        }
    }
}

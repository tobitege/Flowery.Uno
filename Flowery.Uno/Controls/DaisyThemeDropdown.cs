using System;
using System.Collections.Generic;
using System.Linq;
using Flowery.Helpers;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// Contains preview information for a theme including colors.
    /// </summary>
    [Microsoft.UI.Xaml.Data.Bindable]
    public class ThemePreviewInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName => Name;
        public bool IsDark { get; set; }
        public Brush Base100 { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        public Brush BaseContent { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        public Brush Primary { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        public Brush Secondary { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        public Brush Accent { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        public Brush Neutral { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        public double SwatchSize { get; set; }
        public Thickness TextMargin { get; set; }
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// A dropdown for selecting themes with visual theme previews.
    /// Adapted to use the new DaisyComboBoxBase ContentControl architecture.
    /// </summary>
    public partial class DaisyThemeDropdown : DaisyComboBoxBase
    {
        private static ThemePreviewInfo[]? _cachedThemeData;
        protected bool _isSyncing;
        private ThemePreviewInfo? _pendingThemeInfo;
        private bool _pendingThemeApplyScheduled;
        private bool _ownsItemsSource;
        private DaisySize? _lastAppliedSize;
        private string? _lastAppliedThemeName;

        protected override bool UseListBox
            => false;
        protected override bool UseCustomItemContainerTemplate => true;

        #region SelectedTheme
        public static readonly DependencyProperty SelectedThemeProperty =
            DependencyProperty.Register(
                nameof(SelectedTheme),
                typeof(string),
                typeof(DaisyThemeDropdown),
                new PropertyMetadata("Light"));

        public string SelectedTheme
        {
            get => (string)GetValue(SelectedThemeProperty);
            set => SetValue(SelectedThemeProperty, value);
        }
        #endregion

        public DaisyThemeDropdown() : base(true)
        {
            // Ensure dropdown is wide enough
            MinWidth = 180;
        }

        protected override void OnBeforeLoaded()
        {
            if (ItemsSource == null && Items.Count == 0)
            {
                var themes = GetThemeInfos();
                ItemsSource = themes;
                _ownsItemsSource = true;

                var defaultTheme = GetDefaultThemeName();
                if (!string.IsNullOrEmpty(defaultTheme))
                {
                    SyncToTheme(defaultTheme);
                }
            }

            EnsureFallbackSelection();
        }

        protected override void OnThemeUpdated()
        {
            base.OnThemeUpdated(); // Calls ApplyAll
            SyncWithCurrentTheme();
        }

        protected virtual ThemePreviewInfo[] GetThemeInfos()
        {
            if (_cachedThemeData != null) return CloneThemeInfos(_cachedThemeData);

            List<ThemePreviewInfo> themeData = [];

            foreach (var themeInfo in DaisyThemeManager.AvailableThemes)
            {
                var preview = new ThemePreviewInfo { Name = themeInfo.Name, IsDark = themeInfo.IsDark };
                try
                {
                    var palette = DaisyPaletteFactory.Create(themeInfo.Name);

                    if (palette.TryGetValue("DaisyPrimaryBrush", out var primary) && primary is Brush pb)
                        preview.Primary = pb;
                    if (palette.TryGetValue("DaisySecondaryBrush", out var secondary) && secondary is Brush sb)
                        preview.Secondary = sb;
                    if (palette.TryGetValue("DaisyAccentBrush", out var accent) && accent is Brush ab)
                        preview.Accent = ab;
                    if (palette.TryGetValue("DaisyNeutralBrush", out var neutral) && neutral is Brush nb)
                        preview.Neutral = nb;
                    if (palette.TryGetValue("DaisyBase100Brush", out var base100) && base100 is Brush b100)
                        preview.Base100 = b100;
                    if (palette.TryGetValue("DaisyBaseContentBrush", out var baseContent) && baseContent is Brush bc)
                        preview.BaseContent = bc;
                }
                catch { }
                themeData.Add(preview);
            }

            _cachedThemeData = [.. themeData];
            return CloneThemeInfos(_cachedThemeData);
        }

        protected virtual string GetDefaultThemeName()
        {
            return DaisyThemeManager.CurrentThemeName ?? "Dark";
        }

        protected virtual void OnThemeSelected(ThemePreviewInfo themeInfo)
        {
            DaisyThemeManager.ApplyTheme(themeInfo.Name);
        }

        protected virtual void ApplyItemTemplate()
        {
            ItemTemplate = TryGetThemeItemTemplate();
            DisplayMemberPath = ItemTemplate == null ? nameof(ThemePreviewInfo.DisplayName) : null;
        }

        protected void SyncToTheme(string themeName)
        {
            ThemePreviewInfo? match = null;
            object? matchItem = null;

            if (ItemsSource is IEnumerable<ThemePreviewInfo> themes)
            {
                var themesList = themes.ToList();
                match = themesList.FirstOrDefault(t => string.Equals(t.Name, themeName, StringComparison.OrdinalIgnoreCase));
                matchItem = match;

                if (match == null)
                {
                    FloweryDiagnostics.Log($"[DaisyThemeDropdown] SyncToTheme: no match for '{themeName}' in {themesList.Count} themes. First theme is '{(themesList.Count > 0 ? themesList[0].Name : "<none>")}'");
                }
            }

            if (match != null && SelectedItem != matchItem)
            {
                _isSyncing = true;
                try
                {
                    SelectedItem = matchItem;
                    SelectedTheme = match.Name;
                }
                finally { _isSyncing = false; }
            }
        }

        public static void InvalidateThemeCache()
        {
            _cachedThemeData = null;
        }

        protected override void OnSelectionChanged(object? newItem)
        {
            base.OnSelectionChanged(newItem);

            // Get ThemePreviewInfo
            ThemePreviewInfo? themeInfo = null;
            if (newItem is ThemePreviewInfo directInfo)
                themeInfo = directInfo;

            if (themeInfo != null)
            {
                SelectedTheme = themeInfo.Name;
                if (!_isSyncing)
                {
                    if (string.Equals(themeInfo.Name, DaisyThemeManager.CurrentThemeName, StringComparison.OrdinalIgnoreCase))
                    {
                        _lastAppliedThemeName = themeInfo.Name;
                        return;
                    }

                    if (string.Equals(themeInfo.Name, _lastAppliedThemeName, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    QueueThemeApply(themeInfo);
                }
            }
        }

        protected override void ApplySizing(ResourceDictionary? resources)
        {
            base.ApplySizing(resources);

            // Set MinWidth for swatches room
            var swatchSize = DaisyResourceLookup.GetDefaultSwatchSize(Size);
            MinWidth = (swatchSize * 5) + 4 + 8 + 60 + 26;

            ApplyItemTemplate();
            if (_lastAppliedSize != Size)
            {
                _lastAppliedSize = Size;
                RefreshThemeInfosForSize();
            }
        }

        protected virtual void SyncWithCurrentTheme()
        {
            var currentTheme = DaisyThemeManager.CurrentThemeName;
            if (string.IsNullOrEmpty(currentTheme)) return;
            SyncToTheme(currentTheme!);
            _lastAppliedThemeName = currentTheme;
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

        private void EnsureFallbackSelection()
        {
            if (SelectedItem != null)
            {
                return;
            }

            if (ItemsSource is not IEnumerable<ThemePreviewInfo> themes)
            {
                return;
            }

            var first = themes.FirstOrDefault();
            if (first == null)
            {
                return;
            }

            _isSyncing = true;
            try
            {
                SelectedItem = first;
                SelectedTheme = first.Name;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void RefreshThemeInfosForSize()
        {
            if (!_ownsItemsSource) return;

            var themes = GetThemeInfos();
            _isSyncing = true;
            try
            {
                ItemsSource = themes;
                SyncToTheme(SelectedTheme);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void QueueThemeApply(ThemePreviewInfo themeInfo)
        {
            _pendingThemeInfo = themeInfo;
            if (_pendingThemeApplyScheduled)
            {
                return;
            }

            _pendingThemeApplyScheduled = true;
            if (DispatcherQueue is { } dispatcherQueue)
            {
                dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, ApplyPendingThemeSelection);
            }
            else
            {
                ApplyPendingThemeSelection();
            }
        }

        private void ApplyPendingThemeSelection()
        {
            _pendingThemeApplyScheduled = false;
            var themeInfo = _pendingThemeInfo;
            _pendingThemeInfo = null;

            if (themeInfo == null || _isSyncing)
            {
                return;
            }

            OnThemeSelected(themeInfo);
            _lastAppliedThemeName = themeInfo.Name;
        }

        protected override void ApplyTheme(ResourceDictionary? resources)
        {
            var base200 = GetBrush(resources, "DaisyBase200Brush", new SolidColorBrush(Color.FromArgb(255, 211, 211, 211)));
            var base300 = GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)));
            var baseContent = GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)));

            Background = base200;
            Foreground = baseContent;
            BorderBrush = base300;
            BorderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));

            base.ApplyTheme(resources);
        }

        private DataTemplate? TryGetThemeItemTemplate()
        {
            const string key = "DaisyThemeDropdownItemTemplate";

            if (Resources != null && Resources.TryGetValue(key, out var local) && local is DataTemplate localTemplate)
            {
                return localTemplate;
            }

            if (Application.Current?.Resources is { } appResources
                && DaisyResourceLookup.TryGetResource(appResources, key, out var appValue)
                && appValue is DataTemplate appTemplate)
            {
                return appTemplate;
            }

            var daisyResources = GetDaisyControlsResources();
            if (daisyResources != null
                && DaisyResourceLookup.TryGetResource(daisyResources, key, out var daisyValue)
                && daisyValue is DataTemplate daisyTemplate)
            {
                return daisyTemplate;
            }

            return null;
        }
    }
}

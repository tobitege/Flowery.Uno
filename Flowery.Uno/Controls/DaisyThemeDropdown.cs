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
        public string DisplayName => DisplayNameOverride ?? Name;
        internal string? DisplayNameOverride { get; set; }

        /// <summary>Whether the popup draws a group separator before this theme.</summary>
        public bool HasSeparatorBefore { get; private set; }

        /// <summary>Visibility of the group separator, for item templates.</summary>
        public Visibility SeparatorVisibility => HasSeparatorBefore ? Visibility.Visible : Visibility.Collapsed;

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

        internal ThemePreviewInfo WithSeparator(bool show)
        {
            if (HasSeparatorBefore == show) return this;
            var copy = (ThemePreviewInfo)MemberwiseClone();
            copy.HasSeparatorBefore = show;
            return copy;
        }

        internal ThemePreviewInfo WithDisplayName(string displayName)
        {
            var copy = (ThemePreviewInfo)MemberwiseClone();
            copy.DisplayNameOverride = displayName;
            return copy;
        }

        internal void ApplyPaletteBrushes(ResourceDictionary palette)
        {
            if (palette.TryGetValue("DaisyPrimaryBrush", out var primary) && primary is Brush pb)
                Primary = pb;
            if (palette.TryGetValue("DaisySecondaryBrush", out var secondary) && secondary is Brush sb)
                Secondary = sb;
            if (palette.TryGetValue("DaisyAccentBrush", out var accent) && accent is Brush ab)
                Accent = ab;
            if (palette.TryGetValue("DaisyNeutralBrush", out var neutral) && neutral is Brush nb)
                Neutral = nb;
            if (palette.TryGetValue("DaisyBase100Brush", out var base100) && base100 is Brush b100)
                Base100 = b100;
            if (palette.TryGetValue("DaisyBaseContentBrush", out var baseContent) && baseContent is Brush bc)
                BaseContent = bc;
        }
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
        private bool _isSubscribedToThemeList;
        private readonly Dictionary<string, DaisyThemeManager.ThemeEntry> _localOverrides = new(StringComparer.OrdinalIgnoreCase);
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

        static DaisyThemeDropdown()
        {
            DaisyThemeManager.AvailableThemesChanged += (_, _) => InvalidateThemeCache();
        }

        public DaisyThemeDropdown()
        {
            // Ensure dropdown is wide enough
            MinWidth = 180;
        }

        protected override void OnBeforeLoaded()
        {
            if (!_isSubscribedToThemeList)
            {
                DaisyThemeManager.AvailableThemesChanged += OnAvailableThemesChanged;
                _isSubscribedToThemeList = true;
            }

            if (ItemsSource == null && Items.Count == 0)
            {
                ItemsSource = ResolveThemeInfos();
                _ownsItemsSource = true;

                var defaultTheme = GetDefaultThemeName();
                if (!string.IsNullOrEmpty(defaultTheme))
                {
                    SyncToTheme(defaultTheme);
                }
            }

            EnsureFallbackSelection();
        }

        protected override void OnBeforeUnloaded()
        {
            if (_isSubscribedToThemeList)
            {
                DaisyThemeManager.AvailableThemesChanged -= OnAvailableThemesChanged;
                _isSubscribedToThemeList = false;
            }
        }

        private void OnAvailableThemesChanged(object? sender, EventArgs e)
        {
            if (DispatcherQueue is { HasThreadAccess: false } dispatcherQueue)
            {
                dispatcherQueue.TryEnqueue(() => OnAvailableThemesChanged(sender, e));
                return;
            }

            RefreshOwnedThemeInfos();
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
                if (DaisyThemeManager.TryCreatePalette(themeInfo.Name, out var palette) && palette != null)
                    preview.ApplyPaletteBrushes(palette);
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
            EnsureThemeRegistered(themeInfo.Name);
            DaisyThemeManager.ApplyTheme(themeInfo.Name);
        }

        /// <summary>
        /// Registers a product theme that was added to the list but is not registered yet.
        /// </summary>
        protected static void EnsureThemeRegistered(string themeName)
        {
            if (DaisyThemeManager.GetThemeInfo(themeName) == null &&
                ProductPaletteFactory.FindByName(themeName) is { } product)
            {
                DaisyThemeManager.EnsureProductThemeRegistered(product);
            }
        }

        /// <summary>
        /// Adds a standard, registered, or product theme to this dropdown without replacing
        /// the default list. Repeated original names update the entry without duplicating it.
        /// Call on the UI thread. This does not select, apply, or register a theme.
        /// Only affects dropdowns that fill their own list (no ItemsSource set by the app).
        /// </summary>
        /// <param name="originalName">Original theme name, matched case-insensitively.</param>
        /// <param name="displayName">Text shown only in this dropdown.</param>
        /// <param name="preferred">Whether to place the entry in the preferred group at the top.</param>
        /// <exception cref="ArgumentException">A name is empty or the original theme is unknown.</exception>
        public void AddTheme(string originalName, string displayName, bool preferred)
        {
            var entry = DaisyThemeManager.CreateThemeEntry(originalName, displayName, preferred);
            if (_localOverrides.TryGetValue(entry.Name, out var existing) && existing == entry) return;
            _localOverrides[entry.Name] = entry;
            RefreshOwnedThemeInfos();
        }

        /// <summary>
        /// Removes a local entry override and restores the manager's list entry, if available.
        /// Returns true when an override was removed.
        /// </summary>
        public bool RemoveThemeOverride(string originalName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(originalName);
            if (!_localOverrides.Remove(originalName)) return false;
            RefreshOwnedThemeInfos();
            return true;
        }

        private List<ThemePreviewInfo> ResolveThemeInfos()
        {
            var themes = ThemeListResolver.Resolve(GetThemeInfos(), _localOverrides);
            var swatchSize = DaisyResourceLookup.GetDefaultSwatchSize(ResolvedSize);
            var textMargin = new Thickness(ResolvedSize == DaisySize.ExtraSmall ? 4 : 8, 0, 0, 0);
            foreach (var theme in themes)
            {
                theme.SwatchSize = swatchSize;
                theme.TextMargin = textMargin;
            }

            return themes;
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
            var swatchSize = DaisyResourceLookup.GetDefaultSwatchSize(ResolvedSize);
            MinWidth = (swatchSize * 5) + 4 + 8 + 60 + 26;

            ApplyItemTemplate();
            if (_lastAppliedSize != ResolvedSize)
            {
                _lastAppliedSize = ResolvedSize;
                RefreshOwnedThemeInfos();
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
            var swatchSize = DaisyResourceLookup.GetDefaultSwatchSize(ResolvedSize);
            var textMargin = new Thickness(ResolvedSize == DaisySize.ExtraSmall ? 4 : 8, 0, 0, 0);
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

        private void RefreshOwnedThemeInfos()
        {
            if (!_ownsItemsSource) return;

            var themes = ResolveThemeInfos();
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

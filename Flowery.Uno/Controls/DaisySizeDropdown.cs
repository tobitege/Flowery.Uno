using System.Collections;
using Flowery.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// Contains display information for a DaisySize option.
    /// </summary>
    [Microsoft.UI.Xaml.Data.Bindable]
    public class SizePreviewInfo
    {
        public DaisySize Size { get; set; }
        public string Name { get; set; } = "";
        public string? DisplayNameOverride { get; set; }
        public string Abbreviation { get; set; } = "";
        public bool IsVisible { get; set; } = true;

        public string DisplayName =>
            !string.IsNullOrEmpty(DisplayNameOverride)
                ? DisplayNameOverride!
                : (FloweryLocalization.GetStringInternal($"Size_{Name}") is string s && s != $"Size_{Name}" ? s : Name);

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// A dropdown control for selecting a global DaisySize.
    /// When the user selects a size, it is applied globally via <see cref="FlowerySizeManager"/>.
    /// </summary>
    public partial class DaisySizeDropdown : DaisyComboBoxBase
    {
        public static readonly DependencyProperty SelectedSizeProperty =
            DependencyProperty.Register(
                nameof(SelectedSize),
                typeof(DaisySize),
                typeof(DaisySizeDropdown),
                new PropertyMetadata(DaisySize.Medium));

        public DaisySize SelectedSize
        {
            get => (DaisySize)GetValue(SelectedSizeProperty);
            set => SetValue(SelectedSizeProperty, value);
        }

        public static readonly DependencyProperty ShowAbbreviationsProperty =
            DependencyProperty.Register(
                nameof(ShowAbbreviations),
                typeof(bool),
                typeof(DaisySizeDropdown),
                new PropertyMetadata(false, OnDisplayModeChanged));

        public bool ShowAbbreviations
        {
            get => (bool)GetValue(ShowAbbreviationsProperty);
            set => SetValue(ShowAbbreviationsProperty, value);
        }

        public static readonly DependencyProperty SizeOptionsProperty =
            DependencyProperty.Register(
                nameof(SizeOptions),
                typeof(IEnumerable),
                typeof(DaisySizeDropdown),
                new PropertyMetadata(null, OnSizeOptionsChanged));

        public IEnumerable? SizeOptions
        {
            get => (IEnumerable?)GetValue(SizeOptionsProperty);
            set => SetValue(SizeOptionsProperty, value);
        }

        private SizePreviewInfo[]? _instanceSizes;
        private bool _isSyncing;

        public DaisySizeDropdown() : base(subscribeSizeChanges: false)
        {
            SelectionChanged += OnSelectionChanged;
            UpdateDisplayMemberPath();
            RefreshVisibleSizes();
        }

        private static void OnDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySizeDropdown dropdown)
            {
                dropdown.UpdateDisplayMemberPath();
                dropdown.RefreshVisibleSizes();
            }
        }

        private static void OnSizeOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySizeDropdown dropdown)
            {
                dropdown.RefreshVisibleSizes();
            }
        }

        protected override void OnBeforeLoaded()
        {
            FlowerySizeManager.SizeChanged += OnSizeChanged;
            FloweryLocalization.CultureChanged += OnCultureChanged;
        }

        protected override void OnAfterLoaded()
        {
            SyncWithCurrentSize();
        }

        protected override void OnBeforeUnloaded()
        {
            FlowerySizeManager.SizeChanged -= OnSizeChanged;
            FloweryLocalization.CultureChanged -= OnCultureChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedItem is SizePreviewInfo sizeInfo)
            {
                SelectedSize = sizeInfo.Size;
                if (!_isSyncing)
                {
                    FlowerySizeManager.ApplySize(sizeInfo.Size);
                }
            }
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            _instanceSizes = null;
            RefreshVisibleSizes();
        }

        private void OnSizeChanged(object? sender, DaisySize size)
        {
            // Use ShouldIgnoreGlobalSize to check this control AND ancestors
            if (FlowerySizeManager.ShouldIgnoreGlobalSize(this))
                return;

            Size = size;
            ApplyAll();
            SyncWithCurrentSize();
        }

        protected override void ApplySizing(ResourceDictionary? resources)
        {
            base.ApplySizing(resources);

            // Set MinWidth to ensure there's room for size names + dropdown glyph (26px)
            MinWidth = 90 + 26;  // "Extra Small" text + glyph margin
        }

        protected override void ApplyTheme(ResourceDictionary? resources)
        {
            // Match DaisySelect's theming pattern (use Base100 for background, same as other dropdowns)
            var base100 = GetBrush(resources, "DaisyBase100Brush", new SolidColorBrush(Microsoft.UI.Colors.White));
            var base300 = GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)));
            var baseContent = GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)));

            Background = base100;
            Foreground = baseContent;
            BorderBrush = base300;
            BorderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));

            base.ApplyTheme(resources);
        }

        private void UpdateDisplayMemberPath()
        {
            DisplayMemberPath = ShowAbbreviations ? nameof(SizePreviewInfo.Abbreviation) : nameof(SizePreviewInfo.DisplayName);
        }

        private void RefreshVisibleSizes()
        {
            var sizes = GetVisibleSizeInfos();
            ItemsSource = sizes;
            SyncToSize(SelectedSize, sizes);
        }

        private void SyncWithCurrentSize()
        {
            SyncToSize(FlowerySizeManager.CurrentSize);
        }

        private void SyncToSize(DaisySize size, SizePreviewInfo[]? sizes = null)
        {
            sizes ??= GetVisibleSizeInfos();
            SizePreviewInfo? match = null;
            for (var i = 0; i < sizes.Length; i++)
            {
                if (sizes[i].Size == size)
                {
                    match = sizes[i];
                    break;
                }
            }

            if (match != null && !ReferenceEquals(SelectedItem, match))
            {
                _isSyncing = true;
                try
                {
                    SelectedItem = match;
                    SelectedSize = match.Size;
                }
                finally
                {
                    _isSyncing = false;
                }
            }
        }

        private bool TryGetSizeOptions(out SizePreviewInfo[] options)
        {
            options = [];

            if (SizeOptions == null)
                return false;

            if (SizeOptions is SizePreviewInfo[] array)
            {
                if (array.Length == 0)
                    return false;

                options = array;
                return true;
            }

            if (SizeOptions is ICollection collection)
            {
                if (collection.Count == 0)
                    return false;

                var buffer = new SizePreviewInfo[collection.Count];
                var index = 0;
                foreach (var item in collection)
                {
                    if (item is SizePreviewInfo info)
                    {
                        buffer[index++] = info;
                    }
                }

                if (index == 0)
                    return false;

                if (index != buffer.Length)
                    Array.Resize(ref buffer, index);

                options = buffer;
                return true;
            }

            var temp = new ArrayList();
            foreach (var item in SizeOptions)
            {
                if (item is SizePreviewInfo info)
                {
                    temp.Add(info);
                }
            }

            if (temp.Count == 0)
                return false;

            var result = new SizePreviewInfo[temp.Count];
            for (var i = 0; i < temp.Count; i++)
            {
                result[i] = (SizePreviewInfo)temp[i]!;
            }

            options = result;
            return true;
        }

        private static SizePreviewInfo[] FilterVisibleSizeInfos(SizePreviewInfo[] options)
        {
            var visibleCount = 0;
            foreach (var option in options)
            {
                if (option.IsVisible)
                    visibleCount++;
            }

            if (visibleCount == options.Length)
                return options;

            var visible = new SizePreviewInfo[visibleCount];
            var index = 0;
            foreach (var option in options)
            {
                if (option.IsVisible)
                    visible[index++] = option;
            }

            return visible;
        }

        private SizePreviewInfo[] GetVisibleSizeInfos()
        {
            if (TryGetSizeOptions(out var options))
            {
                var visible = FilterVisibleSizeInfos(options);
                if (visible.Length > 0)
                    return visible;
            }

            if (_instanceSizes != null)
                return _instanceSizes;

            _instanceSizes =
            [
                new() { Size = DaisySize.ExtraSmall, Name = "ExtraSmall", Abbreviation = "XS" },
                new() { Size = DaisySize.Small, Name = "Small", Abbreviation = "S" },
                new() { Size = DaisySize.Medium, Name = "Medium", Abbreviation = "M" },
                new() { Size = DaisySize.Large, Name = "Large", Abbreviation = "L" },
                new() { Size = DaisySize.ExtraLarge, Name = "ExtraLarge", Abbreviation = "XL" }
            ];

            return _instanceSizes;
        }

    }
}

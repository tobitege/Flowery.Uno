using Flowery.Localization;
using Flowery.Services;
using Microsoft.UI.Xaml.Automation;
using Windows.System;

namespace Flowery.Controls
{
    public enum DaisyTagPickerMode
    {
        Selection,
        Library
    }

    /// <summary>
    /// A selectable chip list for choosing multiple tags.
    /// </summary>
    public partial class DaisyTagPicker : DaisyBaseContentControl
    {
        private Border? _containerBorder;
        private Border? _dividerBorder;
        private StackPanel? _rootPanel;
        private TextBlock? _titleText;
        private WrapPanel? _selectedPanel;
        private WrapPanel? _availablePanel;
        private readonly List<string> _internalSelected = [];
        private readonly List<TagChipInfo> _chipInfos = [];
        private string? _pendingFocusedTag;
        private int _focusedIndex = -1;
        private System.Collections.Specialized.INotifyCollectionChanged? _tagsNotifier;
        private System.Collections.Specialized.INotifyCollectionChanged? _selectedTagsNotifier;
        private bool _isNormalizingLibrary;
        private bool _isTitleExplicit;
        private bool _isLibraryTitleExplicit;

        private sealed class TagChipInfo(string tag, bool isSelected, Border border)
        {
            public string Tag { get; } = tag;
            public bool IsSelected { get; } = isSelected;
            public Border Border { get; } = border;
        }

        public DaisyTagPicker()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            IsTabStop = true;
            UseSystemFocusVisuals = true;
            Unloaded += OnUnloaded;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            EnsureCollectionTracking();
            ApplyAll();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _containerBorder ?? base.GetNeumorphicHostElement();
        }

        #region Tags
        public static readonly DependencyProperty TagsProperty =
            DependencyProperty.Register(
                nameof(Tags),
                typeof(IList<string>),
                typeof(DaisyTagPicker),
                new PropertyMetadata(null, OnTagsChanged));

        /// <summary>
        /// Gets or sets the list of available tags.
        /// </summary>
        public IList<string>? Tags
        {
            get => (IList<string>?)GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }
        #endregion

        #region SelectedTags
        public static readonly DependencyProperty SelectedTagsProperty =
            DependencyProperty.Register(
                nameof(SelectedTags),
                typeof(IList<string>),
                typeof(DaisyTagPicker),
                new PropertyMetadata(null, OnSelectedTagsChanged));

        /// <summary>
        /// Gets or sets the selected tags. When null, selection is managed internally.
        /// </summary>
        public IList<string>? SelectedTags
        {
            get => (IList<string>?)GetValue(SelectedTagsProperty);
            set => SetValue(SelectedTagsProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyTagPicker),
                new PropertyMetadata(DaisySize.Small, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of the tag chips.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region Mode
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(DaisyTagPickerMode),
                typeof(DaisyTagPicker),
                new PropertyMetadata(DaisyTagPickerMode.Selection, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets how tags are managed and displayed.
        /// </summary>
        public DaisyTagPickerMode Mode
        {
            get => (DaisyTagPickerMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
        #endregion

        #region Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(DaisyTagPicker),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("TagPicker_SelectedTags", "Selected Tags"), OnTitleChanged));

        /// <summary>
        /// Gets or sets the title for the selected tags section.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region LibraryTitle
        public static readonly DependencyProperty LibraryTitleProperty =
            DependencyProperty.Register(
                nameof(LibraryTitle),
                typeof(string),
                typeof(DaisyTagPicker),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("TagPicker_AvailableTags", "Available Tags"), OnLibraryTitleChanged));

        /// <summary>
        /// Gets or sets the title for the library tags section.
        /// </summary>
        public string LibraryTitle
        {
            get => (string)GetValue(LibraryTitleProperty);
            set => SetValue(LibraryTitleProperty, value);
        }
        #endregion

        #region Accessibility
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyTagPicker),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTagPicker picker)
            {
                picker.UpdateAutomationProperties();
            }
        }
        #endregion

        /// <summary>
        /// Raised when the selection changes.
        /// </summary>
        public event EventHandler<IReadOnlyList<string>>? SelectionChanged;

        private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTagPicker picker)
            {
                picker.UpdateTagsBinding(e.NewValue as IList<string>);
                picker.ApplyAll();
            }
        }

        private static void OnSelectedTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTagPicker picker)
            {
                picker.UpdateSelectedTagsBinding(e.NewValue as IList<string>);
                picker.ApplyAll();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTagPicker picker)
                picker.ApplyAll();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTagPicker picker)
            {
                picker._isTitleExplicit = true;
                picker.ApplyAll();
            }
        }

        private static void OnLibraryTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTagPicker picker)
            {
                picker._isLibraryTitleExplicit = true;
                picker.ApplyAll();
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateFocusVisuals();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            UpdateFocusVisuals();
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || _chipInfos.Count == 0)
                return;

            var handled = true;
            switch (e.Key)
            {
                case VirtualKey.Left:
                case VirtualKey.Up:
                    MoveFocus(-1);
                    break;
                case VirtualKey.Right:
                case VirtualKey.Down:
                    MoveFocus(1);
                    break;
                case VirtualKey.Home:
                    SetFocusedIndex(0);
                    break;
                case VirtualKey.End:
                    SetFocusedIndex(_chipInfos.Count - 1);
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    ToggleFocusedTag();
                    break;
                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        private void BuildVisualTree()
        {
            if (_rootPanel != null)
                return;

            _rootPanel = new StackPanel
            {
                Spacing = 10
            };

            _titleText = new TextBlock
            {
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Opacity = 0.8
            };
            _rootPanel.Children.Add(_titleText);

            _selectedPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };
            _rootPanel.Children.Add(_selectedPanel);

            _dividerBorder = new Border
            {
                Height = 1,
                Background = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                Opacity = 0.35,
                Margin = new Thickness(0, 4, 0, 4)
            };
            _rootPanel.Children.Add(_dividerBorder);

            _availablePanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };
            _rootPanel.Children.Add(_availablePanel);

            var scrollViewer = new ScrollViewer
            {
                Content = _rootPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollMode = ScrollMode.Enabled,
                HorizontalScrollMode = ScrollMode.Disabled
            };

            _containerBorder = new Border
            {
                Child = scrollViewer,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12)
            };

            Content = _containerBorder;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachTagsNotifier();
            DetachSelectedTagsNotifier();
        }

        private void EnsureCollectionTracking()
        {
            if (_tagsNotifier == null && Tags is System.Collections.Specialized.INotifyCollectionChanged tagNotifier)
            {
                _tagsNotifier = tagNotifier;
                _tagsNotifier.CollectionChanged += OnTagsCollectionChanged;
            }

            if (_selectedTagsNotifier == null && SelectedTags is System.Collections.Specialized.INotifyCollectionChanged selectedNotifier)
            {
                _selectedTagsNotifier = selectedNotifier;
                _selectedTagsNotifier.CollectionChanged += OnSelectedTagsCollectionChanged;
            }
        }

        private void UpdateTagsBinding(IList<string>? tags)
        {
            DetachTagsNotifier();
            if (tags is System.Collections.Specialized.INotifyCollectionChanged notifier)
            {
                _tagsNotifier = notifier;
                _tagsNotifier.CollectionChanged += OnTagsCollectionChanged;
            }
        }

        private void UpdateSelectedTagsBinding(IList<string>? selectedTags)
        {
            DetachSelectedTagsNotifier();
            if (selectedTags is System.Collections.Specialized.INotifyCollectionChanged notifier)
            {
                _selectedTagsNotifier = notifier;
                _selectedTagsNotifier.CollectionChanged += OnSelectedTagsCollectionChanged;
            }
        }

        private void DetachTagsNotifier()
        {
            if (_tagsNotifier != null)
            {
                _tagsNotifier.CollectionChanged -= OnTagsCollectionChanged;
                _tagsNotifier = null;
            }
        }

        private void DetachSelectedTagsNotifier()
        {
            if (_selectedTagsNotifier != null)
            {
                _selectedTagsNotifier.CollectionChanged -= OnSelectedTagsCollectionChanged;
                _selectedTagsNotifier = null;
            }
        }

        private void OnTagsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateListsAndRebuild();
        }

        private void OnSelectedTagsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateListsAndRebuild();
        }

        private void ApplyAll()
        {
            if (_titleText == null || _selectedPanel == null || _availablePanel == null || _containerBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);
            _titleText.Text = GetEffectiveTitle();
            _titleText.Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");

            // Style the container
            _containerBorder.Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _containerBorder.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            _containerBorder.BorderThickness = new Thickness(1);

            var showAvailable = Mode == DaisyTagPickerMode.Selection;
            _availablePanel.Visibility = showAvailable ? Visibility.Visible : Visibility.Collapsed;
            if (_dividerBorder != null)
            {
                _dividerBorder.Visibility = showAvailable ? Visibility.Visible : Visibility.Collapsed;
            }

            UpdateListsAndRebuild();
        }

        private string GetEffectiveTitle()
        {
            if (Mode == DaisyTagPickerMode.Library)
            {
                if (_isLibraryTitleExplicit)
                    return LibraryTitle;

                if (_isTitleExplicit)
                    return Title;

                return FloweryLocalization.GetStringInternal("TagPicker_AvailableTags", "Available Tags");
            }

            if (_isTitleExplicit)
                return Title;

            return FloweryLocalization.GetStringInternal("TagPicker_SelectedTags", "Selected Tags");
        }

        private IList<string> GetSelectedList()
        {
            if (Mode == DaisyTagPickerMode.Library)
            {
                return GetLibraryList();
            }

            return SelectedTags ?? _internalSelected;
        }

        private IList<string> GetLibraryList()
        {
            if (Tags != null && CanMutate(Tags))
                return Tags;

            if (SelectedTags != null && CanMutate(SelectedTags))
                return SelectedTags;

            return Tags ?? SelectedTags ?? _internalSelected;
        }

        private IList<string>? GetLibrarySecondaryList(IList<string> primary)
        {
            if (Tags != null && !ReferenceEquals(primary, Tags) && CanMutate(Tags))
                return Tags;

            if (SelectedTags != null && !ReferenceEquals(primary, SelectedTags) && CanMutate(SelectedTags))
                return SelectedTags;

            return null;
        }

        private void UpdateListsAndRebuild()
        {
            if (_selectedPanel == null || _availablePanel == null)
                return;

            _selectedPanel.Children.Clear();
            _availablePanel.Children.Clear();
            var previousFocusedTag = _pendingFocusedTag ?? GetFocusedTag();
            _pendingFocusedTag = null;
            _chipInfos.Clear();

            if (Mode == DaisyTagPickerMode.Library)
            {
                var libraryTags = GetLibraryList();
                NormalizeLibraryTags(libraryTags);
                foreach (var tag in SortTags(libraryTags))
                {
                    AddChip(tag, isSelected: true, _selectedPanel);
                }
            }
            else
            {
                IList<string> tags = Tags ?? [];
                var selected = GetSelectedList();

                foreach (var tag in SortTags(tags.Where(t => selected.Contains(t))))
                {
                    AddChip(tag, isSelected: true, _selectedPanel);
                }

                foreach (var tag in SortTags(tags.Where(t => !selected.Contains(t))))
                {
                    AddChip(tag, isSelected: false, _availablePanel);
                }
            }

            if (!string.IsNullOrWhiteSpace(previousFocusedTag))
            {
                SetFocusedTag(previousFocusedTag);
            }
            else if (_focusedIndex < 0 && _chipInfos.Count > 0)
            {
                _focusedIndex = 0;
            }
            else if (_focusedIndex >= _chipInfos.Count)
            {
                _focusedIndex = _chipInfos.Count - 1;
            }

            UpdateFocusVisuals();
            UpdateAutomationProperties();
        }

        private static IEnumerable<string> SortTags(IEnumerable<string> tags)
        {
            return tags.OrderBy(tag => tag, StringComparer.CurrentCultureIgnoreCase);
        }

        private void AddChip(string tag, bool isSelected, Panel panel)
        {
            var chip = CreateChip(tag, isSelected);
            panel.Children.Add(chip);
            _chipInfos.Add(new TagChipInfo(tag, isSelected, chip));
        }

        private Border CreateChip(string tag, bool isSelected)
        {
            var effectiveSize = FlowerySizeManager.ShouldIgnoreGlobalSize(this) ? Size : FlowerySizeManager.CurrentSize;
            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(effectiveSize);

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);
            var lookupResources = resources ?? [];

            // Get corner radius from standard tokens
            CornerRadius cornerRadiusValue = DaisyResourceLookup.GetCornerRadius(
                lookupResources, $"DaisySize{sizeKey}CornerRadius",
                DaisyResourceLookup.GetDefaultCornerRadius(effectiveSize));
            double cornerRadius = cornerRadiusValue.TopLeft;

            // Get font size from tokens
            double fontSize = DaisyResourceLookup.GetDouble(
                lookupResources, $"DaisySize{sizeKey}FontSize",
                DaisyResourceLookup.GetDefaultFontSize(effectiveSize));

            // Get padding from tokens
            Thickness paddingValue = DaisyResourceLookup.GetThickness(
                lookupResources, $"DaisyTag{sizeKey}Padding",
                DaisyResourceLookup.GetDefaultTagPadding(effectiveSize));

            // Get close icon size from tokens
            double iconSize = DaisyResourceLookup.GetDouble(
                lookupResources, $"DaisyTagClose{sizeKey}Size",
                DaisyResourceLookup.GetDefaultIconSize(effectiveSize));

            if (isSelected)
            {
                // Selected tags: filled background with X close button
                var chipBorder = new Border
                {
                    Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"),
                    CornerRadius = new CornerRadius(cornerRadius),
                    Padding = new Thickness(paddingValue.Left, paddingValue.Top, paddingValue.Left / 2, paddingValue.Top),
                    Margin = new Thickness(4)
                };

                var chipStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4
                };

                var tagText = new TextBlock
                {
                    Text = tag,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = fontSize
                };
                chipStack.Children.Add(tagText);

                // Close button (X icon)
                var closeButton = new Button
                {
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(2),
                    MinWidth = 0,
                    MinHeight = 0,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var closeIcon = FloweryPathHelpers.CreateClose(
                    size: iconSize,
                    strokeThickness: 1.5,
                    stroke: DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush"));
                closeButton.Content = closeIcon;
                closeButton.Click += (_, _) => OnChipInvoked(tag, FocusState.Pointer);
                chipStack.Children.Add(closeButton);

                chipBorder.Child = chipStack;

                // Make the whole chip clickable too
                chipBorder.PointerPressed += (_, _) => OnChipInvoked(tag, FocusState.Pointer);

                return chipBorder;
            }
            else
            {
                // Unselected tags: bordered with rounded corners (auto-width)
                var chipBorder = new Border
                {
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(cornerRadius),
                    Padding = paddingValue,
                    Margin = new Thickness(4)
                };

                var tagText = new TextBlock
                {
                    Text = tag,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = fontSize
                };

                chipBorder.Child = tagText;
                chipBorder.PointerPressed += (_, _) => OnChipInvoked(tag, FocusState.Pointer);

                return chipBorder;
            }
        }

        public void ToggleTag(string tag)
        {
            if (Mode == DaisyTagPickerMode.Library)
            {
                ToggleTagInLibraryMode(tag);
                return;
            }

            var selected = GetSelectedList();

            var isSelected = selected.Contains(tag);

            if (SelectedTags != null)
            {
                if (!SelectedTags.IsReadOnly)
                {
                    if (isSelected)
                        SelectedTags.Remove(tag);
                    else
                        SelectedTags.Add(tag);
                }

                ApplyAll();
                SelectionChanged?.Invoke(this, new ReadOnlyStringList(SelectedTags));
                return;
            }

            if (isSelected)
                _internalSelected.Remove(tag);
            else
                _internalSelected.Add(tag);

            ApplyAll();
            SelectionChanged?.Invoke(this, new ReadOnlyStringList(_internalSelected));
        }

        private void ToggleTagInLibraryMode(string tag)
        {
            var primary = GetLibraryList();
            var secondary = GetLibrarySecondaryList(primary);
            var isPresent = primary.Contains(tag);

            if (isPresent)
            {
                TryRemoveTag(primary, tag);
                if (secondary != null)
                    TryRemoveTag(secondary, tag);
            }
            else
            {
                TryAddTag(primary, tag);
                if (secondary != null)
                    TryAddTag(secondary, tag);
            }

            ApplyAll();
            var listForEvent = SelectedTags ?? primary;
            SelectionChanged?.Invoke(this, new ReadOnlyStringList(listForEvent));
        }

        private static bool CanMutate(IList<string> list)
        {
            return list is not System.Collections.Generic.ICollection<string> collection || !collection.IsReadOnly;
        }

        private void NormalizeLibraryTags(IList<string> list)
        {
            if (_isNormalizingLibrary || !CanMutate(list) || list.Count < 2)
                return;

            _isNormalizingLibrary = true;
            try
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var duplicates = new List<string>();

                foreach (var tag in list)
                {
                    if (!seen.Add(tag))
                        duplicates.Add(tag);
                }

                if (duplicates.Count == 0)
                    return;

                foreach (var tag in duplicates)
                {
                    list.Remove(tag);
                }
            }
            finally
            {
                _isNormalizingLibrary = false;
            }
        }

        private static void TryAddTag(IList<string> list, string tag)
        {
            if (!CanMutate(list) || list.Contains(tag))
                return;

            list.Add(tag);
        }

        private static void TryRemoveTag(IList<string> list, string tag)
        {
            if (!CanMutate(list))
                return;

            list.Remove(tag);
        }

        private void MoveFocus(int delta)
        {
            if (_chipInfos.Count == 0)
                return;

            var current = _focusedIndex < 0 ? 0 : _focusedIndex;
            var next = Math.Clamp(current + delta, 0, _chipInfos.Count - 1);
            SetFocusedIndex(next);
        }

        private void SetFocusedIndex(int index)
        {
            if (index < 0 || index >= _chipInfos.Count)
                return;

            _focusedIndex = index;
            UpdateFocusVisuals();
            UpdateAutomationProperties();
        }

        private void SetFocusedTag(string tag)
        {
            var index = _chipInfos.FindIndex(info => info.Tag == tag);
            if (index >= 0)
            {
                _focusedIndex = index;
            }
            else if (_chipInfos.Count > 0)
            {
                _focusedIndex = 0;
            }
            else
            {
                _focusedIndex = -1;
            }
        }

        private string? GetFocusedTag()
        {
            if (_focusedIndex < 0 || _focusedIndex >= _chipInfos.Count)
                return null;

            return _chipInfos[_focusedIndex].Tag;
        }

        private void ToggleFocusedTag()
        {
            var tag = GetFocusedTag();
            if (string.IsNullOrWhiteSpace(tag))
                return;

            _pendingFocusedTag = tag;
            Focus(FocusState.Keyboard);
            ToggleTag(tag);
        }

        private void OnChipInvoked(string tag, FocusState focusState)
        {
            _pendingFocusedTag = tag;
            Focus(focusState);
            ToggleTag(tag);
        }

        private void UpdateFocusVisuals()
        {
            var showFocus = FocusState != FocusState.Unfocused;
            var focusBrushSelected = DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush");
            var focusBrushUnselected = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
            var defaultBorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            for (int i = 0; i < _chipInfos.Count; i++)
            {
                var info = _chipInfos[i];
                var border = info.Border;

                if (showFocus && i == _focusedIndex)
                {
                    border.BorderThickness = new Thickness(2);
                    border.BorderBrush = info.IsSelected ? focusBrushSelected : focusBrushUnselected;
                    continue;
                }

                if (info.IsSelected)
                {
                    border.BorderThickness = new Thickness(0);
                    border.BorderBrush = null;
                }
                else
                {
                    border.BorderThickness = new Thickness(1);
                    border.BorderBrush = defaultBorderBrush;
                }
            }
        }

        private void UpdateAutomationProperties()
        {
            var baseName = !string.IsNullOrWhiteSpace(AccessibleText) ? AccessibleText : GetEffectiveTitle();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "Tag picker";
            }

            var focusedTag = GetFocusedTag();
            var name = string.IsNullOrWhiteSpace(focusedTag) ? baseName : $"{baseName}: {focusedTag}";

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }
    }
}

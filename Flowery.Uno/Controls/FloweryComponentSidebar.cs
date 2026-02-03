// CsWinRT1028: Generic collections implementing WinRT interfaces - inherent to Uno Platform, not fixable via code
#pragma warning disable CsWinRT1028

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Controls
{
    public partial class SidebarCategory : INotifyPropertyChanged
    {
        private bool _isExpanded = true;
        private string _name = string.Empty;
        private string _displayName = string.Empty;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }

        public string DisplayName
        {
            get => string.IsNullOrWhiteSpace(_displayName) ? _name : _displayName;
            set
            {
                if (!string.Equals(_displayName, value, StringComparison.Ordinal))
                {
                    _displayName = value ?? string.Empty;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }

        public string IconKey { get; set; } = string.Empty;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                }
            }
        }

        public ObservableCollection<SidebarItem> Items { get; set; } = [];

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class SidebarItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _displayName = string.Empty;
        private bool _isFavorite;
        private bool _showFavoriteIcon;

        public string Id { get; set; } = string.Empty;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }

        public string DisplayName
        {
            get => string.IsNullOrWhiteSpace(_displayName) ? _name : _displayName;
            set
            {
                if (!string.Equals(_displayName, value, StringComparison.Ordinal))
                {
                    _displayName = value ?? string.Empty;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }

        public string TabHeader { get; set; } = string.Empty;
        public string? Badge { get; set; }

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite)));
                }
            }
        }

        public bool ShowFavoriteIcon
        {
            get => _showFavoriteIcon;
            set
            {
                if (_showFavoriteIcon != value)
                {
                    _showFavoriteIcon = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowFavoriteIcon)));
                }
            }
        }

        public event EventHandler? FavoriteToggleRequested;

        internal void RaiseFavoriteToggleRequested()
        {
            FavoriteToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class SidebarItemSelectedEventArgs(SidebarItem item, SidebarCategory category) : EventArgs
    {
        public SidebarItem Item { get; } = item;
        public SidebarCategory Category { get; } = category;
    }

    public class SidebarFavoriteToggledEventArgs(SidebarItem item) : EventArgs
    {
        public SidebarItem Item { get; } = item;
    }

    internal sealed class SidebarItemView(Border container, DaisyIconText textItem, SidebarItem item, SidebarCategory category)
    {
        public Border Container { get; } = container;
        public DaisyIconText TextItem { get; } = textItem;
        public SidebarItem Item { get; } = item;
        public SidebarCategory Category { get; } = category;
        public bool IsHovered { get; set; }
        public Button? FavoriteButton { get; set; }
        public DaisyIconText? FavoriteFilled { get; set; }
        public DaisyIconText? FavoriteOutline { get; set; }
    }

    public partial class FloweryComponentSidebar : UserControl
    {
        private const string StateKey = "sidebar";
        private const double SidebarFontScale = 1.0d;

        private ObservableCollection<SidebarCategory> _allCategories = [];
        private readonly Dictionary<SidebarItem, SidebarItemView> _itemViews = [];
        private readonly Dictionary<SidebarItem, List<PropertyChangedEventHandler>> _itemHandlers = [];
        private readonly Dictionary<SidebarCategory, List<PropertyChangedEventHandler>> _categoryHandlers = [];

        private Border? _rootBorder;
        private DaisyInput? _searchInput;
        private StackPanel? _categoriesPanel;
        private ScrollViewer? _categoriesScrollViewer;

        private SidebarItemView? _selectedItemView;
        private bool _updatingSearch;
        private bool _suppressSelectionEvents;
        private double? _pendingScrollRestoreOffset;
        private bool _pendingScrollRestore;
        private bool _rebuildQueued;
        private bool _pendingRebuild;
        private bool _pendingRebuildPreserveScroll;
        private bool _pendingRebuildSuppressSelection;
        private readonly DaisyControlLifecycle _lifecycle;

        // Incremental rebuild state
        private long _rebuildGeneration;
        private readonly Queue<SidebarCategory> _buildQueue = new();
        private bool _isProcessingQueue;
        private SidebarMetrics _currentMetrics;
        private Brush? _currentBaseContentBrush;
        private Brush? _currentBase300Brush;

        public static readonly DependencyProperty SearchPlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(SearchPlaceholderText),
                typeof(string),
                typeof(FloweryComponentSidebar),
                new PropertyMetadata("Search components...", OnSearchPlaceholderTextChanged));

        public string SearchPlaceholderText
        {
            get => (string)GetValue(SearchPlaceholderTextProperty);
            set => SetValue(SearchPlaceholderTextProperty, value ?? string.Empty);
        }

        public static readonly DependencyProperty CategoriesProperty =
            DependencyProperty.Register(
                nameof(Categories),
                typeof(ObservableCollection<SidebarCategory>),
                typeof(FloweryComponentSidebar),
                new PropertyMetadata(null, OnCategoriesChanged));

        public ObservableCollection<SidebarCategory> Categories
        {
            get => (ObservableCollection<SidebarCategory>)GetValue(CategoriesProperty);
            set => SetValue(CategoriesProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(SidebarItem),
                typeof(FloweryComponentSidebar),
                new PropertyMetadata(null, OnSelectedItemChanged));

        public SidebarItem? SelectedItem
        {
            get => (SidebarItem?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SidebarWidthProperty =
            DependencyProperty.Register(
                nameof(SidebarWidth),
                typeof(double),
                typeof(FloweryComponentSidebar),
                new PropertyMetadata(254d, OnSidebarWidthChanged));

        public double SidebarWidth
        {
            get => (double)GetValue(SidebarWidthProperty);
            set => SetValue(SidebarWidthProperty, value);
        }

        public static readonly DependencyProperty AutoSizeSidebarWidthProperty =
            DependencyProperty.Register(
                nameof(AutoSizeSidebarWidth),
                typeof(bool),
                typeof(FloweryComponentSidebar),
                new PropertyMetadata(false, OnAutoSizeSidebarWidthChanged));

        public bool AutoSizeSidebarWidth
        {
            get => (bool)GetValue(AutoSizeSidebarWidthProperty);
            set => SetValue(AutoSizeSidebarWidthProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(FloweryComponentSidebar),
                new PropertyMetadata(DaisySize.Medium, OnSizeChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(FloweryComponentSidebar),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value ?? string.Empty);
        }

        public event EventHandler<SidebarItemSelectedEventArgs>? ItemSelected;
        public event EventHandler<SidebarFavoriteToggledEventArgs>? FavoriteToggled;

        public FloweryComponentSidebar()
        {
            _lifecycle = new DaisyControlLifecycle(
                this,
                OnThemeUpdated,
                () => Size,
                s => Size = s,
                handleLifecycleEvents: false);

            BuildLayout();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            if (FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.GetIgnoreGlobalSize(this))
            {
                Size = FlowerySizeManager.CurrentSize;
            }
        }

        // Wrapper for lifecycle - combines all theme-related updates
        private void OnThemeUpdated()
        {
            UpdateThemeResources();
            ApplyFontSizeForSize(Size);
            UpdateTextForegrounds();
            RefreshItemVisuals();
        }

        private void BuildLayout()
        {
            _rootBorder = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 0),
                Width = SidebarWidth
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _searchInput = new DaisyInput
            {
                PlaceholderText = SearchPlaceholderText,
                Variant = DaisyInputVariant.Bordered,
                Margin = new Thickness(8, 8, 8, 8),
                Padding = new Thickness(10, 8, 10, 8),
                MinHeight = 36,
                NeumorphicEnabled = false
            };
            FlowerySizeManager.SetIgnoreGlobalSize(_searchInput, true);
            _searchInput.TextChanged += OnSearchInputTextChanged;
            Grid.SetRow(_searchInput, 0);
            grid.Children.Add(_searchInput);

            _categoriesScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollMode = ScrollMode.Disabled,
                VerticalScrollMode = ScrollMode.Auto,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsTabStop = true,
                // Transparent background required for hit-testing (mouse wheel input)
                Background = new SolidColorBrush(Colors.Transparent)
            };

            _categoriesPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 10, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinHeight = 200, // Ensure it has a size for layout debugging
                // Transparent background required for hit-testing (mouse wheel input)
                Background = new SolidColorBrush(Colors.Transparent)
            };
            _categoriesScrollViewer.Content = _categoriesPanel;
            Grid.SetRow(_categoriesScrollViewer, 1);
            grid.Children.Add(_categoriesScrollViewer);

            _rootBorder.Child = grid;
            Content = _rootBorder;

            UpdateThemeResources();
            ApplyFontSizeForSize(Size);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleLoaded();

            // Apply current theme state (theme may have been applied before we subscribed)
            UpdateThemeResources();
            ApplyFontSizeForSize(Size);
            UpdateTextForegrounds();

            if (_pendingRebuild)
            {
                var preserve = _pendingRebuildPreserveScroll;
                var suppress = _pendingRebuildSuppressSelection;
                _pendingRebuild = false;
                _pendingRebuildPreserveScroll = false;
                _pendingRebuildSuppressSelection = false;
                RequestRebuildCategories(preserve, suppress, "OnLoaded pending");
            }
            else
            {
                RequestRebuildCategories(preserveScrollOffset: false, suppressSelectionEvents: false, "OnLoaded");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleUnloaded();
        }

        public (string? lastItemId, SidebarCategory? category) GetLastViewedItem()
        {
            if (SelectedItem != null)
            {
                var cat = FindCategoryForItem(SelectedItem);
                return (SelectedItem.Id, cat);
            }
            return (null, null);
        }

        private static void OnCategoriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryComponentSidebar sidebar)
            {
                sidebar.OnCategoriesChanged(e.NewValue as ObservableCollection<SidebarCategory>);
            }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryComponentSidebar sidebar)
            {
                sidebar.UpdateSelectedItemVisual((SidebarItem?)e.NewValue);
            }
        }

        private static void OnSidebarWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryComponentSidebar sidebar && sidebar._rootBorder != null)
            {
                sidebar._rootBorder.Width = (double)e.NewValue;
                sidebar.UpdateContentWidths();
            }
        }

        private static void OnAutoSizeSidebarWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryComponentSidebar sidebar && e.NewValue is bool enabled && enabled)
            {
                sidebar.ApplySidebarWidthForSize(sidebar.Size);
            }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryComponentSidebar sidebar)
            {
                sidebar.ApplySidebarWidthForSize(sidebar.Size);
                sidebar.ApplyFontSizeForSize(sidebar.Size);
                sidebar.ApplySizeToControls();
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryComponentSidebar sidebar)
            {
                sidebar.ApplySearchText((string?)e.NewValue);
            }
        }

        private static void OnSearchPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryComponentSidebar sidebar && sidebar._searchInput != null)
            {
                sidebar._searchInput.PlaceholderText = (string?)e.NewValue ?? string.Empty;
            }
        }

        private void OnCategoriesChanged(ObservableCollection<SidebarCategory>? newCategories)
        {
            ClearCategoryHandlers();

            _allCategories = newCategories ?? [];
            LoadState();
            RequestRebuildCategories(preserveScrollOffset: false, suppressSelectionEvents: false, "OnCategoriesChanged");

            foreach (var category in _allCategories)
            {
                RegisterCategoryHandler(category, (_, args) =>
                {
                    if (args.PropertyName == nameof(SidebarCategory.IsExpanded))
                        SaveState(SelectedItem?.Id);
                });
            }
        }

        private void UpdateSelectedItemVisual(SidebarItem? item)
        {
            if (item == null)
                return;

            if (_itemViews.TryGetValue(item, out var view))
            {
                SetSelectedItemView(view, scrollToView: true);
            }
        }

        private void OnSearchInputTextChanged(DaisyInput sender, TextChangedEventArgs e)
        {
            if (_updatingSearch)
                return;

            _updatingSearch = true;
            try
            {
                SearchText = sender.Text ?? string.Empty;
            }
            finally
            {
                _updatingSearch = false;
            }
        }

        private void ApplySearchText(string? newValue)
        {
            if (_searchInput != null && !_updatingSearch)
            {
                _searchInput.Text = newValue ?? string.Empty;
            }

            RequestRebuildCategories(preserveScrollOffset: false, suppressSelectionEvents: false, "ApplySearchText");
        }

        private void UpdateThemeResources()
        {
            if (_rootBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            _rootBorder.Background = GetBrush(resources, "DaisyBase200Brush", new SolidColorBrush(Color.FromArgb(255, 30, 30, 32)));
            _rootBorder.BorderBrush = GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Color.FromArgb(255, 60, 60, 70)));
        }

        private void UpdateTextForegrounds()
        {
            var resources = Application.Current?.Resources;
            var baseContentBrush = GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.White));

            _currentBaseContentBrush = baseContentBrush;

            // Category headers (DaisyExpander.Header) are created as DaisyIconText.
            if (_categoriesPanel != null)
            {
                foreach (var child in _categoriesPanel.Children)
                {
                    if (child is not DaisyExpander expander)
                        continue;

                    expander.HeaderForeground = baseContentBrush;
                    expander.Foreground = baseContentBrush;

                    if (expander.Header is DaisyIconText dit)
                    {
                        dit.Foreground = baseContentBrush;
                    }
                }
            }

            UpdateCustomTextForegrounds(baseContentBrush);
        }

        protected virtual void UpdateCustomTextForegrounds(Brush baseContentBrush)
        {
        }

        protected virtual void ApplySizeToControls()
        {
            if (_searchInput != null)
            {
                _searchInput.Size = Size;
            }
        }

        private void ApplySidebarWidthForSize(DaisySize size)
        {
            if (!AutoSizeSidebarWidth)
                return;

            SidebarWidth = FlowerySizeManager.GetSidebarWidth(size);
        }

        private void ApplyFontSizeForSize(DaisySize size)
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            FontSize = GetFontSizeForSize(size, resources);

            UpdateCategoryHeaderFontSizes();
            UpdateItemFontSizes();
            ApplyControlFontSizes();
        }

        protected static double GetFontSizeForSize(DaisySize size, ResourceDictionary? resources)
        {
            var fallback = size switch
            {
                DaisySize.ExtraSmall => 8d,
                DaisySize.Small => 10d,
                DaisySize.Medium => 12d,
                DaisySize.Large => 14d,
                DaisySize.ExtraLarge => 16d,
                _ => 12d
            };

            var key = size switch
            {
                DaisySize.ExtraSmall => "DaisySizeExtraSmallFontSize",
                DaisySize.Small => "DaisySizeSmallFontSize",
                DaisySize.Medium => "DaisySizeMediumFontSize",
                DaisySize.Large => "DaisySizeLargeFontSize",
                DaisySize.ExtraLarge => "DaisySizeExtraLargeFontSize",
                _ => "DaisySizeMediumFontSize"
            };

            var value = resources != null
                ? DaisyResourceLookup.GetDouble(resources, key, fallback)
                : fallback;

            return Math.Max(1d, value * SidebarFontScale);
        }

        protected static DaisySize GetSelectorSize(DaisySize size)
        {
            return size == DaisySize.ExtraLarge ? DaisySize.Large : size;
        }

        protected virtual void ApplyControlFontSizes()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            var fontSize = GetFontSizeForSize(Size, resources);

            if (_searchInput != null)
            {
                _searchInput.FontSize = fontSize;
            }
        }

        private void UpdateCategoryHeaderFontSizes()
        {
            if (_categoriesPanel == null)
                return;

            var metrics = GetSidebarMetrics();
            foreach (var child in _categoriesPanel.Children)
            {
                if (child is not DaisyExpander expander)
                    continue;

                expander.ChevronSize = metrics.ChevronSize;
                expander.HeaderPadding = metrics.ExpanderPadding;
                expander.MinHeight = metrics.ExpanderMinHeight;

                if (expander.Header is DaisyIconText dit)
                {
                    dit.FontSizeOverride = GetHeaderFontSize(Size);
                    dit.IconSize = metrics.HeaderIconSize;
                    dit.Spacing = metrics.HeaderSpacing;
                    dit.Size = Size;
                }
            }
        }

        private void UpdateItemFontSizes()
        {
            var metrics = GetSidebarMetrics();
            foreach (var view in _itemViews.Values)
            {
                view.Container.Padding = metrics.ItemPadding;
                view.Container.Margin = metrics.ItemMargin;

                view.TextItem.FontSizeOverride = GetItemFontSize(Size);
                view.TextItem.Size = Size;

                if (view.FavoriteFilled is DaisyIconText filled)
                {
                    filled.IconSize = metrics.FavoriteIconSize;
                    filled.Size = Size;
                }

                if (view.FavoriteOutline is DaisyIconText outline)
                {
                    outline.IconSize = metrics.FavoriteIconSize;
                    outline.Size = Size;
                }
            }
        }

        private static double GetWidthMinusMargin(double width, Thickness margin)
        {
            return Math.Max(0, width - margin.Left - margin.Right);
        }

        private double GetSidebarContentWidth()
        {
            var width = SidebarWidth;

            if (_rootBorder != null && !double.IsNaN(_rootBorder.ActualWidth) && _rootBorder.ActualWidth > 0)
                width = _rootBorder.ActualWidth;

            if (_categoriesPanel != null)
                width = GetWidthMinusMargin(width, _categoriesPanel.Margin);

            return Math.Max(0, width);
        }

        private void UpdateContentWidths()
        {
            if (_categoriesPanel == null)
                return;

            var contentWidth = GetSidebarContentWidth();
            if (contentWidth <= 0)
                return;

            // Do not manually set Width on the panel or its children.
            // Let the layout system and HorizontalAlignment="Stretch" handle it.
            // This ensures correct sizing when scrollbars are visible.
        }

        protected void RequestRebuildCategories(bool preserveScrollOffset, bool suppressSelectionEvents, string reason)
        {
            if (_categoriesPanel == null)
                return;

            if (!IsLoaded)
            {
                _pendingRebuild = true;
                _pendingRebuildPreserveScroll |= preserveScrollOffset;
                _pendingRebuildSuppressSelection |= suppressSelectionEvents;
                return;
            }

            _pendingRebuild = false;
            _pendingRebuildPreserveScroll |= preserveScrollOffset;
            _pendingRebuildSuppressSelection |= suppressSelectionEvents;

            if (_rebuildQueued)
            {
                return;
            }

            _rebuildQueued = true;
            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    var preserve = _pendingRebuildPreserveScroll;
                    var suppress = _pendingRebuildSuppressSelection;
                    _pendingRebuildPreserveScroll = false;
                    _pendingRebuildSuppressSelection = false;
                    _rebuildQueued = false;
                    RebuildCategories(preserve, suppress);
                });
            }
            else
            {
                var preserve = _pendingRebuildPreserveScroll;
                var suppress = _pendingRebuildSuppressSelection;
                _pendingRebuildPreserveScroll = false;
                _pendingRebuildSuppressSelection = false;
                _rebuildQueued = false;
                RebuildCategories(preserve, suppress);
            }
        }

        private void RebuildCategories()
        {
            RebuildCategories(preserveScrollOffset: false, suppressSelectionEvents: false);
        }

        private void RebuildCategories(bool preserveScrollOffset, bool suppressSelectionEvents)
        {
            if (_categoriesPanel == null)
                return;

            _rebuildGeneration++;
            var currentGeneration = _rebuildGeneration;

            double? previousOffset = null;
            if (preserveScrollOffset && _categoriesScrollViewer != null)
                previousOffset = _categoriesScrollViewer.VerticalOffset;

            var previousSuppressSelection = _suppressSelectionEvents;
            _suppressSelectionEvents = suppressSelectionEvents;

            try
            {
                ClearItemViews();
                ClearCustomItemHosts();
                _buildQueue.Clear();
                _categoriesPanel.Children.Clear();

                var filteredCategories = GetFilteredCategories(SearchText);
                _currentMetrics = GetSidebarMetrics();

                _currentBaseContentBrush = GetBrush(
                    Application.Current?.Resources,
                    "DaisyBaseContentBrush",
                    new SolidColorBrush(Colors.White));

                _currentBase300Brush = GetBrush(
                    Application.Current?.Resources,
                    "DaisyBase300Brush",
                    new SolidColorBrush(Color.FromArgb(255, 60, 60, 70)));

                SidebarCategory? priorityCategory = null;
                if (SelectedItem != null)
                {
                    priorityCategory = FindCategoryForItem(SelectedItem);
                }

                foreach (var category in filteredCategories)
                {
                    var expander = CreateCategoryExpanderShell(category);
                    _categoriesPanel.Children.Add(expander);

                    if (category.IsExpanded && category.Items.Count > 0)
                    {
                        if (category == priorityCategory)
                        {
                            // Priority category goes to the front of the queue
                            // Actually, we'll build it immediately if it's the priority
                            FillCategoryItems(category, expander);
                        }
                        else
                        {
                            _buildQueue.Enqueue(category);
                        }
                    }
                }

                ApplySizeToControls();
                UpdateCategoryHeaderFontSizes();
                UpdateContentWidths();

                if (_buildQueue.Count > 0 && !_isProcessingQueue)
                {
                    _isProcessingQueue = true;
                    DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => ProcessNextBuildBatch(currentGeneration));
                }
            }
            catch (Exception ex)
            {
                FloweryDiagnostics.Log($"[FloweryComponentSidebar] RebuildCategories: FAILED: {ex}");
            }
            finally
            {
                _suppressSelectionEvents = previousSuppressSelection;
            }

            if (previousOffset.HasValue)
                RequestScrollRestore(previousOffset.Value);
        }

        private DaisyExpander CreateCategoryExpanderShell(SidebarCategory category)
        {
            var headerItem = new DaisyIconText
            {
                Text = category.DisplayName,
                // Use IconData (path string) instead of IconGeometry to avoid CloneGeometry issues on Skia
                IconData = FloweryPathHelpers.GetIconPathData(category.IconKey),
                Size = Size,
                Spacing = _currentMetrics.HeaderSpacing,
                IconSize = _currentMetrics.HeaderIconSize,
                FontSizeOverride = GetHeaderFontSize(Size),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Foreground = _currentBaseContentBrush,
                NeumorphicEnabled = false
            };
            FlowerySizeManager.SetIgnoreGlobalSize(headerItem, true);

            var expander = new DaisyExpander
            {
                IsExpanded = category.IsExpanded,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                HeaderPadding = _currentMetrics.ExpanderPadding,
                MinHeight = _currentMetrics.ExpanderMinHeight,
                ChevronSize = _currentMetrics.ChevronSize,
                Header = headerItem,
                HeaderForeground = _currentBaseContentBrush,
                Foreground = _currentBaseContentBrush,
                BorderBrush = _currentBase300Brush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 0, 0, 4),
                Tag = category, // Store category for later item filling
                NeumorphicEnabled = false
            };

            // Sync IsExpanded changes back to category
            expander.RegisterPropertyChangedCallback(DaisyExpander.IsExpandedProperty, (s, _) =>
            {
                if (s is DaisyExpander e && e.Tag is SidebarCategory cat)
                {
                    cat.IsExpanded = e.IsExpanded;
                    if (e.IsExpanded && (e.ExpanderContent == null || (e.ExpanderContent is Panel p && p.Children.Count == 0)))
                    {
                        FillCategoryItems(cat, e);
                    }
                }
            });

            var categoryItem = new SidebarItem { Id = "CategoryHeader_" + category.Name, Name = category.Name };
            RegisterItemLabelUpdate(categoryItem, headerItem);

            return expander;
        }

        private void FillCategoryItems(SidebarCategory category, DaisyExpander expander)
        {
            if (expander.ExpanderContent is Panel p && p.Children.Count > 0)
            {
                return;
            }

            var itemsPanel = new StackPanel
            {
                Margin = new Thickness(
                    _currentMetrics.ItemsPanelMargin.Left,
                    _currentMetrics.ItemsPanelMargin.Top + 2,
                    _currentMetrics.ItemsPanelMargin.Right,
                    _currentMetrics.ItemsPanelMargin.Bottom),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            for (int i = 0; i < category.Items.Count; i++)
            {
                try
                {
                    var item = category.Items[i];
                    var itemElement = CreateItemElement(item, category, itemsPanel);

                    // Add bottom border to last item for visual separation between sections
                    bool isLastItem = i == category.Items.Count - 1;
                    if (isLastItem && itemElement is Border itemBorder)
                    {
                        itemBorder.BorderBrush = _currentBase300Brush;
                        itemBorder.BorderThickness = new Thickness(0, 0, 0, 1);
                        itemBorder.Padding = new Thickness(
                            itemBorder.Padding.Left,
                            itemBorder.Padding.Top,
                            itemBorder.Padding.Right,
                            itemBorder.Padding.Bottom + 8);
                        itemBorder.Margin = new Thickness(
                            itemBorder.Margin.Left,
                            itemBorder.Margin.Top,
                            itemBorder.Margin.Right,
                            itemBorder.Margin.Bottom + 8);
                    }

                    if (!itemsPanel.Children.Contains(itemElement))
                    {
                        itemsPanel.Children.Add(itemElement);
                    }
                }
                catch (Exception itemEx)
                {
                    FloweryDiagnostics.Log($"[FloweryComponentSidebar] FillCategoryItems: item creation failed for category '{category.Name}', index {i}: {itemEx}");
                }
            }

            expander.ExpanderContent = itemsPanel;
            UpdateItemFontSizes(); // Refresh font sizes for newly added items
            RefreshItemVisuals();
        }

        private void ProcessNextBuildBatch(long generation)
        {
            if (generation != _rebuildGeneration || _buildQueue.Count == 0)
            {
                _isProcessingQueue = false;
                return;
            }

            var category = _buildQueue.Dequeue();

            // Find the expander for this category
            DaisyExpander? targetExpander = null;
            if (_categoriesPanel != null)
            {
                foreach (var child in _categoriesPanel.Children)
                {
                    if (child is DaisyExpander e && e.Tag == category)
                    {
                        targetExpander = e;
                        break;
                    }
                }
            }

            if (targetExpander != null)
            {
                FillCategoryItems(category, targetExpander);
            }

            if (_buildQueue.Count > 0)
            {
                DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => ProcessNextBuildBatch(generation));
            }
            else
            {
                _isProcessingQueue = false;
            }
        }

        private void RequestScrollRestore(double offset)
        {
            if (_categoriesScrollViewer == null)
                return;

            _pendingScrollRestoreOffset = offset;
            _pendingScrollRestore = true;

            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, RestoreScrollOffset);
            }
            else
            {
                RestoreScrollOffset();
            }
        }

        private void RestoreScrollOffset()
        {
            if (!_pendingScrollRestore || _categoriesScrollViewer == null || _pendingScrollRestoreOffset is not double offset)
                return;

            _pendingScrollRestore = false;
            _pendingScrollRestoreOffset = null;

            var targetOffset = Math.Clamp(offset, 0, _categoriesScrollViewer.ScrollableHeight);
            if (Math.Abs(_categoriesScrollViewer.VerticalOffset - targetOffset) < 0.5)
                return;

            _categoriesScrollViewer.ChangeView(null, targetOffset, null, disableAnimation: true);
        }

        protected virtual UIElement? TryCreateCustomItemElement(SidebarItem item, SidebarCategory category, Panel parentPanel)
        {
            return null;
        }

        private UIElement CreateItemElement(SidebarItem item, SidebarCategory category, Panel parentPanel)
        {
            var customElement = TryCreateCustomItemElement(item, category, parentPanel);
            return customElement ?? BuildSidebarItem(item, category);
        }

        protected virtual void ClearCustomItemHosts()
        {
        }

        private Border BuildSidebarItem(SidebarItem item, SidebarCategory category)
        {
            var metrics = GetSidebarMetrics();
            var container = new Border
            {
                Tag = item,
                CornerRadius = new CornerRadius(6),
                Padding = metrics.ItemPadding,
                Margin = metrics.ItemMargin,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textItem = new DaisyIconText
            {
                Text = item.DisplayName,
                Size = Size,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                NeumorphicEnabled = false
            };
            FlowerySizeManager.SetIgnoreGlobalSize(textItem, true);
            Grid.SetColumn(textItem, 0);
            grid.Children.Add(textItem);

            Button? favoriteButton = null;
            DaisyIconText? filledIcon = null;
            DaisyIconText? outlineIcon = null;

            if (item.ShowFavoriteIcon)
            {
                favoriteButton = new Button
                {
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(4, 0, 0, 0)
                };

                var favoritePanel = new Grid();

                filledIcon = new DaisyIconText
                {
                    // Use IconData (path string) instead of IconGeometry to avoid CloneGeometry issues on Skia
                    IconData = FloweryPathHelpers.GetIconPathData("DaisyIconStarFilled"),
                    IconSize = metrics.FavoriteIconSize,
                    Size = Size,
                    Foreground = GetBrush(Application.Current?.Resources, "DaisyWarningBrush", new SolidColorBrush(Color.FromArgb(255, 245, 158, 11))),
                    NeumorphicEnabled = false
                };
                FlowerySizeManager.SetIgnoreGlobalSize(filledIcon, true);
                if (filledIcon != null)
                {
                    filledIcon.Visibility = item.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
                    favoritePanel.Children.Add(filledIcon);
                }

                outlineIcon = new DaisyIconText
                {
                    // Use IconData (path string) instead of IconGeometry to avoid CloneGeometry issues on Skia
                    IconData = FloweryPathHelpers.GetIconPathData("DaisyIconStarOutline"),
                    IconSize = metrics.FavoriteIconSize,
                    Size = Size,
                    Foreground = GetBrush(Application.Current?.Resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.White)),
                    Opacity = 0.5,
                    NeumorphicEnabled = false
                };
                FlowerySizeManager.SetIgnoreGlobalSize(outlineIcon, true);
                if (outlineIcon != null)
                {
                    outlineIcon.Visibility = item.IsFavorite ? Visibility.Collapsed : Visibility.Visible;
                    favoritePanel.Children.Add(outlineIcon);
                }
                favoriteButton.Content = favoritePanel;

                favoriteButton.Click += (_, args) =>
                {
                    item.IsFavorite = !item.IsFavorite;
                    item.RaiseFavoriteToggleRequested();
                    FavoriteToggled?.Invoke(this, new SidebarFavoriteToggledEventArgs(item));
                };

                Grid.SetColumn(favoriteButton, 1);
                grid.Children.Add(favoriteButton);
            }

            DaisyBadge? badge = null;
            if (!string.IsNullOrWhiteSpace(item.Badge))
            {
                badge = new DaisyBadge
                {
                    Variant = DaisyBadgeVariant.Neutral,
                    Size = DaisySize.ExtraSmall,
                    Content = item.Badge,
                    Margin = new Thickness(8, 0, 0, 0),
                    Opacity = 0.7
                };
                Grid.SetColumn(badge, 2);
                grid.Children.Add(badge);
            }

            container.Child = grid;

            var view = new SidebarItemView(container, textItem, item, category)
            {
                FavoriteButton = favoriteButton,
                FavoriteFilled = filledIcon,
                FavoriteOutline = outlineIcon
            };
            _itemViews[item] = view;

            RegisterItemLabelUpdate(item, textItem);
            RegisterFavoriteUpdate(item, view);

            container.PointerEntered += (_, _) =>
            {
                view.IsHovered = true;
                ApplyItemVisuals(view);
            };
            container.PointerExited += (_, _) =>
            {
                view.IsHovered = false;
                ApplyItemVisuals(view);
            };
            container.Tapped += (s, args) =>
            {
                if (favoriteButton != null && IsFromElement(args.OriginalSource as DependencyObject, favoriteButton))
                    return;

                SelectItem(item, category, view, scrollToView: false);
            };

            if (SelectedItem != null && SelectedItem == item)
            {
                if (_suppressSelectionEvents)
                {
                    SetSelectedItemView(view, scrollToView: false);
                }
                else
                {
                    // Trigger full selection logic (event, scroll) to restore state
                    SelectItem(item, category, view, scrollToView: true);
                }
            }

            return container;
        }
        protected void RegisterItemLabelUpdate(SidebarItem item, DaisyIconText iconText)
        {
            RegisterItemHandler(item, (_, args) =>
            {
                if (args.PropertyName == nameof(SidebarItem.DisplayName))
                {
                    iconText.Text = item.DisplayName;
                }
            });
        }

        protected void RegisterItemLabelUpdate(SidebarItem item, TextBlock textBlock)
        {
            RegisterItemHandler(item, (_, args) =>
            {
                if (args.PropertyName == nameof(SidebarItem.DisplayName))
                {
                    textBlock.Text = item.DisplayName;
                }
            });
        }

        protected void RegisterItemLabelUpdate(SidebarItem item, DaisyToggle toggle)
        {
            RegisterItemHandler(item, (_, args) =>
            {
                if (args.PropertyName == nameof(SidebarItem.DisplayName))
                {
                    toggle.Content = item.DisplayName;
                }
            });
        }

        private void RegisterFavoriteUpdate(SidebarItem item, SidebarItemView view)
        {
            if (view.FavoriteFilled == null || view.FavoriteOutline == null)
                return;

            RegisterItemHandler(item, (_, args) =>
            {
                if (args.PropertyName == nameof(SidebarItem.IsFavorite))
                {
                    view.FavoriteFilled.Visibility = item.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
                    view.FavoriteOutline.Visibility = item.IsFavorite ? Visibility.Collapsed : Visibility.Visible;
                }
            });
        }

        private void RegisterItemHandler(SidebarItem item, PropertyChangedEventHandler handler)
        {
            if (!_itemHandlers.TryGetValue(item, out var handlers))
            {
                handlers = [];
                _itemHandlers[item] = handlers;
            }

            handlers.Add(handler);
            item.PropertyChanged += handler;
        }

        private void RegisterCategoryHandler(SidebarCategory category, PropertyChangedEventHandler handler)
        {
            if (!_categoryHandlers.TryGetValue(category, out var handlers))
            {
                handlers = [];
                _categoryHandlers[category] = handlers;
            }

            handlers.Add(handler);
            category.PropertyChanged += handler;
        }

        private void SelectItem(SidebarItem item, SidebarCategory category, SidebarItemView view, bool scrollToView)
        {
            SelectedItem = item;
            SaveState(item.Id);
            ItemSelected?.Invoke(this, new SidebarItemSelectedEventArgs(item, category));
            SetSelectedItemView(view, scrollToView: scrollToView);
        }

        private void SetSelectedItemView(SidebarItemView view, bool scrollToView = false)
        {
            if (_selectedItemView != null && _selectedItemView != view)
            {
                ApplyItemVisuals(_selectedItemView);
            }

            _selectedItemView = view;
            ApplyItemVisuals(view, forceSelected: true);

            if (scrollToView)
            {
                // Scroll the sidebar item into view within the sidebar's ScrollViewer
                if (view.Container.IsLoaded)
                {
                    DispatcherQueue.TryEnqueue(() => ScrollIntoView(view.Container));
                }
                else
                {
                    void OnLoaded(object s, RoutedEventArgs e)
                    {
                        view.Container.Loaded -= OnLoaded;
                        DispatcherQueue.TryEnqueue(() => ScrollIntoView(view.Container));
                    }
                    view.Container.Loaded += OnLoaded;
                }
            }
        }

        private static void ScrollIntoView(FrameworkElement container)
        {
            try
            {
                container.StartBringIntoView(new BringIntoViewOptions
                {
                    AnimationDesired = false,
                    VerticalAlignmentRatio = 0.5
                });
            }
            catch { }
        }

        private void RefreshItemVisuals()
        {
            foreach (var view in _itemViews.Values)
            {
                ApplyItemVisuals(view);
            }
        }

        private void ApplyItemVisuals(SidebarItemView view, bool forceSelected = false)
        {
            var resources = Application.Current?.Resources;
            var baseContent = GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.White));
            var base300 = GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Color.FromArgb(255, 60, 60, 70)));

            // Use Neutral variant colors for selected items
            var neutral = GetBrush(resources, "DaisyNeutralBrush", new SolidColorBrush(Color.FromArgb(255, 42, 50, 60)));
            var neutralContent = GetBrush(resources, "DaisyNeutralContentBrush", new SolidColorBrush(Color.FromArgb(255, 166, 173, 187)));

            var isSelected = forceSelected || (SelectedItem != null && view.Item == SelectedItem);

            if (isSelected)
            {
                view.Container.Background = neutral;
                view.TextItem.Background = neutral;
                view.TextItem.Variant = DaisyBadgeVariant.Neutral;
                view.TextItem.Foreground = neutralContent;

                // Also update favorite icons if they exist to match primary content
                if (view.FavoriteOutline != null)
                {
                    view.FavoriteOutline.Background = neutral;
                    view.FavoriteOutline.Variant = DaisyBadgeVariant.Neutral;
                    view.FavoriteOutline.Foreground = neutralContent;
                }

                return;
            }

            if (view.IsHovered)
            {
                view.Container.Background = base300;
                view.TextItem.Background = base300;
                view.TextItem.Variant = DaisyBadgeVariant.Default;
                view.TextItem.Foreground = baseContent;

                if (view.FavoriteOutline != null)
                {
                    view.FavoriteOutline.Background = base300;
                    view.FavoriteOutline.Variant = DaisyBadgeVariant.Default;
                    view.FavoriteOutline.Foreground = baseContent;
                }

                return;
            }

            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            view.Container.Background = transparent;
            view.TextItem.Background = transparent;
            view.TextItem.Variant = DaisyBadgeVariant.Default;
            view.TextItem.Foreground = baseContent;

            if (view.FavoriteOutline != null)
            {
                view.FavoriteOutline.Background = transparent;
                view.FavoriteOutline.Variant = DaisyBadgeVariant.Default;
                view.FavoriteOutline.Foreground = baseContent;
            }
        }

        private void ClearItemViews()
        {
            foreach (var pair in _itemHandlers)
            {
                foreach (var handler in pair.Value)
                {
                    pair.Key.PropertyChanged -= handler;
                }
            }
            _itemHandlers.Clear();
            _itemViews.Clear();
            _selectedItemView = null;
        }

        private void ClearCategoryHandlers()
        {
            foreach (var pair in _categoryHandlers)
            {
                foreach (var handler in pair.Value)
                {
                    pair.Key.PropertyChanged -= handler;
                }
            }
            _categoryHandlers.Clear();
        }

        private SidebarCategory? FindCategoryForItem(SidebarItem item)
        {
            foreach (var category in _allCategories)
            {
                if (category.Items.Any(i => i.Name == item.Name && i.TabHeader == item.TabHeader))
                    return category;
            }
            return null;
        }

        private IReadOnlyList<SidebarCategory> GetFilteredCategories(string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return _allCategories;
            }

            var search = searchText.Trim();
            List<SidebarCategory> filtered = [];

            foreach (var category in _allCategories)
            {
                // Always include Home section regardless of search filter
                var isHomeCategory = category.Name == "Sidebar_Home";
                var categoryMatches = category.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase);

                var matchingItems = category.Items
                    .Where(item => item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (isHomeCategory || categoryMatches || matchingItems.Count > 0)
                {
                    filtered.Add(new SidebarCategory
                    {
                        Name = category.Name,
                        DisplayName = category.DisplayName,
                        IconKey = category.IconKey,
                        IsExpanded = true,
                        Items = (isHomeCategory || categoryMatches)
                            ? category.Items
                            : new ObservableCollection<SidebarItem>(matchingItems)
                    });
                }
            }
            return filtered;
        }

        private static bool IsFromElement(DependencyObject? source, DependencyObject target)
        {
            var current = source;
            while (current != null)
            {
                if (current == target)
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        protected static Brush GetBrush(ResourceDictionary? resources, string key, Brush fallback)
        {
            return DaisyResourceLookup.GetBrush(resources, key, fallback);
        }

        private static readonly SidebarMetrics ExtraSmallMetrics = new(
            expanderPadding: new Thickness(2),
            expanderMinHeight: 24,
            headerMargin: new Thickness(4, 0, 0, 0),
            headerSpacing: 4,
            headerIconSize: 12,
            itemsPanelMargin: new Thickness(0, 0, 0, 0),
            itemPadding: new Thickness(6, 2, 4, 2),
            itemMargin: new Thickness(4, 1, 4, 1),
            favoriteIconSize: 8,
            chevronSize: 10,
            selectorPanelMargin: new Thickness(0, 2, 0, 4),
            selectorLabelMargin: new Thickness(6, 0, 0, 0),
            selectorControlMargin: new Thickness(6, 0, 4, 0));

        private static readonly SidebarMetrics SmallMetrics = new(
            expanderPadding: new Thickness(3),
            expanderMinHeight: 28,
            headerMargin: new Thickness(6, 0, 0, 0),
            headerSpacing: 6,
            headerIconSize: 14,
            itemsPanelMargin: new Thickness(0, 0, 0, 0),
            itemPadding: new Thickness(7, 3, 6, 3),
            itemMargin: new Thickness(4, 1, 4, 1),
            favoriteIconSize: 10,
            chevronSize: 11,
            selectorPanelMargin: new Thickness(0, 3, 0, 5),
            selectorLabelMargin: new Thickness(7, 0, 0, 0),
            selectorControlMargin: new Thickness(7, 0, 6, 0));

        private static readonly SidebarMetrics MediumMetrics = new(
            expanderPadding: new Thickness(4),
            expanderMinHeight: 36,
            headerMargin: new Thickness(8, 0, 0, 0),
            headerSpacing: 8,
            headerIconSize: 16,
            itemsPanelMargin: new Thickness(0, 0, 0, 0),
            itemPadding: new Thickness(8, 4, 8, 4),
            itemMargin: new Thickness(4, 1, 4, 1),
            favoriteIconSize: 12,
            chevronSize: 12,
            selectorPanelMargin: new Thickness(0, 4, 0, 6),
            selectorLabelMargin: new Thickness(8, 0, 0, 0),
            selectorControlMargin: new Thickness(8, 0, 8, 0));

        private static readonly SidebarMetrics LargeMetrics = new(
            expanderPadding: new Thickness(5),
            expanderMinHeight: 40,
            headerMargin: new Thickness(10, 0, 0, 0),
            headerSpacing: 10,
            headerIconSize: 20,
            itemsPanelMargin: new Thickness(0, 0, 0, 0),
            itemPadding: new Thickness(9, 5, 10, 5),
            itemMargin: new Thickness(4, 1, 4, 1),
            favoriteIconSize: 16,
            chevronSize: 14,
            selectorPanelMargin: new Thickness(0, 5, 0, 7),
            selectorLabelMargin: new Thickness(9, 0, 0, 0),
            selectorControlMargin: new Thickness(9, 0, 10, 0));

        private static readonly SidebarMetrics ExtraLargeMetrics = new(
            expanderPadding: new Thickness(6),
            expanderMinHeight: 44,
            headerMargin: new Thickness(12, 0, 0, 0),
            headerSpacing: 10,
            headerIconSize: 24,
            itemsPanelMargin: new Thickness(0, 0, 0, 0),
            itemPadding: new Thickness(10, 6, 12, 6),
            itemMargin: new Thickness(4, 1, 4, 1),
            favoriteIconSize: 20,
            chevronSize: 16,
            selectorPanelMargin: new Thickness(0, 6, 0, 8),
            selectorLabelMargin: new Thickness(10, 0, 0, 0),
            selectorControlMargin: new Thickness(10, 0, 8, 0));

        protected SidebarMetrics GetSidebarMetrics()
        {
            return Size switch
            {
                DaisySize.ExtraSmall => ExtraSmallMetrics,
                DaisySize.Small => SmallMetrics,
                DaisySize.Medium => MediumMetrics,
                DaisySize.Large => LargeMetrics,
                DaisySize.ExtraLarge => ExtraLargeMetrics,
                _ => MediumMetrics
            };
        }

        protected readonly struct SidebarMetrics(
            Thickness expanderPadding,
            double expanderMinHeight,
            Thickness headerMargin,
            double headerSpacing,
            double headerIconSize,
            Thickness itemsPanelMargin,
            Thickness itemPadding,
            Thickness itemMargin,
            double favoriteIconSize,
            double chevronSize,
            Thickness selectorPanelMargin,
            Thickness selectorLabelMargin,
            Thickness selectorControlMargin)
        {
            public Thickness ExpanderPadding { get; } = expanderPadding;
            public double ExpanderMinHeight { get; } = expanderMinHeight;
            public Thickness HeaderMargin { get; } = headerMargin;
            public double HeaderSpacing { get; } = headerSpacing;
            public double HeaderIconSize { get; } = headerIconSize;
            public Thickness ItemsPanelMargin { get; } = itemsPanelMargin;
            public Thickness ItemPadding { get; } = itemPadding;
            public Thickness ItemMargin { get; } = itemMargin;
            public double FavoriteIconSize { get; } = favoriteIconSize;
            public double ChevronSize { get; } = chevronSize;
            public Thickness SelectorPanelMargin { get; } = selectorPanelMargin;
            public Thickness SelectorLabelMargin { get; } = selectorLabelMargin;
            public Thickness SelectorControlMargin { get; } = selectorControlMargin;
        }

        private static double GetHeaderFontSize(DaisySize size)
        {
            var resources = Application.Current?.Resources;
            var key = size switch
            {
                DaisySize.ExtraSmall => "DaisySizeExtraSmallSectionHeaderFontSize",
                DaisySize.Small => "DaisySizeSmallSectionHeaderFontSize",
                DaisySize.Medium => "DaisySizeMediumSectionHeaderFontSize",
                DaisySize.Large => "DaisySizeLargeSectionHeaderFontSize",
                DaisySize.ExtraLarge => "DaisySizeExtraLargeSectionHeaderFontSize",
                _ => "DaisySizeMediumSectionHeaderFontSize"
            };
            var fallback = size switch
            {
                DaisySize.ExtraSmall => 10d,
                DaisySize.Small => 12d,
                DaisySize.Medium => 13d,
                DaisySize.Large => 15d,
                DaisySize.ExtraLarge => 17d,
                _ => 13d
            };

            return resources != null
                ? DaisyResourceLookup.GetDouble(resources, key, fallback)
                : fallback;
        }

        private static double GetItemFontSize(DaisySize size)
        {
            var resources = Application.Current?.Resources;
            return GetFontSizeForSize(size, resources);
        }

        private void LoadState()
        {
            try
            {
                var lines = StateStorageProvider.Instance.LoadLines(StateKey);
                if (lines.Count == 0)
                    return;

                string? lastItemId = null;
                var collapsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in lines)
                {
                    if (line.StartsWith("last:", StringComparison.OrdinalIgnoreCase))
                        lastItemId = line[5..];
                    else if (line.StartsWith("collapsed:", StringComparison.OrdinalIgnoreCase))
                        collapsed.Add(line[10..]);
                }

                foreach (var cat in _allCategories)
                    cat.IsExpanded = !collapsed.Contains(cat.Name);

                if (lastItemId != null)
                {
                    foreach (var cat in _allCategories)
                    {
                        var item = cat.Items.FirstOrDefault(i => string.Equals(i.Id, lastItemId, StringComparison.OrdinalIgnoreCase));
                        if (item != null)
                        {
                            // Set SelectedItem and update visual state first
                            SelectedItem = item;
                            if (_itemViews.TryGetValue(item, out var view))
                            {
                                SetSelectedItemView(view, scrollToView: true);
                            }

                            // Fire ItemSelected event AFTER state is set
                            // Use TryEnqueue to ensure proper timing
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                ItemSelected?.Invoke(this, new SidebarItemSelectedEventArgs(item, cat));
                            });
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveState(string? currentItemId)
        {
            try
            {
                List<string> lines = [];
                if (!string.IsNullOrEmpty(currentItemId))
                    lines.Add("last:" + currentItemId);
                foreach (var cat in _allCategories.Where(c => !c.IsExpanded))
                    lines.Add("collapsed:" + cat.Name);
                StateStorageProvider.Instance.SaveLines(StateKey, lines);
            }
            catch
            {
            }
        }
    }
}

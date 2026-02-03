using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Flowery.Controls;
using Flowery.Localization;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// Home screen view for listing and managing available Kanban boards.
    /// </summary>
    public partial class FlowKanbanHome : DaisyBaseContentControl
    {
        private readonly HashSet<FlowBoardMetadata> _trackedBoards = new();
        private ItemsRepeater? _boardRepeater;
        private bool _isSyncingSortMode;
        private bool _isLocalizationSubscribed;
        private static readonly FlowKanbanHomeSortMode[] SortModeOrder =
        [
            FlowKanbanHomeSortMode.RecentlyModified,
            FlowKanbanHomeSortMode.RecentlyModifiedAscending,
            FlowKanbanHomeSortMode.NameAscending,
            FlowKanbanHomeSortMode.NameDescending
        ];

        public FlowKanbanHome()
        {
            DefaultStyleKey = typeof(FlowKanbanHome);
            EnsureFilteredBoards();
            UpdateWelcomeText();
            Loaded += OnHomeLoaded;
            Unloaded += OnHomeUnloaded;
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_boardRepeater != null)
            {
                _boardRepeater.ElementPrepared -= OnBoardElementPrepared;
                _boardRepeater.ElementClearing -= OnBoardElementClearing;
            }

            _boardRepeater = GetTemplateChild("PART_BoardGrid") as ItemsRepeater;
            if (_boardRepeater != null)
            {
                _boardRepeater.ElementPrepared += OnBoardElementPrepared;
                _boardRepeater.ElementClearing += OnBoardElementClearing;
            }
        }

        #region Localization
        public static readonly DependencyProperty LocalizationProperty =
            DependencyProperty.Register(
                nameof(Localization),
                typeof(FloweryLocalization),
                typeof(FlowKanbanHome),
                new PropertyMetadata(FloweryLocalization.Instance, OnLocalizationChanged));

        public FloweryLocalization Localization
        {
            get => (FloweryLocalization)GetValue(LocalizationProperty);
            set => SetValue(LocalizationProperty, value);
        }
        #endregion

        #region WelcomeMessage
        public static readonly DependencyProperty ShowWelcomeMessageProperty =
            DependencyProperty.Register(
                nameof(ShowWelcomeMessage),
                typeof(bool),
                typeof(FlowKanbanHome),
                new PropertyMetadata(true, OnWelcomeMessageChanged));

        public bool ShowWelcomeMessage
        {
            get => (bool)GetValue(ShowWelcomeMessageProperty);
            set => SetValue(ShowWelcomeMessageProperty, value);
        }

        public static readonly DependencyProperty WelcomeMessageTitleProperty =
            DependencyProperty.Register(
                nameof(WelcomeMessageTitle),
                typeof(string),
                typeof(FlowKanbanHome),
                new PropertyMetadata(string.Empty, OnWelcomeMessageChanged));

        public string WelcomeMessageTitle
        {
            get => (string)GetValue(WelcomeMessageTitleProperty);
            set => SetValue(WelcomeMessageTitleProperty, value ?? string.Empty);
        }

        public static readonly DependencyProperty WelcomeMessageSubtitleProperty =
            DependencyProperty.Register(
                nameof(WelcomeMessageSubtitle),
                typeof(string),
                typeof(FlowKanbanHome),
                new PropertyMetadata(string.Empty, OnWelcomeMessageChanged));

        public string WelcomeMessageSubtitle
        {
            get => (string)GetValue(WelcomeMessageSubtitleProperty);
            set => SetValue(WelcomeMessageSubtitleProperty, value ?? string.Empty);
        }

        public static readonly DependencyProperty IsWelcomeMessageVisibleProperty =
            DependencyProperty.Register(
                nameof(IsWelcomeMessageVisible),
                typeof(bool),
                typeof(FlowKanbanHome),
                new PropertyMetadata(false));

        public bool IsWelcomeMessageVisible
        {
            get => (bool)GetValue(IsWelcomeMessageVisibleProperty);
            private set => SetValue(IsWelcomeMessageVisibleProperty, value);
        }

        public static readonly DependencyProperty WelcomeTitleDisplayProperty =
            DependencyProperty.Register(
                nameof(WelcomeTitleDisplay),
                typeof(string),
                typeof(FlowKanbanHome),
                new PropertyMetadata(string.Empty));

        public string WelcomeTitleDisplay
        {
            get => (string)GetValue(WelcomeTitleDisplayProperty);
            private set => SetValue(WelcomeTitleDisplayProperty, value ?? string.Empty);
        }

        public static readonly DependencyProperty WelcomeSubtitleDisplayProperty =
            DependencyProperty.Register(
                nameof(WelcomeSubtitleDisplay),
                typeof(string),
                typeof(FlowKanbanHome),
                new PropertyMetadata(string.Empty));

        public string WelcomeSubtitleDisplay
        {
            get => (string)GetValue(WelcomeSubtitleDisplayProperty);
            private set => SetValue(WelcomeSubtitleDisplayProperty, value ?? string.Empty);
        }

        private static void OnWelcomeMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanHome home)
            {
                home.UpdateWelcomeText();
            }
        }

        private static void OnLocalizationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanHome home)
            {
                home.UpdateWelcomeText();
            }
        }
        #endregion

        #region Boards
        public static readonly DependencyProperty BoardsProperty =
            DependencyProperty.Register(
                nameof(Boards),
                typeof(ObservableCollection<FlowBoardMetadata>),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null, OnBoardsChanged));

        public ObservableCollection<FlowBoardMetadata> Boards
        {
            get
            {
                if (GetValue(BoardsProperty) is not ObservableCollection<FlowBoardMetadata> boards)
                {
                    boards = new ObservableCollection<FlowBoardMetadata>();
                    SetValue(BoardsProperty, boards);
                }

                return boards;
            }
            set => SetValue(BoardsProperty, value);
        }

        private static void OnBoardsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanHome home)
            {
                home.AttachBoards(e.OldValue as ObservableCollection<FlowBoardMetadata>,
                    e.NewValue as ObservableCollection<FlowBoardMetadata>);
            }
        }
        #endregion

        #region FilteredBoards
        public static readonly DependencyProperty FilteredBoardsProperty =
            DependencyProperty.Register(
                nameof(FilteredBoards),
                typeof(ObservableCollection<FlowBoardMetadata>),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null));

        public ObservableCollection<FlowBoardMetadata> FilteredBoards
        {
            get => EnsureFilteredBoards();
            private set => SetValue(FilteredBoardsProperty, value);
        }
        #endregion

        #region SearchText
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(FlowKanbanHome),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanHome home)
            {
                home.UpdateFilteredBoards();
            }
        }
        #endregion

        #region SortMode
        public static readonly DependencyProperty SortModeProperty =
            DependencyProperty.Register(
                nameof(SortMode),
                typeof(FlowKanbanHomeSortMode),
                typeof(FlowKanbanHome),
                new PropertyMetadata(FlowKanbanHomeSortMode.RecentlyModified, OnSortModeChanged));

        public FlowKanbanHomeSortMode SortMode
        {
            get => (FlowKanbanHomeSortMode)GetValue(SortModeProperty);
            set => SetValue(SortModeProperty, value);
        }

        private static void OnSortModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanHome home)
            {
                home.SyncSortModeIndex((FlowKanbanHomeSortMode)e.NewValue);
                home.UpdateFilteredBoards();
            }
        }
        #endregion

        #region SortModeIndex
        public static readonly DependencyProperty SortModeIndexProperty =
            DependencyProperty.Register(
                nameof(SortModeIndex),
                typeof(int),
                typeof(FlowKanbanHome),
                new PropertyMetadata(0, OnSortModeIndexChanged));

        public int SortModeIndex
        {
            get => (int)GetValue(SortModeIndexProperty);
            set => SetValue(SortModeIndexProperty, value);
        }

        private static void OnSortModeIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanHome home)
            {
                home.ApplySortModeFromIndex((int)e.NewValue);
            }
        }
        #endregion

        #region HasBoards
        public static readonly DependencyProperty HasBoardsProperty =
            DependencyProperty.Register(
                nameof(HasBoards),
                typeof(bool),
                typeof(FlowKanbanHome),
                new PropertyMetadata(false));

        public bool HasBoards
        {
            get => (bool)GetValue(HasBoardsProperty);
            private set => SetValue(HasBoardsProperty, value);
        }
        #endregion

        #region HasAnyBoards
        public static readonly DependencyProperty HasAnyBoardsProperty =
            DependencyProperty.Register(
                nameof(HasAnyBoards),
                typeof(bool),
                typeof(FlowKanbanHome),
                new PropertyMetadata(false));

        public bool HasAnyBoards
        {
            get => (bool)GetValue(HasAnyBoardsProperty);
            private set => SetValue(HasAnyBoardsProperty, value);
        }
        #endregion

        #region IsEmptyStateVisible
        public static readonly DependencyProperty IsEmptyStateVisibleProperty =
            DependencyProperty.Register(
                nameof(IsEmptyStateVisible),
                typeof(bool),
                typeof(FlowKanbanHome),
                new PropertyMetadata(true));

        public bool IsEmptyStateVisible
        {
            get => (bool)GetValue(IsEmptyStateVisibleProperty);
            private set => SetValue(IsEmptyStateVisibleProperty, value);
        }
        #endregion

        #region Commands
        public static readonly DependencyProperty OpenBoardCommandProperty =
            DependencyProperty.Register(
                nameof(OpenBoardCommand),
                typeof(ICommand),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null));

        public ICommand? OpenBoardCommand
        {
            get => (ICommand?)GetValue(OpenBoardCommandProperty);
            set => SetValue(OpenBoardCommandProperty, value);
        }

        public static readonly DependencyProperty CreateBoardCommandProperty =
            DependencyProperty.Register(
                nameof(CreateBoardCommand),
                typeof(ICommand),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null));

        public ICommand? CreateBoardCommand
        {
            get => (ICommand?)GetValue(CreateBoardCommandProperty);
            set => SetValue(CreateBoardCommandProperty, value);
        }

        public static readonly DependencyProperty CreateDemoBoardCommandProperty =
            DependencyProperty.Register(
                nameof(CreateDemoBoardCommand),
                typeof(ICommand),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null));

        public ICommand? CreateDemoBoardCommand
        {
            get => (ICommand?)GetValue(CreateDemoBoardCommandProperty);
            set => SetValue(CreateDemoBoardCommandProperty, value);
        }

        public static readonly DependencyProperty RenameBoardHomeCommandProperty =
            DependencyProperty.Register(
                nameof(RenameBoardHomeCommand),
                typeof(ICommand),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null));

        public ICommand? RenameBoardHomeCommand
        {
            get => (ICommand?)GetValue(RenameBoardHomeCommandProperty);
            set => SetValue(RenameBoardHomeCommandProperty, value);
        }

        public static readonly DependencyProperty DeleteBoardCommandProperty =
            DependencyProperty.Register(
                nameof(DeleteBoardCommand),
                typeof(ICommand),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null));

        public ICommand? DeleteBoardCommand
        {
            get => (ICommand?)GetValue(DeleteBoardCommandProperty);
            set => SetValue(DeleteBoardCommandProperty, value);
        }

        public static readonly DependencyProperty DuplicateBoardCommandProperty =
            DependencyProperty.Register(
                nameof(DuplicateBoardCommand),
                typeof(ICommand),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null));

        public ICommand? DuplicateBoardCommand
        {
            get => (ICommand?)GetValue(DuplicateBoardCommandProperty);
            set => SetValue(DuplicateBoardCommandProperty, value);
        }

        public static readonly DependencyProperty ExportBoardCommandProperty =
            DependencyProperty.Register(
                nameof(ExportBoardCommand),
                typeof(ICommand),
                typeof(FlowKanbanHome),
                new PropertyMetadata(null));

        public ICommand? ExportBoardCommand
        {
            get => (ICommand?)GetValue(ExportBoardCommandProperty);
            set => SetValue(ExportBoardCommandProperty, value);
        }
        #endregion

        private ObservableCollection<FlowBoardMetadata> EnsureFilteredBoards()
        {
            if (GetValue(FilteredBoardsProperty) is not ObservableCollection<FlowBoardMetadata> boards)
            {
                boards = new ObservableCollection<FlowBoardMetadata>();
                boards.CollectionChanged += OnFilteredBoardsCollectionChanged;
                SetValue(FilteredBoardsProperty, boards);
            }

            return boards;
        }

        private void AttachBoards(ObservableCollection<FlowBoardMetadata>? oldBoards, ObservableCollection<FlowBoardMetadata>? newBoards)
        {
            if (oldBoards != null)
            {
                oldBoards.CollectionChanged -= OnBoardsCollectionChanged;
                DetachBoardItems(oldBoards);
            }

            if (newBoards != null)
            {
                newBoards.CollectionChanged += OnBoardsCollectionChanged;
                AttachBoardItems(newBoards);
            }

            UpdateFilteredBoards();
        }

        private void OnBoardsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (FlowBoardMetadata board in e.OldItems)
                {
                    DetachBoardItem(board);
                }
            }

            if (e.NewItems != null)
            {
                foreach (FlowBoardMetadata board in e.NewItems)
                {
                    AttachBoardItem(board);
                }
            }

            UpdateFilteredBoards();
        }

        private void AttachBoardItems(IEnumerable<FlowBoardMetadata> boards)
        {
            foreach (var board in boards)
            {
                AttachBoardItem(board);
            }
        }

        private void DetachBoardItems(IEnumerable<FlowBoardMetadata> boards)
        {
            foreach (var board in boards)
            {
                DetachBoardItem(board);
            }
        }

        private void AttachBoardItem(FlowBoardMetadata board)
        {
            if (_trackedBoards.Add(board))
            {
                board.PropertyChanged += OnBoardPropertyChanged;
            }
        }

        private void DetachBoardItem(FlowBoardMetadata board)
        {
            if (_trackedBoards.Remove(board))
            {
                board.PropertyChanged -= OnBoardPropertyChanged;
            }
        }

        private void OnBoardPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(FlowBoardMetadata.Title), StringComparison.Ordinal) ||
                string.Equals(e.PropertyName, nameof(FlowBoardMetadata.LastModified), StringComparison.Ordinal))
            {
                UpdateFilteredBoards();
            }
        }

        private void OnFilteredBoardsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateBoardVisibilityState(FilteredBoards.Count);
        }

        private void UpdateFilteredBoards()
        {
            var filtered = EnsureFilteredBoards();
            filtered.CollectionChanged -= OnFilteredBoardsCollectionChanged;

            try
            {
                filtered.Clear();

                var boards = Boards ?? new ObservableCollection<FlowBoardMetadata>();
                var filteredItems = ApplyFilteringAndSorting(boards);
                foreach (var board in filteredItems)
                {
                    filtered.Add(board);
                }
            }
            finally
            {
                filtered.CollectionChanged += OnFilteredBoardsCollectionChanged;
            }

            UpdateBoardVisibilityState(filtered.Count);
        }

        private IReadOnlyList<FlowBoardMetadata> ApplyFilteringAndSorting(IEnumerable<FlowBoardMetadata> boards)
        {
            var query = boards ?? Enumerable.Empty<FlowBoardMetadata>();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var filter = SearchText.Trim();
                query = query.Where(board => board.Title.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            query = SortMode switch
            {
                FlowKanbanHomeSortMode.NameAscending => query.OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase),
                FlowKanbanHomeSortMode.NameDescending => query.OrderByDescending(b => b.Title, StringComparer.OrdinalIgnoreCase),
                FlowKanbanHomeSortMode.RecentlyModifiedAscending => query.OrderBy(b => b.LastModified),
                _ => query.OrderByDescending(b => b.LastModified)
            };

            return query.ToList();
        }

        private void UpdateBoardVisibilityState(int count)
        {
            var hasBoards = count > 0;
            HasAnyBoards = (Boards?.Count ?? 0) > 0;
            HasBoards = hasBoards;
            IsEmptyStateVisible = !hasBoards;
        }

        private void ApplySortModeFromIndex(int index)
        {
            if (_isSyncingSortMode)
                return;

            var mode = GetSortModeFromIndex(index);

            if (SortMode != mode)
            {
                SortMode = mode;
            }
        }

        private void SyncSortModeIndex(FlowKanbanHomeSortMode mode)
        {
            if (_isSyncingSortMode)
                return;

            _isSyncingSortMode = true;
            try
            {
                var targetIndex = GetIndexFromSortMode(mode);
                if (SortModeIndex != targetIndex)
                {
                    SortModeIndex = targetIndex;
                }
            }
            finally
            {
                _isSyncingSortMode = false;
            }
        }

        private static FlowKanbanHomeSortMode GetSortModeFromIndex(int index)
        {
            return index >= 0 && index < SortModeOrder.Length
                ? SortModeOrder[index]
                : FlowKanbanHomeSortMode.RecentlyModified;
        }

        private static int GetIndexFromSortMode(FlowKanbanHomeSortMode mode)
        {
            var index = Array.IndexOf(SortModeOrder, mode);
            return index >= 0 ? index : 0;
        }

        private void OnBoardElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            if (args.Element is not FrameworkElement element || element.DataContext is not FlowBoardMetadata board)
                return;

            if (element.FindName("PART_BoardOpenSurface") is FrameworkElement openSurface)
            {
                openSurface.Tapped -= OnBoardSurfaceTapped;
                openSurface.Tapped += OnBoardSurfaceTapped;
            }

            if (element.FindName("PART_BoardMenuButton") is DaisyButton menuButton)
            {
                if (menuButton.Flyout is MenuFlyout flyout)
                {
                    foreach (var item in flyout.Items)
                    {
                        if (item is MenuFlyoutItem menuItem)
                        {
                            menuItem.CommandParameter = board;
                            menuItem.Command = ResolveMenuCommand(menuItem);
                        }
                    }
                }
            }
        }

        private void OnBoardElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
        {
            if (args.Element is not FrameworkElement element)
                return;

            if (element.FindName("PART_BoardOpenSurface") is FrameworkElement openSurface)
            {
                openSurface.Tapped -= OnBoardSurfaceTapped;
            }

            if (element.FindName("PART_BoardMenuButton") is DaisyButton menuButton &&
                menuButton.Flyout is MenuFlyout flyout)
            {
                foreach (var item in flyout.Items)
                {
                    if (item is MenuFlyoutItem menuItem)
                    {
                        menuItem.Command = null;
                        menuItem.CommandParameter = null;
                    }
                }
            }
        }

        private ICommand? ResolveMenuCommand(MenuFlyoutItem menuItem)
        {
            if (menuItem is not DaisyMenuFlyoutItem daisyItem)
                return null;

            return daisyItem.LocalizationKey switch
            {
                "Kanban_Home_Open" => OpenBoardCommand,
                "Kanban_Home_Rename" => RenameBoardHomeCommand,
                "Kanban_Home_Duplicate" => DuplicateBoardCommand,
                "Kanban_Home_Export" => ExportBoardCommand,
                "Kanban_Home_Delete" => DeleteBoardCommand,
                _ => null
            };
        }

        private void OnBoardSurfaceTapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsMenuButtonSource(e.OriginalSource))
                return;

            if (sender is FrameworkElement element &&
                element.DataContext is FlowBoardMetadata board &&
                OpenBoardCommand?.CanExecute(board) == true)
            {
                OpenBoardCommand.Execute(board);
            }
        }

        private static bool IsMenuButtonSource(object? source)
        {
            var current = source as DependencyObject;
            while (current != null)
            {
                if (current is FrameworkElement element &&
                    string.Equals(element.Name, "PART_BoardMenuButton", StringComparison.Ordinal))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private void OnHomeLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isLocalizationSubscribed)
            {
                FloweryLocalization.CultureChanged += OnLocalizationCultureChanged;
                _isLocalizationSubscribed = true;
            }

            UpdateWelcomeText();
        }

        private void OnHomeUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isLocalizationSubscribed)
            {
                FloweryLocalization.CultureChanged -= OnLocalizationCultureChanged;
                _isLocalizationSubscribed = false;
            }
        }

        private void OnLocalizationCultureChanged(object? sender, string cultureName)
        {
            UpdateWelcomeText();
        }

        private void UpdateWelcomeText()
        {
            var title = WelcomeMessageTitle;
            var subtitle = WelcomeMessageSubtitle;
            WelcomeTitleDisplay = string.IsNullOrWhiteSpace(title) ? string.Empty : title;
            WelcomeSubtitleDisplay = string.IsNullOrWhiteSpace(subtitle) ? string.Empty : subtitle;
            IsWelcomeMessageVisible = ShowWelcomeMessage &&
                                      (!string.IsNullOrWhiteSpace(WelcomeTitleDisplay) ||
                                       !string.IsNullOrWhiteSpace(WelcomeSubtitleDisplay));
        }

    }

    public enum FlowKanbanHomeSortMode
    {
        RecentlyModified = 0,
        NameAscending = 1,
        NameDescending = 2,
        RecentlyModifiedAscending = 3
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// Partial class containing UI-related properties and methods for FlowKanban.
    /// </summary>
    public partial class FlowKanban
    {
        private const int ThemeRefreshDebounceMilliseconds = 120;
        private const double CompactLayoutColumnPadding = 16;
        private const double CompactLayoutColumnSpacing = 8;
        private const int CompactLayoutMinColumnCount = 2;
        // Board header elements for hover behavior
        private Grid? _boardHeaderGrid;
        private Grid? _boardContentHost;
        private TextBlock? _boardTitleText;
        private DaisyButton? _renameBoardButton;
        private DaisyPopover? _boardMenuPopover;
        private DaisyMenu? _boardMenu;
        private DaisyInput? _boardSearchInput;
        private Border? _sidebarBorder;
        private Border? _statusBarBorder;
        private bool _isBoardHeaderPointerOver;
        private bool _isBoardHeaderFocused;
        private long _boardMenuPopoverOpenToken;
        private DispatcherQueueTimer? _statusMessageTimer;
        private DispatcherQueueTimer? _themeRefreshTimer;
        private bool _isLocalizationSubscribed;
        private bool _isColumnReorderActive = true;
        private DaisyResizableColumnsHost? _standardColumnsHost;
        private DaisyResizableColumnsHost? _swimlaneColumnsHost;
        private Border? _compactColumnsHost;
        private DaisySelect? _compactColumnSelect;
        private ContentPresenter? _compactColumnPresenter;
        private int _compactScrollLockCount;
        private bool? _compactLayoutOverride;
        private bool _isColumnWidthDragActive;
        private bool _columnWidthSavePending;
        private bool _isClampingColumnWidth;
        private bool _compactScrollRefreshPending;
        private bool _visualCacheDirty = true;
        private readonly List<FlowKanbanColumn> _cachedColumnControls = new();
        private readonly List<Border> _cachedLaneHeaderBorders = new();
        private readonly List<TextBlock> _cachedLaneHeaderTexts = new();
        private readonly List<FlowKanbanUserManagement> _cachedUserManagementViews = new();
        private readonly Dictionary<(string ColumnId, string? LaneId), FlowKanbanColumn> _columnByKey = new();

        #region Layout DPs
        public static readonly DependencyProperty IsSwimlaneLayoutEnabledProperty =
            DependencyProperty.Register(
                nameof(IsSwimlaneLayoutEnabled),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        public bool IsSwimlaneLayoutEnabled
        {
            get => (bool)GetValue(IsSwimlaneLayoutEnabledProperty);
            private set => SetValue(IsSwimlaneLayoutEnabledProperty, value);
        }

        public static readonly DependencyProperty IsStandardLayoutEnabledProperty =
            DependencyProperty.Register(
                nameof(IsStandardLayoutEnabled),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(true));

        public bool IsStandardLayoutEnabled
        {
            get => (bool)GetValue(IsStandardLayoutEnabledProperty);
            private set => SetValue(IsStandardLayoutEnabledProperty, value);
        }

        public static readonly DependencyProperty IsCompactLayoutEnabledProperty =
            DependencyProperty.Register(
                nameof(IsCompactLayoutEnabled),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnCompactLayoutEnabledChanged));

        public bool IsCompactLayoutEnabled
        {
            get => (bool)GetValue(IsCompactLayoutEnabledProperty);
            private set => SetValue(IsCompactLayoutEnabledProperty, value);
        }

        public static readonly DependencyProperty StandardColumnsSourceProperty =
            DependencyProperty.Register(
                nameof(StandardColumnsSource),
                typeof(IEnumerable<FlowKanbanColumnData>),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public IEnumerable<FlowKanbanColumnData>? StandardColumnsSource
        {
            get => (IEnumerable<FlowKanbanColumnData>?)GetValue(StandardColumnsSourceProperty);
            private set => SetValue(StandardColumnsSourceProperty, value);
        }

        public static readonly DependencyProperty CompactColumnsSourceProperty =
            DependencyProperty.Register(
                nameof(CompactColumnsSource),
                typeof(IEnumerable<FlowKanbanColumnData>),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public IEnumerable<FlowKanbanColumnData>? CompactColumnsSource
        {
            get => (IEnumerable<FlowKanbanColumnData>?)GetValue(CompactColumnsSourceProperty);
            private set => SetValue(CompactColumnsSourceProperty, value);
        }

        public static readonly DependencyProperty CompactSelectedColumnProperty =
            DependencyProperty.Register(
                nameof(CompactSelectedColumn),
                typeof(FlowKanbanColumnData),
                typeof(FlowKanban),
                new PropertyMetadata(null, OnCompactSelectedColumnChanged));

        public FlowKanbanColumnData? CompactSelectedColumn
        {
            get => (FlowKanbanColumnData?)GetValue(CompactSelectedColumnProperty);
            set => SetValue(CompactSelectedColumnProperty, value);
        }

        public static readonly DependencyProperty SwimlaneColumnsSourceProperty =
            DependencyProperty.Register(
                nameof(SwimlaneColumnsSource),
                typeof(IEnumerable<FlowKanbanColumnData>),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public IEnumerable<FlowKanbanColumnData>? SwimlaneColumnsSource
        {
            get => (IEnumerable<FlowKanbanColumnData>?)GetValue(SwimlaneColumnsSourceProperty);
            private set => SetValue(SwimlaneColumnsSourceProperty, value);
        }

        public static readonly DependencyProperty CompactColumnMaxHeightProperty =
            DependencyProperty.Register(
                nameof(CompactColumnMaxHeight),
                typeof(double),
                typeof(FlowKanban),
                new PropertyMetadata(double.PositiveInfinity));

        public double CompactColumnMaxHeight
        {
            get => (double)GetValue(CompactColumnMaxHeightProperty);
            private set => SetValue(CompactColumnMaxHeightProperty, value);
        }

        public static readonly DependencyProperty EnableStaggeredTaskRenderingProperty =
            DependencyProperty.Register(
                nameof(EnableStaggeredTaskRendering),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(GetDefaultStaggeredTaskRendering()));

        public bool EnableStaggeredTaskRendering
        {
            get => (bool)GetValue(EnableStaggeredTaskRenderingProperty);
            set => SetValue(EnableStaggeredTaskRenderingProperty, value);
        }

        private static bool GetDefaultStaggeredTaskRendering()
        {
#if __ANDROID__ || __IOS__
            return true;
#else
            return false;
#endif
        }

        public static readonly DependencyProperty LaneRowsProperty =
            DependencyProperty.Register(
                nameof(LaneRows),
                typeof(ObservableCollection<FlowKanbanLaneRowView>),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ObservableCollection<FlowKanbanLaneRowView> LaneRows
        {
            get
            {
                if (GetValue(LaneRowsProperty) is not ObservableCollection<FlowKanbanLaneRowView> lanes)
                {
                    lanes = new ObservableCollection<FlowKanbanLaneRowView>();
                    SetValue(LaneRowsProperty, lanes);
                }

                return lanes;
            }
            private set => SetValue(LaneRowsProperty, value);
        }

        private static void OnCompactLayoutEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban && e.NewValue is bool isCompact)
            {
                if (isCompact)
                {
                    kanban.SeedCompactColumnMaxHeight();
                    kanban.EndColumnResizeSessionIfNeeded();
                    if (kanban.IsColumnResizeEnabled)
                        kanban.IsColumnResizeEnabled = false;
                }
                kanban.ApplyLayoutMode();
                kanban.UpdateLaneRows();
                kanban.UpdateColumnReorderState();
                kanban.ScheduleCompactColumnSizingUpdate();
                kanban.CompactLayoutChanged?.Invoke(kanban, isCompact);
            }
        }

        private static void OnCompactSelectedColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                kanban.RequestCompactLayoutScrollRefresh();
                kanban.ScheduleCompactColumnSizingUpdate();
            }
        }
        #endregion

        #region Layout Methods
        /// <summary>
        /// Toggles between compact and regular layout modes.
        /// </summary>
        public void ToggleCompactLayout()
        {
            SetCompactLayout(!IsCompactLayoutEnabled);
        }

        /// <summary>
        /// Sets the layout mode to compact or regular.
        /// </summary>
        /// <param name="isCompact">When true, forces compact layout.</param>
        public void SetCompactLayout(bool isCompact)
        {
            _compactLayoutOverride = isCompact;
            UpdateCompactLayoutState();
        }

        /// <summary>
        /// Restores automatic layout selection based on available width.
        /// </summary>
        public void RestoreAutomaticLayout()
        {
            _compactLayoutOverride = null;
            UpdateCompactLayoutState();
        }

        /// <summary>
        /// Sets the compact layout to display a specific column by ID.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <returns>True if the column was found and selected.</returns>
        public bool TrySetCompactColumnById(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
                return false;

            var column = Board.Columns.FirstOrDefault(c => string.Equals(c.Id, columnId, StringComparison.Ordinal));
            if (column == null)
                return false;

            CompactSelectedColumn = column;
            return true;
        }

        /// <summary>
        /// Sets the compact layout to display a specific column by title.
        /// </summary>
        /// <param name="title">The column title.</param>
        /// <returns>True if the column was found and selected.</returns>
        public bool TrySetCompactColumnByTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return false;

            var column = Board.Columns.FirstOrDefault(c => string.Equals(c.Title, title, StringComparison.Ordinal));
            if (column == null)
                return false;

            CompactSelectedColumn = column;
            return true;
        }

        /// <summary>
        /// Clears the compact column selection.
        /// </summary>
        public void ClearCompactColumnSelection()
        {
            CompactSelectedColumn = null;
        }

        private void ApplyCompactLayoutOptions(bool useCompactLayout, string? compactColumnId, string? compactColumnTitle)
        {
            SetCompactLayout(useCompactLayout);
            if (!useCompactLayout)
            {
                CompactSelectedColumn = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(compactColumnId))
            {
                TrySetCompactColumnById(compactColumnId);
                return;
            }

            if (!string.IsNullOrWhiteSpace(compactColumnTitle))
            {
                TrySetCompactColumnByTitle(compactColumnTitle);
            }
        }
        #endregion

        #region Archive Column Visibility
        public static readonly DependencyProperty CanShowArchiveColumnProperty =
            DependencyProperty.Register(
                nameof(CanShowArchiveColumn),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        public bool CanShowArchiveColumn
        {
            get => (bool)GetValue(CanShowArchiveColumnProperty);
            private set => SetValue(CanShowArchiveColumnProperty, value);
        }

        public static readonly DependencyProperty CanHideArchiveColumnProperty =
            DependencyProperty.Register(
                nameof(CanHideArchiveColumn),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        public bool CanHideArchiveColumn
        {
            get => (bool)GetValue(CanHideArchiveColumnProperty);
            private set => SetValue(CanHideArchiveColumnProperty, value);
        }

        public static readonly DependencyProperty HasArchiveColumnToggleProperty =
            DependencyProperty.Register(
                nameof(HasArchiveColumnToggle),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        public bool HasArchiveColumnToggle
        {
            get => (bool)GetValue(HasArchiveColumnToggleProperty);
            private set => SetValue(HasArchiveColumnToggleProperty, value);
        }
        #endregion

        #region Bulk Move Columns
        public static readonly DependencyProperty BulkMoveColumnsProperty =
            DependencyProperty.Register(
                nameof(BulkMoveColumns),
                typeof(ObservableCollection<FlowKanbanColumnData>),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ObservableCollection<FlowKanbanColumnData> BulkMoveColumns
        {
            get
            {
                if (GetValue(BulkMoveColumnsProperty) is not ObservableCollection<FlowKanbanColumnData> columns)
                {
                    columns = new ObservableCollection<FlowKanbanColumnData>();
                    SetValue(BulkMoveColumnsProperty, columns);
                }

                return columns;
            }
            private set => SetValue(BulkMoveColumnsProperty, value);
        }
        #endregion

        #region Sidebar UI
        public static readonly DependencyProperty IsUserManagementButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsUserManagementButtonVisible),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        public bool IsUserManagementButtonVisible
        {
            get => (bool)GetValue(IsUserManagementButtonVisibleProperty);
            private set => SetValue(IsUserManagementButtonVisibleProperty, value);
        }
        #endregion

        #region Keyboard UI
        public static readonly DependencyProperty IsKeyboardHelpVisibleProperty =
            DependencyProperty.Register(
                nameof(IsKeyboardHelpVisible),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(GetDefaultKeyboardHelpVisibility()));

        public bool IsKeyboardHelpVisible
        {
            get => (bool)GetValue(IsKeyboardHelpVisibleProperty);
            set => SetValue(IsKeyboardHelpVisibleProperty, value);
        }

        public static readonly DependencyProperty IsColumnTooltipsEnabledProperty =
            DependencyProperty.Register(
                nameof(IsColumnTooltipsEnabled),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(GetDefaultColumnTooltipsEnabled()));

        public bool IsColumnTooltipsEnabled
        {
            get => (bool)GetValue(IsColumnTooltipsEnabledProperty);
            set => SetValue(IsColumnTooltipsEnabledProperty, value);
        }

        public static readonly DependencyProperty ShowKeyboardAcceleratorTooltipsProperty =
            DependencyProperty.Register(
                nameof(ShowKeyboardAcceleratorTooltips),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnShowKeyboardAcceleratorTooltipsChanged));

        /// <summary>
        /// When true, keyboard accelerator UI hints (e.g. "Ctrl+B") may be shown in tooltips.
        /// Default is false.
        /// </summary>
        public bool ShowKeyboardAcceleratorTooltips
        {
            get => (bool)GetValue(ShowKeyboardAcceleratorTooltipsProperty);
            set => SetValue(ShowKeyboardAcceleratorTooltipsProperty, value);
        }

        private static void OnShowKeyboardAcceleratorTooltipsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                kanban.RegisterKeyboardAccelerators();
            }
        }

        private static bool GetDefaultKeyboardHelpVisibility()
        {
#if __WASM__ || __ANDROID__
            return false;
#else
            return true;
#endif
        }

        private static bool GetDefaultColumnTooltipsEnabled()
        {
#if __WASM__ || __ANDROID__
            return false;
#else
            return true;
#endif
        }
        #endregion

        #region BoardSize
        public static readonly DependencyProperty BoardSizeProperty =
            DependencyProperty.Register(
                nameof(BoardSize),
                typeof(DaisySize),
                typeof(FlowKanban),
                new PropertyMetadata(DaisySize.Medium, OnBoardSizeChanged));

        /// <summary>
        /// The size tier for all controls within the Kanban board.
        /// Does not affect the global FlowerySizeManager state.
        /// </summary>
        public DaisySize BoardSize
        {
            get => (DaisySize)GetValue(BoardSizeProperty);
            set => SetValue(BoardSizeProperty, value);
        }

        private static void OnBoardSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban && e.NewValue is DaisySize newSize)
            {
                kanban.BoardSizeChanged?.Invoke(kanban, newSize);
                kanban.NotifyZoomCommandsChanged();
                kanban.RefreshBoardSizeLabel();
            }
        }
        #endregion

        #region ColumnWidth
        public static readonly DependencyProperty ColumnWidthProperty =
            DependencyProperty.Register(
                nameof(ColumnWidth),
                typeof(double),
                typeof(FlowKanban),
                new PropertyMetadata(DefaultColumnWidth, OnColumnWidthChanged));

        /// <summary>
        /// Shared width for all Kanban columns.
        /// </summary>
        public double ColumnWidth
        {
            get => (double)GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, value);
        }

        public static readonly DependencyProperty MinColumnWidthProperty =
            DependencyProperty.Register(
                nameof(MinColumnWidth),
                typeof(double),
                typeof(FlowKanban),
                new PropertyMetadata(DefaultMinColumnWidth, OnColumnWidthBoundsChanged));

        /// <summary>
        /// Minimum width for Kanban columns.
        /// </summary>
        public double MinColumnWidth
        {
            get => (double)GetValue(MinColumnWidthProperty);
            set => SetValue(MinColumnWidthProperty, value);
        }

        public static readonly DependencyProperty MaxColumnWidthProperty =
            DependencyProperty.Register(
                nameof(MaxColumnWidth),
                typeof(double),
                typeof(FlowKanban),
                new PropertyMetadata(DefaultMaxColumnWidth, OnColumnWidthBoundsChanged));

        /// <summary>
        /// Maximum width for Kanban columns.
        /// </summary>
        public double MaxColumnWidth
        {
            get => (double)GetValue(MaxColumnWidthProperty);
            set => SetValue(MaxColumnWidthProperty, value);
        }

        private static void OnColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban && e.NewValue is double newValue)
            {
                kanban.ApplyColumnWidthChange(newValue);
            }
        }

        private static void OnColumnWidthBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                kanban.ColumnWidth = kanban.ClampColumnWidth(kanban.ColumnWidth);
            }
        }

        private void ApplyColumnWidthChange(double value)
        {
            if (_isClampingColumnWidth)
                return;

            var clamped = ClampColumnWidth(value);
            if (Math.Abs(clamped - value) > 0.01)
            {
                _isClampingColumnWidth = true;
                SetValue(ColumnWidthProperty, clamped);
                _isClampingColumnWidth = false;
                return;
            }

            ColumnWidthChanged?.Invoke(this, clamped);

            if (_isColumnWidthDragActive)
            {
                _columnWidthSavePending = true;
            }
            else
            {
                TrySaveSettings(out _);
            }
        }

        private double ClampColumnWidth(double value)
        {
            var min = MinColumnWidth;
            var max = MaxColumnWidth;
            if (min > max)
            {
                (min, max) = (max, min);
            }

            return Math.Clamp(value, min, max);
        }
        #endregion

        #region ColumnResize
        public static readonly DependencyProperty IsColumnResizeEnabledProperty =
            DependencyProperty.Register(
                nameof(IsColumnResizeEnabled),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnColumnResizeEnabledChanged));

        public bool IsColumnResizeEnabled
        {
            get => (bool)GetValue(IsColumnResizeEnabledProperty);
            set => SetValue(IsColumnResizeEnabledProperty, value);
        }

        private static void OnColumnResizeEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban && e.NewValue is bool isEnabled)
            {
                if (!isEnabled)
                {
                    kanban.TrySaveSettings(out _);
                }
            }
        }
        #endregion


        #region File Picker
        public static readonly DependencyProperty IsFilePickerAvailableProperty =
            DependencyProperty.Register(
                nameof(IsFilePickerAvailable),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        public bool IsFilePickerAvailable
        {
            get => (bool)GetValue(IsFilePickerAvailableProperty);
            private set => SetValue(IsFilePickerAvailableProperty, value);
        }
        #endregion

        #region BoardSizeLabel
        public static readonly DependencyProperty BoardSizeLabelProperty =
            DependencyProperty.Register(
                nameof(BoardSizeLabel),
                typeof(string),
                typeof(FlowKanban),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets the localized label for the current BoardSize.
        /// </summary>
        public string BoardSizeLabel
        {
            get => (string)GetValue(BoardSizeLabelProperty);
            private set => SetValue(BoardSizeLabelProperty, value);
        }

        private void RefreshBoardSizeLabel()
        {
            BoardSizeLabel = FloweryLocalization.GetStringInternal($"Size_{BoardSize}");
        }
        #endregion

        #region IsStatusBarVisible
        public static readonly DependencyProperty IsStatusBarVisibleProperty =
            DependencyProperty.Register(
                nameof(IsStatusBarVisible),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(true, OnSettingChanged));

        /// <summary>
        /// Controls visibility of the status bar at the bottom.
        /// </summary>
        public bool IsStatusBarVisible
        {
            get => (bool)GetValue(IsStatusBarVisibleProperty);
            set => SetValue(IsStatusBarVisibleProperty, value);
        }
        #endregion

        #region StatusMessage
        public static readonly DependencyProperty StatusMessageProperty =
            DependencyProperty.Register(
                nameof(StatusMessage),
                typeof(string),
                typeof(FlowKanban),
                new PropertyMetadata(string.Empty));

        public string StatusMessage
        {
            get => (string)GetValue(StatusMessageProperty);
            private set => SetValue(StatusMessageProperty, value);
        }

        public static readonly DependencyProperty HasStatusMessageProperty =
            DependencyProperty.Register(
                nameof(HasStatusMessage),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        public bool HasStatusMessage
        {
            get => (bool)GetValue(HasStatusMessageProperty);
            private set => SetValue(HasStatusMessageProperty, value);
        }

        internal void ShowStatusMessage(string message, TimeSpan? duration = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ClearStatusMessage();
                return;
            }

            StatusMessage = message.Trim();
            HasStatusMessage = true;

            if (duration.HasValue)
            {
                StartStatusMessageTimer(duration.Value);
            }
        }

        private void ClearStatusMessage()
        {
            StatusMessage = string.Empty;
            HasStatusMessage = false;
            _statusMessageTimer?.Stop();
        }

        private void StartStatusMessageTimer(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero || DispatcherQueue == null)
                return;

            _statusMessageTimer ??= DispatcherQueue.CreateTimer();
            _statusMessageTimer.Stop();
            _statusMessageTimer.Interval = duration;
            _statusMessageTimer.Tick -= OnStatusMessageTimerTick;
            _statusMessageTimer.Tick += OnStatusMessageTimerTick;
            _statusMessageTimer.Start();
        }

        private void OnStatusMessageTimerTick(DispatcherQueueTimer sender, object args)
        {
            sender.Stop();
            ClearStatusMessage();
        }
        #endregion

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            // Refresh background to pick up new theme colors
            Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            ApplySidebarAndStatusBarTheme();
            ApplySwimlaneLaneHeaderTheme();
            ScheduleThemeRefresh();
        }

        private void OnKanbanLoaded(object sender, RoutedEventArgs e)
        {
            DaisyResourceLookup.EnsureTokens();
            ApplyDefaultUserProvider();
            AttachUserProvider(UserProvider);
            _ = EnsureGlobalAdminAsync();
            _ = RefreshAdminStateAsync();
            _ = EnsureUserSettingsLoadedAsync(forceReload: false);
            RefreshBoards();
            if (CurrentView == FlowKanbanView.Board && Boards.Count == 0)
            {
                CurrentView = FlowKanbanView.Home;
            }
            FindBoardHeaderElements();
            AttachBoardTracking(Board);
            InvalidateVisualCache();
            RefreshBoardSizeLabel();
            ApplySearchFilter();
            AttachBoardLayoutTracking();
            UpdateLaneGroupingState();
            UpdateColumnReorderState();
            AttachColumnResizeHandlers();
            AttachKeyboardSupport();
            RefreshDoneAgingState();
            if (!_isLocalizationSubscribed)
            {
                FloweryLocalization.CultureChanged += OnLocalizationCultureChanged;
                _isLocalizationSubscribed = true;
            }
        }

        private void OnKanbanUnloaded(object sender, RoutedEventArgs e)
        {
            if (CurrentView == FlowKanbanView.Home || CurrentView == FlowKanbanView.UserManagement)
            {
                ClearLastBoardId();
            }
            DetachColumnDragHandlers();
            DetachColumnResizeHandlers();
            DetachBoardLayoutTracking();
            if (_boardHeaderGrid != null)
            {
                _boardHeaderGrid.PointerEntered -= OnBoardHeaderPointerEntered;
                _boardHeaderGrid.PointerExited -= OnBoardHeaderPointerExited;
                _boardHeaderGrid.GotFocus -= OnBoardHeaderGotFocus;
                _boardHeaderGrid.LostFocus -= OnBoardHeaderLostFocus;
                _boardHeaderGrid.SizeChanged -= OnBoardHeaderSizeChanged;
            }
            if (_boardMenuPopover != null && _boardMenuPopoverOpenToken != 0)
            {
                _boardMenuPopover.UnregisterPropertyChangedCallback(DaisyPopover.IsOpenProperty, _boardMenuPopoverOpenToken);
                _boardMenuPopoverOpenToken = 0;
            }
            DetachBoardMenuEvents();
            _boardTitleText = null;
            _boardMenuPopover = null;
            _boardMenu = null;
            _isBoardHeaderPointerOver = false;
            _isBoardHeaderFocused = false;
            HideColumnDropIndicator();
            DetachKeyboardSupport();
            DetachUserProvider();
            StopDoneAgingTimer();
            DisposeDoneAgingTimer();
            _themeRefreshTimer?.Stop();
            if (_isLocalizationSubscribed)
            {
                FloweryLocalization.CultureChanged -= OnLocalizationCultureChanged;
                _isLocalizationSubscribed = false;
            }
            InvalidateVisualCache();
        }

        private void OnLocalizationCultureChanged(object? sender, string cultureName)
        {
            RefreshLocalizationState();
        }

        private void RefreshLocalizationState()
        {
            NotifyLaneGroupingChanged();
            RefreshTaskCardLocalization();
            RefreshBoardSizeLabel();
        }

        private void RefreshTaskCardLocalization()
        {
            EnsureVisualCache();
            foreach (var column in _cachedColumnControls)
            {
                if (!column.IsLoaded || column.Visibility != Visibility.Visible)
                    continue;

                foreach (var card in column.GetRealizedTaskCards())
                {
                    card.RefreshLocalization();
                }
            }
        }

        private void InvalidateVisualCache()
        {
            _visualCacheDirty = true;
            _cachedColumnControls.Clear();
            _cachedLaneHeaderBorders.Clear();
            _cachedLaneHeaderTexts.Clear();
            _cachedUserManagementViews.Clear();
            _columnByKey.Clear();
        }

        private void EnsureVisualCache()
        {
            if (!_visualCacheDirty)
                return;

            if (!IsLoaded)
                return;

            _cachedLaneHeaderBorders.Clear();
            _cachedLaneHeaderTexts.Clear();
            _cachedUserManagementViews.Clear();

            foreach (var element in EnumerateVisualTree(this))
            {
                if (element is Border border && string.Equals(border.Name, "PART_LaneHeaderBorder", StringComparison.Ordinal))
                {
                    _cachedLaneHeaderBorders.Add(border);
                    continue;
                }

                if (element is TextBlock textBlock && string.Equals(textBlock.Name, "PART_LaneHeaderText", StringComparison.Ordinal))
                {
                    _cachedLaneHeaderTexts.Add(textBlock);
                    continue;
                }

                if (element is FlowKanbanUserManagement view)
                {
                    _cachedUserManagementViews.Add(view);
                }
            }

            _visualCacheDirty = false;
        }

        internal void RegisterColumn(FlowKanbanColumn column)
        {
            if (column.ColumnData == null)
                return;

            if (!ReferenceEquals(column.ParentKanban, this))
                return;

            if (!_cachedColumnControls.Contains(column))
                _cachedColumnControls.Add(column);

            var key = (column.ColumnData.Id, NormalizeLaneId(column.LaneFilterId));
            _columnByKey[key] = column;
            _visualCacheDirty = false;
        }

        internal void UnregisterColumn(FlowKanbanColumn column)
        {
            _cachedColumnControls.Remove(column);

            if (column.ColumnData == null)
                return;

            var key = (column.ColumnData.Id, NormalizeLaneId(column.LaneFilterId));
            if (_columnByKey.TryGetValue(key, out var existing) && ReferenceEquals(existing, column))
            {
                _columnByKey.Remove(key);
            }
        }

        private void RegisterLaneHeaderBorder(Border border)
        {
            if (!_cachedLaneHeaderBorders.Contains(border))
                _cachedLaneHeaderBorders.Add(border);

            _visualCacheDirty = false;
        }

        private void UnregisterLaneHeaderBorder(Border border)
        {
            _cachedLaneHeaderBorders.Remove(border);
        }

        private void RegisterLaneHeaderText(TextBlock textBlock)
        {
            if (!_cachedLaneHeaderTexts.Contains(textBlock))
                _cachedLaneHeaderTexts.Add(textBlock);

            _visualCacheDirty = false;
        }

        private void UnregisterLaneHeaderText(TextBlock textBlock)
        {
            _cachedLaneHeaderTexts.Remove(textBlock);
        }

        private void RegisterUserManagementView(FlowKanbanUserManagement view)
        {
            if (!_cachedUserManagementViews.Contains(view))
                _cachedUserManagementViews.Add(view);

            _visualCacheDirty = false;
        }

        private void UnregisterUserManagementView(FlowKanbanUserManagement view)
        {
            _cachedUserManagementViews.Remove(view);
        }

        private void OnLaneHeaderBorderLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                RegisterLaneHeaderBorder(border);
            }
        }

        private void OnLaneHeaderBorderUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                UnregisterLaneHeaderBorder(border);
            }
        }

        private void OnLaneHeaderTextLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                RegisterLaneHeaderText(textBlock);
            }
        }

        private void OnLaneHeaderTextUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                UnregisterLaneHeaderText(textBlock);
            }
        }

        private void OnUserManagementViewLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FlowKanbanUserManagement view)
            {
                RegisterUserManagementView(view);
            }
        }

        private void OnUserManagementViewUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FlowKanbanUserManagement view)
            {
                UnregisterUserManagementView(view);
            }
        }

        private void FindBoardHeaderElements()
        {
            if (_boardHeaderGrid != null)
            {
                _boardHeaderGrid.PointerEntered -= OnBoardHeaderPointerEntered;
                _boardHeaderGrid.PointerExited -= OnBoardHeaderPointerExited;
                _boardHeaderGrid.GotFocus -= OnBoardHeaderGotFocus;
                _boardHeaderGrid.LostFocus -= OnBoardHeaderLostFocus;
                _boardHeaderGrid.SizeChanged -= OnBoardHeaderSizeChanged;
            }

            // Find the board header grid
            _boardHeaderGrid = FindChild<Grid>(this, g => g.Name == "PART_BoardHeader");

            // Find the rename button
            _renameBoardButton = FindChild<DaisyButton>(this, btn => btn.Name == "PART_RenameBoardBtn");

            var searchRoot = _boardHeaderGrid != null ? (DependencyObject)_boardHeaderGrid : this;
            _boardTitleText = FindChild<TextBlock>(searchRoot, text => text.Name == "PART_BoardTitle");
            _boardMenuPopover = FindChild<DaisyPopover>(searchRoot, popover => popover.Name == "PART_BoardMenuPopover");
            _boardMenu = FindChild<DaisyMenu>(searchRoot, menu => menu.Name == "PART_BoardMenu");
            _boardSearchInput = FindChild<DaisyInput>(searchRoot, input => input.Name == "PART_SearchInput");
            _sidebarBorder = FindChild<Border>(this, b => b.Name == "PART_SidebarBorder");
            _statusBarBorder = FindChild<Border>(this, b => b.Name == "PART_StatusBarBorder");
            ApplySidebarAndStatusBarTheme();
            UpdateBoardMenuPopoverOffset();
            AttachBoardMenuEvents();
            if (_boardMenuPopover != null)
            {
                _boardMenuPopoverOpenToken = _boardMenuPopover.RegisterPropertyChangedCallback(
                    DaisyPopover.IsOpenProperty,
                    OnBoardMenuPopoverOpenChanged);
            }

            // Wire up hover events on board header grid
            if (_boardHeaderGrid != null)
            {
                _boardHeaderGrid.PointerEntered += OnBoardHeaderPointerEntered;
                _boardHeaderGrid.PointerExited += OnBoardHeaderPointerExited;
                _boardHeaderGrid.GotFocus += OnBoardHeaderGotFocus;
                _boardHeaderGrid.LostFocus += OnBoardHeaderLostFocus;
                _boardHeaderGrid.SizeChanged += OnBoardHeaderSizeChanged;
            }
        }
        private void OnBoardHeaderSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBoardMenuPopoverOffset();
        }

        private void UpdateBoardMenuPopoverOffset()
        {
            if (_boardMenuPopover == null || _boardTitleText == null)
                return;

            if (!_boardMenuPopover.IsLoaded || !_boardTitleText.IsLoaded)
                return;

            var titleTop = _boardTitleText.TransformToVisual(_boardMenuPopover)
                .TransformPoint(new Windows.Foundation.Point(0, 0)).Y;

            if (double.IsNaN(titleTop) || double.IsInfinity(titleTop))
                return;

            var triggerHeight = _boardMenuPopover.ActualHeight;
            if (triggerHeight <= 0 || double.IsNaN(triggerHeight) || double.IsInfinity(triggerHeight))
                return;

            _boardMenuPopover.VerticalOffset = titleTop - triggerHeight;
        }

        private void OnBoardHeaderPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isBoardHeaderPointerOver = true;
            ShowBoardHeaderButtons();
        }

        private void OnBoardHeaderPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isBoardHeaderPointerOver = false;
            if (!_isBoardHeaderFocused)
            {
                HideBoardHeaderButtons();
            }
        }

        private void OnBoardHeaderGotFocus(object sender, RoutedEventArgs e)
        {
            _isBoardHeaderFocused = true;
            ShowBoardHeaderButtons();
        }

        private void OnBoardHeaderLostFocus(object sender, RoutedEventArgs e)
        {
            _isBoardHeaderFocused = false;
            if (!_isBoardHeaderPointerOver)
            {
                HideBoardHeaderButtons();
            }
        }

        private void OnBoardMenuPopoverOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is not DaisyPopover popover)
                return;

            if (popover.IsOpen)
            {
                AttachBoardMenuEvents();
                ShowBoardHeaderButtons();
                return;
            }

            if (!_isBoardHeaderPointerOver && !_isBoardHeaderFocused)
            {
                HideBoardHeaderButtons();
            }
        }

        private void AttachBoardMenuEvents()
        {
            DetachBoardMenuEvents();

            EnsureBoardMenu();

            if (_boardMenu != null)
            {
                _boardMenu.ItemInvoking += OnBoardMenuItemInvoking;
                _boardMenu.ItemInvoked += OnBoardMenuItemInvoked;
            }
        }

        private void DetachBoardMenuEvents()
        {
            if (_boardMenu != null)
            {
                _boardMenu.ItemInvoking -= OnBoardMenuItemInvoking;
                _boardMenu.ItemInvoked -= OnBoardMenuItemInvoked;
            }
        }

        private void EnsureBoardMenu()
        {
            if (_boardMenuPopover == null)
                return;

            _boardMenu ??= _boardMenuPopover.PopoverContent as DaisyMenu;
            _boardMenu ??= FindChild<DaisyMenu>(_boardMenuPopover, menu => menu.Name == "PART_BoardMenu");
        }

        private void OnBoardMenuItemInvoking(object? sender, DaisyMenuItem item)
        {
            if (_boardMenuPopover != null)
            {
                _boardMenuPopover.IsOpen = false;
            }
        }

        private void OnBoardMenuItemInvoked(object? sender, DaisyMenuItem item)
        {
            if (string.Equals(item.Name, "PART_BoardMenuResizeItem", StringComparison.Ordinal))
            {
                IsColumnResizeEnabled = !IsColumnResizeEnabled;
            }
            item.IsActive = false;
        }

        private void AttachBoardLayoutTracking()
        {
            DetachBoardLayoutTracking();

            _boardContentHost = FindChild<Grid>(this, g => g.Name == "PART_BoardContentHost");
            if (_boardContentHost != null)
            {
                _boardContentHost.SizeChanged += OnBoardContentSizeChanged;
                if (_isColumnReorderActive)
                {
                    AttachBoardDragHandlers();
                }
            }

            _compactColumnsHost = FindChild<Border>(this, b => b.Name == "PART_CompactColumnsHost");
            _compactColumnSelect = FindChild<DaisySelect>(this, s => s.Name == "PART_CompactColumnSelect");
            _compactColumnPresenter = FindChild<ContentPresenter>(this, p => p.Name == "PART_CompactColumnPresenter");
            if (_compactColumnsHost != null)
            {
                _compactColumnsHost.SizeChanged += OnCompactColumnsHostSizeChanged;
            }

            UpdateCompactLayoutState();
            ApplyCompactColumnsScrollLock();
        }

        private void DetachBoardLayoutTracking()
        {
            if (_boardContentHost != null)
            {
                _boardContentHost.SizeChanged -= OnBoardContentSizeChanged;
                DetachBoardDragHandlers();
                _boardContentHost = null;
            }

            if (_compactColumnsHost != null)
            {
                _compactColumnsHost.SizeChanged -= OnCompactColumnsHostSizeChanged;
            }
            _compactColumnsHost = null;
            _compactColumnSelect = null;
            _compactColumnPresenter = null;
        }

        private void OnBoardContentSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCompactLayoutState();
            ScheduleCompactColumnSizingUpdate();
        }

        private void OnCompactColumnsHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleCompactColumnSizingUpdate();
        }

        private void AttachColumnResizeHandlers()
        {
            DetachColumnResizeHandlers();

            _standardColumnsHost = FindChild<DaisyResizableColumnsHost>(this, host => host.Name == "PART_ColumnsItemsControl");
            _swimlaneColumnsHost = FindChild<DaisyResizableColumnsHost>(this, host => host.Name == "PART_SwimlaneColumnsItemsControl");
            AttachColumnResizeHandlers(_standardColumnsHost);
            AttachColumnResizeHandlers(_swimlaneColumnsHost);
        }

        private void AttachColumnResizeHandlers(DaisyResizableColumnsHost? host)
        {
            if (host == null)
                return;

            host.ResizeDragStarted += OnColumnResizeDragStarted;
            host.ResizeDragCompleted += OnColumnResizeDragCompleted;
        }

        private void DetachColumnResizeHandlers()
        {
            DetachColumnResizeHandlers(_standardColumnsHost);
            DetachColumnResizeHandlers(_swimlaneColumnsHost);

            _standardColumnsHost = null;
            _swimlaneColumnsHost = null;
        }

        private void DetachColumnResizeHandlers(DaisyResizableColumnsHost? host)
        {
            if (host == null)
                return;

            host.ResizeDragStarted -= OnColumnResizeDragStarted;
            host.ResizeDragCompleted -= OnColumnResizeDragCompleted;
        }

        private void OnColumnResizeDragStarted(object? sender, EventArgs e)
        {
            _isColumnWidthDragActive = true;
            _columnWidthSavePending = false;
        }

        private void OnColumnResizeDragCompleted(object? sender, EventArgs e)
        {
            _isColumnWidthDragActive = false;

            if (_columnWidthSavePending)
            {
                TrySaveSettings(out _);
                _columnWidthSavePending = false;
            }
        }

        private void EndColumnResizeSessionIfNeeded()
        {
            if (!_isColumnWidthDragActive && !_columnWidthSavePending)
                return;

            _isColumnWidthDragActive = false;
            if (_columnWidthSavePending)
            {
                TrySaveSettings(out _);
                _columnWidthSavePending = false;
            }
        }

        private void UpdateCompactLayoutState()
        {
            if (!IsBoardViewActive)
            {
                SetCompactLayoutEnabled(false);
                return;
            }

            if (_compactLayoutOverride.HasValue)
            {
                SetCompactLayoutEnabled(_compactLayoutOverride.Value);
                return;
            }

            var availableWidth = GetDeterministicAvailableWidth();
            if (availableWidth <= 0 || double.IsNaN(availableWidth))
            {
                if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
                {
                    SetCompactLayoutEnabled(true);
                }
                return;
            }

            var threshold = GetCompactLayoutThresholdWidth();
            SetCompactLayoutEnabled(availableWidth < threshold);
        }

        internal void SetCompactColumnsScrollLock(bool isLocked)
        {
            if (isLocked)
            {
                _compactScrollLockCount++;
            }
            else if (_compactScrollLockCount > 0)
            {
                _compactScrollLockCount--;
            }

            ApplyCompactColumnsScrollLock();
        }

        private void ApplyCompactColumnsScrollLock()
        {
            var column = TryGetCompactSizingColumn();
            if (column == null)
                return;

            var scrollViewer = FindChild<ScrollViewer>(column);
            if (scrollViewer == null)
                return;

            var locked = _compactScrollLockCount > 0;
            scrollViewer.VerticalScrollMode = locked ? ScrollMode.Disabled : ScrollMode.Auto;
            scrollViewer.VerticalScrollBarVisibility = locked ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        }

        private double GetDeterministicAvailableWidth()
        {
            if (_boardContentHost != null && _boardContentHost.ActualWidth > 0 && !double.IsNaN(_boardContentHost.ActualWidth))
                return _boardContentHost.ActualWidth;

            if (XamlRoot?.Size.Width > 0 && !double.IsNaN(XamlRoot.Size.Width))
                return XamlRoot.Size.Width;

            var mainWindow = FlowerySizeManager.MainWindow;
            if (mainWindow != null)
            {
                try
                {
                    var bounds = mainWindow.Bounds;
                    if (bounds.Width > 0 && !double.IsNaN(bounds.Width))
                        return bounds.Width;
                }
                catch
                {
                    // Ignore access failures.
                }

                try
                {
                    var appWindow = mainWindow.AppWindow;
                    if (appWindow != null && appWindow.Size.Width > 0 && !double.IsNaN(appWindow.Size.Width))
                    {
                        var scale = mainWindow.Content?.XamlRoot?.RasterizationScale ?? 1.0;
                        return appWindow.Size.Width / (scale <= 0 ? 1.0 : scale);
                    }
                }
                catch
                {
                    // Ignore access failures.
                }
            }

            try
            {
                var currentWindow = Window.Current;
                if (currentWindow != null)
                {
                    var bounds = currentWindow.Bounds;
                    if (bounds.Width > 0 && !double.IsNaN(bounds.Width))
                        return bounds.Width;
                }
            }
            catch
            {
                // Ignore access failures.
            }

            return 0;
        }

        private double GetDeterministicAvailableHeight()
        {
            if (_compactColumnsHost != null && _compactColumnsHost.ActualHeight > 0 && !double.IsNaN(_compactColumnsHost.ActualHeight))
                return _compactColumnsHost.ActualHeight;

            if (_boardContentHost != null && _boardContentHost.ActualHeight > 0 && !double.IsNaN(_boardContentHost.ActualHeight))
                return _boardContentHost.ActualHeight;

            if (XamlRoot?.Size.Height > 0 && !double.IsNaN(XamlRoot.Size.Height))
                return XamlRoot.Size.Height;

            var mainWindow = FlowerySizeManager.MainWindow;
            if (mainWindow != null)
            {
                try
                {
                    var bounds = mainWindow.Bounds;
                    if (bounds.Height > 0 && !double.IsNaN(bounds.Height))
                        return bounds.Height;
                }
                catch
                {
                    // Ignore access failures.
                }

                try
                {
                    var appWindow = mainWindow.AppWindow;
                    if (appWindow != null && appWindow.Size.Height > 0 && !double.IsNaN(appWindow.Size.Height))
                    {
                        var scale = mainWindow.Content?.XamlRoot?.RasterizationScale ?? 1.0;
                        return appWindow.Size.Height / (scale <= 0 ? 1.0 : scale);
                    }
                }
                catch
                {
                    // Ignore access failures.
                }
            }

            try
            {
                var currentWindow = Window.Current;
                if (currentWindow != null)
                {
                    var bounds = currentWindow.Bounds;
                    if (bounds.Height > 0 && !double.IsNaN(bounds.Height))
                        return bounds.Height;
                }
            }
            catch
            {
                // Ignore access failures.
            }

            return 0;
        }

        private void SeedCompactColumnMaxHeight()
        {
            var availableHeight = GetCompactAvailableHeight();
            if (availableHeight <= 0 || double.IsNaN(availableHeight))
                return;
            CompactColumnMaxHeight = Math.Max(0, availableHeight);
        }

        private void RequestCompactLayoutScrollRefresh()
        {
            if (_compactColumnsHost == null)
                return;

            if (_compactScrollRefreshPending)
                return;

            _compactScrollRefreshPending = true;

            if (DispatcherQueue == null)
            {
                _compactScrollRefreshPending = false;
                _compactColumnPresenter?.InvalidateMeasure();
                _compactColumnsHost.InvalidateMeasure();
                _compactColumnsHost.UpdateLayout();
                UpdateCompactColumnSizing();
                return;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                _compactScrollRefreshPending = false;
                _compactColumnPresenter?.InvalidateMeasure();
                _compactColumnsHost.InvalidateMeasure();
                _compactColumnsHost.UpdateLayout();
                UpdateCompactColumnSizing();
            });
        }

        private bool _compactSizingUpdatePending;

        private void ScheduleCompactColumnSizingUpdate()
        {
            if (_compactSizingUpdatePending)
                return;

            _compactSizingUpdatePending = true;

            if (DispatcherQueue == null)
            {
                _compactSizingUpdatePending = false;
                UpdateCompactColumnSizing();
                return;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                _compactSizingUpdatePending = false;
                UpdateCompactColumnSizing();
            });
        }

        internal void RefreshLayoutAfterSettingsChange()
        {
            InvalidateVisualCache();
            ScheduleCompactColumnSizingUpdate();

            if (IsCompactLayoutEnabled)
            {
                RequestCompactLayoutScrollRefresh();
            }
            else
            {
                _standardColumnsHost?.InvalidateMeasure();
                _swimlaneColumnsHost?.InvalidateMeasure();
                _boardContentHost?.InvalidateMeasure();
                _boardContentHost?.UpdateLayout();
            }
        }

        private void ClampCompactManualCardCount()
        {
            var clamped = Math.Clamp(CompactManualCardCount, 1, 20);
            if (clamped != CompactManualCardCount)
            {
                SetValue(CompactManualCardCountProperty, clamped);
            }
        }

        private void UpdateCompactColumnSizing()
        {
            if (!IsCompactLayoutEnabled || _compactColumnsHost == null)
            {
                CompactColumnMaxHeight = double.PositiveInfinity;
                return;
            }

            var availableHeight = GetCompactAvailableHeight();
            if (availableHeight <= 0 || double.IsNaN(availableHeight))
                return;

            var column = TryGetCompactSizingColumn();
            if (column != null)
            {
                availableHeight -= column.Margin.Top + column.Margin.Bottom;
            }

            if (availableHeight <= 0 || double.IsNaN(availableHeight))
                return;

            var maxHeight = availableHeight;
            if (CompactColumnSizingMode == FlowKanbanCompactColumnSizingMode.Manual && column != null)
            {
                var count = Math.Clamp(CompactManualCardCount, 1, 20);
                var spacing = DaisyResourceLookup.GetKanbanCardSpacing(column.ColumnSize);
                var baseHeight = EstimateCompactColumnBaseHeight(column);
                var cardHeight = EstimateCompactCardHeight(column, availableHeight, baseHeight, spacing, count);
                if (cardHeight > 0)
                {
                    var cardsHeight = (cardHeight * count) + (spacing * Math.Max(0, count - 1));
                    var desiredHeight = baseHeight + cardsHeight;
                    maxHeight = Math.Min(availableHeight, desiredHeight);
                }
            }

            CompactColumnMaxHeight = Math.Max(0, maxHeight);
        }

        private double GetCompactAvailableHeight()
        {
            var availableHeight = _compactColumnsHost?.ActualHeight ?? 0;
            if (availableHeight <= 0 || double.IsNaN(availableHeight))
                availableHeight = GetDeterministicAvailableHeight();

            if (availableHeight <= 0 || double.IsNaN(availableHeight))
                return 0;

            var padding = _compactColumnsHost?.Padding ?? new Thickness(CompactLayoutColumnPadding);
            availableHeight -= padding.Top + padding.Bottom;
            if (availableHeight <= 0 || double.IsNaN(availableHeight))
                return 0;

            if (_compactColumnSelect != null && _compactColumnSelect.ActualHeight > 0 && !double.IsNaN(_compactColumnSelect.ActualHeight))
            {
                var margin = _compactColumnSelect.Margin;
                availableHeight -= _compactColumnSelect.ActualHeight + margin.Top + margin.Bottom;
            }

            return availableHeight;
        }

        private FlowKanbanColumn? TryGetCompactSizingColumn()
        {
            var compactColumn = TryGetCompactSizingColumnFromHost();
            if (compactColumn != null)
                return compactColumn;

            EnsureVisualCache();
            foreach (var column in _cachedColumnControls)
            {
                if (!column.IsLoaded || column.Visibility != Visibility.Visible)
                    continue;

                if (!ReferenceEquals(column.ParentKanban, this))
                    continue;

                return column;
            }

            return null;
        }

        private FlowKanbanColumn? TryGetCompactSizingColumnFromHost()
        {
            if (_compactColumnPresenter != null)
            {
                var column = FindChild<FlowKanbanColumn>(_compactColumnPresenter);
                if (column != null && column.IsLoaded && column.Visibility == Visibility.Visible)
                    return column;
            }

            if (_compactColumnsHost != null)
            {
                var column = FindChild<FlowKanbanColumn>(_compactColumnsHost);
                if (column != null && column.IsLoaded && column.Visibility == Visibility.Visible)
                    return column;
            }

            return null;
        }

        private double EstimateCompactColumnBaseHeight(FlowKanbanColumn column)
        {
            var height = column.Padding.Top + column.Padding.Bottom;

            var header = FindChild<Grid>(column, g => g.Name == "PART_ColumnHeaderGrid");
            if (header != null && header.Visibility == Visibility.Visible)
            {
                height += header.ActualHeight + header.Margin.Top + header.Margin.Bottom;
            }

            var addTop = FindChild<FlowKanbanAddCard>(column, c => c.Name == "PART_AddCardTop");
            if (addTop != null && addTop.Visibility == Visibility.Visible)
            {
                height += addTop.ActualHeight + addTop.Margin.Top + addTop.Margin.Bottom;
            }

            var addBottom = FindChild<FlowKanbanAddCard>(column, c => c.Name == "PART_AddCardBottom");
            if (addBottom != null && addBottom.Visibility == Visibility.Visible)
            {
                height += addBottom.ActualHeight + addBottom.Margin.Top + addBottom.Margin.Bottom;
            }

            return height;
        }

        private double EstimateCompactCardHeight(
            FlowKanbanColumn column,
            double availableHeight,
            double baseHeight,
            double spacing,
            int count)
        {
            var heights = new List<double>();
            foreach (var card in column.GetRealizedTaskCards())
            {
                if (card.ActualHeight > 0 && !double.IsNaN(card.ActualHeight))
                    heights.Add(card.ActualHeight);

                if (heights.Count >= 6)
                    break;
            }

            if (heights.Count > 0)
                return heights.Average();

            var usable = availableHeight - baseHeight - (spacing * Math.Max(0, count - 1));
            if (usable <= 0)
                return 0;

            return usable / count;
        }

        private void SetCompactLayoutEnabled(bool value)
        {
            if (IsCompactLayoutEnabled == value)
                return;

            SetValue(IsCompactLayoutEnabledProperty, value);
        }

        private static double GetCompactLayoutThresholdWidth()
        {
            return (DefaultColumnWidth * CompactLayoutMinColumnCount)
                   + CompactLayoutColumnSpacing
                   + (CompactLayoutColumnPadding * 2);
        }

        private void ApplyLayoutMode()
        {
            if (IsCompactLayoutEnabled)
            {
                IsSwimlaneLayoutEnabled = false;
                IsStandardLayoutEnabled = false;
            }
            else
            {
                var enabled = IsLaneGroupingEnabled;
                IsSwimlaneLayoutEnabled = enabled;
                IsStandardLayoutEnabled = !enabled;
            }

            UpdateLayoutVisibilityState();
        }

        private void UpdateLayoutItemsSources()
        {
            var columns = _trackedBoard?.Columns;
            if (columns == null)
            {
                StandardColumnsSource = null;
                CompactColumnsSource = null;
                SwimlaneColumnsSource = null;
                CompactSelectedColumn = null;
                return;
            }

            StandardColumnsSource = IsStandardLayoutVisible ? columns : null;
            SwimlaneColumnsSource = IsSwimlaneLayoutVisible ? columns : null;

            var compactColumns = new List<FlowKanbanColumnData>();
            foreach (var column in columns)
            {
                if (column.IsArchiveColumnVisible)
                {
                    compactColumns.Add(column);
                }
            }

            CompactColumnsSource = IsCompactLayoutVisible ? compactColumns : null;
            EnsureCompactSelectedColumn(compactColumns);
        }

        private void EnsureCompactSelectedColumn(IReadOnlyList<FlowKanbanColumnData> columns)
        {
            if (columns.Count == 0)
            {
                if (CompactSelectedColumn != null)
                {
                    CompactSelectedColumn = null;
                }
                return;
            }

            var current = CompactSelectedColumn;
            if (current != null)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    if (ReferenceEquals(columns[i], current))
                        return;
                }
            }

            CompactSelectedColumn = columns[0];
        }

        private void UpdateColumnReorderState()
        {
            var shouldEnable = !IsCompactLayoutEnabled;
            if (_isColumnReorderActive == shouldEnable)
                return;

            _isColumnReorderActive = shouldEnable;
            if (shouldEnable)
            {
                AttachColumnDragHandlers();
                AttachBoardDragHandlers();
            }
            else
            {
                DetachColumnDragHandlers();
                DetachBoardDragHandlers();
                HideColumnDropIndicator();
            }
        }

        private void UpdateLaneGroupingState()
        {
            ApplyLayoutMode();
            UpdateLaneRows();
        }

        private void UpdateLaneRows()
        {
            var groupingEnabled = IsLaneGroupingEnabled;

            if (_trackedBoard == null || !groupingEnabled || IsCompactLayoutEnabled)
            {
                LaneRows = new ObservableCollection<FlowKanbanLaneRowView>();
                InvalidateVisualCache();
                return;
            }

            var rows = new ObservableCollection<FlowKanbanLaneRowView>();
            var columns = _trackedBoard.Columns;

            if (HasUnassignedTasks(_trackedBoard))
            {
                var unassignedLane = new FlowKanbanLane
                {
                    Id = UnassignedLaneId,
                    Title = FloweryLocalization.GetStringInternal("Kanban_Lanes_Unassigned")
                };
                rows.Add(BuildLaneRow(unassignedLane, columns, UnassignedLaneId));
            }

            foreach (var lane in _trackedBoard.Lanes)
            {
                if (string.IsNullOrWhiteSpace(lane.Id))
                    continue;

                rows.Add(BuildLaneRow(lane, columns, lane.Id));
            }

            LaneRows = rows;
            InvalidateVisualCache();
        }

        private static FlowKanbanLaneRowView BuildLaneRow(
            FlowKanbanLane lane,
            ObservableCollection<FlowKanbanColumnData> columns,
            string? laneId)
        {
            var cells = new ObservableCollection<FlowKanbanLaneCellView>();
            foreach (var column in columns)
            {
                cells.Add(new FlowKanbanLaneCellView(column, laneId));
            }

            return new FlowKanbanLaneRowView(lane, cells);
        }

        private static bool HasUnassignedTasks(FlowKanbanData board)
        {
            var validLaneIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var lane in board.Lanes)
            {
                if (!string.IsNullOrWhiteSpace(lane.Id))
                    validLaneIds.Add(lane.Id);
            }

            foreach (var column in board.Columns)
            {
                foreach (var task in column.Tasks)
                {
                    if (IsUnassignedLaneId(task.LaneId))
                        return true;

                    if (!string.IsNullOrWhiteSpace(task.LaneId) && !validLaneIds.Contains(task.LaneId))
                        return true;
                }
            }

            return false;
        }

        private void ApplySidebarAndStatusBarTheme()
        {
            var base100 = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            var base300 = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            if (_sidebarBorder != null)
            {
                _sidebarBorder.Background = base100;
                _sidebarBorder.BorderBrush = base300;
            }

            if (_statusBarBorder != null)
            {
                _statusBarBorder.Background = base100;
                _statusBarBorder.BorderBrush = base300;
            }
        }

        private void ApplySwimlaneLaneHeaderTheme()
        {
            var base200 = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            var base300 = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            var baseContent = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");

            EnsureVisualCache();
            foreach (var border in _cachedLaneHeaderBorders)
            {
                if (!border.IsLoaded)
                    continue;

                if (base200 != null)
                    border.Background = base200;
                if (base300 != null)
                    border.BorderBrush = base300;
            }

            foreach (var textBlock in _cachedLaneHeaderTexts)
            {
                if (!textBlock.IsLoaded)
                    continue;

                if (baseContent != null)
                    textBlock.Foreground = baseContent;
            }
        }

        private void RefreshTaskCardThemes()
        {
            EnsureVisualCache();
            foreach (var column in _cachedColumnControls)
            {
                if (!column.IsLoaded || column.Visibility != Visibility.Visible)
                    continue;

                foreach (var card in column.GetRealizedTaskCards())
                {
                    card.RefreshTheme();
                }
            }
        }

        private void ScheduleThemeRefresh()
        {
            if (DispatcherQueue == null)
            {
                RefreshTaskCardThemes();
                RefreshUserManagementThemes();
                return;
            }

            _themeRefreshTimer ??= DispatcherQueue.CreateTimer();
            _themeRefreshTimer.Stop();
            _themeRefreshTimer.Interval = TimeSpan.FromMilliseconds(ThemeRefreshDebounceMilliseconds);
            _themeRefreshTimer.Tick -= OnThemeRefreshTimerTick;
            _themeRefreshTimer.Tick += OnThemeRefreshTimerTick;
            _themeRefreshTimer.Start();
        }

        private void OnThemeRefreshTimerTick(DispatcherQueueTimer sender, object args)
        {
            sender.Stop();
            RefreshTaskCardThemes();
            RefreshUserManagementThemes();
        }

        private void RefreshUserManagementThemes()
        {
            EnsureVisualCache();
            foreach (var view in _cachedUserManagementViews)
            {
                if (!view.IsLoaded)
                    continue;

                view.RefreshTheme();
            }
        }

        private void ShowBoardHeaderButtons()
        {
            const double targetOpacity = 0.7;
            if (_renameBoardButton != null)
            {
                _renameBoardButton.IsHitTestVisible = true;
                _renameBoardButton.Opacity = targetOpacity;
            }
        }

        private void HideBoardHeaderButtons()
        {
            if (_renameBoardButton != null)
            {
                _renameBoardButton.Opacity = 0;
                _renameBoardButton.IsHitTestVisible = false;
            }
        }

        private T? FindChild<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result && (predicate == null || predicate(result)))
                    return result;
                var found = FindChild<T>(child, predicate);
                if (found != null)
                    return found;
            }
            return default;
        }

        private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
        {
            yield return root;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                foreach (var descendant in EnumerateVisualTree(child))
                    yield return descendant;
            }
        }

        #region Zoom Methods
        private static readonly DaisySize[] SizeOrder =
        {
            DaisySize.ExtraSmall,
            DaisySize.Small,
            DaisySize.Medium,
            DaisySize.Large,
            DaisySize.ExtraLarge
        };

        private bool CanZoomIn() => Array.IndexOf(SizeOrder, BoardSize) < SizeOrder.Length - 1;

        private bool CanZoomOut() => Array.IndexOf(SizeOrder, BoardSize) > 0;

        private void ExecuteZoomIn()
        {
            var currentIndex = Array.IndexOf(SizeOrder, BoardSize);
            if (currentIndex < SizeOrder.Length - 1)
            {
                BoardSize = SizeOrder[currentIndex + 1];
            }
        }

        private void ExecuteZoomOut()
        {
            var currentIndex = Array.IndexOf(SizeOrder, BoardSize);
            if (currentIndex > 0)
            {
                BoardSize = SizeOrder[currentIndex - 1];
            }
        }

        private void NotifyZoomCommandsChanged()
        {
            if (ZoomInCommand is RelayCommand zoomIn)
                zoomIn.RaiseCanExecuteChanged();
            if (ZoomOutCommand is RelayCommand zoomOut)
                zoomOut.RaiseCanExecuteChanged();
        }
        #endregion
    }
}

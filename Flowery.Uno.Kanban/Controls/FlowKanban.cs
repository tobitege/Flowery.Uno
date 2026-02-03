using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Windows.Storage;
using Windows.Storage.Pickers;
using Flowery.Helpers;
using Flowery.Localization;
using Flowery.Services;
using Flowery.Uno.Kanban.Controls.Users;
using Flowery.Uno.Kanban.Interfaces;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// A Kanban board container that manages columns and task cards.
    /// Supports JSON persistence and column rearrangement.
    /// </summary>
    public partial class FlowKanban : DaisyBaseContentControl
    {
        /// <summary>
        /// Raised when the board size changes.
        /// </summary>
        public event EventHandler<DaisySize>? BoardSizeChanged;

        /// <summary>
        /// Raised when the column width changes.
        /// </summary>
        public event EventHandler<double>? ColumnWidthChanged;

        /// <summary>
        /// Raised when the Kanban switches between compact and standard layouts.
        /// </summary>
        public event EventHandler<bool>? CompactLayoutChanged;

        /// <summary>
        /// Raised when a drag operation ends (completed or cancelled).
        /// Columns should clean up their drop indicators when this fires.
        /// </summary>
        public event EventHandler? DragEnded;

        /// <summary>
        /// Raised before a card is moved. Handlers can cancel the move by setting Cancel = true.
        /// </summary>
        public event EventHandler<CardMovingEventArgs>? CardMoving;

        /// <summary>
        /// Raised after a card has been moved to a new location.
        /// </summary>
        public event EventHandler<CardMovedEventArgs>? CardMoved;

        /// <summary>
        /// Raised when a card's properties are edited.
        /// </summary>
        public event EventHandler<FlowTask>? CardEdited;

        /// <summary>
        /// Raised when a column's properties are edited.
        /// </summary>
        public event EventHandler<FlowKanbanColumnData>? ColumnEdited;

        /// <summary>
        /// Raised when board-level properties are edited.
        /// </summary>
        public event EventHandler<BoardEditedEventArgs>? BoardEdited;

        /// <summary>
        /// Raised when lane grouping or lane definitions change.
        /// </summary>
        public event EventHandler? LaneGroupingChanged;

        /// <summary>
        /// Raised when the search filter changes (text or task match state).
        /// </summary>
        public event EventHandler? SearchFilterChanged;

        internal const double DefaultColumnWidth = 280;
        internal const double DefaultMinColumnWidth = 100;
        internal const double DefaultMaxColumnWidth = 380;
        internal const string UnassignedLaneId = "__unassigned__";
        public static bool IsUserManagementButtonEnabled { get; set; }

        internal static string? NormalizeLaneId(string? laneId)
        {
            return string.IsNullOrWhiteSpace(laneId) ? null : laneId.Trim();
        }

        internal static bool IsUnassignedLaneId(string? laneId)
        {
            if (string.IsNullOrWhiteSpace(laneId))
                return true;

            var trimmed = laneId.Trim();
            return string.Equals(trimmed, "0", StringComparison.Ordinal)
                   || string.Equals(trimmed, UnassignedLaneId, StringComparison.Ordinal);
        }

        internal bool RaiseCardMoving(CardMovingEventArgs args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            CardMoving?.Invoke(this, args);
            return args.Cancel;
        }

        internal void RaiseCardMoved(CardMovedEventArgs args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            CardMoved?.Invoke(this, args);
        }

        private ObservableCollection<string>? _trackedTags;
        private ObservableCollection<FlowKanbanLane>? _trackedLanes;
        private readonly FlowKanbanCommandHistory _commandHistory = new();
        private IBoardStore _boardStore = new FlowKanbanBoardStore(StateStorageProvider.Instance);
        private readonly List<ItemsControl> _columnItemsControls = new();
        private readonly Dictionary<ItemsControl, Canvas> _columnDropLayers = new();
        private Rectangle? _columnDropIndicator;
        private int _currentColumnDropIndex = -1;
        private bool _isApplyingSearchFilter;

        internal const string ColumnDragDataFormat = "flowery/kanban-column";
        internal const string ColumnDragDataPropertyKey = "flowery/kanban-column-id";
        internal const string TaskDragDataPropertyKey = "flowery/kanban-task-id";

        public FloweryLocalization Localization { get; } = FloweryLocalization.Instance;
        public FlowKanbanCommandHistory CommandHistory => _commandHistory;
        public IBoardStore BoardStore
        {
            get => _boardStore;
            set => _boardStore = value ?? throw new ArgumentNullException(nameof(value));
        }
        internal bool IsLaneGroupingEnabled => Board.GroupBy == FlowKanbanGroupBy.Lane && Board.Lanes.Count > 0;

        static FlowKanban()
        {
            FloweryLocalization.RegisterAssembly(typeof(FlowKanban).Assembly);
        }

        public FlowKanban()
        {
            DefaultStyleKey = typeof(FlowKanban);

            IsFilePickerAvailable = GetDefaultFilePickerAvailability();
            IsUserManagementButtonVisible = IsUserManagementButtonEnabled;

            AddColumnCommand = new RelayCommand(ExecuteAddColumn);
            SaveCommand = new RelayCommand(ExecuteSave);
            LoadCommand = new RelayCommand(ExecuteLoad);
            SaveToFileCommand = new RelayCommand(ExecuteSaveToFile, CanExecuteFilePickerCommand);
            LoadFromFileCommand = new RelayCommand(ExecuteLoadFromFile, CanExecuteFilePickerCommand);
            SettingsCommand = new RelayCommand(ExecuteOpenSettings);
            ShowHomeCommand = new RelayCommand(ExecuteShowHome);
            ShowUserManagementCommand = new RelayCommand(ExecuteShowUserManagement);
            OpenBoardCommand = new RelayCommand<FlowBoardMetadata>(ExecuteOpenBoard);
            CreateBoardCommand = new RelayCommand(ExecuteCreateBoard);
            CreateDemoBoardCommand = new RelayCommand(ExecuteCreateDemoBoard);
            RenameBoardHomeCommand = new RelayCommand<FlowBoardMetadata>(ExecuteRenameBoard);
            DeleteBoardCommand = new RelayCommand<FlowBoardMetadata>(ExecuteDeleteBoard);
            DuplicateBoardCommand = new RelayCommand<FlowBoardMetadata>(ExecuteDuplicateBoard);
            ExportBoardCommand = new RelayCommand<FlowBoardMetadata>(ExecuteExportBoard);
            AddCardCommand = new RelayCommand<FlowKanbanColumnData>(ExecuteAddCard);
            QuickAddCardCommand = new RelayCommand(ExecuteQuickAddCard);
            RemoveCardCommand = new RelayCommand<FlowTask>(ExecuteRemoveCard);
            RemoveColumnCommand = new RelayCommand<FlowKanbanColumnData>(ExecuteRemoveColumn);
            EditColumnCommand = new RelayCommand<FlowKanbanColumnData>(ExecuteEditColumn);
            ToggleColumnCollapseCommand = new RelayCommand<FlowKanbanColumnData>(ExecuteToggleColumnCollapse);
            ZoomInCommand = new RelayCommand(ExecuteZoomIn, CanZoomIn);
            ZoomOutCommand = new RelayCommand(ExecuteZoomOut, CanZoomOut);
            ToggleStatusBarCommand = new RelayCommand(() => IsStatusBarVisible = !IsStatusBarVisible);
            ToggleArchiveColumnVisibilityCommand = new RelayCommand(ExecuteToggleArchiveColumnVisibility);
            ShowKeyboardHelpCommand = new RelayCommand(ExecuteShowKeyboardHelp);
            ShowMetricsCommand = new RelayCommand(ExecuteShowMetrics);
            EditBoardCommand = new RelayCommand(ExecuteEditBoard);
            RenameBoardCommand = new RelayCommand(ExecuteRenameBoard);
            UndoCommand = new RelayCommand(() => _commandHistory.Undo(), () => EnableUndoRedo && _commandHistory.CanUndo);
            RedoCommand = new RelayCommand(() => _commandHistory.Redo(), () => EnableUndoRedo && _commandHistory.CanRedo);
            BulkMoveCommand = new RelayCommand<FlowKanbanColumnData?>(ExecuteBulkMove, CanExecuteBulkMove);
            BulkSetPriorityCommand = new RelayCommand<string?>(ExecuteBulkSetPriority, _ => HasSelection);
            BulkSetTagsCommand = new RelayCommand<string?>(ExecuteBulkSetTags, _ => HasSelection);
            BulkSetDueDateCommand = new RelayCommand<DateTimeOffset?>(ExecuteBulkSetDueDate, _ => HasSelection);
            BulkArchiveCommand = new RelayCommand(ExecuteBulkArchive, () => HasSelection);
            BulkDeleteCommand = new RelayCommand(ExecuteBulkDelete, () => HasSelection);
            ClearSelectionCommand = new RelayCommand(DeselectAllTasks, () => HasSelection);
            _commandHistory.PropertyChanged += OnCommandHistoryChanged;
            InitializeFilterbar();
            Loaded += OnKanbanLoaded;
            Unloaded += OnKanbanUnloaded;
            LaneRows = new ObservableCollection<FlowKanbanLaneRowView>();
            Boards = new ObservableCollection<FlowBoardMetadata>();
        }

        #region Board
        public static readonly DependencyProperty BoardProperty =
            DependencyProperty.Register(
                nameof(Board),
                typeof(FlowKanbanData),
                typeof(FlowKanban),
                new PropertyMetadata(new FlowKanbanData(), OnBoardChanged));

        public FlowKanbanData Board
        {
            get => (FlowKanbanData)GetValue(BoardProperty);
            set => SetValue(BoardProperty, value);
        }

        private static void OnBoardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                kanban.AttachBoardTracking(e.NewValue as FlowKanbanData);
            }
        }
        #endregion

        #region Commands
        public static readonly DependencyProperty AddColumnCommandProperty =
            DependencyProperty.Register(
                nameof(AddColumnCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand AddColumnCommand
        {
            get => (ICommand)GetValue(AddColumnCommandProperty);
            set => SetValue(AddColumnCommandProperty, value);
        }

        public static readonly DependencyProperty SaveCommandProperty =
            DependencyProperty.Register(
                nameof(SaveCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand SaveCommand
        {
            get => (ICommand)GetValue(SaveCommandProperty);
            set => SetValue(SaveCommandProperty, value);
        }

        public static readonly DependencyProperty LoadCommandProperty =
            DependencyProperty.Register(
                nameof(LoadCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand LoadCommand
        {
            get => (ICommand)GetValue(LoadCommandProperty);
            set => SetValue(LoadCommandProperty, value);
        }

        public static readonly DependencyProperty SaveToFileCommandProperty =
            DependencyProperty.Register(
                nameof(SaveToFileCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand SaveToFileCommand
        {
            get => (ICommand)GetValue(SaveToFileCommandProperty);
            set => SetValue(SaveToFileCommandProperty, value);
        }

        public static readonly DependencyProperty LoadFromFileCommandProperty =
            DependencyProperty.Register(
                nameof(LoadFromFileCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand LoadFromFileCommand
        {
            get => (ICommand)GetValue(LoadFromFileCommandProperty);
            set => SetValue(LoadFromFileCommandProperty, value);
        }

        public static readonly DependencyProperty SettingsCommandProperty =
            DependencyProperty.Register(
                nameof(SettingsCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand SettingsCommand
        {
            get => (ICommand)GetValue(SettingsCommandProperty);
            set => SetValue(SettingsCommandProperty, value);
        }

        public static readonly DependencyProperty AddCardCommandProperty =
            DependencyProperty.Register(
                nameof(AddCardCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand AddCardCommand
        {
            get => (ICommand)GetValue(AddCardCommandProperty);
            set => SetValue(AddCardCommandProperty, value);
        }

        public static readonly DependencyProperty QuickAddCardCommandProperty =
            DependencyProperty.Register(
                nameof(QuickAddCardCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand QuickAddCardCommand
        {
            get => (ICommand)GetValue(QuickAddCardCommandProperty);
            set => SetValue(QuickAddCardCommandProperty, value);
        }

        public static readonly DependencyProperty RemoveCardCommandProperty =
            DependencyProperty.Register(
                nameof(RemoveCardCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand RemoveCardCommand
        {
            get => (ICommand)GetValue(RemoveCardCommandProperty);
            set => SetValue(RemoveCardCommandProperty, value);
        }

        public static readonly DependencyProperty RemoveColumnCommandProperty =
            DependencyProperty.Register(
                nameof(RemoveColumnCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand RemoveColumnCommand
        {
            get => (ICommand)GetValue(RemoveColumnCommandProperty);
            set => SetValue(RemoveColumnCommandProperty, value);
        }

        public static readonly DependencyProperty EditColumnCommandProperty =
            DependencyProperty.Register(
                nameof(EditColumnCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Command invoked when the edit column action is requested.
        /// The command parameter is the FlowKanbanColumnData to edit.
        /// </summary>
        public ICommand EditColumnCommand
        {
            get => (ICommand)GetValue(EditColumnCommandProperty);
            set => SetValue(EditColumnCommandProperty, value);
        }

        public static readonly DependencyProperty ToggleColumnCollapseCommandProperty =
            DependencyProperty.Register(
                nameof(ToggleColumnCollapseCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand ToggleColumnCollapseCommand
        {
            get => (ICommand)GetValue(ToggleColumnCollapseCommandProperty);
            set => SetValue(ToggleColumnCollapseCommandProperty, value);
        }

        public static readonly DependencyProperty EditCardCommandProperty =
            DependencyProperty.Register(
                nameof(EditCardCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Command invoked when a card is double-tapped for editing.
        /// The command parameter is the FlowTask to edit.
        /// </summary>
        public ICommand EditCardCommand
        {
            get => (ICommand)GetValue(EditCardCommandProperty);
            set => SetValue(EditCardCommandProperty, value);
        }

        public static readonly DependencyProperty ZoomInCommandProperty =
            DependencyProperty.Register(
                nameof(ZoomInCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Command to zoom in (increase board size).
        /// </summary>
        public ICommand ZoomInCommand
        {
            get => (ICommand)GetValue(ZoomInCommandProperty);
            set => SetValue(ZoomInCommandProperty, value);
        }

        public static readonly DependencyProperty ZoomOutCommandProperty =
            DependencyProperty.Register(
                nameof(ZoomOutCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Command to zoom out (decrease board size).
        /// </summary>
        public ICommand ZoomOutCommand
        {
            get => (ICommand)GetValue(ZoomOutCommandProperty);
            set => SetValue(ZoomOutCommandProperty, value);
        }

        public static readonly DependencyProperty ToggleStatusBarCommandProperty =
            DependencyProperty.Register(
                nameof(ToggleStatusBarCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand ToggleStatusBarCommand
        {
            get => (ICommand)GetValue(ToggleStatusBarCommandProperty);
            set => SetValue(ToggleStatusBarCommandProperty, value);
        }

        public static readonly DependencyProperty ToggleArchiveColumnVisibilityCommandProperty =
            DependencyProperty.Register(
                nameof(ToggleArchiveColumnVisibilityCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand ToggleArchiveColumnVisibilityCommand
        {
            get => (ICommand)GetValue(ToggleArchiveColumnVisibilityCommandProperty);
            set => SetValue(ToggleArchiveColumnVisibilityCommandProperty, value);
        }

        public static readonly DependencyProperty ShowKeyboardHelpCommandProperty =
            DependencyProperty.Register(
                nameof(ShowKeyboardHelpCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand ShowKeyboardHelpCommand
        {
            get => (ICommand)GetValue(ShowKeyboardHelpCommandProperty);
            set => SetValue(ShowKeyboardHelpCommandProperty, value);
        }

        public static readonly DependencyProperty ShowMetricsCommandProperty =
            DependencyProperty.Register(
                nameof(ShowMetricsCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand ShowMetricsCommand
        {
            get => (ICommand)GetValue(ShowMetricsCommandProperty);
            set => SetValue(ShowMetricsCommandProperty, value);
        }

        public static readonly DependencyProperty EditBoardCommandProperty =
            DependencyProperty.Register(
                nameof(EditBoardCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Command to open the board editor dialog.
        /// </summary>
        public ICommand EditBoardCommand
        {
            get => (ICommand)GetValue(EditBoardCommandProperty);
            set => SetValue(EditBoardCommandProperty, value);
        }

        public static readonly DependencyProperty RenameBoardCommandProperty =
            DependencyProperty.Register(
                nameof(RenameBoardCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Command to rename the board title inline (similar to column rename).
        /// </summary>
        public ICommand RenameBoardCommand
        {
            get => (ICommand)GetValue(RenameBoardCommandProperty);
            set => SetValue(RenameBoardCommandProperty, value);
        }

        public static readonly DependencyProperty EnableUndoRedoProperty =
            DependencyProperty.Register(
                nameof(EnableUndoRedo),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnEnableUndoRedoChanged));

        /// <summary>
        /// Enables undo/redo command history for Kanban operations.
        /// </summary>
        public bool EnableUndoRedo
        {
            get => (bool)GetValue(EnableUndoRedoProperty);
            set => SetValue(EnableUndoRedoProperty, value);
        }

        public static readonly DependencyProperty UndoCommandProperty =
            DependencyProperty.Register(
                nameof(UndoCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Command to undo the last Kanban operation.
        /// </summary>
        public ICommand UndoCommand
        {
            get => (ICommand)GetValue(UndoCommandProperty);
            set => SetValue(UndoCommandProperty, value);
        }

        public static readonly DependencyProperty RedoCommandProperty =
            DependencyProperty.Register(
                nameof(RedoCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Command to redo the last undone Kanban operation.
        /// </summary>
        public ICommand RedoCommand
        {
            get => (ICommand)GetValue(RedoCommandProperty);
            set => SetValue(RedoCommandProperty, value);
        }

        public static readonly DependencyProperty BulkMoveCommandProperty =
            DependencyProperty.Register(
                nameof(BulkMoveCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand BulkMoveCommand
        {
            get => (ICommand)GetValue(BulkMoveCommandProperty);
            set => SetValue(BulkMoveCommandProperty, value);
        }

        public static readonly DependencyProperty BulkSetPriorityCommandProperty =
            DependencyProperty.Register(
                nameof(BulkSetPriorityCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand BulkSetPriorityCommand
        {
            get => (ICommand)GetValue(BulkSetPriorityCommandProperty);
            set => SetValue(BulkSetPriorityCommandProperty, value);
        }

        public static readonly DependencyProperty BulkSetTagsCommandProperty =
            DependencyProperty.Register(
                nameof(BulkSetTagsCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand BulkSetTagsCommand
        {
            get => (ICommand)GetValue(BulkSetTagsCommandProperty);
            set => SetValue(BulkSetTagsCommandProperty, value);
        }

        public static readonly DependencyProperty BulkSetDueDateCommandProperty =
            DependencyProperty.Register(
                nameof(BulkSetDueDateCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand BulkSetDueDateCommand
        {
            get => (ICommand)GetValue(BulkSetDueDateCommandProperty);
            set => SetValue(BulkSetDueDateCommandProperty, value);
        }

        public static readonly DependencyProperty BulkArchiveCommandProperty =
            DependencyProperty.Register(
                nameof(BulkArchiveCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand BulkArchiveCommand
        {
            get => (ICommand)GetValue(BulkArchiveCommandProperty);
            set => SetValue(BulkArchiveCommandProperty, value);
        }

        public static readonly DependencyProperty BulkDeleteCommandProperty =
            DependencyProperty.Register(
                nameof(BulkDeleteCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand BulkDeleteCommand
        {
            get => (ICommand)GetValue(BulkDeleteCommandProperty);
            set => SetValue(BulkDeleteCommandProperty, value);
        }

        public static readonly DependencyProperty ClearSelectionCommandProperty =
            DependencyProperty.Register(
                nameof(ClearSelectionCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand ClearSelectionCommand
        {
            get => (ICommand)GetValue(ClearSelectionCommandProperty);
            set => SetValue(ClearSelectionCommandProperty, value);
        }
        #endregion

        #region SearchText
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(FlowKanban),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        /// <summary>
        /// Search text used to filter task cards.
        /// </summary>
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                kanban.SyncCriteriaTextFromSearch();
                kanban.ApplySearchFilter();
            }
        }
        #endregion

        #region Selection
        public static readonly DependencyProperty SelectedCountProperty =
            DependencyProperty.Register(
                nameof(SelectedCount),
                typeof(int),
                typeof(FlowKanban),
                new PropertyMetadata(0));

        public int SelectedCount
        {
            get => (int)GetValue(SelectedCountProperty);
            private set => SetValue(SelectedCountProperty, value);
        }

        public static readonly DependencyProperty HasSelectionProperty =
            DependencyProperty.Register(
                nameof(HasSelection),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        public bool HasSelection
        {
            get => (bool)GetValue(HasSelectionProperty);
            private set => SetValue(HasSelectionProperty, value);
        }

        public static readonly DependencyProperty SelectedBulkMoveColumnProperty =
            DependencyProperty.Register(
                nameof(SelectedBulkMoveColumn),
                typeof(FlowKanbanColumnData),
                typeof(FlowKanban),
                new PropertyMetadata(null, OnSelectedBulkMoveColumnChanged));

        public FlowKanbanColumnData? SelectedBulkMoveColumn
        {
            get => (FlowKanbanColumnData?)GetValue(SelectedBulkMoveColumnProperty);
            set => SetValue(SelectedBulkMoveColumnProperty, value);
        }

        private static void OnSelectedBulkMoveColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                kanban.NotifySelectionCommandsChanged();
            }
        }
        #endregion

        #region Persistence Keys
        private const string LegacySettingsStorageKey = "kanban.settings";
        private const string UserSettingsStorageKeyPrefix = "kanban.user.settings";
        private const string GlobalAdminStorageKey = "kanban.admin.global";
        #endregion

        #region Settings
        private const int AutoSaveDebounceMilliseconds = 800;
        private const int DoneAgingCheckIntervalMinutes = 60;

        private bool _userSettingsLoaded;
        private bool _isApplyingUserSettings;
        private string? _currentUserSettingsKey;
        private bool _suppressAutoSave;
        private string? _lastBoardId;
        private FlowKanbanData? _trackedBoard;
        private readonly HashSet<FlowKanbanColumnData> _trackedColumns = new();
        private readonly HashSet<FlowTask> _trackedTasks = new();
        private readonly HashSet<FlowSubtask> _trackedSubtasks = new();
        private readonly HashSet<FlowKanbanLane> _trackedLaneItems = new();
        private DispatcherQueueTimer? _autoSaveTimer;
        private ManagedTimer? _doneAgingTimer;

        /// <summary>
        /// Last board ID loaded or saved via persistence.
        /// </summary>
        public string? LastBoardId => _lastBoardId;

        public static readonly DependencyProperty ConfirmColumnRemovalsProperty =
            DependencyProperty.Register(
                nameof(ConfirmColumnRemovals),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(true, OnSettingChanged));

        /// <summary>
        /// Whether to confirm before removing columns.
        /// </summary>
        public bool ConfirmColumnRemovals
        {
            get => (bool)GetValue(ConfirmColumnRemovalsProperty);
            set => SetValue(ConfirmColumnRemovalsProperty, value);
        }

        public static readonly DependencyProperty ConfirmCardRemovalsProperty =
            DependencyProperty.Register(
                nameof(ConfirmCardRemovals),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(true, OnSettingChanged));

        /// <summary>
        /// Whether to confirm before removing cards.
        /// </summary>
        public bool ConfirmCardRemovals
        {
            get => (bool)GetValue(ConfirmCardRemovalsProperty);
            set => SetValue(ConfirmCardRemovalsProperty, value);
        }

        public static readonly DependencyProperty AutoSaveAfterEditsProperty =
            DependencyProperty.Register(
                nameof(AutoSaveAfterEdits),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(true, OnSettingChanged));

        /// <summary>
        /// Whether the board auto-saves after edits.
        /// </summary>
        public bool AutoSaveAfterEdits
        {
            get => (bool)GetValue(AutoSaveAfterEditsProperty);
            set => SetValue(AutoSaveAfterEditsProperty, value);
        }

        public static readonly DependencyProperty AutoExpandCardDetailsProperty =
            DependencyProperty.Register(
                nameof(AutoExpandCardDetails),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(true, OnSettingChanged));

        /// <summary>
        /// Whether the task editor expands all sections by default.
        /// </summary>
        public bool AutoExpandCardDetails
        {
            get => (bool)GetValue(AutoExpandCardDetailsProperty);
            set => SetValue(AutoExpandCardDetailsProperty, value);
        }

        public static readonly DependencyProperty AddCardPlacementProperty =
            DependencyProperty.Register(
                nameof(AddCardPlacement),
                typeof(FlowKanbanAddCardPlacement),
                typeof(FlowKanban),
                new PropertyMetadata(FlowKanbanAddCardPlacement.Bottom, OnSettingChanged));

        /// <summary>
        /// Controls where the inline "Add Card" control appears in a column.
        /// </summary>
        public FlowKanbanAddCardPlacement AddCardPlacement
        {
            get => (FlowKanbanAddCardPlacement)GetValue(AddCardPlacementProperty);
            set => SetValue(AddCardPlacementProperty, value);
        }

        public static readonly DependencyProperty CompactColumnSizingModeProperty =
            DependencyProperty.Register(
                nameof(CompactColumnSizingMode),
                typeof(FlowKanbanCompactColumnSizingMode),
                typeof(FlowKanban),
                new PropertyMetadata(FlowKanbanCompactColumnSizingMode.Adaptive, OnCompactColumnSizingChanged));

        /// <summary>
        /// Controls how compact columns are sized vertically.
        /// </summary>
        public FlowKanbanCompactColumnSizingMode CompactColumnSizingMode
        {
            get => (FlowKanbanCompactColumnSizingMode)GetValue(CompactColumnSizingModeProperty);
            set => SetValue(CompactColumnSizingModeProperty, value);
        }

        public static readonly DependencyProperty CompactManualCardCountProperty =
            DependencyProperty.Register(
                nameof(CompactManualCardCount),
                typeof(int),
                typeof(FlowKanban),
                new PropertyMetadata(3, OnCompactColumnSizingChanged));

        /// <summary>
        /// Number of task cards to target when using manual compact sizing.
        /// </summary>
        public int CompactManualCardCount
        {
            get => (int)GetValue(CompactManualCardCountProperty);
            set => SetValue(CompactManualCardCountProperty, value);
        }

        public static readonly DependencyProperty ShowWelcomeMessageProperty =
            DependencyProperty.Register(
                nameof(ShowWelcomeMessage),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(true, OnSettingChanged));

        /// <summary>
        /// Whether the home welcome message is visible.
        /// </summary>
        public bool ShowWelcomeMessage
        {
            get => (bool)GetValue(ShowWelcomeMessageProperty);
            set => SetValue(ShowWelcomeMessageProperty, value);
        }

        public static readonly DependencyProperty WelcomeMessageTitleProperty =
            DependencyProperty.Register(
                nameof(WelcomeMessageTitle),
                typeof(string),
                typeof(FlowKanban),
                new PropertyMetadata(string.Empty, OnSettingChanged));

        /// <summary>
        /// Custom title for the home welcome message. Leave empty to use the localized default.
        /// </summary>
        public string WelcomeMessageTitle
        {
            get => (string)GetValue(WelcomeMessageTitleProperty);
            set => SetValue(WelcomeMessageTitleProperty, value ?? string.Empty);
        }

        public static readonly DependencyProperty WelcomeMessageSubtitleProperty =
            DependencyProperty.Register(
                nameof(WelcomeMessageSubtitle),
                typeof(string),
                typeof(FlowKanban),
                new PropertyMetadata(string.Empty, OnSettingChanged));

        /// <summary>
        /// Custom subtitle for the home welcome message. Leave empty to use the localized default.
        /// </summary>
        public string WelcomeMessageSubtitle
        {
            get => (string)GetValue(WelcomeMessageSubtitleProperty);
            set => SetValue(WelcomeMessageSubtitleProperty, value ?? string.Empty);
        }

        private static void OnSettingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                kanban.TrySaveSettings(out _);
                if (e.Property == IsStatusBarVisibleProperty)
                {
                    kanban.UpdateBoardStatusBarVisibility();
                }
                if (e.Property == AutoSaveAfterEditsProperty && !kanban.AutoSaveAfterEdits)
                {
                    kanban.StopAutoSaveTimer();
                }
            }
        }

        private static void OnCompactColumnSizingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnSettingChanged(d, e);
            if (d is FlowKanban kanban)
            {
                kanban.ClampCompactManualCardCount();
                kanban.ScheduleCompactColumnSizingUpdate();
            }
        }
        #endregion

        private string? GetDefaultLaneId()
        {
            if (!IsLaneGroupingEnabled)
                return null;

            if (Board.Lanes.Count == 0)
                return null;

            var laneId = Board.Lanes[0].Id;
            return string.IsNullOrWhiteSpace(laneId) ? null : laneId;
        }

        internal void ShowWipWarning(FlowKanbanColumnData column, string? laneId)
        {
            if (column == null)
                return;

            var display = column.GetLaneWipDisplay(laneId);
            var message = string.Format(
                CultureInfo.CurrentUICulture,
                FloweryLocalization.GetStringInternal("Kanban_WipWarning", "WIP limit exceeded ({0})."),
                display);

            ShowStatusMessage(message, TimeSpan.FromSeconds(4));
        }

        private void ExecuteAddCard(FlowKanbanColumnData? column)
        {
            if (column != null)
            {
                var task = new FlowTask
                {
                    Title = FloweryLocalization.GetStringInternal("Kanban_Editor_NewTask"),
                    LaneId = GetDefaultLaneId()
                };
                FlowKanbanWorkItemNumberHelper.EnsureTaskNumber(Board, task);
                ExecuteCommand(new AddCardCommand(column, task));
                FlowKanbanDoneColumnHelper.UpdateCompletedAtOnAdd(Board, column, task);
            }
        }

        private void ExecuteQuickAddCard()
        {
            if (!IsBoardViewActive)
                return;

            var column = GetActiveColumnData();
            if (column == null)
                return;

            BeginInlineAddCard(column);
        }

        private void ExecuteToggleColumnCollapse(FlowKanbanColumnData? column)
        {
            if (column != null)
            {
                column.IsCollapsed = !column.IsCollapsed;
            }
        }

        private void ExecuteToggleArchiveColumnVisibility()
        {
            if (Board == null)
                return;

            if (string.IsNullOrWhiteSpace(Board.ArchiveColumnId) || Board.IsArchiveColumnHidden)
            {
                var manager = new FlowKanbanManager(this, autoAttach: false);
                manager.EnsureArchiveColumn();
                Board.IsArchiveColumnHidden = false;
            }
            else
            {
                Board.IsArchiveColumnHidden = true;
            }

            UpdateArchiveColumnState();
            ApplySearchFilter();
        }

        private bool CanExecuteBulkMove(FlowKanbanColumnData? column)
        {
            return HasSelection && column != null;
        }

        private void ExecuteBulkMove(FlowKanbanColumnData? column)
        {
            if (!HasSelection || column == null)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            var movedCount = manager.BulkMove(column);
            if (movedCount > 0)
            {
                DeselectAllTasks();
            }
            else
            {
                UpdateSelectionMetrics();
            }
        }

        private void ExecuteBulkSetPriority(string? priorityValue)
        {
            if (!HasSelection || string.IsNullOrWhiteSpace(priorityValue))
                return;

            if (!Enum.TryParse(priorityValue, out FlowTaskPriority priority))
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            manager.BulkSetPriority(priority);
        }

        private void ExecuteBulkSetTags(string? tags)
        {
            if (!HasSelection)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            manager.BulkSetTags(tags);
        }

        private void ExecuteBulkSetDueDate(DateTimeOffset? dueDate)
        {
            if (!HasSelection)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            manager.BulkSetDueDate(dueDate?.Date);
        }

        private async void ExecuteBulkArchive()
        {
            if (!HasSelection || XamlRoot == null)
                return;

            var confirmed = await FlowKanbanConfirmDialog.ShowAsync(
                FloweryLocalization.GetStringInternal("Kanban_Bulk_Archive"),
                FloweryLocalization.GetStringInternal("Kanban_Bulk_ArchiveConfirmMessage"),
                FloweryLocalization.GetStringInternal("Kanban_Bulk_Archive"),
                Flowery.Controls.DaisyButtonVariant.Primary,
                XamlRoot);
            if (!confirmed)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            manager.BulkArchive();
            UpdateSelectionMetrics();
        }

        private async void ExecuteBulkDelete()
        {
            if (!HasSelection || XamlRoot == null)
                return;

            var confirmed = await FlowKanbanConfirmDialog.ShowAsync(
                FloweryLocalization.GetStringInternal("Common_ConfirmDelete"),
                FloweryLocalization.GetStringInternal("Kanban_Bulk_DeleteConfirmMessage"),
                FloweryLocalization.GetStringInternal("Common_Delete"),
                Flowery.Controls.DaisyButtonVariant.Error,
                XamlRoot);
            if (!confirmed)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            manager.BulkDelete();
            UpdateSelectionMetrics();
        }

        private void ExecuteSave()
        {
            if (!TrySaveBoard(out var error) && error != null)
            {
                // In a real app, we'd log or show a toast
                System.Diagnostics.Debug.WriteLine($"Failed to save Kanban: {error.Message}");
            }
        }

        private void ExecuteLoad()
        {
            if (TryLoadLastBoard(out var error))
            {
                CurrentView = FlowKanbanView.Board;
                return;
            }

            if (error != null)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Kanban: {error.Message}");
            }
        }

        private async void ExecuteSaveToFile()
        {
            if (!IsFilePickerAvailable)
                return;

            try
            {
                var boardId = EnsureBoardId(Board);
                var json = JsonSerializer.Serialize(Board, FlowKanbanJsonContext.Default.FlowKanbanData);
                var payload = new FlowKanbanBoardExportRequestedEventArgs(boardId, Board.Title, json);
                if (!await TrySaveExportToFileAsync(payload))
                {
                    System.Diagnostics.Debug.WriteLine("Failed to save board export.");
                }
            }
            catch (Exception ex) when (ex is COMException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is InvalidOperationException || ex is IOException || ex is ArgumentException)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save board export: {ex.Message}");
            }
        }

        private async void ExecuteLoadFromFile()
        {
            if (!IsFilePickerAvailable)
                return;

            try
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".json");
                InitializeFileOpenPicker(picker);

                var file = await picker.PickSingleFileAsync();
                if (file == null)
                    return;

                var json = await FileIO.ReadTextAsync(file);
                if (!FlowKanbanBoardSanitizer.TryLoadFromJson(json, out var data) || data == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to import board data from file.");
                    return;
                }

                ApplyImportedBoard(data);
            }
            catch (Exception ex) when (ex is COMException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is InvalidOperationException || ex is IOException || ex is ArgumentException)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load board from file: {ex.Message}");
            }
        }

        private static void InitializeFileOpenPicker(FileOpenPicker picker)
        {
#if WINDOWS
            var window = FlowerySizeManager.MainWindow;
            if (window == null)
                return;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
#endif
        }

        private void ApplyImportedBoard(FlowKanbanData data)
        {
            _suppressAutoSave = true;
            try
            {
                Board = data;
            }
            finally
            {
                _suppressAutoSave = false;
            }

            _ = EnsureAssigneeIdsValidAsync();
            TrySaveBoard(out _);
            RefreshBoards();
            CurrentView = FlowKanbanView.Board;
        }

        private bool CanExecuteFilePickerCommand()
        {
            return IsFilePickerAvailable;
        }

        private static bool GetDefaultFilePickerAvailability()
        {
            return OperatingSystem.IsWindows();
        }

        private void OnCommandHistoryChanged(object? sender, PropertyChangedEventArgs e)
        {
            NotifyCommandHistoryChanged();
        }

        private static void OnEnableUndoRedoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                if (!kanban.EnableUndoRedo)
                {
                    kanban._commandHistory.Clear();
                }

                kanban.NotifyCommandHistoryChanged();
            }
        }

        private void NotifyCommandHistoryChanged()
        {
            if (UndoCommand is RelayCommand undo)
                undo.RaiseCanExecuteChanged();
            if (RedoCommand is RelayCommand redo)
                redo.RaiseCanExecuteChanged();
        }

        private void NotifySelectionCommandsChanged()
        {
            if (BulkMoveCommand is RelayCommand<FlowKanbanColumnData?> bulkMove)
                bulkMove.RaiseCanExecuteChanged();
            if (BulkSetPriorityCommand is RelayCommand<string?> bulkPriority)
                bulkPriority.RaiseCanExecuteChanged();
            if (BulkSetTagsCommand is RelayCommand<string?> bulkTags)
                bulkTags.RaiseCanExecuteChanged();
            if (BulkSetDueDateCommand is RelayCommand<DateTimeOffset?> bulkDueDate)
                bulkDueDate.RaiseCanExecuteChanged();
            if (BulkArchiveCommand is RelayCommand bulkArchive)
                bulkArchive.RaiseCanExecuteChanged();
            if (BulkDeleteCommand is RelayCommand bulkDelete)
                bulkDelete.RaiseCanExecuteChanged();
            if (ClearSelectionCommand is RelayCommand clearSelection)
                clearSelection.RaiseCanExecuteChanged();
        }

        private void NotifyLaneGroupingChanged()
        {
            UpdateLaneGroupingState();
            LaneGroupingChanged?.Invoke(this, EventArgs.Empty);
        }

        internal void ExecuteCommand(IKanbanCommand command)
        {
            if (EnableUndoRedo)
            {
                _commandHistory.Execute(command);
            }
            else
            {
                command.Execute();
            }
        }

        internal sealed class FlowKanbanUserSettingsState
        {
            public bool ConfirmColumnRemovals { get; set; } = true;
            public bool ConfirmCardRemovals { get; set; } = true;
            public bool AutoSaveAfterEdits { get; set; } = true;
            public bool AutoExpandCardDetails { get; set; } = true;
            public bool EnableUndoRedo { get; set; } = false;
            public FlowKanbanAddCardPlacement AddCardPlacement { get; set; } = FlowKanbanAddCardPlacement.Bottom;
            public FlowKanbanCompactColumnSizingMode CompactColumnSizingMode { get; set; } = FlowKanbanCompactColumnSizingMode.Adaptive;
            public int CompactManualCardCount { get; set; } = 3;
            public bool ShowWelcomeMessage { get; set; } = true;
            public string? WelcomeMessageTitle { get; set; } = string.Empty;
            public string? WelcomeMessageSubtitle { get; set; } = string.Empty;
            public bool IsStatusBarVisible { get; set; } = true;
            public double ColumnWidth { get; set; } = DefaultColumnWidth;
            public string? LastBoardId { get; set; }
            public FlowKanbanView LastView { get; set; } = FlowKanbanView.Board;
        }

        private FlowKanbanUserSettingsState BuildUserSettingsState()
        {
            return new FlowKanbanUserSettingsState
            {
                ConfirmColumnRemovals = ConfirmColumnRemovals,
                ConfirmCardRemovals = ConfirmCardRemovals,
                AutoSaveAfterEdits = AutoSaveAfterEdits,
                AutoExpandCardDetails = AutoExpandCardDetails,
                EnableUndoRedo = EnableUndoRedo,
                AddCardPlacement = AddCardPlacement,
                CompactColumnSizingMode = CompactColumnSizingMode,
                CompactManualCardCount = CompactManualCardCount,
                ShowWelcomeMessage = ShowWelcomeMessage,
                WelcomeMessageTitle = WelcomeMessageTitle,
                WelcomeMessageSubtitle = WelcomeMessageSubtitle,
                IsStatusBarVisible = IsStatusBarVisible,
                ColumnWidth = ColumnWidth,
                LastBoardId = _lastBoardId,
                LastView = CurrentView
            };
        }

        private void ApplyUserSettingsState(FlowKanbanUserSettingsState state)
        {
            _isApplyingUserSettings = true;
            try
            {
                ConfirmColumnRemovals = state.ConfirmColumnRemovals;
                ConfirmCardRemovals = state.ConfirmCardRemovals;
                AutoSaveAfterEdits = state.AutoSaveAfterEdits;
                AutoExpandCardDetails = state.AutoExpandCardDetails;
                EnableUndoRedo = state.EnableUndoRedo;
                AddCardPlacement = state.AddCardPlacement;
                CompactColumnSizingMode = state.CompactColumnSizingMode;
                CompactManualCardCount = state.CompactManualCardCount;
                ShowWelcomeMessage = state.ShowWelcomeMessage;
                WelcomeMessageTitle = state.WelcomeMessageTitle ?? string.Empty;
                WelcomeMessageSubtitle = state.WelcomeMessageSubtitle ?? string.Empty;
                IsStatusBarVisible = state.IsStatusBarVisible;
                ColumnWidth = state.ColumnWidth;
                _lastBoardId = state.LastBoardId;
                CurrentView = Enum.IsDefined(typeof(FlowKanbanView), state.LastView)
                    ? state.LastView
                    : FlowKanbanView.Board;
            }
            finally
            {
                _isApplyingUserSettings = false;
            }
        }

        /// <summary>
        /// Loads persisted settings if available.
        /// </summary>
        /// <param name="forceReload">When true, reloads even if settings were already loaded.</param>
        /// <param name="error">Exception details when the load fails.</param>
        /// <returns>True when the operation completed without throwing; otherwise false.</returns>
        public bool TryLoadSettings(bool forceReload, out Exception? error)
        {
            return TryLoadUserSettings(forceReload, out error);
        }

        /// <summary>
        /// Saves current settings to persistent storage.
        /// </summary>
        /// <param name="error">Exception details when the save fails.</param>
        /// <returns>True when the operation completed without throwing; otherwise false.</returns>
        public bool TrySaveSettings(out Exception? error)
        {
            return TrySaveUserSettings(out error);
        }

        /// <summary>
        /// Loads persisted user settings if available.
        /// </summary>
        /// <param name="forceReload">When true, reloads even if settings were already loaded.</param>
        /// <param name="error">Exception details when the load fails.</param>
        /// <returns>True when the operation completed without throwing; otherwise false.</returns>
        public bool TryLoadUserSettings(bool forceReload, out Exception? error)
        {
            try
            {
                LoadUserSettingsCore(forceReload);
                _ = EnsureUserSettingsLoadedAsync(forceReload);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        /// <summary>
        /// Saves current user settings to persistent storage.
        /// </summary>
        /// <param name="error">Exception details when the save fails.</param>
        /// <returns>True when the operation completed without throwing; otherwise false.</returns>
        public bool TrySaveUserSettings(out Exception? error)
        {
            try
            {
                SaveUserSettingsCore();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        private void LoadUserSettingsCore(bool forceReload)
        {
            if (_userSettingsLoaded && !forceReload)
                return;

            _userSettingsLoaded = true;
            var storageKey = GetEffectiveUserSettingsKey();
            var json = LoadStateText(storageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                TryMigrateLegacyUserSettings(storageKey);
                return;
            }

            var data = JsonSerializer.Deserialize(json, FlowKanbanJsonContext.Default.FlowKanbanUserSettingsState);
            if (data != null)
                ApplyUserSettingsState(data);
        }

        private void SaveUserSettingsCore()
        {
            if (_isApplyingUserSettings)
                return;

            var json = JsonSerializer.Serialize(BuildUserSettingsState(), FlowKanbanJsonContext.Default.FlowKanbanUserSettingsState);
            SaveStateText(GetEffectiveUserSettingsKey(), json);
        }

        private async Task EnsureUserSettingsLoadedAsync(bool forceReload)
        {
            var resolvedKey = await ResolveUserSettingsKeyAsync();
            if (string.IsNullOrWhiteSpace(resolvedKey))
            {
                resolvedKey = LegacySettingsStorageKey;
            }

            if (!string.Equals(_currentUserSettingsKey, resolvedKey, StringComparison.Ordinal))
            {
                _currentUserSettingsKey = resolvedKey;
                _userSettingsLoaded = false;
            }

            LoadUserSettingsCore(forceReload);
        }

        private async Task<string?> ResolveUserSettingsKeyAsync()
        {
            var provider = UserProvider;
            if (provider == null)
                return _currentUserSettingsKey ?? LegacySettingsStorageKey;

            var user = await provider.GetCurrentUserAsync();
            var userId = ResolveUserId(user);
            if (string.IsNullOrWhiteSpace(userId))
                return LegacySettingsStorageKey;

            return StateStorageKeyHelper.BuildScopedKey(UserSettingsStorageKeyPrefix, userId);
        }

        private static string? ResolveUserId(IFlowUser? user)
        {
            if (user == null)
                return null;

            if (!string.IsNullOrWhiteSpace(user.Id))
                return user.Id;

            if (!string.IsNullOrWhiteSpace(user.RawId))
                return FlowUserIdHelper.Compose(user.ProviderKey, user.RawId);

            return null;
        }

        private string GetEffectiveUserSettingsKey()
        {
            return string.IsNullOrWhiteSpace(_currentUserSettingsKey)
                ? LegacySettingsStorageKey
                : _currentUserSettingsKey;
        }

        private void TryMigrateLegacyUserSettings(string currentKey)
        {
            if (string.Equals(currentKey, LegacySettingsStorageKey, StringComparison.Ordinal))
                return;

            var legacyJson = LoadStateText(LegacySettingsStorageKey);
            if (string.IsNullOrWhiteSpace(legacyJson))
                return;

            var legacyData = JsonSerializer.Deserialize(legacyJson, FlowKanbanJsonContext.Default.FlowKanbanUserSettingsState);
            if (legacyData == null)
                return;

            ApplyUserSettingsState(legacyData);
            SaveUserSettingsCore();
        }

        /// <summary>
        /// Saves the current board to persistent storage.
        /// </summary>
        /// <param name="error">Exception details when the save fails.</param>
        /// <returns>True when the operation completed without throwing; otherwise false.</returns>
        public bool TrySaveBoard(out Exception? error)
        {
            try
            {
                SaveBoardStateCore(Board, updateLastBoardId: true);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        /// <summary>
        /// Loads the most recently saved board from persistent storage.
        /// </summary>
        /// <param name="error">Exception details when the load fails.</param>
        /// <param name="useCompactLayout">When true, forces compact layout for the loaded board.</param>
        /// <param name="compactColumnId">Optional column ID to display in compact layout.</param>
        /// <param name="compactColumnTitle">Optional column title to display in compact layout.</param>
        /// <returns>True when a board was loaded successfully; otherwise false.</returns>
        public bool TryLoadLastBoard(
            out Exception? error,
            bool useCompactLayout = false,
            string? compactColumnId = null,
            string? compactColumnTitle = null)
        {
            try
            {
                var loaded = LoadBoardStateCore();
                if (loaded)
                {
                    ApplyCompactLayoutOptions(useCompactLayout, compactColumnId, compactColumnTitle);
                }
                error = null;
                return loaded;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        private void SaveBoardStateCore(FlowKanbanData board, bool updateLastBoardId)
        {
            if (board == null)
                return;

            var boardId = EnsureBoardId(board);
            if (!BoardStore.TrySaveBoard(board, out var error))
            {
                if (error != null)
                    throw error;

                throw new InvalidOperationException("Failed to save board.");
            }
            if (updateLastBoardId)
            {
                UpdateLastBoardId(boardId);
            }
        }

        private bool LoadBoardStateCore()
        {
            var boardId = _lastBoardId;
            if (string.IsNullOrWhiteSpace(boardId))
                boardId = EnsureBoardId(Board);

            return LoadBoardStateCore(boardId);
        }

        private bool LoadBoardStateCore(string boardId)
        {
            if (string.IsNullOrWhiteSpace(boardId))
                return false;

            if (!BoardStore.TryLoadBoard(boardId, out var data, out var error))
            {
                if (error != null)
                    throw error;

                return false;
            }

            if (data == null)
                return false;

            if (string.IsNullOrWhiteSpace(data.Id) || !string.Equals(data.Id, boardId, StringComparison.Ordinal))
            {
                data.Id = boardId;
            }

            _suppressAutoSave = true;
            try
            {
                Board = data;
            }
            finally
            {
                _suppressAutoSave = false;
            }

            UpdateLastBoardId(data.Id);
            _ = EnsureAssigneeIdsValidAsync();
            return true;
        }

        private static string? LoadStateText(string key)
        {
            var lines = StateStorageProvider.Instance.LoadLines(key);
            if (lines.Count == 0)
                return null;

            return string.Join(Environment.NewLine, lines);
        }

        private static void SaveStateText(string key, string content)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            StateStorageProvider.Instance.SaveLines(key, lines);
        }

        private void UpdateLastBoardId(string boardId)
        {
            if (string.IsNullOrWhiteSpace(boardId))
                return;

            if (string.Equals(_lastBoardId, boardId, StringComparison.Ordinal))
                return;

            _lastBoardId = boardId;
            TrySaveSettings(out _);
        }

        private static string EnsureBoardId(FlowKanbanData board)
        {
            if (!FlowKanbanBoardSanitizer.IsValidId(board.Id))
            {
                board.Id = Guid.NewGuid().ToString();
            }

            return board.Id;
        }

        private void RequestAutoSave()
        {
            if (_suppressAutoSave || !AutoSaveAfterEdits)
                return;

            if (!EnsureAutoSaveTimer())
            {
                ExecuteSave();
                return;
            }

            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Start();
        }

        private bool EnsureAutoSaveTimer()
        {
            if (_autoSaveTimer != null)
                return true;

            var dispatcherQueue = DispatcherQueue;
            if (dispatcherQueue == null)
                return false;

            _autoSaveTimer = dispatcherQueue.CreateTimer();
            _autoSaveTimer.IsRepeating = false;
            _autoSaveTimer.Interval = TimeSpan.FromMilliseconds(AutoSaveDebounceMilliseconds);
            _autoSaveTimer.Tick += OnAutoSaveTimerTick;
            return true;
        }

        private void StopAutoSaveTimer()
        {
            _autoSaveTimer?.Stop();
        }

        private void OnAutoSaveTimerTick(DispatcherQueueTimer sender, object args)
        {
            sender.Stop();
            ExecuteSave();
        }

        private void RefreshDoneAgingState()
        {
            if (_trackedBoard == null)
            {
                StopDoneAgingTimer();
                return;
            }

            var manager = new FlowKanbanManager(this, autoAttach: false);
            manager.SyncDoneColumnTimestamps();

            if (!IsDoneAgingActive(manager))
            {
                StopDoneAgingTimer();
                return;
            }

            manager.AutoArchiveDoneTasks();
            EnsureDoneAgingTimer();
        }

        private bool IsDoneAgingActive(FlowKanbanManager manager)
        {
            if (_trackedBoard == null)
                return false;

            if (!_trackedBoard.AutoArchiveDoneEnabled || _trackedBoard.AutoArchiveDoneDays <= 0)
                return false;

            return manager.GetDoneColumn() != null;
        }

        private void EnsureDoneAgingTimer()
        {
            if (_doneAgingTimer == null)
            {
                _doneAgingTimer = new ManagedTimer(TimeSpan.FromMinutes(DoneAgingCheckIntervalMinutes));
                _doneAgingTimer.Tick += OnDoneAgingTimerTick;
            }
            else
            {
                _doneAgingTimer.Interval = TimeSpan.FromMinutes(DoneAgingCheckIntervalMinutes);
            }

            if (!_doneAgingTimer.IsRunning)
            {
                _doneAgingTimer.Start();
            }
        }

        private void StopDoneAgingTimer()
        {
            _doneAgingTimer?.Stop();
        }

        private void DisposeDoneAgingTimer()
        {
            if (_doneAgingTimer == null)
                return;

            _doneAgingTimer.Dispose();
            _doneAgingTimer = null;
        }

        private void OnDoneAgingTimerTick(object? sender, EventArgs e)
        {
            if (_trackedBoard == null)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            if (!IsDoneAgingActive(manager))
            {
                StopDoneAgingTimer();
                return;
            }

            manager.AutoArchiveDoneTasks();
        }

        private void AttachBoardTracking(FlowKanbanData? board)
        {
            if (ReferenceEquals(_trackedBoard, board))
                return;

            DetachBoardTracking();
            _trackedBoard = board;

            if (_trackedBoard == null)
                return;

            _trackedBoard.PropertyChanged += OnBoardPropertyChanged;
            _trackedBoard.Columns.CollectionChanged += OnColumnsCollectionChanged;
            _trackedTags = _trackedBoard.Tags;
            _trackedTags.CollectionChanged += OnTagsCollectionChanged;
            _trackedLanes = _trackedBoard.Lanes;
            _trackedLanes.CollectionChanged += OnLanesCollectionChanged;

            foreach (var column in _trackedBoard.Columns)
            {
                TrackColumn(column);
            }

            foreach (var lane in _trackedBoard.Lanes)
            {
                TrackLane(lane);
            }

            UpdateArchiveColumnState();
            UpdateLayoutItemsSources();
            ApplySearchFilter();
            NotifyLaneGroupingChanged();
            ClampKeyboardColumnIndex();
            RefreshDoneAgingState();
            InvalidateVisualCache();
        }

        private void DetachBoardTracking()
        {
            if (_trackedBoard == null)
                return;

            _trackedBoard.PropertyChanged -= OnBoardPropertyChanged;
            _trackedBoard.Columns.CollectionChanged -= OnColumnsCollectionChanged;
            if (_trackedTags != null)
            {
                _trackedTags.CollectionChanged -= OnTagsCollectionChanged;
                _trackedTags = null;
            }
            if (_trackedLanes != null)
            {
                _trackedLanes.CollectionChanged -= OnLanesCollectionChanged;
                _trackedLanes = null;
            }

            var columns = new List<FlowKanbanColumnData>(_trackedColumns);
            foreach (var column in columns)
            {
                UntrackColumn(column);
            }

            var lanes = new List<FlowKanbanLane>(_trackedLaneItems);
            foreach (var lane in lanes)
            {
                UntrackLane(lane);
            }

            _trackedColumns.Clear();
            _trackedTasks.Clear();
            _trackedSubtasks.Clear();
            _trackedLaneItems.Clear();
            _trackedBoard = null;
            UpdateLayoutItemsSources();
            InvalidateVisualCache();
        }

        private void TrackColumn(FlowKanbanColumnData column)
        {
            if (!_trackedColumns.Add(column))
                return;

            column.PropertyChanged += OnColumnPropertyChanged;
            column.Tasks.CollectionChanged += OnTasksCollectionChanged;

            foreach (var task in column.Tasks)
            {
                TrackTask(task);
            }
        }

        private void UntrackColumn(FlowKanbanColumnData column)
        {
            if (!_trackedColumns.Remove(column))
                return;

            column.PropertyChanged -= OnColumnPropertyChanged;
            column.Tasks.CollectionChanged -= OnTasksCollectionChanged;

            foreach (var task in column.Tasks)
            {
                UntrackTask(task);
            }
        }

        private void TrackTask(FlowTask task)
        {
            if (!_trackedTasks.Add(task))
                return;

            task.PropertyChanged += OnTaskPropertyChanged;
            task.Subtasks.CollectionChanged += OnSubtasksCollectionChanged;
            ApplySearchFilterToTask(task);

            foreach (var subtask in task.Subtasks)
            {
                TrackSubtask(subtask);
            }
        }

        private void UntrackTask(FlowTask task)
        {
            if (!_trackedTasks.Remove(task))
                return;

            task.PropertyChanged -= OnTaskPropertyChanged;
            task.Subtasks.CollectionChanged -= OnSubtasksCollectionChanged;

            foreach (var subtask in task.Subtasks)
            {
                UntrackSubtask(subtask);
            }
        }

        private void TrackSubtask(FlowSubtask subtask)
        {
            if (!_trackedSubtasks.Add(subtask))
                return;

            subtask.PropertyChanged += OnSubtaskPropertyChanged;
        }

        private void UntrackSubtask(FlowSubtask subtask)
        {
            if (!_trackedSubtasks.Remove(subtask))
                return;

            subtask.PropertyChanged -= OnSubtaskPropertyChanged;
        }

        private void TrackLane(FlowKanbanLane lane)
        {
            if (!_trackedLaneItems.Add(lane))
                return;

            lane.PropertyChanged += OnLanePropertyChanged;
        }

        private void UntrackLane(FlowKanbanLane lane)
        {
            if (!_trackedLaneItems.Remove(lane))
                return;

            lane.PropertyChanged -= OnLanePropertyChanged;
        }

        private void RebuildTracking()
        {
            if (_trackedBoard == null)
                return;

            var columns = new List<FlowKanbanColumnData>(_trackedColumns);
            foreach (var column in columns)
            {
                UntrackColumn(column);
            }

            _trackedColumns.Clear();
            _trackedTasks.Clear();
            _trackedSubtasks.Clear();

            foreach (var column in _trackedBoard.Columns)
            {
                TrackColumn(column);
            }
        }

        private void RebuildLaneTracking()
        {
            if (_trackedBoard == null)
                return;

            var lanes = new List<FlowKanbanLane>(_trackedLaneItems);
            foreach (var lane in lanes)
            {
                UntrackLane(lane);
            }

            _trackedLaneItems.Clear();

            foreach (var lane in _trackedBoard.Lanes)
            {
                TrackLane(lane);
            }
        }

        private void OnBoardPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(FlowKanbanData.Columns), StringComparison.Ordinal))
            {
                RebuildTracking();
                UpdateLaneRows();
                UpdateArchiveColumnState();
                UpdateLayoutItemsSources();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowKanbanData.Tags), StringComparison.Ordinal))
            {
                if (_trackedBoard != null)
                {
                    if (_trackedTags != null)
                    {
                        _trackedTags.CollectionChanged -= OnTagsCollectionChanged;
                    }

                    _trackedTags = _trackedBoard.Tags;
                    _trackedTags.CollectionChanged += OnTagsCollectionChanged;
                }
            }
            else if (string.Equals(e.PropertyName, nameof(FlowKanbanData.Lanes), StringComparison.Ordinal))
            {
                if (_trackedBoard != null)
                {
                    if (_trackedLanes != null)
                    {
                        _trackedLanes.CollectionChanged -= OnLanesCollectionChanged;
                    }

                    _trackedLanes = _trackedBoard.Lanes;
                    _trackedLanes.CollectionChanged += OnLanesCollectionChanged;
                    RebuildLaneTracking();
                }

                NotifyLaneGroupingChanged();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowKanbanData.GroupBy), StringComparison.Ordinal))
            {
                NotifyLaneGroupingChanged();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowKanbanData.ArchiveColumnId), StringComparison.Ordinal)
                || string.Equals(e.PropertyName, nameof(FlowKanbanData.IsArchiveColumnHidden), StringComparison.Ordinal))
            {
                UpdateArchiveColumnState();
                ApplySearchFilter();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowKanbanData.DoneColumnId), StringComparison.Ordinal)
                || string.Equals(e.PropertyName, nameof(FlowKanbanData.AutoArchiveDoneEnabled), StringComparison.Ordinal)
                || string.Equals(e.PropertyName, nameof(FlowKanbanData.AutoArchiveDoneDays), StringComparison.Ordinal))
            {
                RefreshDoneAgingState();
            }

            if (_trackedBoard != null)
            {
                BoardEdited?.Invoke(this, new BoardEditedEventArgs(_trackedBoard, e.PropertyName));
            }

            RequestAutoSave();
        }

        private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RebuildTracking();
                RequestAutoSave();
                return;
            }

            if (e.OldItems != null)
            {
                foreach (FlowKanbanColumnData column in e.OldItems)
                {
                    UntrackColumn(column);
                }
            }

            if (e.NewItems != null)
            {
                foreach (FlowKanbanColumnData column in e.NewItems)
                {
                    TrackColumn(column);
                }
            }

            UpdateLaneRows();
            UpdateArchiveColumnState();
            RefreshDoneAgingState();
            RequestAutoSave();
            ClampKeyboardColumnIndex();
            InvalidateVisualCache();
        }

        private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RebuildTracking();
                RequestAutoSave();
                ApplySearchFilter();
                return;
            }

            if (e.OldItems != null)
            {
                foreach (FlowTask task in e.OldItems)
                {
                    UntrackTask(task);
                }
            }

            if (e.NewItems != null)
            {
                foreach (FlowTask task in e.NewItems)
                {
                    TrackTask(task);
                    ApplySearchFilterToTask(task);
                }
            }

            UpdateLaneRows();
            UpdateSelectionMetrics();
            RequestAutoSave();
            InvalidateVisualCache();
        }

        private void OnSubtasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RebuildTracking();
                RequestAutoSave();
                return;
            }

            if (e.OldItems != null)
            {
                foreach (FlowSubtask subtask in e.OldItems)
                {
                    UntrackSubtask(subtask);
                }
            }

            if (e.NewItems != null)
            {
                foreach (FlowSubtask subtask in e.NewItems)
                {
                    TrackSubtask(subtask);
                }
            }

            RequestAutoSave();
        }

        private void OnColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is FlowKanbanColumnData column)
            {
                ColumnEdited?.Invoke(this, column);
            }

            if (string.Equals(e.PropertyName, nameof(FlowKanbanColumnData.WipLimit), StringComparison.Ordinal)
                || string.Equals(e.PropertyName, nameof(FlowKanbanColumnData.LaneWipLimits), StringComparison.Ordinal))
            {
                UpdateLaneRows();
            }

            if (string.Equals(e.PropertyName, nameof(FlowKanbanColumnData.IsCollapsed), StringComparison.Ordinal))
            {
                RequestCompactLayoutScrollRefresh();
            }

            RequestAutoSave();
        }

        private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(FlowTask.IsSearchMatch), StringComparison.Ordinal))
                return;

            if (sender is FlowTask task)
            {
                var isSearchRelevant = IsSearchRelevantProperty(e.PropertyName)
                                       || string.Equals(e.PropertyName, nameof(FlowTask.IsArchived), StringComparison.Ordinal);
                if (isSearchRelevant)
                {
                    ApplySearchFilterToTask(task);
                }
            }

            if (sender is FlowTask editedTask)
            {
                CardEdited?.Invoke(this, editedTask);
            }

            if (string.Equals(e.PropertyName, nameof(FlowTask.LaneId), StringComparison.Ordinal))
            {
                UpdateLaneRows();
            }

            if (string.Equals(e.PropertyName, nameof(FlowTask.IsSelected), StringComparison.Ordinal))
            {
                UpdateSelectionMetrics();
            }

            RequestAutoSave();
        }

        internal void DeselectAllTasks()
        {
            foreach (var column in Board.Columns)
            {
                foreach (var task in column.Tasks)
                {
                    if (task.IsSelected)
                        task.IsSelected = false;
                }
            }

            UpdateSelectionMetrics();
        }

        private void UpdateSelectionMetrics()
        {
            var count = 0;
            foreach (var column in Board.Columns)
            {
                foreach (var task in column.Tasks)
                {
                    if (task.IsSelected)
                        count++;
                }
            }

            SelectedCount = count;
            HasSelection = count > 0;
            if (count >= 2 && !IsStatusBarVisible)
            {
                IsStatusBarVisible = true;
            }
            NotifySelectionCommandsChanged();
        }

        private void OnSubtaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RequestAutoSave();
        }

        private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RequestAutoSave();
        }

        private void OnLanesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RebuildLaneTracking();
                RequestAutoSave();
                NotifyLaneGroupingChanged();
                return;
            }

            if (e.OldItems != null)
            {
                foreach (FlowKanbanLane lane in e.OldItems)
                {
                    UntrackLane(lane);
                }
            }

            if (e.NewItems != null)
            {
                foreach (FlowKanbanLane lane in e.NewItems)
                {
                    TrackLane(lane);
                }
            }

            RequestAutoSave();
            NotifyLaneGroupingChanged();
            InvalidateVisualCache();
        }

        private void OnLanePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RequestAutoSave();
            NotifyLaneGroupingChanged();
        }

        private bool IsSearchRelevantProperty(string? propertyName)
        {
            return string.Equals(propertyName, nameof(FlowTask.Title), StringComparison.Ordinal) ||
                   string.Equals(propertyName, nameof(FlowTask.Description), StringComparison.Ordinal) ||
                   string.Equals(propertyName, nameof(FlowTask.Tags), StringComparison.Ordinal) ||
                   string.Equals(propertyName, nameof(FlowTask.Assignee), StringComparison.Ordinal) ||
                   string.Equals(propertyName, nameof(FlowTask.AssigneeId), StringComparison.Ordinal);
        }

        private void ApplySearchFilter()
        {
            if (_trackedBoard == null)
                return;

            _isApplyingSearchFilter = true;
            try
            {
                foreach (var column in _trackedBoard.Columns)
                {
                    foreach (var task in column.Tasks)
                    {
                        ApplySearchFilterToTask(task);
                    }
                }
            }
            finally
            {
                _isApplyingSearchFilter = false;
            }

            SearchFilterChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplySearchFilterToTask(FlowTask task)
        {
            var isInArchiveColumn = IsTaskInArchiveColumn(task);
            if (isInArchiveColumn)
            {
                var matches = string.IsNullOrWhiteSpace(SearchText);
                if (task.IsSearchMatch != matches)
                {
                    task.IsSearchMatch = matches;
                    if (!_isApplyingSearchFilter)
                    {
                        SearchFilterChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                return;
            }

            if (task.IsArchived)
            {
                if (task.IsSearchMatch)
                {
                    task.IsSearchMatch = false;
                    if (!_isApplyingSearchFilter)
                    {
                        SearchFilterChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                return;
            }

            var isMatch = IsTaskMatch(task, SearchText);
            if (task.IsSearchMatch == isMatch)
                return;

            task.IsSearchMatch = isMatch;
            if (!_isApplyingSearchFilter)
            {
                SearchFilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool IsTaskInArchiveColumn(FlowTask task)
        {
            if (_trackedBoard == null)
                return false;

            var archiveId = _trackedBoard.ArchiveColumnId;
            if (string.IsNullOrWhiteSpace(archiveId))
                return false;

            foreach (var column in _trackedBoard.Columns)
            {
                if (!string.Equals(column.Id, archiveId, StringComparison.Ordinal))
                    continue;

                return column.Tasks.Contains(task);
            }

            return false;
        }

        private void UpdateArchiveColumnState()
        {
            if (_trackedBoard == null)
                return;

            var archiveId = _trackedBoard.ArchiveColumnId;
            FlowKanbanColumnData? archiveColumn = null;
            if (!string.IsNullOrWhiteSpace(archiveId))
            {
                foreach (var column in _trackedBoard.Columns)
                {
                    if (string.Equals(column.Id, archiveId, StringComparison.Ordinal))
                    {
                        archiveColumn = column;
                        break;
                    }
                }
            }

            var hasArchiveColumn = archiveColumn != null;
            var isHidden = _trackedBoard.IsArchiveColumnHidden;

            foreach (var column in _trackedBoard.Columns)
            {
                var isArchive = ReferenceEquals(column, archiveColumn);
                column.IsArchiveColumn = isArchive;
                column.IsArchiveColumnVisible = !isArchive || !isHidden;
            }

            CanShowArchiveColumn = !hasArchiveColumn || isHidden;
            CanHideArchiveColumn = hasArchiveColumn && !isHidden;
            HasArchiveColumnToggle = CanShowArchiveColumn || CanHideArchiveColumn;
            UpdateBulkMoveColumns();
            UpdateLayoutItemsSources();
        }

        private void UpdateBulkMoveColumns()
        {
            var columns = BulkMoveColumns;
            columns.Clear();

            if (_trackedBoard == null)
            {
                SelectedBulkMoveColumn = null;
                return;
            }

            foreach (var column in _trackedBoard.Columns)
            {
                if (!column.IsArchiveColumnVisible)
                    continue;

                columns.Add(column);
            }

            if (SelectedBulkMoveColumn != null && !columns.Contains(SelectedBulkMoveColumn))
            {
                SelectedBulkMoveColumn = null;
            }
        }

        private bool IsTaskMatch(FlowTask task, string? searchText)
        {
            // Use enhanced filter criteria if available
            if (FilterCriteria != null && FilterCriteria.HasAnyFilter)
            {
                return IsTaskMatchWithCriteria(task, FilterCriteria);
            }

            // Legacy text-only search mode
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            var filter = searchText.Trim();
            var comparison = StringComparison.OrdinalIgnoreCase;

            if (!string.IsNullOrWhiteSpace(task.Title) && task.Title.Contains(filter, comparison))
                return true;

            if (!string.IsNullOrWhiteSpace(task.Description) && task.Description.Contains(filter, comparison))
                return true;

            if (!string.IsNullOrWhiteSpace(task.Tags) && task.Tags.Contains(filter, comparison))
                return true;

            if (!string.IsNullOrWhiteSpace(task.Assignee) && task.Assignee.Contains(filter, comparison))
                return true;

            return false;
        }

    }

    /// <summary>
    /// View model for a swimlane row with per-column lane cells.
    /// </summary>
    public sealed class FlowKanbanLaneRowView
    {
        public FlowKanbanLaneRowView(FlowKanbanLane lane, ObservableCollection<FlowKanbanLaneCellView> cells)
        {
            Lane = lane ?? throw new ArgumentNullException(nameof(lane));
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
        }

        public FlowKanbanLane Lane { get; }
        public ObservableCollection<FlowKanbanLaneCellView> Cells { get; }
    }

    /// <summary>
    /// View model for a swimlane cell, bound to a column and lane filter.
    /// </summary>
    public sealed class FlowKanbanLaneCellView
    {
        public FlowKanbanLaneCellView(FlowKanbanColumnData column, string? laneId)
        {
            Column = column ?? throw new ArgumentNullException(nameof(column));
            LaneId = laneId;
        }

        public FlowKanbanColumnData Column { get; }
        public string? LaneId { get; }

        public string LaneWipDisplay => Column.GetLaneWipDisplay(LaneId);
        public bool IsLaneWipExceeded => Column.IsLaneWipExceeded(LaneId);
        public bool HasLaneWipLimit => Column.GetLaneWipLimit(LaneId).HasValue;
    }

    /// <summary>
    /// Event arguments for the CardMoving event, which fires before a card is moved.
    /// </summary>
    public class CardMovingEventArgs : EventArgs
    {
        public CardMovingEventArgs(FlowTask card, FlowKanbanColumnData sourceColumn, FlowKanbanColumnData targetColumn, int targetIndex)
        {
            Card = card;
            SourceColumn = sourceColumn;
            TargetColumn = targetColumn;
            TargetIndex = targetIndex;
        }

        /// <summary>The card being moved.</summary>
        public FlowTask Card { get; }

        /// <summary>The column the card is moving from.</summary>
        public FlowKanbanColumnData SourceColumn { get; }

        /// <summary>The column the card is moving to.</summary>
        public FlowKanbanColumnData TargetColumn { get; }

        /// <summary>The target index within the destination column.</summary>
        public int TargetIndex { get; }

        /// <summary>The target lane ID when moving into a swimlane.</summary>
        public string? TargetLaneId { get; internal set; }

        /// <summary>True when the move would exceed the effective WIP limit.</summary>
        public bool WouldExceedWip { get; internal set; }

        /// <summary>The effective WIP limit for the move target.</summary>
        public int? TargetWipLimit { get; internal set; }

        /// <summary>The effective WIP count after the move.</summary>
        public int TargetWipCount { get; internal set; }

        /// <summary>Set to true to cancel the move operation.</summary>
        public bool Cancel { get; set; }

        /// <summary>Optional reason for cancellation.</summary>
        public string? CancelReason { get; set; }
    }

    /// <summary>
    /// Event arguments for the CardMoved event, which fires after a card has been moved.
    /// </summary>
    public class CardMovedEventArgs : EventArgs
    {
        public CardMovedEventArgs(FlowTask card, FlowKanbanColumnData sourceColumn, FlowKanbanColumnData targetColumn)
        {
            Card = card;
            SourceColumn = sourceColumn;
            TargetColumn = targetColumn;
        }

        /// <summary>The card that was moved.</summary>
        public FlowTask Card { get; }

        /// <summary>The column the card was moved from.</summary>
        public FlowKanbanColumnData SourceColumn { get; }

        /// <summary>The column the card was moved to.</summary>
        public FlowKanbanColumnData TargetColumn { get; }
    }

    /// <summary>
    /// Event arguments for board-level edits.
    /// </summary>
    public sealed class BoardEditedEventArgs : EventArgs
    {
        public BoardEditedEventArgs(FlowKanbanData board, string? propertyName)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            PropertyName = propertyName;
        }

        public FlowKanbanData Board { get; }
        public string? PropertyName { get; }
    }

    /// <summary>
    /// Result of a card move operation with validation.
    /// </summary>
    public enum MoveResult
    {
        /// <summary>The move completed successfully.</summary>
        Success,

        /// <summary>The move was canceled by a CardMoving event handler.</summary>
        CanceledByEvent,

        /// <summary>The move was blocked due to WIP limit (hard enforcement).</summary>
        BlockedByWip,

        /// <summary>The move succeeded but exceeded WIP limit (soft warning).</summary>
        AllowedWithWipWarning,

        /// <summary>The source task or column was not found.</summary>
        NotFound
    }

    /// <summary>
    /// Placement options for the inline add card control.
    /// </summary>
    public enum FlowKanbanAddCardPlacement
    {
        Bottom,
        Top,
        Both
    }

    /// <summary>
    /// Compact layout sizing modes for columns.
    /// </summary>
    public enum FlowKanbanCompactColumnSizingMode
    {
        Adaptive,
        Manual
    }
}

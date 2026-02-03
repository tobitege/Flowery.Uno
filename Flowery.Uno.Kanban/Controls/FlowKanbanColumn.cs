using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Flowery.Controls;
using Flowery.Theming;
using Flowery.Enums;
using Flowery.Localization;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

// Only for development FlowKanban debugging and logging:
//#define KANBAN_DEBUG

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// A column container for the Kanban board that supports drop operations.
    /// </summary>
    public partial class FlowKanbanColumn : DaisyBaseContentControl
    {
        private ItemsControl? _tasksItemsControl;
        private ListViewBase? _tasksListView;
        private Panel? _tasksItemsHost;
        private Canvas? _tasksDropIndicatorHost;
        private Rectangle? _dropIndicator;
        private int _currentDropIndex = -1;
        private double _currentDropIndicatorY = double.NaN;
        private double _taskItemSpacing = double.NaN;
        private Style? _tasksItemContainerStyle;
        private string? _draggedTaskId;
        private const int TaskViewStaggerBatchSize = 5;
        private readonly ObservableCollection<FlowKanbanTaskView> _taskViews = new();
        private readonly HashSet<FlowTask> _trackedViewTasks = new();
        private readonly Dictionary<FlowKanbanTaskView, FlowTaskCard> _realizedCards = new();
        private int _taskViewBuildVersion;
        private bool _taskViewBuildPending;
        private TaskViewBuildState? _taskViewBuildState;
        private ObservableCollection<FlowTask>? _trackedTasksCollection;
        private FlowKanbanColumnData? _trackedColumnData;
        private FlowTask? _lastSelectedTask;
        private bool _isLoaded;

        // Child elements for sizing and hover
        private TextBlock? _headerText;
        private DaisyButton? _editButton;
        private DaisyButton? _collapseButton;
        private FlowKanbanAddCard? _addCardTop;
        private FlowKanbanAddCard? _addCardBottom;
        private UIElement? _columnGripElement;
        private Grid? _columnHeaderGrid;
        private FrameworkElement? _headerIconsPanel;
        private Border? _taskCountBadge;
        private TextBlock? _taskCountText;
        private DaisyIconText? _collapseIcon;
        private bool? _baseShowTasks;
        private bool? _baseShowAddCard;
        private bool _isKeyboardFocusVisible;
        private bool _isDragHighlightActive;

        // Drag highlight border: reserve 2px always (transparent by default),
        // and show a green outline during drag operations.
        private static readonly Thickness s_dragHighlightBorderThickness = new(2);
        private static readonly SolidColorBrush s_dragHighlightTransparentBrush = new(Microsoft.UI.Colors.Transparent);
        private static readonly SolidColorBrush s_dragHighlightFallbackBrush = new(Microsoft.UI.Colors.LimeGreen);

        private static Brush GetDragHighlightBrush()
        {
            return DaisyResourceLookup.GetBrush("DaisySuccessBrush") ?? s_dragHighlightFallbackBrush;
        }

        private void SetDragHighlightBorder(bool isActive)
        {
            BorderThickness = s_dragHighlightBorderThickness;
            BorderBrush = isActive ? GetDragHighlightBrush() : s_dragHighlightTransparentBrush;

            // Ensure the focus VisualState doesn't override the drag highlight brush.
            if (isActive)
            {
                _isKeyboardFocusVisible = false;
                VisualStateManager.GoToState(this, "Unfocused", true);
            }
        }
#if HAS_UNO_SKIA
        private bool _isPointerColumnReorderTracking;
        private bool _isPointerColumnReorderActive;
        private uint _pointerColumnReorderPointerId;
        private Point _pointerColumnReorderStart;
        private UIElement? _pointerColumnReorderCaptureElement;

        private bool _isKanbanPointerReorderHandlersAttached;
        private PointerEventHandler? _kanbanPointerMovedHandler;
        private PointerEventHandler? _kanbanPointerReleasedHandler;

        private UIElement? _columnGripIconElement;

        private Microsoft.UI.Xaml.DragEventHandler? _skiaColumnDragOverHandler;
        private Microsoft.UI.Xaml.DragEventHandler? _skiaColumnDropHandler;
        private Microsoft.UI.Xaml.DragEventHandler? _skiaColumnDragLeaveHandler;

#if KANBAN_DEBUG
        private bool _isDebugHitTestProbeAttached;

        private static readonly string s_debugLogPath =
            global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), "flowery-kanban-column-debug.log");

        private bool _debugColumnBorderOverrideActive;
        private object? _debugColumnBorderSavedBorderBrushLocalValue;
        private object? _debugColumnBorderSavedBorderThicknessLocalValue;
        private SolidColorBrush? _debugColumnBorderBrush;

        private bool _debugColumnGripBackgroundOverrideActive;
        private object? _debugColumnGripSavedBackgroundLocalValue;
        private SolidColorBrush? _debugColumnGripBackgroundBrush;

        private bool _debugHeaderBackgroundOverrideActive;
        private object? _debugHeaderSavedBackgroundLocalValue;
        private SolidColorBrush? _debugHeaderBackgroundBrush;
#endif
#endif

        public ObservableCollection<FlowKanbanTaskView> TaskViews => _taskViews;

        private bool IsEffectiveCollapsed => IsCollapsed || ColumnData?.IsCollapsed == true;

        public FlowKanbanColumn()
        {
            DefaultStyleKey = typeof(FlowKanbanColumn);
            IsTabStop = true;
            DaisyNeumorphic.SetIsEnabled(this, false);
            AllowDrop = true;
#if HAS_UNO_SKIA
            // On Uno/Skia Desktop the inner ListView/ScrollViewer frequently marks drag routed
            // events as handled. Register as handledEventsToo so we always get target updates.
            _skiaColumnDragOverHandler ??= OnDragOver;
            _skiaColumnDropHandler ??= OnDrop;
            _skiaColumnDragLeaveHandler ??= OnDragLeave;
            AddHandler(UIElement.DragOverEvent, _skiaColumnDragOverHandler, handledEventsToo: true);
            AddHandler(UIElement.DropEvent, _skiaColumnDropHandler, handledEventsToo: true);
            AddHandler(UIElement.DragLeaveEvent, _skiaColumnDragLeaveHandler, handledEventsToo: true);
#else
            DragOver += OnDragOver;
            Drop += OnDrop;
            DragLeave += OnDragLeave;
#endif
            Loaded += OnColumnLoaded;
            Unloaded += OnColumnUnloaded;
            GotFocus += OnColumnGotFocus;
            LostFocus += OnColumnLostFocus;
            DoubleTapped += OnColumnDoubleTapped;

#if KANBAN_DEBUG && HAS_UNO_SKIA
            DebugLog($"KANBAN_DEBUG: ctor. Assembly='{typeof(FlowKanbanColumn).Assembly.FullName}' Location='{typeof(FlowKanbanColumn).Assembly.Location}'");
#endif

            SetDragHighlightBorder(isActive: false);
        }

        private static bool IsUnassignedLaneId(string? laneId) => FlowKanban.IsUnassignedLaneId(laneId);

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            // Refresh background and border to pick up new theme colors
            Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            ApplyTaskCountBadgeTheme();
            if (_dropIndicator != null)
            {
                _dropIndicator.Fill = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
            }

            var isDragActive = _isDragHighlightActive;
#if HAS_UNO_SKIA
            isDragActive |= _isPointerColumnReorderActive;
#endif
            SetDragHighlightBorder(isDragActive);
        }

        private void OnColumnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
#if KANBAN_DEBUG && HAS_UNO_SKIA
            DebugLog($"KANBAN_DEBUG: Loaded. ColumnId='{ColumnData?.Id ?? "<null>"}' IsCollapsed={IsCollapsed} IsEffectiveCollapsed={IsEffectiveCollapsed} ActualSize={ActualWidth:0.##}x{ActualHeight:0.##}");
#endif
            SetDragHighlightBorder(isActive: false);
            var searchRoot = Content as DependencyObject ?? this;

            // Find the ItemsControl that contains the tasks
            _tasksItemsControl = FindTasksItemsControl(searchRoot);
            _tasksListView = _tasksItemsControl as ListViewBase;
            _tasksItemsHost = _tasksItemsControl?.ItemsPanelRoot as Panel;
            _tasksDropIndicatorHost = FindChild<Canvas>(searchRoot, c => c.Name == "PART_TaskDropIndicatorHost");
            _realizedCards.Clear();
            if (_tasksListView != null)
            {
                _tasksListView.ContainerContentChanging -= OnTaskContainerContentChanging;
                _tasksListView.ContainerContentChanging += OnTaskContainerContentChanging;
                _tasksListView.AllowDrop = IsDropEnabled;
            }

            if (_tasksItemsControl is FrameworkElement tasksControl)
            {
                tasksControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            if (_tasksListView != null)
            {
                _tasksListView.AllowDrop = IsDropEnabled;
#if HAS_UNO_SKIA
                _tasksListView.HorizontalAlignment = HorizontalAlignment.Stretch;

                // On Skia Desktop, scrollbars typically reserve layout space (non-overlay),
                // so extra right padding from the template produces an oversized gap.
                _tasksListView.Padding = new Thickness(0);

                // Keep scrollbars visible (Auto) but avoid adding extra padding on top of
                // the reserved scrollbar width.
                _tasksListView.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
#endif
            }

            // Find header and close button for sizing
            FindHeaderElements();
            FindAddCardElements();
            UpdateTabStop();

#if KANBAN_DEBUG && HAS_UNO_SKIA
            DebugLog($"KANBAN_DEBUG: HeaderElements. GripFound={_columnGripElement != null} HeaderFound={_columnHeaderGrid != null} GripVisibility={_columnGripElement?.Visibility.ToString() ?? "<null>"} GripIsHitTestVisible={_columnGripElement?.IsHitTestVisible.ToString() ?? "<null>"} GripCanDrag={_columnGripElement?.CanDrag.ToString() ?? "<null>"}");
#endif
#if HAS_UNO_SKIA

            // Uno/Skia can report Loaded before the column's content is fully realized.
            // Retry finding the header parts once on the dispatcher if they weren't found yet.
            if (_columnGripElement == null && DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (!_isLoaded || _columnGripElement != null)
                        return;

                    FindHeaderElements();
                    UpdateColumnReorderState();
#if KANBAN_DEBUG
                    DebugLog($"KANBAN_DEBUG: HeaderElements (deferred). GripFound={_columnGripElement != null} HeaderFound={_columnHeaderGrid != null}");
#endif
                });
            }
#endif

#if KANBAN_DEBUG && HAS_UNO_SKIA
            if (!_isDebugHitTestProbeAttached)
            {
                AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnColumnHitTestProbePointerPressed), handledEventsToo: true);
                AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnColumnHitTestProbePointerReleased), handledEventsToo: true);
                AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(OnColumnHitTestProbePointerCaptureLost), handledEventsToo: true);
                _isDebugHitTestProbeAttached = true;
            }
#endif

            // Find parent FlowKanban if not already set
            if (ParentKanban == null)
            {
                var foundParent = FindParentKanban();
                if (foundParent != null)
                {
                    ParentKanban = foundParent;
                }
            }
            if (ParentKanban != null)
            {
                ParentKanban.RegisterColumn(this);
            }

            // Apply initial sizing
            if (ParentKanban != null)
            {
                ColumnSize = ParentKanban.BoardSize;
                ApplySizing();
            }
            else
            {
                // Even without a parent board, apply sizing once so task spacing/styles are initialized.
                ApplySizing();
            }

            AttachColumnData(ColumnData);
            UpdateCollapsedState();
            UpdateEditButtonVisibilityForPlatform();
            UpdateAddCardVisibility();
            UpdateFocusVisualState();
        }

        private void EnsureTasksParts()
        {
            if (_tasksItemsControl != null && _tasksDropIndicatorHost != null)
                return;

            var searchRoot = Content as DependencyObject ?? this;
            _tasksItemsControl ??= FindTasksItemsControl(searchRoot);
            _tasksListView = _tasksItemsControl as ListViewBase;
            _tasksItemsHost = _tasksItemsControl?.ItemsPanelRoot as Panel;
            _tasksDropIndicatorHost ??= FindChild<Canvas>(searchRoot, c => c.Name == "PART_TaskDropIndicatorHost");

            if (_tasksItemsControl is FrameworkElement tasksControl)
            {
                tasksControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            if (_tasksListView != null)
            {
#if HAS_UNO_SKIA
                _tasksListView.HorizontalAlignment = HorizontalAlignment.Stretch;

                // On Skia Desktop, scrollbars typically reserve layout space (non-overlay),
                // so extra right padding from the template produces an oversized gap.
                _tasksListView.Padding = new Thickness(0);
                _tasksListView.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
#endif
            }

            if (_tasksListView != null && !double.IsNaN(_taskItemSpacing))
            {
                ApplyTaskItemSpacing(_taskItemSpacing);
            }
        }

        private void FindHeaderElements()
        {
            var searchRoot = Content as DependencyObject ?? this;

            // Find the column header grid
            _columnHeaderGrid = FindChild<Grid>(searchRoot, g => g.Name == "PART_ColumnHeaderGrid");

            // Find the first TextBlock with FontWeight=Bold (the header)
            _headerText = FindChild<TextBlock>(searchRoot, tb => tb.Name == "PART_ColumnTitle");

            // Find the edit button (PART_RenameBtn)
            _editButton = FindChild<DaisyButton>(searchRoot, btn => btn.Name == "PART_RenameBtn");

            _collapseButton = FindChild<DaisyButton>(searchRoot, btn => btn.Name == "PART_CollapseBtn");
            _collapseIcon = FindChild<DaisyIconText>(searchRoot, icon => icon.Name == "PART_CollapseIcon");

            _columnGripElement = FindChild<Border>(searchRoot, b => b.Name == "PART_ColumnGrip");
            if (_columnGripElement != null)
            {
                _columnGripElement.DragStarting += OnColumnGripDragStarting;
#if HAS_UNO_SKIA
                _columnGripElement.PointerPressed += OnColumnGripPointerPressed;
                _columnGripElement.PointerMoved += OnColumnGripPointerMoved;
                _columnGripElement.PointerReleased += OnColumnGripPointerReleased;
                _columnGripElement.PointerCaptureLost += OnColumnGripPointerCaptureLost;

                _columnGripIconElement = FindChild<DaisyIconText>(_columnGripElement);
                if (_columnGripIconElement != null)
                {
                    _columnGripIconElement.PointerPressed += OnColumnGripPointerPressed;
                    _columnGripIconElement.PointerMoved += OnColumnGripPointerMoved;
                    _columnGripIconElement.PointerReleased += OnColumnGripPointerReleased;
                    _columnGripIconElement.PointerCaptureLost += OnColumnGripPointerCaptureLost;
                }
#endif
            }

            _taskCountBadge = FindChild<Border>(searchRoot, b => b.Name == "PART_TaskCountBadge");
            _taskCountText = FindChild<TextBlock>(searchRoot, tb => tb.Name == "PART_TaskCountText");
            _headerIconsPanel = FindChild<FrameworkElement>(searchRoot, panel => panel.Name == "PART_ColumnHeaderIcons");
            ApplyTaskCountBadgeTheme();
            UpdateTaskCountDisplay();
            UpdateCollapseIcon();

            // Wire up hover events on column header grid
            if (_columnHeaderGrid != null)
            {
                _columnHeaderGrid.PointerEntered += OnHeaderPointerEntered;
                _columnHeaderGrid.PointerExited += OnHeaderPointerExited;
                _columnHeaderGrid.PointerPressed += OnHeaderPointerPressed;
                _columnHeaderGrid.GotFocus += OnHeaderGotFocus;
                _columnHeaderGrid.LostFocus += OnHeaderLostFocus;
            }
        }

        private void FindAddCardElements()
        {
            var searchRoot = Content as DependencyObject ?? this;
            _addCardTop = FindChild<FlowKanbanAddCard>(searchRoot, card => card.Name == "PART_AddCardTop");
            _addCardBottom = FindChild<FlowKanbanAddCard>(searchRoot, card => card.Name == "PART_AddCardBottom");
        }

        internal FlowKanbanAddCard? GetVisibleAddCardControl()
        {
            if (_addCardTop == null && _addCardBottom == null)
            {
                FindAddCardElements();
            }

            if (_addCardTop?.Visibility == Visibility.Visible)
                return _addCardTop;

            if (_addCardBottom?.Visibility == Visibility.Visible)
                return _addCardBottom;

            return _addCardTop ?? _addCardBottom;
        }

        private void OnHeaderPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
                ShowHeaderButtons();
        }

        private void OnHeaderPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
                HideHeaderButtons();
        }

        private void OnHeaderPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
                return;

            ShowHeaderButtons();
            Focus(FocusState.Programmatic);
        }

        private void OnHeaderGotFocus(object sender, RoutedEventArgs e)
        {
            ShowHeaderButtons();
        }

        private void OnHeaderLostFocus(object sender, RoutedEventArgs e)
        {
            HideHeaderButtons();
        }

        private void ShowHeaderButtons()
        {
            if (IsMobilePlatform())
            {
                UpdateEditButtonVisibilityForPlatform();
                return;
            }

            if (_editButton != null)
            {
                _editButton.IsHitTestVisible = true;
                _editButton.Opacity = 0.7;
            }
        }

        private void HideHeaderButtons()
        {
            if (IsMobilePlatform())
                return;

            if (_editButton != null)
            {
                _editButton.Opacity = 0;
                _editButton.IsHitTestVisible = false;
            }
        }

        private void OnColumnGripDragStarting(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
        {
            if (ColumnData == null)
                return;

            args.Data.SetData(FlowKanban.ColumnDragDataFormat, ColumnData.Id);
            args.Data.Properties[FlowKanban.ColumnDragDataPropertyKey] = ColumnData.Id;
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }

#if HAS_UNO_SKIA
        private void AttachKanbanPointerReorderHandlers()
        {
            if (_isKanbanPointerReorderHandlersAttached)
            {
                return;
            }

            if (ParentKanban == null)
            {
                return;
            }

            _kanbanPointerMovedHandler ??= new PointerEventHandler(OnKanbanPointerMoved);
            _kanbanPointerReleasedHandler ??= new PointerEventHandler(OnKanbanPointerReleased);

            ParentKanban.AddHandler(UIElement.PointerMovedEvent, _kanbanPointerMovedHandler, handledEventsToo: true);
            ParentKanban.AddHandler(UIElement.PointerReleasedEvent, _kanbanPointerReleasedHandler, handledEventsToo: true);
            _isKanbanPointerReorderHandlersAttached = true;
        }

        private void DetachKanbanPointerReorderHandlers()
        {
            if (!_isKanbanPointerReorderHandlersAttached)
            {
                return;
            }

            if (ParentKanban == null)
            {
                _isKanbanPointerReorderHandlersAttached = false;
                return;
            }

            if (_kanbanPointerMovedHandler != null)
            {
                ParentKanban.RemoveHandler(UIElement.PointerMovedEvent, _kanbanPointerMovedHandler);
            }

            if (_kanbanPointerReleasedHandler != null)
            {
                ParentKanban.RemoveHandler(UIElement.PointerReleasedEvent, _kanbanPointerReleasedHandler);
            }

            _isKanbanPointerReorderHandlersAttached = false;
        }

        private void OnKanbanPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            HandlePointerReorderMoved(e);
        }

        private void OnKanbanPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            HandlePointerReorderReleased(e);
        }

        private void HandlePointerReorderMoved(PointerRoutedEventArgs e)
        {
            if (!_isPointerColumnReorderTracking)
                return;

            if (e.Pointer.PointerId != _pointerColumnReorderPointerId)
                return;

            if (ColumnData == null || ParentKanban == null)
                return;

            var current = e.GetCurrentPoint(ParentKanban).Position;
            var dx = current.X - _pointerColumnReorderStart.X;
            var dy = current.Y - _pointerColumnReorderStart.Y;

            if (!_isPointerColumnReorderActive)
            {
                const double threshold = 6;
                if (Math.Abs(dx) < threshold && Math.Abs(dy) < threshold)
                    return;

                _isPointerColumnReorderActive = true;
                SetDragHighlightBorder(isActive: true);
#if KANBAN_DEBUG
                DebugLog($"KANBAN_DEBUG: PointerReorder BeginPointerColumnReorder. ColumnId='{ColumnData.Id}'");
#endif
                ParentKanban.BeginPointerColumnReorder(ColumnData.Id);
            }

            ParentKanban.UpdatePointerColumnReorder(e);
            e.Handled = true;
        }

        private void HandlePointerReorderReleased(PointerRoutedEventArgs e)
        {
            if (!_isPointerColumnReorderTracking)
            {
                SetDragHighlightBorder(isActive: false);
                DetachKanbanPointerReorderHandlers();
                return;
            }

            if (e.Pointer.PointerId != _pointerColumnReorderPointerId)
                return;

            var captureElement = _pointerColumnReorderCaptureElement;
            _pointerColumnReorderCaptureElement = null;
            if (captureElement != null)
            {
                captureElement.ReleasePointerCapture(e.Pointer);
            }

            _isPointerColumnReorderTracking = false;

            if (_isPointerColumnReorderActive && ParentKanban != null)
            {
#if KANBAN_DEBUG
                DebugLog($"KANBAN_DEBUG: PointerReorder CompletePointerColumnReorder. ColumnId='{ColumnData?.Id ?? "<null>"}'");
#endif
                ParentKanban.CompletePointerColumnReorder(e);
            }

            _isPointerColumnReorderActive = false;
            SetDragHighlightBorder(isActive: false);
            DetachKanbanPointerReorderHandlers();
            e.Handled = true;
        }

#if KANBAN_DEBUG
        private static void DebugLog(string message)
        {
            try
            {
                var line = $"{DateTimeOffset.Now:O} [Kanban.Column] {message}{Environment.NewLine}";
                global::System.IO.File.AppendAllText(s_debugLogPath, line);
            }
            catch
            {
                // Ignore logging failures (debug-only diagnostics).
            }

            try
            {
                // May be disabled in Release; keep for completeness.
                FloweryLogging.Log(FloweryLogLevel.Debug, "Kanban.Column", message);
            }
            catch
            {
                // Ignore logging failures (debug-only diagnostics).
            }
        }

        private void DebugSetColumnBorderColor(Windows.UI.Color color, double thickness = 3)
        {
            if (!_debugColumnBorderOverrideActive)
            {
                _debugColumnBorderSavedBorderBrushLocalValue = ReadLocalValue(BorderBrushProperty);
                _debugColumnBorderSavedBorderThicknessLocalValue = ReadLocalValue(BorderThicknessProperty);
                _debugColumnBorderOverrideActive = true;
            }

            _debugColumnBorderBrush ??= new SolidColorBrush(color);
            _debugColumnBorderBrush.Color = color;

            BorderBrush = _debugColumnBorderBrush;
            BorderThickness = new Thickness(thickness);
        }

        private void DebugRestoreColumnBorder()
        {
            if (!_debugColumnBorderOverrideActive)
                return;

            if (_debugColumnBorderSavedBorderBrushLocalValue == DependencyProperty.UnsetValue)
            {
                ClearValue(BorderBrushProperty);
            }
            else if (_debugColumnBorderSavedBorderBrushLocalValue is Brush savedBrush)
            {
                BorderBrush = savedBrush;
            }
            else
            {
                ClearValue(BorderBrushProperty);
            }

            if (_debugColumnBorderSavedBorderThicknessLocalValue == DependencyProperty.UnsetValue)
            {
                ClearValue(BorderThicknessProperty);
            }
            else if (_debugColumnBorderSavedBorderThicknessLocalValue is Thickness savedThickness)
            {
                BorderThickness = savedThickness;
            }
            else
            {
                ClearValue(BorderThicknessProperty);
            }

            _debugColumnBorderOverrideActive = false;
            _debugColumnBorderSavedBorderBrushLocalValue = null;
            _debugColumnBorderSavedBorderThicknessLocalValue = null;
        }

        private void DebugSetColumnGripBackgroundColor(Windows.UI.Color color, double opacity = 0.25)
        {
            if (_columnGripElement is not Border gripBorder)
                return;

            if (!_debugColumnGripBackgroundOverrideActive)
            {
                _debugColumnGripSavedBackgroundLocalValue = gripBorder.ReadLocalValue(Border.BackgroundProperty);
                _debugColumnGripBackgroundOverrideActive = true;
            }

            _debugColumnGripBackgroundBrush ??= new SolidColorBrush(color) { Opacity = opacity };
            _debugColumnGripBackgroundBrush.Color = color;
            _debugColumnGripBackgroundBrush.Opacity = opacity;
            gripBorder.Background = _debugColumnGripBackgroundBrush;
        }

        private void DebugRestoreColumnGripBackground()
        {
            if (!_debugColumnGripBackgroundOverrideActive)
                return;

            if (_columnGripElement is not Border gripBorder)
                return;

            if (_debugColumnGripSavedBackgroundLocalValue == DependencyProperty.UnsetValue)
            {
                gripBorder.ClearValue(Border.BackgroundProperty);
            }
            else if (_debugColumnGripSavedBackgroundLocalValue is Brush savedBrush)
            {
                gripBorder.Background = savedBrush;
            }
            else
            {
                gripBorder.ClearValue(Border.BackgroundProperty);
            }

            _debugColumnGripBackgroundOverrideActive = false;
            _debugColumnGripSavedBackgroundLocalValue = null;
        }

        private void DebugSetHeaderBackgroundColor(Windows.UI.Color color, double opacity = 0.25)
        {
            if (_columnHeaderGrid is not Grid headerGrid)
                return;

            if (!_debugHeaderBackgroundOverrideActive)
            {
                _debugHeaderSavedBackgroundLocalValue = headerGrid.ReadLocalValue(Panel.BackgroundProperty);
                _debugHeaderBackgroundOverrideActive = true;
            }

            _debugHeaderBackgroundBrush ??= new SolidColorBrush(color) { Opacity = opacity };
            _debugHeaderBackgroundBrush.Color = color;
            _debugHeaderBackgroundBrush.Opacity = opacity;
            headerGrid.Background = _debugHeaderBackgroundBrush;
        }

        private void DebugRestoreHeaderBackground()
        {
            if (!_debugHeaderBackgroundOverrideActive)
                return;

            if (_columnHeaderGrid is not Grid headerGrid)
                return;

            if (_debugHeaderSavedBackgroundLocalValue == DependencyProperty.UnsetValue)
            {
                headerGrid.ClearValue(Panel.BackgroundProperty);
            }
            else if (_debugHeaderSavedBackgroundLocalValue is Brush savedBrush)
            {
                headerGrid.Background = savedBrush;
            }
            else
            {
                headerGrid.ClearValue(Panel.BackgroundProperty);
            }

            _debugHeaderBackgroundOverrideActive = false;
            _debugHeaderSavedBackgroundLocalValue = null;
        }

        private static bool IsDescendantOf(DependencyObject? element, DependencyObject? ancestor)
        {
            if (element == null || ancestor == null)
                return false;

            var current = element;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private void OnColumnHitTestProbePointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // This should fire even when inner controls handle the event.
            var original = e.OriginalSource as DependencyObject;

            var originalName = (original as FrameworkElement)?.Name;

            // If the column content wasn't realized when Loaded fired, the grip/header parts will be null.
            // Refresh them on-demand when we get the first real pointer interaction.
            if (_columnGripElement == null && _columnHeaderGrid == null)
            {
                FindHeaderElements();
                UpdateColumnReorderState();
#if KANBAN_DEBUG
                DebugLog($"KANBAN_DEBUG: HeaderElements (probe refresh). GripFound={_columnGripElement != null} HeaderFound={_columnHeaderGrid != null}");
#endif
            }

            var hitGrip = IsDescendantOf(original, _columnGripElement);
            var hitHeader = IsDescendantOf(original, _columnHeaderGrid);

#if KANBAN_DEBUG
            DebugLog($"KANBAN_DEBUG: HitTestProbe Pressed. OriginalSource='{original?.GetType().FullName ?? "<null>"}' OriginalName='{(string.IsNullOrWhiteSpace(originalName) ? "<null>" : originalName)}' HitGrip={hitGrip} HitHeader={hitHeader} GripFound={_columnGripElement != null} HeaderFound={_columnHeaderGrid != null} Handled={e.Handled}");
#endif

            // Big, obvious visuals so we can conclusively prove hit-testing:
            // - Green: click landed on grip (or its descendants)
            // - Gold: click landed in header, but NOT on grip
            // - HotPink: click landed elsewhere in the column
            var color = hitGrip
                ? Microsoft.UI.Colors.LimeGreen
                : hitHeader
                    ? Microsoft.UI.Colors.Gold
                    : Microsoft.UI.Colors.HotPink;

            DebugSetColumnBorderColor(color, thickness: 5);
            DebugSetHeaderBackgroundColor(color, opacity: 0.35);
            DebugSetColumnGripBackgroundColor(color, opacity: 0.35);
        }

        private void OnColumnHitTestProbePointerReleased(object sender, PointerRoutedEventArgs e)
        {
#if KANBAN_DEBUG
            DebugLog("KANBAN_DEBUG: HitTestProbe Released.");
#endif
            DebugRestoreHeaderBackground();
            DebugRestoreColumnGripBackground();
            DebugRestoreColumnBorder();
        }

        private void OnColumnHitTestProbePointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
#if KANBAN_DEBUG
            DebugLog("KANBAN_DEBUG: HitTestProbe CaptureLost.");
#endif
            DebugRestoreHeaderBackground();
            DebugRestoreColumnGripBackground();
            DebugRestoreColumnBorder();
        }

        private void OnColumnGripPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // If this never shows, the grip (or its icon) is not being hit-tested.
            DebugSetColumnGripBackgroundColor(Microsoft.UI.Colors.Gold, opacity: 0.18);
        }

        private void OnColumnGripPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_isPointerColumnReorderTracking)
                return;

            DebugRestoreColumnGripBackground();
            DebugRestoreColumnBorder();
        }

#endif
        private void OnColumnGripPointerPressed(object sender, PointerRoutedEventArgs e)
        {
#if KANBAN_DEBUG
            DebugLog($"KANBAN_DEBUG: Grip PointerPressed. Sender='{sender?.GetType().FullName ?? "<null>"}' ColumnId='{ColumnData?.Id ?? "<null>"}' ParentKanbanNull={ParentKanban == null} PointerId={e.Pointer.PointerId}");
#endif
            if (ColumnData == null)
                return;

            if (IsCollapsed || ParentKanban?.IsCompactLayoutEnabled == true)
                return;

            if (sender is not UIElement element)
                return;

            if (_isPointerColumnReorderTracking)
                return;

            _pointerColumnReorderPointerId = e.Pointer.PointerId;
            // Track start relative to the board so we can keep computing dx/dy even if
            // the pointer gets captured by other elements (e.g., ScrollViewer) on Skia.
            _pointerColumnReorderStart = ParentKanban != null
                ? e.GetCurrentPoint(ParentKanban).Position
                : e.GetCurrentPoint(element).Position;
            _isPointerColumnReorderTracking = true;
            _isPointerColumnReorderActive = false;

            AttachKanbanPointerReorderHandlers();
            _pointerColumnReorderCaptureElement = element;
            element.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void OnColumnGripPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            HandlePointerReorderMoved(e);
        }

        private void OnColumnGripPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            HandlePointerReorderReleased(e);
        }

        private void OnColumnGripPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            // IMPORTANT (Skia): capture is often stolen by the board ScrollViewer during a drag.
            // Do NOT cancel the reorder here; keep it alive and complete on PointerReleased at the board level.
#if KANBAN_DEBUG
            DebugLog($"KANBAN_DEBUG: Grip PointerCaptureLost. ColumnId='{ColumnData?.Id ?? "<null>"}' Tracking={_isPointerColumnReorderTracking} Active={_isPointerColumnReorderActive}");
#endif
            _pointerColumnReorderCaptureElement = null;

            if (!_isPointerColumnReorderTracking)
            {
                SetDragHighlightBorder(isActive: false);
                DetachKanbanPointerReorderHandlers();
                return;
            }

            // If reorder never activated, treat capture-loss as cancellation of the tracking gesture.
            if (!_isPointerColumnReorderActive)
            {
                _isPointerColumnReorderTracking = false;
                SetDragHighlightBorder(isActive: false);
                DetachKanbanPointerReorderHandlers();
            }
        }
#endif

        private void ApplyTaskCountBadgeTheme()
        {
            if (_taskCountBadge != null)
            {
                _taskCountBadge.Background = DaisyResourceLookup.GetBrush("DaisySecondaryBrush");
            }

            if (_taskCountText != null)
            {
                _taskCountText.Foreground = DaisyResourceLookup.GetBrush("DaisySecondaryContentBrush");
            }
        }

        private void UpdateTaskCountDisplay()
        {
            if (_taskCountText == null || _trackedColumnData == null)
                return;

            var totalCount = _trackedColumnData.Tasks.Count;
            var isFiltering = ParentKanban?.IsFilterActive == true;
            var display = isFiltering
                ? $"{GetFilteredTaskCount(_trackedColumnData.Tasks)}/{totalCount}"
                : _trackedColumnData.WipDisplay;

            if (!string.Equals(_taskCountText.Text, display, StringComparison.Ordinal))
            {
                _taskCountText.Text = display;
            }
        }

        private static int GetFilteredTaskCount(IEnumerable<FlowTask> tasks)
        {
            var count = 0;
            foreach (var task in tasks)
            {
                if (task.IsSearchMatch)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnColumnUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            if (_tasksListView != null)
            {
                _tasksListView.ContainerContentChanging -= OnTaskContainerContentChanging;
            }
            _realizedCards.Clear();
            // Clean up drop indicator
            HideDropIndicator();
            _isDragHighlightActive = false;
            SetDragHighlightBorder(isActive: false);

#if HAS_UNO_SKIA
#if KANBAN_DEBUG
            DebugLog($"KANBAN_DEBUG: Unloaded. ColumnId='{ColumnData?.Id ?? "<null>"}'");
#endif
            DetachKanbanPointerReorderHandlers();
#if KANBAN_DEBUG
            if (_isDebugHitTestProbeAttached)
            {
                RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnColumnHitTestProbePointerPressed));
                RemoveHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnColumnHitTestProbePointerReleased));
                RemoveHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(OnColumnHitTestProbePointerCaptureLost));
                _isDebugHitTestProbeAttached = false;
            }
#endif
#endif

            // Unsubscribe from parent when unloaded
            if (ParentKanban != null)
            {
                ParentKanban.UnregisterColumn(this);
                ParentKanban.BoardSizeChanged -= OnBoardSizeChanged;
                ParentKanban.ColumnWidthChanged -= OnColumnWidthChanged;
                ParentKanban.DragEnded -= OnParentDragEnded;
                ParentKanban.LaneGroupingChanged -= OnLaneGroupingChanged;
                ParentKanban.CompactLayoutChanged -= OnCompactLayoutChanged;
            }

            if (_columnGripElement != null)
            {
                _columnGripElement.DragStarting -= OnColumnGripDragStarting;
#if HAS_UNO_SKIA
                _columnGripElement.PointerPressed -= OnColumnGripPointerPressed;
                _columnGripElement.PointerMoved -= OnColumnGripPointerMoved;
                _columnGripElement.PointerReleased -= OnColumnGripPointerReleased;
                _columnGripElement.PointerCaptureLost -= OnColumnGripPointerCaptureLost;

                if (_columnGripIconElement != null)
                {
                    _columnGripIconElement.PointerPressed -= OnColumnGripPointerPressed;
                    _columnGripIconElement.PointerMoved -= OnColumnGripPointerMoved;
                    _columnGripIconElement.PointerReleased -= OnColumnGripPointerReleased;
                    _columnGripIconElement.PointerCaptureLost -= OnColumnGripPointerCaptureLost;
                    _columnGripIconElement = null;
                }
#endif
            }

            if (_columnHeaderGrid != null)
            {
                _columnHeaderGrid.PointerEntered -= OnHeaderPointerEntered;
                _columnHeaderGrid.PointerExited -= OnHeaderPointerExited;
                _columnHeaderGrid.PointerPressed -= OnHeaderPointerPressed;
                _columnHeaderGrid.GotFocus -= OnHeaderGotFocus;
                _columnHeaderGrid.LostFocus -= OnHeaderLostFocus;
            }

            DetachColumnData();
            _tasksItemsControl = null;
            _tasksListView = null;
            _tasksItemsHost = null;
            _tasksDropIndicatorHost = null;
            _addCardTop = null;
            _addCardBottom = null;
        }

        private FlowKanban? FindParentKanban()
        {
            DependencyObject? current = this;
            while (current != null)
            {
                current = VisualTreeHelper.GetParent(current);
                if (current is FlowKanban kanban)
                    return kanban;
            }
            return null;
        }

        private T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            return FindChild<T>(parent, null);
        }

        private T? FindChild<T>(DependencyObject parent, Func<T, bool>? predicate) where T : DependencyObject
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

        private ItemsControl? FindTasksItemsControl(DependencyObject parent)
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ItemsControl ic && ic.Name == "PART_TasksItemsControl")
                {
                    return ic;
                }
                var result = FindTasksItemsControl(child);
                if (result != null) return result;
            }
            return null;
        }

        #region LaneFilterId
        public static readonly DependencyProperty LaneFilterIdProperty =
            DependencyProperty.Register(
                nameof(LaneFilterId),
                typeof(string),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(null, OnLaneFilterIdChanged));

        public string? LaneFilterId
        {
            get => (string?)GetValue(LaneFilterIdProperty);
            set => SetValue(LaneFilterIdProperty, value);
        }

        private static void OnLaneFilterIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanColumn column)
            {
                column.RebuildTaskViews();
            }
        }
        #endregion

        #region ShowColumnHeader
        public static readonly DependencyProperty ShowColumnHeaderProperty =
            DependencyProperty.Register(
                nameof(ShowColumnHeader),
                typeof(bool),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(true, OnShowColumnHeaderChanged));

        public bool ShowColumnHeader
        {
            get => (bool)GetValue(ShowColumnHeaderProperty);
            set => SetValue(ShowColumnHeaderProperty, value);
        }

        private static void OnShowColumnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanColumn column)
            {
                column.UpdateTabStop();
            }
        }
        #endregion

        #region ShowTasks
        public static readonly DependencyProperty ShowTasksProperty =
            DependencyProperty.Register(
                nameof(ShowTasks),
                typeof(bool),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(true));

        public bool ShowTasks
        {
            get => (bool)GetValue(ShowTasksProperty);
            set => SetValue(ShowTasksProperty, value);
        }
        #endregion


        #region ShowAddCard
        public static readonly DependencyProperty ShowAddCardProperty =
            DependencyProperty.Register(
                nameof(ShowAddCard),
                typeof(bool),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(true, OnShowAddCardChanged));

        public bool ShowAddCard
        {
            get => (bool)GetValue(ShowAddCardProperty);
            set => SetValue(ShowAddCardProperty, value);
        }

        private static void OnShowAddCardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanColumn column)
            {
                column.UpdateAddCardVisibility();
            }
        }
        #endregion

        #region AddCardPlacement
        public static readonly DependencyProperty AddCardPlacementProperty =
            DependencyProperty.Register(
                nameof(AddCardPlacement),
                typeof(FlowKanbanAddCardPlacement),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(FlowKanbanAddCardPlacement.Bottom, OnAddCardPlacementChanged));

        public FlowKanbanAddCardPlacement AddCardPlacement
        {
            get => (FlowKanbanAddCardPlacement)GetValue(AddCardPlacementProperty);
            set => SetValue(AddCardPlacementProperty, value);
        }

        private static void OnAddCardPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanColumn column)
            {
                column.UpdateAddCardVisibility();
            }
        }
        #endregion

        #region AddCardVisibility
        public static readonly DependencyProperty ShowAddCardTopProperty =
            DependencyProperty.Register(
                nameof(ShowAddCardTop),
                typeof(bool),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(false));

        public bool ShowAddCardTop
        {
            get => (bool)GetValue(ShowAddCardTopProperty);
            private set => SetValue(ShowAddCardTopProperty, value);
        }

        public static readonly DependencyProperty ShowAddCardBottomProperty =
            DependencyProperty.Register(
                nameof(ShowAddCardBottom),
                typeof(bool),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(true));

        public bool ShowAddCardBottom
        {
            get => (bool)GetValue(ShowAddCardBottomProperty);
            private set => SetValue(ShowAddCardBottomProperty, value);
        }

        private void UpdateAddCardVisibility()
        {
            var placement = AddCardPlacement;
            var show = ShowAddCard;
            var showTop = show && (placement == FlowKanbanAddCardPlacement.Top || placement == FlowKanbanAddCardPlacement.Both);
            var showBottom = show && (placement == FlowKanbanAddCardPlacement.Bottom || placement == FlowKanbanAddCardPlacement.Both);

            ShowAddCardTop = showTop;
            ShowAddCardBottom = showBottom;
        }
        #endregion

        #region IsCollapsed
        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.Register(
                nameof(IsCollapsed),
                typeof(bool),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(false, OnIsCollapsedChanged));

        public bool IsCollapsed
        {
            get => (bool)GetValue(IsCollapsedProperty);
            set => SetValue(IsCollapsedProperty, value);
        }

        private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanColumn column)
            {
                column.UpdateCollapsedState();
            }
        }
        #endregion

        #region IsDropEnabled
        public static readonly DependencyProperty IsDropEnabledProperty =
            DependencyProperty.Register(
                nameof(IsDropEnabled),
                typeof(bool),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(true, OnIsDropEnabledChanged));

        public bool IsDropEnabled
        {
            get => (bool)GetValue(IsDropEnabledProperty);
            set => SetValue(IsDropEnabledProperty, value);
        }

        private static void OnIsDropEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanColumn column && e.NewValue is bool isEnabled)
            {
                column.AllowDrop = isEnabled;
                if (column._tasksListView != null)
                {
                    column._tasksListView.AllowDrop = isEnabled;
                }
                if (!isEnabled)
                {
                    column.HideDropIndicator();
                }
            }
        }
        #endregion

        #region ColumnData
        public static readonly DependencyProperty ColumnDataProperty =
            DependencyProperty.Register(
                nameof(ColumnData),
                typeof(FlowKanbanColumnData),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(null, OnColumnDataChanged));

        public FlowKanbanColumnData? ColumnData
        {
            get => (FlowKanbanColumnData?)GetValue(ColumnDataProperty);
            set => SetValue(ColumnDataProperty, value);
        }

        private static void OnColumnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanColumn column)
            {
                if (!column._isLoaded)
                    return;

                column.AttachColumnData(e.NewValue as FlowKanbanColumnData);
            }
        }
        #endregion

        #region ParentKanban
        public static readonly DependencyProperty ParentKanbanProperty =
            DependencyProperty.Register(
                nameof(ParentKanban),
                typeof(FlowKanban),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(null, OnParentKanbanChanged));

        public FlowKanban? ParentKanban
        {
            get => (FlowKanban?)GetValue(ParentKanbanProperty);
            set => SetValue(ParentKanbanProperty, value);
        }

        private static void OnParentKanbanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanColumn column)
            {
                // Unsubscribe from old parent
                if (e.OldValue is FlowKanban oldKanban)
                {
                    oldKanban.UnregisterColumn(column);
                    oldKanban.BoardSizeChanged -= column.OnBoardSizeChanged;
                    oldKanban.ColumnWidthChanged -= column.OnColumnWidthChanged;
                    oldKanban.DragEnded -= column.OnParentDragEnded;
                    oldKanban.LaneGroupingChanged -= column.OnLaneGroupingChanged;
                    oldKanban.CompactLayoutChanged -= column.OnCompactLayoutChanged;
                    oldKanban.SearchFilterChanged -= column.OnSearchFilterChanged;
                }

                // Subscribe to new parent
                if (e.NewValue is FlowKanban newKanban)
                {
                    newKanban.BoardSizeChanged += column.OnBoardSizeChanged;
                    newKanban.ColumnWidthChanged += column.OnColumnWidthChanged;
                    newKanban.DragEnded += column.OnParentDragEnded;
                    newKanban.LaneGroupingChanged += column.OnLaneGroupingChanged;
                    newKanban.CompactLayoutChanged += column.OnCompactLayoutChanged;
                    newKanban.SearchFilterChanged += column.OnSearchFilterChanged;
                    column.ColumnSize = newKanban.BoardSize;
                    column.ApplySizing();
                    column.UpdateCollapsedState();
                    column.UpdateColumnReorderState();
                    column.RebuildTaskViews();
                    if (column._isLoaded)
                    {
                        newKanban.RegisterColumn(column);
                    }
                }
            }
        }

        private void OnBoardSizeChanged(object? sender, DaisySize newSize)
        {
            ColumnSize = newSize;
            ApplySizing();
        }

        private void OnColumnWidthChanged(object? sender, double newWidth)
        {
            ApplySizing();
        }

        private void OnParentDragEnded(object? sender, EventArgs e)
        {
            // Ensure drop indicator is cleaned up when any drag ends
            HideDropIndicator();

            _isDragHighlightActive = false;
            SetDragHighlightBorder(isActive: false);
            UpdateFocusVisualState();
        }

        private void OnLaneGroupingChanged(object? sender, EventArgs e)
        {
            RebuildTaskViews();
        }

        private void OnSearchFilterChanged(object? sender, EventArgs e)
        {
            RebuildTaskViews();
            UpdateTaskCountDisplay();
        }

        private void OnCompactLayoutChanged(object? sender, bool isCompact)
        {
            ApplySizing();
            UpdateColumnReorderState();
            UpdateCollapseIcon();
            UpdateCollapsedHeaderVisibility();
        }
        #endregion

        #region ColumnSize
        public static readonly DependencyProperty ColumnSizeProperty =
            DependencyProperty.Register(
                nameof(ColumnSize),
                typeof(DaisySize),
                typeof(FlowKanbanColumn),
                new PropertyMetadata(DaisySize.Medium, (d, _) => ((FlowKanbanColumn)d).ApplySizing()));

        /// <summary>
        /// The size tier for this column's visual elements.
        /// </summary>
        public DaisySize ColumnSize
        {
            get => (DaisySize)GetValue(ColumnSizeProperty);
            set => SetValue(ColumnSizeProperty, value);
        }
        #endregion

        private void AttachColumnData(FlowKanbanColumnData? columnData)
        {
            if (ReferenceEquals(_trackedColumnData, columnData))
                return;

            DetachColumnData();
            _trackedColumnData = columnData;

            if (_trackedColumnData == null)
            {
                _taskViews.Clear();
                return;
            }

            _trackedColumnData.PropertyChanged += OnColumnDataPropertyChanged;
            AttachTasksCollection(_trackedColumnData.Tasks);
            RebuildTaskViews();
            UpdateAutomationProperties();
            UpdateTaskCountDisplay();
        }

        private void DetachColumnData()
        {
            if (_trackedColumnData == null)
                return;

            _trackedColumnData.PropertyChanged -= OnColumnDataPropertyChanged;
            DetachTasksCollection();
            _trackedColumnData = null;
            _taskViews.Clear();
            UpdateAutomationProperties();
        }

        private void OnColumnDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(FlowKanbanColumnData.Tasks), StringComparison.Ordinal))
            {
                AttachTasksCollection(_trackedColumnData?.Tasks);
                RebuildTaskViews();
                UpdateAutomationProperties();
                UpdateTaskCountDisplay();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowKanbanColumnData.IsCollapsed), StringComparison.Ordinal)
                     || string.Equals(e.PropertyName, nameof(FlowKanbanColumnData.DisplayTitle), StringComparison.Ordinal))
            {
                UpdateCollapsedState();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowKanbanColumnData.Title), StringComparison.Ordinal))
            {
                UpdateAutomationProperties();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowKanbanColumnData.WipDisplay), StringComparison.Ordinal))
            {
                UpdateTaskCountDisplay();
            }
        }

        private void UpdateAutomationProperties()
        {
            if (_trackedColumnData == null)
            {
                ClearValue(AutomationProperties.NameProperty);
                ClearValue(AutomationProperties.AutomationIdProperty);
                return;
            }

            AutomationProperties.SetName(this, _trackedColumnData.Title);
            AutomationProperties.SetAutomationId(this, $"kanban-column-{_trackedColumnData.Id}");
        }

        private void UpdateTabStop()
        {
            IsTabStop = ShowColumnHeader;
        }

        private void AttachTasksCollection(ObservableCollection<FlowTask>? tasks)
        {
            if (ReferenceEquals(_trackedTasksCollection, tasks))
                return;

            DetachTasksCollection();
            _trackedTasksCollection = tasks;

            if (_trackedTasksCollection == null)
                return;

            _trackedTasksCollection.CollectionChanged += OnTasksCollectionChanged;

            foreach (var task in _trackedTasksCollection)
            {
                TrackViewTask(task);
            }
        }

        private void DetachTasksCollection()
        {
            if (_trackedTasksCollection != null)
            {
                _trackedTasksCollection.CollectionChanged -= OnTasksCollectionChanged;
            }

            foreach (var task in _trackedViewTasks)
            {
                task.PropertyChanged -= OnTaskPropertyChanged;
            }

            _trackedViewTasks.Clear();
            _trackedTasksCollection = null;
        }

        private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                var tasks = _trackedTasksCollection;
                DetachTasksCollection();
                AttachTasksCollection(tasks);
                RebuildTaskViews();
                UpdateTaskCountDisplay();
                return;
            }

            if (e.OldItems != null)
            {
                foreach (FlowTask task in e.OldItems)
                {
                    UntrackViewTask(task);
                }
            }

            if (e.NewItems != null)
            {
                foreach (FlowTask task in e.NewItems)
                {
                    TrackViewTask(task);
                }
            }

            RebuildTaskViews();
            UpdateTaskCountDisplay();
        }

        private void TrackViewTask(FlowTask task)
        {
            if (!_trackedViewTasks.Add(task))
                return;

            task.PropertyChanged += OnTaskPropertyChanged;
        }

        private void UntrackViewTask(FlowTask task)
        {
            if (!_trackedViewTasks.Remove(task))
                return;

            task.PropertyChanged -= OnTaskPropertyChanged;
        }

        private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(FlowTask.LaneId), StringComparison.Ordinal))
            {
                RebuildTaskViews();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowTask.IsSearchMatch), StringComparison.Ordinal))
            {
                UpdateTaskCountDisplay();
            }
        }

        private void ApplySizing()
        {
            var isCollapsed = IsEffectiveCollapsed;

            // Column sizing
            if (ParentKanban?.IsCompactLayoutEnabled == true)
            {
                ClearValue(WidthProperty);
                Padding = DaisyResourceLookup.GetKanbanColumnPadding(ColumnSize);
                if (isCollapsed)
                {
                    var headerFontSize = DaisyResourceLookup.GetKanbanColumnHeaderFontSize(ColumnSize);
                    var padding = Padding;
                    MinHeight = headerFontSize + padding.Top + padding.Bottom + 12;
                }
                else
                {
                    ClearValue(MinHeightProperty);
                }
            }
            else
            {
                Width = isCollapsed ? GetCollapsedColumnWidth(ColumnSize) : GetExpandedColumnWidth();
                Padding = isCollapsed ? GetCollapsedColumnPadding(ColumnSize) : DaisyResourceLookup.GetKanbanColumnPadding(ColumnSize);
                ClearValue(MinHeightProperty);
            }
            CornerRadius = DaisyResourceLookup.GetKanbanColumnCornerRadius(ColumnSize);

            // Header text sizing
            if (_headerText != null)
            {
                _headerText.FontSize = DaisyResourceLookup.GetKanbanColumnHeaderFontSize(ColumnSize);
            }

            // Update task card spacing
            ApplyTaskItemSpacing(DaisyResourceLookup.GetKanbanCardSpacing(ColumnSize));
        }

        private void ApplyTaskItemSpacing(double spacing)
        {
            if (_tasksListView == null)
            {
                _taskItemSpacing = spacing;
                return;
            }

            if (_tasksItemContainerStyle != null
                && ReferenceEquals(_tasksListView.ItemContainerStyle, _tasksItemContainerStyle)
                && Math.Abs(_taskItemSpacing - spacing) < 0.1)
                return;

            _taskItemSpacing = spacing;

            var itemStyle = new Style(typeof(ListViewItem));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
            itemStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            itemStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, spacing)));
            _tasksItemContainerStyle = itemStyle;
            _tasksListView.ItemContainerStyle = itemStyle;
        }

        private void CacheCollapsedDefaults()
        {
            _baseShowTasks ??= ShowTasks;
            _baseShowAddCard ??= ShowAddCard;
        }

        private void UpdateCollapsedState()
        {
            if (!_isLoaded)
                return;

            CacheCollapsedDefaults();

            var isCollapsed = IsEffectiveCollapsed;

            if (isCollapsed)
            {
                ShowTasks = false;
                ShowAddCard = false;
            }
            else
            {
                if (_baseShowTasks.HasValue)
                    ShowTasks = _baseShowTasks.Value;
                if (_baseShowAddCard.HasValue)
                    ShowAddCard = _baseShowAddCard.Value;
            }

            ApplySizing();
            UpdateCollapseIcon();
            UpdateCollapsedHeaderVisibility();
        }

        private void UpdateHeaderLayout()
        {
            // Header layout is handled in XAML with separate expanded/collapsed containers.
        }

        private void UpdateCollapsedHeaderVisibility()
        {
            UpdateColumnReorderState();

            var visibility = IsEffectiveCollapsed ? Visibility.Collapsed : Visibility.Visible;

            if (_editButton != null)
            {
                _editButton.Visibility = visibility;
            }

            if (_headerIconsPanel != null)
            {
                _headerIconsPanel.Visibility = visibility;
            }

            if (_taskCountBadge != null)
            {
                _taskCountBadge.Visibility = visibility;
            }

            UpdateEditButtonVisibilityForPlatform();

            if (IsEffectiveCollapsed && _columnGripElement != null)
            {
                _columnGripElement.Visibility = Visibility.Collapsed;
                _columnGripElement.CanDrag = false;
                _columnGripElement.IsHitTestVisible = false;
            }
        }

        private void UpdateEditButtonVisibilityForPlatform()
        {
            if (_editButton == null)
                return;

            if (!IsMobilePlatform())
                return;

            if (_editButton.Visibility != Visibility.Visible)
                return;

            _editButton.IsHitTestVisible = true;
            _editButton.Opacity = 0.7;
        }

        private static bool IsMobilePlatform()
        {
            return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        }

        private void UpdateColumnReorderState()
        {
            if (_columnGripElement == null)
                return;

            var reorderEnabled = ParentKanban?.IsCompactLayoutEnabled != true;
            var isVisible = !IsEffectiveCollapsed && reorderEnabled;
            _columnGripElement.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
#if HAS_UNO_SKIA
            // On Skia we use the pointer-based column reorder path; enabling CanDrag here can
            // interfere with Pointer* hit-testing/dispatch.
            _columnGripElement.CanDrag = false;
#else
            _columnGripElement.CanDrag = isVisible;
#endif
            _columnGripElement.IsHitTestVisible = isVisible;
        }

        private void UpdateCollapseIcon()
        {
            if (_collapseIcon == null)
                return;

            var isCompact = ParentKanban?.IsCompactLayoutEnabled == true;
            var isCollapsed = IsEffectiveCollapsed;
            var iconKey = isCompact
                ? (isCollapsed ? "DaisyIconChevronDown" : "DaisyIconChevronUp")
                : (isCollapsed ? "DaisyIconChevronRight" : "DaisyIconChevronLeft");
            _collapseIcon.IconData = FloweryPathHelpers.GetIconPathData(iconKey) ?? _collapseIcon.IconData;
        }

        private double GetExpandedColumnWidth()
        {
            return ParentKanban?.ColumnWidth ?? FlowKanban.DefaultColumnWidth;
        }

        private static double GetCollapsedColumnWidth(DaisySize size)
        {
            // Ensure the collapsed rail is wide enough to show the expand button + border/padding.
            // Skia is less forgiving with ultra-tight layouts.
            return size switch
            {
                DaisySize.ExtraSmall => 40,
                DaisySize.Small => 44,
                DaisySize.Medium => 48,
                DaisySize.Large => 52,
                DaisySize.ExtraLarge => 56,
                _ => 48
            };
        }

        private static Thickness GetCollapsedColumnPadding(DaisySize size)
        {
            return new Thickness(2);
        }

        private void RebuildTaskViews()
        {
            _taskViewBuildVersion++;
            _taskViewBuildState = null;

            if (!ShouldStaggerTaskViewBuild())
            {
                BuildTaskViewsImmediate();
                return;
            }

            ScheduleTaskViewRebuild();
        }

        private bool ShouldStaggerTaskViewBuild()
        {
            return ParentKanban?.EnableStaggeredTaskRendering == true;
        }

        private void BuildTaskViewsImmediate()
        {
            _taskViews.Clear();

            if (!TryCreateTaskViewBuildState(out var state) || state == null)
                return;

            BuildTaskViewsBatch(state, int.MaxValue);
        }

        private void ScheduleTaskViewRebuild()
        {
            if (_taskViewBuildPending)
                return;

            if (DispatcherQueue == null)
            {
                BuildTaskViewsImmediate();
                return;
            }

            _taskViewBuildPending = true;
            var version = _taskViewBuildVersion;
            DispatcherQueue.TryEnqueue(() =>
            {
                _taskViewBuildPending = false;
                if (version != _taskViewBuildVersion)
                {
                    ScheduleTaskViewRebuild();
                    return;
                }

                _taskViews.Clear();

                if (!TryCreateTaskViewBuildState(out var state) || state == null)
                    return;

                _taskViewBuildState = state;
                EnqueueTaskViewBatch(version);
            });
        }

        private void EnqueueTaskViewBatch(int version)
        {
            if (DispatcherQueue == null)
            {
                DrainTaskViewBatches(version);
                return;
            }

            DispatcherQueue.TryEnqueue(() => DrainTaskViewBatches(version));
        }

        private void DrainTaskViewBatches(int version)
        {
            if (version != _taskViewBuildVersion)
                return;

            if (_taskViewBuildState == null)
                return;

            var state = _taskViewBuildState;
            var isComplete = BuildTaskViewsBatch(state, TaskViewStaggerBatchSize);
            if (version != _taskViewBuildVersion)
                return;

            if (isComplete)
            {
                _taskViewBuildState = null;
                return;
            }

            EnqueueTaskViewBatch(version);
        }

        private bool TryCreateTaskViewBuildState(out TaskViewBuildState? state)
        {
            state = null;
            if (_trackedColumnData == null)
                return false;

            var groupingEnabled = ParentKanban?.IsLaneGroupingEnabled == true;
            var laneFilterId = string.IsNullOrWhiteSpace(LaneFilterId) ? null : LaneFilterId;
            var filteringEnabled = groupingEnabled && laneFilterId != null;
            var filterIsUnassigned = filteringEnabled && IsUnassignedLaneId(laneFilterId);
            Dictionary<string, FlowKanbanLane>? laneLookup = null;
            if (groupingEnabled && ParentKanban != null)
            {
                laneLookup = new Dictionary<string, FlowKanbanLane>(StringComparer.Ordinal);
                foreach (var lane in ParentKanban.Board.Lanes)
                {
                    if (string.IsNullOrWhiteSpace(lane.Id))
                        continue;

                    if (!laneLookup.ContainsKey(lane.Id))
                    {
                        laneLookup.Add(lane.Id, lane);
                    }
                }
            }

            var tasks = new List<FlowTask>(_trackedColumnData.Tasks);
            state = new TaskViewBuildState(tasks, groupingEnabled, filteringEnabled, filterIsUnassigned, laneFilterId, laneLookup);
            return true;
        }

        private bool BuildTaskViewsBatch(TaskViewBuildState state, int maxItems)
        {
            var added = 0;
            while (state.TaskIndex < state.Tasks.Count)
            {
                var task = state.Tasks[state.TaskIndex];
                state.TaskIndex++;

                if (!task.IsSearchMatch)
                    continue;

                FlowKanbanLane? lane = null;
                string? laneId = null;

                if (state.FilteringEnabled)
                {
                    var filterId = state.LaneFilterId!;
                    var rawLaneId = task.LaneId;
                    if (state.FilterIsUnassigned)
                    {
                        var missingLane = !string.IsNullOrWhiteSpace(rawLaneId)
                                          && state.LaneLookup != null
                                          && !state.LaneLookup.ContainsKey(rawLaneId);
                        if (!IsUnassignedLaneId(rawLaneId) && !missingLane)
                            continue;

                        laneId = FlowKanban.UnassignedLaneId;
                        state.UnassignedLane ??= new FlowKanbanLane
                        {
                            Id = FlowKanban.UnassignedLaneId,
                            Title = FloweryLocalization.GetStringInternal("Kanban_Lanes_Unassigned")
                        };
                        lane = state.UnassignedLane;
                    }
                    else
                    {
                        if (!string.Equals(rawLaneId, filterId, StringComparison.Ordinal))
                            continue;

                        laneId = filterId;
                        if (state.LaneLookup != null && state.LaneLookup.TryGetValue(filterId, out var matchedLane))
                        {
                            lane = matchedLane;
                        }
                    }

                    _taskViews.Add(new FlowKanbanTaskView(task, lane, showLaneHeader: false));
                }
                else
                {
                    if (state.GroupingEnabled)
                    {
                        var rawLaneId = task.LaneId;
                        if (IsUnassignedLaneId(rawLaneId))
                        {
                            laneId = FlowKanban.UnassignedLaneId;
                            state.UnassignedLane ??= new FlowKanbanLane
                            {
                                Id = FlowKanban.UnassignedLaneId,
                                Title = FloweryLocalization.GetStringInternal("Kanban_Lanes_Unassigned")
                            };
                            lane = state.UnassignedLane;
                        }
                        else if (state.LaneLookup != null && !string.IsNullOrWhiteSpace(rawLaneId)
                                 && state.LaneLookup.TryGetValue(rawLaneId, out var matchedLane))
                        {
                            lane = matchedLane;
                            laneId = matchedLane.Id;
                        }
                        else
                        {
                            laneId = FlowKanban.UnassignedLaneId;
                            state.UnassignedLane ??= new FlowKanbanLane
                            {
                                Id = FlowKanban.UnassignedLaneId,
                                Title = FloweryLocalization.GetStringInternal("Kanban_Lanes_Unassigned")
                            };
                            lane = state.UnassignedLane;
                        }
                    }

                    var showHeader = state.GroupingEnabled
                        && laneId != null
                        && !string.Equals(state.PreviousLaneId, laneId, StringComparison.Ordinal);

                    _taskViews.Add(new FlowKanbanTaskView(task, lane, showHeader));
                    state.PreviousLaneId = laneId;
                }

                added++;
                if (added >= maxItems)
                    break;
            }

            return state.TaskIndex >= state.Tasks.Count;
        }

        private sealed class TaskViewBuildState
        {
            public TaskViewBuildState(
                IReadOnlyList<FlowTask> tasks,
                bool groupingEnabled,
                bool filteringEnabled,
                bool filterIsUnassigned,
                string? laneFilterId,
                Dictionary<string, FlowKanbanLane>? laneLookup)
            {
                Tasks = tasks;
                GroupingEnabled = groupingEnabled;
                FilteringEnabled = filteringEnabled;
                FilterIsUnassigned = filterIsUnassigned;
                LaneFilterId = laneFilterId;
                LaneLookup = laneLookup;
            }

            public IReadOnlyList<FlowTask> Tasks { get; }
            public int TaskIndex { get; set; }
            public bool GroupingEnabled { get; }
            public bool FilteringEnabled { get; }
            public bool FilterIsUnassigned { get; }
            public string? LaneFilterId { get; }
            public Dictionary<string, FlowKanbanLane>? LaneLookup { get; }
            public FlowKanbanLane? UnassignedLane { get; set; }
            public string? PreviousLaneId { get; set; }
        }

        private void EnsureDropIndicator()
        {
            if (_dropIndicator == null)
            {
                _dropIndicator = new Rectangle
                {
                    Height = 4,
                    RadiusX = 2,
                    RadiusY = 2,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    IsHitTestVisible = false
                };
                _dropIndicator.Fill = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
            }
        }

        private Panel? GetTasksItemsHost()
        {
            if (_tasksItemsControl == null)
                return null;

            _tasksItemsHost = _tasksItemsControl.ItemsPanelRoot as Panel;
            return _tasksItemsHost;
        }

        private Canvas? GetDropIndicatorHost()
        {
            if (_tasksDropIndicatorHost == null)
            {
                _tasksDropIndicatorHost = FindChild<Canvas>(this, c => c.Name == "PART_TaskDropIndicatorHost");
            }

            return _tasksDropIndicatorHost;
        }

        private void ShowDropIndicator(int index, double indicatorY)
        {
            var dropHost = GetDropIndicatorHost();
            if (dropHost == null)
                return;

            EnsureDropIndicator();
            if (_dropIndicator == null) return;

            // Remove from current position if already in tree
            if (_dropIndicator.Parent is Panel oldParent)
            {
                oldParent.Children.Remove(_dropIndicator);
            }

            var hostWidth = dropHost.ActualWidth;
            if (hostWidth <= 0 && _tasksItemsControl != null)
            {
                hostWidth = _tasksItemsControl.ActualWidth;
            }

            _dropIndicator.Width = Math.Max(0, hostWidth);
            Canvas.SetLeft(_dropIndicator, 0);
            Canvas.SetTop(_dropIndicator, Math.Max(0, indicatorY - (_dropIndicator.Height / 2)));

            if (!dropHost.Children.Contains(_dropIndicator))
            {
                dropHost.Children.Add(_dropIndicator);
            }

            _currentDropIndex = index;
            _currentDropIndicatorY = indicatorY;
        }

        private void HideDropIndicator()
        {
            if (_dropIndicator != null)
            {
                // Try removing from parent
                if (_dropIndicator.Parent is Panel parent)
                {
                    parent.Children.Remove(_dropIndicator);
                }

                _tasksDropIndicatorHost?.Children.Remove(_dropIndicator);
            }

            _currentDropIndex = -1;
            _currentDropIndicatorY = double.NaN;
            _draggedTaskId = null;
        }

        private void OnDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (!IsDropEnabled)
                return;
            if (!IsTaskDragData(e.DataView))
                return;

            EnsureTasksParts();

            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.DragUIOverride.Caption = FloweryLocalization.GetStringInternal("Kanban_Drag_MoveHere");
            e.DragUIOverride.IsCaptionVisible = false; // Hide caption, we show indicator instead
            e.DragUIOverride.IsGlyphVisible = false;
            _isDragHighlightActive = true;
            SetDragHighlightBorder(isActive: true);

            // Get the dragged task ID if not already cached
            if (_draggedTaskId == null && e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                // We can't await here, so try to get it synchronously if available
                try
                {
                    var textTask = e.DataView.GetTextAsync();
                    if (textTask.Status == Windows.Foundation.AsyncStatus.Completed)
                    {
                        _draggedTaskId = textTask.GetResults();
                    }
                }
                catch { /* Ignore if not available yet */ }
            }

            // Calculate and show drop indicator
            if (_tasksItemsControl != null && ColumnData != null)
            {
                var dropPosition = e.GetPosition(_tasksItemsControl);
                int insertIndex = CalculateInsertIndex(dropPosition.Y, out var indicatorY);

                if (insertIndex != _currentDropIndex || Math.Abs(indicatorY - _currentDropIndicatorY) > 0.5)
                {
                    ShowDropIndicator(insertIndex, indicatorY);
                }
            }

            e.Handled = true;
        }

        private void OnDragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (!IsDropEnabled)
                return;
            if (!IsTaskDragData(e.DataView))
                return;

            // Reset visual feedback
            HideDropIndicator();
            _isDragHighlightActive = false;
            SetDragHighlightBorder(isActive: false);
            UpdateFocusVisualState();

            e.Handled = true;
        }

        private async void OnDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (!IsDropEnabled)
                return;
            if (!IsTaskDragData(e.DataView))
                return;

            EnsureTasksParts();

            // Capture the drop index before hiding indicator
            int insertIndex = _currentDropIndex;

            // Reset visual feedback
            HideDropIndicator();
            _isDragHighlightActive = false;
            SetDragHighlightBorder(isActive: false);
            UpdateFocusVisualState();

            if (ColumnData == null || ParentKanban == null)
                return;

            // Get the dragged task ID from the data package
            var taskId = GetTaskIdFromDataView(e.DataView);
            if (string.IsNullOrWhiteSpace(taskId)
                && e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                taskId = await e.DataView.GetTextAsync();
            }

            if (!string.IsNullOrWhiteSpace(taskId))
            {
                // Find the task in any column
                FlowTask? draggedTask = null;
                FlowKanbanColumnData? sourceColumn = null;
                int sourceIndex = -1;

                foreach (var column in ParentKanban.Board.Columns)
                {
                    for (int i = 0; i < column.Tasks.Count; i++)
                    {
                        if (column.Tasks[i].Id == taskId)
                        {
                            draggedTask = column.Tasks[i];
                            sourceColumn = column;
                            sourceIndex = i;
                            break;
                        }
                    }
                    if (draggedTask != null) break;
                }

                if (draggedTask == null || sourceColumn == null)
                    return;

                // Use the calculated position, or recalculate if needed
                if (insertIndex < 0)
                {
                    var dropTarget = (UIElement?)_tasksItemsControl ?? this;
                    var dropPosition = e.GetPosition(dropTarget);
                    insertIndex = CalculateInsertIndex(dropPosition.Y, out _);
                }

                var targetIndex = insertIndex;
                if (sourceColumn == ColumnData && targetIndex == sourceIndex)
                    return;

                var manager = new FlowKanbanManager(ParentKanban, autoAttach: false);
                int? moveIndex = targetIndex >= 0 ? targetIndex : null;
                var targetLaneId = LaneFilterId;
                var result = manager.TryMoveTaskWithWipEnforcement(draggedTask, ColumnData, moveIndex, targetLaneId, enforceHard: false);
                if (result == MoveResult.AllowedWithWipWarning)
                {
                    ParentKanban.ShowWipWarning(ColumnData, targetLaneId);
                }
            }

            e.Handled = true;
        }

        private static bool IsTaskDragData(Windows.ApplicationModel.DataTransfer.DataPackageView dataView)
        {
            if (dataView.Properties.TryGetValue(FlowKanban.TaskDragDataPropertyKey, out var value)
                && !string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return true;
            }

            return dataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text);
        }

        private static string? GetTaskIdFromDataView(Windows.ApplicationModel.DataTransfer.DataPackageView dataView)
        {
            if (dataView.Properties.TryGetValue(FlowKanban.TaskDragDataPropertyKey, out var value))
            {
                return value as string ?? value?.ToString();
            }

            return null;
        }

        private void OnColumnGotFocus(object sender, RoutedEventArgs e)
        {
            UpdateFocusVisualState();
        }

        private void OnColumnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Expand-only gesture: never collapse via double tap/click.
            if (!IsEffectiveCollapsed)
                return;

            if (ColumnData != null)
            {
                ColumnData.IsCollapsed = false;
            }
            IsCollapsed = false;
            e.Handled = true;
        }

        private void OnColumnLostFocus(object sender, RoutedEventArgs e)
        {
            if (IsFocusWithin())
                return;

            HideHeaderButtons();
            UpdateFocusVisualState();
        }

        internal bool HandleTaskSelection(FlowTask task, bool isRangeSelection, bool isToggleSelection)
        {
            if (ParentKanban == null)
                return false;

            var selectableTasks = GetSelectableTasks();
            if (selectableTasks.Count == 0)
                return false;

            if (isRangeSelection && _lastSelectedTask != null)
            {
                var startIndex = selectableTasks.IndexOf(_lastSelectedTask);
                var endIndex = selectableTasks.IndexOf(task);
                if (startIndex >= 0 && endIndex >= 0)
                {
                    if (!isToggleSelection)
                    {
                        ParentKanban.DeselectAllTasks();
                    }

                    var from = Math.Min(startIndex, endIndex);
                    var to = Math.Max(startIndex, endIndex);
                    for (var i = from; i <= to; i++)
                    {
                        selectableTasks[i].IsSelected = true;
                    }

                    _lastSelectedTask = task;
                    return true;
                }
            }

            if (!isToggleSelection)
            {
                ParentKanban.DeselectAllTasks();
                task.IsSelected = true;
            }
            else
            {
                task.IsSelected = !task.IsSelected;
            }

            _lastSelectedTask = task;
            return true;
        }

        private List<FlowTask> GetSelectableTasks()
        {
            var tasks = new List<FlowTask>();
            if (!ShowTasks)
                return tasks;

            foreach (var view in TaskViews)
            {
                if (view.Task != null && view.Task.IsSearchMatch)
                {
                    tasks.Add(view.Task);
                }
            }

            return tasks;
        }

        private void UpdateFocusVisualState()
        {
            if (_isDragHighlightActive)
                return;
#if HAS_UNO_SKIA
            if (_isPointerColumnReorderActive)
                return;
#endif

            var shouldHighlight = IsFocusWithin() && IsKeyboardFocusActive();
            if (shouldHighlight == _isKeyboardFocusVisible)
                return;

            _isKeyboardFocusVisible = shouldHighlight;
            VisualStateManager.GoToState(this, shouldHighlight ? "Focused" : "Unfocused", true);
        }

        private bool IsKeyboardFocusActive()
        {
            if (XamlRoot == null)
                return false;

            var focused = FocusManager.GetFocusedElement(XamlRoot) as UIElement;
            return focused?.FocusState is FocusState.Keyboard or FocusState.Programmatic;
        }

        private bool IsFocusWithin()
        {
            if (XamlRoot == null)
                return false;

            var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (focused == null)
                return false;

            return FindAncestor<FlowKanbanColumn>(focused) == this;
        }

        private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
        {
            var current = element;
            while (current != null)
            {
                if (current is T match)
                    return match;

                current = VisualTreeHelper.GetParent(current);
            }

            return default;
        }

        private FlowKanbanTaskView? FindTaskView(FlowTask task)
        {
            foreach (var view in _taskViews)
            {
                if (ReferenceEquals(view.Task, task))
                    return view;
            }

            return null;
        }

        private void OnTaskContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Item is not FlowKanbanTaskView view)
                return;

            if (args.InRecycleQueue)
            {
                if (args.ItemContainer is ListViewItem recycleContainer)
                {
                    recycleContainer.GotFocus -= OnTaskContainerGotFocus;
                }
                _realizedCards.Remove(view);
                return;
            }

            if (args.ItemContainer is ListViewItem container)
            {
                container.GotFocus -= OnTaskContainerGotFocus;
                container.GotFocus += OnTaskContainerGotFocus;
                var root = container.ContentTemplateRoot as DependencyObject;
                var card = root as FlowTaskCard ?? (root != null ? FindChild<FlowTaskCard>(root) : null);
                if (card != null)
                {
                    _realizedCards[view] = card;
                }
            }
        }

        private void OnTaskContainerGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not ListViewItem container)
                return;

            var root = container.ContentTemplateRoot as DependencyObject;
            var card = root as FlowTaskCard ?? (root != null ? FindChild<FlowTaskCard>(root) : null);
            if (card == null)
                return;

            var focused = XamlRoot == null
                ? null
                : FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (focused != null && FindAncestor<FlowTaskCard>(focused) == card)
                return;

            card.Focus(FocusState.Programmatic);
        }

        internal FlowTaskCard? TryGetTaskCard(FlowTask task)
        {
            var view = FindTaskView(task);
            if (view == null)
                return null;

            return _realizedCards.TryGetValue(view, out var card) ? card : null;
        }

        internal bool ScrollTaskIntoView(FlowTask task)
        {
            if (_tasksListView == null)
                return false;

            var view = FindTaskView(task);
            if (view == null)
                return false;

            _tasksListView.ScrollIntoView(view);
            return true;
        }

        internal IEnumerable<FlowTaskCard> GetRealizedTaskCards()
        {
            return _realizedCards.Values;
        }

        /// <summary>
        /// Calculate the insertion index based on the Y position of the drop.
        /// Uses edge-based detection - if you're past the top half of a task, insert after it.
        /// </summary>
        private int CalculateInsertIndex(double dropY, out double indicatorY)
        {
            indicatorY = 0;
            if (ColumnData == null)
                return 0;

            if (_tasksItemsControl == null)
            {
                EnsureTasksParts();
            }

            if (_tasksItemsControl == null)
                return ColumnData.Tasks.Count;

            var panel = GetTasksItemsHost();
            if (panel == null || panel.Children.Count == 0)
                return 0;

            FrameworkElement? lastContainer = null;
            var lastIndex = -1;

            foreach (var child in panel.Children)
            {
                if (child is not FrameworkElement fe)
                    continue;

                var index = _tasksListView?.IndexFromContainer(fe) ?? -1;
                if (index < 0)
                    continue;

                var topLeft = fe.TransformToVisual(_tasksItemsControl).TransformPoint(new Point(0, 0));
                var midpoint = topLeft.Y + (fe.ActualHeight / 2);

                if (dropY < midpoint)
                {
                    indicatorY = topLeft.Y;
                    return index;
                }

                lastContainer = fe;
                lastIndex = index;
            }

            if (lastContainer != null)
            {
                var topLeft = lastContainer.TransformToVisual(_tasksItemsControl).TransformPoint(new Point(0, 0));
                indicatorY = topLeft.Y + lastContainer.ActualHeight;
                return Math.Min(lastIndex + 1, ColumnData.Tasks.Count);
            }

            return ColumnData.Tasks.Count;
        }
    }
}

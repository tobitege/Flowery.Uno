using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Input;
using Flowery.Localization;
using Flowery.Theming;
using Flowery.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// A lightweight task card for the Kanban board.
    /// </summary>
    public partial class FlowTaskCard : DaisyBaseContentControl
    {
        private const int TitleLongPressMilliseconds = 500;
        private const double TitleLongPressMoveThreshold = 8;
        private FlowKanban? _parentKanban;
        private DaisyButton? _closeButton;
        private TextBlock? _titleTextBlock;
        private Border? _rootBorder;
        private Brush? _defaultBorderBrush;
        private Thickness _defaultBorderThickness;
        private FlowTask? _trackedTask;
        private readonly HashSet<FlowSubtask> _trackedSubtasks = new();
        private bool _isPointerOver;
        private bool _isKeyboardFocusVisible;
        private bool _isLocalizationSubscribed;
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _titleLongPressTimer;
        private bool _isTitlePressTracking;
        private uint _titlePressPointerId;
        private Point _titlePressStartPoint;
        private static readonly Brush TransparentBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        public FlowTaskCard()
        {
            DefaultStyleKey = typeof(FlowTaskCard);
            IsTabStop = true;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            GotFocus += OnCardGotFocus;
            LostFocus += OnCardLostFocus;
            KeyDown += OnCardKeyDown;
            PointerPressed += OnCardPointerPressed;
            UpdateSelectionBadgePlacement();
        }

        /// <summary>
        /// FlowTaskCard disables neumorphic effects to prevent composition effect storms
        /// when many cards are rendered on the board.
        /// </summary>
        protected override bool IsNeumorphicEffectivelyEnabled() => false;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Find parent FlowKanban and subscribe to its size changes
            _parentKanban = FindParentKanban();
            if (_parentKanban != null)
            {
                CardSize = _parentKanban.BoardSize;
                _parentKanban.BoardSizeChanged += OnParentSizeChanged;
                ApplySizing();
            }

            if (!_isLocalizationSubscribed)
            {
                FloweryLocalization.CultureChanged += OnLocalizationCultureChanged;
                _isLocalizationSubscribed = true;
            }

            UpdateFocusVisualState();
            UpdateSelectionBadgePlacement();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from parent
            if (_parentKanban != null)
            {
                _parentKanban.BoardSizeChanged -= OnParentSizeChanged;
                _parentKanban = null;
            }

            if (_isLocalizationSubscribed)
            {
                FloweryLocalization.CultureChanged -= OnLocalizationCultureChanged;
                _isLocalizationSubscribed = false;
            }

            DetachTitleTextBlock();

            if (_trackedTask != null)
            {
                _trackedTask.PropertyChanged -= OnTaskPropertyChanged;
                _trackedTask.Subtasks.CollectionChanged -= OnSubtasksCollectionChanged;
                _trackedTask = null;
            }

            DetachSubtasks();
        }

        private void OnLocalizationCultureChanged(object? sender, string cultureName)
        {
            RefreshLocalization();
        }

        private void OnParentSizeChanged(object? sender, DaisySize newSize)
        {
            CardSize = newSize;
            ApplySizing();
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

        #region Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty, OnTitleChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region AssigneeText
        public static readonly DependencyProperty AssigneeTextProperty =
            DependencyProperty.Register(
                nameof(AssigneeText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string AssigneeText
        {
            get => (string)GetValue(AssigneeTextProperty);
            set => SetValue(AssigneeTextProperty, value);
        }
        #endregion

        #region AssigneeInitials
        public static readonly DependencyProperty AssigneeInitialsProperty =
            DependencyProperty.Register(
                nameof(AssigneeInitials),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string AssigneeInitials
        {
            get => (string)GetValue(AssigneeInitialsProperty);
            set => SetValue(AssigneeInitialsProperty, value);
        }
        #endregion

        #region HasAssignee
        public static readonly DependencyProperty HasAssigneeProperty =
            DependencyProperty.Register(
                nameof(HasAssignee),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasAssignee
        {
            get => (bool)GetValue(HasAssigneeProperty);
            set => SetValue(HasAssigneeProperty, value);
        }
        #endregion

        #region StartDateText
        public static readonly DependencyProperty StartDateTextProperty =
            DependencyProperty.Register(
                nameof(StartDateText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string StartDateText
        {
            get => (string)GetValue(StartDateTextProperty);
            set => SetValue(StartDateTextProperty, value);
        }
        #endregion

        #region EndDateText
        public static readonly DependencyProperty EndDateTextProperty =
            DependencyProperty.Register(
                nameof(EndDateText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string EndDateText
        {
            get => (string)GetValue(EndDateTextProperty);
            set => SetValue(EndDateTextProperty, value);
        }
        #endregion

        #region HasStartDate
        public static readonly DependencyProperty HasStartDateProperty =
            DependencyProperty.Register(
                nameof(HasStartDate),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasStartDate
        {
            get => (bool)GetValue(HasStartDateProperty);
            set => SetValue(HasStartDateProperty, value);
        }
        #endregion

        #region HasEndDate
        public static readonly DependencyProperty HasEndDateProperty =
            DependencyProperty.Register(
                nameof(HasEndDate),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasEndDate
        {
            get => (bool)GetValue(HasEndDateProperty);
            set => SetValue(HasEndDateProperty, value);
        }
        #endregion

        #region HasDateRange
        public static readonly DependencyProperty HasDateRangeProperty =
            DependencyProperty.Register(
                nameof(HasDateRange),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasDateRange
        {
            get => (bool)GetValue(HasDateRangeProperty);
            set => SetValue(HasDateRangeProperty, value);
        }
        #endregion

        #region HasFooter
        public static readonly DependencyProperty HasFooterProperty =
            DependencyProperty.Register(
                nameof(HasFooter),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasFooter
        {
            get => (bool)GetValue(HasFooterProperty);
            set => SetValue(HasFooterProperty, value);
        }
        #endregion

        #region WorkItemNumberText
        public static readonly DependencyProperty WorkItemNumberTextProperty =
            DependencyProperty.Register(
                nameof(WorkItemNumberText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string WorkItemNumberText
        {
            get => (string)GetValue(WorkItemNumberTextProperty);
            set => SetValue(WorkItemNumberTextProperty, value);
        }
        #endregion

        #region HasWorkItemNumber
        public static readonly DependencyProperty HasWorkItemNumberProperty =
            DependencyProperty.Register(
                nameof(HasWorkItemNumber),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasWorkItemNumber
        {
            get => (bool)GetValue(HasWorkItemNumberProperty);
            set => SetValue(HasWorkItemNumberProperty, value);
        }
        #endregion

        #region Task
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(
                nameof(Task),
                typeof(FlowTask),
                typeof(FlowTaskCard),
                new PropertyMetadata(null, OnTaskChanged));

        public FlowTask? Task
        {
            get => (FlowTask?)GetValue(TaskProperty);
            set => SetValue(TaskProperty, value);
        }
        #endregion

        #region SubtaskSummaryText
        public static readonly DependencyProperty SubtaskSummaryTextProperty =
            DependencyProperty.Register(
                nameof(SubtaskSummaryText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string SubtaskSummaryText
        {
            get => (string)GetValue(SubtaskSummaryTextProperty);
            set => SetValue(SubtaskSummaryTextProperty, value);
        }
        #endregion

        #region HasSubtaskSummary
        public static readonly DependencyProperty HasSubtaskSummaryProperty =
            DependencyProperty.Register(
                nameof(HasSubtaskSummary),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasSubtaskSummary
        {
            get => (bool)GetValue(HasSubtaskSummaryProperty);
            set => SetValue(HasSubtaskSummaryProperty, value);
        }
        #endregion

        #region DueDateText
        public static readonly DependencyProperty DueDateTextProperty =
            DependencyProperty.Register(
                nameof(DueDateText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string DueDateText
        {
            get => (string)GetValue(DueDateTextProperty);
            set => SetValue(DueDateTextProperty, value);
        }
        #endregion

        #region HasDueDate
        public static readonly DependencyProperty HasDueDateProperty =
            DependencyProperty.Register(
                nameof(HasDueDate),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasDueDate
        {
            get => (bool)GetValue(HasDueDateProperty);
            set => SetValue(HasDueDateProperty, value);
        }
        #endregion

        #region PriorityText
        public static readonly DependencyProperty PriorityTextProperty =
            DependencyProperty.Register(
                nameof(PriorityText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string PriorityText
        {
            get => (string)GetValue(PriorityTextProperty);
            set => SetValue(PriorityTextProperty, value);
        }
        #endregion

        #region HasPriority
        public static readonly DependencyProperty HasPriorityProperty =
            DependencyProperty.Register(
                nameof(HasPriority),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasPriority
        {
            get => (bool)GetValue(HasPriorityProperty);
            set => SetValue(HasPriorityProperty, value);
        }
        #endregion

        #region ProgressText
        public static readonly DependencyProperty ProgressTextProperty =
            DependencyProperty.Register(
                nameof(ProgressText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string ProgressText
        {
            get => (string)GetValue(ProgressTextProperty);
            set => SetValue(ProgressTextProperty, value);
        }
        #endregion

        #region HasProgress
        public static readonly DependencyProperty HasProgressProperty =
            DependencyProperty.Register(
                nameof(HasProgress),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool HasProgress
        {
            get => (bool)GetValue(HasProgressProperty);
            set => SetValue(HasProgressProperty, value);
        }
        #endregion

        #region IsBlocked
        public static readonly DependencyProperty IsBlockedProperty =
            DependencyProperty.Register(
                nameof(IsBlocked),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool IsBlocked
        {
            get => (bool)GetValue(IsBlockedProperty);
            set => SetValue(IsBlockedProperty, value);
        }
        #endregion

        #region BlockedBadgeText
        public static readonly DependencyProperty BlockedBadgeTextProperty =
            DependencyProperty.Register(
                nameof(BlockedBadgeText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string BlockedBadgeText
        {
            get => (string)GetValue(BlockedBadgeTextProperty);
            set => SetValue(BlockedBadgeTextProperty, value);
        }
        #endregion

        #region BlockedDetailText
        public static readonly DependencyProperty BlockedDetailTextProperty =
            DependencyProperty.Register(
                nameof(BlockedDetailText),
                typeof(string),
                typeof(FlowTaskCard),
                new PropertyMetadata(string.Empty));

        public string BlockedDetailText
        {
            get => (string)GetValue(BlockedDetailTextProperty);
            set => SetValue(BlockedDetailTextProperty, value);
        }
        #endregion

        #region CloseCommand
        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register(
                nameof(CloseCommand),
                typeof(ICommand),
                typeof(FlowTaskCard),
                new PropertyMetadata(null));

        public ICommand? CloseCommand
        {
            get => (ICommand?)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }
        #endregion

        #region CloseCommandParameter
        public static readonly DependencyProperty CloseCommandParameterProperty =
            DependencyProperty.Register(
                nameof(CloseCommandParameter),
                typeof(object),
                typeof(FlowTaskCard),
                new PropertyMetadata(null));

        public object? CloseCommandParameter
        {
            get => (object?)GetValue(CloseCommandParameterProperty);
            set => SetValue(CloseCommandParameterProperty, value);
        }
        #endregion

        #region EditCommand
        public static readonly DependencyProperty EditCommandProperty =
            DependencyProperty.Register(
                nameof(EditCommand),
                typeof(ICommand),
                typeof(FlowTaskCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Command invoked when the card is double-tapped for editing.
        /// </summary>
        public ICommand? EditCommand
        {
            get => (ICommand?)GetValue(EditCommandProperty);
            set => SetValue(EditCommandProperty, value);
        }
        #endregion

        #region IsSelected
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false, OnIsSelectedChanged));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowTaskCard card && e.NewValue is bool isSelected)
            {
                card.UpdateSelectionState(isSelected);
            }
        }
        #endregion

        #region SelectionBadgeColumn
        public static readonly DependencyProperty SelectionBadgeColumnProperty =
            DependencyProperty.Register(
                nameof(SelectionBadgeColumn),
                typeof(int),
                typeof(FlowTaskCard),
                new PropertyMetadata(2));

        public int SelectionBadgeColumn
        {
            get => (int)GetValue(SelectionBadgeColumnProperty);
            private set => SetValue(SelectionBadgeColumnProperty, value);
        }
        #endregion

        #region ActionPillHorizontalAlignment
        public static readonly DependencyProperty ActionPillHorizontalAlignmentProperty =
            DependencyProperty.Register(
                nameof(ActionPillHorizontalAlignment),
                typeof(HorizontalAlignment),
                typeof(FlowTaskCard),
                new PropertyMetadata(HorizontalAlignment.Right));

        public HorizontalAlignment ActionPillHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(ActionPillHorizontalAlignmentProperty);
            private set => SetValue(ActionPillHorizontalAlignmentProperty, value);
        }
        #endregion

        #region ActionPillOffsetX
        public static readonly DependencyProperty ActionPillOffsetXProperty =
            DependencyProperty.Register(
                nameof(ActionPillOffsetX),
                typeof(double),
                typeof(FlowTaskCard),
                new PropertyMetadata(0d));

        public double ActionPillOffsetX
        {
            get => (double)GetValue(ActionPillOffsetXProperty);
            private set => SetValue(ActionPillOffsetXProperty, value);
        }
        #endregion

        #region IsActionPillVisible
        public static readonly DependencyProperty IsActionPillVisibleProperty =
            DependencyProperty.Register(
                nameof(IsActionPillVisible),
                typeof(bool),
                typeof(FlowTaskCard),
                new PropertyMetadata(false));

        public bool IsActionPillVisible
        {
            get => (bool)GetValue(IsActionPillVisibleProperty);
            private set => SetValue(IsActionPillVisibleProperty, value);
        }
        #endregion

        #region ActionPillOpacity
        public static readonly DependencyProperty ActionPillOpacityProperty =
            DependencyProperty.Register(
                nameof(ActionPillOpacity),
                typeof(double),
                typeof(FlowTaskCard),
                new PropertyMetadata(0d));

        public double ActionPillOpacity
        {
            get => (double)GetValue(ActionPillOpacityProperty);
            private set => SetValue(ActionPillOpacityProperty, value);
        }
        #endregion

        #region Palette
        public static readonly DependencyProperty PaletteProperty =
            DependencyProperty.Register(
                nameof(Palette),
                typeof(DaisyColor),
                typeof(FlowTaskCard),
                new PropertyMetadata(DaisyColor.Default, (d, _) => ((FlowTaskCard)d).ApplyPalette()));

        public DaisyColor Palette
        {
            get => (DaisyColor)GetValue(PaletteProperty);
            set => SetValue(PaletteProperty, value);
        }
        #endregion

        #region CardSize
        public static readonly DependencyProperty CardSizeProperty =
            DependencyProperty.Register(
                nameof(CardSize),
                typeof(DaisySize),
                typeof(FlowTaskCard),
                new PropertyMetadata(DaisySize.Medium, (d, _) => ((FlowTaskCard)d).ApplySizing()));

        /// <summary>
        /// The size tier for this card's visual elements.
        /// </summary>
        public DaisySize CardSize
        {
            get => (DaisySize)GetValue(CardSizeProperty);
            set => SetValue(CardSizeProperty, value);
        }
        #endregion

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ApplyPalette();
            ApplySizing();

            if (_rootBorder != null)
            {
                _rootBorder.DragStarting -= OnDragStarting;
            }

            _rootBorder = GetTemplateChild("PART_Root") as Border;
            if (_rootBorder != null)
            {
                _defaultBorderBrush ??= _rootBorder.BorderBrush;
                if (_defaultBorderThickness == default)
                {
                    _defaultBorderThickness = _rootBorder.BorderThickness;
                }
                _rootBorder.CanDrag = true;
                _rootBorder.DragStarting += OnDragStarting;
            }

            // Find close button
            if (_closeButton != null)
            {
                _closeButton.GotFocus -= OnCardGotFocus;
                _closeButton.LostFocus -= OnCardLostFocus;
            }

            _closeButton = GetTemplateChild("PART_CloseBtn") as DaisyButton;
            UpdateCloseButtonVisibility();
            UpdateAutomationProperties();
            if (_closeButton != null)
            {
                DaisyNeumorphic.SetIsEnabled(_closeButton, false);
                AutomationProperties.SetName(_closeButton, FloweryLocalization.GetStringInternal("Common_Delete"));
                _closeButton.GotFocus += OnCardGotFocus;
                _closeButton.LostFocus += OnCardLostFocus;
            }
            AttachTitleTextBlock();

            // Enable drag for this card
            CanDrag = true;
            DragStarting += OnDragStarting;

            // Enable double-tap for editing
            DoubleTapped += OnDoubleTapped;

            // Enable hover to show close button
            PointerEntered += OnPointerEntered;
            PointerExited += OnPointerExited;

            UpdateTaskVisuals();
            ApplySelectionVisual();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowTaskCard card)
            {
                card.UpdateTitleToolTip();
                card.UpdateAutomationProperties();
            }
        }

        private void OnCardGotFocus(object sender, RoutedEventArgs e)
        {
            UpdateCloseButtonVisibility();
            UpdateFocusVisualState();
        }

        private void OnCardLostFocus(object sender, RoutedEventArgs e)
        {
            if (IsFocusWithin())
                return;

            UpdateCloseButtonVisibility();
            UpdateFocusVisualState();
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOver = true;
            UpdateCloseButtonVisibility();
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOver = false;
            UpdateCloseButtonVisibility();
        }

        private void OnDragStarting(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
        {
            // Store the task data for the drag operation
            var task = CloseCommandParameter as FlowTask;
            if (task != null)
            {
                args.Data.SetText(task.Id);
                args.Data.Properties[FlowKanban.TaskDragDataPropertyKey] = task.Id;
                args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            }
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (IsMobileSelectionMode())
            {
                var task = CloseCommandParameter as FlowTask ?? Task ?? _trackedTask;
                if (task != null)
                {
                    task.IsSelected = !task.IsSelected;
                    e.Handled = true;
                }
                return;
            }

            if (TryExecuteEdit())
                e.Handled = true;
        }

        private void OnCardKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter)
                return;

            if (TryExecuteEdit())
                e.Handled = true;
        }

        private void OnCardPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (IsMobileSelectionMode())
                return;

            var task = CloseCommandParameter as FlowTask ?? Task ?? _trackedTask;
            if (task == null)
                return;

            var column = FindAncestor<FlowKanbanColumn>(this);
            if (column != null)
            {
                var modifiers = e.KeyModifiers;
                if (column.HandleTaskSelection(task, modifiers.HasFlag(VirtualKeyModifiers.Shift), modifiers.HasFlag(VirtualKeyModifiers.Control)))
                {
                    e.Handled = true;
                }
                return;
            }

            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                task.IsSelected = !task.IsSelected;
            }
            else if (!e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
            {
                task.IsSelected = true;
            }
        }

        private bool TryExecuteEdit()
        {
            var task = CloseCommandParameter as FlowTask ?? Task;
            if (task == null)
                return false;

            if (EditCommand?.CanExecute(task) == true)
            {
                EditCommand.Execute(task);
                return true;
            }

            return false;
        }

        private void ApplyPalette()
        {
            if (Palette == DaisyColor.Default)
            {
                var secondaryBackground = DaisyResourceLookup.GetBrush("DaisySecondaryBrush");
                if (secondaryBackground != null)
                {
                    Background = CloneBrush(secondaryBackground) ?? secondaryBackground;
                }
                else
                {
                    ClearValue(BackgroundProperty);
                }

                var secondaryContent = DaisyResourceLookup.GetBrush("DaisySecondaryContentBrush");
                if (secondaryContent != null)
                {
                    Foreground = CloneBrush(secondaryContent) ?? secondaryContent;
                }
                else
                {
                    ClearValue(ForegroundProperty);
                }
            }
            else
            {
                var (bg, fg) = DaisyResourceLookup.GetPaletteBrushes(Palette.ToString());
                if (bg != null) Background = CloneBrush(bg) ?? bg;
                if (fg != null) Foreground = CloneBrush(fg) ?? fg;
            }

            SyncTitleForeground();
        }

        private void ApplySizing()
        {
            Padding = DaisyResourceLookup.GetKanbanCardPadding(CardSize);
            CornerRadius = DaisyResourceLookup.GetKanbanCardCornerRadius(CardSize);
            var titleSize = DaisyResourceLookup.GetKanbanCardTitleFontSize(CardSize);
            FontSize = Math.Max(1, titleSize - 1);
            UpdateTitleToolTip();
        }

        private static void OnTaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowTaskCard card)
            {
                card.UpdateTaskBinding(e.OldValue as FlowTask, e.NewValue as FlowTask);
            }
        }

        private void UpdateTaskBinding(FlowTask? oldTask, FlowTask? newTask)
        {
            if (oldTask != null)
            {
                oldTask.PropertyChanged -= OnTaskPropertyChanged;
                oldTask.Subtasks.CollectionChanged -= OnSubtasksCollectionChanged;
            }

            DetachSubtasks();
            _trackedTask = newTask;

            if (newTask != null)
            {
                newTask.PropertyChanged += OnTaskPropertyChanged;
                newTask.Subtasks.CollectionChanged += OnSubtasksCollectionChanged;
                AttachSubtasks(newTask.Subtasks);
            }

            UpdateTaskVisuals();
            UpdateAutomationProperties();
            IsSelected = newTask?.IsSelected ?? false;
        }

        private void OnTaskPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(FlowTask.PlannedEndDate), StringComparison.Ordinal))
            {
                UpdateDueDateText();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowTask.Title), StringComparison.Ordinal))
            {
                UpdateAutomationProperties();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowTask.IsBlocked), StringComparison.Ordinal)
                     || string.Equals(e.PropertyName, nameof(FlowTask.BlockedReason), StringComparison.Ordinal)
                     || string.Equals(e.PropertyName, nameof(FlowTask.BlockedSince), StringComparison.Ordinal))
            {
                UpdateBlockedVisuals();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowTask.Priority), StringComparison.Ordinal)
                     || string.Equals(e.PropertyName, nameof(FlowTask.ProgressPercent), StringComparison.Ordinal))
            {
                UpdatePriorityAndProgress();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowTask.IsSelected), StringComparison.Ordinal))
            {
                if (sender is FlowTask task)
                {
                    IsSelected = task.IsSelected;
                }
            }
            else if (string.Equals(e.PropertyName, nameof(FlowTask.WorkItemNumber), StringComparison.Ordinal))
            {
                UpdateWorkItemNumber();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowTask.Assignee), StringComparison.Ordinal))
            {
                UpdateAssignee();
            }
            else if (string.Equals(e.PropertyName, nameof(FlowTask.PlannedStartDate), StringComparison.Ordinal)
                     || string.Equals(e.PropertyName, nameof(FlowTask.PlannedEndDate), StringComparison.Ordinal))
            {
                UpdateDateRange();
            }
        }

        private void OnSubtasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
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

            UpdateSubtaskSummary();
        }

        private void OnSubtaskPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(FlowSubtask.IsCompleted), StringComparison.Ordinal))
            {
                UpdateSubtaskSummary();
            }
        }

        private void AttachSubtasks(IEnumerable<FlowSubtask> subtasks)
        {
            foreach (var subtask in subtasks)
            {
                TrackSubtask(subtask);
            }
        }

        private void DetachSubtasks()
        {
            foreach (var subtask in _trackedSubtasks.ToArray())
            {
                subtask.PropertyChanged -= OnSubtaskPropertyChanged;
            }

            _trackedSubtasks.Clear();
        }

        private void TrackSubtask(FlowSubtask subtask)
        {
            if (_trackedSubtasks.Add(subtask))
            {
                subtask.PropertyChanged += OnSubtaskPropertyChanged;
            }
        }

        private void UntrackSubtask(FlowSubtask subtask)
        {
            if (_trackedSubtasks.Remove(subtask))
            {
                subtask.PropertyChanged -= OnSubtaskPropertyChanged;
            }
        }

        private void UpdateTaskVisuals()
        {
            UpdateWorkItemNumber();
            UpdateAssignee();
            UpdateDateRange();
            UpdateSubtaskSummary();
            UpdateDueDateText();
            UpdateBlockedVisuals();
            UpdatePriorityAndProgress();
        }

        private void UpdateAssignee()
        {
            var task = Task ?? _trackedTask;
            var assignee = task?.Assignee?.Trim();
            if (string.IsNullOrWhiteSpace(assignee))
            {
                HasAssignee = false;
                AssigneeText = string.Empty;
                AssigneeInitials = string.Empty;
            }
            else
            {
                HasAssignee = true;
                AssigneeText = assignee;
                AssigneeInitials = BuildInitials(assignee);
            }

            UpdateFooterVisibility();
        }

        private void UpdateDateRange()
        {
            var task = Task ?? _trackedTask;
            if (task?.PlannedStartDate is { } startDate)
            {
                HasStartDate = true;
                StartDateText = FormatRelativeDate(startDate);
            }
            else
            {
                HasStartDate = false;
                StartDateText = string.Empty;
            }

            if (task?.PlannedEndDate is { } endDate)
            {
                HasEndDate = true;
                EndDateText = FormatRelativeDate(endDate);
            }
            else
            {
                HasEndDate = false;
                EndDateText = string.Empty;
            }

            HasDateRange = HasStartDate && HasEndDate;
            UpdateFooterVisibility();
        }

        private void UpdateFooterVisibility()
        {
            HasFooter = HasAssignee || HasStartDate || HasEndDate || HasProgress;
        }

        private static string FormatRelativeDate(DateTime date)
        {
            var today = DateTime.Today;
            var days = (date.Date - today).Days;

            if (days == 0)
                return FloweryLocalization.GetStringInternal("Kanban_Date_Today", "Today");

            if (days == -1)
                return FloweryLocalization.GetStringInternal("Kanban_Date_Yesterday", "Yesterday");

            if (days == 1)
                return FloweryLocalization.GetStringInternal("Kanban_Date_Tomorrow", "Tomorrow");

            if (days < 0)
            {
                return string.Format(
                    CultureInfo.CurrentUICulture,
                    FloweryLocalization.GetStringInternal("Kanban_Date_DaysAgo", "{0} days ago"),
                    Math.Abs(days));
            }

            return string.Format(
                CultureInfo.CurrentUICulture,
                FloweryLocalization.GetStringInternal("Kanban_Date_InDays", "in {0} days"),
                days);
        }

        private void UpdateWorkItemNumber()
        {
            var task = Task ?? _trackedTask;
            if (task == null || task.WorkItemNumber <= 0)
            {
                HasWorkItemNumber = false;
                WorkItemNumberText = string.Empty;
                return;
            }

            HasWorkItemNumber = true;
            WorkItemNumberText = task.WorkItemNumber.ToString(CultureInfo.CurrentUICulture);
        }

        private void UpdateSubtaskSummary()
        {
            var task = Task ?? _trackedTask;
            var count = task?.Subtasks.Count ?? 0;

            if (count <= 0)
            {
                HasSubtaskSummary = false;
                SubtaskSummaryText = string.Empty;
                return;
            }

            HasSubtaskSummary = true;
            var completed = task?.Subtasks.Count(s => s.IsCompleted) ?? 0;
            if (completed <= 0)
            {
                SubtaskSummaryText = count.ToString(CultureInfo.CurrentUICulture);
            }
            else
            {
                SubtaskSummaryText = string.Concat(
                    completed.ToString(CultureInfo.CurrentUICulture),
                    "/",
                    count.ToString(CultureInfo.CurrentUICulture));
            }
        }

        private void UpdateDueDateText()
        {
            var task = Task ?? _trackedTask;
            if (task?.PlannedEndDate is not { } dueDate)
            {
                HasDueDate = false;
                DueDateText = string.Empty;
                return;
            }

            var today = DateTime.Today;
            var daysUntil = (dueDate.Date - today).Days;

            if (daysUntil >= 0 && daysUntil <= 7)
            {
                if (daysUntil == 1)
                {
                    DueDateText = FloweryLocalization.GetStringInternal("Kanban_DueInDay", "in 1 day");
                }
                else
                {
                    DueDateText = string.Format(
                        CultureInfo.CurrentUICulture,
                        FloweryLocalization.GetStringInternal("Kanban_DueInDays", "in {0} days"),
                        daysUntil);
                }
            }
            else
            {
                DueDateText = dueDate.ToString("d", CultureInfo.CurrentUICulture);
            }

            HasDueDate = true;
        }

        private void UpdatePriorityAndProgress()
        {
            var task = Task ?? _trackedTask;
            if (task == null)
            {
                HasPriority = false;
                PriorityText = string.Empty;
                HasProgress = false;
                ProgressText = string.Empty;
                UpdateFooterVisibility();
                return;
            }

            if (task.Priority == FlowTaskPriority.Normal)
            {
                HasPriority = false;
                PriorityText = string.Empty;
            }
            else
            {
                HasPriority = true;
                PriorityText = GetPriorityLabel(task.Priority);
            }

            if (task.ProgressPercent > 0)
            {
                HasProgress = true;
                ProgressText = string.Concat(task.ProgressPercent.ToString(CultureInfo.CurrentUICulture), "%");
            }
            else
            {
                HasProgress = false;
                ProgressText = string.Empty;
            }

            UpdateFooterVisibility();
        }

        private static string GetPriorityLabel(FlowTaskPriority priority)
        {
            return priority switch
            {
                FlowTaskPriority.Low => FloweryLocalization.GetStringInternal("Kanban_Priority_Low", "Low"),
                FlowTaskPriority.High => FloweryLocalization.GetStringInternal("Kanban_Priority_High", "High"),
                FlowTaskPriority.Urgent => FloweryLocalization.GetStringInternal("Kanban_Priority_Urgent", "Urgent"),
                FlowTaskPriority.Normal => FloweryLocalization.GetStringInternal("Kanban_Priority_Normal", "Normal"),
                _ => priority.ToString()
            };
        }

        private void UpdateBlockedVisuals()
        {
            var task = Task ?? _trackedTask;
            if (task?.IsBlocked != true)
            {
                IsBlocked = false;
                BlockedBadgeText = string.Empty;
                BlockedDetailText = string.Empty;
                return;
            }

            IsBlocked = true;
            BlockedBadgeText = FloweryLocalization.GetStringInternal("Kanban_Blocked", "Blocked");

            if (!string.IsNullOrWhiteSpace(task.BlockedReason))
            {
                BlockedDetailText = task.BlockedReason!;
                return;
            }

            if (task.BlockedDays is { } days)
            {
                BlockedDetailText = string.Format(
                    CultureInfo.CurrentUICulture,
                    FloweryLocalization.GetStringInternal("Kanban_BlockedDays", "Blocked {0}d"),
                    days);
                return;
            }

            BlockedDetailText = FloweryLocalization.GetStringInternal("Kanban_Blocked", "Blocked");
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyPalette();
            ApplySizing();
            ApplySelectionVisual();
        }

        internal void RefreshTheme()
        {
            ApplyPalette();
            ApplySizing();
        }

        internal void RefreshLocalization()
        {
            UpdateTaskVisuals();
            UpdateAutomationProperties();
            UpdateSelectionBadgePlacement();
            if (_closeButton != null)
            {
                AutomationProperties.SetName(_closeButton, FloweryLocalization.GetStringInternal("Common_Delete"));
            }
        }

        private void AttachTitleTextBlock()
        {
            DetachTitleTextBlock();
            _titleTextBlock = GetTemplateChild("PART_TitleText") as TextBlock;
            if (_titleTextBlock == null)
                return;

            _titleTextBlock.SizeChanged += OnTitleSizeChanged;
            _titleTextBlock.Loaded += OnTitleLoaded;
            _titleTextBlock.PointerPressed += OnTitlePointerPressed;
            _titleTextBlock.PointerMoved += OnTitlePointerMoved;
            _titleTextBlock.PointerReleased += OnTitlePointerReleased;
            _titleTextBlock.PointerCanceled += OnTitlePointerCanceled;
            _titleTextBlock.PointerCaptureLost += OnTitlePointerCaptureLost;
            UpdateTitleToolTip();
            SyncTitleForeground();
        }

        private void DetachTitleTextBlock()
        {
            if (_titleTextBlock != null)
            {
                _titleTextBlock.SizeChanged -= OnTitleSizeChanged;
                _titleTextBlock.Loaded -= OnTitleLoaded;
                _titleTextBlock.PointerPressed -= OnTitlePointerPressed;
                _titleTextBlock.PointerMoved -= OnTitlePointerMoved;
                _titleTextBlock.PointerReleased -= OnTitlePointerReleased;
                _titleTextBlock.PointerCanceled -= OnTitlePointerCanceled;
                _titleTextBlock.PointerCaptureLost -= OnTitlePointerCaptureLost;
                _titleTextBlock = null;
            }

            CancelTitleLongPress();
        }

        private void OnTitleSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTitleToolTip();
        }

        private void OnTitleLoaded(object sender, RoutedEventArgs e)
        {
            UpdateTitleToolTip();
        }

        private void UpdateTitleToolTip()
        {
            if (_titleTextBlock == null)
                return;

            var title = Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                ToolTipService.SetToolTip(_titleTextBlock, null);
                return;
            }

            var availableWidth = _titleTextBlock.ActualWidth;
            if (double.IsNaN(availableWidth) || availableWidth <= 1)
            {
                ToolTipService.SetToolTip(_titleTextBlock, null);
                return;
            }

            var isTrimmed = IsTitleTrimmed(title, _titleTextBlock, availableWidth);

            ToolTipService.SetToolTip(_titleTextBlock, isTrimmed ? title : null);
        }

        private void OnTitlePointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsMobileSelectionMode())
                return;

            if (e.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Touch)
                return;

            if (sender is not UIElement element)
                return;

            _titlePressPointerId = e.Pointer.PointerId;
            _titlePressStartPoint = e.GetCurrentPoint(element).Position;
            _isTitlePressTracking = true;
            StartTitleLongPressTimer();
        }

        private void OnTitlePointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isTitlePressTracking || e.Pointer.PointerId != _titlePressPointerId)
                return;

            if (sender is not UIElement element)
                return;

            var current = e.GetCurrentPoint(element).Position;
            var dx = current.X - _titlePressStartPoint.X;
            var dy = current.Y - _titlePressStartPoint.Y;
            if ((dx * dx) + (dy * dy) > (TitleLongPressMoveThreshold * TitleLongPressMoveThreshold))
            {
                CancelTitleLongPress();
            }
        }

        private void OnTitlePointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId != _titlePressPointerId)
                return;

            CancelTitleLongPress();
        }

        private void OnTitlePointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            CancelTitleLongPress();
        }

        private void OnTitlePointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            CancelTitleLongPress();
        }

        private void StartTitleLongPressTimer()
        {
            if (DispatcherQueue == null)
                return;

            if (_titleLongPressTimer == null)
            {
                _titleLongPressTimer = DispatcherQueue.CreateTimer();
            }
            _titleLongPressTimer.Stop();
            _titleLongPressTimer.Interval = TimeSpan.FromMilliseconds(TitleLongPressMilliseconds);
            _titleLongPressTimer.Tick -= OnTitleLongPressTimerTick;
            _titleLongPressTimer.Tick += OnTitleLongPressTimerTick;
            _titleLongPressTimer.Start();
        }

        private void OnTitleLongPressTimerTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
        {
            sender.Stop();

            if (!_isTitlePressTracking)
                return;

            _isTitlePressTracking = false;
            if (TryExecuteEdit())
            {
                Focus(FocusState.Programmatic);
            }
        }

        private void CancelTitleLongPress()
        {
            _isTitlePressTracking = false;
            _titlePressPointerId = 0;
            _titleLongPressTimer?.Stop();
        }

        private void SyncTitleForeground()
        {
            if (_titleTextBlock == null)
                return;

            if (_titleTextBlock.ReadLocalValue(TextBlock.ForegroundProperty) != DependencyProperty.UnsetValue)
                return;

            if (Foreground is Brush brush)
                _titleTextBlock.Foreground = brush;
        }

        private void UpdateCloseButtonVisibility()
        {
            var isVisible = _isPointerOver || IsFocusWithin();
            if (_closeButton != null)
            {
                _closeButton.IsHitTestVisible = isVisible;
                _closeButton.Opacity = isVisible ? 0.7 : 0;
            }

            var showPill = IsSelected || isVisible;
            IsActionPillVisible = showPill;
            ActionPillOpacity = showPill ? 1 : 0;
        }

        private void UpdateFocusVisualState()
        {
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

        private void UpdateAutomationProperties()
        {
            var title = Title;
            if (string.IsNullOrWhiteSpace(title))
                title = Task?.Title ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                ClearValue(AutomationProperties.NameProperty);
            }
            else
            {
                AutomationProperties.SetName(this, title);
            }

            var task = Task ?? _trackedTask;
            if (task == null)
            {
                ClearValue(AutomationProperties.AutomationIdProperty);
            }
            else
            {
                AutomationProperties.SetAutomationId(this, $"kanban-card-{task.Id}");
            }
        }

        private void UpdateSelectionState(bool isSelected)
        {
            ApplySelectionVisual();

            if (_trackedTask != null && _trackedTask.IsSelected != isSelected)
            {
                _trackedTask.IsSelected = isSelected;
            }

            UpdateCloseButtonVisibility();
        }

        private void UpdateSelectionBadgePlacement()
        {
            SelectionBadgeColumn = FloweryLocalization.Instance.IsRtl ? 0 : 2;
            ActionPillHorizontalAlignment = FloweryLocalization.Instance.IsRtl
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;
            ActionPillOffsetX = FloweryLocalization.Instance.IsRtl ? -2 : 2;
        }

        private void ApplySelectionVisual()
        {
            if (_rootBorder == null)
                return;

            if (!IsSelected)
            {
                _rootBorder.BorderThickness = _defaultBorderThickness;
                _rootBorder.BorderBrush = TransparentBorderBrush;
                return;
            }

            var accent = DaisyResourceLookup.GetBrush("DaisyAccentBrush");
            if (accent != null)
                _rootBorder.BorderBrush = accent;
            _rootBorder.BorderThickness = _defaultBorderThickness;
        }

        private bool IsFocusWithin()
        {
            if (XamlRoot == null)
                return false;

            var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (focused == null)
                return false;

            return FindAncestor<FlowTaskCard>(focused) == this;
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

        private static bool IsTitleTrimmed(string text, TextBlock reference, double availableWidth)
        {
            const int maxLines = 2;
            var limitedHeight = MeasureTextHeight(text, reference, availableWidth, maxLines);
            var fullHeight = MeasureTextHeight(text, reference, availableWidth, null);

            return fullHeight > limitedHeight + 1;
        }

        private static double MeasureTextHeight(string text, TextBlock reference, double width, int? maxLines)
        {
            var measuringText = new TextBlock
            {
                Text = text,
                FontFamily = reference.FontFamily,
                FontSize = reference.FontSize,
                FontStyle = reference.FontStyle,
                FontWeight = reference.FontWeight,
                FontStretch = reference.FontStretch,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            if (reference.LineHeight > 0)
            {
                measuringText.LineHeight = reference.LineHeight;
            }

            if (maxLines.HasValue)
            {
                measuringText.MaxLines = maxLines.Value;
            }

            measuringText.Measure(new Size(width, double.PositiveInfinity));
            return measuringText.DesiredSize.Height;
        }

        private static Brush? CloneBrush(Brush? brush)
        {
            if (brush is SolidColorBrush scb)
            {
                return new SolidColorBrush(scb.Color);
            }

            return brush;
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var trimmed = name.Trim();
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return string.Empty;

            if (parts.Length == 1)
            {
                var single = parts[0];
                if (single.Length >= 2)
                {
                    return string.Concat(single[0], single[1]).ToUpper(CultureInfo.CurrentUICulture);
                }

                return single.ToUpper(CultureInfo.CurrentUICulture);
            }

            var first = parts[0][0];
            var last = parts[^1][0];
            return string.Concat(first, last).ToUpper(CultureInfo.CurrentUICulture);
        }

        private static bool IsMobileSelectionMode()
        {
            return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        }
    }
}

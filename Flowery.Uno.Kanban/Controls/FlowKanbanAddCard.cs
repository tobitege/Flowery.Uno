using System;
using Flowery.Localization;
using Flowery.Theming;
using Flowery.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// An inline add card control that toggles between a button and a text input.
    /// </summary>
    public partial class FlowKanbanAddCard : DaisyBaseContentControl
    {
        private DaisyButton? _addButton;
        private Grid? _inputPanel;
        private DaisyInput? _titleInput;
        private FlowKanban? _parentKanban;

        public FlowKanbanAddCard()
        {
            DefaultStyleKey = typeof(FlowKanbanAddCard);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Find parent FlowKanban and subscribe to its size changes
            _parentKanban = FindParentKanban();
            if (_parentKanban != null)
            {
                AddCardSize = _parentKanban.BoardSize;
                _parentKanban.BoardSizeChanged += OnParentSizeChanged;
                ApplySizing();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_parentKanban != null)
            {
                _parentKanban.BoardSizeChanged -= OnParentSizeChanged;
                _parentKanban = null;
            }
        }

        private void OnParentSizeChanged(object? sender, DaisySize newSize)
        {
            AddCardSize = newSize;
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

        #region AddCardSize
        public static readonly DependencyProperty AddCardSizeProperty =
            DependencyProperty.Register(
                nameof(AddCardSize),
                typeof(DaisySize),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(DaisySize.Medium, (d, _) => ((FlowKanbanAddCard)d).ApplySizing()));

        public DaisySize AddCardSize
        {
            get => (DaisySize)GetValue(AddCardSizeProperty);
            set => SetValue(AddCardSizeProperty, value);
        }
        #endregion

        #region AddCardText
        public static readonly DependencyProperty AddCardTextProperty =
            DependencyProperty.Register(
                nameof(AddCardText),
                typeof(string),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("Kanban_AddCard"), OnAddCardTextChanged));

        public string AddCardText
        {
            get => (string)GetValue(AddCardTextProperty);
            set => SetValue(AddCardTextProperty, value);
        }

        private static void OnAddCardTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanAddCard control)
            {
                control.UpdateAutomationProperties();
            }
        }
        #endregion

        #region AddCardPlaceholderText
        public static readonly DependencyProperty AddCardPlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(AddCardPlaceholderText),
                typeof(string),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("Kanban_AddCardPlaceholder")));

        public string AddCardPlaceholderText
        {
            get => (string)GetValue(AddCardPlaceholderTextProperty);
            set => SetValue(AddCardPlaceholderTextProperty, value);
        }
        #endregion

        #region AddCardConfirmText
        public static readonly DependencyProperty AddCardConfirmTextProperty =
            DependencyProperty.Register(
                nameof(AddCardConfirmText),
                typeof(string),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("Common_Add")));

        public string AddCardConfirmText
        {
            get => (string)GetValue(AddCardConfirmTextProperty);
            set => SetValue(AddCardConfirmTextProperty, value);
        }
        #endregion

        #region AddCardCancelText
        public static readonly DependencyProperty AddCardCancelTextProperty =
            DependencyProperty.Register(
                nameof(AddCardCancelText),
                typeof(string),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("Common_Cancel")));

        public string AddCardCancelText
        {
            get => (string)GetValue(AddCardCancelTextProperty);
            set => SetValue(AddCardCancelTextProperty, value);
        }
        #endregion

        private void ApplySizing()
        {
            FontSize = DaisyResourceLookup.GetKanbanCardTitleFontSize(AddCardSize);
        }

        #region ColumnData
        public static readonly DependencyProperty ColumnDataProperty =
            DependencyProperty.Register(
                nameof(ColumnData),
                typeof(FlowKanbanColumnData),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(null));

        public FlowKanbanColumnData? ColumnData
        {
            get => (FlowKanbanColumnData?)GetValue(ColumnDataProperty);
            set => SetValue(ColumnDataProperty, value);
        }
        #endregion

        #region LaneId
        public static readonly DependencyProperty LaneIdProperty =
            DependencyProperty.Register(
                nameof(LaneId),
                typeof(string),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(null));

        public string? LaneId
        {
            get => (string?)GetValue(LaneIdProperty);
            set => SetValue(LaneIdProperty, value);
        }
        #endregion

        #region InsertAtTop
        public static readonly DependencyProperty InsertAtTopProperty =
            DependencyProperty.Register(
                nameof(InsertAtTop),
                typeof(bool),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(false));

        public bool InsertAtTop
        {
            get => (bool)GetValue(InsertAtTopProperty);
            set => SetValue(InsertAtTopProperty, value);
        }
        #endregion

        #region IsEditing
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(
                nameof(IsEditing),
                typeof(bool),
                typeof(FlowKanbanAddCard),
                new PropertyMetadata(false, OnIsEditingChanged));

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanAddCard control)
            {
                control.UpdateVisualState();
            }
        }
        #endregion

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _addButton = GetTemplateChild("PART_AddButton") as DaisyButton;
            _inputPanel = GetTemplateChild("PART_InputPanel") as Grid;
            _titleInput = GetTemplateChild("PART_TitleInput") as DaisyInput;
            UpdateAutomationProperties();

            if (_addButton != null)
            {
                _addButton.Click += OnAddButtonClick;
            }

            if (_titleInput != null)
            {
                _titleInput.AcceptsReturn = false;
                _titleInput.TextWrapping = TextWrapping.NoWrap;
                _titleInput.KeyDown += OnTitleInputKeyDown;
                _titleInput.LostFocus += OnTitleInputLostFocus;
            }

            var cancelButton = GetTemplateChild("PART_CancelButton") as DaisyButton;
            if (cancelButton != null)
            {
                cancelButton.Click += OnCancelButtonClick;
            }

            var confirmButton = GetTemplateChild("PART_ConfirmButton") as DaisyButton;
            if (confirmButton != null)
            {
                confirmButton.Click += OnConfirmButtonClick;
            }

            UpdateVisualState();
        }

        internal bool FocusForKeyboard()
        {
            if (IsEditing && _titleInput != null)
            {
                _titleInput.FocusInput(selectAll: true);
                return true;
            }

            if (_addButton != null)
            {
                _addButton.Focus(FocusState.Keyboard);
                return true;
            }

            return false;
        }

        internal void BeginInlineAdd()
        {
            IsEditing = true;
            _titleInput?.FocusInput(selectAll: true);
        }

        private void UpdateAutomationProperties()
        {
            if (_addButton != null)
            {
                AutomationProperties.SetName(_addButton, AddCardText);
            }
        }

        private void OnAddButtonClick(object sender, RoutedEventArgs e)
        {
            IsEditing = true;
        }

        private void OnTitleInputKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddCard();
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Escape)
            {
                CancelAdd();
                e.Handled = true;
            }
        }

        private void OnTitleInputLostFocus(object sender, RoutedEventArgs e)
        {
            // Don't cancel if focus moved to confirm/cancel buttons
            // This is handled by the button clicks
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            CancelAdd();
        }

        private void OnConfirmButtonClick(object sender, RoutedEventArgs e)
        {
            AddCard();
        }

        private void AddCard()
        {
            if (ColumnData == null || _titleInput == null)
                return;

            var title = _titleInput.Text?.Trim();
            if (!string.IsNullOrEmpty(title))
            {
                string? laneId = null;
                if (!string.IsNullOrWhiteSpace(LaneId))
                {
                    laneId = FlowKanban.IsUnassignedLaneId(LaneId) ? null : LaneId;
                }
                else if (_parentKanban?.IsLaneGroupingEnabled == true && _parentKanban.Board.Lanes.Count > 0)
                {
                    var candidateLaneId = _parentKanban.Board.Lanes[0].Id;
                    laneId = string.IsNullOrWhiteSpace(candidateLaneId) ? null : candidateLaneId;
                }

                var task = new FlowTask { Title = title, LaneId = laneId };
                FlowKanbanWorkItemNumberHelper.EnsureTaskNumber(_parentKanban?.Board, task);
                if (InsertAtTop)
                {
                    ColumnData.Tasks.Insert(0, task);
                }
                else
                {
                    ColumnData.Tasks.Add(task);
                }
                FlowKanbanDoneColumnHelper.UpdateCompletedAtOnAdd(_parentKanban?.Board, ColumnData, task);
            }

            _titleInput.Text = string.Empty;
            IsEditing = false;
        }

        private void CancelAdd()
        {
            if (_titleInput != null)
            {
                _titleInput.Text = string.Empty;
            }
            IsEditing = false;
        }

        private void UpdateVisualState()
        {
            if (_addButton != null)
            {
                _addButton.Visibility = IsEditing ? Visibility.Collapsed : Visibility.Visible;
            }
            if (_inputPanel != null)
            {
                _inputPanel.Visibility = IsEditing ? Visibility.Visible : Visibility.Collapsed;
            }
            if (IsEditing)
            {
                _titleInput?.FocusInput(selectAll: true);
            }
        }
    }
}

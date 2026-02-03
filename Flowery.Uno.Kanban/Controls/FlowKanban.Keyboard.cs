using System;
using System.Collections.Generic;
using System.Linq;
using Flowery.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Flowery.Uno.Kanban.Controls
{
    public partial class FlowKanban
    {
        private const int InvalidColumnIndex = -1;
        // VK_OEM_PLUS/VK_OEM_MINUS for main keyboard "+" and "-" shortcuts.
        private const VirtualKey OemPlusKey = (VirtualKey)187;
        private const VirtualKey OemMinusKey = (VirtualKey)189;
        private const int TabNavigationThrottleMs = 100;
        private int _keyboardColumnIndex = InvalidColumnIndex;
        private KeyEventHandler? _keyboardKeyDownHandler;
        private readonly List<KeyboardAccelerator> _keyboardAccelerators = new();
        private bool _keyboardAcceleratorsIncludeAdmin;
        private bool _keyboardAcceleratorsShowTooltips;
        private int _lastTabNavigationTick = int.MinValue;

        private void AttachKeyboardSupport()
        {
            if (_keyboardKeyDownHandler == null)
            {
                _keyboardKeyDownHandler = OnKanbanKeyDown;
                AddHandler(KeyDownEvent, _keyboardKeyDownHandler, true);
            }

            RegisterKeyboardAccelerators();
        }

        private void DetachKeyboardSupport()
        {
            if (_keyboardKeyDownHandler != null)
            {
                RemoveHandler(KeyDownEvent, _keyboardKeyDownHandler);
                _keyboardKeyDownHandler = null;
            }

            UnregisterKeyboardAccelerators();
        }

        private void RegisterKeyboardAccelerators()
        {
            var includeAdmin = _isCurrentUserGlobalAdmin;
            var showTooltips = ShowKeyboardAcceleratorTooltips;

            if (_keyboardAccelerators.Count > 0)
            {
                if (_keyboardAcceleratorsIncludeAdmin == includeAdmin
                    && _keyboardAcceleratorsShowTooltips == showTooltips)
                {
                    return;
                }

                UnregisterKeyboardAccelerators();
            }

            _keyboardAcceleratorsIncludeAdmin = includeAdmin;
            _keyboardAcceleratorsShowTooltips = showTooltips;

            if (!showTooltips)
            {
                return;
            }

            if (includeAdmin)
            {
                AddKeyboardAccelerator(VirtualKey.B, VirtualKeyModifiers.Control, OnAddColumnAccelerator);
                AddKeyboardAccelerator(VirtualKey.D, VirtualKeyModifiers.Control, OnDeleteColumnAccelerator);
                AddKeyboardAccelerator(VirtualKey.T, VirtualKeyModifiers.Control, OnRenameColumnAccelerator);
            }

            AddKeyboardAccelerator(VirtualKey.N, VirtualKeyModifiers.Control, OnAddCardAccelerator);
            AddKeyboardAccelerator(VirtualKey.F, VirtualKeyModifiers.Control, OnSearchFocusAccelerator);
            AddKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, OnPreviousColumnAccelerator);
            AddKeyboardAccelerator(VirtualKey.Right, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, OnNextColumnAccelerator);
            AddKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, OnMoveCardAccelerator);
            AddKeyboardAccelerator(VirtualKey.Right, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, OnMoveCardAccelerator);
            AddKeyboardAccelerator(VirtualKey.Up, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, OnMoveCardAccelerator);
            AddKeyboardAccelerator(VirtualKey.Down, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, OnMoveCardAccelerator);
            AddKeyboardAccelerator(OemPlusKey, VirtualKeyModifiers.Control, OnZoomInAccelerator);
            AddKeyboardAccelerator(VirtualKey.Add, VirtualKeyModifiers.Control, OnZoomInAccelerator);
            AddKeyboardAccelerator(OemMinusKey, VirtualKeyModifiers.Control, OnZoomOutAccelerator);
            AddKeyboardAccelerator(VirtualKey.Subtract, VirtualKeyModifiers.Control, OnZoomOutAccelerator);
        }

        private void UnregisterKeyboardAccelerators()
        {
            foreach (var accelerator in _keyboardAccelerators)
            {
                KeyboardAccelerators.Remove(accelerator);
            }

            _keyboardAccelerators.Clear();
        }

        private void AddKeyboardAccelerator(
            VirtualKey key,
            VirtualKeyModifiers modifiers,
            TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
        {
            var accelerator = new KeyboardAccelerator
            {
                Key = key,
                Modifiers = modifiers
            };
            accelerator.Invoked += handler;
            KeyboardAccelerators.Add(accelerator);
            _keyboardAccelerators.Add(accelerator);
        }

        private void OnAddColumnAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!_isCurrentUserGlobalAdmin)
                return;

            if (!IsBoardViewActive)
                return;

            ExecuteAddColumn();
            args.Handled = true;
        }

        private void OnDeleteColumnAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!_isCurrentUserGlobalAdmin)
                return;

            if (!IsBoardViewActive)
                return;

            if (Board.Columns.Count == 0)
                return;

            ExecuteRemoveColumn(Board.Columns[^1]);
            args.Handled = true;
        }

        private void OnRenameColumnAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!_isCurrentUserGlobalAdmin)
                return;

            if (!IsBoardViewActive)
                return;

            var column = GetActiveColumnData();
            if (column == null)
                return;

            ExecuteEditColumn(column);
            args.Handled = true;
        }

        private void OnAddCardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!IsBoardViewActive)
                return;

            var column = GetActiveColumnData();
            if (column == null)
                return;

            BeginInlineAddCard(column);
            args.Handled = true;
        }

        private void OnSearchFocusAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!IsBoardViewActive)
                return;

            _boardSearchInput?.FocusInput(selectAll: true);
            args.Handled = true;
        }

        private void OnPreviousColumnAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!IsBoardViewActive)
                return;

            MoveActiveColumn(-1);
            args.Handled = true;
        }

        private void OnNextColumnAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!IsBoardViewActive)
                return;

            MoveActiveColumn(1);
            args.Handled = true;
        }

        private void OnZoomInAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!IsBoardViewActive)
                return;

            ExecuteZoomIn();
            args.Handled = true;
        }

        private void OnZoomOutAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!IsBoardViewActive)
                return;

            ExecuteZoomOut();
            args.Handled = true;
        }

        private void OnMoveCardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!IsBoardViewActive)
                return;

            var focused = GetFocusedElement();
            if (focused == null)
                return;

            var card = TryResolveFocusedCard(focused);
            if (card?.Task == null)
                return;

            var column = FindAncestor<FlowKanbanColumn>(card);
            if (column?.ColumnData == null)
                return;

            switch (sender.Key)
            {
                case VirtualKey.Left:
                    MoveCardHorizontal(card.Task, column, -1);
                    args.Handled = true;
                    break;
                case VirtualKey.Right:
                    MoveCardHorizontal(card.Task, column, 1);
                    args.Handled = true;
                    break;
                case VirtualKey.Up:
                    MoveCardVertical(card.Task, column, -1);
                    args.Handled = true;
                    break;
                case VirtualKey.Down:
                    MoveCardVertical(card.Task, column, 1);
                    args.Handled = true;
                    break;
            }
        }

        private void OnKanbanKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!IsBoardViewActive)
                return;

            var focused = GetFocusedElement();
            if (focused == null || IsTextInputElement(focused))
                return;

            var isControlDown = IsKeyDown(VirtualKey.Control);
            var isShiftDown = IsKeyDown(VirtualKey.Shift);
            var isAltDown = IsKeyDown(VirtualKey.Menu);

            if (!ShowKeyboardAcceleratorTooltips && isControlDown)
            {
                if (TryHandleControlShortcut(e.Key, isAltDown, isShiftDown))
                {
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == VirtualKey.Tab)
            {
                if (!isControlDown && !isAltDown && ShouldThrottleTabNavigation())
                {
                    e.Handled = true;
                    return;
                }
                if (!isControlDown && !isAltDown && TryHandleTabNavigation(focused, isShiftDown))
                {
                    e.Handled = true;
                }
                return;
            }

            if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
            {
                if (!isControlDown && !isShiftDown && !isAltDown)
                {
                    e.Handled = true;
                    return;
                }
            }

            if (!IsDirectionalKey(e.Key))
                return;

            // Only treat plain arrow keys as navigation. Modified arrows are reserved for shortcuts.
            if (isControlDown || isShiftDown || isAltDown)
                return;

            if (TryHandleDirectionalNavigation(focused, e.Key))
            {
                e.Handled = true;
            }
        }

        private static bool IsKeyDown(VirtualKey key)
        {
            var state = InputKeyboardSource.GetKeyStateForCurrentThread(key);
            return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        private bool ShouldThrottleTabNavigation()
        {
            var now = Environment.TickCount;
            var elapsed = unchecked(now - _lastTabNavigationTick);
            if (elapsed >= 0 && elapsed < TabNavigationThrottleMs)
                return true;

            _lastTabNavigationTick = now;
            return false;
        }

        private bool TryHandleControlShortcut(VirtualKey key, bool isAltDown, bool isShiftDown)
        {
            // Admin-only column operations.
            if (!isAltDown && !isShiftDown && _isCurrentUserGlobalAdmin)
            {
                switch (key)
                {
                    case VirtualKey.B:
                        ExecuteAddColumn();
                        return true;
                    case VirtualKey.D:
                        if (Board.Columns.Count > 0)
                        {
                            ExecuteRemoveColumn(Board.Columns[^1]);
                            return true;
                        }
                        break;
                    case VirtualKey.T:
                        var column = GetActiveColumnData();
                        if (column != null)
                        {
                            ExecuteEditColumn(column);
                            return true;
                        }
                        break;
                }
            }

            // Everyone shortcuts.
            if (!isAltDown && !isShiftDown)
            {
                switch (key)
                {
                    case VirtualKey.N:
                        var column = GetActiveColumnData();
                        if (column != null)
                        {
                            BeginInlineAddCard(column);
                            return true;
                        }
                        break;
                    case VirtualKey.F:
                        _boardSearchInput?.FocusInput(selectAll: true);
                        return true;
                    case OemPlusKey:
                    case VirtualKey.Add:
                        ExecuteZoomIn();
                        return true;
                    case OemMinusKey:
                    case VirtualKey.Subtract:
                        ExecuteZoomOut();
                        return true;
                }
            }

            // Ctrl + Alt + Left/Right (switch columns).
            if (isAltDown && !isShiftDown)
            {
                switch (key)
                {
                    case VirtualKey.Left:
                        MoveActiveColumn(-1);
                        return true;
                    case VirtualKey.Right:
                        MoveActiveColumn(1);
                        return true;
                }
            }

            // Ctrl + Shift + Arrow (move card).
            if (!isAltDown && isShiftDown)
            {
                switch (key)
                {
                    case VirtualKey.Left:
                    case VirtualKey.Right:
                    case VirtualKey.Up:
                    case VirtualKey.Down:
                        return TryMoveFocusedCard(key);
                }
            }

            return false;
        }

        private bool TryMoveFocusedCard(VirtualKey direction)
        {
            var focused = GetFocusedElement();
            if (focused == null)
                return false;

            var card = TryResolveFocusedCard(focused);
            if (card?.Task == null)
                return false;

            var column = FindAncestor<FlowKanbanColumn>(card);
            if (column?.ColumnData == null)
                return false;

            switch (direction)
            {
                case VirtualKey.Left:
                    MoveCardHorizontal(card.Task, column, -1);
                    return true;
                case VirtualKey.Right:
                    MoveCardHorizontal(card.Task, column, 1);
                    return true;
                case VirtualKey.Up:
                    MoveCardVertical(card.Task, column, -1);
                    return true;
                case VirtualKey.Down:
                    MoveCardVertical(card.Task, column, 1);
                    return true;
            }

            return false;
        }

        private static bool IsDirectionalKey(VirtualKey key)
        {
            return key == VirtualKey.Left
                   || key == VirtualKey.Right
                   || key == VirtualKey.Up
                   || key == VirtualKey.Down;
        }

        private DependencyObject? GetFocusedElement()
        {
            return XamlRoot == null
                ? null
                : FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        }

        private bool TryHandleDirectionalNavigation(DependencyObject focused, VirtualKey key)
        {
            var card = TryResolveFocusedCard(focused);
            if (card != null)
                return HandleCardNavigation(card, key);

            var addCard = FindAncestor<FlowKanbanAddCard>(focused);
            if (addCard != null)
                return HandleAddCardNavigation(addCard, key);

            var column = FindAncestor<FlowKanbanColumn>(focused);
            if (column != null)
                return HandleColumnNavigation(column, key);

            return false;
        }

        private FlowTaskCard? TryResolveFocusedCard(DependencyObject focused)
        {
            var card = FindAncestor<FlowTaskCard>(focused);
            if (card != null)
                return card;

            var container = FindAncestor<ListViewItem>(focused);
            if (container == null)
                return null;

            if (container.Content is FlowKanbanTaskView view && view.Task != null)
            {
                var column = FindAncestor<FlowKanbanColumn>(container);
                if (column != null)
                {
                    var realizedCard = column.TryGetTaskCard(view.Task);
                    if (realizedCard != null)
                        return realizedCard;
                }
            }

            var root = container.ContentTemplateRoot as DependencyObject;
            if (root == null)
                return null;

            return root as FlowTaskCard ?? FindDescendant<FlowTaskCard>(root);
        }

        private bool TryHandleTabNavigation(DependencyObject focused, bool isShiftDown)
        {
            var card = TryResolveFocusedCard(focused);
            if (card != null)
                return HandleCardTabNavigation(card, isShiftDown);

            var addCard = FindAncestor<FlowKanbanAddCard>(focused);
            if (addCard != null)
                return HandleAddCardTabNavigation(addCard, isShiftDown);

            var column = FindAncestor<FlowKanbanColumn>(focused);
            if (column != null)
                return HandleColumnTabNavigation(column, isShiftDown);

            return false;
        }

        private bool HandleCardTabNavigation(FlowTaskCard card, bool isShiftDown)
        {
            var column = FindAncestor<FlowKanbanColumn>(card);
            if (column == null || column.ColumnData == null)
                return false;

            var tasks = GetVisibleTaskViews(column);
            if (tasks.Count == 0 || card.Task == null)
                return false;

            var index = tasks.FindIndex(view => ReferenceEquals(view.Task, card.Task));
            if (index < 0)
                return false;

            if (isShiftDown)
            {
                if (index > 0 && TryFocusTaskAtIndex(column, index - 1))
                    return true;
                return FocusColumnHeader(column);
            }

            if (index < tasks.Count - 1 && TryFocusTaskAtIndex(column, index + 1))
                return true;

            FocusAddCard(column);
            return true;
        }

        private bool HandleAddCardTabNavigation(FlowKanbanAddCard addCard, bool isShiftDown)
        {
            var column = FindAncestor<FlowKanbanColumn>(addCard);
            if (column == null || column.ColumnData == null)
                return false;

            if (!isShiftDown)
                return false;

            var tasks = GetVisibleTaskViews(column);
            var lastIndex = tasks.Count - 1;
            if (lastIndex >= 0 && TryFocusTaskAtIndex(column, lastIndex))
                return true;

            return FocusColumnHeader(column);
        }

        private bool HandleColumnTabNavigation(FlowKanbanColumn column, bool isShiftDown)
        {
            if (isShiftDown)
                return false;

            return FocusFirstCardOrAdd(column);
        }

        private bool HandleCardNavigation(FlowTaskCard card, VirtualKey key)
        {
            var column = FindAncestor<FlowKanbanColumn>(card);
            if (column == null || column.ColumnData == null)
                return false;

            var tasks = GetVisibleTaskViews(column);
            if (tasks.Count == 0 || card.Task == null)
                return false;

            var index = tasks.FindIndex(view => ReferenceEquals(view.Task, card.Task));
            if (index < 0)
                return false;

            switch (key)
            {
                case VirtualKey.Up:
                    if (index > 0 && TryFocusTaskAtIndex(column, index - 1))
                        return true;
                    return FocusColumnHeader(column);
                case VirtualKey.Down:
                    if (index < tasks.Count - 1 && TryFocusTaskAtIndex(column, index + 1))
                        return true;
                    return FocusAddCard(column);
                case VirtualKey.Left:
                    return MoveHorizontalFromIndex(column, index, -1);
                case VirtualKey.Right:
                    return MoveHorizontalFromIndex(column, index, 1);
                default:
                    return false;
            }
        }

        private bool HandleAddCardNavigation(FlowKanbanAddCard addCard, VirtualKey key)
        {
            var column = FindAncestor<FlowKanbanColumn>(addCard);
            if (column == null || column.ColumnData == null)
                return false;

            var tasks = GetVisibleTaskViews(column);
            var lastIndex = tasks.Count - 1;

            switch (key)
            {
                case VirtualKey.Up:
                    if (lastIndex >= 0 && TryFocusTaskAtIndex(column, lastIndex))
                        return true;
                    return FocusColumnHeader(column);
                case VirtualKey.Left:
                    return MoveHorizontalFromIndex(column, tasks.Count, -1);
                case VirtualKey.Right:
                    return MoveHorizontalFromIndex(column, tasks.Count, 1);
                default:
                    return false;
            }
        }

        private bool HandleColumnNavigation(FlowKanbanColumn column, VirtualKey key)
        {
            if (!column.ShowColumnHeader || column.ColumnData == null)
                return false;

            switch (key)
            {
                case VirtualKey.Left:
                    return FocusAdjacentColumnHeader(column, -1);
                case VirtualKey.Right:
                    return FocusAdjacentColumnHeader(column, 1);
                case VirtualKey.Down:
                    return FocusFirstCardOrAdd(column);
                default:
                    return false;
            }
        }

        private bool FocusAdjacentColumnHeader(FlowKanbanColumn column, int delta)
        {
            var target = GetAdjacentColumn(column.ColumnData, delta);
            if (target == null)
                return false;

            var targetColumn = FindColumnControl(target, laneId: null, requireHeader: true);
            return MoveFocusToElement(targetColumn);
        }

        private bool FocusFirstCardOrAdd(FlowKanbanColumn column)
        {
            if (!column.ShowTasks)
            {
                if (IsSwimlaneLayoutEnabled && LaneRows.Count > 0 && column.ColumnData != null)
                {
                    var laneId = NormalizeLaneId(LaneRows[0].Lane.Id);
                    var laneColumn = FindColumnControl(column.ColumnData, laneId, requireHeader: false);
                    if (laneColumn != null && laneColumn.ShowTasks)
                        return FocusFirstCardOrAdd(laneColumn);
                }

                return FocusAddCard(column);
            }

            var tasks = GetVisibleTaskViews(column);
            if (tasks.Count > 0)
                return TryFocusTaskAtIndex(column, 0);

            return FocusAddCard(column);
        }

        private bool MoveHorizontalFromIndex(FlowKanbanColumn column, int index, int delta)
        {
            var target = GetAdjacentColumn(column.ColumnData, delta);
            if (target == null)
                return false;

            var laneId = NormalizeLaneId(column.LaneFilterId);
            var targetColumn = FindColumnControl(target, laneId, requireHeader: false);
            if (targetColumn == null)
                return false;

            var tasks = GetVisibleTaskViews(targetColumn);
            if (tasks.Count > 0)
            {
                var targetIndex = Math.Clamp(index, 0, tasks.Count - 1);
                if (TryFocusTaskAtIndex(targetColumn, targetIndex))
                    return true;
            }

            if (FocusAddCard(targetColumn))
                return true;

            return MoveFocusToElement(targetColumn);
        }

        private bool TryFocusTaskAtIndex(FlowKanbanColumn column, int index)
        {
            var tasks = GetVisibleTaskViews(column);
            if (index < 0 || index >= tasks.Count)
                return false;

            var task = tasks[index].Task;
            if (task == null)
                return false;

            return FocusTaskOrColumn(column, task);
        }

        private bool FocusTaskOrColumn(FlowKanbanColumn column, FlowTask task)
        {
            var card = FindTaskCard(column, task);
            if (card != null)
            {
                return MoveFocusToElement(card);
            }

            if (column.ScrollTaskIntoView(task))
            {
                QueueFocusAction(() =>
                {
                    var realizedCard = FindTaskCard(column, task);
                    MoveFocusToElement(realizedCard ?? (DependencyObject)column);
                });
                return true;
            }

            return MoveFocusToElement(column);
        }

        private bool FocusColumnHeader(FlowKanbanColumn column)
        {
            if (column.ShowColumnHeader)
                return MoveFocusToElement(column);

            var fallback = FindColumnControl(column.ColumnData, laneId: null, requireHeader: true);
            return MoveFocusToElement(fallback);
        }

        private bool FocusColumnHeader(FlowKanbanColumnData? column)
        {
            var target = FindColumnControl(column, laneId: null, requireHeader: true);
            return MoveFocusToElement(target);
        }

        private bool FocusAddCard(FlowKanbanColumn column)
        {
            if (!column.ShowAddCard)
                return false;

            var laneId = NormalizeLaneId(column.LaneFilterId);
            var addCard = FindAddCardControl(column.ColumnData, laneId);
            return addCard?.FocusForKeyboard() == true;
        }

        private FlowKanbanColumnData? GetAdjacentColumn(FlowKanbanColumnData? column, int delta)
        {
            if (column == null)
                return null;

            var index = Board.Columns.IndexOf(column);
            if (index < 0)
                return null;

            var targetIndex = index + delta;
            if (targetIndex < 0 || targetIndex >= Board.Columns.Count)
                return null;

            return Board.Columns[targetIndex];
        }

        private static List<FlowKanbanTaskView> GetVisibleTaskViews(FlowKanbanColumn column)
        {
            if (!column.ShowTasks)
                return new List<FlowKanbanTaskView>();

            return column.TaskViews
                .Where(view => view.Task != null && view.Task.IsSearchMatch)
                .ToList();
        }

        private FlowKanbanColumn? FindColumnControl(
            FlowKanbanColumnData? column,
            string? laneId,
            bool requireHeader)
        {
            if (column == null)
                return null;

            EnsureVisualCache();
            var key = (column.Id, NormalizeLaneId(laneId));
            if (!_columnByKey.TryGetValue(key, out var columnControl))
                return null;

            if (!columnControl.IsLoaded || columnControl.Visibility != Visibility.Visible)
                return null;

            if (requireHeader && !columnControl.ShowColumnHeader)
                return null;

            return columnControl;
        }

        private static FlowTaskCard? FindTaskCard(FlowKanbanColumn column, FlowTask task)
        {
            return column.TryGetTaskCard(task);
        }

        private FlowKanbanAddCard? FindAddCardControl(FlowKanbanColumnData? column, string? laneId)
        {
            if (column == null)
                return null;

            var columnControl = FindColumnControl(column, laneId, requireHeader: false);
            return columnControl?.GetVisibleAddCardControl();
        }

        private void BeginInlineAddCard(FlowKanbanColumnData column)
        {
            var laneId = NormalizeLaneId(GetFocusedLaneId());
            QueueFocusAction(() =>
            {
                var addCard = FindAddCardControl(column, laneId);
                if (addCard != null)
                {
                    addCard.BeginInlineAdd();
                    return;
                }

                ExecuteAddCard(column);
            });
        }

        private void MoveActiveColumn(int delta)
        {
            if (Board.Columns.Count == 0)
                return;

            var index = GetActiveColumnIndex();
            if (index == InvalidColumnIndex)
                return;

            var targetIndex = Math.Clamp(index + delta, 0, Board.Columns.Count - 1);
            _keyboardColumnIndex = targetIndex;
            var column = Board.Columns[targetIndex];
            QueueFocusAction(() => FocusColumnHeader(column));
        }

        private int GetActiveColumnIndex()
        {
            var focused = GetFocusedElement();
            var column = GetColumnDataFromElement(focused);
            if (column != null)
            {
                UpdateKeyboardColumnIndex(column);
            }

            if (_keyboardColumnIndex == InvalidColumnIndex || _keyboardColumnIndex >= Board.Columns.Count)
            {
                _keyboardColumnIndex = Board.Columns.Count - 1;
            }

            return _keyboardColumnIndex;
        }

        private FlowKanbanColumnData? GetActiveColumnData()
        {
            var focused = GetFocusedElement();
            var column = GetColumnDataFromElement(focused);
            if (column != null)
            {
                UpdateKeyboardColumnIndex(column);
                return column;
            }

            var index = GetActiveColumnIndex();
            return index >= 0 && index < Board.Columns.Count
                ? Board.Columns[index]
                : null;
        }

        private void UpdateKeyboardColumnIndex(FlowKanbanColumnData column)
        {
            var index = Board.Columns.IndexOf(column);
            if (index >= 0)
            {
                _keyboardColumnIndex = index;
            }
        }

        private FlowKanbanColumnData? GetColumnDataFromElement(DependencyObject? element)
        {
            var columnControl = FindAncestor<FlowKanbanColumn>(element);
            return columnControl?.ColumnData;
        }

        private string? GetFocusedLaneId()
        {
            var focused = GetFocusedElement();
            var column = FindAncestor<FlowKanbanColumn>(focused);
            if (column == null)
                return null;

            return NormalizeLaneId(column.LaneFilterId);
        }

        private void QueueFocusAction(Action focusAction)
        {
            if (DispatcherQueue == null)
            {
                focusAction();
                return;
            }

            DispatcherQueue.TryEnqueue(() => focusAction());
        }

        private void MoveCardHorizontal(FlowTask task, FlowKanbanColumn column, int delta)
        {
            var targetColumn = GetAdjacentColumn(column.ColumnData, delta);
            if (targetColumn == null)
                return;

            var tasks = GetVisibleTaskViews(column);
            var index = tasks.FindIndex(view => ReferenceEquals(view.Task, task));
            if (index < 0)
                return;

            var targetLaneId = column.LaneFilterId;
            var targetInsertIndex = GetLaneInsertIndex(targetColumn, targetLaneId, index);

            var manager = new FlowKanbanManager(this, autoAttach: false);
            var result = manager.TryMoveTaskWithWipEnforcement(task, targetColumn, targetInsertIndex, targetLaneId, enforceHard: false);
            if (result == MoveResult.AllowedWithWipWarning)
            {
                ShowWipWarning(targetColumn, targetLaneId);
            }

            QueueFocusAction(() =>
            {
                var targetColumnControl = FindColumnControl(targetColumn, NormalizeLaneId(targetLaneId), requireHeader: false);
                if (targetColumnControl == null)
                    return;

                FocusTaskOrColumn(targetColumnControl, task);
            });
        }

        private void MoveCardVertical(FlowTask task, FlowKanbanColumn column, int delta)
        {
            var tasks = GetVisibleTaskViews(column);
            var currentIndex = tasks.FindIndex(view => ReferenceEquals(view.Task, task));
            if (currentIndex < 0)
                return;

            var targetIndex = currentIndex + delta;
            if (targetIndex < 0 || targetIndex >= tasks.Count)
                return;

            var targetTask = tasks[targetIndex].Task;
            if (targetTask == null || column.ColumnData == null)
                return;

            var baseIndex = column.ColumnData.Tasks.IndexOf(targetTask);
            if (baseIndex < 0)
                return;

            var insertIndex = delta > 0 ? baseIndex + 1 : baseIndex;
            var manager = new FlowKanbanManager(this, autoAttach: false);
            var targetLaneId = column.LaneFilterId;
            var result = manager.TryMoveTaskWithWipEnforcement(task, column.ColumnData, insertIndex, targetLaneId, enforceHard: false);
            if (result == MoveResult.AllowedWithWipWarning)
            {
                ShowWipWarning(column.ColumnData, targetLaneId);
            }

            QueueFocusAction(() =>
            {
                FocusTaskOrColumn(column, task);
            });
        }

        private static int GetLaneInsertIndex(FlowKanbanColumnData targetColumn, string? laneId, int desiredLaneIndex)
        {
            var laneTasks = new List<FlowTask>();
            foreach (var task in targetColumn.Tasks)
            {
                if (LaneIdsMatch(task.LaneId, laneId))
                {
                    laneTasks.Add(task);
                }
            }

            if (laneTasks.Count == 0)
                return targetColumn.Tasks.Count;

            if (desiredLaneIndex >= laneTasks.Count)
            {
                var lastTask = laneTasks[^1];
                var lastIndex = targetColumn.Tasks.IndexOf(lastTask);
                return lastIndex + 1;
            }

            var targetTask = laneTasks[Math.Clamp(desiredLaneIndex, 0, laneTasks.Count - 1)];
            var targetIndex = targetColumn.Tasks.IndexOf(targetTask);
            return targetIndex < 0 ? targetColumn.Tasks.Count : targetIndex;
        }

        private static bool LaneIdsMatch(string? left, string? right)
        {
            return string.Equals(NormalizeLaneId(left), NormalizeLaneId(right), StringComparison.Ordinal);
        }

        private static bool IsTextInputElement(DependencyObject element)
        {
            if (element is TextBox || element is PasswordBox || element is RichEditBox || element is AutoSuggestBox)
                return true;

            if (element is ComboBox)
                return true;

            if (FindAncestor<TextBox>(element) != null
                || FindAncestor<PasswordBox>(element) != null
                || FindAncestor<RichEditBox>(element) != null
                || FindAncestor<AutoSuggestBox>(element) != null
                || FindAncestor<ComboBox>(element) != null)
            {
                return true;
            }

            return FindAncestor<DaisyInput>(element) != null;
        }

        private static bool MoveFocusToElement(DependencyObject? element)
        {
            if (element is not UIElement uiElement)
                return false;

            uiElement.Focus(FocusState.Keyboard);
            uiElement.StartBringIntoView();
            return true;
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

        private static T? FindDescendant<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null)
                return default;

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                    return match;

                var found = FindDescendant<T>(child);
                if (found != null)
                    return found;
            }

            return default;
        }

        private void ClampKeyboardColumnIndex()
        {
            if (Board.Columns.Count == 0)
            {
                _keyboardColumnIndex = InvalidColumnIndex;
                return;
            }

            if (_keyboardColumnIndex == InvalidColumnIndex)
            {
                _keyboardColumnIndex = Board.Columns.Count - 1;
                return;
            }

            _keyboardColumnIndex = Math.Clamp(_keyboardColumnIndex, 0, Board.Columns.Count - 1);
        }
    }
}

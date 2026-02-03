namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// Partial class containing drag and drop support for FlowKanban.
    /// </summary>
    public partial class FlowKanban
    {
        #region Column Drag and Drop Support

        private Microsoft.UI.Xaml.DragEventHandler? _boardDragOverHandler;
        private Microsoft.UI.Xaml.DragEventHandler? _boardDropHandler;
        private Microsoft.UI.Xaml.DragEventHandler? _boardDragLeaveHandler;
#if HAS_UNO_SKIA
        private bool _isPointerColumnReorderActive;
        private string? _pointerDraggedColumnId;
#endif

        private void AttachColumnDragHandlers()
        {
            DetachColumnDragHandlers();

            var standardColumns = FindChild<ItemsControl>(this, ic => ic.Name == "PART_ColumnsItemsControl");
            var standardDropLayer = FindChild<Canvas>(this, c => c.Name == "PART_ColumnDropLayer");
            AttachColumnDragHandlers(standardColumns, standardDropLayer);

            var swimlaneColumns = FindChild<ItemsControl>(this, ic => ic.Name == "PART_SwimlaneColumnsItemsControl");
            var swimlaneDropLayer = FindChild<Canvas>(this, c => c.Name == "PART_SwimlaneColumnDropLayer");
            AttachColumnDragHandlers(swimlaneColumns, swimlaneDropLayer);
        }

        private void AttachColumnDragHandlers(ItemsControl? itemsControl, Canvas? dropLayer)
        {
            if (itemsControl == null || _columnItemsControls.Contains(itemsControl))
                return;

            itemsControl.AllowDrop = true;
            itemsControl.DragOver += OnColumnDragOver;
            itemsControl.Drop += OnColumnDrop;
            itemsControl.DragLeave += OnColumnDragLeave;
            _columnItemsControls.Add(itemsControl);
            if (dropLayer != null)
            {
                dropLayer.IsHitTestVisible = false;
                _columnDropLayers[itemsControl] = dropLayer;
            }
        }

        private void DetachColumnDragHandlers()
        {
            foreach (var itemsControl in _columnItemsControls)
            {
                itemsControl.DragOver -= OnColumnDragOver;
                itemsControl.Drop -= OnColumnDrop;
                itemsControl.DragLeave -= OnColumnDragLeave;
            }

            _columnItemsControls.Clear();
            _columnDropLayers.Clear();
        }

        private void AttachBoardDragHandlers()
        {
            if (_boardContentHost == null)
                return;

            _boardDragOverHandler ??= OnBoardDragOver;
            _boardDropHandler ??= OnBoardDrop;
            _boardDragLeaveHandler ??= OnBoardDragLeave;

            _boardContentHost.AllowDrop = true;
            if (_boardDragOverHandler != null)
                _boardContentHost.RemoveHandler(UIElement.DragOverEvent, _boardDragOverHandler);
            if (_boardDropHandler != null)
                _boardContentHost.RemoveHandler(UIElement.DropEvent, _boardDropHandler);
            if (_boardDragLeaveHandler != null)
                _boardContentHost.RemoveHandler(UIElement.DragLeaveEvent, _boardDragLeaveHandler);

            if (_boardDragOverHandler != null)
                _boardContentHost.AddHandler(UIElement.DragOverEvent, _boardDragOverHandler, handledEventsToo: true);
            if (_boardDropHandler != null)
                _boardContentHost.AddHandler(UIElement.DropEvent, _boardDropHandler, handledEventsToo: true);
            if (_boardDragLeaveHandler != null)
                _boardContentHost.AddHandler(UIElement.DragLeaveEvent, _boardDragLeaveHandler, handledEventsToo: true);
        }

        private void DetachBoardDragHandlers()
        {
            if (_boardContentHost == null)
                return;

            if (_boardDragOverHandler != null)
                _boardContentHost.RemoveHandler(UIElement.DragOverEvent, _boardDragOverHandler);
            if (_boardDropHandler != null)
                _boardContentHost.RemoveHandler(UIElement.DropEvent, _boardDropHandler);
            if (_boardDragLeaveHandler != null)
                _boardContentHost.RemoveHandler(UIElement.DragLeaveEvent, _boardDragLeaveHandler);
        }

        private static bool HasColumnDragData(DataPackageView dataView)
        {
            return dataView.Contains(ColumnDragDataFormat)
                   || dataView.Properties.ContainsKey(ColumnDragDataPropertyKey);
        }

        private void OnColumnDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (!HasColumnDragData(e.DataView))
                return;

            if (sender is not ItemsControl itemsControl)
                return;

            var panel = itemsControl.ItemsPanelRoot as StackPanel;
            if (panel == null)
                return;

            var position = e.GetPosition(panel);
            var draggedColumnId = GetDraggedColumnId(e.DataView);
            if (!TryGetColumnInsertIndex(panel, position.X, draggedColumnId, out var insertIndex))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                HideColumnDropIndicator();
                return;
            }

            e.AcceptedOperation = DataPackageOperation.Move;

            if (insertIndex != _currentColumnDropIndex)
            {
                HideColumnDropIndicator();
                ShowColumnDropIndicator(itemsControl, panel, insertIndex, draggedColumnId);
            }
        }

        private void OnColumnDragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (HasColumnDragData(e.DataView))
            {
                HideColumnDropIndicator();
            }
        }

        private async void OnColumnDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (!HasColumnDragData(e.DataView))
                return;

            var data = await e.DataView.GetDataAsync(ColumnDragDataFormat);
            var columnId = data as string ?? data?.ToString();

            if (string.IsNullOrWhiteSpace(columnId))
            {
                HideColumnDropIndicator();
                return;
            }

            var column = FindColumnById(columnId);
            if (column == null)
            {
                HideColumnDropIndicator();
                return;
            }

            if (sender is not ItemsControl itemsControl)
            {
                HideColumnDropIndicator();
                return;
            }

            var panel = itemsControl.ItemsPanelRoot as StackPanel;
            if (panel == null)
            {
                HideColumnDropIndicator();
                return;
            }

            var draggedColumnId = GetDraggedColumnId(e.DataView);
            var insertIndex = _currentColumnDropIndex;
            if (insertIndex < 0)
            {
                var position = e.GetPosition(panel);
                if (!TryGetColumnInsertIndex(panel, position.X, draggedColumnId, out insertIndex))
                {
                    HideColumnDropIndicator();
                    return;
                }
            }

            HideColumnDropIndicator();

            var currentIndex = Board.Columns.IndexOf(column);
            if (currentIndex < 0)
                return;

            insertIndex = Math.Clamp(insertIndex, 0, Board.Columns.Count);

            if (insertIndex == currentIndex)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            manager.MoveColumn(column, insertIndex);
        }

        private void OnBoardDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (!HasColumnDragData(e.DataView))
                return;

            if (!TryGetActiveColumnsContext(out var itemsControl, out var panel, out var dropLayer))
                return;

            if (panel == null)
                return;

            e.Handled = true;
            e.AcceptedOperation = DataPackageOperation.Move;

            var position = e.GetPosition(panel);
            var draggedColumnId = GetDraggedColumnId(e.DataView);
            if (!TryGetColumnInsertIndex(panel, position.X, draggedColumnId, out var insertIndex))
            {
                HideColumnDropIndicator();
                return;
            }

            if (dropLayer == null)
                return;

            if (insertIndex != _currentColumnDropIndex)
            {
                HideColumnDropIndicator();
                ShowColumnDropIndicator(itemsControl, panel, insertIndex, draggedColumnId);
            }
        }

        private void OnBoardDragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (HasColumnDragData(e.DataView))
            {
                e.Handled = true;
                HideColumnDropIndicator();
            }
        }

        private async void OnBoardDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (!HasColumnDragData(e.DataView))
                return;

            if (!TryGetActiveColumnsContext(out var itemsControl, out var panel, out _))
                return;

            if (panel == null)
                return;

            e.Handled = true;

            var data = await e.DataView.GetDataAsync(ColumnDragDataFormat);
            var columnId = data as string ?? data?.ToString();
            if (string.IsNullOrWhiteSpace(columnId))
            {
                columnId = GetDraggedColumnId(e.DataView);
            }

            if (string.IsNullOrWhiteSpace(columnId))
            {
                HideColumnDropIndicator();
                return;
            }

            var column = FindColumnById(columnId);
            if (column == null)
            {
                HideColumnDropIndicator();
                return;
            }

            var draggedColumnId = GetDraggedColumnId(e.DataView);
            var insertIndex = _currentColumnDropIndex;
            if (insertIndex < 0)
            {
                var position = e.GetPosition(panel);
                if (!TryGetColumnInsertIndex(panel, position.X, draggedColumnId, out insertIndex))
                {
                    HideColumnDropIndicator();
                    return;
                }
            }

            HideColumnDropIndicator();

            var currentIndex = Board.Columns.IndexOf(column);
            if (currentIndex < 0)
                return;

            insertIndex = Math.Clamp(insertIndex, 0, Board.Columns.Count);

            if (insertIndex == currentIndex)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            manager.MoveColumn(column, insertIndex);
        }

        private bool TryGetActiveColumnsContext(
            out ItemsControl itemsControl,
            out StackPanel? panel,
            out Canvas? dropLayer)
        {
            var targetItemsControl = IsSwimlaneLayoutVisible && _swimlaneColumnsHost != null
                ? _swimlaneColumnsHost
                : _standardColumnsHost;

            if (targetItemsControl == null)
            {
                itemsControl = null!;
                panel = null;
                dropLayer = null;
                return false;
            }

            itemsControl = targetItemsControl;
            panel = itemsControl.ItemsPanelRoot as StackPanel;
            if (panel == null)
            {
                dropLayer = null;
                return false;
            }

            dropLayer = GetDropLayerForItemsControl(itemsControl);
            return true;
        }

        private Canvas? GetDropLayerForItemsControl(ItemsControl? itemsControl)
        {
            if (itemsControl == null)
                return null;

            if (_columnDropLayers.TryGetValue(itemsControl, out var dropLayer))
                return dropLayer;

            var targetName = string.Equals(itemsControl.Name, "PART_SwimlaneColumnsItemsControl", StringComparison.Ordinal)
                ? "PART_SwimlaneColumnDropLayer"
                : "PART_ColumnDropLayer";

            dropLayer = FindChild<Canvas>(this, c => c.Name == targetName);
            if (dropLayer != null)
            {
                dropLayer.IsHitTestVisible = false;
                _columnDropLayers[itemsControl] = dropLayer;
            }

            return dropLayer;
        }

        private int CalculateColumnInsertIndex(StackPanel panel, double dropX, string? excludeColumnId)
        {
            var children = GetColumnElements(panel, excludeColumnId);
            if (children.Count == 0)
                return 0;

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var left = child.TransformToVisual(panel).TransformPoint(new Point(0, 0)).X;
                var midpoint = left + (child.ActualWidth / 2);
                if (dropX < midpoint)
                    return i;
            }

            return children.Count;
        }

        private bool TryGetColumnInsertIndex(
            StackPanel panel,
            double dropX,
            string? draggedColumnId,
            out int insertIndex)
        {
            insertIndex = -1;

            var layouts = GetColumnLayouts(panel);
            if (layouts.Count == 0)
                return false;

            if (string.IsNullOrWhiteSpace(draggedColumnId))
            {
                insertIndex = CalculateColumnInsertIndex(panel, dropX, null);
                return true;
            }

            var draggedIndex = layouts.FindIndex(info =>
                string.Equals(info.ColumnId, draggedColumnId, StringComparison.Ordinal));

            if (draggedIndex < 0)
            {
                insertIndex = CalculateColumnInsertIndex(panel, dropX, draggedColumnId);
                return true;
            }

            var dragged = layouts[draggedIndex];
            if (dropX >= dragged.Left && dropX <= dragged.Right)
                return false;

            if (draggedIndex > 0)
            {
                var prev = layouts[draggedIndex - 1];
                if (dropX > prev.Right && dropX < dragged.Left)
                    return false;
            }

            if (draggedIndex < layouts.Count - 1)
            {
                var next = layouts[draggedIndex + 1];
                if (dropX > dragged.Right && dropX < next.Left)
                    return false;
            }

            ColumnLayoutInfo? hovered = null;
            foreach (var info in layouts)
            {
                if (dropX >= info.Left && dropX <= info.Right)
                {
                    hovered = info;
                    break;
                }
            }

            if (hovered != null)
            {
                if (hovered.Index == draggedIndex - 1)
                {
                    if (dropX >= hovered.Midpoint)
                        return false;

                    insertIndex = hovered.Index;
                    return true;
                }

                if (hovered.Index == draggedIndex + 1)
                {
                    if (dropX <= hovered.Midpoint)
                        return false;

                    insertIndex = hovered.Index;
                    return true;
                }
            }

            insertIndex = CalculateColumnInsertIndex(panel, dropX, draggedColumnId);
            return true;
        }

        private void EnsureColumnDropIndicator()
        {
            if (_columnDropIndicator != null)
                return;

            _columnDropIndicator = new Rectangle
            {
                Width = 4,
                RadiusX = 2,
                RadiusY = 2,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(2, 0, 2, 0)
            };
            _columnDropIndicator.Fill = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
        }

        private void ShowColumnDropIndicator(
            ItemsControl itemsControl,
            StackPanel panel,
            int index,
            string? draggedColumnId)
        {
            EnsureColumnDropIndicator();
            if (_columnDropIndicator == null)
                return;

            if (!_columnDropLayers.TryGetValue(itemsControl, out var dropLayer))
                return;

            if (_columnDropIndicator.Parent is Panel oldParent)
            {
                oldParent.Children.Remove(_columnDropIndicator);
            }

            var visualIndex = Math.Clamp(index, 0, panel.Children.Count);
            var panelOrigin = panel.TransformToVisual(dropLayer).TransformPoint(new Point(0, 0));
            var offset = CalculateColumnInsertOffset(panel, visualIndex, draggedColumnId);
            _columnDropIndicator.Height = Math.Max(1, panel.ActualHeight - 8);
            Canvas.SetLeft(_columnDropIndicator, panelOrigin.X + offset);
            Canvas.SetTop(_columnDropIndicator, panelOrigin.Y + 4);
            if (!dropLayer.Children.Contains(_columnDropIndicator))
                dropLayer.Children.Add(_columnDropIndicator);
            _currentColumnDropIndex = index;
        }

        private static double CalculateColumnInsertOffset(StackPanel panel, int index, string? excludeColumnId)
        {
            var children = GetColumnElements(panel, excludeColumnId);
            if (children.Count == 0)
                return 0;

            if (index <= 0)
                return children[0].TransformToVisual(panel).TransformPoint(new Point(0, 0)).X;

            if (index >= children.Count)
            {
                var last = children[^1];
                var lastLeft = last.TransformToVisual(panel).TransformPoint(new Point(0, 0)).X;
                return lastLeft + last.ActualWidth;
            }

            return children[index].TransformToVisual(panel).TransformPoint(new Point(0, 0)).X;
        }

        private sealed class ColumnLayoutInfo
        {
            public required string ColumnId { get; init; }
            public required int Index { get; init; }
            public required double Left { get; init; }
            public required double Right { get; init; }
            public required double Midpoint { get; init; }
        }

        private static List<ColumnLayoutInfo> GetColumnLayouts(StackPanel panel)
        {
            var layouts = new List<ColumnLayoutInfo>();
            var index = 0;

            foreach (var child in panel.Children)
            {
                if (child is not FrameworkElement fe)
                    continue;

                if (!TryGetColumnDataId(fe, out var columnId) || string.IsNullOrWhiteSpace(columnId))
                    continue;

                var left = fe.TransformToVisual(panel).TransformPoint(new Point(0, 0)).X;
                var right = left + fe.ActualWidth;
                layouts.Add(new ColumnLayoutInfo
                {
                    ColumnId = columnId,
                    Index = index,
                    Left = left,
                    Right = right,
                    Midpoint = left + (fe.ActualWidth / 2)
                });

                index++;
            }

            return layouts;
        }

        private static List<FrameworkElement> GetColumnElements(StackPanel panel, string? excludeColumnId)
        {
            var children = new List<FrameworkElement>();
            foreach (var child in panel.Children)
            {
                if (child is FrameworkElement fe)
                {
                    if (excludeColumnId != null
                        && TryGetColumnDataId(fe, out var columnId)
                        && string.Equals(columnId, excludeColumnId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    children.Add(fe);
                }
            }

            return children;
        }

        private static bool TryGetColumnDataId(FrameworkElement element, out string? columnId)
        {
            // 1. Check if the element itself is a FlowKanbanColumn
            if (element is FlowKanbanColumn columnControl && columnControl.ColumnData != null)
            {
                columnId = columnControl.ColumnData.Id;
                return true;
            }

            // 2. Check if it's a ContentPresenter wrapping a FlowKanbanColumn
            if (element is ContentPresenter cp && cp.Content is FlowKanbanColumn wrappedColumn && wrappedColumn.ColumnData != null)
            {
                columnId = wrappedColumn.ColumnData.Id;
                return true;
            }

            // 3. Check DataContext (works for direct bindings or if inherited)
            if (element.DataContext is FlowKanbanColumnData dataContext)
            {
                columnId = dataContext.Id;
                return true;
            }

            // 4. Check if it's a ContentPresenter wrapping the data itself
            if (element is ContentPresenter presenter && presenter.Content is FlowKanbanColumnData content)
            {
                columnId = content.Id;
                return true;
            }

            columnId = null;
            return false;
        }

        private static string? GetDraggedColumnId(DataPackageView dataView)
        {
            if (dataView.Properties.TryGetValue(ColumnDragDataPropertyKey, out var value))
            {
                return value as string ?? value?.ToString();
            }

            return null;
        }

        private void HideColumnDropIndicator()
        {
            if (_columnDropIndicator?.Parent is Panel parent)
            {
                parent.Children.Remove(_columnDropIndicator);
            }

            _currentColumnDropIndex = -1;
        }

#if HAS_UNO_SKIA
        internal void BeginPointerColumnReorder(string columnId)
        {
            if (!_isColumnReorderActive)
                return;

            if (string.IsNullOrWhiteSpace(columnId))
                return;

            _pointerDraggedColumnId = columnId;
            _isPointerColumnReorderActive = true;
            HideColumnDropIndicator();
        }

        internal void UpdatePointerColumnReorder(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!_isPointerColumnReorderActive)
                return;

            var draggedColumnId = _pointerDraggedColumnId;
            if (string.IsNullOrWhiteSpace(draggedColumnId))
                return;

            if (!TryGetActiveColumnsContext(out var itemsControl, out var panel, out var dropLayer))
                return;

            if (panel == null || dropLayer == null)
                return;

            var position = e.GetCurrentPoint(panel).Position;
            if (!TryGetColumnInsertIndex(panel, position.X, draggedColumnId, out var insertIndex))
            {
                HideColumnDropIndicator();
                return;
            }

            if (insertIndex != _currentColumnDropIndex)
            {
                HideColumnDropIndicator();
                ShowColumnDropIndicator(itemsControl, panel, insertIndex, draggedColumnId);
            }
        }

        internal void CompletePointerColumnReorder(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!_isPointerColumnReorderActive)
                return;

            var draggedColumnId = _pointerDraggedColumnId;
            _isPointerColumnReorderActive = false;
            _pointerDraggedColumnId = null;

            if (string.IsNullOrWhiteSpace(draggedColumnId))
            {
                HideColumnDropIndicator();
                DragEnded?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (!TryGetActiveColumnsContext(out _, out var panel, out _))
            {
                HideColumnDropIndicator();
                DragEnded?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (panel == null)
            {
                HideColumnDropIndicator();
                DragEnded?.Invoke(this, EventArgs.Empty);
                return;
            }

            var insertIndex = _currentColumnDropIndex;
            if (insertIndex < 0)
            {
                var position = e.GetCurrentPoint(panel).Position;
                if (!TryGetColumnInsertIndex(panel, position.X, draggedColumnId, out insertIndex))
                {
                    HideColumnDropIndicator();
                    DragEnded?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            HideColumnDropIndicator();

            var column = FindColumnById(draggedColumnId);
            if (column == null)
            {
                DragEnded?.Invoke(this, EventArgs.Empty);
                return;
            }

            var currentIndex = Board.Columns.IndexOf(column);
            if (currentIndex < 0)
            {
                DragEnded?.Invoke(this, EventArgs.Empty);
                return;
            }

            insertIndex = Math.Clamp(insertIndex, 0, Board.Columns.Count);
            if (insertIndex != currentIndex)
            {
                var manager = new FlowKanbanManager(this, autoAttach: false);
                manager.MoveColumn(column, insertIndex);
            }

            DragEnded?.Invoke(this, EventArgs.Empty);
        }

        internal void CancelPointerColumnReorder()
        {
            if (!_isPointerColumnReorderActive)
                return;

            _isPointerColumnReorderActive = false;
            _pointerDraggedColumnId = null;
            HideColumnDropIndicator();
            DragEnded?.Invoke(this, EventArgs.Empty);
        }
#endif

        private FlowKanbanColumnData? FindColumnById(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
                return null;

            foreach (var column in Board.Columns)
            {
                if (string.Equals(column.Id, columnId, StringComparison.Ordinal))
                    return column;
            }

            return null;
        }

        #endregion

        #region Drag and Drop Support

        // Store the currently dragged task and its source column
        private FlowTask? _draggedTask;
        private FlowKanbanColumnData? _dragSourceColumn;

        /// <summary>
        /// Called when a task card starts being dragged.
        /// </summary>
        public void OnTaskDragStarting(FlowTask task, FlowKanbanColumnData sourceColumn)
        {
            _draggedTask = task;
            _dragSourceColumn = sourceColumn;
        }

        /// <summary>
        /// Called when a task is dropped onto a column.
        /// Moves the task from its source column to the target column.
        /// </summary>
        public void OnTaskDropped(FlowKanbanColumnData targetColumn, int insertIndex = -1)
        {
            if (_draggedTask == null || _dragSourceColumn == null)
                return;

            var manager = new FlowKanbanManager(this, autoAttach: false);
            int? targetIndex = insertIndex >= 0 ? insertIndex : null;
            var result = manager.TryMoveTaskWithWipEnforcement(_draggedTask, targetColumn, targetIndex, enforceHard: false);
            if (result == MoveResult.AllowedWithWipWarning)
            {
                ShowWipWarning(targetColumn, _draggedTask.LaneId);
            }

            // Clear drag state and notify
            _draggedTask = null;
            _dragSourceColumn = null;
            DragEnded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when drag is cancelled.
        /// </summary>
        public void OnTaskDragCancelled()
        {
            _draggedTask = null;
            _dragSourceColumn = null;
            DragEnded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets the currently dragged task, if any.
        /// </summary>
        public FlowTask? DraggedTask => _draggedTask;

        #endregion
    }
}

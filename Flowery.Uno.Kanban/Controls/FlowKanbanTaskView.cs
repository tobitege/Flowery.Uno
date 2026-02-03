using System;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// View-only wrapper for lane-aware task rendering.
    /// </summary>
    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed class FlowKanbanTaskView
    {
        public FlowKanbanTaskView(FlowTask task, FlowKanbanLane? lane, bool showLaneHeader)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            Lane = lane;
            ShowLaneHeader = showLaneHeader;
        }

        public FlowTask Task { get; }

        public FlowKanbanLane? Lane { get; }

        public bool ShowLaneHeader { get; }
    }
}

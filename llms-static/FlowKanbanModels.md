<!-- Supplementary documentation for FlowKanbanModels -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

The Kanban system relies on a set of lightweight, observable data models designed for both UI binding and JSON serialization. These models use `ObservableCollection` and implement `INotifyPropertyChanged` to ensure that any programmatic changes (like adding a task or moving a column) are instantly reflected in the `FlowKanban` user interface.

## Models

### FlowTask

Represents an individual item or "card" on the board with full project management properties.

#### Core Properties

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Unique identifier (GUID). |
| `Title` | `string` | The header text for the task. |
| `Description` | `string` | Detailed text or body content. |
| `Palette` | `DaisyColor` | The visual theme applied to the card. |

#### Scheduling Properties

| Property | Type | Description |
| --- | --- | --- |
| `PlannedStartDate` | `DateTime?` | Planned start date for the task. |
| `PlannedEndDate` | `DateTime?` | Planned end/due date for the task. |
| `ActualStartDate` | `DateTime?` | Actual start date when work began. |
| `ActualEndDate` | `DateTime?` | Actual completion date. |

#### Effort Tracking Properties

| Property | Type | Description |
| --- | --- | --- |
| `EstimatedHours` | `double?` | Estimated effort in hours. |
| `ActualHours` | `double?` | Actual hours spent on the task. |
| `EstimatedDays` | `double?` | Estimated effort in days (for larger tasks). |
| `ActualDays` | `double?` | Actual days spent on the task. |

#### Status & Priority Properties

| Property | Type | Description |
| --- | --- | --- |
| `Priority` | `FlowTaskPriority` | Task priority level (Low, Normal, High, Urgent). |
| `ProgressPercent` | `int` | Manual progress percentage (0-100, clamped). |

#### Assignment & Categorization Properties

| Property | Type | Description |
| --- | --- | --- |
| `Assignee` | `string?` | Person assigned to this task. |
| `Tags` | `string?` | Comma-separated tags for categorization. |
| `LaneId` | `string?` | Optional swimlane identifier (null/empty/`"0"` treated as Unassigned). |

#### Subtasks

| Property | Type | Description |
| --- | --- | --- |
| `Subtasks` | `ObservableCollection<FlowSubtask>` | Child tasks for breakdown. |

#### Computed Properties (JSON Ignored)

| Property | Type | Description |
| --- | --- | --- |
| `IsOverdue` | `bool` | True if past `PlannedEndDate` without `ActualEndDate`. |
| `SubtaskProgress` | `int` | Completion % calculated from subtasks. |
| `EffectiveProgress` | `int` | Uses `SubtaskProgress` if subtasks exist, else `ProgressPercent`. |

### FlowSubtask

Represents a child task within a `FlowTask`.

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Unique identifier (GUID). |
| `Title` | `string` | The subtask text. |
| `IsCompleted` | `bool` | Whether the subtask is done. |

### FlowTaskPriority (Enum)

| Value | Description |
| --- | --- |
| `Low` | Low priority. |
| `Normal` | Default priority. |
| `High` | High priority. |
| `Urgent` | Critical/urgent priority. |

### FlowKanbanColumnData

Represents a single section or "bucket" on the board.

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Unique identifier (GUID). |
| `Title` | `string` | The header text displayed at the top of the column. |
| `Tasks` | `ObservableCollection<FlowTask>` | The list of tasks contained in this column. |

### FlowKanbanData

The root container for the entire board with metadata.

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Unique identifier (GUID). |
| `Title` | `string` | The board title displayed at the top of the Kanban. |
| `Description` | `string` | Optional description of the board's purpose. |
| `CreatedAt` | `DateTime` | Timestamp when the board was created. |
| `CreatedBy` | `string` | Name of the user who created the board. |
| `Columns` | `ObservableCollection<FlowKanbanColumnData>` | The list of columns making up the board. |
| `Lanes` | `ObservableCollection<FlowKanbanLane>` | The list of swimlanes available on the board. |
| `GroupBy` | `FlowKanbanGroupBy` | View-only grouping mode (None, Lane). |

### FlowKanbanLane

Represents a swimlane definition.

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Unique identifier (GUID). |
| `Title` | `string` | Lane display name. |
| `Description` | `string?` | Optional lane description. |
| `CustomFields` | `Dictionary<string, JsonElement>?` | Extensible fields for custom lane data. |

### FlowKanbanGroupBy (Enum)

| Value | Description |
| --- | --- |
| `None` | Standard board layout (no swimlanes). |
| `Lane` | Swimlane grouping and layout enabled. |

## Serialization Details

All models are optimized for `System.Text.Json` with explicit `[JsonPropertyName]` attributes. This ensures that the generated JSON keys remain consistent and compatible even if the C# property names are refactored.

### Example JSON Structure

```json
{
  "id": "board-123",
  "title": "My Project Board",
  "description": "Track tasks for my project",
  "createdAt": "2026-01-21T08:30:00",
  "createdBy": "John Doe",
  "lanes": [
    { "id": "l1", "title": "Backend", "description": null },
    { "id": "l2", "title": "Frontend", "description": null }
  ],
  "groupBy": 1,
  "columns": [
    {
      "id": "c1",
      "title": "To Do",
      "tasks": [
        {
          "id": "t1",
          "title": "Initial Setup",
          "description": "Configure the project workspace.",
          "palette": "Info",
          "laneId": "l1",
          "priority": "High",
          "progressPercent": 50,
          "plannedStartDate": "2026-01-20T00:00:00",
          "plannedEndDate": "2026-01-25T00:00:00",
          "estimatedHours": 8,
          "assignee": "Alice",
          "tags": "setup,infrastructure",
          "subtasks": [
            { "id": "s1", "title": "Create repo", "isCompleted": true },
            { "id": "s2", "title": "Configure CI", "isCompleted": false }
          ]
        }
      ]
    }
  ]
}
```

## Implementation Tips

- **Real-time Updates**: Always use the `ObservableCollection` instances (`Columns`, `Tasks`, `Subtasks`) to add or remove items to ensure the UI updates automatically.
- **GUID Generation**: The `Id` properties are automatically initialized with a new GUID string, ensuring uniqueness when creating new items programmatically.
- **DaisyColor Enumeration**: The `Palette` property uses the standard `DaisyColor` enum, making it easy to integrate with the rest of the Flowery ecosystem.
- **Progress Tracking**: Use `ProgressPercent` for manual progress, or populate `Subtasks` to have progress auto-calculated via `EffectiveProgress`.
- **Priority Levels**: Use `FlowTaskPriority` enum for consistent priority handling across the UI and persistence.
- **Overdue Detection**: The computed `IsOverdue` property enables easy visual flagging of tasks past their due date.
- **Unassigned Lane**: The "Unassigned" swimlane is a UI-only row derived from tasks with null/empty/`"0"`/unknown `LaneId`.

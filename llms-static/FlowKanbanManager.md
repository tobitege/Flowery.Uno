# FlowKanbanManager

A comprehensive programmatic API for managing Kanban boards in Flowery.Uno.

## Overview

`FlowKanbanManager` is a helper class that wraps a `FlowKanban` control and provides a fluent, intuitive API for all board management operations. It's ideal for:

- Programmatic board manipulation from ViewModels
- Test automation scenarios
- Undo/redo implementations
- Data-driven board population

## Quick Start

```csharp
using Flowery.Uno.Kanban.Controls;

// Create a manager for your Kanban control
var manager = new FlowKanbanManager(myFlowKanban);

// Optional: load settings and last board immediately
manager.Initialize();

// Create a default board with To Do, In Progress, Done columns
manager.CreateDefaultBoard();

// Add tasks
var todoColumn = manager.FindColumnByTitle("To Do");
manager.AddTask(todoColumn, "Design UI", "Create mockups", DaisyColor.Primary);
manager.AddTask(todoColumn, "Write tests");

// Move a task to another column
var task = manager.FindTaskById("...");
var inProgress = manager.FindColumnByTitle("In Progress");
manager.MoveTask(task, inProgress, 0);
```

## API Reference

### Properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Kanban` | `FlowKanban` | The underlying Kanban control |
| `Board` | `FlowKanbanData` | The board data model |
| `Columns` | `IReadOnlyList<FlowKanbanColumnData>` | All columns in the board |
| `BoardSize` | `DaisySize` | Current zoom level (get/set) |
| `ColumnCount` | `int` | Number of columns |
| `TotalTaskCount` | `int` | Total tasks across all columns |
| `AllTasks` | `IEnumerable<FlowTask>` | All tasks in the board |
| `CanZoomIn` | `bool` | Whether zoom in is possible |
| `CanZoomOut` | `bool` | Whether zoom out is possible |
| `ConfirmColumnRemovals` | `bool` | Whether column removals require confirmation (get/set) |
| `ConfirmCardRemovals` | `bool` | Whether card removals require confirmation (get/set) |
| `AutoSaveAfterEdits` | `bool` | Whether the board auto-saves after edits (get/set) |
| `LastBoardId` | `string?` | Last board ID loaded or saved by the control |

---

## Column Operations

### AddColumn

```csharp
FlowKanbanColumnData AddColumn(string title)
```

Adds a new column at the end of the board.

**Example:**

```csharp
var reviewColumn = manager.AddColumn("Code Review");
```

### InsertColumn

```csharp
FlowKanbanColumnData InsertColumn(string title, int index)
```

Inserts a column at a specific position.

**Example:**

```csharp
// Insert "Testing" between "In Progress" (index 1) and "Done" (index 2)
manager.InsertColumn("Testing", 2);
```

### RemoveColumn

```csharp
bool RemoveColumn(FlowKanbanColumnData column)
bool RemoveColumnByTitle(string title)
bool RemoveColumnAt(int index)
```

Removes a column and all its tasks.

Note: `RemoveColumnByTitle` returns `false` if multiple columns share the same title.

**Example:**

```csharp
manager.RemoveColumnByTitle("Archived");
// or
manager.RemoveColumnAt(0);
```

### FindColumnByTitle

```csharp
FlowKanbanColumnData? FindColumnByTitle(string title)
```

Finds a column by its title.

**Example:**

```csharp
var done = manager.FindColumnByTitle("Done");
if (done != null)
{
    // Work with the column
}
```

### GetColumnAt / GetColumnIndex

```csharp
FlowKanbanColumnData? GetColumnAt(int index)
int GetColumnIndex(FlowKanbanColumnData column)
```

Gets a column by index or gets the index of a column.

### GetTasksForColumn

```csharp
IEnumerable<FlowTask> GetTasksForColumn(FlowKanbanColumnData column)
IEnumerable<FlowTask> GetTasksForColumn(string columnId)
IEnumerable<FlowTask> GetTasksForColumn(int columnIndex)
```

Gets tasks for a specific column, by reference, ID, or index.

**Example:**

```csharp
var tasks = manager.GetTasksForColumn("col-123");
foreach (var task in tasks)
{
    // ...
}
```

### MoveColumn

```csharp
bool MoveColumn(FlowKanbanColumnData column, int newIndex)
```

Moves a column to a new position.

**Example:**

```csharp
var review = manager.FindColumnByTitle("Review");
manager.MoveColumn(review, 0); // Move to first position
```

### RenameColumn

```csharp
void RenameColumn(FlowKanbanColumnData column, string newTitle)
```

Renames a column.

### ClearColumns

```csharp
void ClearColumns()
```

Removes all columns from the board.

---

## Task Operations

### AddTask

```csharp
FlowTask AddTask(FlowKanbanColumnData column, string title, string? description = null, DaisyColor palette = DaisyColor.Default)
FlowTask? AddTask(int columnIndex, string title, string? description = null, DaisyColor palette = DaisyColor.Default)
```

Adds a task to a column.

**Example:**

```csharp
// By column reference
var task = manager.AddTask(todoColumn, "Fix bug", "Critical issue", DaisyColor.Error);

// By column index
manager.AddTask(0, "New feature", "Description here", DaisyColor.Primary);
```

### AddTaskToColumn / InsertTaskToColumn

```csharp
FlowTask? AddTaskToColumn(string columnId, string title, string? description = null, DaisyColor palette = DaisyColor.Default, string? laneId = null)
FlowTask? InsertTaskToColumn(string columnId, int index, string title, string? description = null, DaisyColor palette = DaisyColor.Default, string? laneId = null)
```

Adds or inserts a task by column ID.

**Example:**

```csharp
manager.AddTaskToColumn("col-123", "Wireframes", "Landing page", DaisyColor.Primary);
manager.InsertTaskToColumn("col-123", 0, "Top priority", null, DaisyColor.Error);
```

### InsertTask

```csharp
FlowTask InsertTask(FlowKanbanColumnData column, int index, string title, string? description = null, DaisyColor palette = DaisyColor.Default)
```

Inserts a task at a specific position in a column.

**Example:**

```csharp
// Insert at the top of the column
manager.InsertTask(todoColumn, 0, "Urgent task", null, DaisyColor.Error);
```

### RemoveTask

```csharp
bool RemoveTask(FlowTask task)
bool RemoveTaskById(string taskId)
```

Removes a task from the board.

### FindTaskById

```csharp
FlowTask? FindTaskById(string taskId)
```

Finds a task by its unique ID.

### FindTaskInColumn / UpdateTaskInColumn / RemoveTaskFromColumn

```csharp
FlowTask? FindTaskInColumn(string columnId, string taskId)
bool UpdateTaskInColumn(string columnId, string taskId, string? title = null, string? description = null, DaisyColor? palette = null)
bool RemoveTaskFromColumn(FlowKanbanColumnData column, FlowTask task)
bool RemoveTaskFromColumn(string columnId, string taskId)
```

Column-scoped task helpers when you already know the column.

### FindTasks

```csharp
IEnumerable<FlowTask> FindTasks(Func<FlowTask, bool> predicate)
```

Finds all tasks matching a condition.

**Example:**

```csharp
// Find all high-priority tasks
var urgentTasks = manager.FindTasks(t => t.Palette == DaisyColor.Error);

// Find tasks with "bug" in title
var bugs = manager.FindTasks(t => t.Title.Contains("bug", StringComparison.OrdinalIgnoreCase));
```

### FindColumnForTask

```csharp
FlowKanbanColumnData? FindColumnForTask(FlowTask task)
FlowKanbanColumnData? FindColumnForTaskById(string taskId)
```

Finds which column contains a task.

### MoveTask

```csharp
bool MoveTask(FlowTask task, FlowKanbanColumnData targetColumn, int? index = null)
```

Moves a task to a different column, optionally at a specific index.

**Example:**

```csharp
var task = manager.FindTaskById("abc123");
var doneColumn = manager.FindColumnByTitle("Done");

// Move to end of column
manager.MoveTask(task, doneColumn);

// Move to top of column
manager.MoveTask(task, doneColumn, 0);
```

### ReorderTask

```csharp
bool ReorderTask(FlowTask task, int newIndex)
```

Moves a task within its current column to a new position.

### UpdateTask

```csharp
void UpdateTask(FlowTask task, string? title = null, string? description = null, DaisyColor? palette = null)
```

Updates task properties. Only non-null values are applied.

**Example:**

```csharp
// Update only the title
manager.UpdateTask(task, title: "New title");

// Update multiple properties
manager.UpdateTask(task, 
    title: "Updated title",
    description: "New description",
    palette: DaisyColor.Success);
```

### ClearAllTasks / ClearColumnTasks

```csharp
void ClearAllTasks()
void ClearColumnTasks(FlowKanbanColumnData column)
```

Clears tasks from all columns or a specific column.

---

## Subtask Operations

### AddSubtask

```csharp
FlowSubtask AddSubtask(FlowTask task, string title, bool isCompleted = false)
```

Adds a subtask (checklist item) to a task.

**Example:**

```csharp
var task = manager.FindTaskById("...");
manager.AddSubtask(task, "Write unit tests");
manager.AddSubtask(task, "Update documentation");
manager.AddSubtask(task, "Code review", true); // Already completed
```

### RemoveSubtask

```csharp
bool RemoveSubtask(FlowTask task, FlowSubtask subtask)
```

Removes a subtask from a task.

### ToggleSubtask

```csharp
void ToggleSubtask(FlowSubtask subtask)
```

Toggles a subtask's completion status.

### SetAllSubtasksCompleted

```csharp
void SetAllSubtasksCompleted(FlowTask task, bool isCompleted)
```

Sets all subtasks to completed or not completed.

**Example:**

```csharp
// Mark all subtasks as done
manager.SetAllSubtasksCompleted(task, true);
```

### ClearSubtasks

```csharp
void ClearSubtasks(FlowTask task)
```

Removes all subtasks from a task.

### GetSubtaskProgress

```csharp
(int completed, int total) GetSubtaskProgress(FlowTask task)
```

Gets the completion progress of subtasks.

**Example:**

```csharp
var (completed, total) = manager.GetSubtaskProgress(task);
Console.WriteLine($"Progress: {completed}/{total}");
```

---

## Zoom Operations

### ZoomIn / ZoomOut

```csharp
bool ZoomIn()
bool ZoomOut()
```

Increases or decreases the board size tier.

**Example:**

```csharp
while (manager.CanZoomIn)
{
    manager.ZoomIn();
}
```

### SetZoom

```csharp
void SetZoom(DaisySize size)
```

Sets the board to a specific size tier.

**Example:**

```csharp
manager.SetZoom(DaisySize.Large);
```

---

## Board Operations

### ClearBoard

```csharp
void ClearBoard()
```

Removes all columns and tasks from the board.

### ResetBoard

```csharp
void ResetBoard(params string[] columnTitles)
```

Clears the board and creates columns with the specified titles.

**Example:**

```csharp
manager.ResetBoard("Backlog", "Sprint", "In Progress", "Review", "Done");
```

### CreateDefaultBoard

```csharp
void CreateDefaultBoard()
```

Creates a standard Kanban board with "To Do", "In Progress", and "Done" columns.

### GetBoardData / SetBoardData

```csharp
FlowKanbanData GetBoardData()
void SetBoardData(FlowKanbanData data)
```

Gets or sets the complete board data model.

### ExportToJson / ImportFromJson

```csharp
string ExportToJson()
bool ImportFromJson(string json)
```

Serializes/deserializes the board to/from JSON.

**Example:**

```csharp
// Save board state
string json = manager.ExportToJson();
await File.WriteAllTextAsync("board.json", json);

// Load board state
string saved = await File.ReadAllTextAsync("board.json");
manager.ImportFromJson(saved);
```

---

## Statistics

### GetStatistics

```csharp
BoardStatistics GetStatistics()
```

Returns comprehensive statistics about the board.

```csharp
var stats = manager.GetStatistics();
Console.WriteLine($"Columns: {stats.ColumnCount}");
Console.WriteLine($"Total Tasks: {stats.TotalTaskCount}");
Console.WriteLine($"Empty Columns: {stats.EmptyColumns}");

foreach (var (column, count) in stats.TasksPerColumn)
{
    Console.WriteLine($"  {column}: {count} tasks");
}
```

### BoardStatistics Class

| Property | Type | Description |
| -------- | ---- | ----------- |
| `ColumnCount` | `int` | Number of columns |
| `TotalTaskCount` | `int` | Total number of tasks |
| `TasksPerColumn` | `Dictionary<string, int>` | Tasks grouped by column title |
| `EmptyColumns` | `int` | Number of columns with no tasks |

---

## Lifecycle and Persistence

`FlowKanbanManager` provides helper methods to load/save settings and board data, plus optional lifecycle hooks.

### Lifecycle

```csharp
void AttachLifecycle()
void DetachLifecycle()
bool Initialize()
void Shutdown()
```

- `AttachLifecycle` hooks `Loaded`/`Unloaded` on the control (constructor does this by default).
- `Initialize` loads settings and the last saved board, then raises `Initialized`.
- `Shutdown` saves board + settings, then raises `Teardown`.

### Persistence

```csharp
bool SaveBoard()
bool LoadLastBoard()
bool SaveSettings()
bool LoadSettings(bool forceReload = false)
bool ReloadSettings()
void ApplySettings(bool confirmColumnRemovals, bool confirmCardRemovals, bool autoSaveAfterEdits)
void EnableAutoSave()
void DisableAutoSave()
```

### Events

```csharp
event EventHandler? Initialized
event EventHandler? Teardown
event EventHandler? SettingsLoaded
event EventHandler? SettingsSaved
event EventHandler? BoardLoaded
event EventHandler? BoardSaved
event EventHandler<FlowKanbanPersistenceFailedEventArgs>? PersistenceFailed
```

`PersistenceFailed` provides the operation type and exception so callers can log or display errors.

---

## Complete Example

```csharp
public async Task SetupProjectBoard(FlowKanban kanban)
{
    var manager = new FlowKanbanManager(kanban);
    
    // Reset with custom columns
    manager.ResetBoard("Backlog", "Sprint", "In Dev", "Testing", "Done");
    
    // Add sample tasks with subtasks
    var backlog = manager.FindColumnByTitle("Backlog");
    
    var feature1 = manager.AddTask(backlog, 
        "User Authentication", 
        "Implement OAuth2 login flow", 
        DaisyColor.Primary);
    manager.AddSubtask(feature1, "Design login UI");
    manager.AddSubtask(feature1, "Implement OAuth provider");
    manager.AddSubtask(feature1, "Add session management");
    
    var bug1 = manager.AddTask(backlog, 
        "Fix memory leak", 
        "Dispose handlers on navigation", 
        DaisyColor.Error);
    
    // Move bug to sprint (urgent)
    var sprint = manager.FindColumnByTitle("Sprint");
    manager.MoveTask(bug1, sprint, 0);
    
    // Set zoom level
    manager.SetZoom(DaisySize.Medium);
    
    // Export for backup
    string backup = manager.ExportToJson();
    await SaveBackup(backup);
}
```

## See Also

- [FlowKanban Control](FlowKanban.md)
- [FlowTaskCard Control](FlowTaskCard.md)
- [FlowKanbanModels](FlowKanbanModels.md)

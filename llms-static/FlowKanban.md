<!-- Supplementary documentation for FlowKanban -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

`FlowKanban` is a high-level container component for building interactive Kanban boards. It manages a collection of columns and the tasks within them, providing a modern, industrial interface with specialized layout zones. It features a horizontally scrollable main board area and a vertical utility sidebar for board-level actions (home, settings, undo/redo, zoom, save/load, metrics/help).

## File Composition

The Kanban system spans multiple files for modularity:

| File | Description | Dependencies / Relationships |
| --- | --- | --- |
| `FlowKanban.cs` | Main control shell: core layout, persistence, and state management. | Dependency: `FlowKanbanManager`, `FlowKanbanHome` |
| `FlowKanban.Dialogs.cs` | Partial class: Encapsulates all modal dialog invocation logic. | Dependency: `FloweryDialogBase`, `FlowBoardEditorDialog` |
| `FlowKanban.Drop.cs` | Partial class: Handles Drag & Drop interactions for columns and cards. | Dependency: `FlowTask`, `FlowKanbanColumnData` |
| `FlowKanban.Home.cs` | Partial class: Orchestrates board navigation and Home screen integration. | Dependency: `FlowKanbanHome`, `FlowBoardMetadata` |
| `FlowKanban.UI.cs` | Partial class: Manages UI state, visual sizing tiers, and scrolling logic. | Dependency: `DaisySize` tokens |
| `FlowKanbanHome.cs` | **NEW** - Home screen control for multi-board management and search. | Dependency: `FlowBoardMetadata`, `FlowKanbanManager` |
| `FlowBoardMetadata.cs` | **NEW** - Lightweight model for board listings on the Home screen. | - |
| `FlowKanbanColumn.cs` | UI control: Renders a vertical column with a header and task items. | Dependency: `FlowKanbanColumnData`, `FlowTaskCard` |
| `FlowTaskCard.cs` | UI control: Visual representation of an individual Kanban task (card). | Dependency: `FlowTask` model |
| `FlowKanbanAddCard.cs` | UI control: Inline "Add Task" button/placeholder within a column. | Dependency: `FlowKanbanColumnData` |
| `FlowKanbanModels.cs` | Core data models: `FlowTask`, `FlowKanbanData`, `FlowKanbanColumnData`. | - |
| `FlowKanbanTaskView.cs` | **NEW** - View-only wrapper for lane-aware task rendering in swimlanes. | Dependency: `FlowTask`, `FlowKanbanLane` |
| `FlowKanbanManager.cs` | Central Engine: Full programmatic logic for board/task manipulation. | Dependency: `FlowKanbanData`, `FlowKanbanCommandHistory` |
| `FlowKanbanMigration.cs` | Migration Engine: Versions schema and upgrades legacy board JSON files. | Dependency: `FlowKanbanData` |
| `IBoardStore.cs` | Persistence abstraction for board data (swap JSON/DB/cloud). | Used by `FlowKanban`, `FlowKanbanBoardStore` |
| `FlowKanbanBoardStore.cs` | Default JSON board store with atomic save and key management. | Dependency: `IStateStorage`, `FlowKanbanBoardSanitizer` |
| `FlowKanbanBoardSanitizer.cs` | Load-time validation and sanitization (limits, control chars, invalid IDs). | Dependency: `FlowKanbanMigration` |
| `FlowKanbanCommands.cs` | Command implementations: Undo/redo transactional operations. | Dependency: `FlowKanbanManager` |
| `FlowKanbanCommandHistory.cs` | Transactional History: Manages the undo/redo stack. | Dependency: `FlowKanbanCommands` |
| `FlowKanbanJsonContext.cs` | Serialization: Source-generated JSON context for Native AOT support. | Dependency: `FlowKanbanModels` |
| `FlowTaskEditorDialog.cs` | Modal: Comprehensive editor for task details, dates, and effort. | Dependency: `FlowTask`, `FloweryDialogBase` |
| `FlowBoardEditorDialog.cs` | Modal: Edits board metadata and manages swimlane definitions. | Dependency: `FlowKanbanData`, `FloweryDialogBase` |
| `FlowKanbanSettingsDialog.cs` | Modal: Configures control behavior and UI preferences. | Dependency: `FlowKanban`, `FloweryDialogBase` |

## Key Features

- **Column Management**: Add or remove board sections dynamically.
- **Task Management**: Create and delete tasks within individual columns.
- **Add Card Placement**: Inline add card controls can appear at the top, bottom, or both (user preference).
- **WIP Limits**: Set per-column limits with automatic enforcement and visual warnings.
- **Swimlanes**: Group tasks vertically using lanes for matrix-style organization.
- **Unassigned Lane**: Legacy lane IDs (`0`, null, or unknown) surface in an auto-created "Unassigned" swimlane row.
- **Lane Management**: Create/reorder/delete lanes in board properties, with fallback lane selection on delete.
- **Lane-Aware Drag & Drop**: Dropping into a lane cell updates the task's `LaneId`.
- **Card Title UX**: Title font size is slightly reduced; wraps up to two lines with tooltips only when truncated.
- **Card Footer Badges**: Priority (non-normal) and progress (>0%) badges render in a responsive, wrapping footer row.
- **Blocked State**: Mark tasks as blocked with reasons and duration tracking (blocked tasks cannot move forward to later columns or be archived, and progress is capped at 99% until unblocked).
- **Undo/Redo**: Command history for card operations (move/add/delete/edit/palette/block/archive), bounded to 50 entries; new actions clear the redo stack.
- **Multi-Select**: Bulk operations (move, archive, delete) on selected tasks.
- **Multi-Board Management**: Navigate between multiple boards using the Home screen.
- **Search & Sort**: Filter boards by name and sort them by modification date or title.
- **Board Actions**: Rename, duplicate, delete, and export boards directly from the Home screen.
- **Drag & Drop Ready**: Columns and tasks are hosted in reorder-capable containers.
- **JSON Persistence**: Atomic save strategy (temp-file rename), schema migration support, and safe-load validation.
- **Source Gen Support**: Native AOT and WASM compatibility via `FlowKanbanJsonContext`.
- **Industrial Sidebar**: A dedicated space for board-level controls (home, settings, undo/redo, zoom, save/load, metrics/help).
- **User-Scoped Preferences**: Confirmation prompts, auto-save, add card placement, and welcome message settings are stored per user.
- **Global Admin Bootstrap**: Assigns the first available current user as global admin on initial run.
- **Local User Default**: If no provider is set, FlowKanban defaults to `LocalUserProvider`.
- **Provider Workflows**: External providers support explicit connect and disconnect flows, with confirmation before disconnecting and token validation after connect.

## Properties

| Property | Type | Description |
| --- | --- | --- |
| `Board` | `FlowKanbanData` | The root data model containing the columns and tasks. |
| `BoardStore` | `IBoardStore` | Persistence backend for boards (default JSON store). |
| `BoardSize` | `DaisySize` | Current size tier for board controls. |
| `ConfirmColumnRemovals` | `bool` | Require confirmation before deleting a column. |
| `ConfirmCardRemovals` | `bool` | Require confirmation before deleting a card. |
| `AutoSaveAfterEdits` | `bool` | Auto-save board after edits. |
| `AddCardPlacement` | `FlowKanbanAddCardPlacement` | User preference for inline add card placement (Bottom/Top/Both). |
| `EnableUndoRedo` | `bool` | Enables undo/redo command history. |
| `CommandHistory` | `FlowKanbanCommandHistory` | Undo/redo stack manager (counts + next descriptions). |
| `UndoCommand` | `ICommand` | Command to undo the last operation. |
| `RedoCommand` | `ICommand` | Command to redo the last undone operation. |
| `IsStatusBarVisible` | `bool` | Controls visibility of the status bar. |
| `LastBoardId` | `string?` | Last board ID loaded or saved via persistence. |
| `IsSwimlaneLayoutEnabled` | `bool` | True when `Board.GroupBy` is set to `Lane` and lanes exist. |
| `IsStandardLayoutEnabled` | `bool` | True when swimlanes are disabled or there are no lanes. |
| `LaneRows` | `ObservableCollection<FlowKanbanLaneRowView>` | Lane row view models used by the swimlane grid template. |
| `UserProvider` | `IUserProvider` | User provider for identity resolution and current-user context (defaults to `LocalUserProvider`). |
| `GlobalAdminId` | `string?` | Global admin user ID stored in app settings (if assigned). |

## User Providers & Identity Workflows

FlowKanban supports multiple user providers (local and external). The user management surface (`FlowKanbanUserManagement`) exposes consistent connect/disconnect UX and link/unlink workflows.

### Connect (External Providers)

- **GitHub**: Uses a personal access token (PAT) and **only loads the current user** (no org member enumeration).
- Token validation runs immediately after connect; invalid/insufficient scopes trigger a user-visible error and the token is cleared.
- Current requirement: `read:user` scope for GitHub.

### Disconnect (External Providers)

- Disconnect requires **user confirmation**.
- On confirm, the provider token is removed and any linked identities for that provider are cleared.
- The provider is re-listed as disconnected; user lists are refreshed.

### Link/Unlink Local Users

- External identities can be linked to a local user (optional).
- Unlinking removes only the link metadata; it does not delete the external or local user entries.

## Commands

`FlowKanban` provides a suite of pre-wired commands for easy integration with your UI or ViewModels:

| Command | Parameter | Action |
| --- | --- | --- |
| `AddColumnCommand` | None | Appends a new column to the board. |
| `RemoveColumnCommand` | `FlowKanbanColumnData` | Removes the specified column. |
| `EditColumnCommand` | `FlowKanbanColumnData` | Opens the column editor dialog for the specified column. |
| `AddCardCommand` | `FlowKanbanColumnData` | Adds a new task to the specified column. |
| `RemoveCardCommand` | `FlowTask` | Removes the specific task from its parent column. |
| `EditCardCommand` | `FlowTask` | Opens the task editor dialog. |
| `UndoCommand` | None | Undoes the last command (when enabled). |
| `RedoCommand` | None | Redoes the last undone command (when enabled). |
| `SaveCommand` | None | Serializes the current board to the app state storage (per-board ID). |
| `LoadCommand` | None | Deserializes the most recently saved board from app state storage. |
| `SettingsCommand` | None | Opens Kanban settings dialog. |
| `EditBoardCommand` | None | Opens board properties dialog. |
| `RenameBoardCommand` | None | Opens rename dialog for the board title. |
| `ZoomInCommand` | None | Increases board size tier. |
| `ZoomOutCommand` | None | Decreases board size tier. |
| `ToggleStatusBarCommand` | None | Toggles the status bar visibility. |

## Undo/Redo Behavior

- **History size**: 50 commands (`FlowKanbanCommandHistory.MaxHistorySize`).
- **Recorded operations**: move card, add card, delete card, edit title/description/palette, block/unblock, archive/unarchive (including bulk flows, which enqueue one command per task).
- **Not recorded**: priority/tags/due date edits, column operations, board settings, zoom, selection, and search/sort changes.
- **Redo invalidation**: executing a new command clears the redo stack.
- **Toggle**: when `EnableUndoRedo` is false, commands execute immediately without history.

## Dialogs

### FlowKanban.Dialogs.cs (Partial Class)

Contains all dialog-related methods, separated for maintainability. All dialogs use themed `DaisyButton` controls.

| Method | Purpose |
| --- | --- |
| `ExecuteAddColumn()` | Shows input dialog to add a new column. |
| `ExecuteRemoveColumn(column)` | Confirmation dialog before column deletion. |
| `ExecuteRenameColumn(column)` | Input dialog to rename a column. |
| `ExecuteEditBoard()` | Opens `FlowBoardEditorDialog` for board properties. |
| `ExecuteRenameBoard()` | Quick input dialog for renaming the board. |
| `ExecuteRemoveCard(task)` | Confirmation dialog before card deletion. |
| `ExecuteOpenSettings()` | Opens `FlowKanbanSettingsDialog`. |
| `ShowInputDialogAsync(...)` | Reusable themed input dialog. |
| `ShowConfirmDialogAsync(...)` | Reusable themed confirmation dialog. |

### FlowTaskEditorDialog

A full-featured modal for editing `FlowTask` objects. Uses `DaisyCollapse` sections for organized editing:

- **Details**: Title, description, palette color, subtasks.
- **Scheduling**: Due date, start date, reminder.
- **Effort**: Story points, estimated hours, priority, progress.
- **People**: Assignee field.

Based on `FloweryDialogBase` for smart sizing and automatic theming.

```csharp
var saved = await FlowTaskEditorDialog.ShowAsync(task, control.XamlRoot);
```

### FlowBoardEditorDialog

Edits board-level properties: title, description, created by, and displays read-only metadata (created date, board ID).
Includes swimlane enablement and full lane management (add, reorder, delete with fallback reassignment).

```csharp
var saved = await FlowBoardEditorDialog.ShowAsync(board, control.XamlRoot);
```

### FlowKanbanSettingsDialog

Configures Kanban instance settings. User-scoped preferences are persisted per user (see Persistence); board-level toggles update the current board. The undo/redo toggle applies immediately.

| Setting | Description |
| --- | --- |
| Confirm column removals | Require confirmation before deleting a column. |
| Confirm card removals | Require confirmation before deleting a card. |
| Auto-Save after edits | Auto-save board after any edit (uses the per-board storage key). |
| Auto-expand card details | Expand card details in the editor by default. |
| Enable undo/redo | Toggles command history and undo/redo UI (stored per user). |
| Display Add Card | User preference for inline add placement (Bottom/Top/Both). |
| Show archive column | Shows or hides the archive column for the current board. |
| Show welcome message | Toggle the Home welcome panel. |
| Welcome title | Home screen welcome title text. |
| Welcome message | Home screen welcome subtitle text. |
| Status bar visibility | Persisted in settings, not exposed in the dialog. |

```csharp
await FlowKanbanSettingsDialog.ShowAsync(kanban, control.XamlRoot);
```

## Layout Zones

### 1. Main Board Area (Left/Center)

The core workspace. It uses a `ScrollViewer` to allow horizontal navigation between columns. Each column is a vertical stack of `FlowTaskCard` controls.

### 1a. Swimlane Grid (When Enabled)

The swimlane layout renders a fixed header row for columns and a row per lane. Each lane row contains a column cell with lane filtering applied, enabling true matrix-style boards. The `FlowKanbanTaskView` wrapper is used to provide lane context to individual cards.

### 2. Utility Sidebar (Right)

A space-efficient vertical bar (`56px` wide) containing square `DaisyButton` controls for board-level management (home, settings, undo/redo, zoom, save/load, metrics/help).

### 3. Home Screen (Dashboard)

A high-level view for managing board collections. It features a responsive grid of boards with thumbnails, a search bar, sorting options, and a Create Board action in the toolbar. Users can navigate from the sidebar to the Home screen to manage their library of boards.

## Persistence (JSON)

Persistence is handled via `StateStorageProvider` and stores data in the same application settings location as other `.state` files.

- **User settings**: per-user entry `kanban.user.settings.<userId>` with fallback to legacy `kanban.settings` (includes confirmations, auto-save, add card placement, welcome message, undo/redo, and status bar visibility).
- **Board files**: one entry per board ID using `kanban.board.<id>`.
- **Board store**: `IBoardStore` allows swapping JSON for DB/cloud without changing the control.
- **Global admin**: app-level entry `kanban.admin.global` storing the bootstrap admin ID.
- **Atomic Save**: Uses temporary file and rename strategy for power-loss safety.
- **Migration**: Automatic upgrade of older board files via `FlowKanbanMigration`.
- **Load hardening**: input length/control characters are validated and invalid IDs are pruned on load.
- **Board ID**: `FlowKanbanData.Id` controls the storage key; if empty, a new GUID is generated on save.
- **Save/Load**: `SaveCommand` writes the current board, `LoadCommand` loads the last saved board.
- **Status bar**: `IsStatusBarVisible` is persisted with settings.

### Persistence API (safe wrappers)

| Method | Description |
| --- | --- |
| `TryLoadSettings(forceReload, out error)` | Loads persisted settings; returns false if an exception occurs. |
| `TrySaveSettings(out error)` | Saves current settings to storage. |
| `TrySaveBoard(out error)` | Saves the current board to the per-board storage key. |
| `TryLoadLastBoard(out error)` | Loads the last saved board (by `LastBoardId`). |

Board data is serialized using `System.Text.Json`. The data structure includes IDs, titles, descriptions, and palette settings for every task and column, ensuring a round-trip save/load preserves the entire board state.

```json
{
  "columns": [
    {
      "id": "...",
      "title": "Backlog",
      "tasks": [
        {
          "id": "...",
          "title": "Fix Auth",
          "description": "...",
          "palette": "Error"
        }
      ]
    }
  ]
}
```

## Usage Examples

### Minimal Kanban Setup

Simply dropping the control into a page provides a fully functional board.

```xml
<kanban:FlowKanban x:Name="ProjectBoard" />
```

### Programmatic Board Setup

You can also bind the `Board` property to an existing data structure in your ViewModel.

```xml
<kanban:FlowKanban Board="{x:Bind ViewModel.MyGlobalBoard}" />
```

## Interaction Tips

- **Reordering**: Drag columns to rearrange your workflow phases. Drag tasks within a column to prioritize them.
- **Swimlanes**: Toggle in board properties. Unassigned cards (legacy lane IDs) appear in the "Unassigned" lane until reassigned.
- **Side Actions**: Use the sidebar to save your progress frequently if auto-save is disabled.
- **Empty States**: If no columns exist, the board will show the background (`DaisyBase100Brush`) and is ready to add the first section via dialogs or keyboard shortcuts.

## FlowKanbanManager

`FlowKanbanManager` provides a **full programmatic API** for managing a `FlowKanban` board. It wraps the control and exposes fluent methods for columns, tasks, subtasks, zooming, serialization, and persistence lifecycle helpers.

### Initialization

```csharp
var manager = new FlowKanbanManager(myKanbanControl);
```

### Manager Properties

| Property | Type | Description |
| --- | --- | --- |
| `Kanban` | `FlowKanban` | The managed control. |
| `Board` | `FlowKanbanData` | The root data model. |
| `GroupBy` | `FlowKanbanGroupBy` | View-only grouping mode (None, Lane). |
| `CommandHistory` | `FlowKanbanCommandHistory` | The undo/redo manager instance. |
| `EnableUndoRedo` | `bool` | Enables transactional command history. |
| `Columns` | `IReadOnlyList<FlowKanbanColumnData>` | All columns. |
| `Lanes` | `IReadOnlyList<FlowKanbanLane>` | All swimlanes. |
| `ColumnCount` | `int` | Number of columns. |
| `TotalTaskCount` | `int` | Total tasks across all columns. |
| `BoardSize` | `DaisySize` | Current board size tier. |
| `AllTasks` | `IEnumerable<FlowTask>` | All tasks across all columns. |
| `CanZoomIn` | `bool` | Whether zoom in is possible. |
| `CanZoomOut` | `bool` | Whether zoom out is possible. |
| `ConfirmColumnRemovals` | `bool` | Require confirmation before deleting a column. |
| `ConfirmCardRemovals` | `bool` | Require confirmation before deleting a card. |
| `AutoSaveAfterEdits` | `bool` | Auto-save board after edits. |
| `LastBoardId` | `string?` | Last board ID loaded or saved by the control. |

### Column Management

| Method | Description |
| --- | --- |
| `AddColumn(title)` | Adds a new column at the end. |
| `InsertColumn(title, index)` | Inserts a column at a specific position. |
| `RemoveColumn(column)` | Removes a column and its tasks. |
| `RemoveColumnByTitle(title)` | Removes a column by title. |
| `RemoveColumnAt(index)` | Removes the column at an index. |
| `FindColumnByTitle(title)` | Finds a column by title. |
| `GetColumnAt(index)` | Gets the column at an index. |
| `GetColumnIndex(column)` | Gets the index of a column. |
| `MoveColumn(column, newIndex)` | Moves a column to a new position. |
| `RenameColumn(column, newTitle)` | Renames a column. |
| `ClearColumns()` | Removes all columns. |

### Task Management

| Method | Description |
| --- | --- |
| `AddTask(...)` | Adds a task with optional lane assignment. |
| `RemoveTask(task)` | Removes a task via `DeleteCardCommand`. |
| `RemoveTaskById(taskId)` | Removes a task by ID. |
| `FindTaskById(taskId)` | Finds a task by ID. |
| `FindTasks(predicate)` | Finds all tasks matching a predicate. |
| `FindColumnForTask(task)` | Finds the column containing a task. |
| `FindColumnForTaskById(taskId)` | Finds the column by task ID. |
| `MoveTask(task, targetColumn, index?)` | Moves task via `MoveCardCommand`. |
| `TryMoveTask(...)` | Moves task with event raising. |
| `TryMoveTaskWithWipEnforcement(...)` | Moves with automated WIP validation. |
| `ReorderTask(task, newIndex)` | Reorders a task within its column. |
| `UpdateTask(task, ...)` | Updates task via `EditCardCommand`. |
| `ClearAllTasks()` | Clears all tasks from all columns. |
| `ClearColumnTasks(column)` | Clears tasks from a specific column. |

### WIP & Policy Management

| Method | Description |
| --- | --- |
| `SetColumnWipLimit(col, limit)` | Sets max card limit for a column. |
| `IsWipExceeded(col)` | Checks if column is over its WIP limit. |
| `GetOverWipColumns()` | Returns all columns currently over limit. |
| `SetColumnPolicy(col, text)` | Defines DoD/Entry criteria for a column. |

### Blocked & Archive State

| Method | Description |
| --- | --- |
| `SetBlocked(task, reason?)` | Marks a task as blocked. |
| `ClearBlocked(task)` | Removes the blocked status. |
| `GetBlockedTasks()` | Returns all currently blocked items. |
| `ArchiveTask(task)` | Archives a task. |
| `ArchiveCompletedTasks(col)` | Archives all 100% progress tasks in a column. |
| `PurgeArchivedTasks()` | Permanently deletes all archived cards. |

Blocked policy behavior:

- Blocked cards cannot move forward to later columns or into the archive column.
- Blocked cards can still be edited and moved backward or reordered within a column.
- Progress is capped at 99% while blocked; unblocking restores normal progress updates.

### Lane Operations (Swimlanes)

| Method | Description |
| --- | --- |
| `AddLane(title)` | Appends a new lane to the board. |
| `InsertLane(title, index)` | Inserts a lane at a specific position. |
| `RemoveLane(lane, fallback?)` | Deletes lane and reassigns tasks to fallback. |
| `MoveLane(lane, newIndex)` | Rearranges lane order. |
| `GetTasksForLane(lane)` | Returns all tasks assigned to the lane. |
| `SetTaskLane(task, lane)` | Assigns or clears a task's lane. |

### Selection & Bulk Operations

| Method | Description |
| --- | --- |
| `SelectTask(task)` | Adds task to selection. |
| `DeselectAll()` | Clears current selection. |
| `BulkMove(targetColumn)` | Moves all selected tasks (with events). |
| `BulkSetPriority(priority)` | Updates priority for all selected tasks. |
| `BulkSetBlocked(blocked, ...)` | Blocks/Unblocks selected items. |
| `BulkArchive()` | Archives all selected tasks. |
| `BulkDelete()` | Permanently removes selection. |

### Subtask Management

| Method | Description |
| --- | --- |
| `AddSubtask(task, title, isCompleted?)` | Adds a subtask. |
| `RemoveSubtask(task, subtask)` | Removes a subtask. |
| `ToggleSubtask(subtask)` | Toggles subtask completion. |
| `SetAllSubtasksCompleted(task, isCompleted)` | Sets all subtasks' completion status. |
| `ClearSubtasks(task)` | Clears all subtasks from a task. |
| `GetSubtaskProgress(task)` | Returns `(completed, total)` tuple. |

### Zoom & Size

| Method | Description |
| --- | --- |
| `ZoomIn()` | Increases board size tier. |
| `ZoomOut()` | Decreases board size tier. |
| `SetZoom(DaisySize)` | Sets a specific size tier. |

### Board Operations

| Method | Description |
| --- | --- |
| `ClearBoard()` | Clears all columns and tasks. |
| `ResetBoard(columnTitles)` | Resets with specified default columns. |
| `CreateDefaultBoard()` | Creates "To Do", "In Progress", "Done" columns. |
| `GetBoardData()` | Returns the `FlowKanbanData` model. |
| `SetBoardData(data)` | Replaces the board with new data. |
| `ExportToJson()` | Exports board to JSON string. |
| `ImportFromJson(json)` | Imports board from JSON string. |
| `GetStatistics()` | Returns `BoardStatistics` with counts. |

### BoardStatistics Record

| Property | Type | Description |
| --- | --- | --- |
| `ColumnCount` | `int` | Number of columns. |
| `TotalTaskCount` | `int` | Total tasks across all columns. |
| `TasksPerColumn` | `Dictionary<string, int>` | Task count grouped by column title. |
| `EmptyColumns` | `int` | Number of columns with no tasks. |

### Example: Programmatic Board Setup

```csharp
var manager = new FlowKanbanManager(ProjectBoard);

// Create default structure
manager.CreateDefaultBoard();

// Add some tasks
var backlog = manager.FindColumnByTitle("To Do");
manager.AddTask(backlog, "Implement auth", "OAuth2 integration", DaisyPalette.Primary);
manager.AddTask(backlog, "Write tests", null, DaisyPalette.Info);

// Move a task
var inProgress = manager.FindColumnByTitle("In Progress");
var task = manager.FindTaskById("some-id");
manager.MoveTask(task, inProgress);

// Export for backup
var json = manager.ExportToJson();
```

## Accessibility & Keyboard Navigation (Plan)

### Goals

- Provide UI Automation metadata for board regions, columns, cards, swimlanes, and actions.
- Ensure Tab navigation works across the header, board, and sidebar.
- Add arrow-key navigation across columns and vertically within cards.
- Keep keyboard shortcuts consistent with the board size and creation workflows.

### Focus Zones

1. **Board Header**: title actions, tags, search input.
2. **Board Surface**: columns, cards, and the inline "Add Card" control.
3. **Sidebar**: compact action buttons (home, settings, undo/redo, zoom, save/load, metrics/help).
4. **Status Bar**: informational only (not in tab order).

### Tab Navigation

- Tab/Shift+Tab moves through focusable controls in header, board surface, and sidebar.
- Cards and "Add Card" are focusable so screen readers can announce content in context.

### Arrow-Key Navigation (Board Surface)

- **Column header focus**: Left/Right moves to adjacent column header. Down enters the first visible card, or the add-card control if empty.
- **Card focus**: Up/Down moves within the same column; Left/Right moves to the adjacent column at the nearest card index.
- **Add-card focus**: Up moves to the last visible card; Left/Right moves to the add-card control in adjacent columns.
- **Swimlane layout**: Left/Right moves across columns within the same lane row; Up/Down moves within the same column/lane list.

### Keyboard Shortcuts (Board View)

- `Ctrl + B` → Add column (append).
- `Ctrl + D` → Delete last column (confirmation rules apply).
- `Ctrl + T` → Rename active column (defaults to last column).
- `Ctrl + N` → Create card in active column (uses inline add).
- `Ctrl + Alt + Left/Right` → Switch active column (for rename/add).
- `Ctrl + +` / `Ctrl + -` → Zoom in/out.

### Accessibility Metadata

- Columns, cards, and action buttons expose `AutomationProperties.Name`.
- Cards expose `AutomationProperties.AutomationId` via task ID for stable testing hooks.
- Buttons and icon-only controls use their localized tooltip string as the UIA name.

### Platform Notes

- Keyboard focus is supported on all targets except iOS by default; enable experimental keyboard focus on iOS:
  `WinRTFeatureConfiguration.Focus.EnableExperimentalKeyboardFocus = true`.

# Kanban Critic Review

Date: 2026-01-27
Scope: Flowery.Uno.Kanban
Reviewer: codereview-roasted (Linus-style, fundamentals-first)

## Taste Rating

- Needs improvement (fundamental scalability/architecture mismatches)

## Findings

### [CRITICAL ISSUES]

- [x] 1) Non-virtualized task list at scale
  - File: Flowery.Uno.Kanban/Themes/DaisyKanban.xaml:1324-1358
  - Problem: `ItemsControl` + `StackPanel` inside a `ScrollViewer` renders every task and measure/arrange cost grows linearly with the task count. The model/sanitizer allows up to 10,000 tasks, so this will blow up UI memory and scrolling perf and can stall or crash on lower-end devices.
  - Impact: UX jank, high memory usage, and potential OOM for large boards.

- [x] 2) Event handler leaks from lambda subscriptions
  - File: Flowery.Uno.Kanban/Controls/FlowKanban.UI.cs:565-570
  - Problem: Pointer/focus handlers are attached with inline lambdas and are never removed in `OnKanbanUnloaded`. Each load adds more handlers, causing duplicate effects and leaks the control.
  - Impact: memory leak + duplicate UI behavior after navigation or reload.

- [x] 3) Column header pointer handlers never detached
  - File: Flowery.Uno.Kanban/Controls/FlowKanbanColumn.cs:147-155 and 251-255
  - Problem: PointerEntered/Exited/Pressed are added but never removed. Only focus handlers are detached in `OnColumnUnloaded`.
  - Impact: memory leak and duplicated hover behavior for columns.

- [x] 4) Statistics crash on duplicate column titles
  - File: Flowery.Uno.Kanban/Controls/FlowKanbanManager.cs:2129-2134
  - Problem: `ToDictionary(c => c.Title, ...)` throws if two columns have the same title, which is a normal user scenario.
  - Impact: runtime exception when calling `GetStatistics()`.

### [IMPROVEMENT OPPORTUNITIES]

- [x] 1) Visual tree scans in keyboard navigation
  - File: Flowery.Uno.Kanban/Controls/FlowKanban.Keyboard.cs:470-525
  - Problem: `EnumerateVisualTree` is used to find column/card/add-card controls on every directional keypress. This is O(N) per key press and multiplies with a non-virtualized UI.
  - Impact: input latency increases with board size.

- [ ] 2) Theme/localization refresh performs full tree walks
  - File: Flowery.Uno.Kanban/Controls/FlowKanban.UI.cs:524-884
  - Problem: refresh calls iterate all descendants to update card styles and lane headers.
  - Impact: noticeable stalls on large boards. Prefer bindings or cached references.

- [x] 3) Drag/drop index calc hardcodes spacing
  - File: Flowery.Uno.Kanban/Controls/FlowKanbanColumn.cs:1392-1421
  - Problem: Uses `+ 8` spacing constant; spacing is size/theme dependent (`_tasksPanel.Spacing`).
  - Impact: incorrect drop index on non-default sizes or theme adjustments.

### [TESTING GAPS]

- [x] 1) Undo/redo, archive/unarchive, and persistence/migration are untested
  - File: Flowery.Uno.RuntimeTests/Controls/Given_KanbanBehavior.cs (existing tests cover defaults and WIP)
  - Impact: regressions can ship in core workflows without any test signal.

- [x] 2) Statistics with duplicate column titles not covered
  - No tests cover `GetStatistics()` for duplicate titles
  - Impact: crash goes unnoticed.

## Plan: Next Steps to Fix Critical Architectural Issues

### Step 1: Virtualize task lists (highest priority)

- [x] Replace `ItemsControl` + `StackPanel` with a virtualized control.
- [x] Options:
  - [ ] `ItemsRepeater` + `ItemsRepeater.Layout` using `VirtualizingLayout` / recycling element factory.
  - [x] Or `ListView` with `ItemsStackPanel` virtualization enabled.
- [x] Remove the extra `ScrollViewer` wrapper to avoid double scrolling and to let the virtualizing panel control scrolling.
- [x] Ensure drag/drop visuals still work with virtualized containers (may require container lookup through `ItemsRepeater`/`ListView` APIs instead of visual tree recursion).

### Step 2: Fix event handler leaks

- [x] Replace lambda handlers with named methods so they can be removed.
- [x] In `FlowKanban.OnKanbanUnloaded`, detach all pointer/focus handlers added in `FindBoardHeaderElements`.
- [x] In `FlowKanbanColumn.OnColumnUnloaded`, detach PointerEntered/Exited/Pressed from `_columnHeaderGrid`.
- [x] Verify no duplicate subscriptions when controls are reloaded.

### Step 3: Make statistics stable under real user input

- [x] Replace `ToDictionary(c => c.Title, ...)` with:
  - [ ] Dictionary keyed by `Id`, or
  - [x] Group by `Title` and sum counts to avoid collision.
- [x] Add a runtime test for duplicate titles.

### Step 4: Contain the expensive visual tree scans

- [ ] Cache references to `FlowKanbanColumn`, `FlowTaskCard`, `FlowKanbanAddCard` containers as items are realized.
- [ ] Use container APIs from the virtualized control to map model -> UI element.
- [ ] Ensure refresh actions are incremental (refresh only the cards affected).

### Step 5: Add regression tests for critical flows

- [x] Add tests for:
  - [x] Undo/redo behavior after moves
  - [x] Archive/unarchive round-trip
  - [x] Persistence/migration round-trip with large boards
  - [x] Statistics with duplicate titles

## Notes / Assumptions

- This review assumes Kanban boards can grow to the limits defined in `FlowKanbanBoardSanitizer` (10k tasks), so UI must handle large datasets gracefully.
- The priority is to align UI architecture with data constraints; feature polishing comes after stability and performance.

## Implementation steps

Here are code-based improvement ideas for Step 4 (contain expensive visual tree scans), grounded in the current patterns in `Flowery.Uno.Kanban/Controls/FlowKanban.Keyboard.cs`, `Flowery.Uno.Kanban/Controls/FlowKanban.UI.cs`, and `Flowery.Uno.Kanban/Controls/FlowKanbanColumn.cs`.

- Replace the `EnsureVisualCache`/`EnumerateVisualTree` full scan in `Flowery.Uno.Kanban/Controls/FlowKanban.UI.cs` with incremental registration from `FlowKanbanColumn.OnColumnLoaded/OnColumnUnloaded`; keep a dictionary keyed by `(ColumnId, LaneId)` so `FindColumnControl` becomes O(1) and keyboard navigation stops forcing a tree walk.

```csharp
// Flowery.Uno.Kanban/Controls/FlowKanban.UI.cs
private readonly Dictionary<(string ColumnId, string? LaneId), FlowKanbanColumn> _columnByKey = new();

internal void RegisterColumn(FlowKanbanColumn column)
{
    if (column.ColumnData == null) return;
    var key = (column.ColumnData.Id, NormalizeLaneId(column.LaneFilterId));
    _columnByKey[key] = column;
}

internal void UnregisterColumn(FlowKanbanColumn column)
{
    if (column.ColumnData == null) return;
    var key = (column.ColumnData.Id, NormalizeLaneId(column.LaneFilterId));
    _columnByKey.Remove(key);
}
```

- Stop `TryGetTaskCard` from walking container trees by caching realized card containers in `Flowery.Uno.Kanban/Controls/FlowKanbanColumn.cs` using `ListViewBase.ContainerContentChanging`; this makes keyboard focus and theme refresh constant time and avoids `FindChild` per keypress.

```csharp
// Flowery.Uno.Kanban/Controls/FlowKanbanColumn.cs
private readonly Dictionary<FlowKanbanTaskView, FlowTaskCard> _realizedCards = new();

private void OnTaskContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
{
    if (args.Item is not FlowKanbanTaskView view)
        return;

    if (args.InRecycleQueue)
    {
        _realizedCards.Remove(view);
        return;
    }

    if (args.ItemContainer is ListViewItem lvi && lvi.ContentTemplateRoot is FlowTaskCard card)
        _realizedCards[view] = card;
}

internal FlowTaskCard? TryGetTaskCard(FlowTask task)
{
    var view = FindTaskView(task);
    return view != null && _realizedCards.TryGetValue(view, out var card) ? card : null;
}

internal IEnumerable<FlowTaskCard> GetRealizedTaskCards() => _realizedCards.Values;
```

- In `Flowery.Uno.Kanban/Controls/FlowKanban.Keyboard.cs`, switch from `FindColumnControl` + `FindTaskCard` scanning to dictionary lookups + `ScrollTaskIntoView` + `QueueFocusAction` (the caches above make the post-scroll focus O(1) once realized).

- Replace `_cachedLaneHeaderBorders`, `_cachedLaneHeaderTexts`, and `_cachedUserManagementViews` population via `EnumerateVisualTree` with `Loaded/Unloaded` registration on those elements (template parts can call back into `FlowKanban`), so theme/localization refresh doesn’t trigger a full tree walk.

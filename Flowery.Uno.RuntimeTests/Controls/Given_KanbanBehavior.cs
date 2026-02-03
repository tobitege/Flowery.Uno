using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Flowery.Uno.Kanban.Controls;
using Flowery.Enums;
using Flowery.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_KanbanBehavior
    {
        [TestMethod]
        public void When_Constructing_Kanban_Defaults_AreSet()
        {
            var kanban = new FlowKanban();

            Assert.IsNotNull(kanban.Board);
            Assert.AreEqual(DaisySize.Medium, kanban.BoardSize);
            Assert.AreEqual(string.Empty, kanban.SearchText);
            Assert.IsTrue(kanban.IsStatusBarVisible);
            Assert.IsTrue(kanban.ConfirmColumnRemovals);
            Assert.IsTrue(kanban.ConfirmCardRemovals);
            Assert.IsTrue(kanban.AutoSaveAfterEdits);
            Assert.IsTrue(kanban.AutoExpandCardDetails);
            Assert.IsFalse(kanban.EnableUndoRedo);
            Assert.AreEqual(FlowKanbanView.Board, kanban.CurrentView);
        }

        [TestMethod]
        public async Task When_SearchText_Changes_TaskMatchesUpdate()
        {
            var kanban = new FlowKanban
            {
                Board = new FlowKanbanData
                {
                    Columns =
                    {
                        new FlowKanbanColumnData
                        {
                            Title = "Backlog",
                            Tasks =
                            {
                                new FlowTask { Title = "Fix bug", Description = "urgent issue", Tags = "urgent" },
                                new FlowTask { Title = "Write docs", Description = "guides", Tags = "documentation" }
                            }
                        }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(kanban);
            await RuntimeTestHelpers.EnsureLoadedAsync(kanban);

            kanban.SearchText = "bug";

            var tasks = kanban.Board.Columns.SelectMany(column => column.Tasks).ToList();
            Assert.AreEqual(2, tasks.Count);
            Assert.IsTrue(tasks[0].IsSearchMatch);
            Assert.IsFalse(tasks[1].IsSearchMatch);
        }

        [TestMethod]
        public async Task When_Grouping_ByLane_UpdatesLaneRows()
        {
            var lane = new FlowKanbanLane { Id = "lane-1", Title = "Lane 1" };
            var kanban = new FlowKanban
            {
                Board = new FlowKanbanData
                {
                    GroupBy = FlowKanbanGroupBy.Lane,
                    Lanes = { lane },
                    Columns =
                    {
                        new FlowKanbanColumnData
                        {
                            Title = "Column",
                            Tasks =
                            {
                                new FlowTask { Title = "Assigned", LaneId = lane.Id },
                                new FlowTask { Title = "Unassigned", LaneId = null }
                            }
                        }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(kanban);
            await RuntimeTestHelpers.EnsureLoadedAsync(kanban);

            Assert.IsTrue(kanban.IsSwimlaneLayoutEnabled);
            Assert.IsFalse(kanban.IsStandardLayoutEnabled);
            Assert.AreEqual(2, kanban.LaneRows.Count);
        }

        [TestMethod]
        public void When_BoardSize_Changes_EventFires()
        {
            var kanban = new FlowKanban();
            var invocationCount = 0;
            DaisySize? lastSize = null;

            kanban.BoardSizeChanged += (_, size) =>
            {
                invocationCount++;
                lastSize = size;
            };

            kanban.BoardSize = DaisySize.Small;
            kanban.BoardSize = DaisySize.Large;

            Assert.AreEqual(2, invocationCount);
            Assert.AreEqual(DaisySize.Large, lastSize);
        }

        [TestMethod]
        public void When_WipLimit_Exceeded_MoveResultReflectsPolicy()
        {
            var kanban = new FlowKanban
            {
                Board = new FlowKanbanData
                {
                    Columns =
                    {
                        new FlowKanbanColumnData
                        {
                            Title = "Todo",
                            Tasks = { new FlowTask { Title = "Task A" } }
                        },
                        new FlowKanbanColumnData
                        {
                            Title = "Doing",
                            WipLimit = 1,
                            Tasks = { new FlowTask { Title = "Task B" } }
                        }
                    }
                }
            };

            var manager = new FlowKanbanManager(kanban, autoAttach: false);
            var source = kanban.Board.Columns[0];
            var target = kanban.Board.Columns[1];
            var task = source.Tasks[0];

            var blocked = manager.TryMoveTaskWithWipEnforcement(task, target, enforceHard: true);
            Assert.AreEqual(MoveResult.BlockedByWip, blocked);
            Assert.AreEqual(1, target.Tasks.Count);

            var warning = manager.TryMoveTaskWithWipEnforcement(task, target, enforceHard: false);
            Assert.AreEqual(MoveResult.AllowedWithWipWarning, warning);
            Assert.AreEqual(2, target.Tasks.Count);
        }

        [TestMethod]
        public void When_Configuring_Column_Defaults_RespectSettings()
        {
            var column = new FlowKanbanColumn();

            Assert.IsTrue(column.ShowColumnHeader);
            Assert.IsTrue(column.ShowTasks);
            Assert.IsTrue(column.ShowAddCard);
            Assert.IsFalse(column.IsCollapsed);
            Assert.IsTrue(column.IsDropEnabled);
            Assert.AreEqual(DaisySize.Medium, column.ColumnSize);
            Assert.IsNull(column.LaneFilterId);

            column.IsDropEnabled = false;
            Assert.IsFalse(column.AllowDrop);
        }

        [TestMethod]
        public void When_TaskCard_BindsTask_SubtaskSummaryUpdates()
        {
            var task = new FlowTask
            {
                Title = "Card",
                PlannedEndDate = DateTime.Today.AddDays(1),
                Subtasks =
                {
                    new FlowSubtask { Title = "A", IsCompleted = true },
                    new FlowSubtask { Title = "B", IsCompleted = false },
                    new FlowSubtask { Title = "C", IsCompleted = false }
                }
            };

            var card = new FlowTaskCard
            {
                Task = task
            };

            Assert.IsTrue(card.HasSubtaskSummary);
            Assert.IsFalse(string.IsNullOrWhiteSpace(card.SubtaskSummaryText));
            Assert.IsTrue(card.HasDueDate);
            Assert.IsFalse(string.IsNullOrWhiteSpace(card.DueDateText));
        }

        [TestMethod]
        public void When_Statistics_Handle_DuplicateColumnTitles()
        {
            var kanban = new FlowKanban
            {
                Board = new FlowKanbanData
                {
                    Columns =
                    {
                        new FlowKanbanColumnData
                        {
                            Title = "Todo",
                            Tasks = { new FlowTask { Title = "A" } }
                        },
                        new FlowKanbanColumnData
                        {
                            Title = "Todo",
                            Tasks = { new FlowTask { Title = "B" }, new FlowTask { Title = "C" } }
                        }
                    }
                }
            };

            var manager = new FlowKanbanManager(kanban, autoAttach: false);
            var stats = manager.GetStatistics();

            Assert.AreEqual(2, stats.ColumnCount);
            Assert.IsTrue(stats.TasksPerColumn.ContainsKey("Todo"));
            Assert.AreEqual(3, stats.TasksPerColumn["Todo"]);
        }

        [TestMethod]
        public void When_UndoRedo_MoveTask_RestoresState()
        {
            var task = new FlowTask { Title = "Task A" };
            var source = new FlowKanbanColumnData { Title = "Todo", Tasks = { task } };
            var target = new FlowKanbanColumnData { Title = "Doing" };
            var kanban = new FlowKanban
            {
                EnableUndoRedo = true,
                Board = new FlowKanbanData
                {
                    Columns = { source, target }
                }
            };

            var manager = new FlowKanbanManager(kanban, autoAttach: false);
            var result = manager.TryMoveTaskWithWipEnforcement(task, target, 0, enforceHard: false);
            Assert.AreEqual(MoveResult.Success, result);
            Assert.IsFalse(source.Tasks.Contains(task));
            Assert.IsTrue(target.Tasks.Contains(task));

            Assert.IsTrue(kanban.UndoCommand.CanExecute(null));
            kanban.UndoCommand.Execute(null);
            Assert.IsTrue(source.Tasks.Contains(task));
            Assert.IsFalse(target.Tasks.Contains(task));

            Assert.IsTrue(kanban.RedoCommand.CanExecute(null));
            kanban.RedoCommand.Execute(null);
            Assert.IsFalse(source.Tasks.Contains(task));
            Assert.IsTrue(target.Tasks.Contains(task));
        }

        [TestMethod]
        public void When_ArchiveAndUnarchive_Task_ReturnsToSource()
        {
            var task = new FlowTask { Title = "Archived Task" };
            var source = new FlowKanbanColumnData { Title = "Doing", Tasks = { task } };
            var kanban = new FlowKanban
            {
                Board = new FlowKanbanData
                {
                    Columns = { source }
                }
            };

            var manager = new FlowKanbanManager(kanban, autoAttach: false);
            manager.ArchiveTask(task);

            var archiveColumn = manager.GetArchiveColumn();
            Assert.IsNotNull(archiveColumn);
            Assert.IsTrue(task.IsArchived);
            Assert.IsTrue(archiveColumn!.Tasks.Contains(task));

            manager.UnarchiveTask(task);
            Assert.IsFalse(task.IsArchived);
            Assert.IsTrue(source.Tasks.Contains(task));
        }

        [TestMethod]
        public void When_PersistingBoard_RoundTrips()
        {
            var storage = new InMemoryStateStorage();
            var store = new FlowKanbanBoardStore(storage);
            var board = new FlowKanbanData
            {
                Id = "board-1",
                Title = "Round Trip",
                Columns =
                {
                    new FlowKanbanColumnData
                    {
                        Title = "Todo",
                        Tasks = { new FlowTask { Title = "Saved Task" } }
                    }
                }
            };

            Assert.IsTrue(store.TrySaveBoard(board, out var saveError));
            Assert.IsNull(saveError);

            Assert.IsTrue(store.TryLoadBoard(board.Id, out var loaded, out var loadError));
            Assert.IsNull(loadError);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(board.Id, loaded!.Id);
            Assert.AreEqual(board.Title, loaded.Title);
            Assert.AreEqual(1, loaded.Columns.Count);
            Assert.AreEqual("Todo", loaded.Columns[0].Title);
            Assert.AreEqual(1, loaded.Columns[0].Tasks.Count);
        }

        [TestMethod]
        public void When_Migrating_V0Json_LoadsBoard()
        {
            var board = new FlowKanbanData
            {
                Id = "legacy-board",
                Title = "Legacy",
                Columns =
                {
                    new FlowKanbanColumnData
                    {
                        Title = "Backlog",
                        Tasks = { new FlowTask { Title = "Legacy Task" } }
                    }
                }
            };

            var json = JsonSerializer.Serialize(board, FlowKanbanJsonContext.Default.FlowKanbanData);
            var root = JsonNode.Parse(json) as JsonObject;
            root?.Remove("schemaVersion");

            var migrated = FlowKanbanMigration.MigrateFromJson(root?.ToJsonString() ?? json);

            Assert.IsNotNull(migrated);
            Assert.AreEqual(board.Id, migrated!.Id);
            Assert.AreEqual(board.Title, migrated.Title);
            Assert.AreEqual(1, migrated.Columns.Count);
            Assert.AreEqual("Backlog", migrated.Columns[0].Title);
            Assert.AreEqual(1, migrated.Columns[0].Tasks.Count);
        }

        private sealed class InMemoryStateStorage : IStateStorage
        {
            private readonly Dictionary<string, List<string>> _store = new(StringComparer.Ordinal);

            public IReadOnlyList<string> LoadLines(string key)
            {
                return _store.TryGetValue(key, out var lines) ? lines : Array.Empty<string>();
            }

            public void SaveLines(string key, IEnumerable<string> lines)
            {
                _store[key] = new List<string>(lines);
            }

            public void Delete(string key)
            {
                _store.Remove(key);
            }

            public void Rename(string sourceKey, string targetKey)
            {
                if (_store.TryGetValue(sourceKey, out var lines))
                {
                    _store[targetKey] = new List<string>(lines);
                    _store.Remove(sourceKey);
                }
            }

            public IEnumerable<string> GetKeys(string prefix)
            {
                foreach (var key in _store.Keys.ToList())
                {
                    if (key.StartsWith(prefix, StringComparison.Ordinal))
                        yield return key;
                }
            }
        }
    }
}

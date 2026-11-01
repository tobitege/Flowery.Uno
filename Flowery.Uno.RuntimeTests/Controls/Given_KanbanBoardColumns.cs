using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Flowery.Uno.Kanban.Controls;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    // A new column collection reaches the board, which rebuilds its columns on the Columns change.
    [TestClass]
    public class Given_KanbanBoardColumns
    {
        [TestMethod]
        public void When_TheBoardDataGetsANewColumnCollection_ItReportsTheChange()
        {
            var board = new FlowKanbanData();
            List<string?> changes = [];
            board.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            board.Columns = [new FlowKanbanColumnData { Title = "A" }];
            CollectionAssert.AreEqual(new[] { nameof(FlowKanbanData.Columns) }, changes);

            board.Columns = null!;
            Assert.IsNotNull(board.Columns, "the board holds no column collection");
        }

        [TestMethod]
        public async Task When_TheBoardGetsANewColumnCollection_TheBoardShowsIt()
        {
            // An empty store of its own and the board view keep saved boards out of the test; a fixed width keeps the
            // standard layout, which the board picks only after a layout pass.
            var board = new FlowKanbanData { Columns = { new FlowKanbanColumnData { Title = "Todo" } } };
            var kanban = new FlowKanban
            {
                BoardStore = new FlowKanbanBoardStore(new InMemoryStateStorage()), AutoSaveAfterEdits = false, Board = board,
                Width = 1000, Height = 600, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
            };
            RuntimeTestHelpers.AttachToHost(kanban);
            await RuntimeTestHelpers.EnsureLoadedAsync(kanban);
            kanban.CurrentView = FlowKanbanView.Board;
            for (var attempt = 0; attempt < 50 && !ReferenceEquals(kanban.StandardColumnsSource, board.Columns); attempt++)
            {
                await Task.Delay(100);
            }

            Assert.AreSame(board.Columns, kanban.StandardColumnsSource, "the board shows no columns");
            var columns = new ObservableCollection<FlowKanbanColumnData> { new() { Title = "A" }, new() { Title = "B" } };
            board.Columns = columns;
            Assert.AreSame(columns, kanban.StandardColumnsSource, "the board still shows the replaced columns");
        }
    }
}

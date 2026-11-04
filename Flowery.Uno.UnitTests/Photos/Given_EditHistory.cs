using Flowery.Uno.Photos.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_EditHistory
    {
        [TestMethod]
        public void When_StateIsOriginal_NothingHasChanged()
        {
            Assert.IsFalse(EditState.Original.HasChanges);
            Assert.IsFalse(EditState.Original.HasGeometry);
            Assert.IsFalse(EditState.Original.HasColorEdits);
        }

        [TestMethod]
        public void When_EditsAreSet_TheMatchingFlagsTurnOn()
        {
            Assert.IsTrue((EditState.Original with { Rotation = 1 }).HasGeometry);
            Assert.IsTrue((EditState.Original with { Rotation = 4 }).HasGeometry == false, "four quarter turns are no turn");
            Assert.IsTrue((EditState.Original with { Exposure = 10 }).HasColorEdits);
            Assert.IsTrue((EditState.Original with { Filter = "sepia", FilterIntensity = 50 }).HasColorEdits);
            Assert.IsFalse((EditState.Original with { Filter = "sepia", FilterIntensity = 0 }).HasColorEdits);
        }

        [TestMethod]
        public void When_ApplyingUndoingAndRedoing_CurrentFollows()
        {
            var history = new EditHistory();
            var changed = 0;
            history.Changed += (_, _) => changed++;

            history.Apply(EditState.Original with { Brightness = 10 });
            history.Apply(EditState.Original with { Brightness = 20 });

            Assert.AreEqual(20, history.Current.Brightness);
            history.Undo();
            Assert.AreEqual(10, history.Current.Brightness);
            Assert.IsTrue(history.CanRedo);
            history.Redo();
            Assert.AreEqual(20, history.Current.Brightness);
            Assert.AreEqual(4, changed);
        }

        [TestMethod]
        public void When_ApplyingAfterUndo_RedoStatesAreDropped()
        {
            var history = new EditHistory();
            history.Apply(EditState.Original with { Brightness = 10 });
            history.Undo();

            history.Apply(EditState.Original with { Contrast = 5 });

            Assert.IsFalse(history.CanRedo);
            Assert.AreEqual(5, history.Current.Contrast);
        }

        [TestMethod]
        public void When_SameStateIsApplied_NothingHappens()
        {
            var history = new EditHistory();
            var changed = 0;
            history.Changed += (_, _) => changed++;

            history.Apply(EditState.Original);

            Assert.AreEqual(0, changed);
            Assert.IsFalse(history.CanUndo);
        }

        [TestMethod]
        public void When_SameKeyIsAppliedQuickly_ChangesShareOneUndoStep()
        {
            var history = new EditHistory();
            history.Apply(EditState.Original with { Brightness = 1 }, "brightness");

            history.Apply(EditState.Original with { Brightness = 2 }, "brightness");
            history.Apply(EditState.Original with { Brightness = 3 }, "brightness");

            history.Undo();
            Assert.AreEqual(0, history.Current.Brightness, "the drag counts as one step");
        }

        [TestMethod]
        public void When_Committed_TheNextChangeStartsANewStep()
        {
            var history = new EditHistory();
            history.Apply(EditState.Original with { Brightness = 1 }, "brightness");
            history.Apply(EditState.Original with { Brightness = 2 }, "brightness");

            history.Commit();
            history.Apply(EditState.Original with { Brightness = 3 }, "brightness");

            history.Undo();
            Assert.AreEqual(2, history.Current.Brightness);
        }

        [TestMethod]
        public void When_Reset_HistoryReturnsToTheOriginal()
        {
            var history = new EditHistory();
            history.Apply(EditState.Original with { Rotation = 1 });

            history.Reset();

            Assert.AreEqual(EditState.Original, history.Current);
            Assert.IsFalse(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
        }

        [TestMethod]
        public void When_FilterPresetsAreListed_EachHasAFullMatrixAndANoneEntry()
        {
            Assert.AreEqual(FilterPresets.NoneId, FilterPresets.All[0].Id);
            foreach (var preset in FilterPresets.All)
                Assert.AreEqual(20, preset.Matrix.ToArray().Length, preset.Id);
            Assert.AreSame(FilterPresets.All[0], FilterPresets.Get("unknown"));
            Assert.AreEqual("sepia", FilterPresets.Get("SEPIA").Id);
        }
    }
}

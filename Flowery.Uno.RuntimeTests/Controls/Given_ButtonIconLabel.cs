using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    /// <summary>
    /// An icon button keeps its icon when the label changes after it was loaded (a binding refresh or a localized text).
    /// </summary>
    [TestClass]
    public class Given_ButtonIconLabel
    {
        [TestMethod]
        public async Task When_LabelIsSetAgainAfterLoading_TheIconStays()
        {
            var button = new DaisyButton { Content = "Rescan", IconSymbol = Symbol.Refresh };
            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);

            button.Content = "Neu scannen";
            await NextDispatcherTurnAsync(button);

            AssertIconThenLabel(button, "Neu scannen");
        }

        [TestMethod]
        public async Task When_BoundLabelIsRebound_TheIconStaysAndShowsTheNewText()
        {
            var button = CreateBoundIconButton("Add folder");
            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);

            // The Skia desktop language fallback: clear and set the data context again.
            button.DataContext = null;
            button.DataContext = new LabelSource("Ordner hinzufügen");
            await NextDispatcherTurnAsync(button);

            AssertIconThenLabel(button, "Ordner hinzufügen");
        }

        [TestMethod]
        public async Task When_IconChangesAtRuntime_BoundLabelKeepsUpdating()
        {
            var button = CreateBoundIconButton("Add folder");
            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);

            button.IconSymbol = Symbol.Folder;
            button.DataContext = new LabelSource("Ordner hinzufügen");
            await NextDispatcherTurnAsync(button);

            AssertIconThenLabel(button, "Ordner hinzufügen");
        }

        [TestMethod]
        public async Task When_IconIsRemovedAtRuntime_BoundLabelKeepsUpdating()
        {
            var button = CreateBoundIconButton("Add folder");
            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);

            button.IconSymbol = null;
            button.DataContext = new LabelSource("Ordner hinzufügen");
            await NextDispatcherTurnAsync(button);

            Assert.AreEqual("Ordner hinzufügen", button.Content, "Without an icon the bound label should be the plain content again.");
        }

        [TestMethod]
        public async Task When_IconIsOnTheRight_TheNewLabelStaysInFrontOfIt()
        {
            var button = new DaisyButton { Content = "Next", IconSymbol = Symbol.Forward, IconPlacement = IconPlacement.Right };
            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);

            button.Content = "Weiter";
            await NextDispatcherTurnAsync(button);

            var panel = button.Content as StackPanel;
            Assert.IsNotNull(panel, "The icon layout should still be the button content.");
            Assert.AreEqual(2, panel.Children.Count);
            Assert.AreEqual("Weiter", (panel.Children[0] as ContentPresenter)?.Content);
            Assert.IsInstanceOfType<Viewbox>((panel.Children[1] as ContentPresenter)?.Content);
        }

        [TestMethod]
        public async Task When_LabelIsCleared_OnlyTheIconRemains()
        {
            var button = new DaisyButton { Content = "Delete", IconSymbol = Symbol.Delete };
            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);

            button.Content = null;
            await NextDispatcherTurnAsync(button);

            var panel = button.Content as StackPanel;
            Assert.IsNotNull(panel, "The icon layout should still be the button content.");
            Assert.AreEqual(1, panel.Children.Count);
            Assert.IsInstanceOfType<Viewbox>((panel.Children[0] as ContentPresenter)?.Content);
        }

        private static DaisyButton CreateBoundIconButton(string label)
        {
            var button = new DaisyButton { IconSymbol = Symbol.NewFolder };
            button.SetBinding(ContentControl.ContentProperty, new Binding { Path = new PropertyPath(nameof(LabelSource.Label)) });
            button.DataContext = new LabelSource(label);
            return button;
        }

        private static Task NextDispatcherTurnAsync(DaisyButton button)
        {
            // A label set directly moves next to the icon on the next dispatcher turn (WinUI forbids it inside OnContentChanged).
            var done = new TaskCompletionSource();
            if (button.DispatcherQueue?.TryEnqueue(DispatcherQueuePriority.Low, () => done.TrySetResult()) != true)
                done.TrySetResult();
            return done.Task;
        }

        private static void AssertIconThenLabel(DaisyButton button, string label)
        {
            var panel = button.Content as StackPanel;
            Assert.IsNotNull(panel, "The icon layout should still be the button content.");
            var presenters = panel.Children.OfType<ContentPresenter>().ToList();
            Assert.AreEqual(2, presenters.Count);
            Assert.IsInstanceOfType<Viewbox>(presenters[0].Content, "The icon should stay in front of the label.");
            Assert.AreEqual(label, presenters[1].Content);
        }

        public sealed class LabelSource(string label)
        {
            public string Label { get; } = label;
        }
    }
}

using Flowery.Uno.Photos.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_WindowCloseGuard
    {
        [TestMethod]
        public void When_ClosingWithoutEdits_TheWindowCloses()
        {
            Assert.AreEqual(CloseDecision.Close, new WindowCloseGuard().OnClosing(hasUnsavedEdits: false));
        }

        [TestMethod]
        public void When_ClosingWithEdits_ItAsksOnceAndClosesAfterTheAnswer()
        {
            var guard = new WindowCloseGuard();

            Assert.AreEqual(CloseDecision.CancelAndAsk, guard.OnClosing(hasUnsavedEdits: true));
            Assert.AreEqual(CloseDecision.Cancel, guard.OnClosing(hasUnsavedEdits: true), "a second click on X does not ask again");
            Assert.IsTrue(guard.OnAnswer(closeConfirmed: true));
            Assert.AreEqual(CloseDecision.Close, guard.OnClosing(hasUnsavedEdits: true), "the close after the answer goes through");
        }

        [TestMethod]
        public void When_TheUserStays_TheNextCloseAsksAgain()
        {
            var guard = new WindowCloseGuard();
            guard.OnClosing(hasUnsavedEdits: true);

            Assert.IsFalse(guard.OnAnswer(closeConfirmed: false));
            Assert.AreEqual(CloseDecision.CancelAndAsk, guard.OnClosing(hasUnsavedEdits: true));
        }
    }
}

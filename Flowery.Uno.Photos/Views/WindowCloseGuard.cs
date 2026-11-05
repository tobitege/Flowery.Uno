namespace Flowery.Uno.Photos.Views
{
    /// <summary>What a close request of the window does; see <see cref="WindowCloseGuard"/>.</summary>
    public enum CloseDecision
    {
        /// <summary>Let the window close.</summary>
        Close,

        /// <summary>Keep the window open: the question about unsaved edits is still shown.</summary>
        Cancel,

        /// <summary>Keep the window open and ask whether to save or discard the edits.</summary>
        CancelAndAsk
    }

    /// <summary>
    /// Asks about unsaved edits before the window closes. The closing event cannot wait for a dialog, so a close with
    /// edits is cancelled, the question is asked once, and the window is closed again after a save or discard.
    /// </summary>
    public sealed class WindowCloseGuard
    {
        private bool _asking;
        private bool _confirmed;

        /// <summary>Called from the closing event.</summary>
        public CloseDecision OnClosing(bool hasUnsavedEdits)
        {
            if (_confirmed)
                return CloseDecision.Close;
            if (_asking)
                return CloseDecision.Cancel;
            if (!hasUnsavedEdits)
                return CloseDecision.Close;
            _asking = true;
            return CloseDecision.CancelAndAsk;
        }

        /// <summary>Records the answer; returns true when the window should close now.</summary>
        public bool OnAnswer(bool closeConfirmed)
        {
            _asking = false;
            _confirmed = closeConfirmed;
            return closeConfirmed;
        }
    }
}

using Microsoft.UI.Dispatching;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// One-shot UI timers. A <see cref="DispatcherQueueTimer"/> that is only referenced by a local variable
    /// can be garbage collected before it fires, so pending timers are kept alive here until they tick.
    /// </summary>
    internal static class UiTimers
    {
        private static readonly HashSet<DispatcherQueueTimer> Pending = [];

        public static DispatcherQueueTimer RunOnce(DispatcherQueue dispatcher, TimeSpan delay, Action action)
        {
            var timer = dispatcher.CreateTimer();
            timer.Interval = delay;
            timer.IsRepeating = false;
            timer.Tick += (t, _) =>
            {
                t.Stop();
                Pending.Remove(t);
                action();
            };
            Pending.Add(timer);
            timer.Start();
            return timer;
        }

        public static void Cancel(DispatcherQueueTimer? timer)
        {
            if (timer == null)
                return;
            timer.Stop();
            Pending.Remove(timer);
        }
    }
}

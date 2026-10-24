using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests
{
    internal static class RuntimeTestHelpers
    {
        public static Panel GetHostPanel()
        {
            if (RuntimeTestContext.HostPanel is Panel hostPanel)
            {
                return hostPanel;
            }

            Assert.Inconclusive("Runtime test host panel is not initialized.");
            return new Grid();
        }

        public static void AttachToHost(FrameworkElement element)
        {
            var hostPanel = GetHostPanel();
            hostPanel.Children.Clear();
            hostPanel.Children.Add(element);
            element.UpdateLayout();
        }

        public static Task EnsureLoadedAsync(FrameworkElement element)
        {
            if (element.IsLoaded)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object?>();
            RoutedEventHandler? handler = null;
            handler = (_, _) =>
            {
                element.Loaded -= handler;
                tcs.TrySetResult(null);
            };
            element.Loaded += handler;
            return tcs.Task;
        }

        // CI runners can hold back low-priority dispatcher work for more than five seconds.
        private static readonly TimeSpan LayoutTimeout = TimeSpan.FromSeconds(30);

        public static async Task CompleteLayoutAsync(FrameworkElement element, params FrameworkElement[] requiredElements)
        {
            await EnsureLoadedAsync(element).WaitAsync(LayoutTimeout);
            foreach (var required in requiredElements)
                await EnsureLoadedAsync(required).WaitAsync(LayoutTimeout);
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    element.UpdateLayout();
                    completion.TrySetResult(null);
                }))
                throw new InvalidOperationException("The runtime dispatcher rejected layout verification.");
            await completion.Task.WaitAsync(LayoutTimeout);
        }
    }
}

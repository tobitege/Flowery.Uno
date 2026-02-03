using System.Threading.Tasks;
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
    }
}

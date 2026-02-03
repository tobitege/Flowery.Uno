using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    public static class FloweryDialogHost
    {
        private const string HostName = "FloweryDialogHost";
        private const int HostZIndex = 10000;

        public static Panel? EnsureHost(XamlRoot? xamlRoot)
        {
            if (xamlRoot == null)
                return null;

            var root = ResolveRootElement(xamlRoot);
            if (root == null)
                return null;

            if (FindHost(root) is Panel existingHost)
                return existingHost;

            if (TryAttachToPanel(root, out var host))
                return host;

            if (TryAttachToContentPanel(root, out host))
                return host;

            if (TryAttachToFirstPanel(root, out host))
                return host;

            return null;
        }

        private static FrameworkElement? ResolveRootElement(XamlRoot xamlRoot)
        {
            if (FlowerySizeManager.MainWindow?.Content is FrameworkElement mainRoot &&
                mainRoot.XamlRoot == xamlRoot)
            {
                return mainRoot;
            }

            if (Window.Current?.Content is FrameworkElement currentRoot &&
                currentRoot.XamlRoot == xamlRoot)
            {
                return currentRoot;
            }

            return FlowerySizeManager.MainWindow?.Content as FrameworkElement
                ?? Window.Current?.Content as FrameworkElement;
        }

        private static bool TryAttachToPanel(FrameworkElement root, out Panel? host)
        {
            if (root is Panel panel)
            {
                host = CreateHostPanel();
                panel.Children.Add(host);
                return true;
            }

            host = null;
            return false;
        }

        private static bool TryAttachToContentPanel(FrameworkElement root, out Panel? host)
        {
            if (root is ContentControl contentControl && contentControl.Content is Panel panel)
            {
                host = CreateHostPanel();
                panel.Children.Add(host);
                return true;
            }

            host = null;
            return false;
        }

        private static bool TryAttachToFirstPanel(FrameworkElement root, out Panel? host)
        {
            var panel = FindFirstPanel(root);
            if (panel == null)
            {
                host = null;
                return false;
            }

            host = CreateHostPanel();
            panel.Children.Add(host);
            return true;
        }

        private static Panel CreateHostPanel()
        {
            var host = new Grid
            {
                Name = HostName,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Canvas.SetZIndex(host, HostZIndex);
            return host;
        }

        private static Panel? FindHost(FrameworkElement root)
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current is Panel panel && string.Equals(panel.Name, HostName, StringComparison.Ordinal))
                    return panel;

                var count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child != null)
                        queue.Enqueue(child);
                }
            }

            return null;
        }

        private static Panel? FindFirstPanel(FrameworkElement root)
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current is Panel panel)
                    return panel;

                var count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child != null)
                        queue.Enqueue(child);
                }
            }

            return null;
        }
    }
}

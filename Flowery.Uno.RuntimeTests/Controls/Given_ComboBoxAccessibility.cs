using System.ComponentModel;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_ComboBoxAccessibility
    {
        [TestMethod]
        public async Task When_DisplayMemberPath_Binds_AutomationName()
        {
            var item = new NameItem { Name = "Alpha" };
            var combo = new DaisySelect
            {
                DisplayMemberPath = nameof(NameItem.Name),
                SelectedItem = item
            };

            RuntimeTestHelpers.AttachToHost(combo);
            await RuntimeTestHelpers.EnsureLoadedAsync(combo);

            var trigger = FindDescendant<Button>(combo);
            Assert.IsNotNull(trigger);
            Assert.AreEqual("Alpha", AutomationProperties.GetName(trigger));

            item.Name = "Beta";
            await EnqueueAsync(combo.DispatcherQueue);
            Assert.AreEqual("Beta", AutomationProperties.GetName(trigger));
        }

        [TestMethod]
        public async Task When_AccessibleText_Overrides_AutomationName()
        {
            var item = new NameItem { Name = "Option" };
            var combo = new DaisySelect
            {
                DisplayMemberPath = nameof(NameItem.Name),
                SelectedItem = item
            };

            RuntimeTestHelpers.AttachToHost(combo);
            await RuntimeTestHelpers.EnsureLoadedAsync(combo);

            var trigger = FindDescendant<Button>(combo);
            Assert.IsNotNull(trigger);

            combo.AccessibleText = "Override";
            await EnqueueAsync(combo.DispatcherQueue);
            Assert.AreEqual("Override", AutomationProperties.GetName(trigger));

            combo.AccessibleText = null;
            await EnqueueAsync(combo.DispatcherQueue);
            Assert.AreEqual("Option", AutomationProperties.GetName(trigger));
        }

        private static Task EnqueueAsync(DispatcherQueue dispatcher)
        {
            var tcs = new TaskCompletionSource<object?>();
            dispatcher.TryEnqueue(() => tcs.TrySetResult(null));
            return tcs.Task;
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T match)
            {
                return match;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var result = FindDescendant<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return default;
        }

        private sealed class NameItem : INotifyPropertyChanged
        {
            private string _name = string.Empty;

            public string Name
            {
                get => _name;
                set
                {
                    if (_name == value)
                    {
                        return;
                    }

                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }
}

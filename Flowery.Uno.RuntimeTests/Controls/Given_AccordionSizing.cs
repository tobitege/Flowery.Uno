using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_AccordionSizing
    {
        [TestMethod]
        public Task When_GlobalAndParentSizesChange_AuthoredItemSizesKeepPrecedence() =>
            WithSizeScopeAsync(static async host =>
            {
                var automatic = CreateItem("Automatic");
                var local = CreateItem("Local");
                local.Size = DaisySize.Medium;
                local.FontSize = 14;
                local.Padding = new Thickness(0);
                var styled = CreateItem("Styled");
                var basis = new Style(typeof(DaisyAccordionItem));
                basis.Setters.Add(new Setter(DaisyAccordionItem.SizeProperty, DaisySize.ExtraSmall));
                styled.Style = new Style(typeof(DaisyAccordionItem)) { BasedOn = basis };
                var bound = CreateItem("Bound");
                var source = new ContentControl { Tag = DaisySize.Medium };
                bound.SetBinding(DaisyAccordionItem.SizeProperty, TagBinding(source));
                var accordion = AddAccordion(host, automatic, local, styled, bound);

                foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge, DaisySize.Small })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, accordion, automatic, local, styled, bound);
                    Assert.AreEqual(DaisySize.Auto, accordion.Size);
                    Assert.AreEqual(size, accordion.ResolvedSize);
                    AssertMetrics(automatic, size);
                    AssertMetrics(local, DaisySize.Medium);
                    AssertMetrics(styled, DaisySize.ExtraSmall);
                    AssertMetrics(bound, DaisySize.Medium);
                    Assert.AreEqual(DependencyProperty.UnsetValue, accordion.ReadLocalValue(DaisyAccordion.SizeProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyAccordionItem.SizeProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, styled.ReadLocalValue(DaisyAccordionItem.SizeProperty));
                    Assert.AreEqual(DaisySize.Medium, local.Size);
                    Assert.AreEqual(14d, local.FontSize);
                    Assert.AreEqual(new Thickness(0), local.Padding);
                }

                accordion.Size = DaisySize.Large;
                FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
                await CompleteLayoutAsync(host);
                AssertMetrics(automatic, DaisySize.Large);
                AssertMetrics(local, DaisySize.Medium);
                AssertMetrics(styled, DaisySize.ExtraSmall);
                AssertMetrics(bound, DaisySize.Medium);

                source.Tag = DaisySize.ExtraLarge;
                await CompleteLayoutAsync(host);
                AssertMetrics(bound, DaisySize.ExtraLarge);
                accordion.Size = DaisySize.Small;
                source.Tag = DaisySize.Auto;
                await CompleteLayoutAsync(host);
                Assert.AreEqual(DaisySize.Auto, bound.Size);
                AssertMetrics(bound, DaisySize.Small);

                local.ClearValue(DaisyAccordionItem.SizeProperty);
                styled.ClearValue(FrameworkElement.StyleProperty);
                bound.ClearValue(DaisyAccordionItem.SizeProperty);
                await CompleteLayoutAsync(host);
                foreach (var item in new[] { automatic, local, styled, bound })
                    AssertMetrics(item, DaisySize.Small);

                accordion.ClearValue(DaisyAccordion.SizeProperty);
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                await CompleteLayoutAsync(host);
                foreach (var item in new[] { automatic, local, styled, bound })
                {
                    AssertMetrics(item, DaisySize.ExtraLarge);
                    Assert.AreEqual(DependencyProperty.UnsetValue, item.ReadLocalValue(DaisyAccordionItem.SizeProperty));
                }
            });

        [TestMethod]
        public Task When_ItemsReloadOrMove_ContentBindingsAndExpansionStayWithTheirActualParent() =>
            WithSizeScopeAsync(static async host =>
            {
                var body = new TextBox { Text = "Retained edit" };
                var first = CreateItem("First");
                first.Content = body;
                var source = new ContentControl { Tag = DaisySize.Auto };
                first.SetBinding(DaisyAccordionItem.SizeProperty, TagBinding(source));
                var second = CreateItem("Second");
                var accordion = AddAccordion(host, first, second);
                accordion.ExpandedIndex = 0;
                await CompleteLayoutAsync(host, accordion, first, second);
                var firstVisual = first.Content;
                Assert.IsTrue(first.IsExpanded);

                host.Children.Remove(accordion);
                await CompleteLayoutAsync(host);
                Assert.IsFalse(first.IsLoaded);
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                host.Children.Add(accordion);
                await CompleteLayoutAsync(host, accordion, first, second);
                AssertMetrics(first, DaisySize.ExtraLarge);
                Assert.AreSame(firstVisual, first.Content);
                Assert.AreSame(body, Body(first).Content);
                Assert.AreEqual("Retained edit", body.Text);
                Assert.IsTrue(first.IsExpanded);

                var itemPanel = (StackPanel)accordion.Content;
                itemPanel.Children.Remove(first);
                await CompleteLayoutAsync(host);
                host.Children.Add(first);
                await CompleteLayoutAsync(host, first);
                accordion.Size = DaisySize.Small;
                accordion.Variant = DaisyCollapseVariant.Plus;
                accordion.ExpandedIndex = 1;
                await CompleteLayoutAsync(host);
                AssertMetrics(first, DaisySize.ExtraLarge);
                Assert.AreEqual(DaisyCollapseVariant.Arrow, first.Variant);
                Assert.IsTrue(first.IsExpanded, "The old parent must not change a removed item's expansion.");
                first.IsExpanded = false;
                first.IsExpanded = true;
                Assert.AreEqual(1, accordion.ExpandedIndex);
                Assert.IsTrue(second.IsExpanded, "A standalone item must not collapse its old siblings.");

                source.Tag = DaisySize.ExtraSmall;
                await CompleteLayoutAsync(host);
                AssertMetrics(first, DaisySize.ExtraSmall);
                source.Tag = DaisySize.Auto;
                await CompleteLayoutAsync(host);
                AssertMetrics(first, DaisySize.ExtraLarge);

                host.Children.Remove(first);
                await CompleteLayoutAsync(host);
                itemPanel.Children.Insert(0, first);
                await CompleteLayoutAsync(host, first);
                AssertMetrics(first, DaisySize.Small);
                accordion.ExpandedIndex = 0;
                Assert.IsTrue(first.IsExpanded);
                Assert.IsFalse(second.IsExpanded);
                source.Tag = DaisySize.Large;
                await CompleteLayoutAsync(host);
                AssertMetrics(first, DaisySize.Large);
                Assert.AreSame(firstVisual, first.Content);
                Assert.AreSame(body, Body(first).Content);
                Assert.AreEqual("Retained edit", body.Text);
            });

        private static DaisyAccordionItem CreateItem(string header) => new()
        {
            Header = header,
            Content = new TextBlock { Text = "Body" },
            NeumorphicEnabled = false
        };

        private static DaisyAccordion AddAccordion(Panel host, params DaisyAccordionItem[] items)
        {
            var content = new StackPanel();
            foreach (var item in items)
                content.Children.Add(item);
            var accordion = new DaisyAccordion { Content = content, NeumorphicEnabled = false };
            host.Children.Add(accordion);
            return accordion;
        }

        private static Binding TagBinding(ContentControl source) => new()
        {
            Source = source,
            Path = new PropertyPath(nameof(ContentControl.Tag)),
            Mode = BindingMode.OneWay
        };

        private static ContentPresenter Body(DaisyAccordionItem item) =>
            (ContentPresenter)((Grid)item.Content).Children[1];

        private static void AssertMetrics(DaisyAccordionItem item, DaisySize size)
        {
            Assert.AreEqual(size, item.ResolvedSize);
            var (padding, minHeight, fontSize) = size switch
            {
                DaisySize.ExtraSmall => (8d, 32d, 10d),
                DaisySize.Small => (12d, 40d, 12d),
                DaisySize.Large => (20d, 56d, 18d),
                DaisySize.ExtraLarge => (24d, 64d, 20d),
                _ => (16d, 48d, 14d)
            };
            var header = (Border)((Grid)item.Content).Children[0];
            var headerGrid = (Grid)header.Child;
            Assert.AreEqual(new Thickness(padding), header.Padding);
            Assert.AreEqual(minHeight, header.MinHeight, 0.001);
            Assert.AreEqual(fontSize, ((TextBlock)headerGrid.Children[0]).FontSize, 0.001);
            Assert.AreEqual(14d, ((Microsoft.UI.Xaml.Shapes.Path)headerGrid.Children[1]).Width, 0.001);
            Assert.AreEqual(14d, ((Microsoft.UI.Xaml.Shapes.Path)headerGrid.Children[1]).Height, 0.001);
            Assert.AreEqual(new Thickness(padding), Body(item).Margin);
        }

        private static async Task WithSizeScopeAsync(Func<StackPanel, Task> test)
        {
            var originalSize = FlowerySizeManager.CurrentSize;
            var originalUseGlobal = FlowerySizeManager.UseGlobalSizeByDefault;
            var originalAutoSize = FlowerySizeManager.EnableGlobalAutoSize;
            var panel = RuntimeTestHelpers.GetHostPanel();
            var host = new StackPanel();
            try
            {
                panel.Children.Clear();
                FlowerySizeManager.UseGlobalSizeByDefault = true;
                FlowerySizeManager.EnableGlobalAutoSize = true;
                FlowerySizeManager.ApplySize(DaisySize.Small);
                panel.Children.Add(host);
                await CompleteLayoutAsync(host);
                await test(host);
            }
            finally
            {
                panel.Children.Clear();
                try
                {
                    await CompleteLayoutAsync(panel);
                }
                finally
                {
                    FlowerySizeManager.UseGlobalSizeByDefault = originalUseGlobal;
                    FlowerySizeManager.EnableGlobalAutoSize = originalAutoSize;
                    FlowerySizeManager.ApplySize(originalSize);
                }
            }
        }

        private static async Task CompleteLayoutAsync(FrameworkElement element, params FrameworkElement[] requiredElements)
        {
            await RuntimeTestHelpers.EnsureLoadedAsync(element).WaitAsync(TimeSpan.FromSeconds(5));
            foreach (var required in requiredElements)
                await RuntimeTestHelpers.EnsureLoadedAsync(required).WaitAsync(TimeSpan.FromSeconds(5));
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    element.UpdateLayout();
                    completion.TrySetResult(null);
                }))
                throw new InvalidOperationException("The runtime dispatcher rejected accordion layout verification.");
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}

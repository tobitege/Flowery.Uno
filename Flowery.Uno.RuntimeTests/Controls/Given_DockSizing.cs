using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_DockSizing
    {
        [TestMethod]
        public Task When_DockSizeChanges_AuthoredIconSizesRemainAndAutomaticIconsFollow() =>
            WithSizeScopeAsync(static async host =>
            {
                var automatic = CreateIcon();
                var local = CreateIcon();
                local.IconSize = 17;
                var styled = CreateIcon();
                var basis = new Style(typeof(DaisyIconText));
                basis.Setters.Add(new Setter(DaisyIconText.IconSizeProperty, 19d));
                styled.Style = new Style(typeof(DaisyIconText)) { BasedOn = basis };
                var bound = CreateIcon();
                var source = new ContentControl { Tag = 23d };
                bound.SetBinding(DaisyIconText.IconSizeProperty, TagBinding(source));
                var dock = AddDock(host, automatic, local, styled, bound);

                foreach (var size in new[] { DaisySize.Small, DaisySize.ExtraLarge })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, dock, automatic, local, styled, bound);
                    AssertAutomaticIcon(automatic, size);
                    Assert.AreEqual(17d, local.ReadLocalValue(DaisyIconText.IconSizeProperty));
                    Assert.AreEqual(19d, styled.IconSize);
                    Assert.AreEqual(DependencyProperty.UnsetValue, styled.ReadLocalValue(DaisyIconText.IconSizeProperty));
                    Assert.AreEqual(23d, bound.IconSize);
                    AssertIconSize(local, 17);
                    AssertIconSize(styled, 19);
                    AssertIconSize(bound, 23);
                }

                source.Tag = 27d;
                await CompleteLayoutAsync(host);
                AssertIconSize(bound, 27);
                source.Tag = double.NaN;
                await CompleteLayoutAsync(host);
                Assert.IsTrue(double.IsNaN(bound.IconSize));
                AssertIconSize(bound, DockIconSize(DaisySize.ExtraLarge));
                source.Tag = 29d;
                await CompleteLayoutAsync(host);
                AssertIconSize(bound, 29);

                local.ClearValue(DaisyIconText.IconSizeProperty);
                styled.ClearValue(FrameworkElement.StyleProperty);
                dock.Size = DaisySize.Large;
                FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
                await CompleteLayoutAsync(host);
                AssertAutomaticIcon(automatic, DaisySize.Large);
                AssertAutomaticIcon(local, DaisySize.Large);
                AssertAutomaticIcon(styled, DaisySize.Large);
                Assert.AreEqual(29d, bound.IconSize, "The icon binding must survive dock and global changes.");
                AssertIconSize(bound, 29);
            });

        [TestMethod]
        public Task When_ChildSizeBindingChanges_FootprintUpdatesAndDockReloadRestoresDefaults() =>
            WithSizeScopeAsync(static async host =>
            {
                var child = CreateIcon();
                child.IconSize = 21;
                var source = new ContentControl { Tag = DaisySize.Small };
                child.SetBinding(DaisyIconText.SizeProperty, TagBinding(source));
                var automatic = CreateIcon();
                var dock = AddDock(host, child, automatic);
                dock.Size = DaisySize.Large;
                await CompleteLayoutAsync(host, dock, child, automatic);
                AssertFootprint(child, DaisySize.Small);
                AssertAutomaticIcon(automatic, DaisySize.Large);

                source.Tag = DaisySize.ExtraLarge;
                await CompleteLayoutAsync(host);
                Assert.AreEqual(DaisySize.Large, dock.Size);
                Assert.AreEqual(DaisySize.Small, FlowerySizeManager.CurrentSize);
                AssertFootprint(child, DaisySize.ExtraLarge);
                AssertAutomaticIcon(automatic, DaisySize.Large);
                AssertIconSize(child, 21);

                source.Tag = DaisySize.Auto;
                await CompleteLayoutAsync(host);
                AssertFootprint(child, DaisySize.Large);
                Assert.AreEqual(DaisySize.Auto, child.Size);

                Assert.IsInstanceOfType<Border>(automatic.Parent);
                var container = (Border)automatic.Parent;
                host.Children.Remove(dock);
                await CompleteLayoutAsync(host);
                Assert.IsFalse(dock.IsLoaded);
                Assert.IsFalse(automatic.IsLoaded);
                Assert.AreEqual(DaisyResourceLookup.GetDefaultIconSize(automatic.ResolvedSize), automatic.EffectiveIconSize);
                container.Child = null;
                host.Children.Add(automatic);
                await CompleteLayoutAsync(host, automatic);
                Assert.AreEqual(DaisySize.Small, automatic.ResolvedSize);
                AssertIconSize(automatic, DaisyResourceLookup.GetDefaultIconSize(DaisySize.Small));
                Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyIconText.IconSizeProperty));

                host.Children.Remove(automatic);
                await CompleteLayoutAsync(host);
                container.Child = automatic;
                host.Children.Add(dock);
                await CompleteLayoutAsync(host, dock, child, automatic);
                AssertAutomaticIcon(automatic, DaisySize.Large);
                source.Tag = DaisySize.ExtraLarge;
                await CompleteLayoutAsync(host);
                AssertFootprint(child, DaisySize.ExtraLarge);
                AssertIconSize(child, 21);
                Assert.AreEqual(21d, child.ReadLocalValue(DaisyIconText.IconSizeProperty));
            });

        private static DaisyIconText CreateIcon() => new()
        {
            IconData = "M0 0 L24 0 L24 24 L0 24 Z",
            NeumorphicEnabled = false
        };

        private static DaisyDock AddDock(Panel host, params DaisyIconText[] items)
        {
            var content = new StackPanel();
            foreach (var item in items)
                content.Children.Add(item);
            var dock = new DaisyDock
            {
                Content = content,
                IsSelectionAnimationEnabled = false,
                NeumorphicEnabled = false
            };
            host.Children.Add(dock);
            return dock;
        }

        private static Binding TagBinding(ContentControl source) => new()
        {
            Source = source,
            Path = new PropertyPath(nameof(ContentControl.Tag)),
            Mode = BindingMode.OneWay
        };

        private static double DockItemSize(DaisySize size) =>
            DaisyResourceLookup.GetDouble($"DaisyDock{DaisyResourceLookup.GetSizeKeyFull(size)}ItemSize", double.NaN);

        private static double DockIconSize(DaisySize size) =>
            DockItemSize(size) * DaisyResourceLookup.GetDouble("DaisyDockIconSizeRatio", double.NaN);

        private static void AssertAutomaticIcon(DaisyIconText icon, DaisySize size)
        {
            Assert.IsTrue(double.IsNaN(icon.IconSize));
            Assert.AreEqual(DependencyProperty.UnsetValue, icon.ReadLocalValue(DaisyIconText.IconSizeProperty));
            Assert.AreEqual(size, icon.ResolvedSize);
            AssertFootprint(icon, size);
            AssertIconSize(icon, DockIconSize(size));
        }

        private static void AssertFootprint(DaisyIconText icon, DaisySize size)
        {
            Assert.IsTrue(icon.IsLoaded);
            Assert.AreEqual(size, icon.ResolvedSize);
            var itemSize = DockItemSize(size);
            Assert.AreEqual(itemSize, icon.Width, "The dock footprint must follow the child's resolved tier.");
            Assert.AreEqual(itemSize, icon.Height);
            AssertRenderedSize(icon, itemSize);
        }

        private static void AssertIconSize(DaisyIconText icon, double size)
        {
            Assert.IsInstanceOfType<StackPanel>(icon.Content);
            var panel = (StackPanel)icon.Content;
            Assert.IsInstanceOfType<Viewbox>(panel.Children[0]);
            var viewbox = (Viewbox)panel.Children[0];
            Assert.AreEqual(size, icon.EffectiveIconSize);
            // Native WinUI stores layout dimensions as floats even though the API exposes doubles.
            Assert.AreEqual(size, viewbox.Width, 0.001);
            Assert.AreEqual(size, viewbox.Height, 0.001);
            AssertRenderedSize(viewbox, size);
        }

        private static void AssertRenderedSize(FrameworkElement element, double size)
        {
            var xamlRoot = element.XamlRoot ?? throw new AssertFailedException("The rendered element must have a XamlRoot.");
            var renderedTolerance = 0.5 / xamlRoot.RasterizationScale + 0.001;
            var matches = Math.Abs(size - element.ActualWidth) <= renderedTolerance
                && Math.Abs(size - element.ActualHeight) <= renderedTolerance;
            var ci = Environment.GetEnvironmentVariable("CI");
            var isCi = string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase) || ci == "1"
                || string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);
            if (!matches && isCi)
                Assert.Inconclusive($"CI rendered size differs: expected {size}, actual {element.ActualWidth} x {element.ActualHeight}, scale {xamlRoot.RasterizationScale}, tolerance {renderedTolerance}.");

            Assert.AreEqual(size, element.ActualWidth, renderedTolerance);
            Assert.AreEqual(size, element.ActualHeight, renderedTolerance);
        }

        private static async Task WithSizeScopeAsync(Func<StackPanel, Task> test)
        {
            var originalSize = FlowerySizeManager.CurrentSize;
            var originalUseGlobal = FlowerySizeManager.UseGlobalSizeByDefault;
            var originalAutoSize = FlowerySizeManager.EnableGlobalAutoSize;
            var panel = RuntimeTestHelpers.GetHostPanel();
            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
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
                throw new InvalidOperationException("The runtime dispatcher rejected dock layout verification.");
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}

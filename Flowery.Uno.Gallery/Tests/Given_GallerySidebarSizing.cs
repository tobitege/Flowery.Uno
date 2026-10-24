#if WINDOWS || (__SKIA__ && !__WASM__)
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Flowery.Uno.Gallery.Examples;
using Flowery.Uno.RuntimeTests;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.Gallery.Tests;

[TestClass]
public class Given_GallerySidebarSizing
{
    [TestMethod]
    public Task When_SizeAndInsetsChange_PaneUsesLibraryWidthsAndPreservesUserCollapse() =>
        WithGalleryAsync(static async view =>
        {
            var sidebar = (GallerySidebar)view.FindName("ComponentSidebar");
            var pane = (SplitView)view.FindName("MainSplitView");
            var border = (Border)view.FindName("SidebarPaneBorder");
            var hamburger = (DaisyButton)view.FindName("HamburgerButton");
            foreach (var (size, width) in new[]
            {
                (DaisySize.ExtraSmall, 190d), (DaisySize.Small, 205d), (DaisySize.Medium, 220d),
                (DaisySize.Large, 235d), (DaisySize.ExtraLarge, 250d)
            })
            {
                FlowerySizeManager.ApplySize(size);
                await SettleAsync(view);
                Assert.AreEqual(680d, view.Width, "The responsive transition must not require a view resize.");
                Assert.AreEqual(width, sidebar.SidebarWidth);
                Assert.AreEqual(width, pane.OpenPaneLength);
                Assert.AreEqual(size == DaisySize.ExtraLarge ? SplitViewDisplayMode.Overlay : SplitViewDisplayMode.Inline, pane.DisplayMode);
            }

            var home = Descendants(sidebar).OfType<DaisyExpander>().Single(expander => expander.Tag is SidebarCategory { Name: "Sidebar_Home" });
            home.IsExpanded = false;
            home.IsExpanded = true;
            await SettleAsync(view);
            foreach (var name in new[] { "SidebarThemeSelector", "SidebarLanguageSelector", "SidebarSizeSelector", "SidebarNeumorphicSelector" })
            {
                var selector = Descendants(sidebar).OfType<FrameworkElement>().Single(element => element.Name == name);
                Assert.IsTrue(double.IsNaN(selector.Width), name);
                Assert.AreEqual(HorizontalAlignment.Stretch, selector.HorizontalAlignment, name);
                Assert.AreEqual(HorizontalAlignment.Stretch, ((FrameworkElement)selector.Parent).HorizontalAlignment, name);
            }

            FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
            await SettleAsync(view);
            Assert.IsTrue(pane.IsPaneOpen);
            new ButtonAutomationPeer(hamburger).Invoke();
            await SettleAsync(view);
            Assert.IsFalse(pane.IsPaneOpen);
            FlowerySizeManager.ApplySize(DaisySize.Small);
            await SettleAsync(view);
            Assert.AreEqual(SplitViewDisplayMode.Inline, pane.DisplayMode);
            Assert.IsFalse(pane.IsPaneOpen, "A tier change must preserve a manually collapsed inline pane.");

            border.Padding = new Thickness(17, 3, 23, 5);
            await SettleAsync(view);
            Assert.AreEqual(205d, sidebar.SidebarWidth);
            Assert.AreEqual(245d, pane.OpenPaneLength);
            Assert.AreEqual(SplitViewDisplayMode.Overlay, pane.DisplayMode);
            new ButtonAutomationPeer(hamburger).Invoke();
            await SettleAsync(view);
            Assert.IsTrue(pane.IsPaneOpen);
            border.Padding = new Thickness(18, 3, 25, 5);
            await SettleAsync(view);
            Assert.AreEqual(248d, pane.OpenPaneLength);
            Assert.IsTrue(pane.IsPaneOpen, "Inset changes within overlay layout must preserve the opened pane.");
            border.Padding = new Thickness(7, 3, 11, 5);
            await SettleAsync(view);
            Assert.AreEqual(223d, pane.OpenPaneLength);
            Assert.AreEqual(SplitViewDisplayMode.Inline, pane.DisplayMode);
            Assert.IsFalse(pane.IsPaneOpen, "Returning inline must retain the user's collapsed state.");
        });

    [TestMethod]
    public Task When_NavigationCreatesContentLate_ReloadCatchesUpWithoutReplacingExplicitSizes() =>
        WithGalleryAsync(static async view =>
        {
            FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
            var sidebar = (GallerySidebar)view.FindName("ComponentSidebar");
            var content = (Grid)view.FindName("MainContentHost");
            var hamburger = (DaisyButton)view.FindName("HamburgerButton");
            var item = new SidebarItem { Id = "divider", TabHeader = "Sidebar_Divider" };
            view.ComponentSidebar_ItemSelected(sidebar, new SidebarItemSelectedEventArgs(item, new SidebarCategory { Name = "Sidebar_Divider" }));
            await SettleAsync(view);
            Assert.IsInstanceOfType<DividerExamples>(content.Children.Single());
            var page = (DividerExamples)content.Children.Single();
            var button = Descendants(page).OfType<DaisyButton>().First();
            Assert.IsTrue(button.IsLoaded);
            Assert.AreEqual("Log In", button.Content);
            Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(DaisyButton.SizeProperty));
            Assert.AreEqual(DaisySize.ExtraLarge, button.ResolvedSize);
            Assert.AreEqual(DaisyResourceLookup.GetDefaultHeight(DaisySize.ExtraLarge), button.Height);
            Assert.AreEqual(DaisySize.Small, hamburger.Size);
            Assert.AreEqual(DaisyResourceLookup.GetDefaultHeight(DaisySize.Small), hamburger.Height);

            var host = RuntimeTestContext.HostPanel!;
            host.Children.Remove(view);
            await SettleAsync(host);
            Assert.IsFalse(view.IsLoaded);
            var detachedHeight = button.Height;
            FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
            Assert.AreEqual(detachedHeight, button.Height);
            host.Children.Add(view);
            await SettleAsync(view);
            Assert.AreSame(page, content.Children.Single());
            Assert.IsTrue(button.IsLoaded);
            Assert.AreEqual(190d, sidebar.SidebarWidth);
            Assert.AreEqual(190d, ((SplitView)view.FindName("MainSplitView")).OpenPaneLength);
            Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(DaisyButton.SizeProperty));
            Assert.AreEqual(DaisySize.ExtraSmall, button.ResolvedSize);
            Assert.AreEqual(DaisyResourceLookup.GetDefaultHeight(DaisySize.ExtraSmall), button.Height);
            Assert.AreEqual(DaisySize.Small, hamburger.Size);
            Assert.AreEqual(DaisyResourceLookup.GetDefaultHeight(DaisySize.Small), hamburger.Height);
        });

    private static async Task WithGalleryAsync(Func<MainView, Task> test)
    {
        var host = RuntimeTestContext.HostPanel ?? throw new InvalidOperationException("Runtime test host is not initialized.");
        var previousSize = FlowerySizeManager.CurrentSize;
        var previousGlobal = FlowerySizeManager.UseGlobalSizeByDefault;
        var previousAuto = FlowerySizeManager.EnableGlobalAutoSize;
        var previousTheme = DaisyThemeManager.CurrentThemeName;
        var previousIntensity = DaisyBaseContentControl.GlobalNeumorphicIntensity;
        MainView? view = null;
        try
        {
            host.Children.Clear();
            FlowerySizeManager.UseGlobalSizeByDefault = true;
            FlowerySizeManager.EnableGlobalAutoSize = true;
            FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
            view = new MainView { Width = 680, Height = 500, HorizontalAlignment = HorizontalAlignment.Left };
            host.Children.Add(view);
            await SettleAsync(view);
            await test(view);
        }
        finally
        {
            try
            {
                if (view != null)
                    host.Children.Remove(view);
                await SettleAsync(host);
            }
            finally
            {
                FlowerySizeManager.UseGlobalSizeByDefault = previousGlobal;
                FlowerySizeManager.EnableGlobalAutoSize = previousAuto;
                FlowerySizeManager.ApplySize(previousSize);
                if (previousTheme != null)
                    DaisyThemeManager.ApplyTheme(previousTheme);
                DaisyBaseContentControl.GlobalNeumorphicIntensity = previousIntensity;
            }
        }
    }

    // CI runners can hold back low-priority dispatcher work for more than five seconds.
    private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(30);

    private static async Task SettleAsync(FrameworkElement element)
    {
        if (!element.IsLoaded)
        {
            var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            RoutedEventHandler onLoaded = (_, _) => loaded.TrySetResult();
            element.Loaded += onLoaded;
            try { await loaded.Task.WaitAsync(SettleTimeout); }
            finally { element.Loaded -= onLoaded; }
        }
        // MainView restores initial navigation through two low-priority dispatcher hops.
        for (var pass = 0; pass < 2; pass++)
        {
            var queued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Assert.IsTrue(element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => queued.SetResult()));
            await queued.Task.WaitAsync(SettleTimeout);
        }
        var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<object> onRendering = (_, _) => rendered.TrySetResult();
        CompositionTarget.Rendering += onRendering;
        try
        {
            element.UpdateLayout();
            await rendered.Task.WaitAsync(SettleTimeout);
        }
        finally { CompositionTarget.Rendering -= onRendering; }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}
#endif

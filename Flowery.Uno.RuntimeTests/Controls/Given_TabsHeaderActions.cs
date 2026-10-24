using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Flowery.Controls;
using Flowery.Enums;
using Flowery.Localization;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_TabsHeaderActions
    {
        [TestMethod]
        public async Task When_HeaderButtons_Constructed_DefaultsHold()
        {
            var control = new DaisyTabsHeaderButtons();

            Assert.IsFalse(control.ShowNavigationButtons);
            Assert.IsFalse(control.ShowViewMenuButton);
            Assert.IsTrue(control.ShowManageViewsEntry);
            Assert.IsNull(control.Views);
            Assert.IsNull(control.ActiveViewId);

            await LoadAsync(control);

            Assert.AreEqual(DaisySize.Auto, control.Size);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, control.ResolvedSize);
            Assert.AreEqual(Visibility.Collapsed, Previous(control).Visibility);
            Assert.AreEqual(Visibility.Collapsed, Next(control).Visibility);
            Assert.AreEqual(Visibility.Collapsed, ViewMenuButton(control).Visibility);
            foreach (var button in FindDescendants<DaisyButton>(control))
            {
                Assert.AreEqual(DaisyButtonVariant.Default, button.Variant);
                Assert.AreEqual(DaisyButtonShape.Square, button.Shape);
                Assert.AreEqual(control.ResolvedSize, button.ResolvedSize);
            }
        }

        [TestMethod]
        public void When_ViewMenuPrepared_HasEntriesAndCheckMark()
        {
            foreach (var showManage in new[] { false, true })
            {
                var control = CreateControl();
                control.ShowManageViewsEntry = showManage;

                Assert.IsTrue(control.PrepareViewMenu());
                var items = control.ViewMenu.Items.ToList();
                Assert.HasCount(showManage ? 5 : 3, items);

                var views = items.Take(3).Cast<ToggleMenuFlyoutItem>().ToList();
                CollectionAssert.AreEqual(new[] { "Overview", "Details", "Archive" }, views.Select(item => item.Text).ToList());
                CollectionAssert.AreEqual(new[] { false, true, false }, views.Select(item => item.IsChecked).ToList());

                if (showManage)
                {
                    Assert.IsInstanceOfType<MenuFlyoutSeparator>(items[3]);
                    Assert.IsInstanceOfType<MenuFlyoutItem>(items[4]);
                }
                else
                {
                    Assert.IsFalse(items.OfType<MenuFlyoutSeparator>().Any());
                }
            }
        }

        [TestMethod]
        public void When_ViewMenuPrepared_RebuildsFromCurrentListAndProperties()
        {
            var control = CreateControl();
            Assert.IsTrue(control.PrepareViewMenu());
            var firstItem = control.ViewMenu.Items[0];

            control.Views!.RemoveAt(0);
            control.Views.Add(new DaisyTabView { Id = 4, Name = "Recent" });
            control.ActiveViewId = 4;
            control.ShowManageViewsEntry = false;

            Assert.IsTrue(control.PrepareViewMenu());
            var items = control.ViewMenu.Items.Cast<ToggleMenuFlyoutItem>().ToList();
            Assert.HasCount(3, items);
            Assert.DoesNotContain(firstItem, control.ViewMenu.Items.ToList());
            CollectionAssert.AreEqual(new[] { "Details", "Archive", "Recent" }, items.Select(item => item.Text).ToList());
            CollectionAssert.AreEqual(new[] { false, false, true }, items.Select(item => item.IsChecked).ToList());
        }

        [TestMethod]
        public async Task When_ViewsEmpty_MenuButtonDisabled_AndManageOnlyMenuHasNoSeparator()
        {
            var views = new ObservableCollection<DaisyTabView>();
            var control = new DaisyTabsHeaderButtons { ShowViewMenuButton = true, ShowManageViewsEntry = false, Views = views };
            await LoadAsync(control);

            Assert.IsFalse(ViewMenuButton(control).IsEnabled);
            views.Add(new DaisyTabView { Id = "one", Name = "One" });
            Assert.IsTrue(ViewMenuButton(control).IsEnabled);
            views.Clear();
            Assert.IsFalse(ViewMenuButton(control).IsEnabled);
            Assert.IsFalse(control.PrepareViewMenu());

            control.ShowManageViewsEntry = true;
            Assert.IsTrue(ViewMenuButton(control).IsEnabled);
            Assert.IsTrue(control.PrepareViewMenu());
            Assert.HasCount(1, control.ViewMenu.Items);
            Assert.IsInstanceOfType<MenuFlyoutItem>(control.ViewMenu.Items[0]);
        }

        [TestMethod]
        public async Task When_ViewsReplaced_ObservesOnlyCurrentCollection()
        {
            var oldViews = new ObservableCollection<DaisyTabView>();
            var replacement = new ObservableCollection<DaisyTabView>();
            var control = new DaisyTabsHeaderButtons { ShowViewMenuButton = true, ShowManageViewsEntry = false, Views = oldViews };
            await LoadAsync(control);

            control.Views = replacement;
            oldViews.Add(new DaisyTabView { Id = 1, Name = "Old" });
            Assert.IsFalse(ViewMenuButton(control).IsEnabled);
            replacement.Add(new DaisyTabView { Id = 2, Name = "New" });
            Assert.IsTrue(ViewMenuButton(control).IsEnabled);

            var plainViews = new List<DaisyTabView>();
            control.Views = plainViews;
            Assert.IsFalse(ViewMenuButton(control).IsEnabled);
            plainViews.Add(new DaisyTabView { Id = 3, Name = "Plain list" });
            control.InvalidateMeasure();
            control.UpdateLayout();
            Assert.IsTrue(ViewMenuButton(control).IsEnabled, "A plain list is rechecked after the next layout pass.");
        }

        [TestMethod]
        public async Task When_NavigationClicked_RaisesEventsAndExecutesCommands()
        {
            foreach (var useCommands in new[] { false, true })
            {
                var previousRequests = 0;
                var nextRequests = 0;
                var previousCommand = new RecordingCommand();
                var nextCommand = new RecordingCommand();
                var control = new DaisyTabsHeaderButtons { ShowNavigationButtons = true };
                control.PreviousRequested += (_, _) => previousRequests++;
                control.NextRequested += (_, _) => nextRequests++;
                if (useCommands)
                {
                    control.PreviousCommand = previousCommand;
                    control.NextCommand = nextCommand;
                }

                await LoadAsync(control);
                Click(Previous(control));
                Click(Next(control));

                Assert.AreEqual(1, previousRequests, $"Previous requests (commands: {useCommands})");
                Assert.AreEqual(1, nextRequests, $"Next requests (commands: {useCommands})");
                if (useCommands)
                {
                    Assert.HasCount(1, previousCommand.Parameters);
                    Assert.IsNull(previousCommand.Parameters[0]);
                    Assert.HasCount(1, nextCommand.Parameters);
                    Assert.IsNull(nextCommand.Parameters[0]);
                }
            }
        }

        [TestMethod]
        public async Task When_CommandsCannotExecute_ButtonsAndEntriesAreDisabled()
        {
            var blocked = new RecordingCommand { Allowed = false };
            var allowed = new RecordingCommand();
            var control = CreateControl();
            control.ShowNavigationButtons = true;
            control.PreviousCommand = blocked;
            control.NextCommand = blocked;
            control.ViewSelectedCommand = blocked;
            control.ManageViewsCommand = blocked;
            await LoadAsync(control);

            Assert.IsFalse(Previous(control).IsEnabled);
            Assert.IsFalse(Next(control).IsEnabled);
            Assert.IsTrue(control.PrepareViewMenu());
            Assert.IsTrue(control.ViewMenu.Items.OfType<MenuFlyoutItem>().All(item => !item.IsEnabled));

            blocked.Allowed = true;
            blocked.NotifyCanExecuteChanged();
            Assert.IsTrue(Previous(control).IsEnabled);
            Assert.IsTrue(control.ViewMenu.Items.OfType<MenuFlyoutItem>().All(item => item.IsEnabled));

            control.PreviousCommand = allowed;
            Click(Previous(control));
            Assert.HasCount(1, allowed.Parameters);
            Assert.IsEmpty(blocked.Parameters);
        }

        [TestMethod]
        public async Task When_ViewAndManageEntriesInvoked_RaiseEventsAndExecuteCommands()
        {
            foreach (var useCommands in new[] { false, true })
            {
                var selected = new List<DaisyTabViewEventArgs>();
                var manageRequests = 0;
                var viewCommand = new RecordingCommand();
                var manageCommand = new RecordingCommand();
                var control = CreateControl();
                control.ViewSelected += (_, args) => selected.Add(args);
                control.ManageViewsRequested += (_, _) => manageRequests++;
                if (useCommands)
                {
                    control.ViewSelectedCommand = viewCommand;
                    control.ManageViewsCommand = manageCommand;
                }

                await LoadAsync(control);
                Assert.IsTrue(control.PrepareViewMenu());
                var viewItem = (ToggleMenuFlyoutItem)control.ViewMenu.Items[2];
                var manageItem = (MenuFlyoutItem)control.ViewMenu.Items[4];

                // MenuFlyoutItem executes Command with CommandParameter itself when clicked.
                Assert.AreSame(useCommands ? viewCommand : null, viewItem.Command);
                Assert.AreEqual(3, viewItem.CommandParameter);
                Assert.AreSame(useCommands ? manageCommand : null, manageItem.Command);
                Assert.IsNull(manageItem.CommandParameter);

                control.OnViewClick(viewItem, new RoutedEventArgs());
                Assert.HasCount(1, selected);
                Assert.AreEqual(3, selected[0].Id);
                Assert.AreEqual("Archive", selected[0].Name);
                Assert.AreEqual(2, control.ActiveViewId, "The application owns the active view.");

                control.OnManageViewsClick(manageItem, new RoutedEventArgs());
                Assert.AreEqual(1, manageRequests);
            }
        }

        [TestMethod]
        public async Task When_ViewMenuButtonClicked_MenuOpens()
        {
            var control = CreateControl();
            await LoadAsync(control);
            var opened = new TaskCompletionSource();
            control.ViewMenu.Opened += (_, _) => opened.TrySetResult();

            try
            {
                Click(ViewMenuButton(control));
                var completed = await Task.WhenAny(opened.Task, Task.Delay(TimeSpan.FromSeconds(5)));
                Assert.AreSame(opened.Task, completed, "The view menu button should open the menu.");
                Assert.HasCount(5, control.ViewMenu.Items);
            }
            finally
            {
                control.ViewMenu.Hide();
            }
        }

        [TestMethod]
        public async Task When_ViewMenuOpening_LoadsViewsBeforeBuildingEntries()
        {
            var control = new DaisyTabsHeaderButtons { ShowViewMenuButton = true, ShowManageViewsEntry = false };
            await LoadAsync(control);
            var openingCount = 0;
            EventHandler<CancelEventArgs> loadViews = (_, _) =>
            {
                openingCount++;
                Assert.IsEmpty(control.ViewMenu.Items);
                control.Views = new List<DaisyTabView> { new() { Id = 42, Name = "Loaded view" } };
                control.ActiveViewId = 42;
                control.ShowManageViewsEntry = true;
            };

            control.ViewMenuOpening += loadViews;
            Assert.IsTrue(ViewMenuButton(control).IsEnabled, "A subscribed handler keeps the menu button available.");
            Assert.IsTrue(control.PrepareViewMenu());
            Assert.AreEqual(1, openingCount);
            Assert.HasCount(3, control.ViewMenu.Items);
            var item = (ToggleMenuFlyoutItem)control.ViewMenu.Items[0];
            Assert.AreEqual("Loaded view", item.Text);
            Assert.IsTrue(item.IsChecked);
            Assert.IsInstanceOfType<MenuFlyoutSeparator>(control.ViewMenu.Items[1]);
            control.ViewMenuOpening -= loadViews;
        }

        [TestMethod]
        public async Task When_ViewMenuOpeningCancelled_KeepsEntriesAndDoesNotOpen()
        {
            var control = CreateControl();
            await LoadAsync(control);
            Assert.IsTrue(control.PrepareViewMenu());
            var originalItems = control.ViewMenu.Items.ToList();

            var openingCount = 0;
            EventHandler<CancelEventArgs> cancel = (_, args) =>
            {
                openingCount++;
                control.Views = null;
                args.Cancel = true;
            };

            control.ViewMenuOpening += cancel;
            Click(ViewMenuButton(control));
            Assert.AreEqual(1, openingCount);
            Assert.IsFalse(control.ViewMenu.IsOpen);
            CollectionAssert.AreEqual(originalItems, control.ViewMenu.Items.ToList());

            control.ViewMenuOpening -= cancel;
            control.ShowManageViewsEntry = false;
            Assert.IsFalse(ViewMenuButton(control).IsEnabled);
        }

        [TestMethod]
        public async Task When_ViewMenuOpening_LeavesNoEntries_DoesNotOpenEmptyMenu()
        {
            var control = new DaisyTabsHeaderButtons { ShowViewMenuButton = true, ShowManageViewsEntry = false };
            await LoadAsync(control);
            var openingCount = 0;
            control.ViewMenuOpening += (_, _) => openingCount++;

            Click(ViewMenuButton(control));

            Assert.AreEqual(1, openingCount);
            Assert.IsFalse(control.ViewMenu.IsOpen);
            Assert.IsEmpty(control.ViewMenu.Items);
        }

        [TestMethod]
        public async Task When_SizeAndScaleChange_ButtonsAndIconsFollow_AndLabelsAreAccessible()
        {
            var control = new DaisyTabsHeaderButtons { ShowNavigationButtons = true, ShowViewMenuButton = true };
            await LoadAsync(control);

            foreach (var size in Enum.GetValues<DaisySize>())
            {
                if (size == DaisySize.Auto)
                    continue;

                control.Size = size;
                foreach (var button in FindDescendants<DaisyButton>(control))
                {
                    Assert.AreEqual(size, button.Size, $"Button size for {size}");
                }

                foreach (var icon in FindDescendants<DaisyIconText>(control))
                {
                    Assert.AreEqual(size, icon.Size, $"Icon size tier for {size}");
                    Assert.AreEqual(DaisyResourceLookup.GetDefaultIconSize(size), icon.IconSize, $"Icon size for {size}");
                }
            }

            control.Size = DaisySize.Medium;
            control.ApplyScaleFactor(0.5);
            var scaled = FloweryScaleManager.ApplyScale(DaisyResourceLookup.GetDefaultIconSize(DaisySize.Medium), 0.5);
            foreach (var icon in FindDescendants<DaisyIconText>(control))
            {
                Assert.AreEqual(scaled, icon.IconSize, 0.001);
            }

            Assert.IsFalse(string.IsNullOrEmpty(AutomationProperties.GetName(Previous(control))));
            Assert.IsFalse(string.IsNullOrEmpty(AutomationProperties.GetName(Next(control))));
            Assert.IsFalse(string.IsNullOrEmpty(AutomationProperties.GetName(ViewMenuButton(control))));
        }

        [TestMethod]
        public async Task When_CultureChanges_LabelsFollow()
        {
            var originalCulture = FloweryLocalization.CurrentCultureName;
            try
            {
                var control = CreateControl();
                await LoadAsync(control);

                FloweryLocalization.SetCulture("de");
                Assert.AreEqual("Ansichten", AutomationProperties.GetName(ViewMenuButton(control)));
                Assert.AreEqual("Vorheriges Element", AutomationProperties.GetName(Previous(control)));
                Assert.IsTrue(control.PrepareViewMenu());
                Assert.AreEqual("Bearbeitung...", ((MenuFlyoutItem)control.ViewMenu.Items[4]).Text);
            }
            finally
            {
                FloweryLocalization.SetCulture(originalCulture);
            }
        }

        [TestMethod]
        public async Task When_TrailingContentSet_SlotSitsRightOfTheTabs()
        {
            foreach (var variant in Enum.GetValues<DaisyTabVariant>())
            {
                foreach (var mode in Enum.GetValues<DaisyTabWidthMode>())
                {
                    var slot = new Border { Width = 80, Height = 24 };
                    var tabs = CreateTabs(variant, mode);
                    tabs.HeaderTrailingContent = slot;
                    await LoadAsync(tabs);

                    var trailing = FindNamed<ContentPresenter>(tabs, "PART_HeaderTrailingContent");
                    Assert.AreEqual(Visibility.Visible, trailing.Visibility, $"{variant}/{mode}");
                    Assert.IsTrue(slot.ActualWidth > 0, $"{variant}/{mode}");

                    var slotLeft = Left(slot, tabs);
                    foreach (var tabButton in TabButtons(tabs))
                    {
                        var tabRight = Left(tabButton, tabs) + tabButton.ActualWidth;
                        Assert.IsTrue(tabRight <= slotLeft + 0.5, $"Tab right {tabRight} overlaps slot at {slotLeft} ({variant}/{mode}).");
                    }
                }
            }
        }

        [TestMethod]
        public async Task When_TrailingContentCleared_HeaderLayoutIsRestored()
        {
            var tabs = CreateTabs(DaisyTabVariant.Boxed, DaisyTabWidthMode.Auto);
            await LoadAsync(tabs);

            var header = FindNamed<Border>(tabs, "PART_HeaderContainer");
            var trailing = FindNamed<ContentPresenter>(tabs, "PART_HeaderTrailingContent");
            var headerSize = new Size(header.ActualWidth, header.ActualHeight);
            var firstTabLeft = Left(TabButtons(tabs)[0], tabs);
            Assert.AreEqual(Visibility.Collapsed, trailing.Visibility);

            tabs.HeaderTrailingContent = new Border { Width = 80, Height = 24 };
            tabs.UpdateLayout();
            Assert.AreEqual(Visibility.Visible, trailing.Visibility);

            tabs.HeaderTrailingContent = null;
            tabs.UpdateLayout();
            Assert.AreEqual(Visibility.Collapsed, trailing.Visibility);
            Assert.AreEqual(new Thickness(0), trailing.Margin);
            Assert.AreEqual(headerSize, new Size(header.ActualWidth, header.ActualHeight));
            Assert.AreEqual(firstTabLeft, Left(TabButtons(tabs)[0], tabs));
        }

        [TestMethod]
        public async Task When_TrailingContentTemplateSet_PresenterUsesIt()
        {
            var tabs = CreateTabs(DaisyTabVariant.Boxed, DaisyTabWidthMode.Auto);
            tabs.HeaderTrailingContent = "Actions";
            tabs.HeaderTrailingContentTemplate = (DataTemplate)XamlReader.Load(
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                "<TextBlock Text='{Binding}' /></DataTemplate>");
            await LoadAsync(tabs);

            var trailing = FindNamed<ContentPresenter>(tabs, "PART_HeaderTrailingContent");
            Assert.AreSame(tabs.HeaderTrailingContentTemplate, trailing.ContentTemplate);
            Assert.IsTrue(FindDescendants<TextBlock>(trailing).Any(text => text.Text == "Actions"));
        }

        [TestMethod]
        public void When_ShowHeaderButtons_UsesBuiltInGroupUnlessCustomContentIsSet()
        {
            var source = new ContentControl();
            var tabs = new DaisyTabs();
            tabs.SetBinding(DaisyTabs.HeaderTrailingContentProperty, new Binding { Source = source, Path = new PropertyPath(nameof(ContentControl.Content)) });

            Assert.IsNull(tabs.HeaderButtons);
            tabs.ShowHeaderButtons = true;
            var buttons = tabs.HeaderButtons;
            Assert.IsNotNull(buttons);
            Assert.AreSame(buttons, tabs.EffectiveHeaderTrailingContent);
            Assert.IsNull(tabs.HeaderTrailingContent, "WinUI has no SetCurrentValue; the bound property keeps its value.");

            buttons.ShowNavigationButtons = true;
            tabs.ShowHeaderButtons = false;
            Assert.IsNull(tabs.EffectiveHeaderTrailingContent);
            tabs.ShowHeaderButtons = true;
            Assert.AreSame(buttons, tabs.EffectiveHeaderTrailingContent);
            Assert.IsTrue(buttons.ShowNavigationButtons);

            var custom = new TextBlock { Text = "Custom actions" };
            source.Content = custom;
            Assert.AreSame(custom, tabs.HeaderTrailingContent, "The binding still updates the property.");
            Assert.AreSame(custom, tabs.EffectiveHeaderTrailingContent);
            tabs.ShowHeaderButtons = false;
            Assert.AreSame(custom, tabs.EffectiveHeaderTrailingContent);
            tabs.ShowHeaderButtons = true;
            Assert.AreSame(custom, tabs.EffectiveHeaderTrailingContent);
            source.Content = null;
            Assert.AreSame(buttons, tabs.EffectiveHeaderTrailingContent);

            var customFirst = new DaisyTabs { HeaderTrailingContent = custom, ShowHeaderButtons = true };
            Assert.IsNull(customFirst.HeaderButtons);
            Assert.AreSame(custom, customFirst.EffectiveHeaderTrailingContent);
        }

        [TestMethod]
        public async Task When_BuiltInHeaderButtonsShown_TheySitBesideTabs_AndLayoutRestoresWhenHidden()
        {
            var tabs = CreateTabs(DaisyTabVariant.Boxed, DaisyTabWidthMode.Equal);
            await LoadAsync(tabs);
            var header = FindNamed<Border>(tabs, "PART_HeaderContainer");
            var emptySize = new Size(header.ActualWidth, header.ActualHeight);

            tabs.ShowHeaderButtons = true;
            var buttons = tabs.HeaderButtons!;
            buttons.ShowNavigationButtons = true;
            buttons.ShowViewMenuButton = true;
            await RuntimeTestHelpers.EnsureLoadedAsync(buttons);
            tabs.UpdateLayout();

            Assert.IsTrue(buttons.ActualWidth > 0);
            var slotLeft = Left(buttons, tabs);
            foreach (var tabButton in TabButtons(tabs))
            {
                Assert.IsTrue(Left(tabButton, tabs) + tabButton.ActualWidth <= slotLeft + 0.5);
            }

            tabs.ShowHeaderButtons = false;
            tabs.UpdateLayout();
            Assert.AreEqual(emptySize.Width, header.ActualWidth, 0.5);
            Assert.AreEqual(Visibility.Collapsed, FindNamed<ContentPresenter>(tabs, "PART_HeaderTrailingContent").Visibility);
        }

        private static DaisyTabsHeaderButtons CreateControl() => new()
        {
            ShowViewMenuButton = true,
            ActiveViewId = 2,
            Views = new List<DaisyTabView>
            {
                new() { Id = 1, Name = "Overview" },
                new() { Id = 2, Name = "Details" },
                new() { Id = 3, Name = "Archive" }
            }
        };

        private static DaisyTabs CreateTabs(DaisyTabVariant variant, DaisyTabWidthMode mode) => new()
        {
            Variant = variant,
            TabWidthMode = mode,
            Width = 600,
            Content = new StackPanel
            {
                Children =
                {
                    new DaisyTabItem { Header = "One", Content = new TextBlock { Text = "1" } },
                    new DaisyTabItem { Header = "Longer title", Content = new TextBlock { Text = "2" } },
                    new DaisyTabItem { Header = "Three", Content = new TextBlock { Text = "3" } }
                }
            }
        };

        private static async Task LoadAsync(FrameworkElement element)
        {
            RuntimeTestHelpers.AttachToHost(element);
            await RuntimeTestHelpers.EnsureLoadedAsync(element);
            element.UpdateLayout();
        }

        private static void Click(Button button)
        {
            new ButtonAutomationPeer(button).Invoke();
            button.UpdateLayout();
        }

        private static DaisyButton Previous(DaisyTabsHeaderButtons control) => FindNamed<DaisyButton>(control, "PART_PreviousButton");

        private static DaisyButton Next(DaisyTabsHeaderButtons control) => FindNamed<DaisyButton>(control, "PART_NextButton");

        private static DaisyButton ViewMenuButton(DaisyTabsHeaderButtons control) => FindNamed<DaisyButton>(control, "PART_ViewMenuButton");

        private static List<Button> TabButtons(DaisyTabs tabs) =>
            FindDescendants<Button>(tabs).Where(button => button.Content is TextBlock).ToList();

        private static double Left(UIElement element, UIElement relativeTo) =>
            element.TransformToVisual(relativeTo).TransformPoint(new Windows.Foundation.Point(0, 0)).X;

        private static T FindNamed<T>(DependencyObject root, string name)
            where T : FrameworkElement
        {
            var match = FindDescendants<T>(root).FirstOrDefault(element => element.Name == name);
            Assert.IsNotNull(match, $"{name} not found.");
            return match;
        }

        private static IEnumerable<T> FindDescendants<T>(DependencyObject root)
            where T : DependencyObject
        {
            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in FindDescendants<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private sealed class RecordingCommand : ICommand
        {
            public bool Allowed { get; set; } = true;

            public List<object?> Parameters { get; } = [];

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => Allowed;

            public void Execute(object? parameter) => Parameters.Add(parameter);

            public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_InteractiveControls
    {
        [TestMethod]
        public async Task When_Carousel_Loads_DefaultsAndNavigationWork()
        {
            var carousel = new DaisyCarousel
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new Border { Width = 10, Height = 10 },
                        new Border { Width = 10, Height = 10 },
                        new Border { Width = 10, Height = 10 }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(carousel);
            await RuntimeTestHelpers.EnsureLoadedAsync(carousel);

            Assert.AreEqual(3, carousel.ItemCount);
            Assert.AreEqual(0, carousel.SelectedIndex);
            Assert.IsTrue(carousel.ShowNavigation);
            Assert.IsFalse(carousel.WrapAround);
            Assert.AreEqual(Orientation.Horizontal, carousel.Orientation);
            Assert.AreEqual(FlowerySlideshowMode.Manual, carousel.Mode);
            Assert.AreEqual(3.0, carousel.SlideInterval);

            carousel.Next();
            Assert.AreEqual(1, carousel.SelectedIndex);
            carousel.Next();
            Assert.AreEqual(2, carousel.SelectedIndex);
            carousel.Next();
            Assert.AreEqual(2, carousel.SelectedIndex);
        }

        [TestMethod]
        public void When_Select_SelectionSyncs_IndexAndItem()
        {
            var select = new DaisySelect();
            select.Items.Add("A");
            select.Items.Add("B");

            select.SelectedIndex = 1;
            Assert.AreEqual("B", select.SelectedItem);

            select.SelectedItem = "A";
            Assert.AreEqual(0, select.SelectedIndex);
        }

        [TestMethod]
        public void When_Select_LocalizationSource_UpdatesItems()
        {
            var select = new DaisySelect();
            select.Items.Add(new DaisySelectItem { LocalizationKey = "KeyA", Text = "A" });
            select.Items.Add(new DaisySelectItem { LocalizationKey = "KeyB", Text = "B" });

            select.LocalizationSource = new Dictionary<string, string>
            {
                ["KeyA"] = "Alpha",
                ["KeyB"] = "Beta"
            };

            select.RefreshLocalization();

            var first = (DaisySelectItem)select.Items[0];
            var second = (DaisySelectItem)select.Items[1];

            Assert.AreEqual("Alpha", first.Text);
            Assert.AreEqual("Beta", second.Text);
        }

        [TestMethod]
        public async Task When_Select_Unloads_It_Leaves_The_LocalizationSource()
        {
            var source = new CountingLocalizationSource { Text = "Alpha" };
            var item = new DaisySelectItem { LocalizationKey = "Key", Text = "Key" };
            var select = new DaisySelect { LocalizationSource = source };
            select.Items.Add(item);
            Assert.AreEqual(0, source.Subscribers, "An unloaded select does not subscribe to the source.");

            RuntimeTestHelpers.AttachToHost(select);
            await RuntimeTestHelpers.EnsureLoadedAsync(select);
            Assert.AreEqual(1, source.Subscribers);
            Assert.AreEqual("Alpha", item.Text);

            source.Change("Beta");
            Assert.AreEqual("Beta", item.Text, "A loaded select follows the source.");

            var unloaded = new TaskCompletionSource();
            select.Unloaded += (_, _) => unloaded.TrySetResult();
            RuntimeTestHelpers.AttachToHost(new Border());
            await unloaded.Task;
            Assert.AreEqual(0, source.Subscribers, "A long-lived source must not keep the removed select alive.");

            source.Change("Gamma");
            RuntimeTestHelpers.AttachToHost(select);
            await RuntimeTestHelpers.EnsureLoadedAsync(select);
            Assert.AreEqual(1, source.Subscribers);
            Assert.AreEqual("Gamma", item.Text, "Loading again applies what changed while unloaded.");
        }

        private sealed class CountingLocalizationSource : INotifyPropertyChanged
        {
            private PropertyChangedEventHandler? _handlers;

            public event PropertyChangedEventHandler? PropertyChanged
            {
                add => _handlers += value;
                remove => _handlers -= value;
            }

            public string Text { get; set; } = string.Empty;

            public int Subscribers => _handlers?.GetInvocationList().Length ?? 0;

            public string this[string key] => Text;

            public void Change(string text)
            {
                Text = text;
                _handlers?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            }
        }

        [TestMethod]
        public async Task When_Modal_IsOpen_TogglesHitTesting()
        {
            var modal = new DaisyModal();
            RuntimeTestHelpers.AttachToHost(modal);
            await RuntimeTestHelpers.EnsureLoadedAsync(modal);

            Assert.IsFalse(modal.IsOpen);
            Assert.IsFalse(modal.IsHitTestVisible);
            Assert.AreEqual(16.0, modal.TopLeftRadius);

            modal.IsOpen = true;
            Assert.IsTrue(modal.IsHitTestVisible);

            modal.IsOpen = false;
            Assert.IsFalse(modal.IsHitTestVisible);
        }

        [TestMethod]
        public async Task When_Keyboard_Covers_Modal_Input_Moves_Above_It()
        {
            // The on-screen keyboard on Android covers the lower half of the window; the field at the bottom of the
            // dialog has to stay visible while the user types, and return once the keyboard is gone.
            var input = new TextBox();
            var filler = new Border();
            var modal = new DaisyModal
            {
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children = { filler, input }
                }
            };
            RuntimeTestHelpers.AttachToHost(modal);
            modal.IsOpen = true;
            await RuntimeTestHelpers.CompleteLayoutAsync(modal, input);

            // The dialog fits into the upper half with room to spare, whatever the size of the test window: half the
            // window, less the dialog's margins (16 each), its padding (24 each), the spacing, the field and some slack.
            var modalBounds = GetWindowBounds(modal);
            filler.Height = modalBounds.Height / 2 - 32 - 48 - 12 - input.ActualHeight - 8;
            if (filler.Height <= 0)
                Assert.Inconclusive($"The test window is too small for a dialog above the keyboard: {modalBounds}");
            await RuntimeTestHelpers.CompleteLayoutAsync(modal);
            var inputBefore = GetWindowBounds(input);
            var keyboardTop = modalBounds.Top + modalBounds.Height / 2;
            Assert.IsTrue(inputBefore.Bottom > keyboardTop, $"The field must start below the keyboard edge: {inputBefore}, {keyboardTop}");

            modal.ApplyInputPaneOcclusion(new Rect(modalBounds.Left, keyboardTop, modalBounds.Width, modalBounds.Height), Rect.Empty);
            await RuntimeTestHelpers.CompleteLayoutAsync(modal);

            var inputAbove = GetWindowBounds(input);
            Assert.IsTrue(inputAbove.Bottom <= keyboardTop, $"The field stays under the keyboard: {inputAbove}, {keyboardTop}");

            modal.ApplyInputPaneOcclusion(Rect.Empty, Rect.Empty);
            await RuntimeTestHelpers.CompleteLayoutAsync(modal);

            Assert.AreEqual(inputBefore.Top, GetWindowBounds(input).Top, 1.0);
        }

        [TestMethod]
        public async Task When_Keyboard_Shrinks_Dialog_Its_Buttons_Stay_Between_Status_Bar_And_Keyboard()
        {
            // A dialog like the Kanban editors, with its own height, a scrolling body and buttons below it, is taller
            // than the part above the keyboard. It is limited to that part, so its header stays below the status bar
            // (outside the visible bounds of the window on Android) and its buttons stay above the keyboard.
            var (dialog, header, footer) = await OpenEditorDialogAsync(isResizable: false);

            var modalBounds = GetWindowBounds(dialog);
            // Taller than the dialog's margin and padding together, so a dialog that ignores it starts above it.
            const double statusBarHeight = 80;
            var statusBarBottom = modalBounds.Top + statusBarHeight;
            var keyboardTop = modalBounds.Top + modalBounds.Height / 2;
            var visibleBounds = new Rect(modalBounds.Left, statusBarBottom, modalBounds.Width, modalBounds.Height - statusBarHeight);
            Assert.IsTrue(GetWindowBounds(footer).Bottom > keyboardTop, $"The buttons must start below the keyboard edge: {GetWindowBounds(footer)}, {keyboardTop}");

            dialog.ApplyInputPaneOcclusion(new Rect(modalBounds.Left, keyboardTop, modalBounds.Width, modalBounds.Height), visibleBounds);
            await RuntimeTestHelpers.CompleteLayoutAsync(dialog);

            var headerBounds = GetWindowBounds(header);
            var footerBounds = GetWindowBounds(footer);
            Assert.IsTrue(headerBounds.Top >= statusBarBottom, $"The dialog reaches under the status bar: {headerBounds}, {statusBarBottom}");
            Assert.IsTrue(footerBounds.Bottom <= keyboardTop, $"The buttons stay under the keyboard: {footerBounds}, {keyboardTop}");
        }

        [TestMethod]
        public async Task When_Keyboard_Shows_Dragged_Modal_Comes_Back_Above_It()
        {
            // A dialog dragged down to the bottom of the window would stay under the keyboard.
            var input = new TextBox();
            var modal = new OffsetModal { Content = input };
            RuntimeTestHelpers.AttachToHost(modal);
            modal.IsOpen = true;
            await RuntimeTestHelpers.CompleteLayoutAsync(modal, input);

            var modalBounds = GetWindowBounds(modal);
            modal.Offset = new Point(0, modalBounds.Height / 2 - GetWindowBounds(input).Height);
            await RuntimeTestHelpers.CompleteLayoutAsync(modal);
            var keyboardTop = modalBounds.Top + modalBounds.Height / 2;
            Assert.IsTrue(GetWindowBounds(input).Bottom > keyboardTop, $"The field must start below the keyboard edge: {GetWindowBounds(input)}, {keyboardTop}");

            modal.ApplyInputPaneOcclusion(new Rect(modalBounds.Left, keyboardTop, modalBounds.Width, modalBounds.Height), Rect.Empty);
            await RuntimeTestHelpers.CompleteLayoutAsync(modal);

            var inputBounds = GetWindowBounds(input);
            Assert.IsTrue(inputBounds.Bottom <= keyboardTop, $"The field stays under the keyboard: {inputBounds}, {keyboardTop}");
        }

        [TestMethod]
        public async Task When_Modal_Unloads_With_Keyboard_Shown_It_Is_Centered_Again_When_Loaded()
        {
            // A modal taken out of the tree while the keyboard is open, for example with its page, must not keep the
            // dialog squeezed above a keyboard it no longer hears about.
            var input = new TextBox();
            var modal = new DaisyModal { Content = input };
            RuntimeTestHelpers.AttachToHost(modal);
            modal.IsOpen = true;
            await RuntimeTestHelpers.CompleteLayoutAsync(modal, input);
            var centered = GetWindowBounds(input);
            var modalBounds = GetWindowBounds(modal);
            modal.ApplyInputPaneOcclusion(new Rect(modalBounds.Left, modalBounds.Top + 100, modalBounds.Width, modalBounds.Height), Rect.Empty);
            await RuntimeTestHelpers.CompleteLayoutAsync(modal);
            Assert.AreNotEqual(centered.Top, GetWindowBounds(input).Top, 1.0);

            var placeholder = new Border();
            RuntimeTestHelpers.AttachToHost(placeholder);
            await RuntimeTestHelpers.CompleteLayoutAsync(placeholder);
            RuntimeTestHelpers.AttachToHost(modal);
            await RuntimeTestHelpers.CompleteLayoutAsync(modal, input);

            Assert.AreEqual(centered.Top, GetWindowBounds(input).Top, 1.0);
        }

        [TestMethod]
        public async Task When_Reloaded_Modal_Gets_Smaller_With_Keyboard_Shown_It_Follows()
        {
            // A turned screen or a smaller window changes the size of the modal while the keyboard stays. A modal that
            // was loaded a second time has to follow that too; it stopped listening to its size after an unload.
            var input = new TextBox();
            var modal = new DaisyModal { Content = input };
            var frame = new Grid { Children = { modal } };
            RuntimeTestHelpers.AttachToHost(frame);
            modal.IsOpen = true;
            await RuntimeTestHelpers.CompleteLayoutAsync(frame, modal, input);
            var placeholder = new Border();
            RuntimeTestHelpers.AttachToHost(placeholder);
            await RuntimeTestHelpers.CompleteLayoutAsync(placeholder);
            RuntimeTestHelpers.AttachToHost(frame);
            await RuntimeTestHelpers.CompleteLayoutAsync(frame, modal, input);

            var modalBounds = GetWindowBounds(modal);
            frame.VerticalAlignment = VerticalAlignment.Top;
            frame.Height = modalBounds.Height;
            var keyboardTop = modalBounds.Top + modalBounds.Height / 2;
            modal.ApplyInputPaneOcclusion(new Rect(modalBounds.Left, keyboardTop, modalBounds.Width, modalBounds.Height), Rect.Empty);
            await RuntimeTestHelpers.CompleteLayoutAsync(frame);
            Assert.IsTrue(GetWindowBounds(input).Bottom <= keyboardTop, $"The field must start above the keyboard: {GetWindowBounds(input)}, {keyboardTop}");

            // The modal now ends above the keyboard, so the dialog is centered in all of it again. The modal works out
            // its insets on the dispatcher after the layout pass that changed its size.
            frame.Height = modalBounds.Height / 2 - 20;
            await RuntimeTestHelpers.CompleteLayoutAsync(frame);
            await RuntimeTestHelpers.CompleteLayoutAsync(frame);

            var smaller = GetWindowBounds(modal);
            var inputBounds = GetWindowBounds(input);
            Assert.AreEqual(smaller.Top + smaller.Height / 2, inputBounds.Top + inputBounds.Height / 2, 1.0);
        }

        [TestMethod]
        public async Task When_Keyboard_Shown_Resized_Dialog_Keeps_Its_Top_And_Stays_Above_It()
        {
            // Dragging the resize grip of a Kanban editor while the keyboard is open moves only the bottom edge, and the
            // dialog cannot grow under the keyboard.
            var (dialog, header, footer) = await OpenEditorDialogAsync(isResizable: true);
            var modalBounds = GetWindowBounds(dialog);
            // The dialog gets nearly the height of the window, at most MaxDialogHeight, and the keyboard leaves a part a
            // little shorter than the dialog, which therefore fills it, as on a phone. That part has to leave room above
            // the dialog's minimum height, or the resize does nothing and proves nothing.
            var dialogHeight = Math.Min(modalBounds.Height - 40, FloweryDialogBase.MaxDialogHeight);
            var areaHeight = dialogHeight - 20;
            if (areaHeight < FloweryDialogBase.AbsoluteMinHeight + 100)
                Assert.Inconclusive($"The test window is too small to resize the dialog above the keyboard: window {modalBounds}, part above the keyboard {areaHeight}, needs at least {FloweryDialogBase.AbsoluteMinHeight + 100}.");
            dialog.SetHeight(dialogHeight);
            await RuntimeTestHelpers.CompleteLayoutAsync(dialog);
            var keyboardTop = modalBounds.Top + areaHeight;
            dialog.ApplyInputPaneOcclusion(new Rect(modalBounds.Left, keyboardTop, modalBounds.Width, modalBounds.Height), Rect.Empty);
            await RuntimeTestHelpers.CompleteLayoutAsync(dialog);
            var top = GetWindowBounds(header).Top;
            var bottom = GetWindowBounds(footer).Bottom;

            dialog.BeginResize(new Point(0, 0));
            dialog.ResizeTo(new Point(0, -100));
            await RuntimeTestHelpers.CompleteLayoutAsync(dialog);
            Assert.AreEqual(bottom - 100, GetWindowBounds(footer).Bottom, 1.0, "The dialog did not get shorter.");
            Assert.AreEqual(top, GetWindowBounds(header).Top, 1.0, "Making the dialog shorter moved its top edge.");

            dialog.ResizeTo(new Point(0, 2000));
            await RuntimeTestHelpers.CompleteLayoutAsync(dialog);
            var footerBounds = GetWindowBounds(footer);
            Assert.AreEqual(top, GetWindowBounds(header).Top, 1.0, "Making the dialog taller moved its top edge.");
            Assert.IsTrue(footerBounds.Bottom <= keyboardTop, $"The buttons grew under the keyboard: {footerBounds}, {keyboardTop}");
        }

        /// <summary>
        /// Opens a dialog built like the Kanban editors: its own height, a header, a scrolling body and a button below.
        /// </summary>
        private static async Task<(EditorDialog Dialog, FrameworkElement Header, FrameworkElement Footer)> OpenEditorDialogAsync(bool isResizable)
        {
            var header = new TextBlock { Text = "Header" };
            var footer = new Button { Content = "Save" };
            var layout = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
            var body = new ScrollViewer { Content = new Border { Height = 1200 } };
            Grid.SetRow(body, 1);
            Grid.SetRow(footer, 2);
            layout.Children.Add(header);
            layout.Children.Add(body);
            layout.Children.Add(footer);
            var dialog = new EditorDialog { Content = layout, IsDialogResizingEnabled = isResizable };
            RuntimeTestHelpers.AttachToHost(dialog);
            await RuntimeTestHelpers.EnsureLoadedAsync(dialog);
            dialog.ApplySmartSizing(dialog.XamlRoot);
            dialog.IsOpen = true;
            await RuntimeTestHelpers.CompleteLayoutAsync(dialog, footer);
            return (dialog, header, footer);
        }

        private static Rect GetWindowBounds(FrameworkElement element)
        {
            return element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        }

        private sealed partial class EditorDialog : FloweryDialogBase
        {
            /// <summary>Gives the dialog a height of its own, as a resize does.</summary>
            public void SetHeight(double height)
            {
                DialogHeight = height;
                SetDialogSize(DialogWidth, height);
            }
        }

        private sealed partial class OffsetModal : DaisyModal
        {
            public Point Offset
            {
                get => DialogOffset;
                set => DialogOffset = value;
            }
        }

        [TestMethod]
        public async Task When_Popover_OpensAndCloses()
        {
            var popover = new DaisyPopover
            {
                TriggerContent = new TextBlock { Text = "Trigger" },
                PopoverContent = new TextBlock { Text = "Content" }
            };

            RuntimeTestHelpers.AttachToHost(popover);
            await RuntimeTestHelpers.EnsureLoadedAsync(popover);

            Assert.IsFalse(popover.IsOpen);
            Assert.AreEqual(DaisyPopoverPlacement.Bottom, popover.Placement);
            Assert.AreEqual(8.0, popover.VerticalOffset);
            Assert.IsTrue(popover.IsLightDismissEnabled);
            Assert.IsTrue(popover.ToggleOnClick);

            popover.IsOpen = true;
            Assert.IsTrue(popover.IsOpen);
            popover.IsOpen = false;
            Assert.IsFalse(popover.IsOpen);
        }

        [TestMethod]
        public async Task When_Tooltip_Loads_DefaultsHold()
        {
            var tooltip = new DaisyTooltip
            {
                Content = new TextBlock { Text = "Target" }
            };

            RuntimeTestHelpers.AttachToHost(tooltip);
            await RuntimeTestHelpers.EnsureLoadedAsync(tooltip);

            Assert.AreEqual(string.Empty, tooltip.Tip);
            Assert.IsFalse(tooltip.IsOpen);
            Assert.AreEqual(PlacementMode.Top, tooltip.Placement);

            tooltip.Tip = "Hello";
            tooltip.IsOpen = true;
            Assert.AreEqual("Hello", tooltip.Tip);
            var native = (ToolTip)ToolTipService.GetToolTip(tooltip);
            Assert.IsTrue(native.IsOpen);

            // The native tooltip is a separate popup; removing its owner must close it.
            var host = RuntimeTestHelpers.GetHostPanel();
            var unloaded = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            tooltip.Unloaded += (_, _) => unloaded.TrySetResult(null);
            host.Children.Remove(tooltip);
            await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsFalse(native.IsOpen, "An unloaded tooltip must not stay on screen.");
            Assert.IsTrue(tooltip.IsOpen);

            RuntimeTestHelpers.AttachToHost(tooltip);
            await RuntimeTestHelpers.EnsureLoadedAsync(tooltip).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(native.IsOpen, "A tooltip that is still requested opens again when it loads.");
            tooltip.IsOpen = false;
            Assert.IsFalse(native.IsOpen);
        }
    }
}

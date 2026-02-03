using System.Collections.Generic;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        }
    }
}

using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_StatefulControls
    {
        [TestMethod]
        public async Task When_Tabs_Load_ItemsAndSelectionSync()
        {
            var tabs = new DaisyTabs
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyTabItem { Header = "First", Content = new TextBlock { Text = "One" } },
                        new DaisyTabItem { Header = "Second", Content = new TextBlock { Text = "Two" } }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(tabs);
            await RuntimeTestHelpers.EnsureLoadedAsync(tabs);

            Assert.AreEqual(DaisyTabVariant.Boxed, tabs.Variant);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, tabs.Size);
            Assert.AreEqual(0, tabs.SelectedIndex);
            Assert.HasCount(2, tabs.Items);

            tabs.SelectedIndex = 1;
            Assert.AreEqual(1, tabs.SelectedIndex);
        }

        [TestMethod]
        public async Task When_Accordion_ExpandedIndex_UpdatesSelection()
        {
            var accordion = new DaisyAccordion
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyAccordionItem { Header = "One", Content = new TextBlock { Text = "A" } },
                        new DaisyAccordionItem { Header = "Two", Content = new TextBlock { Text = "B" } }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(accordion);
            await RuntimeTestHelpers.EnsureLoadedAsync(accordion);

            Assert.AreEqual(DaisyCollapseVariant.Arrow, accordion.Variant);
            Assert.AreEqual(-1, accordion.ExpandedIndex);

            accordion.ExpandedIndex = 1;
            Assert.AreEqual(1, accordion.ExpandedIndex);
        }

        [TestMethod]
        public async Task When_Collapse_Toggles_ExpandedState()
        {
            var collapse = new DaisyCollapse
            {
                Header = "Header",
                ExpanderContent = new TextBlock { Text = "Content" }
            };

            RuntimeTestHelpers.AttachToHost(collapse);
            await RuntimeTestHelpers.EnsureLoadedAsync(collapse);

            Assert.IsFalse(collapse.IsExpanded);
            Assert.AreEqual(DaisyCollapseVariant.Arrow, collapse.Variant);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, collapse.Size);

            collapse.IsExpanded = true;
            Assert.IsTrue(collapse.IsExpanded);
        }

        [TestMethod]
        public async Task When_Dropdown_SelectionAndOpenState_Update()
        {
            var dropdown = new DaisyDropdown
            {
                ItemsSource = new[] { "Alpha", "Beta" }
            };

            RuntimeTestHelpers.AttachToHost(dropdown);
            await RuntimeTestHelpers.EnsureLoadedAsync(dropdown);

            Assert.IsFalse(dropdown.IsOpen);
            Assert.IsTrue(dropdown.CloseOnSelection);
            Assert.AreEqual("Select", dropdown.PlaceholderText);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, dropdown.Size);
            Assert.AreEqual(DaisyColor.Default, dropdown.Variant);

            dropdown.SelectedItem = "Beta";
            Assert.AreEqual("Beta", dropdown.SelectedItem);

            dropdown.IsOpen = true;
            Assert.IsTrue(dropdown.IsOpen);
            dropdown.IsOpen = false;
            Assert.IsFalse(dropdown.IsOpen);
        }

        [TestMethod]
        public async Task When_Pagination_DefaultsAndClampingHold()
        {
            var pagination = new DaisyPagination
            {
                TotalPages = 3
            };

            RuntimeTestHelpers.AttachToHost(pagination);
            await RuntimeTestHelpers.EnsureLoadedAsync(pagination);

            Assert.AreEqual(1, pagination.CurrentPage);
            Assert.AreEqual(3, pagination.TotalPages);
            Assert.AreEqual(7, pagination.MaxVisiblePages);
            Assert.IsTrue(pagination.ShowPrevNext);
            Assert.IsFalse(pagination.ShowFirstLast);
            Assert.IsFalse(pagination.ShowJumpButtons);
            Assert.AreEqual(Orientation.Horizontal, pagination.Orientation);
            Assert.AreEqual("…", pagination.EllipsisText);

            pagination.CurrentPage = 5;
            Assert.AreEqual(3, pagination.CurrentPage);
        }

        [TestMethod]
        public async Task When_Rating_DefaultsHold_AndValueClamps()
        {
            var rating = new DaisyRating();

            RuntimeTestHelpers.AttachToHost(rating);
            await RuntimeTestHelpers.EnsureLoadedAsync(rating);

            Assert.AreEqual(FlowerySizeManager.CurrentSize, rating.Size);
            Assert.AreEqual(0.0, rating.Value);
            Assert.AreEqual(5, rating.Maximum);
            Assert.IsFalse(rating.IsReadOnly);
            Assert.AreEqual(RatingPrecision.Full, rating.Precision);
            Assert.AreEqual(DaisyColor.Warning, rating.FilledColor);

            rating.Value = 10.0;
            Assert.AreEqual(5.0, rating.Value);
        }

        [TestMethod]
        public async Task When_Range_DefaultsHold_AndValueClamps()
        {
            var range = new DaisyRange();

            RuntimeTestHelpers.AttachToHost(range);
            await RuntimeTestHelpers.EnsureLoadedAsync(range);

            Assert.AreEqual(DaisyRangeVariant.Default, range.Variant);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, range.Size);
            Assert.AreEqual(0.0, range.Value);
            Assert.AreEqual(0.0, range.Minimum);
            Assert.AreEqual(100.0, range.Maximum);
            Assert.IsTrue(range.ShowProgress);
            Assert.AreEqual(0.0, range.StepFrequency);
            Assert.AreEqual(1.0, range.SmallChange);
            Assert.AreEqual(10.0, range.LargeChange);

            range.Value = 150.0;
            Assert.AreEqual(100.0, range.Value);
        }

        [TestMethod]
        public async Task When_OtpInput_DefaultsHold()
        {
            var otp = new DaisyOtpInput();

            RuntimeTestHelpers.AttachToHost(otp);
            await RuntimeTestHelpers.EnsureLoadedAsync(otp);

            Assert.AreEqual(6, otp.Length);
            Assert.IsTrue(otp.AcceptsOnlyDigits);
            Assert.IsTrue(otp.AutoAdvance);
            Assert.IsTrue(otp.AutoSelectOnFocus);
            Assert.AreEqual(0, otp.SeparatorInterval);
            Assert.AreEqual(string.Empty, otp.SeparatorText);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, otp.Size);
            Assert.IsTrue(otp.IsJoined);
        }
    }
}

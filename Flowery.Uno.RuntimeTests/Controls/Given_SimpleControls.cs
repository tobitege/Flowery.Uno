using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Enums;
using Flowery.Helpers;
using Flowery.Localization;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_SimpleControls
    {
        [TestMethod]
        public void When_Badge_Constructed_DefaultsHold()
        {
            var badge = new DaisyBadge
            {
                Content = "New"
            };

            Assert.AreEqual(DaisyBadgeVariant.Default, badge.Variant);
            Assert.AreEqual(DaisySize.Medium, badge.Size);
            Assert.IsFalse(badge.IsOutline);
            Assert.AreEqual("New", badge.BadgeContent);
        }

        [TestMethod]
        public async Task When_Alert_Loads_DefaultsHold()
        {
            var alert = new DaisyAlert
            {
                Message = "Message"
            };

            RuntimeTestHelpers.AttachToHost(alert);
            await RuntimeTestHelpers.EnsureLoadedAsync(alert);

            Assert.AreEqual(FlowerySizeManager.CurrentSize, alert.Size);
            Assert.AreEqual(DaisyAlertVariant.Info, alert.Variant);
            Assert.IsTrue(alert.ShowIcon);
            Assert.AreEqual("Message", alert.Message);
        }

        [TestMethod]
        public void When_Divider_Constructed_DefaultsHold()
        {
            var divider = new DaisyDivider();

            Assert.IsFalse(divider.Horizontal);
            Assert.AreEqual(DaisyDividerColor.Default, divider.Color);
            Assert.AreEqual(DaisyDividerPlacement.Default, divider.Placement);
            Assert.AreEqual(DaisySize.Small, divider.Size);
            Assert.AreEqual(DaisyDividerStyle.Solid, divider.DividerStyle);
            Assert.AreEqual(DaisyDividerOrnament.Diamond, divider.Ornament);
            Assert.AreEqual(1.0, divider.Thickness);
            Assert.IsNull(divider.DividerText);
        }

        [TestMethod]
        public async Task When_Kbd_Loads_DefaultsHold()
        {
            var kbd = new DaisyKbd();

            RuntimeTestHelpers.AttachToHost(kbd);
            await RuntimeTestHelpers.EnsureLoadedAsync(kbd);

            Assert.AreEqual(DaisyKbdStyle.ThreeDimensional, kbd.KbdStyle);
            Assert.AreEqual(DaisyBadgeVariant.Default, kbd.Variant);
        }

        [TestMethod]
        public async Task When_TagPicker_Loads_DefaultsHold()
        {
            var picker = new DaisyTagPicker();

            RuntimeTestHelpers.AttachToHost(picker);
            await RuntimeTestHelpers.EnsureLoadedAsync(picker);

            Assert.AreEqual(DaisySize.Small, picker.Size);
            Assert.AreEqual("Selected Tags", picker.Title);
            Assert.IsNull(picker.Tags);
            Assert.IsNull(picker.SelectedTags);
        }

        [TestMethod]
        public async Task When_TagPicker_TogglesSelection_UpdatesSelectedList()
        {
            var tags = new List<string> { "Alpha", "Beta" };
            var selected = new List<string>();
            var picker = new DaisyTagPicker
            {
                Tags = tags,
                SelectedTags = selected
            };

            RuntimeTestHelpers.AttachToHost(picker);
            await RuntimeTestHelpers.EnsureLoadedAsync(picker);

            picker.ToggleTag("Alpha");
            Assert.HasCount(1, selected);
            Assert.AreEqual("Alpha", selected[0]);

            picker.ToggleTag("Alpha");
            Assert.IsEmpty(selected);
        }

        [TestMethod]
        public async Task When_Avatar_Loads_DefaultsHold()
        {
            var avatar = new DaisyAvatar();

            RuntimeTestHelpers.AttachToHost(avatar);
            await RuntimeTestHelpers.EnsureLoadedAsync(avatar);

            Assert.AreEqual(FlowerySizeManager.CurrentSize, avatar.Size);
            Assert.AreEqual(DaisyAvatarShape.Circle, avatar.Shape);
            Assert.AreEqual(DaisyStatus.None, avatar.Status);
            Assert.IsFalse(avatar.IsPlaceholder);
            Assert.IsFalse(avatar.HasRing);
            Assert.AreEqual(DaisyColor.Primary, avatar.RingColor);
        }

        [TestMethod]
        public async Task When_AvatarGroup_InitialsOverflow_CalculatesCount()
        {
            var group = new DaisyAvatarGroup
            {
                Initials = "AA,BB,CC",
                MaxVisible = 2
            };

            RuntimeTestHelpers.AttachToHost(group);
            await RuntimeTestHelpers.EnsureLoadedAsync(group);

            Assert.AreEqual(FlowerySizeManager.CurrentSize, group.Size);
            Assert.AreEqual(24.0, group.Overlap);
            Assert.AreEqual(2, group.OverflowCount);
        }

        [TestMethod]
        public async Task When_Toast_Loads_DefaultsHold()
        {
            var toast = new DaisyToast();

            RuntimeTestHelpers.AttachToHost(toast);
            await RuntimeTestHelpers.EnsureLoadedAsync(toast);

            Assert.AreEqual(ToastHorizontalPosition.End, toast.HorizontalPosition);
            Assert.AreEqual(ToastVerticalPosition.Bottom, toast.VerticalPosition);
            Assert.AreEqual(8.0, toast.ToastSpacing);
            Assert.AreEqual(new Thickness(16), toast.ToastOffset);
        }

        [TestMethod]
        public async Task When_Progress_Loads_DefaultsHold_AndValueClamps()
        {
            var progress = new DaisyProgress();

            RuntimeTestHelpers.AttachToHost(progress);
            await RuntimeTestHelpers.EnsureLoadedAsync(progress);

            Assert.AreEqual(DaisyProgressVariant.Default, progress.Variant);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, progress.Size);
            Assert.AreEqual(0.0, progress.Value);
            Assert.AreEqual(0.0, progress.Minimum);
            Assert.AreEqual(100.0, progress.Maximum);
            Assert.IsFalse(progress.Shimmer);

            progress.Value = 200;
            Assert.AreEqual(100.0, progress.Value);
        }

        [TestMethod]
        public async Task When_Stat_Loads_DefaultsHold()
        {
            var stat = new DaisyStat
            {
                Title = "Title",
                Value = "123"
            };

            RuntimeTestHelpers.AttachToHost(stat);
            await RuntimeTestHelpers.EnsureLoadedAsync(stat);

            Assert.AreEqual(DaisyStatVariant.Default, stat.Variant);
            Assert.AreEqual(DaisyStatVariant.Default, stat.DescriptionVariant);
            Assert.IsFalse(stat.IsCentered);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, stat.Size);
            Assert.AreEqual("Title", stat.Title);
            Assert.AreEqual("123", stat.Value);
        }

        [TestMethod]
        public async Task When_Stats_Loads_DefaultsHold()
        {
            var stats = new DaisyStats
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyStat { Title = "One", Value = "1" },
                        new DaisyStat { Title = "Two", Value = "2" }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(stats);
            await RuntimeTestHelpers.EnsureLoadedAsync(stats);

            Assert.AreEqual(Orientation.Horizontal, stats.Orientation);
        }

        [TestMethod]
        public async Task When_StatusIndicator_Loads_DefaultsHold()
        {
            var indicator = new DaisyStatusIndicator();

            RuntimeTestHelpers.AttachToHost(indicator);
            await RuntimeTestHelpers.EnsureLoadedAsync(indicator);

            Assert.AreEqual(DaisyStatusIndicatorVariant.Default, indicator.Variant);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, indicator.Size);
            Assert.AreEqual(DaisyColor.Neutral, indicator.Color);
            Assert.AreEqual(100d, indicator.BatteryChargePercent);
            Assert.AreEqual(DaisyTrafficLightState.Green, indicator.TrafficLightActive);
            Assert.AreEqual(3, indicator.SignalStrength);
        }

        [TestMethod]
        public async Task When_Breadcrumbs_Load_DefaultsHold()
        {
            var breadcrumbs = new DaisyBreadcrumbs
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyBreadcrumbItem { Content = "Home" },
                        new DaisyBreadcrumbItem { Content = "Docs" }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(breadcrumbs);
            await RuntimeTestHelpers.EnsureLoadedAsync(breadcrumbs);

            Assert.AreEqual("/", breadcrumbs.Separator);
            Assert.AreEqual(0.5, breadcrumbs.SeparatorOpacity);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, breadcrumbs.Size);
        }

        [TestMethod]
        public async Task When_Card_Loads_DefaultsHold()
        {
            var card = new DaisyCard();

            RuntimeTestHelpers.AttachToHost(card);
            await RuntimeTestHelpers.EnsureLoadedAsync(card);

            Assert.AreEqual(DaisyCardVariant.Normal, card.Variant);
            Assert.AreEqual(DaisyCardStyle.Default, card.CardStyle);
            Assert.AreEqual(DaisyColor.Default, card.ColorVariant);
            Assert.IsFalse(card.IsGlass);
            Assert.AreEqual(14.0, card.BodyFontSize);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, card.Size);
        }

        [TestMethod]
        public async Task When_ChatBubble_Loads_DefaultsHold()
        {
            var bubble = new DaisyChatBubble
            {
                Content = new TextBlock { Text = "Hello" }
            };

            RuntimeTestHelpers.AttachToHost(bubble);
            await RuntimeTestHelpers.EnsureLoadedAsync(bubble);

            Assert.IsFalse(bubble.IsEnd);
            Assert.AreEqual(DaisyChatBubbleVariant.Default, bubble.Variant);
            Assert.AreEqual(HorizontalAlignment.Left, bubble.HorizontalAlignment);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, bubble.Size);
        }

        [TestMethod]
        public async Task When_CheckBox_Loads_DefaultsHold()
        {
            var checkBox = new DaisyCheckBox
            {
                Content = "Remember me"
            };

            RuntimeTestHelpers.AttachToHost(checkBox);
            await RuntimeTestHelpers.EnsureLoadedAsync(checkBox);

            Assert.AreEqual(DaisyCheckBoxVariant.Default, checkBox.Variant);
            Assert.AreEqual(DaisySize.Medium, checkBox.Size);
            Assert.AreEqual(HorizontalAlignment.Left, checkBox.HorizontalAlignment);
        }

        [TestMethod]
        public async Task When_Radio_Loads_DefaultsHold()
        {
            var radio = new DaisyRadio
            {
                Content = "Option"
            };

            RuntimeTestHelpers.AttachToHost(radio);
            await RuntimeTestHelpers.EnsureLoadedAsync(radio);

            Assert.AreEqual(DaisyRadioVariant.Default, radio.Variant);
            Assert.AreEqual(DaisySize.Medium, radio.Size);
        }

        [TestMethod]
        public async Task When_Toggle_Loads_DefaultsHold()
        {
            var toggle = new DaisyToggle();

            RuntimeTestHelpers.AttachToHost(toggle);
            await RuntimeTestHelpers.EnsureLoadedAsync(toggle);

            Assert.IsFalse(toggle.IsOn);
            Assert.AreEqual(DaisyToggleVariant.Default, toggle.Variant);
            Assert.AreEqual(DaisySize.Medium, toggle.Size);
            Assert.AreEqual(2.0, toggle.TogglePadding);
        }

        [TestMethod]
        public async Task When_Indicator_Loads_DefaultsHold()
        {
            var indicator = new DaisyIndicator
            {
                Content = new Border { Width = 12, Height = 12 }
            };

            RuntimeTestHelpers.AttachToHost(indicator);
            await RuntimeTestHelpers.EnsureLoadedAsync(indicator);

            Assert.IsNull(indicator.Marker);
            Assert.AreEqual(BadgePosition.TopRight, indicator.MarkerPosition);
            Assert.AreEqual(BadgeAlignment.Inside, indicator.MarkerAlignment);
        }

        [TestMethod]
        public async Task When_Steps_Load_DefaultsHold()
        {
            var steps = new DaisySteps
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyStepItem { Content = "Start" },
                        new DaisyStepItem { Content = "Finish" }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(steps);
            await RuntimeTestHelpers.EnsureLoadedAsync(steps);

            Assert.AreEqual(Orientation.Horizontal, steps.Orientation);
            Assert.AreEqual(-1, steps.SelectedIndex);
            Assert.AreEqual(2, steps.ItemCount);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, steps.Size);
        }

        [TestMethod]
        public async Task When_Timeline_Loads_DefaultsHold()
        {
            var timeline = new DaisyTimeline
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyTimelineItem { EndContent = "Created" },
                        new DaisyTimelineItem { EndContent = "Shipped" }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(timeline);
            await RuntimeTestHelpers.EnsureLoadedAsync(timeline);

            Assert.AreEqual(Orientation.Vertical, timeline.Orientation);
            Assert.IsFalse(timeline.IsCompact);
            Assert.IsFalse(timeline.SnapIcon);
        }

        [TestMethod]
        public async Task When_RadialProgress_Loads_DefaultsHold()
        {
            var radial = new DaisyRadialProgress();

            RuntimeTestHelpers.AttachToHost(radial);
            await RuntimeTestHelpers.EnsureLoadedAsync(radial);

            Assert.AreEqual(DaisyProgressVariant.Default, radial.Variant);
            Assert.AreEqual(0.0, radial.Value);
            Assert.AreEqual(0.0, radial.Minimum);
            Assert.AreEqual(100.0, radial.Maximum);
            Assert.IsTrue(radial.ShowValue);
            Assert.IsFalse(radial.Shimmer);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, radial.Size);
        }

        [TestMethod]
        public void When_BreadcrumbItem_Constructed_DefaultsHold()
        {
            var item = new DaisyBreadcrumbItem
            {
                Content = "Home"
            };

            Assert.AreEqual("/", item.Separator);
            Assert.IsNull(item.Icon);
            Assert.IsTrue(item.IsClickable);
        }

        [TestMethod]
        public async Task When_Breadcrumbs_Assigns_Item_Separator()
        {
            var home = new DaisyBreadcrumbItem { Content = "Home" };
            var docs = new DaisyBreadcrumbItem { Content = "Docs" };
            var breadcrumbs = new DaisyBreadcrumbs
            {
                Separator = ">",
                Content = new StackPanel
                {
                    Children =
                    {
                        home,
                        docs
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(breadcrumbs);
            await RuntimeTestHelpers.EnsureLoadedAsync(breadcrumbs);

            Assert.AreEqual(">", home.Separator);
            Assert.AreEqual(">", docs.Separator);
            Assert.IsTrue(home.IsClickable);
            Assert.IsTrue(docs.IsClickable);
        }

        [TestMethod]
        public void When_Clock_Constructed_DefaultsHold()
        {
            var clock = new DaisyClock();

            Assert.AreEqual(ClockMode.Clock, clock.Mode);
            Assert.AreEqual(Orientation.Horizontal, clock.Orientation);
            Assert.IsTrue(clock.ShowSeconds);
            Assert.IsFalse(clock.ShowMilliseconds);
            Assert.AreEqual(ClockLabelFormat.None, clock.LabelFormat);
            Assert.AreEqual(":", clock.Separator);
            Assert.AreEqual(ClockFormat.TwentyFourHour, clock.Format);
            Assert.IsFalse(clock.ShowAmPm);
            Assert.AreEqual(DaisySize.Medium, clock.Size);
            Assert.AreEqual(6.0, clock.Spacing);
            Assert.IsFalse(clock.UseJoin);
            Assert.AreEqual(ClockStyle.Segmented, clock.DisplayStyle);
            Assert.AreEqual(TimeSpan.FromMinutes(5), clock.TimerDuration);
            Assert.AreEqual(TimeSpan.Zero, clock.TimerRemaining);
            Assert.AreEqual(TimeSpan.Zero, clock.StopwatchElapsed);
            Assert.IsFalse(clock.IsRunning);
        }

        [TestMethod]
        public async Task When_Countdown_Loads_DefaultsHold()
        {
            var countdown = new DaisyCountdown();

            RuntimeTestHelpers.AttachToHost(countdown);
            await RuntimeTestHelpers.EnsureLoadedAsync(countdown);

            Assert.AreEqual(0, countdown.Value);
            Assert.AreEqual(2, countdown.Digits);
            Assert.AreEqual(CountdownClockUnit.None, countdown.ClockUnit);
            Assert.AreEqual(CountdownMode.Display, countdown.Mode);
            Assert.AreEqual(TimeSpan.FromMinutes(5), countdown.Duration);
            Assert.AreEqual(TimeSpan.Zero, countdown.Remaining);
            Assert.AreEqual(TimeSpan.Zero, countdown.Elapsed);
            Assert.IsFalse(countdown.IsRunning);
            Assert.IsFalse(countdown.IsCountingDown);
            Assert.AreEqual(TimeSpan.FromSeconds(1), countdown.Interval);
            Assert.IsFalse(countdown.Loop);
            Assert.AreEqual(59, countdown.LoopFrom);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, countdown.Size);
        }

        [TestMethod]
        public void When_CopyButton_Constructed_DefaultsHold()
        {
            var button = new DaisyCopyButton();
            var expectedCopy = FloweryLocalization.GetStringInternal("CopyButton_Copy", "Copy");
            var expectedSuccess = FloweryLocalization.GetStringInternal("CopyButton_Copied", "Copied");

            Assert.AreEqual(expectedCopy, button.Content);
            Assert.IsNull(button.CopyText);
            Assert.AreEqual(TimeSpan.FromSeconds(2), button.SuccessDuration);
            Assert.AreEqual(expectedSuccess, button.SuccessContent);
        }

        [TestMethod]
        public async Task When_NumberFlow_Loads_DefaultsHold()
        {
            var numberFlow = new DaisyNumberFlow();

            RuntimeTestHelpers.AttachToHost(numberFlow);
            await RuntimeTestHelpers.EnsureLoadedAsync(numberFlow);

            Assert.AreEqual(0m, numberFlow.Value);
            Assert.AreEqual("N0", numberFlow.FormatString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), numberFlow.Duration);
            Assert.IsNull(numberFlow.Prefix);
            Assert.IsNull(numberFlow.Suffix);
            Assert.AreEqual(CultureInfo.InvariantCulture, numberFlow.Culture);
            Assert.IsFalse(numberFlow.ShowDigitBoxes);
            Assert.IsFalse(numberFlow.ShowControls);
            Assert.IsNull(numberFlow.Minimum);
            Assert.IsNull(numberFlow.Maximum);
            Assert.AreEqual(1m, numberFlow.Step);
            Assert.IsFalse(numberFlow.WrapAround);
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), numberFlow.RepeatInterval);
            Assert.AreEqual(TimeSpan.FromMilliseconds(400), numberFlow.RepeatDelay);
            Assert.AreEqual(TimeSpan.FromMilliseconds(320), numberFlow.RepeatRampInitialInterval);
            Assert.AreEqual(3, numberFlow.RepeatRampSteps);
            Assert.IsFalse(numberFlow.AllowDigitSelection);
            Assert.IsNull(numberFlow.SelectedDigitIndex);
            Assert.IsTrue(numberFlow.CanIncrementValue);
            Assert.IsTrue(numberFlow.CanDecrementValue);
        }

        [TestMethod]
        public async Task When_Table_Loads_DefaultsHold()
        {
            var table = new DaisyTable
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyTableRow
                        {
                            Content = new StackPanel
                            {
                                Children =
                                {
                                    new DaisyTableCell { Content = "A" },
                                    new DaisyTableCell { Content = "B" }
                                }
                            }
                        }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(table);
            await RuntimeTestHelpers.EnsureLoadedAsync(table);

            Assert.AreEqual(FlowerySizeManager.CurrentSize, table.Size);
            Assert.IsFalse(table.Zebra);
            Assert.IsFalse(table.PinRows);
            Assert.IsFalse(table.PinCols);
        }

        [TestMethod]
        public void When_TableRow_Constructed_DefaultsHold()
        {
            var row = new DaisyTableRow();

            Assert.AreEqual(DaisySize.Medium, row.Size);
            Assert.IsFalse(row.IsActive);
            Assert.IsTrue(row.HighlightOnHover);
            Assert.IsFalse(row.IsHeader);
        }

        [TestMethod]
        public void When_TableCell_Constructed_DefaultsHold()
        {
            var cell = new DaisyTableCell();

            Assert.IsNull(cell.ColumnWidth);
        }

        [TestMethod]
        public async Task When_Navbar_Loads_DefaultsHold()
        {
            var navbar = new DaisyNavbar();

            RuntimeTestHelpers.AttachToHost(navbar);
            await RuntimeTestHelpers.EnsureLoadedAsync(navbar);

            Assert.IsNull(navbar.NavbarStart);
            Assert.IsNull(navbar.NavbarCenter);
            Assert.IsNull(navbar.NavbarEnd);
            Assert.IsFalse(navbar.IsFullWidth);
            Assert.IsTrue(navbar.HasShadow);
        }

        [TestMethod]
        public void When_Button_Constructed_DefaultsHold()
        {
            var button = new DaisyButton();

            Assert.AreEqual(DaisyButtonVariant.Default, button.Variant);
            Assert.AreEqual(DaisySize.Medium, button.Size);
            Assert.AreEqual(DaisyButtonStyle.Default, button.ButtonStyle);
            Assert.AreEqual(DaisyButtonShape.Default, button.Shape);
            Assert.AreEqual(IconPlacement.Left, button.IconPlacement);
            Assert.IsNull(button.IconSymbol);
            Assert.IsNull(button.IconData);
            Assert.IsFalse(button.IsExternallyControlled);
        }

        [TestMethod]
        public async Task When_ButtonGroup_Loads_DefaultsHold()
        {
            var group = new DaisyButtonGroup
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyButton { Content = "One" },
                        new DaisyButton { Content = "Two" }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(group);
            await RuntimeTestHelpers.EnsureLoadedAsync(group);

            Assert.AreEqual(DaisyButtonVariant.Default, group.Variant);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, group.Size);
            Assert.AreEqual(DaisyButtonStyle.Default, group.ButtonStyle);
            Assert.AreEqual(DaisyButtonGroupShape.Default, group.Shape);
            Assert.AreEqual(Orientation.Vertical, group.Orientation);
            Assert.IsFalse(group.AutoSelect);
            Assert.AreEqual(-1, group.SelectedIndex);
            Assert.AreEqual(DaisyButtonGroupSelectionMode.Single, group.SelectionMode);
            Assert.IsEmpty(group.SelectedIndices);
        }

        [TestMethod]
        public async Task When_Input_Loads_DefaultsHold()
        {
            var input = new DaisyInput();

            RuntimeTestHelpers.AttachToHost(input);
            await RuntimeTestHelpers.EnsureLoadedAsync(input);

            Assert.AreEqual(DaisyInputVariant.Bordered, input.Variant);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, input.Size);
            Assert.AreEqual(DaisyLabelPosition.Top, input.LabelPosition);
            Assert.IsFalse(input.IsRequired);
            Assert.IsFalse(input.IsOptional);
            Assert.IsNull(input.Label);
            Assert.IsNull(input.PlaceholderText);
            Assert.IsNull(input.HintText);
            Assert.IsNull(input.HelperText);
            Assert.AreEqual(string.Empty, input.Text);
            Assert.IsFalse(input.HasText);
            Assert.IsFalse(input.AcceptsReturn);
            Assert.AreEqual(TextWrapping.NoWrap, input.TextWrapping);
            Assert.IsFalse(input.IsReadOnly);
            Assert.AreEqual(0, input.MaxLength);
        }

        [TestMethod]
        public async Task When_TextArea_Loads_DefaultsHold()
        {
            var textArea = new DaisyTextArea();

            RuntimeTestHelpers.AttachToHost(textArea);
            await RuntimeTestHelpers.EnsureLoadedAsync(textArea);

            Assert.IsTrue(textArea.AcceptsReturn);
            Assert.AreEqual(TextWrapping.Wrap, textArea.TextWrapping);
            Assert.IsGreaterThanOrEqualTo(80.0, textArea.MinHeight);
            Assert.IsFalse(textArea.ShowCharacterCount);
            Assert.IsFalse(textArea.IsAutoGrow);
            Assert.AreEqual(3, textArea.MinRows);
            Assert.AreEqual(0, textArea.MaxRows);
            Assert.IsTrue(textArea.CanResize);
        }

        [TestMethod]
        public async Task When_List_Loads_DefaultsHold()
        {
            var row = new DaisyListRow
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Left" },
                        new TextBlock { Text = "Right" }
                    }
                }
            };
            var list = new DaisyList
            {
                Content = new StackPanel
                {
                    Children = { row }
                }
            };

            RuntimeTestHelpers.AttachToHost(list);
            await RuntimeTestHelpers.EnsureLoadedAsync(list);

            Assert.AreEqual(FlowerySizeManager.CurrentSize, list.Size);
            Assert.AreEqual(list.Size, row.Size);
            Assert.AreEqual(1, row.GrowColumn);
            Assert.AreEqual(12.0, row.Spacing);
        }

        [TestMethod]
        public void When_ListColumn_Constructed_DefaultsHold()
        {
            var column = new DaisyListColumn();

            Assert.IsFalse(column.Grow);
            Assert.IsFalse(column.Wrap);
            Assert.AreEqual(VerticalAlignment.Center, column.VerticalAlignment);
        }

        [TestMethod]
        public async Task When_Menu_Loads_DefaultsHold()
        {
            var item = new DaisyMenuItem { Content = "Item" };
            var menu = new DaisyMenu
            {
                Content = new StackPanel
                {
                    Children = { item }
                }
            };

            RuntimeTestHelpers.AttachToHost(menu);
            await RuntimeTestHelpers.EnsureLoadedAsync(menu);

            Assert.AreEqual(Orientation.Vertical, menu.Orientation);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, menu.Size);
            Assert.IsNull(menu.ActiveForeground);
            Assert.IsNull(menu.ActiveBackground);
            Assert.IsFalse(item.IsActive);
            Assert.IsFalse(item.IsDisabled);
        }

        [TestMethod]
        public void When_MenuFlyoutItem_LocalizationSource_UpdatesText()
        {
            var item = new DaisyMenuFlyoutItem
            {
                LocalizationKey = "KeyA",
                LocalizationSource = new Dictionary<string, string>
                {
                    ["KeyA"] = "Alpha"
                }
            };

            Assert.AreEqual("Alpha", item.Text);
        }

        [TestMethod]
        public async Task When_Join_Loads_DefaultsHold()
        {
            var join = new DaisyJoin
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyButton { Content = "A" },
                        new DaisyButton { Content = "B" }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(join);
            await RuntimeTestHelpers.EnsureLoadedAsync(join);

            Assert.AreEqual(Orientation.Horizontal, join.Orientation);
            Assert.AreEqual(-1, join.ActiveIndex);
            Assert.IsNull(join.ActiveBackground);
            Assert.IsNull(join.ActiveForeground);
            Assert.HasCount(2, join.JoinedItems);
        }

        [TestMethod]
        public async Task When_Dock_Loads_DefaultsHold()
        {
            var dock = new DaisyDock
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new Border { Width = 10, Height = 10 },
                        new Border { Width = 10, Height = 10 }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(dock);
            await RuntimeTestHelpers.EnsureLoadedAsync(dock);

            Assert.AreEqual(FlowerySizeManager.CurrentSize, dock.Size);
            Assert.IsTrue(dock.AutoSelect);
            Assert.AreEqual(-1, dock.SelectedIndex);
        }

        [TestMethod]
        public async Task When_Drawer_Loads_DefaultsHold()
        {
            var drawer = new DaisyDrawer();

            RuntimeTestHelpers.AttachToHost(drawer);
            await RuntimeTestHelpers.EnsureLoadedAsync(drawer);

            Assert.AreEqual(300.0, drawer.DrawerWidth);
            Assert.AreEqual(SplitViewPanePlacement.Left, drawer.DrawerSide);
            Assert.IsFalse(drawer.IsDrawerOpen);
            Assert.IsFalse(drawer.OverlayMode);
            Assert.IsFalse(drawer.ResponsiveMode);
            Assert.AreEqual(500.0, drawer.ResponsiveThreshold);
            Assert.AreEqual(SplitViewDisplayMode.Inline, drawer.DisplayMode);
            Assert.AreEqual(SplitViewPanePlacement.Left, drawer.PanePlacement);
            Assert.AreEqual(300.0, drawer.OpenPaneLength);
            Assert.IsFalse(drawer.IsPaneOpen);
        }

        [TestMethod]
        public async Task When_FileInput_Loads_DefaultsHold()
        {
            var input = new DaisyFileInput();
            var expectedFileName = FloweryLocalization.GetStringInternal("FileInput_NoFileChosen", "No file chosen");
            var expectedButtonText = FloweryLocalization.GetStringInternal("FileInput_ChooseFile", "Choose File");

            RuntimeTestHelpers.AttachToHost(input);
            await RuntimeTestHelpers.EnsureLoadedAsync(input);

            Assert.AreEqual(expectedFileName, input.FileName);
            Assert.AreEqual(expectedButtonText, input.ButtonText);
            Assert.AreEqual(DaisyButtonVariant.Default, input.Variant);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, input.Size);
        }

        [TestMethod]
        public async Task When_Swap_Loads_DefaultsHold()
        {
            var swap = new DaisySwap();

            RuntimeTestHelpers.AttachToHost(swap);
            await RuntimeTestHelpers.EnsureLoadedAsync(swap);

            Assert.IsNull(swap.OnContent);
            Assert.IsNull(swap.OffContent);
            Assert.IsNull(swap.IndeterminateContent);
            Assert.AreEqual(SwapEffect.None, swap.TransitionEffect);
            Assert.AreNotEqual(true, swap.IsChecked);
        }

        [TestMethod]
        public async Task When_Stack_Loads_DefaultsHold_AndNavigationWraps()
        {
            var stack = new DaisyStack
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new Border { Width = 10, Height = 10 },
                        new Border { Width = 10, Height = 10 }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(stack);
            await RuntimeTestHelpers.EnsureLoadedAsync(stack);

            Assert.IsFalse(stack.ShowNavigation);
            Assert.AreEqual(DaisyStackNavigation.Horizontal, stack.NavigationPlacement);
            Assert.AreEqual(DaisyColor.Default, stack.NavigationColor);
            Assert.IsFalse(stack.ShowCounter);
            Assert.AreEqual(DaisyPlacement.Bottom, stack.CounterPlacement);
            Assert.AreEqual(0, stack.SelectedIndex);
            Assert.AreEqual(0.6, stack.StackOpacity);
            Assert.AreEqual(2, stack.ItemCount);

            stack.Next();
            Assert.AreEqual(1, stack.SelectedIndex);
            stack.Next();
            Assert.AreEqual(0, stack.SelectedIndex);
        }

        [TestMethod]
        public async Task When_Mockup_Loads_DefaultsHold()
        {
            var mockup = new DaisyMockup();

            RuntimeTestHelpers.AttachToHost(mockup);
            await RuntimeTestHelpers.EnsureLoadedAsync(mockup);

            Assert.AreEqual(DaisyMockupVariant.Code, mockup.Variant);
            Assert.AreEqual("https://daisyui.com", mockup.Url);
            Assert.AreEqual(DaisyChromeStyle.Mac, mockup.ChromeStyle);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, mockup.Size);
            Assert.IsNull(mockup.AppIcon);
        }

        [TestMethod]
        public async Task When_Mask_Loads_DefaultsHold()
        {
            var mask = new DaisyMask
            {
                Content = new Border { Width = 10, Height = 10 }
            };

            RuntimeTestHelpers.AttachToHost(mask);
            await RuntimeTestHelpers.EnsureLoadedAsync(mask);

            Assert.AreEqual(DaisyMaskVariant.Squircle, mask.Variant);
        }

        [TestMethod]
        public async Task When_Hero_Loads_DefaultsHold()
        {
            var hero = new DaisyHero
            {
                Content = new TextBlock { Text = "Hero" }
            };

            RuntimeTestHelpers.AttachToHost(hero);
            await RuntimeTestHelpers.EnsureLoadedAsync(hero);

            Assert.AreEqual(0.0, hero.OverlayOpacity);
            Assert.AreEqual(300.0, hero.MinimumHeight);
            Assert.AreEqual(HorizontalAlignment.Stretch, hero.HorizontalAlignment);
            Assert.AreEqual(VerticalAlignment.Stretch, hero.VerticalAlignment);
            Assert.AreEqual(HorizontalAlignment.Center, hero.HorizontalContentAlignment);
            Assert.AreEqual(VerticalAlignment.Center, hero.VerticalContentAlignment);
        }

        [TestMethod]
        public async Task When_HoverGallery_Loads_DefaultsHold()
        {
            var gallery = new DaisyHoverGallery();

            RuntimeTestHelpers.AttachToHost(gallery);
            await RuntimeTestHelpers.EnsureLoadedAsync(gallery);

            Assert.AreEqual(0, gallery.VisibleIndex);
            Assert.IsNull(gallery.DividerBrush);
            Assert.AreEqual(1.0, gallery.DividerThickness);
            Assert.IsTrue(gallery.ShowDividers);
            Assert.IsNotNull(gallery.Items);
            Assert.IsEmpty(gallery.Items);
        }

        [TestMethod]
        public void When_ExpandableCard_Constructed_DefaultsHold()
        {
            var card = new DaisyExpandableCard();

            Assert.IsFalse(card.IsExpanded);
            Assert.IsNull(card.ExpandedContent);
            Assert.AreEqual(TimeSpan.FromMilliseconds(300), card.AnimationDuration);
            Assert.IsNotNull(card.ToggleCommand);
            Assert.IsFalse(card.UseBatteriesIncludedMode);
            Assert.AreEqual("Play", card.ActionButtonText);
            Assert.AreEqual(150.0, card.CardWidth);
            Assert.AreEqual(225.0, card.CardHeight);
        }

        [TestMethod]
        public async Task When_Expander_Loads_DefaultsHold()
        {
            var expander = new DaisyExpander();

            RuntimeTestHelpers.AttachToHost(expander);
            await RuntimeTestHelpers.EnsureLoadedAsync(expander);

            Assert.IsNull(expander.Header);
            Assert.IsTrue(expander.IsExpanded);
            Assert.IsNull(expander.HeaderBackground);
            Assert.IsNull(expander.HeaderForeground);
            Assert.AreEqual(new Thickness(8, 6, 8, 6), expander.HeaderPadding);
            Assert.AreEqual(new Thickness(0), expander.ContentPadding);
            Assert.AreEqual(12.0, expander.ChevronSize);
            Assert.IsTrue(expander.ShowChevron);
        }

        [TestMethod]
        public async Task When_PatternedCard_Loads_DefaultsHold()
        {
            var card = new DaisyPatternedCard();

            RuntimeTestHelpers.AttachToHost(card);
            await RuntimeTestHelpers.EnsureLoadedAsync(card);

            Assert.IsFalse(card.DeferPatternGeneration);
            Assert.AreEqual(DaisyCardPattern.None, card.Pattern);
            Assert.AreEqual(FloweryPatternMode.SvgAsset, card.PatternMode);
            Assert.AreEqual(DaisyCardOrnament.None, card.Ornament);
        }

        [TestMethod]
        public async Task When_ContributionGraph_Loads_DefaultsHold()
        {
            var graph = new DaisyContributionGraph();

            RuntimeTestHelpers.AttachToHost(graph);
            await RuntimeTestHelpers.EnsureLoadedAsync(graph);

            Assert.AreEqual(DateTime.Now.Year, graph.Year);
            Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek, graph.StartDayOfWeek);
            Assert.IsTrue(graph.ShowLegend);
            Assert.IsTrue(graph.ShowToolTips);
            Assert.IsTrue(graph.ShowMonthLabels);
            Assert.IsTrue(graph.ShowDayLabels);
            Assert.IsFalse(graph.HighlightMonthStartBorders);
            Assert.AreEqual(12.0, graph.CellSize);
            Assert.AreEqual(new Thickness(1), graph.CellMargin);
            Assert.AreEqual(new CornerRadius(2), graph.CellCornerRadius);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, graph.Size);
        }

        [TestMethod]
        public async Task When_DateTimeline_Loads_DefaultsHold()
        {
            var timeline = new DaisyDateTimeline();

            RuntimeTestHelpers.AttachToHost(timeline);
            await RuntimeTestHelpers.EnsureLoadedAsync(timeline);

            Assert.AreEqual(DateTime.Today.AddMonths(-1), timeline.FirstDate);
            Assert.AreEqual(DateTime.Today.AddMonths(3), timeline.LastDate);
            Assert.AreEqual(DateTime.Today, timeline.SelectedDate);
            Assert.AreEqual(64.0, timeline.ItemWidth);
            Assert.AreEqual(8.0, timeline.ItemSpacing);
            Assert.AreEqual(DateElementDisplay.Default, timeline.DisplayElements);
            Assert.AreEqual(DateDisableStrategy.None, timeline.DisableStrategy);
            Assert.AreEqual(DateSelectionMode.AutoCenter, timeline.SelectionMode);
            Assert.AreEqual(DateTimelineHeaderType.MonthYear, timeline.HeaderType);
            Assert.AreEqual(ScrollBarVisibility.Hidden, timeline.ScrollBarVisibility);
            Assert.IsFalse(timeline.AutoWidth);
            Assert.AreEqual(7, timeline.VisibleDaysCount);
            Assert.IsTrue(timeline.ShowTodayHighlight);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, timeline.Size);
            Assert.AreEqual(DateTime.Today.ToString("MMMM yyyy", CultureInfo.CurrentCulture), timeline.HeaderText);
        }

        [TestMethod]
        public async Task When_Diff_Loads_DefaultsHold()
        {
            var diff = new DaisyDiff();

            RuntimeTestHelpers.AttachToHost(diff);
            await RuntimeTestHelpers.EnsureLoadedAsync(diff);

            Assert.IsNull(diff.Image1);
            Assert.IsNull(diff.Image2);
            Assert.AreEqual(50.0, diff.Offset);
            Assert.AreEqual(DiffOrientation.Horizontal, diff.Orientation);
        }

        [TestMethod]
        public async Task When_SlideToConfirm_Loads_DefaultsHold()
        {
            var slide = new DaisySlideToConfirm();
            var expectedText = FloweryLocalization.GetStringInternal("SlideToConfirm_Text", "SLIDE TO CONFIRM");
            var expectedConfirming = FloweryLocalization.GetStringInternal("SlideToConfirm_Confirming", "CONFIRMING...");
            var expectedIcon = FloweryPathHelpers.GetIconPathData("DaisyIconPowerOff");

            RuntimeTestHelpers.AttachToHost(slide);
            await RuntimeTestHelpers.EnsureLoadedAsync(slide);

            Assert.AreEqual(DaisySlideToConfirmVariant.Default, slide.Variant);
            Assert.AreEqual(DaisyDepthStyle.Flat, slide.DepthStyle);
            Assert.AreEqual(FlowerySizeManager.CurrentSize, slide.Size);
            Assert.AreEqual(expectedText, slide.Text);
            Assert.AreEqual(expectedConfirming, slide.ConfirmingText);
            Assert.AreEqual(expectedIcon, slide.IconData);
            Assert.AreEqual(TimeSpan.FromMilliseconds(900), slide.ResetDelay);
            Assert.IsFalse(slide.AutoReset);
            Assert.IsTrue(double.IsNaN(slide.TrackWidth));
            Assert.IsTrue(double.IsNaN(slide.TrackHeight));
        }

        [TestMethod]
        public async Task When_ProductThemeDropdown_Loads_DefaultsHold()
        {
            var dropdown = new DaisyProductThemeDropdown();

            RuntimeTestHelpers.AttachToHost(dropdown);
            await RuntimeTestHelpers.EnsureLoadedAsync(dropdown);

            Assert.IsTrue(dropdown.ApplyOnSelection);
            Assert.AreEqual(200.0, dropdown.MinWidth);
        }
    }
}

using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using static Flowery.Uno.RuntimeTests.RuntimeTestHelpers;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_SemanticBrushes
    {
        [TestMethod]
        public void When_PalettesAreCreated_TextAndSoftRolesUseSourceAlpha()
        {
            foreach (var theme in new[] { "Light", "Dark", "Business" })
            {
                var palette = DaisyPaletteFactory.Create(theme);
                AssertRole(palette, "DaisyBaseContentMuted", "DaisyBaseContent", 0.7);
                AssertRole(palette, "DaisyBaseContentSubtle", "DaisyBaseContent", 0.5);
                AssertRole(palette, "DaisyBaseContentDisabled", "DaisyBaseContent", 0.5);
                foreach (var variant in new[] { "Primary", "Secondary", "Accent", "Info", "Success", "Warning", "Error" })
                {
                    var prefix = "DaisyButton" + variant;
                    AssertRole(palette, prefix + "Soft", "Daisy" + variant, theme == "Business" && variant == "Primary" ? 0.12 : 0.15);
                    AssertRole(palette, prefix + "SoftHover", "Daisy" + variant, 0.3);
                    AssertRole(palette, prefix + "SoftBorder", "Daisy" + variant, 0.45);
                }
            }
        }

        [TestMethod]
        public void When_CustomRolesAreApplied_OverridesAndStableBrushesSurviveDarkToDarkChanges()
        {
            var previous = DaisyThemeManager.CurrentThemeName ?? "Dark";
            var transparent = new SolidColorBrush(Colors.Red) { Opacity = 0 };
            var translucentBorder = new SolidColorBrush(Color.FromArgb(128, 10, 20, 30)) { Opacity = 0.4 };
            var originalBorderOpacity = translucentBorder.Opacity;
            DaisyThemeManager.RegisterTheme(new DaisyThemeInfo("SemanticRolesTest", true), () => new ResourceDictionary
            {
                ["DaisyBaseContentColor"] = Color.FromArgb(128, 40, 50, 60),
                ["DaisyPrimaryColor"] = Colors.Blue,
                ["DaisyBaseContentSubtleColor"] = Colors.Orange,
                ["DaisyBaseContentSubtleBrush"] = new SolidColorBrush(Colors.Blue),
                MergedDictionaries =
                {
                    new ResourceDictionary
                    {
                        ["DaisyBaseContentMutedBrush"] = transparent,
                        ["DaisyButtonPrimarySoftBorderBrush"] = translucentBorder
                    }
                }
            });
            DaisyThemeManager.RegisterTheme(new DaisyThemeInfo("SemanticPartialTest", true),
                () => new ResourceDictionary { ["DaisyPrimaryColor"] = Colors.Green });
            try
            {
                DaisyThemeManager.ApplyTheme("Dark");
                var resources = Application.Current.Resources;
                var muted = DaisyResourceLookup.GetBrush("DaisyBaseContentMutedBrush");
                var border = DaisyResourceLookup.GetBrush("DaisyButtonPrimarySoftBorderBrush");
                Assert.IsTrue(DaisyThemeManager.ApplyTheme("SemanticRolesTest"));
                Assert.AreSame(muted, DaisyResourceLookup.GetBrush("DaisyBaseContentMutedBrush"));
                AssertBrush(muted, Color.FromArgb(0, 255, 0, 0));
                AssertBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush"), Colors.Orange);
                AssertBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentDisabledBrush"), Color.FromArgb(64, 40, 50, 60));
                AssertBrush(border, Color.FromArgb(51, 10, 20, 30));
                Assert.AreEqual(Colors.Red, transparent.Color, "Publishing must not modify the supplied brush.");
                Assert.AreEqual(0.0, transparent.Opacity);
                Assert.AreEqual(originalBorderOpacity, translucentBorder.Opacity);

                Assert.IsTrue(DaisyThemeManager.ApplyTheme("SemanticPartialTest"));
                AssertBrush(muted, Color.FromArgb(90, 40, 50, 60));
                AssertBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush"), Color.FromArgb(64, 40, 50, 60));
                AssertBrush(border, Color.FromArgb(115, Colors.Green.R, Colors.Green.G, Colors.Green.B));

                Assert.IsTrue(DaisyThemeManager.ApplyTheme("Coffee"));
                var coffee = DaisyPaletteFactory.Create("Coffee");
                Assert.AreSame(muted, DaisyResourceLookup.GetBrush("DaisyBaseContentMutedBrush"));
                Assert.AreSame(border, DaisyResourceLookup.GetBrush("DaisyButtonPrimarySoftBorderBrush"));
                AssertBrush(muted, (Color)coffee["DaisyBaseContentMutedColor"]);
                AssertBrush(border, (Color)coffee["DaisyButtonPrimarySoftBorderColor"]);
                Assert.AreEqual((Color)coffee["DaisyBaseContentMutedColor"], (Color)resources["DaisyBaseContentMutedColor"]);
            }
            finally
            {
                DaisyThemeManager.ApplyTheme(previous);
            }
        }

        [TestMethod]
        public async Task When_SemanticConsumersRefresh_SoftSelectionAndMenuTitlesKeepTheirContracts()
        {
            var previous = DaisyThemeManager.CurrentThemeName ?? "Dark";
            var host = new StackPanel();
            var button = new DaisyButton { Content = "Soft", Variant = DaisyButtonVariant.Primary, ButtonStyle = DaisyButtonStyle.Soft };
            var segment = new Button { Content = "Segment" };
            var group = new DaisyButtonGroup
            {
                Variant = DaisyButtonVariant.Primary, ButtonStyle = DaisyButtonStyle.Soft, AutoSelect = true,
                Content = new StackPanel { Children = { segment } }
            };
            var title = new InvokableMenuItem { Content = "Section", IsTitle = true, IsActive = true };
            var item = new InvokableMenuItem { Content = "Action" };
            var menu = new DaisyMenu { Content = new StackPanel { Children = { title, item } } };
            var stat = new DaisyStat { Title = "Total", Value = "42", Description = "Accounts" };
            var calendar = new DaisyCalendarDatePicker { PlaceholderText = "Select date" };
            host.Children.Add(button);
            host.Children.Add(group);
            host.Children.Add(menu);
            host.Children.Add(stat);
            host.Children.Add(calendar);
            var invoked = 0;
            menu.ItemInvoked += (_, _) => invoked++;
            try
            {
                DaisyThemeManager.ApplyTheme("Light");
                AttachToHost(host);
                await CompleteLayoutAsync(host, button, group, segment, menu, title, item, stat, calendar);
                var nativeCalendar = (CalendarDatePicker)((Grid)((Border)calendar.Content).Child).Children[0];
                foreach (var theme in new[] { "Light", "Coffee" })
                {
                    DaisyThemeManager.ApplyTheme(theme);
                    group.SelectedIndex = -1;
                    await CompleteLayoutAsync(host);
                    AssertBrush(button.Background, RoleColor("DaisyButtonPrimarySoft"));
                    AssertBrush(button.BorderBrush, RoleColor("DaisyButtonPrimarySoftBorder"));
                    Assert.AreEqual(new Thickness(1), button.BorderThickness);
                    AssertBrush((Brush)button.Resources["ButtonBackgroundPointerOver"], RoleColor("DaisyButtonPrimarySoftHover"));
                    AssertBrush((Brush)button.Resources["ButtonBackgroundPressed"], RoleColor("DaisyButtonPrimarySoftHover"));
                    AssertBrush(segment.Background, RoleColor("DaisyButtonPrimarySoft"));
                    AssertBrush(segment.Foreground, RoleColor("DaisyButtonPrimaryForeground"));
                    group.SelectedIndex = 0;
                    AssertBrush(segment.Background, RoleColor("DaisyButtonPrimarySoftHover"));
                    AssertBrush(segment.BorderBrush, RoleColor("DaisyButtonPrimarySoftBorder"));
                    Assert.IsFalse(title.IsTabStop);
                    var titleBorder = (Border)title.Content;
                    Assert.IsFalse(titleBorder.IsHitTestVisible);
                    Assert.AreEqual(new Thickness(0, 0, 0, 1), titleBorder.BorderThickness);
                    Assert.AreEqual(Microsoft.UI.Text.FontWeights.Bold, ((ContentPresenter)titleBorder.Child).FontWeight);
                    AssertBrush(title.Foreground, RoleColor("DaisyPrimary"));
                    var selection = (Border)((Grid)((Border)menu.Content).Child).Children[0];
                    Assert.AreEqual(0.0, selection.Opacity, "An active title must not acquire a selection indicator.");
                    title.Invoke();
                    Assert.AreEqual(0, invoked);
                    var labels = (StackPanel)((Grid)stat.Content).Children[0];
                    foreach (var index in new[] { 0, 2 })
                    {
                        var label = (TextBlock)labels.Children[index];
                        AssertBrush(label.Foreground, RoleColor("DaisyBaseContentMuted"));
                        Assert.AreEqual(1.0, label.Opacity);
                    }
                    Assert.IsNull(nativeCalendar.Date);
                    foreach (var suffix in new[] { "", "PointerOver", "Pressed", "Disabled" })
                        AssertBrush((Brush)nativeCalendar.Resources["CalendarDatePickerTextForeground" + suffix], RoleColor("DaisyBaseContentMuted"));
                    AssertBrush((Brush)nativeCalendar.Resources["CalendarDatePickerTextForegroundSelected"], RoleColor("DaisyBaseContent"));
                    calendar.Date = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
                    Assert.AreEqual(calendar.Date, nativeCalendar.Date);
                    calendar.Date = null;
                    Assert.IsNull(nativeCalendar.Date);
                }
                item.Invoke();
                Assert.AreEqual(1, invoked);
                item.IsDisabled = true;
                AssertBrush(item.Foreground, RoleColor("DaisyBaseContentDisabled"));
                Assert.AreEqual(1.0, ((Border)item.Content).Opacity);
                item.Invoke();
                Assert.AreEqual(1, invoked);
                item.IsDisabled = false;
                title.IsTitle = false;
                Assert.IsTrue(title.IsTabStop);
                Assert.AreEqual(new Thickness(0), ((Border)title.Content).BorderThickness);
                title.Invoke();
                Assert.AreEqual(2, invoked);
                stat.DescriptionVariant = DaisyStatVariant.Success;
                var description = (TextBlock)((StackPanel)((Grid)stat.Content).Children[0]).Children[2];
                AssertBrush(description.Foreground, RoleColor("DaisySuccess"));
                Assert.AreEqual(1.0, description.Opacity);
                var transparent = new SolidColorBrush(Colors.Transparent);
                button.Resources["DaisyButtonBackground"] = transparent;
                button.Variant = DaisyButtonVariant.Error;
                Assert.AreSame(transparent, button.Background);
                stat.Resources["DaisyStatDescriptionForeground"] = transparent;
                stat.DescriptionVariant = DaisyStatVariant.Default;
                Assert.AreSame(transparent, description.Foreground);
                Assert.AreEqual(1.0, description.Opacity);
                calendar.Resources["DaisyCalendarDatePickerPlaceholderForeground"] = transparent;
                foreach (var theme in new[] { "Dark", "Coffee" })
                {
                    DaisyThemeManager.ApplyTheme(theme);
                    await CompleteLayoutAsync(host);
                    Assert.AreSame(transparent, nativeCalendar.Resources["CalendarDatePickerTextForeground"]);
                    AssertBrush((Brush)nativeCalendar.Resources["CalendarDatePickerTextForegroundSelected"], RoleColor("DaisyBaseContent"));
                }
                calendar.Date = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
                host.Children.Remove(calendar);
                await CompleteLayoutAsync(host);
                host.Children.Add(calendar);
                await CompleteLayoutAsync(host, calendar, nativeCalendar);
                Assert.AreSame(nativeCalendar, ((Grid)((Border)calendar.Content).Child).Children[0]);
                Assert.AreEqual(calendar.Date, nativeCalendar.Date);
                Assert.AreSame(transparent, nativeCalendar.Resources["CalendarDatePickerTextForeground"]);
            }
            finally
            {
                GetHostPanel().Children.Clear();
                await CompleteLayoutAsync(GetHostPanel());
                DaisyThemeManager.ApplyTheme(previous);
            }
        }

        private static Color RoleColor(string prefix) => (Color)Application.Current.Resources[prefix + "Color"];

        private static void AssertRole(ResourceDictionary palette, string prefix, string source, double opacity)
        {
            var color = (Color)palette[source + "Color"];
            var expected = Color.FromArgb((byte)Math.Round(color.A * opacity), color.R, color.G, color.B);
            Assert.AreEqual(expected, (Color)palette[prefix + "Color"], prefix);
            AssertBrush((Brush)palette[prefix + "Brush"], expected);
        }

        private static void AssertBrush(Brush? brush, Color color)
        {
            Assert.IsInstanceOfType<SolidColorBrush>(brush);
            Assert.AreEqual(color, ((SolidColorBrush)brush).Color);
            Assert.AreEqual(1.0, brush.Opacity, "Semantic alpha belongs to Color, without a second opacity multiplier.");
        }

        private sealed partial class InvokableMenuItem : DaisyMenuItem
        {
            public void Invoke() => OnBaseClick();
        }
    }
}

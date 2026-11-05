using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Text;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_Mnemonics
    {
        [TestMethod]
        public void When_Parse_Called_StripsMarkersAndFindsAccessCharacter()
        {
            var cases = new (string? Source, string Display, int Index, char? Mnemonic)[]
            {
                ("&Texte", "Texte", 0, 'T'),
                ("Te&xt / Kalkulation", "Text / Kalkulation", 2, 'x'),
                ("Save && Exit", "Save & Exit", -1, null),
                ("&&&A", "&A", 1, 'A'),
                ("A&B&C", "ABC", 1, 'B'),
                ("Trailing&", "Trailing", -1, null),
                ("Reset crop & rotation", "Reset crop & rotation", -1, null),
                ("A & &B", "A & B", 4, 'B'),
                ("Plain", "Plain", -1, null),
                ("", "", -1, null),
                (null, "", -1, null)
            };

            foreach (var (source, display, index, mnemonic) in cases)
            {
                var info = FloweryMnemonicHelpers.Parse(source);
                Assert.AreEqual(display, info.DisplayText, $"Display text for '{source}'");
                Assert.AreEqual(index, info.MnemonicIndex, $"Mnemonic index for '{source}'");
                Assert.AreEqual(mnemonic, info.MnemonicChar, $"Mnemonic character for '{source}'");
                Assert.AreEqual(display, FloweryMnemonicHelpers.GetDisplayText(source));
                Assert.AreEqual(mnemonic, FloweryMnemonicHelpers.GetMnemonicChar(source));
            }
        }

        [TestMethod]
        public void When_MnemonicText_HasMarker_UnderlinesAccessCharacter()
        {
            var text = new DaisyMnemonicText { Text = "&Texte" };
            var textBlock = (TextBlock)text.Content;

            Assert.AreEqual('T', text.MnemonicChar);
            AssertUnderlinedRuns(textBlock, "", "T", "exte");
        }

        [TestMethod]
        public void When_MnemonicText_ShowMnemonicOff_ShowsPlainDisplayText()
        {
            var text = new DaisyMnemonicText { Text = "Te&xt", ShowMnemonic = false };
            var textBlock = (TextBlock)text.Content;

            Assert.AreEqual('x', text.MnemonicChar);
            Assert.AreEqual("Text", textBlock.Text);
            Assert.IsFalse(textBlock.Inlines.OfType<Run>().Any(run => run.TextDecorations == TextDecorations.Underline));

            text.ShowMnemonic = true;
            AssertUnderlinedRuns(textBlock, "Te", "x", "t");
        }

        [TestMethod]
        public void When_MnemonicText_HasLiteralAmpersand_ShowsItWithoutUnderline()
        {
            var text = new DaisyMnemonicText { Text = "Save && Exit" };
            var textBlock = (TextBlock)text.Content;

            Assert.IsNull(text.MnemonicChar);
            Assert.AreEqual("Save & Exit", textBlock.Text);
        }

        [TestMethod]
        public async Task When_Button_HasMarker_KeepsSourceTextAndShowsMnemonic()
        {
            var button = new DaisyButton { Content = "&Save" };

            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);
            button.UpdateLayout();

            Assert.AreEqual("&Save", button.Content as string);
            Assert.AreEqual('S', FloweryMnemonicHelpers.GetMnemonicChar(button.Content as string));

            var mnemonicText = FindDescendant<DaisyMnemonicText>(button);
            Assert.IsNotNull(mnemonicText, "String content with a marker should render as DaisyMnemonicText.");
            Assert.AreEqual('S', mnemonicText.MnemonicChar);

            var textBlock = (TextBlock)mnemonicText.Content;
            AssertUnderlinedRuns(textBlock, "", "S", "ave");
            Assert.AreEqual(button.FontWeight.Weight, textBlock.FontWeight.Weight, "The label should inherit the button font weight.");

            button.Content = "Save";
            button.UpdateLayout();
            Assert.IsNull(button.ContentTemplate, "Plain content should drop the mnemonic template.");
            Assert.IsNull(FindDescendant<DaisyMnemonicText>(button));
        }

        [TestMethod]
        public async Task When_Button_HasOwnContentTemplate_KeepsIt()
        {
            var template = (DataTemplate)XamlReader.Load(
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                "<TextBlock Text='{Binding}' /></DataTemplate>");
            var button = new DaisyButton { ContentTemplate = template, Content = "&Save" };

            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);
            button.UpdateLayout();

            Assert.AreSame(template, button.ContentTemplate);
            Assert.IsNull(FindDescendant<DaisyMnemonicText>(button));
        }

        [TestMethod]
        public async Task When_Button_HasNoMarker_KeepsDefaultPresentation()
        {
            var button = new DaisyButton { Content = "Save" };

            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);
            button.UpdateLayout();

            Assert.IsNull(FindDescendant<DaisyMnemonicText>(button));
        }

        [TestMethod]
        public async Task When_IconButton_HasMarker_ShowsMnemonic()
        {
            var button = new DaisyButton { Content = "&Open", IconSymbol = Symbol.Folder };

            RuntimeTestHelpers.AttachToHost(button);
            await RuntimeTestHelpers.EnsureLoadedAsync(button);
            button.UpdateLayout();

            var mnemonicText = FindDescendant<DaisyMnemonicText>(button);
            Assert.IsNotNull(mnemonicText, "Icon buttons should show string content with a marker as DaisyMnemonicText.");
            Assert.AreEqual('O', mnemonicText.MnemonicChar);
        }

        [TestMethod]
        public async Task When_IconText_HasMarker_UnderlinesAccessCharacter()
        {
            var iconText = new DaisyIconText { Text = "&Print preview" };

            RuntimeTestHelpers.AttachToHost(iconText);
            await RuntimeTestHelpers.EnsureLoadedAsync(iconText);
            iconText.UpdateLayout();

            var textBlock = FindDescendant<TextBlock>(iconText);
            Assert.IsNotNull(textBlock);
            Assert.AreEqual("&Print preview", iconText.Text);
            AssertUnderlinedRuns(textBlock, "", "P", "rint preview");
            Assert.AreEqual("Print preview", DaisyAccessibility.GetAccessibleNameFromContent(iconText));
        }

        [TestMethod]
        public async Task When_TabHeader_HasMarker_UnderlinesAccessCharacter()
        {
            var tabs = new DaisyTabs
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new DaisyTabItem { Header = "&Texte", Content = new TextBlock { Text = "One" } },
                        new DaisyTabItem { Header = "Save && Exit", Content = new TextBlock { Text = "Two" } }
                    }
                }
            };

            RuntimeTestHelpers.AttachToHost(tabs);
            await RuntimeTestHelpers.EnsureLoadedAsync(tabs);
            tabs.UpdateLayout();

            var headerButtons = FindDescendants<Button>(tabs).Where(button => button.Content is TextBlock).ToList();
            Assert.HasCount(2, headerButtons);

            AssertUnderlinedRuns((TextBlock)headerButtons[0].Content, "", "T", "exte");
            Assert.AreEqual("Texte", AutomationProperties.GetName(headerButtons[0]));
            Assert.AreEqual("Save & Exit", ((TextBlock)headerButtons[1].Content).Text);
            Assert.AreEqual("Save & Exit", AutomationProperties.GetName(headerButtons[1]));
            Assert.AreEqual("&Texte", tabs.Items[0].Header);
        }

        [TestMethod]
        public async Task When_MenuItem_HasMarker_ShowsMnemonic()
        {
            var item = new DaisyMenuItem { Content = "&File" };
            var menu = new DaisyMenu { Content = new StackPanel { Children = { item } } };

            RuntimeTestHelpers.AttachToHost(menu);
            await RuntimeTestHelpers.EnsureLoadedAsync(menu);
            await RuntimeTestHelpers.EnsureLoadedAsync(item);
            menu.UpdateLayout();

            Assert.IsNotNull(FloweryMnemonicHelpers.StringContentTemplate, "The mnemonic string template should resolve from the Flowery theme resources.");
            var mnemonicText = FindDescendant<DaisyMnemonicText>(item);
            Assert.IsNotNull(mnemonicText, "Menu items should show string content with a marker as DaisyMnemonicText.");
            Assert.AreEqual('F', mnemonicText.MnemonicChar);
            Assert.AreEqual("File", AutomationProperties.GetName(item));
        }

        [TestMethod]
        public async Task When_MnemonicText_Rendered_DrawsUnderlineBelowAccessCharacterOnly()
        {
            var mnemonicText = new DaisyMnemonicText
            {
                Text = "&Save",
                FontSize = 24,
                Foreground = new SolidColorBrush(Colors.Black)
            };
            var host = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Child = mnemonicText
            };

            RuntimeTestHelpers.AttachToHost(host);
            await RuntimeTestHelpers.EnsureLoadedAsync(mnemonicText);
            host.UpdateLayout();

            var black = await CountLinePixelsAsync(host, mnemonicText, (r, g, b) => (r + g + b) / 3 < 96);
            Assert.IsGreaterThan(black.Columns / 2, black.UnderMnemonic, "A line should run below the access character.");
            Assert.AreEqual(0, black.UnderNeighbor, "The line should end at the access character.");

            mnemonicText.Foreground = new SolidColorBrush(Colors.Red);
            host.UpdateLayout();
            var red = await CountLinePixelsAsync(host, mnemonicText, (r, g, b) => r > 150 && g < 100 && b < 100);
            Assert.IsGreaterThan(red.Columns / 2, red.UnderMnemonic, "The line should follow the foreground.");

            mnemonicText.ShowMnemonic = false;
            host.UpdateLayout();
            var hidden = await CountLinePixelsAsync(host, mnemonicText, (r, g, b) => r > 150 && g < 100 && b < 100);
            Assert.AreEqual(0, hidden.UnderMnemonic, "The line should disappear with ShowMnemonic=false.");
        }

        private static async Task<(int UnderMnemonic, int UnderNeighbor, int Columns)> CountLinePixelsAsync(
            FrameworkElement host,
            DaisyMnemonicText mnemonicText,
            Func<byte, byte, byte, bool> isLine)
        {
            var textBlock = FindDescendant<TextBlock>(mnemonicText);
            Assert.IsNotNull(textBlock);

            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(host);
            var pixels = (await bitmap.GetPixelsAsync()).ToArray();
            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            var scale = width / host.ActualWidth;
            int ToPixels(double value) => (int)Math.Round(value * scale);

            // Columns of "S", "a" and "e" in "Save"; "a" and "e" have no descender.
            var s = ToPixels(MeasureWidth(textBlock, "S"));
            var sa = ToPixels(MeasureWidth(textBlock, "Sa"));
            var sav = ToPixels(MeasureWidth(textBlock, "Sav"));
            var save = ToPixels(MeasureWidth(textBlock, "Save"));

            (byte R, byte G, byte B) Pixel(int x, int y)
            {
                var offset = (y * width + x) * 4;
                return (pixels[offset + 2], pixels[offset + 1], pixels[offset]);
            }

            var glyphBottom = -1;
            for (var y = 0; y < height; y++)
            {
                for (var x = sav + 1; x < Math.Min(save, width) - 1; x++)
                {
                    var (r, g, b) = Pixel(x, y);
                    if ((r + g + b) / 3 < 200)
                    {
                        glyphBottom = y;
                    }
                }
            }

            Assert.IsGreaterThanOrEqualTo(0, glyphBottom, "The text should be rendered.");

            int Count(int fromX, int toX)
            {
                var count = 0;
                for (var y = glyphBottom + 1; y < height; y++)
                {
                    for (var x = fromX; x < toX; x++)
                    {
                        var (r, g, b) = Pixel(x, y);
                        if (isLine(r, g, b))
                        {
                            count++;
                        }
                    }
                }

                return count;
            }

            var mnemonicFrom = (int)(s * 0.15);
            var mnemonicTo = (int)(s * 0.85);
            return (Count(mnemonicFrom, mnemonicTo), Count(s + 1, sa - 1), mnemonicTo - mnemonicFrom);
        }

        private static double MeasureWidth(TextBlock source, string text)
        {
            var probe = new TextBlock
            {
                Text = text,
                FontFamily = source.FontFamily,
                FontSize = source.FontSize,
                FontWeight = source.FontWeight,
                FontStyle = source.FontStyle
            };
            probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            return probe.DesiredSize.Width;
        }

        private static void AssertUnderlinedRuns(TextBlock textBlock, string before, string mnemonic, string after)
        {
            var runs = textBlock.Inlines.OfType<Run>().ToList();
            var expectedCount = (before.Length > 0 ? 1 : 0) + 1 + (after.Length > 0 ? 1 : 0);
            Assert.HasCount(expectedCount, runs);

            var index = 0;
            if (before.Length > 0)
            {
                Assert.AreEqual(before, runs[index].Text);
                Assert.AreEqual(TextDecorations.None, runs[index].TextDecorations);
                index++;
            }

            Assert.AreEqual(mnemonic, runs[index].Text);
            Assert.AreEqual(TextDecorations.Underline, runs[index].TextDecorations);
            index++;

            if (after.Length > 0)
            {
                Assert.AreEqual(after, runs[index].Text);
                Assert.AreEqual(TextDecorations.None, runs[index].TextDecorations);
            }
        }

        private static T? FindDescendant<T>(DependencyObject root)
            where T : DependencyObject
        {
            return FindDescendants<T>(root).FirstOrDefault();
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
    }
}

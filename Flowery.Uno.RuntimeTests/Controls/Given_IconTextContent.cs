using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Flowery.Uno.RuntimeTests.RuntimeTestHelpers;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_IconTextContent
    {
        private const string PathData = "M0 0 L24 0 L24 24 Z";

        [TestMethod]
        public Task When_BoundContentAndSizeChange_LayoutUsesCurrentPresenceAndSpacing() =>
            WithHostAsync(static async host =>
            {
                var textSource = new ContentControl { Tag = "Initial" };
                var iconSource = new ContentControl { Tag = PathData };
                var icon = new DaisyIconText { Size = DaisySize.Medium, NeumorphicEnabled = false };
                icon.SetBinding(DaisyIconText.TextProperty, TagBinding(textSource));
                icon.SetBinding(DaisyIconText.IconDataProperty, TagBinding(iconSource));
                host.Children.Add(icon);
                await CompleteLayoutAsync(host, icon);

                foreach (var (placement, path, text) in new[]
                {
                    (IconPlacement.Left, PathData, "Left"),
                    (IconPlacement.Right, PathData, (string?)null),
                    (IconPlacement.Top, (string?)null, "Top"),
                    (IconPlacement.Bottom, PathData, "Bottom"),
                    (IconPlacement.Left, (string?)null, (string?)null),
                    (IconPlacement.Right, PathData, "Last")
                })
                {
                    textSource.Tag = text;
                    iconSource.Tag = path;
                    icon.IconPlacement = placement;
                    await CompleteLayoutAsync(host);
                    AssertLayout(icon, path != null, text);
                }

                foreach (var size in new[] { DaisySize.Large, DaisySize.Small })
                {
                    icon.Size = size;
                    await CompleteLayoutAsync(host);
                    AssertLayout(icon, true, "Last");
                    var panel = (StackPanel)icon.Content;
                    Assert.AreEqual(DaisyResourceLookup.GetDefaultIconSpacing(size), panel.Spacing);
                    Assert.AreEqual(DaisyResourceLookup.GetDefaultFontSize(size), ((TextBlock)panel.Children[0]).FontSize);
                }
                textSource.Tag = "Still bound";
                await CompleteLayoutAsync(host);
                AssertLayout(icon, true, "Still bound");
                Assert.IsNotNull(icon.GetBindingExpression(DaisyIconText.TextProperty));
                Assert.IsNotNull(icon.GetBindingExpression(DaisyIconText.IconDataProperty));
            });

        [TestMethod]
        public Task When_ContentChangesWhileUnloaded_ReloadRefreshesIconTextAndKbdBindings() =>
            WithHostAsync(static async host =>
            {
                foreach (var icon in new DaisyIconText[]
                {
                    new() { NeumorphicEnabled = false },
                    new DaisyKbd { NeumorphicEnabled = false }
                })
                {
                    var source = new ContentControl { Tag = "Before" };
                    icon.SetBinding(DaisyIconText.TextProperty, TagBinding(source));
                    host.Children.Add(icon);
                    await CompleteLayoutAsync(host, icon);
                    AssertLayout(icon, false, "Before");

                    host.Children.Remove(icon);
                    await CompleteLayoutAsync(host);
                    Assert.IsFalse(icon.IsLoaded);
                    source.Tag = "After";
                    icon.IconData = PathData;
                    icon.IconPlacement = IconPlacement.Bottom;
                    host.Children.Add(icon);
                    await CompleteLayoutAsync(host, icon);
                    AssertLayout(icon, true, "After");

                    source.Tag = null;
                    icon.IconData = null;
                    await CompleteLayoutAsync(host);
                    AssertLayout(icon, false, null);
                    source.Tag = "Restored";
                    await CompleteLayoutAsync(host);
                    AssertLayout(icon, false, "Restored");
                    Assert.IsNotNull(icon.GetBindingExpression(DaisyIconText.TextProperty));
                    host.Children.Remove(icon);
                    await CompleteLayoutAsync(host);
                }
            });

        private static void AssertLayout(DaisyIconText icon, bool hasIcon, string? text)
        {
            var panel = (StackPanel)icon.Content;
            var hasText = !string.IsNullOrEmpty(text);
            var horizontal = icon.IconPlacement is IconPlacement.Left or IconPlacement.Right;
            Assert.AreEqual(horizontal ? Orientation.Horizontal : Orientation.Vertical, panel.Orientation);
            Assert.AreEqual((hasIcon ? 1 : 0) + (hasText ? 1 : 0), panel.Children.Count);
            Assert.AreEqual(hasIcon && hasText ? icon.EffectiveSpacing : 0d, panel.Spacing);
            if (hasIcon)
            {
                var index = hasText && icon.IconPlacement is IconPlacement.Right or IconPlacement.Bottom ? 1 : 0;
                Assert.IsInstanceOfType<Viewbox>(panel.Children[index]);
            }
            if (hasText)
            {
                var index = hasIcon && icon.IconPlacement is IconPlacement.Left or IconPlacement.Top ? 1 : 0;
                Assert.AreEqual(text, ((TextBlock)panel.Children[index]).Text);
            }
        }

        private static Binding TagBinding(ContentControl source) => new()
        {
            Source = source,
            Path = new PropertyPath(nameof(ContentControl.Tag)),
            Mode = BindingMode.OneWay
        };

        private static async Task WithHostAsync(Func<StackPanel, Task> test)
        {
            var panel = RuntimeTestHelpers.GetHostPanel();
            var host = new StackPanel();
            try
            {
                panel.Children.Clear();
                panel.Children.Add(host);
                await CompleteLayoutAsync(host);
                await test(host);
            }
            finally
            {
                panel.Children.Clear();
                await CompleteLayoutAsync(panel);
            }
        }
    }
}

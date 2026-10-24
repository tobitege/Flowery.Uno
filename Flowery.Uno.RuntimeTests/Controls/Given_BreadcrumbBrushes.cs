using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Flowery.Uno.RuntimeTests.RuntimeTestHelpers;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_BreadcrumbBrushes
    {
        [TestMethod]
        public Task When_ParentSeparatorBrushChanges_FallbackThemeAndReloadKeepExistingContent() =>
            WithHostAsync(static async host =>
            {
                var first = CreateItem("First");
                var body = new TextBox { Text = "Retained edit" };
                var automatic = CreateItem(body);
                var resourceItem = CreateItem("Resource");
                var resourceBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
                resourceItem.Resources["DaisyBreadcrumbItemSeparatorForeground"] = resourceBrush;
                var breadcrumbs = CreateBreadcrumbs(first, automatic, resourceItem);
                host.Children.Add(breadcrumbs);
                await CompleteLayoutAsync(host, breadcrumbs, first, automatic, resourceItem, body);

                var root = automatic.Content;
                var separator = Separator(automatic);
                Assert.IsNull(breadcrumbs.SeparatorBrush);
                Assert.AreEqual(DependencyProperty.UnsetValue, breadcrumbs.ReadLocalValue(DaisyBreadcrumbs.SeparatorBrushProperty));
                AssertSeparator(automatic, DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush"));
                AssertSeparator(resourceItem, resourceBrush);
                Assert.AreEqual(Visibility.Collapsed, Separator(first).Visibility);
                Assert.AreEqual(Visibility.Visible, separator.Visibility);
                Assert.AreEqual(1.0, separator.Opacity);

                var parentBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue);
                breadcrumbs.SeparatorBrush = parentBrush;
                await CompleteLayoutAsync(host);
                AssertSeparator(automatic, parentBrush);
                AssertSeparator(resourceItem, parentBrush);

                var resources = Application.Current.Resources;
                const string baseToken = "DaisyBaseContentSubtleBrush";
                var hadBaseToken = resources.ContainsKey(baseToken);
                var originalBaseToken = hadBaseToken ? resources[baseToken] : null;
                var themeBrush = new SolidColorBrush(Microsoft.UI.Colors.Goldenrod);
                try
                {
                    resources[baseToken] = themeBrush;
                    NotifyThemeChanged();
                    await CompleteLayoutAsync(host);
                    AssertSeparator(automatic, parentBrush);
                    AssertSeparator(resourceItem, parentBrush);
                    breadcrumbs.SeparatorBrush = null;
                    await CompleteLayoutAsync(host);
                    AssertSeparator(automatic, themeBrush);
                    AssertSeparator(resourceItem, resourceBrush);

                    var nextThemeBrush = new SolidColorBrush(Microsoft.UI.Colors.Teal);
                    resources[baseToken] = nextThemeBrush;
                    NotifyThemeChanged();
                    await CompleteLayoutAsync(host);
                    AssertSeparator(automatic, nextThemeBrush);
                    AssertSeparator(resourceItem, resourceBrush);

                    host.Children.Remove(breadcrumbs);
                    await CompleteLayoutAsync(host);
                    Assert.IsFalse(automatic.IsLoaded);
                    breadcrumbs.SeparatorBrush = parentBrush;
                    host.Children.Add(breadcrumbs);
                    await CompleteLayoutAsync(host, breadcrumbs, first, automatic, resourceItem, body);
                    AssertSeparator(automatic, parentBrush);
                    Assert.AreSame(root, automatic.Content);
                    Assert.AreSame(separator, Separator(automatic));
                    Assert.AreSame(body, ((ContentPresenter)((Grid)automatic.Content).Children[3]).Content);
                    Assert.AreEqual("Retained edit", body.Text);
                    Assert.AreEqual(Visibility.Collapsed, Separator(first).Visibility);
                    Assert.AreEqual(1.0, separator.Opacity);
                    breadcrumbs.SeparatorOpacity = 0.25;
                    Assert.AreEqual(0.25, separator.Opacity);
                    AssertSeparator(automatic, parentBrush);
                    Assert.IsNull(automatic.SeparatorBrush);
                    Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyBreadcrumbItem.SeparatorBrushProperty));
                }
                finally
                {
                    if (hadBaseToken)
                        resources[baseToken] = originalBaseToken!;
                    else
                        resources.Remove(baseToken);
                }
            });

        [TestMethod]
        public Task When_SeparatorBrushesAreAuthored_LocalStylesAndBindingsKeepPrecedence() =>
            WithHostAsync(static async host =>
            {
                var parentBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue);
                var parentStyleBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
                var nextParentBrush = new SolidColorBrush(Microsoft.UI.Colors.Purple);
                var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                var itemStyleBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                var boundBrush = new SolidColorBrush(Microsoft.UI.Colors.Gold);
                var nextBoundBrush = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                var automatic = CreateItem("Automatic");
                var local = CreateItem("Local");
                local.SeparatorBrush = transparent;
                var styled = CreateItem("Styled");
                styled.Style = BrushStyle(typeof(DaisyBreadcrumbItem), DaisyBreadcrumbItem.SeparatorBrushProperty, itemStyleBrush);
                var bound = CreateItem("Bound");
                var itemSource = new ContentControl { Tag = boundBrush };
                bound.SetBinding(DaisyBreadcrumbItem.SeparatorBrushProperty, TagBinding(itemSource));
                var breadcrumbs = CreateBreadcrumbs(CreateItem("First"), automatic, local, styled, bound);
                breadcrumbs.Style = BrushStyle(typeof(DaisyBreadcrumbs), DaisyBreadcrumbs.SeparatorBrushProperty, parentStyleBrush);
                var parentSource = new ContentControl { Tag = parentBrush };
                breadcrumbs.SetBinding(DaisyBreadcrumbs.SeparatorBrushProperty, TagBinding(parentSource));
                host.Children.Add(breadcrumbs);
                await CompleteLayoutAsync(host, breadcrumbs, automatic, local, styled, bound);
                AssertSeparator(automatic, parentBrush);
                AssertSeparator(local, transparent);
                AssertSeparator(styled, itemStyleBrush);
                AssertSeparator(bound, boundBrush);

                parentSource.Tag = nextParentBrush;
                NotifyThemeChanged();
                await CompleteLayoutAsync(host);
                AssertSeparator(automatic, nextParentBrush);
                AssertSeparator(local, transparent);
                AssertSeparator(styled, itemStyleBrush);
                AssertSeparator(bound, boundBrush);
                Assert.AreEqual(DependencyProperty.UnsetValue, styled.ReadLocalValue(DaisyBreadcrumbItem.SeparatorBrushProperty));

                parentSource.Tag = null;
                await CompleteLayoutAsync(host);
                Assert.IsNull(breadcrumbs.SeparatorBrush);
                AssertSeparator(automatic, DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush"));
                parentSource.Tag = nextParentBrush;
                await CompleteLayoutAsync(host);
                AssertSeparator(automatic, nextParentBrush);

                host.Children.Remove(breadcrumbs);
                await CompleteLayoutAsync(host);
                Assert.IsFalse(bound.IsLoaded);
                parentSource.Tag = parentBrush;
                itemSource.Tag = nextBoundBrush;
                host.Children.Add(breadcrumbs);
                await CompleteLayoutAsync(host, breadcrumbs, automatic, local, styled, bound);
                AssertSeparator(automatic, parentBrush);
                AssertSeparator(local, transparent);
                AssertSeparator(styled, itemStyleBrush);
                AssertSeparator(bound, nextBoundBrush);

                itemSource.Tag = null;
                await CompleteLayoutAsync(host);
                Assert.IsNull(bound.SeparatorBrush);
                AssertSeparator(bound, parentBrush);
                itemSource.Tag = boundBrush;
                await CompleteLayoutAsync(host);
                AssertSeparator(bound, boundBrush);
                Assert.IsNotNull(bound.GetBindingExpression(DaisyBreadcrumbItem.SeparatorBrushProperty));
                local.SeparatorBrush = null;
                styled.SeparatorBrush = nextBoundBrush;
                await CompleteLayoutAsync(host);
                AssertSeparator(local, parentBrush);
                AssertSeparator(styled, nextBoundBrush);
                local.ClearValue(DaisyBreadcrumbItem.SeparatorBrushProperty);
                styled.ClearValue(DaisyBreadcrumbItem.SeparatorBrushProperty);
                await CompleteLayoutAsync(host);
                AssertSeparator(styled, itemStyleBrush);
                styled.ClearValue(FrameworkElement.StyleProperty);
                bound.ClearValue(DaisyBreadcrumbItem.SeparatorBrushProperty);
                breadcrumbs.ClearValue(DaisyBreadcrumbs.SeparatorBrushProperty);
                await CompleteLayoutAsync(host);
                AssertSeparator(automatic, parentStyleBrush);
                AssertSeparator(local, parentStyleBrush);
                AssertSeparator(styled, parentStyleBrush);
                AssertSeparator(bound, parentStyleBrush);
                breadcrumbs.ClearValue(FrameworkElement.StyleProperty);
                await CompleteLayoutAsync(host);
                Assert.IsNull(breadcrumbs.SeparatorBrush);
                AssertSeparator(automatic, DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush"));

                var lateItem = CreateItem("Late");
                var late = CreateBreadcrumbs(CreateItem("First"), lateItem);
                late.SeparatorBrush = nextParentBrush;
                host.Children.Add(late);
                await CompleteLayoutAsync(host, late, lateItem);
                AssertSeparator(lateItem, nextParentBrush);
            });

        private static DaisyBreadcrumbItem CreateItem(object content) => new()
        {
            Content = content,
            NeumorphicEnabled = false
        };

        private static DaisyBreadcrumbs CreateBreadcrumbs(params DaisyBreadcrumbItem[] items)
        {
            var panel = new StackPanel();
            foreach (var item in items)
                panel.Children.Add(item);
            return new DaisyBreadcrumbs { Content = panel, NeumorphicEnabled = false };
        }

        private static TextBlock Separator(DaisyBreadcrumbItem item) => (TextBlock)((Grid)item.Content).Children[0];

        private static void AssertSeparator(DaisyBreadcrumbItem item, Brush expected)
        {
            Assert.IsTrue(item.IsLoaded);
            var actual = Separator(item).Foreground;
            Assert.IsInstanceOfType<SolidColorBrush>(expected);
            Assert.IsInstanceOfType<SolidColorBrush>(actual);
            Assert.AreEqual(((SolidColorBrush)expected).Color, ((SolidColorBrush)actual).Color);
        }

        private static Style BrushStyle(Type target, DependencyProperty property, Brush brush)
        {
            var basis = new Style(target);
            basis.Setters.Add(new Setter(property, brush));
            return new Style(target) { BasedOn = basis };
        }

        private static Binding TagBinding(ContentControl source) => new()
        {
            Source = source,
            Path = new PropertyPath(nameof(ContentControl.Tag)),
            Mode = BindingMode.OneWay
        };

        private static void NotifyThemeChanged() => DaisyThemeManager.SetCurrentTheme(
            (DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName) + "-breadcrumb-brush-test");

        private static async Task WithHostAsync(Func<StackPanel, Task> test)
        {
            var originalTheme = DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName;
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
                try
                {
                    await CompleteLayoutAsync(panel);
                }
                finally
                {
                    DaisyThemeManager.SetCurrentTheme(originalTheme);
                }
            }
        }

    }
}

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
    public class Given_ThemeControllerSizing
    {
        [TestMethod]
        public Task When_CheckboxSizeChanges_AuthoredValuesKeepPrecedence() =>
            WithHostAsync(static async host =>
            {
                var automatic = new DaisyThemeController { Mode = ThemeControllerMode.Checkbox };
                var local = new DaisyThemeController { Mode = ThemeControllerMode.Checkbox, Size = DaisySize.Medium };
                var basis = new Style(typeof(DaisyThemeController));
                basis.Setters.Add(new Setter(DaisyThemeController.SizeProperty, DaisySize.Medium));
                var style = new Style(typeof(DaisyThemeController)) { BasedOn = basis };
                var styled = new DaisyThemeController { Mode = ThemeControllerMode.Checkbox, Style = style };
                var bound = new DaisyThemeController { Mode = ThemeControllerMode.Checkbox, Style = style };
                var source = new ContentControl { Tag = DaisySize.Medium };
                bound.SetBinding(DaisyThemeController.SizeProperty, TagBinding(source));
                host.Children.Add(automatic);
                host.Children.Add(local);
                host.Children.Add(styled);
                host.Children.Add(bound);
                await CompleteLayoutAsync(host, automatic, local, styled, bound);

                foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge, DaisySize.Small })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host);
                    AssertCheckbox(automatic, size);
                    AssertCheckbox(local, DaisySize.Medium);
                    AssertCheckbox(styled, DaisySize.Medium);
                    AssertCheckbox(bound, DaisySize.Medium);
                }
                Assert.AreEqual(DaisySize.Auto, automatic.Size);
                Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyThemeController.SizeProperty));
                Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(FrameworkElement.MinWidthProperty));
                Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(FrameworkElement.MinHeightProperty));
                Assert.AreEqual(DependencyProperty.UnsetValue, styled.ReadLocalValue(DaisyThemeController.SizeProperty));

                source.Tag = DaisySize.Large;
                await CompleteLayoutAsync(host);
                AssertCheckbox(bound, DaisySize.Large);
                source.Tag = DaisySize.Auto;
                await CompleteLayoutAsync(host);
                AssertCheckbox(bound, DaisySize.Small);
                Assert.IsNotNull(bound.GetBindingExpression(DaisyThemeController.SizeProperty));
                bound.ClearValue(DaisyThemeController.SizeProperty);
                local.ClearValue(DaisyThemeController.SizeProperty);
                await CompleteLayoutAsync(host);
                AssertCheckbox(bound, DaisySize.Medium);
                AssertCheckbox(local, DaisySize.Small);
                FlowerySizeManager.SetIgnoreGlobalSize(automatic, true);
                await CompleteLayoutAsync(host);
                AssertCheckbox(automatic, DaisySize.Medium);
                FlowerySizeManager.SetIgnoreGlobalSize(automatic, false);
                await CompleteLayoutAsync(host);
                AssertCheckbox(automatic, DaisySize.Small);
            });

        [TestMethod]
        public Task When_ModeOrLifecycleChanges_CurrentSizeAndAuthoredFontRemainEffective() =>
            WithHostAsync(static async host =>
            {
                var originalTheme = DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName;
                var style = new Style(typeof(DaisyThemeController));
                style.Setters.Add(new Setter(Control.FontSizeProperty, 23d));
                var controller = new DaisyThemeController
                {
                    Mode = ThemeControllerMode.ToggleWithText,
                    UncheckedTheme = originalTheme,
                    Style = style,
                    Width = 160,
                    Height = 50,
                    MinWidth = 3,
                    MinHeight = 4
                };
                host.Children.Add(controller);
                await CompleteLayoutAsync(host, controller);
                AssertLabels(controller, 23);
                controller.FontSize = 14;
                await CompleteLayoutAsync(host);
                AssertLabels(controller, 14);
                var source = new ContentControl { Tag = 14d };
                controller.SetBinding(Control.FontSizeProperty, TagBinding(source));
                source.Tag = 19d;
                await CompleteLayoutAsync(host);
                AssertLabels(controller, 19);

                DaisyThemeManager.SetCurrentTheme(originalTheme + "-controller-sizing-test");
                Assert.IsTrue(controller.IsChecked == true);
                controller.Mode = ThemeControllerMode.Checkbox;
                await CompleteLayoutAsync(host);
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                await CompleteLayoutAsync(host);
                AssertCheckbox(controller, DaisySize.ExtraLarge);
                Assert.IsTrue(controller.IsChecked == true);
                var root = Part<Grid>(controller, "CheckboxRoot");
                var indicator = Part<Border>(controller, "CheckboxMode");

                host.Children.Remove(controller);
                await CompleteLayoutAsync(host);
                Assert.IsFalse(controller.IsLoaded);
                DaisyThemeManager.SetCurrentTheme(originalTheme);
                FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
                Assert.IsTrue(controller.IsChecked == true, "Unloaded controllers must unsubscribe from theme changes.");
                host.Children.Add(controller);
                await CompleteLayoutAsync(host, controller);
                AssertCheckbox(controller, DaisySize.ExtraSmall);
                Assert.IsFalse(controller.IsChecked == true);
                Assert.AreSame(root, Part<Grid>(controller, "CheckboxRoot"));
                Assert.AreSame(indicator, Part<Border>(controller, "CheckboxMode"));
                controller.Mode = ThemeControllerMode.ToggleWithText;
                await CompleteLayoutAsync(host);
                AssertLabels(controller, 19);
                Assert.IsNotNull(controller.GetBindingExpression(Control.FontSizeProperty));
                controller.ClearValue(Control.FontSizeProperty);
                await CompleteLayoutAsync(host);
                AssertLabels(controller, 23);
                Assert.AreEqual(160d, controller.Width);
                Assert.AreEqual(50d, controller.Height);
                Assert.AreEqual(3d, controller.MinWidth);
                Assert.AreEqual(4d, controller.MinHeight);
                Assert.AreEqual(DependencyProperty.UnsetValue, controller.ReadLocalValue(DaisyThemeController.SizeProperty));
            });

        private static Binding TagBinding(ContentControl source) => new()
        {
            Source = source,
            Path = new PropertyPath(nameof(ContentControl.Tag)),
            Mode = BindingMode.OneWay
        };

        private static void AssertCheckbox(DaisyThemeController controller, DaisySize size)
        {
            Assert.IsTrue(controller.IsLoaded);
            Assert.AreEqual(size, controller.ResolvedSize);
            var key = DaisyResourceLookup.GetSizeKeyFull(size);
            var root = Part<Grid>(controller, "CheckboxRoot");
            var indicator = Part<Border>(controller, "CheckboxMode");
            var checkmark = Part<Microsoft.UI.Xaml.Shapes.Path>(controller, "CheckMark");
            var hitSize = DaisyResourceLookup.GetDouble($"DaisySize{key}Height", double.NaN);
            Assert.AreEqual(hitSize, root.MinWidth, 0.001);
            Assert.AreEqual(hitSize, root.MinHeight, 0.001);
            Assert.AreEqual(DaisyResourceLookup.GetDouble($"DaisyCheckbox{key}Size", double.NaN), indicator.Width, 0.001);
            Assert.AreEqual(indicator.Width, indicator.Height, 0.001);
            Assert.AreEqual(DaisyResourceLookup.GetDouble($"DaisyCheckmark{key}Size", double.NaN), checkmark.Width, 0.001);
            Assert.AreEqual(checkmark.Width, checkmark.Height, 0.001);
        }

        private static void AssertLabels(DaisyThemeController controller, double fontSize)
        {
            var root = (StackPanel)VisualTreeHelper.GetChild(controller, 0);
            Assert.AreEqual(fontSize, ((TextBlock)root.Children[0]).FontSize, 0.001);
            Assert.AreEqual(fontSize, ((TextBlock)root.Children[2]).FontSize, 0.001);
        }

        private static T Part<T>(DependencyObject parent, string name) where T : FrameworkElement =>
            FindPart<T>(parent, name) ?? throw new AssertFailedException($"Template part '{name}' was not found.");

        private static T? FindPart<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent is T element && element.Name == name)
                return element;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                if (FindPart<T>(VisualTreeHelper.GetChild(parent, index), name) is { } found)
                    return found;
            }
            return null;
        }

        private static async Task WithHostAsync(Func<StackPanel, Task> test)
        {
            var originalSize = FlowerySizeManager.CurrentSize;
            var originalUseGlobal = FlowerySizeManager.UseGlobalSizeByDefault;
            var originalAutoSize = FlowerySizeManager.EnableGlobalAutoSize;
            var originalTheme = DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName;
            var panel = GetHostPanel();
            var host = new StackPanel();
            try
            {
                panel.Children.Clear();
                FlowerySizeManager.UseGlobalSizeByDefault = true;
                FlowerySizeManager.EnableGlobalAutoSize = true;
                FlowerySizeManager.ApplySize(DaisySize.Small);
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
                    FlowerySizeManager.UseGlobalSizeByDefault = originalUseGlobal;
                    FlowerySizeManager.EnableGlobalAutoSize = originalAutoSize;
                    FlowerySizeManager.ApplySize(originalSize);
                    DaisyThemeManager.SetCurrentTheme(originalTheme);
                }
            }
        }
    }
}

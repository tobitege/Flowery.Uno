using System;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Flowery.Uno.RuntimeTests.RuntimeTestHelpers;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_NumberFlowBrushes
    {
        [TestMethod]
        public Task When_DigitForegroundChanges_AutomaticPaletteAndExistingBoxesFollowTheTheme() =>
            WithHostAsync(static async host =>
            {
                var resources = Application.Current.Resources;
                var keys = new[] { "DaisyBaseContentBrush", "DaisyNeutralBrush", "DaisyNeutralContentBrush" };
                var originals = keys.ToDictionary(static key => key,
                    key => (Present: resources.ContainsKey(key), Value: resources.ContainsKey(key) ? resources[key] : null));
                var baseBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue);
                var neutral = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 33, 55));
                var neutralContent = new SolidColorBrush(Microsoft.UI.Colors.Gold);
                try
                {
                    resources[keys[0]] = baseBrush;
                    resources[keys[1]] = neutral;
                    resources[keys[2]] = neutralContent;
                    NotifyThemeChanged();
                    var plain = CreateFlow();
                    var boxed = CreateFlow();
                    boxed.ShowDigitBoxes = true;
                    host.Children.Add(plain);
                    host.Children.Add(boxed);
                    await CompleteLayoutAsync(host, plain, boxed);
                    AssertParts(plain, baseBrush);
                    AssertParts(boxed, neutralContent);
                    Assert.IsNull(boxed.DigitForeground);
                    Assert.AreEqual(DependencyProperty.UnsetValue, boxed.ReadLocalValue(DaisyNumberFlow.DigitForegroundProperty));
                    var box = DigitBox(boxed);
                    var firstPart = Parts(boxed).Children[0];
                    var explicitBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    boxed.DigitForeground = explicitBrush;
                    await CompleteLayoutAsync(host);
                    AssertParts(boxed, explicitBrush);
                    AssertSurroundingBrushes(boxed, baseBrush);

                    var nextNeutral = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 23, 45, 67));
                    var nextContent = new SolidColorBrush(Microsoft.UI.Colors.Green);
                    var nextBase = new SolidColorBrush(Microsoft.UI.Colors.Purple);
                    resources[keys[0]] = nextBase;
                    resources[keys[1]] = nextNeutral;
                    resources[keys[2]] = nextContent;
                    NotifyThemeChanged();
                    await CompleteLayoutAsync(host);
                    Assert.AreSame(box, DigitBox(boxed));
                    Assert.AreSame(firstPart, Parts(boxed).Children[0]);
                    AssertBrush(nextNeutral, box.Background);
                    AssertParts(boxed, explicitBrush);
                    AssertParts(plain, nextBase);
                    AssertSurroundingBrushes(boxed, nextBase);
                    boxed.DigitForeground = null;
                    await CompleteLayoutAsync(host);
                    AssertParts(boxed, nextContent);
                    boxed.ShowDigitBoxes = false;
                    await CompleteLayoutAsync(host);
                    AssertParts(boxed, nextBase);
                    boxed.ShowDigitBoxes = true;
                    boxed.FormatString = "F2";
                    await CompleteLayoutAsync(host);
                    Assert.AreNotSame(firstPart, Parts(boxed).Children[0]);
                    AssertParts(boxed, nextContent);
                }
                finally
                {
                    foreach (var (key, original) in originals)
                    {
                        if (original.Present)
                            resources[key] = original.Value!;
                        else
                            resources.Remove(key);
                    }
                }
            });

        [TestMethod]
        public Task When_DigitForegroundIsAuthored_StylesBindingsAndNullSurviveReload() =>
            WithHostAsync(static async host =>
            {
                var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                var styleBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
                var boundBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                var nextBrush = new SolidColorBrush(Microsoft.UI.Colors.Gold);
                var local = CreateFlow();
                local.DigitForeground = transparent;
                var styled = CreateFlow();
                var basis = new Style(typeof(DaisyNumberFlow));
                basis.Setters.Add(new Setter(DaisyNumberFlow.DigitForegroundProperty, styleBrush));
                styled.Style = new Style(typeof(DaisyNumberFlow)) { BasedOn = basis };
                var bound = CreateFlow();
                var source = new ContentControl { Tag = boundBrush };
                bound.SetBinding(DaisyNumberFlow.DigitForegroundProperty, new Binding
                {
                    Source = source,
                    Path = new PropertyPath(nameof(ContentControl.Tag)),
                    Mode = BindingMode.OneWay
                });
                host.Children.Add(local);
                host.Children.Add(styled);
                host.Children.Add(bound);
                await CompleteLayoutAsync(host, local, styled, bound);
                AssertParts(local, transparent);
                AssertParts(styled, styleBrush);
                AssertParts(bound, boundBrush);
                source.Tag = nextBrush;
                NotifyThemeChanged();
                await CompleteLayoutAsync(host);
                AssertParts(local, transparent);
                AssertParts(styled, styleBrush);
                AssertParts(bound, nextBrush);
                Assert.AreEqual(DependencyProperty.UnsetValue, styled.ReadLocalValue(DaisyNumberFlow.DigitForegroundProperty));

                host.Children.Clear();
                await CompleteLayoutAsync(host);
                Assert.IsFalse(bound.IsLoaded);
                source.Tag = boundBrush;
                host.Children.Add(local);
                host.Children.Add(styled);
                host.Children.Add(bound);
                await CompleteLayoutAsync(host, local, styled, bound);
                AssertParts(local, transparent);
                AssertParts(styled, styleBrush);
                AssertParts(bound, boundBrush);
                source.Tag = null;
                await CompleteLayoutAsync(host);
                Assert.IsNull(bound.DigitForeground);
                AssertParts(bound, DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"));
                source.Tag = nextBrush;
                await CompleteLayoutAsync(host);
                AssertParts(bound, nextBrush);
                Assert.IsNotNull(bound.GetBindingExpression(DaisyNumberFlow.DigitForegroundProperty));
                styled.DigitForeground = transparent;
                await CompleteLayoutAsync(host);
                AssertParts(styled, transparent);
                styled.ClearValue(DaisyNumberFlow.DigitForegroundProperty);
                await CompleteLayoutAsync(host);
                AssertParts(styled, styleBrush);
                styled.ClearValue(FrameworkElement.StyleProperty);
                local.ClearValue(DaisyNumberFlow.DigitForegroundProperty);
                bound.ClearValue(DaisyNumberFlow.DigitForegroundProperty);
                await CompleteLayoutAsync(host);
                foreach (var control in new[] { local, styled, bound })
                    AssertParts(control, DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"));
            });

        private static DaisyNumberFlow CreateFlow() => new()
        {
            Value = 12.5m,
            FormatString = "F1",
            Prefix = "$",
            Suffix = " USD",
            ShowControls = true,
            Duration = TimeSpan.Zero,
            NeumorphicEnabled = false
        };

        private static StackPanel Row(DaisyNumberFlow control) => (StackPanel)((Border)control.Content).Child;
        private static StackPanel Parts(DaisyNumberFlow control) => (StackPanel)Row(control).Children[1];
        private static Border DigitBox(DaisyNumberFlow control) => (Border)((Grid)((Border)Parts(control).Children[0]).Child).Children[0];

        private static void AssertParts(DaisyNumberFlow control, Brush expected)
        {
            Assert.IsTrue(control.IsLoaded);
            Assert.IsTrue(Parts(control).Children.Count > 0);
            foreach (Border part in Parts(control).Children)
                foreach (TextBlock text in ((Grid)((Grid)part.Child).Children[2]).Children)
                    AssertBrush(expected, text.Foreground);
        }

        private static void AssertSurroundingBrushes(DaisyNumberFlow control, Brush expected)
        {
            var row = Row(control);
            AssertBrush(expected, ((TextBlock)row.Children[0]).Foreground);
            AssertBrush(expected, ((TextBlock)row.Children[2]).Foreground);
            foreach (RepeatButton button in ((StackPanel)row.Children[3]).Children)
                AssertBrush(expected, ((Microsoft.UI.Xaml.Shapes.Path)((Viewbox)button.Content).Child).Stroke);
        }

        private static void AssertBrush(Brush expected, Brush actual)
        {
            Assert.IsInstanceOfType<SolidColorBrush>(expected);
            Assert.IsInstanceOfType<SolidColorBrush>(actual);
            Assert.AreEqual(((SolidColorBrush)expected).Color, ((SolidColorBrush)actual).Color);
        }

        private static void NotifyThemeChanged() => DaisyThemeManager.SetCurrentTheme(
            (DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName) + "-numberflow-brush-test");

        private static async Task WithHostAsync(Func<StackPanel, Task> test)
        {
            var originalTheme = DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName;
            var panel = GetHostPanel();
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
                try { await CompleteLayoutAsync(panel); }
                finally { DaisyThemeManager.SetCurrentTheme(originalTheme); }
            }
        }
    }
}

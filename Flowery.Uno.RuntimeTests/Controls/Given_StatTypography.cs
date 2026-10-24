using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_StatTypography
    {
        [TestMethod]
        public Task When_SizeThemeOrLifecycleChanges_AutomaticTypographyUpdatesExistingText() =>
            WithSizeScopeAsync(static async host =>
            {
                var stat = CreateStat();
                var figure = new TextBlock { Text = "Figure" };
                var actions = new Button { Content = "Action" };
                stat.Figure = figure;
                stat.Actions = actions;
                host.Children.Add(stat);
                await CompleteLayoutAsync(host, stat);
                var root = stat.Content;
                var title = Text(stat, 0);
                var value = Text(stat, 1);
                var description = Text(stat, 2);

                foreach (var (size, valueSize, labelSize) in new[]
                {
                    (DaisySize.ExtraSmall, 18d, 9d),
                    (DaisySize.Small, 20d, 10d),
                    (DaisySize.Medium, 24d, 12d),
                    (DaisySize.Large, 28d, 14d),
                    (DaisySize.ExtraLarge, 32d, 16d)
                })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host);
                    AssertTypography(stat, valueSize, labelSize);
                    Assert.AreSame(root, stat.Content);
                    Assert.AreSame(title, Text(stat, 0));
                    Assert.AreSame(value, Text(stat, 1));
                    Assert.AreSame(description, Text(stat, 2));
                    Assert.IsTrue(double.IsNaN(stat.ValueFontSize));
                    Assert.IsTrue(double.IsNaN(stat.LabelFontSize));
                    Assert.AreEqual(DependencyProperty.UnsetValue, stat.ReadLocalValue(DaisyStat.ValueFontSizeProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, stat.ReadLocalValue(DaisyStat.LabelFontSizeProperty));
                }

                stat.Size = DaisySize.Medium;
                FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
                await CompleteLayoutAsync(host);
                AssertTypography(stat, 24, 12);

                var resources = Application.Current.Resources;
                const string token = "DaisyStatMediumValueFontSize";
                var hadToken = resources.ContainsKey(token);
                var originalToken = hadToken ? resources[token] : null;
                try
                {
                    resources[token] = 37d;
                    var theme = DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName;
                    DaisyThemeManager.SetCurrentTheme(theme + "-stat-typography-test");
                    await CompleteLayoutAsync(host);
                    AssertTypography(stat, 37, 12);

                    host.Children.Remove(stat);
                    await CompleteLayoutAsync(host);
                    Assert.IsFalse(stat.IsLoaded);
                    FlowerySizeManager.ApplySize(DaisySize.Large);
                    host.Children.Add(stat);
                    await CompleteLayoutAsync(host, stat);
                    AssertTypography(stat, 37, 12);
                    Assert.AreSame(root, stat.Content);
                    Assert.AreSame(title, Text(stat, 0));
                    Assert.AreSame(value, Text(stat, 1));
                    Assert.AreSame(description, Text(stat, 2));
                    Assert.AreEqual("Title", title.Text);
                    Assert.AreEqual("42", value.Text);
                    Assert.AreEqual("Description", description.Text);
                    var grid = (Grid)stat.Content;
                    Assert.AreSame(figure, ((ContentPresenter)grid.Children[1]).Content);
                    Assert.AreSame(actions, ((ContentPresenter)((StackPanel)grid.Children[0]).Children[3]).Content);
                }
                finally
                {
                    if (hadToken)
                        resources[token] = originalToken!;
                    else
                        resources.Remove(token);
                }
            });

        [TestMethod]
        public Task When_FontOverridesAreAuthored_LocalStylesAndBindingsSurviveAndClearingRestoresDefaults() =>
            WithSizeScopeAsync(static async host =>
            {
                var local = CreateStat();
                local.ValueFontSize = 24;
                local.LabelFontSize = 12;
                var bound = CreateStat();
                var valueSource = new ContentControl { Tag = 24d };
                var labelSource = new ContentControl { Tag = 12d };
                bound.SetBinding(DaisyStat.ValueFontSizeProperty, TagBinding(valueSource));
                bound.SetBinding(DaisyStat.LabelFontSizeProperty, TagBinding(labelSource));
                host.Children.Add(local);
                host.Children.Add(bound);

                var styledHost = (StackPanel)XamlReader.Load("""
                    <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:controls="using:Flowery.Controls">
                        <StackPanel.Resources>
                            <Style x:Key="BaseTypography" TargetType="controls:DaisyStat">
                                <Setter Property="ValueFontSize" Value="36" />
                                <Setter Property="LabelFontSize" Value="15" />
                            </Style>
                            <Style x:Key="BasedOnTypography" TargetType="controls:DaisyStat" BasedOn="{StaticResource BaseTypography}" />
                            <Style TargetType="controls:DaisyStat" BasedOn="{StaticResource BaseTypography}">
                                <Setter Property="ValueFontSize" Value="38" />
                                <Setter Property="LabelFontSize" Value="17" />
                            </Style>
                        </StackPanel.Resources>
                        <controls:DaisyStat x:Name="Explicit" Title="Title" Value="42" Description="Description" Style="{StaticResource BasedOnTypography}" />
                        <controls:DaisyStat x:Name="Implicit" Title="Title" Value="42" Description="Description" />
                    </StackPanel>
                    """);
                host.Children.Add(styledHost);
                var styled = (DaisyStat)styledHost.FindName("Explicit");
                var implicitStat = (DaisyStat)styledHost.FindName("Implicit");
                foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, local, bound, styled, implicitStat);
                    AssertTypography(local, 24, 12);
                    AssertTypography(bound, 24, 12);
                    AssertTypography(styled, 36, 15);
                    AssertTypography(implicitStat, 38, 17);
                    Assert.AreEqual(24d, local.ValueFontSize);
                    Assert.AreEqual(12d, local.LabelFontSize);
                    Assert.AreEqual(DependencyProperty.UnsetValue, styled.ReadLocalValue(DaisyStat.ValueFontSizeProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, implicitStat.ReadLocalValue(DaisyStat.LabelFontSizeProperty));
                }

                valueSource.Tag = 41d;
                labelSource.Tag = 19d;
                await CompleteLayoutAsync(host);
                AssertTypography(bound, 41, 19);
                var theme = DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName;
                DaisyThemeManager.SetCurrentTheme(theme + "-stat-overrides-test");
                await CompleteLayoutAsync(host);
                AssertTypography(local, 24, 12);
                AssertTypography(bound, 41, 19);
                AssertTypography(styled, 36, 15);
                AssertTypography(implicitStat, 38, 17);

                valueSource.Tag = double.NaN;
                labelSource.Tag = double.NaN;
                await CompleteLayoutAsync(host);
                AssertTypography(bound, 32, 16);
                valueSource.Tag = 42d;
                labelSource.Tag = 18d;
                await CompleteLayoutAsync(host);
                AssertTypography(bound, 42, 18);

                styled.ValueFontSize = 44;
                styled.LabelFontSize = 22;
                implicitStat.ValueFontSize = 46;
                implicitStat.LabelFontSize = 23;
                await CompleteLayoutAsync(host);
                AssertTypography(styled, 44, 22);
                AssertTypography(implicitStat, 46, 23);
                foreach (var stat in new[] { local, bound, styled, implicitStat })
                {
                    stat.ClearValue(DaisyStat.ValueFontSizeProperty);
                    stat.ClearValue(DaisyStat.LabelFontSizeProperty);
                }
                await CompleteLayoutAsync(host);
                AssertTypography(local, 32, 16);
                AssertTypography(bound, 32, 16);
                AssertTypography(styled, 36, 15);
                AssertTypography(implicitStat, 38, 17);
            });

        private static DaisyStat CreateStat() => new()
        {
            Title = "Title", Value = "42", Description = "Description", NeumorphicEnabled = false
        };

        private static TextBlock Text(DaisyStat stat, int index) =>
            (TextBlock)((StackPanel)((Grid)stat.Content).Children[0]).Children[index];

        private static void AssertTypography(DaisyStat stat, double valueFontSize, double labelFontSize)
        {
            Assert.AreEqual(labelFontSize, Text(stat, 0).FontSize, 0.001);
            Assert.AreEqual(valueFontSize, Text(stat, 1).FontSize, 0.001);
            Assert.AreEqual(labelFontSize, Text(stat, 2).FontSize, 0.001);
        }

        private static Binding TagBinding(ContentControl source) => new()
        {
            Source = source, Path = new PropertyPath(nameof(ContentControl.Tag)), Mode = BindingMode.OneWay
        };

        private static async Task WithSizeScopeAsync(Func<StackPanel, Task> test)
        {
            var originalSize = FlowerySizeManager.CurrentSize;
            var originalUseGlobal = FlowerySizeManager.UseGlobalSizeByDefault;
            var originalAutoSize = FlowerySizeManager.EnableGlobalAutoSize;
            var originalTheme = DaisyThemeManager.CurrentThemeName ?? DaisyThemeManager.BaseThemeName;
            var panel = RuntimeTestHelpers.GetHostPanel();
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
                    DaisyThemeManager.SetCurrentTheme(originalTheme);
                    FlowerySizeManager.UseGlobalSizeByDefault = originalUseGlobal;
                    FlowerySizeManager.EnableGlobalAutoSize = originalAutoSize;
                    FlowerySizeManager.ApplySize(originalSize);
                }
            }
        }

        private static async Task CompleteLayoutAsync(FrameworkElement element, params FrameworkElement[] requiredElements)
        {
            await RuntimeTestHelpers.EnsureLoadedAsync(element).WaitAsync(TimeSpan.FromSeconds(5));
            foreach (var required in requiredElements)
                await RuntimeTestHelpers.EnsureLoadedAsync(required).WaitAsync(TimeSpan.FromSeconds(5));
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    element.UpdateLayout();
                    completion.TrySetResult(null);
                }))
                throw new InvalidOperationException("The runtime dispatcher rejected stat typography verification.");
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}

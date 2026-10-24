using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_PaddingOwnership
    {
        [TestMethod]
        public Task When_PaddingIsAutomatic_SizeAndVariantChangesReachTheTemplates() =>
            WithSizeScopeAsync(static async host =>
            {
                var (button, card) = AddPair(host);
                var compact = new DaisyCard { Variant = DaisyCardVariant.Compact, Content = "Compact", NeumorphicEnabled = false };
                var patterned = new DaisyPatternedCard { Content = "Patterned", NeumorphicEnabled = false };
                var expandable = new DaisyExpandableCard { Content = "Expandable", NeumorphicEnabled = false };
                host.Children.Add(compact);
                host.Children.Add(patterned);
                host.Children.Add(expandable);

                var outerPadding = new Thickness(3, 1, 5, 2);
                var groupedButton = new DaisyButton { Content = "Grouped", Padding = outerPadding, NeumorphicEnabled = false };
                var group = new DaisyButtonGroup
                {
                    Content = new StackPanel { Children = { groupedButton } },
                    NeumorphicEnabled = false
                };
                host.Children.Add(group);

                foreach (var (size, bodyPadding) in new[]
                {
                    (DaisySize.ExtraSmall, 16d),
                    (DaisySize.ExtraLarge, 48d),
                    (DaisySize.Small, 24d)
                })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, button, card, compact, patterned, expandable, group, groupedButton);
                    var expectedButtonPadding = ButtonPadding(size);
                    AssertRenderedButton(button, expectedButtonPadding);
                    AssertRenderedButton(groupedButton, expectedButtonPadding);
                    Assert.AreEqual(outerPadding, groupedButton.Padding);
                    Assert.AreEqual(outerPadding, FindPart<Border>(groupedButton, "ButtonBorder").Padding);
                    Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(Control.PaddingProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(DaisyButton.ContentPaddingProperty));

                    AssertRenderedCard(card, new Thickness(bodyPadding));
                    AssertRenderedCard(compact, new Thickness(bodyPadding / 2));
                    AssertRenderedCard(patterned, new Thickness(bodyPadding));
                    AssertRenderedCard(expandable, new Thickness(bodyPadding));
                    foreach (var automaticCard in new DaisyCard[] { card, compact, patterned, expandable })
                    {
                        Assert.IsNull(automaticCard.BodyPadding);
                        Assert.AreEqual(DependencyProperty.UnsetValue, automaticCard.ReadLocalValue(DaisyCard.BodyPaddingProperty));
                    }
                }

                button.Shape = DaisyButtonShape.Square;
                await CompleteLayoutAsync(host);
                AssertRenderedButton(button, new Thickness(0));
                button.Shape = DaisyButtonShape.Default;
                button.Variant = DaisyButtonVariant.Link;
                await CompleteLayoutAsync(host);
                AssertRenderedButton(button, new Thickness(0));
                button.Variant = DaisyButtonVariant.Primary;
                card.Variant = DaisyCardVariant.Compact;
                await CompleteLayoutAsync(host);
                AssertRenderedButton(button, ButtonPadding(DaisySize.Small));
                AssertRenderedCard(card, new Thickness(12));
                card.Variant = DaisyCardVariant.Normal;
                await CompleteLayoutAsync(host);
                AssertRenderedCard(card, new Thickness(24));
            });

        [TestMethod]
        public Task When_PaddingIsLocalOrBound_ZeroAndSourceUpdatesSurvive_AndClearingRestoresAutomatic() =>
            WithSizeScopeAsync(static async host =>
            {
                var (localButton, localCard) = AddPair(host);
                localButton.ContentPadding = new Thickness(0);
                localCard.BodyPadding = new Thickness(0);
                var (boundButton, boundCard) = AddPair(host);
                var source = new ContentControl { Tag = new Thickness(0) };
                boundButton.SetBinding(DaisyButton.ContentPaddingProperty, PaddingBinding(source));
                boundCard.SetBinding(DaisyCard.BodyPaddingProperty, PaddingBinding(source));

                foreach (var size in new[] { DaisySize.ExtraLarge, DaisySize.ExtraSmall })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, localButton, localCard, boundButton, boundCard);
                    AssertRenderedButton(localButton, new Thickness(0));
                    AssertRenderedCard(localCard, new Thickness(0));
                    AssertRenderedButton(boundButton, new Thickness(0));
                    AssertRenderedCard(boundCard, new Thickness(0));
                    Assert.AreEqual(new Thickness(0), localButton.ReadLocalValue(DaisyButton.ContentPaddingProperty));
                    Assert.AreEqual(new Thickness(0), localCard.ReadLocalValue(DaisyCard.BodyPaddingProperty));
                }

                var authoredPadding = new Thickness(7, 3, 11, 5);
                source.Tag = authoredPadding;
                boundButton.Shape = DaisyButtonShape.Square;
                boundButton.Variant = DaisyButtonVariant.Link;
                boundCard.Variant = DaisyCardVariant.Compact;
                await CompleteLayoutAsync(host);
                AssertRenderedButton(boundButton, authoredPadding);
                AssertRenderedCard(boundCard, authoredPadding);

                source.Tag = new Thickness(2);
                FlowerySizeManager.ApplySize(DaisySize.Large);
                await CompleteLayoutAsync(host);
                AssertRenderedButton(boundButton, new Thickness(2));
                AssertRenderedCard(boundCard, new Thickness(2));

                source.Tag = null;
                await CompleteLayoutAsync(host);
                AssertRenderedButton(boundButton, new Thickness(0));
                AssertRenderedCard(boundCard, new Thickness(20));
                source.Tag = authoredPadding;
                await CompleteLayoutAsync(host);
                AssertRenderedButton(boundButton, authoredPadding);
                AssertRenderedCard(boundCard, authoredPadding);

                localButton.ClearValue(DaisyButton.ContentPaddingProperty);
                localCard.ClearValue(DaisyCard.BodyPaddingProperty);
                boundButton.ClearValue(DaisyButton.ContentPaddingProperty);
                boundCard.ClearValue(DaisyCard.BodyPaddingProperty);
                boundButton.Shape = DaisyButtonShape.Default;
                boundButton.Variant = DaisyButtonVariant.Primary;
                boundCard.Variant = DaisyCardVariant.Normal;
                foreach (var (size, bodyPadding) in new[] { (DaisySize.Large, 40d), (DaisySize.ExtraSmall, 16d) })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, localButton, localCard, boundButton, boundCard);
                    AssertRenderedButton(localButton, ButtonPadding(size));
                    AssertRenderedButton(boundButton, ButtonPadding(size));
                    AssertRenderedCard(localCard, new Thickness(bodyPadding));
                    AssertRenderedCard(boundCard, new Thickness(bodyPadding));
                }
                Assert.IsNull(localButton.ContentPadding);
                Assert.IsNull(boundButton.ContentPadding);
                Assert.IsNull(localCard.BodyPadding);
                Assert.IsNull(boundCard.BodyPadding);
            });

        [TestMethod]
        public Task When_StylesSetZero_ExplicitBasedOnAndImplicitStylesKeepTheirPadding() =>
            WithSizeScopeAsync(static async host =>
            {
                var (button, card) = AddPair(host);
                button.Style = ZeroPaddingStyle(typeof(DaisyButton), DaisyButton.ContentPaddingProperty);
                card.Style = ZeroPaddingStyle(typeof(DaisyCard), DaisyCard.BodyPaddingProperty);
                var implicitHost = (StackPanel)XamlReader.Load("""
                    <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:controls="using:Flowery.Controls">
                        <StackPanel.Resources>
                            <Style TargetType="controls:DaisyButton">
                                <Setter Property="ContentPadding" Value="0" />
                            </Style>
                            <Style TargetType="controls:DaisyCard">
                                <Setter Property="BodyPadding" Value="0" />
                            </Style>
                        </StackPanel.Resources>
                        <controls:DaisyButton x:Name="ImplicitButton" Content="Implicit" />
                        <controls:DaisyCard x:Name="ImplicitCard" Content="Implicit" />
                    </StackPanel>
                    """);
                host.Children.Add(implicitHost);
                var implicitButton = (DaisyButton)implicitHost.FindName("ImplicitButton");
                var implicitCard = (DaisyCard)implicitHost.FindName("ImplicitCard");
                foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, button, card, implicitButton, implicitCard);
                    AssertRenderedButton(button, new Thickness(0));
                    AssertRenderedCard(card, new Thickness(0));
                    AssertRenderedButton(implicitButton, new Thickness(0));
                    AssertRenderedCard(implicitCard, new Thickness(0));
                    Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(DaisyButton.ContentPaddingProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, card.ReadLocalValue(DaisyCard.BodyPaddingProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, implicitButton.ReadLocalValue(DaisyButton.ContentPaddingProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, implicitCard.ReadLocalValue(DaisyCard.BodyPaddingProperty));
                }

                button.ClearValue(FrameworkElement.StyleProperty);
                card.ClearValue(FrameworkElement.StyleProperty);
                implicitButton.ContentPadding = null;
                implicitCard.BodyPadding = null;
                await CompleteLayoutAsync(host);
                AssertRenderedButton(button, ButtonPadding(DaisySize.ExtraLarge));
                AssertRenderedCard(card, new Thickness(48));
                AssertRenderedButton(implicitButton, ButtonPadding(DaisySize.ExtraLarge));
                AssertRenderedCard(implicitCard, new Thickness(48));

                implicitButton.ClearValue(DaisyButton.ContentPaddingProperty);
                implicitCard.ClearValue(DaisyCard.BodyPaddingProperty);
                await CompleteLayoutAsync(host);
                AssertRenderedButton(implicitButton, new Thickness(0));
                AssertRenderedCard(implicitCard, new Thickness(0));
            });

        [TestMethod]
        public Task When_ButtonMinWidthIsAuthored_SizeAndShapeChangesKeepIt() =>
            WithSizeScopeAsync(static async host =>
            {
                var authored = new DaisyButton { Content = "Authored", MinWidth = 100, NeumorphicEnabled = false };
                var style = new Style(typeof(DaisyButton));
                style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 120d));
                var styled = new DaisyButton { Content = "Styled", Style = style, NeumorphicEnabled = false };
                var wide = new DaisyButton { Content = "Wide", Shape = DaisyButtonShape.Wide, NeumorphicEnabled = false };
                host.Children.Add(authored);
                host.Children.Add(styled);
                host.Children.Add(wide);
                await CompleteLayoutAsync(host, authored, styled, wide);

                FlowerySizeManager.ApplySize(DaisySize.Large);
                await CompleteLayoutAsync(host);
                Assert.AreEqual(100d, authored.MinWidth);
                Assert.IsTrue(authored.ActualWidth >= 100, $"Authored MinWidth must reach layout: {authored.ActualWidth}.");
                Assert.AreEqual(120d, styled.MinWidth);
                Assert.AreEqual(DependencyProperty.UnsetValue, styled.ReadLocalValue(FrameworkElement.MinWidthProperty));
                Assert.AreEqual(200d, wide.MinWidth);

                authored.Shape = DaisyButtonShape.Wide;
                styled.Shape = DaisyButtonShape.Wide;
                Assert.AreEqual(100d, authored.MinWidth);
                Assert.AreEqual(120d, styled.MinWidth);
                wide.Shape = DaisyButtonShape.Default;
                Assert.AreEqual(DependencyProperty.UnsetValue, wide.ReadLocalValue(FrameworkElement.MinWidthProperty));
                Assert.AreEqual(0d, wide.MinWidth);
                wide.MinWidth = 50;
                wide.Shape = DaisyButtonShape.Wide;
                FlowerySizeManager.ApplySize(DaisySize.Small);
                Assert.AreEqual(50d, wide.MinWidth);
            });

        private static (DaisyButton Button, DaisyCard Card) AddPair(Panel host)
        {
            var button = new DaisyButton { Content = "Save", NeumorphicEnabled = false };
            var card = new DaisyCard { Content = "Body", NeumorphicEnabled = false };
            host.Children.Add(button);
            host.Children.Add(card);
            return (button, card);
        }

        private static Style ZeroPaddingStyle(Type targetType, DependencyProperty property)
        {
            var basis = new Style(targetType);
            basis.Setters.Add(new Setter(property, new Thickness(0)));
            return new Style(targetType) { BasedOn = basis };
        }

        private static Binding PaddingBinding(ContentControl source) => new()
        {
            Source = source,
            Path = new PropertyPath(nameof(ContentControl.Tag)),
            Mode = BindingMode.OneWay
        };

        private static Thickness ButtonPadding(DaisySize size) =>
            DaisyResourceLookup.GetSizeThickness(Application.Current.Resources, "DaisyButton", size, "Padding",
                DaisyResourceLookup.GetDefaultPadding(size));

        private static void AssertRenderedButton(DaisyButton button, Thickness expected)
        {
            Assert.AreEqual(expected, button.ResolvedContentPadding,
                $"Global={FlowerySizeManager.CurrentSize}; Size={button.Size}; ResolvedSize={button.ResolvedSize}; ContentPadding={button.ContentPadding}; Loaded={button.IsLoaded}; Binding={button.GetBindingExpression(DaisyButton.ContentPaddingProperty) != null}");
            Assert.AreEqual(expected, FindPart<ContentPresenter>(button, "ContentPresenter").Margin);
        }

        private static void AssertRenderedCard(DaisyCard card, Thickness expected)
        {
            Assert.AreEqual(expected, card.ResolvedBodyPadding);
            var renderedPadding = card is DaisyExpandableCard
                ? FindPart<Grid>(card, "PART_BodyLayout").Margin
                : FindPart<Border>(card, "PART_BuiltInLayout").Padding;
            Assert.AreEqual(expected, renderedPadding);
        }

        private static T FindPart<T>(DependencyObject root, string name) where T : FrameworkElement =>
            TryFindPart<T>(root, name) ?? throw new AssertFailedException($"Template part {name} was not found.");

        private static T? TryFindPart<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root is T element && element.Name == name)
                return element;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                var match = TryFindPart<T>(VisualTreeHelper.GetChild(root, index), name);
                if (match != null)
                    return match;
            }
            return null;
        }

        private static async Task WithSizeScopeAsync(Func<StackPanel, Task> test)
        {
            var originalSize = FlowerySizeManager.CurrentSize;
            var originalUseGlobal = FlowerySizeManager.UseGlobalSizeByDefault;
            var originalAutoSize = FlowerySizeManager.EnableGlobalAutoSize;
            var originalWindow = FlowerySizeManager.MainWindow;
            var panel = RuntimeTestHelpers.GetHostPanel();
            var host = new StackPanel();
            try
            {
                panel.Children.Clear();
                FlowerySizeManager.MainWindow = RuntimeTestContext.Window
                    ?? throw new InvalidOperationException("The runtime test window is not initialized.");
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
                    FlowerySizeManager.MainWindow = originalWindow;
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
                throw new InvalidOperationException("The runtime dispatcher rejected padding layout verification.");
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}

using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Enums;
using Flowery.Theming;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_FlowerySizeManager
    {
        [TestMethod]
        public void When_GetControlHeight_Called_ReturnsStandardHeights()
        {
            var expected = new (DaisySize Size, double Height)[]
            {
                (DaisySize.ExtraSmall, 24d),
                (DaisySize.Small, 28d),
                (DaisySize.Medium, 32d),
                (DaisySize.Large, 34d),
                (DaisySize.ExtraLarge, 36d)
            };

            foreach (var (size, height) in expected)
            {
                Assert.AreEqual(height, FlowerySizeManager.GetControlHeight(size), $"Height for {size}");
            }
        }

        [TestMethod]
        public void When_GetControlHeight_SizeUnknown_ReturnsMediumHeight()
        {
            Assert.AreEqual(32d, FlowerySizeManager.GetControlHeight((DaisySize)999));
        }

        [TestMethod]
        public Task When_GlobalSizeChangesRepeatedly_DefaultControlsFollowWithoutLocalValues() =>
            WithSizeScopeAsync(static async host =>
            {
                var controls = AddControls(host);
                var reference = AddControls(host, DaisySize.Medium);
                foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge, DaisySize.Small })
                {
                    SetSize(reference, size);
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, controls.Button, controls.Badge, reference.Button, reference.Badge);
                    AssertMatches(controls, reference, size);
                    AssertNoLocalSize(controls);
                }
            });

        [TestMethod]
        public Task When_SizeIsLocalBeforeOrAfterLoad_GlobalChangesPreserveIt() =>
            WithSizeScopeAsync(static async host =>
            {
                var beforeLoad = AddControls(host, DaisySize.Medium);
                var afterLoad = AddControls(host);
                var reference = AddControls(host, DaisySize.Medium);
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                await CompleteLayoutAsync(host, beforeLoad.Button, beforeLoad.Badge, afterLoad.Button, afterLoad.Badge, reference.Button, reference.Badge);
                SetSize(afterLoad, DaisySize.Medium);

                foreach (var globalSize in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge })
                {
                    FlowerySizeManager.ApplySize(globalSize);
                    await CompleteLayoutAsync(host);
                    AssertLocalSize(beforeLoad, DaisySize.Medium);
                    AssertLocalSize(afterLoad, DaisySize.Medium);
                    AssertMatches(beforeLoad, reference, DaisySize.Medium);
                    AssertMatches(afterLoad, reference, DaisySize.Medium);
                }

                FlowerySizeManager.ApplySize(DaisySize.Medium);
                FlowerySizeManager.ApplySize(DaisySize.Large);
                afterLoad.Button.ClearValue(DaisyButton.SizeProperty);
                afterLoad.Badge.ClearValue(DaisyBadge.SizeProperty);
                SetSize(reference, DaisySize.Large);
                await CompleteLayoutAsync(host);
                AssertMatches(afterLoad, reference, DaisySize.Large);
                AssertNoLocalSize(afterLoad);

                FlowerySizeManager.ApplySize(DaisySize.Medium);
                SetSize(reference, DaisySize.Medium);
                await CompleteLayoutAsync(host);
                AssertMatches(afterLoad, reference, DaisySize.Medium);
                AssertNoLocalSize(afterLoad);
            });

        [TestMethod]
        public Task When_StyleSetsMedium_BasedOnPreservesItAndUnspecifiedStyleFollowsGlobal() =>
            WithSizeScopeAsync(static async host =>
            {
                var styled = AddControls(host);
                styled.Button.Style = SizeStyle(typeof(DaisyButton), DaisyButton.SizeProperty);
                styled.Badge.Style = SizeStyle(typeof(DaisyBadge), DaisyBadge.SizeProperty);
                var unspecified = AddControls(host);
                unspecified.Button.Style = new Style(typeof(DaisyButton));
                unspecified.Badge.Style = new Style(typeof(DaisyBadge));
                var medium = AddControls(host, DaisySize.Medium);
                var global = AddControls(host, DaisySize.ExtraSmall);

                foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge })
                {
                    SetSize(global, size);
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, styled.Button, styled.Badge, unspecified.Button, unspecified.Badge, medium.Button, medium.Badge, global.Button, global.Badge);
                    Assert.AreEqual(DaisySize.Medium, styled.Button.Size);
                    Assert.AreEqual(DaisySize.Medium, styled.Badge.Size);
                    AssertNoLocalSize(styled);
                    AssertMatches(styled, medium, DaisySize.Medium);
                    AssertNoLocalSize(unspecified);
                    AssertMatches(unspecified, global, size);
                }

                styled.Button.ClearValue(FrameworkElement.StyleProperty);
                styled.Badge.ClearValue(FrameworkElement.StyleProperty);
                await CompleteLayoutAsync(host);
                AssertMatches(styled, global, DaisySize.ExtraLarge);
                FlowerySizeManager.ApplySize(DaisySize.Small);
                SetSize(global, DaisySize.Small);
                await CompleteLayoutAsync(host);
                AssertMatches(styled, global, DaisySize.Small);
                AssertNoLocalSize(styled);
            });

        [TestMethod]
        public Task When_SizeIsBound_GlobalChangesPreserveBindingAndSourceUpdates() =>
            WithSizeScopeAsync(static async host =>
            {
                var source = new ContentControl { Tag = DaisySize.Medium };
                var controls = AddControls(host);
                controls.Button.SetBinding(DaisyButton.SizeProperty, SizeBinding(source));
                controls.Badge.SetBinding(DaisyBadge.SizeProperty, SizeBinding(source));
                var reference = AddControls(host, DaisySize.Medium);

                foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge })
                {
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host, controls.Button, controls.Badge, reference.Button, reference.Badge);
                    Assert.AreEqual(DaisySize.Medium, controls.Button.Size);
                    Assert.AreEqual(DaisySize.Medium, controls.Badge.Size);
                    AssertMatches(controls, reference, DaisySize.Medium);
                }

                source.Tag = DaisySize.Large;
                SetSize(reference, DaisySize.Large);
                await CompleteLayoutAsync(host);
                FlowerySizeManager.ApplySize(DaisySize.Small);
                await CompleteLayoutAsync(host);
                Assert.AreEqual(DaisySize.Large, controls.Button.Size, "The button binding must remain active.");
                Assert.AreEqual(DaisySize.Large, controls.Badge.Size, "The badge binding must remain active.");
                AssertMatches(controls, reference, DaisySize.Large);
            });

        [TestMethod]
        public Task When_ControlsLoadLateAndReload_TheyUseCurrentSizeWithoutLocalValues() =>
            WithSizeScopeAsync(static async host =>
            {
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                var controls = AddControls(host);
                var reference = AddControls(host, DaisySize.ExtraLarge);
                var password = new DaisyPasswordBox { Password = "reload-value" };
                var passwordReference = new DaisyPasswordBox { Size = DaisySize.ExtraLarge, Password = "reload-value" };
                host.Children.Add(password);
                host.Children.Add(passwordReference);
                var text = new TextBlock { Text = "Responsive size" };
                FlowerySizeManager.SetResponsiveFont(text, ResponsiveFontTier.Primary);
                host.Children.Add(text);
                await CompleteLayoutAsync(host, controls.Button, controls.Badge, reference.Button, reference.Badge, password, passwordReference, text);
                AssertMatches(controls, reference, DaisySize.ExtraLarge);
                AssertPasswordMatches(password, passwordReference);
                AssertNoLocalSize(controls);
                Assert.AreEqual(FlowerySizeManager.GetFontSizeForTier(ResponsiveFontTier.Primary, DaisySize.ExtraLarge), text.FontSize);

                FlowerySizeManager.ApplySize(DaisySize.Large);
                SetSize(reference, DaisySize.Large);
                passwordReference.Size = DaisySize.Large;
                await CompleteLayoutAsync(host);
                AssertMatches(controls, reference, DaisySize.Large);
                AssertPasswordMatches(password, passwordReference);
                Assert.AreEqual(FlowerySizeManager.GetFontSizeForTier(ResponsiveFontTier.Primary, DaisySize.Large), text.FontSize);
                var detachedFontSize = text.FontSize;

                host.Children.Remove(controls.Button);
                host.Children.Remove(controls.Badge);
                host.Children.Remove(password);
                host.Children.Remove(text);
                await CompleteLayoutAsync(host);
                Assert.IsFalse(controls.Button.IsLoaded);
                Assert.IsFalse(controls.Badge.IsLoaded);
                Assert.IsFalse(password.IsLoaded);
                Assert.IsFalse(text.IsLoaded);
                FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
                Assert.AreEqual(detachedFontSize, text.FontSize, "Detached text must not retain a global size subscription.");
                SetSize(reference, DaisySize.ExtraSmall);
                passwordReference.Size = DaisySize.ExtraSmall;
                host.Children.Add(controls.Button);
                host.Children.Add(controls.Badge);
                host.Children.Add(password);
                host.Children.Add(text);
                await CompleteLayoutAsync(host, controls.Button, controls.Badge, password, text);
                AssertMatches(controls, reference, DaisySize.ExtraSmall);
                AssertPasswordMatches(password, passwordReference);
                AssertNoLocalSize(controls);
                Assert.AreEqual(FlowerySizeManager.GetFontSizeForTier(ResponsiveFontTier.Primary, DaisySize.ExtraSmall), text.FontSize);

                FlowerySizeManager.ApplySize(DaisySize.Small);
                SetSize(reference, DaisySize.Small);
                passwordReference.Size = DaisySize.Small;
                await CompleteLayoutAsync(host);
                AssertMatches(controls, reference, DaisySize.Small);
                AssertPasswordMatches(password, passwordReference);
                AssertNoLocalSize(controls);
                Assert.AreEqual(FlowerySizeManager.GetFontSizeForTier(ResponsiveFontTier.Primary, DaisySize.Small), text.FontSize);
            });

        [TestMethod]
        public Task When_AncestorIgnoresGlobalSize_ExplicitChildFalseOptsBackIn() =>
            WithSizeScopeAsync(static async host =>
            {
                var ignoredParent = new StackPanel();
                FlowerySizeManager.SetIgnoreGlobalSize(ignoredParent, true);
                host.Children.Add(ignoredParent);
                var inheritedIgnore = AddControls(ignoredParent);
                var optedIn = AddControls(ignoredParent);
                FlowerySizeManager.SetIgnoreGlobalSize(optedIn.Button, false);
                FlowerySizeManager.SetIgnoreGlobalSize(optedIn.Badge, false);
                var directIgnore = AddControls(host);
                FlowerySizeManager.SetIgnoreGlobalSize(directIgnore.Button, true);
                FlowerySizeManager.SetIgnoreGlobalSize(directIgnore.Badge, true);
                var reference = AddControls(host, DaisySize.Small);
                await CompleteLayoutAsync(host, inheritedIgnore.Button, inheritedIgnore.Badge, optedIn.Button, optedIn.Badge, directIgnore.Button, directIgnore.Badge, reference.Button, reference.Badge);
                var inheritedMetrics = Metrics(inheritedIgnore);
                var directMetrics = Metrics(directIgnore);

                foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.ExtraLarge })
                {
                    SetSize(reference, size);
                    FlowerySizeManager.ApplySize(size);
                    await CompleteLayoutAsync(host);
                    Assert.AreEqual(inheritedMetrics, Metrics(inheritedIgnore), "Ancestor opt-out must preserve rendered sizing.");
                    Assert.AreEqual(directMetrics, Metrics(directIgnore), "Control opt-out must preserve rendered sizing.");
                    AssertMatches(optedIn, reference, size);
                    AssertNoLocalSize(optedIn);
                }
            });

        [TestMethod]
        public Task When_AutomaticRefreshIsDisabled_ExplicitRefreshAndDefaultPolicyRemainIndependent() =>
            WithSizeScopeAsync(static async host =>
            {
                var controls = AddControls(host);
                await CompleteLayoutAsync(host, controls.Button, controls.Badge);
                var originalMetrics = Metrics(controls);
                FlowerySizeManager.EnableGlobalAutoSize = false;
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                await CompleteLayoutAsync(host);
                Assert.AreEqual(originalMetrics, Metrics(controls));

                var reference = AddControls(host, DaisySize.ExtraLarge);
                var late = AddControls(host);
                await CompleteLayoutAsync(host, reference.Button, reference.Badge, late.Button, late.Badge);
                AssertMatches(late, reference, DaisySize.ExtraLarge);
                FlowerySizeManager.RefreshAllSizes();
                await CompleteLayoutAsync(host);
                AssertMatches(controls, reference, DaisySize.ExtraLarge);

                FlowerySizeManager.UseGlobalSizeByDefault = false;
                SetSize(reference, DaisySize.Medium);
                await CompleteLayoutAsync(host);
                AssertMatches(controls, reference, DaisySize.Medium);
                AssertMatches(late, reference, DaisySize.Medium);
                FlowerySizeManager.EnableGlobalAutoSize = true;
                FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
                await CompleteLayoutAsync(host);
                AssertMatches(controls, reference, DaisySize.Medium);

                FlowerySizeManager.UseGlobalSizeByDefault = true;
                SetSize(reference, DaisySize.ExtraSmall);
                await CompleteLayoutAsync(host);
                AssertMatches(controls, reference, DaisySize.ExtraSmall);
                AssertMatches(late, reference, DaisySize.ExtraSmall);
                AssertNoLocalSize(controls);
                AssertNoLocalSize(late);
            });

        [TestMethod]
        public Task When_AvatarGroupSizeChanges_AutoChildrenFollowAndAuthoredChildrenRemainIndependent() =>
            WithSizeScopeAsync(static async host =>
            {
                var automatic = new DaisyAvatar { IsPlaceholder = true, Initials = "A" };
                var local = new DaisyAvatar { IsPlaceholder = true, Initials = "M", Size = DaisySize.Medium };
                var bound = new DaisyAvatar { IsPlaceholder = true, Initials = "B" };
                var source = new ContentControl { Tag = DaisySize.Medium };
                bound.SetBinding(DaisyAvatar.SizeProperty, SizeBinding(source));
                var content = new StackPanel();
                content.Children.Add(automatic);
                content.Children.Add(local);
                content.Children.Add(bound);
                var group = new DaisyAvatarGroup { Content = content, Size = DaisySize.Large };
                var reference = new DaisyAvatar { Size = DaisySize.Large };
                var localReference = new DaisyAvatar { Size = DaisySize.Medium };
                var boundReference = new DaisyAvatar { Size = DaisySize.Medium };
                host.Children.Add(group);
                host.Children.Add(reference);
                host.Children.Add(localReference);
                host.Children.Add(boundReference);

                var stages = new (DaisySize? Parent, DaisySize Global, DaisySize Bound)[]
                {
                    (DaisySize.Large, DaisySize.ExtraSmall, DaisySize.Medium),
                    (DaisySize.ExtraLarge, DaisySize.Small, DaisySize.Small),
                    (null, DaisySize.ExtraSmall, DaisySize.Small),
                    (null, DaisySize.Large, DaisySize.Small)
                };
                foreach (var stage in stages)
                {
                    if (stage.Parent is { } parentSize)
                        group.Size = parentSize;
                    else
                        group.ClearValue(DaisyAvatarGroup.SizeProperty);
                    source.Tag = stage.Bound;
                    boundReference.Size = stage.Bound;
                    reference.Size = stage.Parent ?? stage.Global;
                    FlowerySizeManager.ApplySize(stage.Global);
                    await CompleteLayoutAsync(host, group, automatic, local, bound, reference, localReference, boundReference);

                    Assert.IsTrue(group.IsLoaded && automatic.IsLoaded && local.IsLoaded && bound.IsLoaded, "The group and authored children must be loaded before checking their sizes.");
                    Assert.AreEqual(stage.Parent ?? stage.Global, automatic.ResolvedSize, "An automatic child must resolve the current parent default before rendering is checked.");
                    Assert.AreEqual(DaisySize.Medium, local.ResolvedSize);
                    Assert.AreEqual(stage.Bound, bound.ResolvedSize);
                    Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyAvatar.SizeProperty));
                    Assert.AreEqual(DaisySize.Medium, local.ReadLocalValue(DaisyAvatar.SizeProperty));
                    Assert.AreEqual(stage.Bound, bound.Size, "The authored child binding must remain active.");
                    Assert.IsTrue(reference.ActualHeight > 0);
                    Assert.AreEqual(reference.ActualHeight, automatic.ActualHeight, 0.1);
                    Assert.AreEqual(localReference.ActualHeight, local.ActualHeight, 0.1);
                    Assert.AreEqual(boundReference.ActualHeight, bound.ActualHeight, 0.1);
                }
            });

        [TestMethod]
        public Task When_AccordionScopeChanges_NestedButtonsRefreshWithoutReplacingAuthoredSizes() =>
            WithSizeScopeAsync(static async host =>
            {
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                var automatic = new DaisyButton { Content = "Automatic", NeumorphicEnabled = false };
                var local = new DaisyButton { Content = "Local", Size = DaisySize.Medium, NeumorphicEnabled = false };
                var bound = new DaisyButton { Content = "Bound", NeumorphicEnabled = false };
                var source = new ContentControl { Tag = DaisySize.Medium };
                bound.SetBinding(DaisyButton.SizeProperty, SizeBinding(source));
                var body = new StackPanel { Children = { automatic, local, bound } };
                var item = new DaisyAccordionItem { Header = "Nested controls", Content = body, NeumorphicEnabled = false };
                var accordion = new DaisyAccordion
                {
                    Size = DaisySize.Large,
                    ExpandedIndex = 0,
                    Content = new StackPanel { Children = { item } },
                    NeumorphicEnabled = false
                };
                host.Children.Add(accordion);
                await CompleteLayoutAsync(host, accordion, item, automatic, local, bound);
                Assert.IsTrue(item.IsExpanded);

                foreach (var size in new DaisySize?[] { DaisySize.Large, DaisySize.Small, null })
                {
                    if (size is { } explicitSize)
                        accordion.Size = explicitSize;
                    else
                        accordion.ClearValue(DaisyAccordion.SizeProperty);
                    await CompleteLayoutAsync(host);
                    AssertButtonTier(automatic, size ?? DaisySize.ExtraLarge);
                    AssertButtonTier(local, DaisySize.Medium);
                    AssertButtonTier(bound, DaisySize.Medium);
                    Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyButton.SizeProperty));
                    Assert.AreEqual(DaisySize.Medium, local.ReadLocalValue(DaisyButton.SizeProperty));
                    Assert.AreEqual(DaisySize.Medium, bound.Size);
                }

                source.Tag = DaisySize.Large;
                FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
                await CompleteLayoutAsync(host);
                AssertButtonTier(automatic, DaisySize.ExtraSmall);
                AssertButtonTier(bound, DaisySize.Large);
                Assert.AreEqual(DaisySize.Large, bound.Size, "The nested Size binding must remain active.");
            });

        [TestMethod]
        public Task When_ButtonGroupScopeChanges_ManualNestedIconsRefreshAndKeepAuthoredSizes() =>
            WithSizeScopeAsync(static async host =>
            {
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                var automatic = new DaisyIconText { IconData = "M0 0 L24 0 L24 24 Z", Text = "Auto", NeumorphicEnabled = false };
                var local = new DaisyIconText { IconData = automatic.IconData, Text = "Local", Size = DaisySize.Medium, NeumorphicEnabled = false };
                var bound = new DaisyIconText { IconData = automatic.IconData, Text = "Bound", NeumorphicEnabled = false };
                var source = new ContentControl { Tag = DaisySize.Medium };
                bound.SetBinding(DaisyIconText.SizeProperty, SizeBinding(source));
                var automaticButton = new DaisyButton { Content = automatic, NeumorphicEnabled = false };
                var localButton = new DaisyButton { Content = local, NeumorphicEnabled = false };
                var boundButton = new DaisyButton { Content = bound, NeumorphicEnabled = false };
                var group = new DaisyButtonGroup
                {
                    Size = DaisySize.Large,
                    Content = new StackPanel { Children = { automaticButton, localButton, boundButton } },
                    NeumorphicEnabled = false
                };
                host.Children.Add(group);
                await CompleteLayoutAsync(host, group, automaticButton, localButton, boundButton, automatic, local, bound);

                foreach (var size in new DaisySize?[] { DaisySize.Large, DaisySize.Small, null })
                {
                    if (size is { } explicitSize)
                        group.Size = explicitSize;
                    else
                        group.ClearValue(DaisyButtonGroup.SizeProperty);
                    await CompleteLayoutAsync(host);
                    AssertIconTier(automatic, size ?? DaisySize.ExtraLarge);
                    AssertButtonTier(automaticButton, size ?? DaisySize.ExtraLarge);
                    AssertIconTier(local, DaisySize.Medium);
                    AssertIconTier(bound, DaisySize.Medium);
                    Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyIconText.SizeProperty));
                    Assert.AreEqual(DaisySize.Medium, local.ReadLocalValue(DaisyIconText.SizeProperty));
                    Assert.AreEqual(DaisySize.Medium, bound.Size);
                }

                source.Tag = DaisySize.Large;
                FlowerySizeManager.ApplySize(DaisySize.ExtraSmall);
                await CompleteLayoutAsync(host);
                AssertIconTier(automatic, DaisySize.ExtraSmall);
                AssertIconTier(bound, DaisySize.Large);
                Assert.AreEqual(DaisySize.Large, bound.Size, "The manual icon's Size binding must remain active.");
            });

        [TestMethod]
        public Task When_AutomaticFabOptsOut_ActionsShareLargeFallbackAndCanOptBackIn() =>
            WithSizeScopeAsync(static async host =>
            {
                FlowerySizeManager.UseGlobalSizeByDefault = false;
                var automatic = new DaisyButton { Content = "Automatic", NeumorphicEnabled = false };
                var local = new DaisyButton { Content = "Local", Size = DaisySize.Medium, NeumorphicEnabled = false };
                var bound = new DaisyButton { Content = "Bound", NeumorphicEnabled = false };
                var source = new ContentControl { Tag = DaisySize.Medium };
                bound.SetBinding(DaisyButton.SizeProperty, SizeBinding(source));
                var fab = new DaisyFab
                {
                    Content = new StackPanel { Children = { automatic, local, bound } },
                    NeumorphicEnabled = false
                };
                var ancestor = new StackPanel { Children = { fab } };
                host.Children.Add(ancestor);
                await CompleteLayoutAsync(host, fab);
                fab.IsOpen = true;
                var trigger = (DaisyButton)((Grid)fab.Content).Children[1];
                await CompleteLayoutAsync(host, automatic, local, bound, trigger);
                Assert.AreEqual(Visibility.Visible, ((StackPanel)((Grid)fab.Content).Children[0]).Visibility);

                foreach (var stage in new[]
                {
                    (UseGlobal: false, Ignore: false, Expected: DaisySize.Large),
                    (UseGlobal: true, Ignore: false, Expected: DaisySize.Small),
                    (UseGlobal: true, Ignore: true, Expected: DaisySize.Large)
                })
                {
                    FlowerySizeManager.UseGlobalSizeByDefault = stage.UseGlobal;
                    FlowerySizeManager.SetIgnoreGlobalSize(ancestor, stage.Ignore);
                    await CompleteLayoutAsync(host);
                    Assert.AreEqual(stage.Expected, fab.ResolvedSize);
                    AssertButtonTier(trigger, stage.Expected);
                    AssertButtonTier(automatic, stage.Expected);
                    AssertButtonTier(local, DaisySize.Medium);
                    AssertButtonTier(bound, DaisySize.Medium);
                    Assert.AreEqual(DependencyProperty.UnsetValue, fab.ReadLocalValue(DaisyFab.SizeProperty));
                    Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyButton.SizeProperty));
                    Assert.AreEqual(DaisySize.Medium, local.ReadLocalValue(DaisyButton.SizeProperty));
                    Assert.AreEqual(DaisySize.Medium, bound.Size);
                }

                FlowerySizeManager.SetIgnoreGlobalSize(automatic, false);
                await CompleteLayoutAsync(host);
                AssertButtonTier(automatic, DaisySize.Small);
                AssertButtonTier(trigger, DaisySize.Large);
                source.Tag = DaisySize.Small;
                FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
                await CompleteLayoutAsync(host);
                AssertButtonTier(automatic, DaisySize.ExtraLarge);
                AssertButtonTier(trigger, DaisySize.Large);
                AssertButtonTier(local, DaisySize.Medium);
                AssertButtonTier(bound, DaisySize.Small);
                Assert.AreEqual(DaisySize.Small, bound.Size, "The action's Size binding must remain active.");
                Assert.AreEqual(DependencyProperty.UnsetValue, automatic.ReadLocalValue(DaisyButton.SizeProperty));

                ancestor.ClearValue(FlowerySizeManager.IgnoreGlobalSizeProperty);
                await CompleteLayoutAsync(host);
                Assert.AreEqual(DaisySize.ExtraLarge, fab.ResolvedSize);
                AssertButtonTier(trigger, DaisySize.ExtraLarge);
                AssertButtonTier(automatic, DaisySize.ExtraLarge);
            });

        private static void AssertButtonTier(DaisyButton button, DaisySize size)
        {
            Assert.IsTrue(button.IsLoaded);
            Assert.AreEqual(size, button.ResolvedSize);
            var resources = Application.Current.Resources;
            Assert.AreEqual(DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", size, "Height", DaisyResourceLookup.GetDefaultHeight(size)), button.Height, 0.001);
            Assert.AreEqual(DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", size, "FontSize", DaisyResourceLookup.GetDefaultFontSize(size)), button.FontSize, 0.001);
        }

        private static void AssertIconTier(DaisyIconText icon, DaisySize size)
        {
            Assert.IsTrue(icon.IsLoaded);
            Assert.AreEqual(size, icon.ResolvedSize);
            var content = (StackPanel)icon.Content;
            var viewbox = (Viewbox)content.Children[0];
            Assert.AreEqual(DaisyResourceLookup.GetDefaultIconSize(size), viewbox.Width, 0.001);
            Assert.AreEqual(DaisyResourceLookup.GetDefaultIconSize(size), viewbox.Height, 0.001);
            Assert.AreEqual(DaisyResourceLookup.GetDefaultFontSize(size), ((TextBlock)content.Children[1]).FontSize, 0.001);
        }

        private static async Task WithSizeScopeAsync(Func<StackPanel, Task> test)
        {
            var originalSize = FlowerySizeManager.CurrentSize;
            var originalUseGlobal = FlowerySizeManager.UseGlobalSizeByDefault;
            var originalAutoSize = FlowerySizeManager.EnableGlobalAutoSize;
            var originalWindow = FlowerySizeManager.MainWindow;
            var panel = RuntimeTestHelpers.GetHostPanel();
            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
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

        private static (DaisyButton Button, DaisyBadge Badge) AddControls(Panel host, DaisySize? size = null)
        {
            var button = new DaisyButton { Content = "Save", NeumorphicEnabled = false };
            var badge = new DaisyBadge { BadgeContent = "Save", NeumorphicEnabled = false };
            var controls = (button, badge);
            if (size is { } explicitSize)
                SetSize(controls, explicitSize);
            host.Children.Add(button);
            host.Children.Add(badge);
            return controls;
        }

        private static void AssertPasswordMatches(DaisyPasswordBox password, DaisyPasswordBox reference)
        {
            Assert.AreEqual("reload-value", password.Password);
            Assert.AreEqual(DependencyProperty.UnsetValue, password.ReadLocalValue(DaisyPasswordBox.SizeProperty));
            Assert.IsTrue(reference.ActualHeight > 0);
            Assert.AreEqual(reference.ActualHeight, password.ActualHeight, 0.1, "Password input height must follow the resolved size after reload.");
        }

        private static void SetSize((DaisyButton Button, DaisyBadge Badge) controls, DaisySize size)
        {
            controls.Button.Size = size;
            controls.Badge.Size = size;
        }

        private static Style SizeStyle(Type targetType, DependencyProperty property)
        {
            var basis = new Style(targetType);
            basis.Setters.Add(new Setter(property, DaisySize.Medium));
            return new Style(targetType) { BasedOn = basis };
        }

        private static Binding SizeBinding(ContentControl source) => new()
        {
            Source = source,
            Path = new PropertyPath(nameof(ContentControl.Tag)),
            Mode = BindingMode.OneWay
        };

        private static void AssertLocalSize((DaisyButton Button, DaisyBadge Badge) controls, DaisySize size)
        {
            Assert.AreEqual(size, controls.Button.ReadLocalValue(DaisyButton.SizeProperty));
            Assert.AreEqual(size, controls.Badge.ReadLocalValue(DaisyBadge.SizeProperty));
        }

        private static void AssertNoLocalSize((DaisyButton Button, DaisyBadge Badge) controls)
        {
            Assert.AreEqual(DependencyProperty.UnsetValue, controls.Button.ReadLocalValue(DaisyButton.SizeProperty));
            Assert.AreEqual(DependencyProperty.UnsetValue, controls.Badge.ReadLocalValue(DaisyBadge.SizeProperty));
        }

        private static (double ButtonFont, double ButtonHeight, double BadgeFont, double BadgeHeight) Metrics(
            (DaisyButton Button, DaisyBadge Badge) controls) =>
            (controls.Button.FontSize, controls.Button.ActualHeight, controls.Badge.FontSize, controls.Badge.ActualHeight);

        private static void AssertMatches(
            (DaisyButton Button, DaisyBadge Badge) controls,
            (DaisyButton Button, DaisyBadge Badge) reference,
            DaisySize referenceSize)
        {
            AssertLocalSize(reference, referenceSize);
            Assert.IsTrue(controls.Button.IsLoaded && controls.Badge.IsLoaded);
            Assert.IsTrue(reference.Button.ActualHeight > 0 && reference.Badge.ActualHeight > 0);
            Assert.AreEqual(reference.Button.FontSize, controls.Button.FontSize, 0.01, "Button font must match the requested size.");
            Assert.AreEqual(reference.Button.ActualHeight, controls.Button.ActualHeight, 0.1, "Button height must match the requested size.");
            Assert.AreEqual(reference.Badge.FontSize, controls.Badge.FontSize, 0.01, "Badge font must match the requested size.");
            Assert.AreEqual(reference.Badge.ActualHeight, controls.Badge.ActualHeight, 0.1, "Badge height must match the requested size.");
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
                throw new InvalidOperationException("The runtime dispatcher rejected size layout verification.");
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}

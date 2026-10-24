using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Flowery.Uno.RuntimeTests.RuntimeTestHelpers;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_SemanticInputs
    {
        [TestMethod]
        public async Task When_InputRolesChange_EditingAndAuthoredPlaceholdersSurviveThemeAndReload()
        {
            var originalTheme = DaisyThemeManager.CurrentThemeName ?? "Dark";
            var host = GetHostPanel();
            var panel = new StackPanel { Width = 320 };
            var input = new DaisyInput { Label = "Input", IsOptional = true, HintText = "Hint", HelperText = "Helper", PlaceholderText = "Placeholder" };
            var area = new DaisyTextArea { Label = "Area", IsOptional = true, HintText = "Hint", HelperText = "Helper", PlaceholderText = "Placeholder" };
            var password = new DaisyPasswordBox { Label = "Password", IsOptional = true, HintText = "Hint", HelperText = "Helper", PlaceholderText = "Placeholder" };
            ContentControl[] controls = [input, area, password];
            var editing = new SolidColorBrush(Colors.Crimson);
            var selection = new SolidColorBrush(Colors.Gold);
            var transparent = new SolidColorBrush(Colors.Transparent);
            foreach (var control in controls)
            {
                control.Resources["DaisyInputForeground"] = editing;
                panel.Children.Add(control);
            }

            try
            {
                DaisyThemeManager.ApplyTheme("Light");
                Assert.AreEqual("Light", DaisyThemeManager.CurrentThemeName);
                AttachToHost(panel);
                await CompleteLayoutAsync(panel, controls);
                var nativeInputs = controls.Select(FindNativeInput).ToArray();
                await CompleteLayoutAsync(panel, nativeInputs);
                foreach (var native in nativeInputs)
                {
                    if (native is PasswordBox passwordBox) passwordBox.SelectionHighlightColor = selection;
                    else ((TextBox)native).SelectionHighlightColor = selection;
                }
                for (var index = 0; index < controls.Length; index++)
                    AssertRoles(controls[index], nativeInputs[index], editing, selection, null);

                input.Text = "Retained input";
                area.Text = "Retained area";
                password.Password = "Retained password";
                foreach (var control in controls)
                    control.Resources["DaisyInputPlaceholderForeground"] = transparent;
                Assert.IsTrue(DaisyThemeManager.ApplyTheme("Dark"));
                await CompleteLayoutAsync(panel);
                for (var index = 0; index < controls.Length; index++)
                    AssertRoles(controls[index], nativeInputs[index], editing, selection, transparent);

                var unloaded = controls.Select(WaitForUnloadedAsync).ToArray();
                host.Children.Remove(panel);
                await Task.WhenAll(unloaded);
                await CompleteLayoutAsync(host);
                foreach (var control in controls)
                    control.Resources.Remove("DaisyInputPlaceholderForeground");
                Assert.IsTrue(DaisyThemeManager.ApplyTheme("Light"));
                host.Children.Add(panel);
                await CompleteLayoutAsync(panel, controls.Concat(nativeInputs).ToArray());
                for (var index = 0; index < controls.Length; index++)
                {
                    Assert.AreSame(nativeInputs[index], FindNativeInput(controls[index]));
                    AssertRoles(controls[index], nativeInputs[index], editing, selection, null);
                }
                Assert.AreEqual("Retained input", ((TextBox)nativeInputs[0]).Text);
                Assert.AreEqual("Retained area", ((TextBox)nativeInputs[1]).Text);
                Assert.AreEqual("Retained password", ((PasswordBox)nativeInputs[2]).Password);
            }
            finally
            {
                host.Children.Remove(panel);
                await CompleteLayoutAsync(host);
                DaisyThemeManager.ApplyTheme(originalTheme);
            }
        }

        private static void AssertRoles(ContentControl owner, Control native, Brush editing, SolidColorBrush selection, Brush? placeholderOverride)
        {
            Assert.IsTrue(native.IsLoaded);
            var root = (Grid)owner.Content;
            TextBlock[] auxiliary = [(TextBlock)((StackPanel)root.Children[0]).Children[2], (TextBlock)root.Children[1], (TextBlock)root.Children[3]];
            foreach (var text in auxiliary)
            {
                Assert.AreEqual(Visibility.Visible, text.Visibility);
                Assert.AreEqual(1.0, text.Opacity);
                AssertBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentMutedBrush"), text.Foreground);
            }
            AssertBrush(editing, native.Foreground);
            AssertBrush(editing, native.Resources["TextControlForegroundFocused"]);
            AssertBrush(editing, native.Resources["TextControlForegroundPointerOver"]);
            AssertBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentDisabledBrush"), native.Resources["TextControlForegroundDisabled"]);
            var placeholder = placeholderOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush");
            if (native is PasswordBox passwordBox)
            {
                AssertBrush(selection, passwordBox.SelectionHighlightColor);
                foreach (var prefix in new[] { "PasswordBoxPlaceholderTextForeground", "TextControlPlaceholderForeground" })
                    foreach (var state in new[] { "", "PointerOver", "Focused", "Disabled" })
                        AssertBrush(placeholder, native.Resources[prefix + state]);
            }
            else
            {
                var textBox = (TextBox)native;
                AssertBrush(placeholder, textBox.PlaceholderForeground);
                AssertBrush(selection, textBox.SelectionHighlightColor);
            }
        }

        private static void AssertBrush(Brush expected, object? actual)
        {
            Assert.IsInstanceOfType<SolidColorBrush>(expected);
            Assert.IsInstanceOfType<SolidColorBrush>(actual);
            Assert.AreEqual(((SolidColorBrush)expected).Color, ((SolidColorBrush)actual).Color);
            Assert.AreEqual(expected.Opacity, ((Brush)actual).Opacity);
        }

        private static async Task WaitForUnloadedAsync(FrameworkElement element)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            RoutedEventHandler handler = (_, _) => completion.TrySetResult();
            element.Unloaded += handler;
            try
            {
                await completion.Task.WaitAsync(System.TimeSpan.FromSeconds(5));
            }
            finally
            {
                element.Unloaded -= handler;
            }
        }

        private static Control FindNativeInput(DependencyObject owner) =>
            Descendants(owner).OfType<Control>().First(control => control is TextBox or PasswordBox);

        private static IEnumerable<DependencyObject> Descendants(DependencyObject owner)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
            {
                var child = VisualTreeHelper.GetChild(owner, index);
                yield return child;
                foreach (var descendant in Descendants(child))
                    yield return descendant;
            }
        }
    }
}

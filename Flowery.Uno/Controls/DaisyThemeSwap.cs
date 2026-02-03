using System;
using Microsoft.UI.Xaml;

namespace Flowery.Controls
{
    /// <summary>
    /// A swap control specifically for toggling between light and dark themes.
    /// Shows sun/moon icons by default.
    /// </summary>
    public partial class DaisyThemeSwap : DaisySwap
    {
        private readonly DaisyControlLifecycle _lifecycle;

        #region LightTheme
        public static readonly DependencyProperty LightThemeProperty =
            DependencyProperty.Register(
                nameof(LightTheme),
                typeof(string),
                typeof(DaisyThemeSwap),
                new PropertyMetadata("Light"));

        /// <summary>
        /// The name of the light theme to apply when unchecked.
        /// </summary>
        public string LightTheme
        {
            get => (string)GetValue(LightThemeProperty);
            set => SetValue(LightThemeProperty, value);
        }
        #endregion

        #region DarkTheme
        public static readonly DependencyProperty DarkThemeProperty =
            DependencyProperty.Register(
                nameof(DarkTheme),
                typeof(string),
                typeof(DaisyThemeSwap),
                new PropertyMetadata("Dark"));

        /// <summary>
        /// The name of the dark theme to apply when checked.
        /// </summary>
        public string DarkTheme
        {
            get => (string)GetValue(DarkThemeProperty);
            set => SetValue(DarkThemeProperty, value);
        }
        #endregion

        public DaisyThemeSwap()
        {
            _lifecycle = new DaisyControlLifecycle(
                this,
                SyncState,
                () => DaisySize.Medium,
                _ => { },
                handleLifecycleEvents: false,
                subscribeSizeChanges: false);

            TransitionEffect = SwapEffect.Rotate;
            Click += OnClick;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleLoaded();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleUnloaded();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            ToggleTheme();
        }

        private void ToggleTheme()
        {
            var currentTheme = DaisyThemeManager.CurrentThemeName;
            var isDark = currentTheme != null && DaisyThemeManager.IsDarkTheme(currentTheme);

            if (isDark)
            {
                DaisyThemeManager.ApplyTheme(LightTheme);
            }
            else
            {
                DaisyThemeManager.ApplyTheme(DarkTheme);
            }
        }


        private void SyncState()
        {
            var currentTheme = DaisyThemeManager.CurrentThemeName;
            IsChecked = currentTheme != null && DaisyThemeManager.IsDarkTheme(currentTheme);
        }
    }
}

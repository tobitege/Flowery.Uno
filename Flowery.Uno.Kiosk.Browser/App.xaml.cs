using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Xaml;

namespace Flowery.Uno.Kiosk.Browser
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            this.InitializeComponent();
            ConfigureCrashLogging();

            // WASM: Configure ComboBox dropdown to appear below the control
            global::Uno.UI.FeatureConfiguration.ComboBox.DefaultDropDownPreferredPlacement =
                global::Uno.UI.Xaml.Controls.DropDownPlacement.Below;
        }

        private void ConfigureCrashLogging()
        {
            UnhandledException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
                e.Handled = true;
            };
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            
            // Set managers for scaling/sizing
            Flowery.Controls.FlowerySizeManager.MainWindow = _window;
            Flowery.Services.FloweryScaleManager.MainWindow = _window;

            // Apply default theme
            DaisyThemeManager.ApplyTheme("Dark");

            _window.Activate();
        }
    }
}

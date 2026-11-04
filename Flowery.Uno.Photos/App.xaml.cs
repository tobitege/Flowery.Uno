using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Services;
using IOPath = System.IO.Path;

namespace Flowery.Uno.Photos
{
    public partial class App : Application
    {
        private MainWindow? _window;

        public App()
        {
            InitializeComponent();
            ConfigureCrashLogging();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                Launch();
            }
            catch (Exception ex)
            {
                LogFatal("OnLaunched", ex);
                throw;
            }
        }

        private void Launch()
        {
            // PhotosResources includes XamlControlsResources and the native Flowery dictionary.
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("ms-appx:///PhotosResources.xaml")
            });
            DaisyResourceLookup.EnsureTokens();

            PhotosSettings.Initialize(GetStateDirectory());

            // Ensure the localization registers its resolver early.
            _ = PhotosLocalization.Instance;

            // Apply language BEFORE creating the window so all bindings start in the right culture.
            var savedLanguage = PhotosSettings.LoadLanguage();
            if (!string.IsNullOrWhiteSpace(savedLanguage))
            {
                FloweryLocalization.SetCulture(savedLanguage);
            }

            var startupFile = Environment.GetCommandLineArgs()
                .Skip(1)
                .FirstOrDefault(a => File.Exists(a) && ImageMetadataReader.IsSupported(a));

            _window = new MainWindow(startupFile);
            FlowerySizeManager.MainWindow = _window;
            FloweryScaleManager.MainWindow = _window;

            ApplyPersistedSettings();

            _window.Activate();
        }

        private static void ApplyPersistedSettings()
        {
            FloweryLocalization.CultureChanged += (_, cultureName) =>
                PhotosSettings.SaveLanguage(cultureName);

            var savedTheme = PhotosSettings.LoadTheme() ?? "Dark";
            if (!DaisyThemeManager.ApplyTheme(savedTheme))
            {
                DaisyThemeManager.ApplyTheme("Dark");
            }
            DaisyThemeManager.ThemeChanged += (_, name) =>
                PhotosSettings.SaveTheme(name);

            var savedSize = PhotosSettings.LoadGlobalSize();
            if (savedSize.HasValue)
            {
                FlowerySizeManager.ApplySize(savedSize.Value);
            }

            FlowerySizeManager.SizeChanged += (_, size) =>
                PhotosSettings.SaveGlobalSize(size);
        }

        internal static string GetStateDirectory() => StateDirectory.Resolve();

        private void ConfigureCrashLogging()
        {
            UnhandledException += (_, e) =>
            {
                try
                {
                    LogFatal("Application.UnhandledException", e.Exception);
                }
                finally
                {
                    e.Handled = true;
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                LogFatal("AppDomain.UnhandledException", e.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                LogFatal("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };
        }

        private static void LogFatal(string source, Exception? ex)
        {
            var message = $"[{DateTimeOffset.Now:O}] {source}: {ex}\n";
            Debug.WriteLine(message);

            try
            {
                var dir = GetStateDirectory();
                Directory.CreateDirectory(dir);
                File.AppendAllText(IOPath.Combine(dir, "crash.log"), message);
            }
            catch
            {
                // Ignore file IO failures; Debug output is still useful.
            }
        }
    }
}

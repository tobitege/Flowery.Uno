// Uncomment to regenerate ProductPalettes.Generated.cs on next run:
// #define GENERATE_PRODUCT_PALETTES

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using IOPath = System.IO.Path;
using Flowery.Integrations.Uno.GitHub;
using Flowery.Integrations.Uno.OAuth;
using Flowery.Theming;
using Flowery.Uno.Gallery.Localization;
using Flowery.Uno.Kanban.Controls;
using Flowery.Uno.Kanban.Controls.Users;
using Flowery.Services;

#if WINDOWS || __SKIA__
using Flowery.Uno.RuntimeTests;
#endif

namespace Flowery.Uno.Gallery
{
    public partial class App : Application
    {
        private Window? _window;
        internal static string[]? RuntimeArguments { get; set; }
        internal static ICompositeUserProvider? KanbanUserProvider { get; private set; }

        public App()
        {
#if __ANDROID__
            ConfigureCrashLogging();
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                LogFatal("App.Constructor.InitializeComponent", ex);
                throw;
            }
#else
            InitializeComponent();
            ConfigureCrashLogging();
#endif

#if __WASM__ || __SKIA__
            // Configure ComboBox dropdown placement for non-Windows platforms.
            global::Uno.UI.FeatureConfiguration.ComboBox.DefaultDropDownPreferredPlacement =
                global::Uno.UI.Xaml.Controls.DropDownPlacement.Below;
#endif
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
#if __ANDROID__
            try
            {
                LogFatal("App.OnLaunched", null);
                RunAppLaunch(args);
            }
            catch (Exception ex)
            {
                LogFatal("App.OnLaunched", ex);
                throw;
            }
#else
            RunAppLaunch(args);
#endif
        }

        private void RunAppLaunch(LaunchActivatedEventArgs args)
        {
#if WINDOWS || __SKIA__
            RuntimeArguments ??= Environment.GetCommandLineArgs();
#endif

#if WINDOWS
            var isRuntimeTests = RuntimeTestArguments.TryGetRuntimeTestsPath(args.Arguments, RuntimeArguments, out _);
            EnsureGalleryResources(isRuntimeTests);
            if (isRuntimeTests)
            {
                _window = new Window();
                _window.Activate();
                StartRuntimeTestsIfRequested(args);
                return;
            }
#else
            EnsureGalleryResources();
#endif
            DaisyResourceLookup.EnsureTokens();

            // Ensure GalleryLocalization runs early and registers the custom resolver.
            _ = GalleryLocalization.Instance;

#if GENERATE_PRODUCT_PALETTES
            if (OperatingSystem.IsWindows() && !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS())
            {
                GeneratePrecompiledProductPalettes();
            }
#endif

            ConfigureStateStorage();
            ConfigureKanbanUserProvider();
            // only for demo purposes we enable the user management button!
            // FlowKanban is SINGLE-USER only for time being!
            FlowKanban.IsUserManagementButtonEnabled = true;

            // Apply language BEFORE creating MainWindow so FloweryLocalization.CurrentCultureName
            // is already set when the sidebar reads it during construction.
            var savedLanguage = GallerySettings.LoadLanguage();
            if (!string.IsNullOrWhiteSpace(savedLanguage))
            {
                FloweryLocalization.SetCulture(savedLanguage);
            }

            _window = new MainWindow();
            FlowerySizeManager.MainWindow = _window;
            FloweryScaleManager.MainWindow = _window;

            // Apply remaining persisted settings AFTER MainWindow is set (for visual tree propagation).
            ApplyPersistedSettings();

            _window.Activate();

#if WINDOWS || __SKIA__
            StartRuntimeTestsIfRequested(args);
#endif
        }

        private static void ConfigureStateStorage()
        {
#if __WASM__
#pragma warning disable CA1416 // BrowserStateStorage is browser-only, but this file is only compiled for browser
            StateStorageProvider.Configure(new Flowery.Uno.Gallery.Browser.BrowserStateStorage());
#pragma warning restore CA1416
#else
            StateStorageProvider.Configure(new FileStateStorage("Flowery.Uno.Gallery"));
#endif
        }

        private static void ConfigureKanbanUserProvider()
        {
#pragma warning disable CA1416 // net*-desktop TFMs aren't OS-specific; Kanban providers are Uno-platform supported.
            var provider = new CompositeUserProvider();
            provider.RegisterProvider(new LocalUserProvider(includeDemoUsers: true));
            provider.RegisterProvider(new GitHubUserProvider(new PasswordVaultSecureStorage(), loadTokenFromStorage: true));
            try
            {
                provider.RegisterProvider(new OAuthUserProvider(
                    providerKey: "oidc-demo",
                    displayName: "OIDC Demo",
                    authority: "https://demo.duendesoftware.com/",
                    clientId: "interactive.confidential",
                    clientSecret: "secret",
                    scope: "openid profile api offline_access",
                    secureStorage: new PasswordVaultSecureStorage(),
                    loadTokenFromStorage: true));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Demo OIDC provider init failed: {ex.GetType().Name} - {ex.Message}");
            }
            KanbanUserProvider = provider;
            FlowKanban.DefaultUserProvider = provider;
#pragma warning restore CA1416
        }

#if WINDOWS || __SKIA__
        private async void StartRuntimeTestsIfRequested(LaunchActivatedEventArgs args)
        {
            if (_window == null)
            {
                return;
            }

            if (!RuntimeTestArguments.TryGetRuntimeTestsPath(args.Arguments, RuntimeArguments, out var resultPath))
            {
                return;
            }

            try
            {
                var exitCode = await RuntimeTestRunner.RunAsync(_window, resultPath);
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                TryWriteRuntimeTestFailure(resultPath, ex);
                Environment.Exit(1);
            }
        }

        private static void TryWriteRuntimeTestFailure(string resultPath, Exception ex)
        {
            try
            {
                File.WriteAllText(resultPath, $"Runtime tests failed: {ex}");
            }
            catch
            {
                // Ignore write failures; exit code still signals failure.
            }
        }
#endif

#if GENERATE_PRODUCT_PALETTES
        private static void GeneratePrecompiledProductPalettes()
        {
            // Theme Source (MIT): https://github.com/nextlevelbuilder/ui-ux-pro-max-skill
            try
            {
                var code = ProductPaletteFactory.GeneratePrecompiledPalettesCode();

                // Navigate from bin/Debug/.../Flowery.Uno.Gallery/ up to repo root, then into Flowery.Uno/Theming/
                var assemblyDir = IOPath.GetDirectoryName(typeof(App).Assembly.Location) ?? string.Empty;
                var repoRoot = IOPath.GetFullPath(IOPath.Combine(assemblyDir, "..", "..", "..", "..", ".."));
                var outputPath = IOPath.Combine(repoRoot, "Flowery.Uno", "Theming", "ProductPalettes.Generated.cs");

                File.WriteAllText(outputPath, code);
                Debug.WriteLine($"[App] Generated ProductPalettes to: {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Failed to generate ProductPalettes: {ex.Message}");
            }
        }
#endif

        private void ConfigureCrashLogging()
        {
            // WinUI apps don't have a visible console by default. Log to VS Output (Debug) and a file.
            UnhandledException += (_, e) =>
            {
                try
                {
                    LogFatal("Application.UnhandledException", e.Exception);
                }
                finally
                {
#if !__ANDROID__
                    // Prevent immediate termination so we have a chance to log details.
                    e.Handled = true;
#endif
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                LogFatal("AppDomain.UnhandledException", e.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                LogFatal("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

#if WINDOWS
            AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            {
                if (e.Exception is COMException comException &&
                    ShouldLogFirstChanceException(comException))
                {
                    LogFatal("AppDomain.FirstChanceException", e.Exception);
                }
            };
#endif
        }

#if WINDOWS
        private static bool ShouldLogFirstChanceException(COMException exception)
        {
            const int ErrorNotFound = unchecked((int)0x80070490);
            if (exception.HResult == ErrorNotFound)
            {
                return !PasswordVaultOperationScope.IsActive;
            }

            return true;
        }
#endif

        private void EnsureGalleryResources(bool isRuntimeTests = false)
        {
#if WINDOWS
            if (isRuntimeTests)
            {
                EnsureRuntimeTestThemeLayout();
                TryAddXamlControlsResources();
                var runtimeFloweryOk = TryAddDictionary("ms-appx:///Flowery.Uno/Themes/Generic.xaml");
                var runtimeKanbanOk = TryAddDictionary("ms-appx:///Flowery.Uno.Kanban/Themes/Generic.xaml");

                if (!runtimeFloweryOk || !runtimeKanbanOk)
                {
                    throw new InvalidOperationException("Failed to load runtime test resource dictionaries.");
                }

                return;
            }
#endif

            if (TryAddDictionary("ms-appx:///Flowery.Uno.Gallery.Core/GalleryResources.xaml"))
            {
                return;
            }

            if (TryAddDictionary("ms-appx:///GalleryResources.xaml"))
            {
                return;
            }

            TryAddXamlControlsResources();
            var floweryOk = TryAddDictionary("ms-appx:///Flowery.Uno/Themes/Generic.xaml");
            var kanbanOk = TryAddDictionary("ms-appx:///Flowery.Uno.Kanban/Themes/Generic.xaml");

            if (!floweryOk || !kanbanOk)
            {
                throw new InvalidOperationException("Failed to load one or more gallery resource dictionaries.");
            }
        }

        private bool TryAddDictionary(string source)
        {
            try
            {
                Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri(source)
                });
                return true;
            }
            catch (Exception ex)
            {
                LogFatal($"EnsureGalleryResources failed for {source}", ex);
                return false;
            }
        }

        private void TryAddXamlControlsResources()
        {
            try
            {
                Resources.MergedDictionaries.Add(new XamlControlsResources());
            }
            catch (Exception ex)
            {
                LogFatal("EnsureGalleryResources failed for XamlControlsResources", ex);
            }
        }

#if WINDOWS
        private void EnsureRuntimeTestThemeLayout()
        {
            try
            {
                var baseDirectory = AppContext.BaseDirectory;
                var sourceThemes = IOPath.Combine(baseDirectory, "Themes");
                var targetThemes = IOPath.Combine(baseDirectory, "Flowery.Uno", "Themes");
                var targetGeneric = IOPath.Combine(targetThemes, "Generic.xaml");

                if (!Directory.Exists(sourceThemes) || File.Exists(targetGeneric))
                {
                    return;
                }

                foreach (var sourcePath in Directory.EnumerateFiles(sourceThemes, "*", SearchOption.AllDirectories))
                {
                    var relativePath = IOPath.GetRelativePath(sourceThemes, sourcePath);
                    var destinationPath = IOPath.Combine(targetThemes, relativePath);
                    var destinationDirectory = IOPath.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrWhiteSpace(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    File.Copy(sourcePath, destinationPath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                LogFatal("EnsureRuntimeTestThemeLayout failed", ex);
            }
        }
#endif

        private static void LogFatal(string source, Exception? ex)
        {
            var timestamp = DateTimeOffset.Now.ToString("O");
            var message = ex == null
                ? $"[{timestamp}] {source}: REACHED\n"
                : $"[{timestamp}] {source}: {ex}\n";

            Debug.WriteLine(message);

            try
            {
#if __WASM__
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dir = string.IsNullOrWhiteSpace(localAppData)
                    ? "Flowery.Uno.Gallery"
                    : IOPath.Combine(localAppData, "Flowery.Uno.Gallery");
#elif __ANDROID__
                var localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dir = string.IsNullOrWhiteSpace(localFolder)
                    ? "Flowery.Uno.Gallery"
                    : IOPath.Combine(localFolder, "Flowery.Uno.Gallery");
#else
                var dir = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flowery.Uno.Gallery");
#endif

                Directory.CreateDirectory(dir);
                File.AppendAllText(IOPath.Combine(dir, "crash.log"), message);
            }
            catch
            {
                // Ignore file IO failures; Debug output is still useful.
            }
        }

        private static void ApplyPersistedSettings()
        {
            // Language is now applied BEFORE MainWindow creation (see OnLaunched).
            // We only need to subscribe to save changes here.
            FloweryLocalization.CultureChanged += (_, cultureName) =>
                GallerySettings.SaveLanguage(cultureName);

            var savedTheme = GallerySettings.Load() ?? "Dark";
            DaisyThemeManager.ApplyTheme(savedTheme);
            DaisyThemeManager.ThemeChanged += (_, name) =>
                GallerySettings.Save(name);

            var savedSize = GallerySettings.LoadGlobalSize();
            if (savedSize.HasValue)
            {
                FlowerySizeManager.ApplySize(savedSize.Value);
            }

            FlowerySizeManager.SizeChanged += (_, size) =>
                GallerySettings.SaveGlobalSize(size);
        }
    }
}

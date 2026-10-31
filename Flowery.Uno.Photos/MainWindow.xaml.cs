using Flowery.Uno.Photos.Services;
using Flowery.Uno.Photos.Services.Platform;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.UI.Core;

namespace Flowery.Uno.Photos
{
    public sealed partial class MainWindow : Window
    {
        private static readonly SizeInt32 DefaultSize = new() { Width = 1280, Height = 800 };

        public MainWindow(string? startupFile)
        {
            InitializeComponent();

            Title = "Flowery Photos";
            StartupFile = startupFile;

            // Extend into the title bar only where the head supports it (Windows). Elsewhere the system title bar stays:
            // macOS ignores the request and Linux loses its caption buttons (phase 0 spike).
            UsesExtendedTitleBar = PlatformServices.Current.TitleBarMode == TitleBarMode.Extended
                && AppWindowTitleBar.IsCustomizationSupported();
            ExtendsContentIntoTitleBar = UsesExtendedTitleBar;

            GetSafeAppWindow()?.Resize(DefaultSize);

            Shell.Initialize(this, startupFile);

            WindowsAccent.Initialize(DispatcherQueue);
            Activated += (_, args) =>
            {
                if (args.WindowActivationState != CoreWindowActivationState.Deactivated)
                    WindowsAccent.Refresh();
            };
        }

        public string? StartupFile { get; }

        /// <summary>True when the app bar replaces the system title bar; otherwise it is a normal row below it.</summary>
        public bool UsesExtendedTitleBar { get; }

        public bool IsFullScreen => GetSafeAppWindow()?.Presenter.Kind == AppWindowPresenterKind.FullScreen;

        public void SetFullScreen(bool fullScreen)
        {
            var appWindow = GetSafeAppWindow();
            if (appWindow == null)
                return;

            // Skia: on Linux the window stays maximized when full screen is requested from the maximized state,
            // so leave that state first.
            if (fullScreen && OperatingSystem.IsLinux() && appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Maximized } overlapped)
            {
                overlapped.Restore();
            }

            appWindow.SetPresenter(fullScreen ? AppWindowPresenterKind.FullScreen : AppWindowPresenterKind.Overlapped);
        }

        private AppWindow? GetSafeAppWindow()
        {
            try
            {
                return AppWindow;
            }
            catch
            {
                return null;
            }
        }
    }
}

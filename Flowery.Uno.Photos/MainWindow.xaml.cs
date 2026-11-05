using Flowery.Uno.Photos.Services;
using Flowery.Uno.Photos.Services.Platform;
using Flowery.Uno.Photos.Views;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.UI.Core;

namespace Flowery.Uno.Photos
{
    public sealed partial class MainWindow : Window
    {
        private static readonly SizeInt32 DefaultSize = new() { Width = 1280, Height = 800 };
        private readonly WindowCloseGuard _closeGuard = new();

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

            if (GetSafeAppWindow() is { } appWindow)
            {
                RestoreBounds(appWindow);
                appWindow.Changed += (_, args) =>
                {
                    if (args.DidPresenterChange || args.DidSizeChange || args.DidPositionChange)
                        UpdateFullScreenInset();
                    if (!args.DidPresenterChange && (args.DidSizeChange || args.DidPositionChange))
                        SaveBounds(appWindow);
                };
                appWindow.Closing += OnAppWindowClosing;
            }

            Shell.Initialize(this, startupFile);
            Closed += (_, _) => Shell.Shutdown();

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

            // Windows: in full screen the caption buttons of the extended title bar would stay drawn half above the screen edge
            // and the content would start a few pixels too high, so the extension is given up while full screen lasts.
            if (UsesExtendedTitleBar && !fullScreen)
                ExtendsContentIntoTitleBar = true;

            appWindow.SetPresenter(fullScreen ? AppWindowPresenterKind.FullScreen : AppWindowPresenterKind.Overlapped);

            if (UsesExtendedTitleBar && fullScreen)
                ExtendsContentIntoTitleBar = false;

            UpdateFullScreenInset();
        }

        /// <summary>
        /// Windows: Uno shows full screen as a maximized window without caption, and that window, client area included, reaches
        /// past the monitor by the resize border on every side. The shell is inset by that overhang so nothing is cut off.
        /// </summary>
        private void UpdateFullScreenInset()
        {
            var overhang = default(ScreenOverhang);
            if (GetSafeAppWindow() is { Presenter.Kind: AppWindowPresenterKind.FullScreen } appWindow)
            {
                var window = new RectInt32 { X = appWindow.Position.X, Y = appWindow.Position.Y, Width = appWindow.Size.Width, Height = appWindow.Size.Height };
                if (PlatformServices.Current.GetMonitorBounds(window) is { } monitor)
                    overhang = ScreenOverhang.Between(window, monitor);
            }

            var scale = Shell.XamlRoot?.RasterizationScale ?? 1.0;
            Shell.Margin = new Thickness(overhang.Left / scale, overhang.Top / scale, overhang.Right / scale, overhang.Bottom / scale);
        }

        /// <summary>Reopens the window with the size it had, where the system places it; see <see cref="WindowPlacement"/>.</summary>
        private static void RestoreBounds(AppWindow appWindow)
        {
            var current = new RectInt32 { X = appWindow.Position.X, Y = appWindow.Position.Y, Width = appWindow.Size.Width, Height = appWindow.Size.Height };
            var restored = WindowPlacement.Restore(PhotosSettings.LoadWindowBounds(), PlatformServices.Current.GetWorkArea(current));
            appWindow.Resize(restored is { } r ? new SizeInt32 { Width = r.Width, Height = r.Height } : DefaultSize);
        }

        /// <summary>
        /// Remembers the bounds of the normal window (only the size is used when it reopens); maximized and full screen
        /// bounds are not kept.
        /// </summary>
        private static void SaveBounds(AppWindow appWindow)
        {
            if (appWindow.Presenter is not OverlappedPresenter { State: OverlappedPresenterState.Restored })
                return;
            if (appWindow.Size is { Width: > 0, Height: > 0 } size)
                PhotosSettings.SaveWindowBounds(new WindowBounds(appWindow.Position.X, appWindow.Position.Y, size.Width, size.Height));
        }

        /// <summary>Closing the window with unsaved edits asks first, like closing the viewer.</summary>
        private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            switch (_closeGuard.OnClosing(Shell.HasUnsavedEdits))
            {
                case CloseDecision.Cancel:
                    args.Cancel = true;
                    break;
                case CloseDecision.CancelAndAsk:
                    args.Cancel = true;
                    DispatcherQueue.TryEnqueue(AskBeforeClosing);
                    break;
            }
        }

        private async void AskBeforeClosing()
        {
            bool close;
            try
            {
                close = await Shell.ConfirmDiscardEditsAsync();
            }
            catch (Exception ex)
            {
                // An event handler must not end the app; with the edits in doubt the window stays open.
                System.Diagnostics.Debug.WriteLine(ex);
                close = false;
            }
            if (_closeGuard.OnAnswer(close))
                Close();
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

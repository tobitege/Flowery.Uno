using Flowery.Uno.Photos.Services;
using Microsoft.UI.Dispatching;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Viewer chrome layout: immersive full screen (taskbar and app title bar hidden), an auto-hiding
    /// toolbar and an optional status bar. Overlay bars float above the photo: they stay visible while the
    /// pointer is near their edge, fade out shortly after it leaves, and hide at once on a click elsewhere
    /// or when one of their menus closes.
    /// </summary>
    public sealed partial class ViewerView
    {
        private const double ChromeRevealBand = 96;
        private static readonly TimeSpan ChromeInitialDelay = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan ChromeLeaveDelay = TimeSpan.FromMilliseconds(300);
        private bool _immersive;
        private bool _immersiveBeforeSlideshow;
        private DispatcherQueueTimer? _chromeHideTimer;
        private bool _chromeInitialized;
        private double _lastPointerY = double.NaN;

        public bool IsImmersive => _immersive;

        private bool ToolbarIsOverlay => _immersive || PhotosSettings.AutoHideToolbar;

        private bool StatusBarIsOverlay => _immersive;

        public void ToggleImmersive() => SetImmersive(!_immersive);

        public void SetImmersive(bool immersive)
        {
            if (_shell == null || _slideshow)
                return;

            _immersive = immersive;
            _shell.SetFullScreen(immersive);
            _shell.SetTitleBarVisible(!immersive);
            CanvasHost.Margin = immersive ? new Thickness(0) : new Thickness(8, 0, 8, 0);
            CanvasHost.CornerRadius = immersive ? new CornerRadius(0) : new CornerRadius(12);
            ApplyChromeLayout();
            UpdateFullScreenButton();
            ZoomToFit();
        }

        /// <summary>
        /// Places toolbar/status bar either in their own rows or as overlays above the photo.
        /// </summary>
        private void ApplyChromeLayout()
        {
            EnsureChromeInitialized();
            if (_slideshow)
                return;

            StatusBar.Visibility = PhotosSettings.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;

            var startRow = ToolbarIsOverlay ? 0 : 1;
            var endRow = StatusBarIsOverlay ? 2 : 1;
            Grid.SetRow(MainArea, startRow);
            Grid.SetRowSpan(MainArea, endRow - startRow + 1);

            Toolbar.Background = ToolbarIsOverlay ? CreateChromeBrush() : null;
            StatusBar.Background = StatusBarIsOverlay ? CreateChromeBrush() : null;

            var toolbarHeight = Toolbar.ActualHeight > 0 ? Toolbar.ActualHeight : 52;
            var statusHeight = StatusBar.Visibility == Visibility.Visible && StatusBar.ActualHeight > 0 ? StatusBar.ActualHeight : 0;
            // The edit panel stays usable between overlay bars.
            EditPanel.Margin = new Thickness(0, ToolbarIsOverlay ? toolbarHeight : 0, 8, StatusBarIsOverlay ? statusHeight + 4 : 0);

            if (ToolbarIsOverlay || StatusBarIsOverlay)
            {
                RevealChrome();
            }
            else
            {
                UiTimers.Cancel(_chromeHideTimer);
                _chromeHideTimer = null;
                SetChromeVisible(true);
            }
            Canvas.Invalidate();
        }

        private static Brush CreateChromeBrush()
        {
            var color = DaisyResourceLookup.GetBrush("DaisyBase200Brush") is SolidColorBrush b ? b.Color : Color.FromArgb(255, 30, 30, 30);
            return new SolidColorBrush(Color.FromArgb(225, color.R, color.G, color.B));
        }

        private void EnsureChromeInitialized()
        {
            if (_chromeInitialized)
                return;
            _chromeInitialized = true;
            Microsoft.UI.Xaml.Controls.Canvas.SetZIndex(Toolbar, 10);
            Microsoft.UI.Xaml.Controls.Canvas.SetZIndex(StatusBar, 10);
            Toolbar.OpacityTransition = new ScalarTransition { Duration = TimeSpan.FromMilliseconds(180) };
            StatusBar.OpacityTransition = new ScalarTransition { Duration = TimeSpan.FromMilliseconds(180) };
            RootGrid.PointerMoved += RootGrid_PointerMoved;
            // Leaving upwards into the title bar keeps the toolbar (it sits right below it).
            RootGrid.PointerExited += (_, _) =>
            {
                if (!(ToolbarIsOverlay && _lastPointerY <= ChromeRevealBand))
                    ScheduleChromeHide(ChromeLeaveDelay);
            };
            RootGrid.AddHandler(PointerPressedEvent, new PointerEventHandler(RootGrid_PointerPressed), true);
            // A click outside a chrome menu closes it (light dismiss) without reaching RootGrid; hide right away.
            MoreMenu.Closed += (_, _) => HideChromeNow();
            SaveMenu.Closed += (_, _) => HideChromeNow();
            ZoomMenu.Closed += (_, _) => HideChromeNow();
            ViewFlyout.Closed += (_, _) => HideChromeNow();
            Toolbar.SizeChanged += (_, _) =>
            {
                if (ToolbarIsOverlay)
                    EditPanel.Margin = new Thickness(0, Toolbar.ActualHeight, 8, EditPanel.Margin.Bottom);
            };
        }

        private void SetChromeVisible(bool visible)
        {
            var toolbarVisible = visible || !ToolbarIsOverlay;
            var statusVisible = visible || !StatusBarIsOverlay;
            Toolbar.Opacity = toolbarVisible ? 1 : 0;
            Toolbar.IsHitTestVisible = toolbarVisible;
            StatusBar.Opacity = statusVisible ? 1 : 0;
            StatusBar.IsHitTestVisible = statusVisible;
            // With the toolbar hidden, the view options stay reachable from the title bar (hidden in immersive mode).
            _shell?.SetTitleViewButtonVisible(IsOpen && !toolbarVisible && !_immersive);
        }

        /// <summary>Opens the view options flyout below another element (the title bar button).</summary>
        public void ShowViewOptions(FrameworkElement target) =>
            ViewFlyout.ShowAt(target, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });

        /// <summary>Shows the overlay bars briefly (viewer opened, layout changed).</summary>
        private void RevealChrome()
        {
            SetChromeVisible(true);
            ScheduleChromeHide(ChromeInitialDelay);
        }

        /// <summary>Shows the overlay bars and keeps them while the pointer stays near their edge.</summary>
        private void HoldChrome()
        {
            SetChromeVisible(true);
            UiTimers.Cancel(_chromeHideTimer);
            _chromeHideTimer = null;
        }

        private void ScheduleChromeHide(TimeSpan delay)
        {
            if (!ToolbarIsOverlay && !StatusBarIsOverlay)
                return;
            UiTimers.Cancel(_chromeHideTimer);
            _chromeHideTimer = UiTimers.RunOnce(DispatcherQueue, delay, HideChromeIfIdle);
        }

        private void HideChromeNow()
        {
            if (!ToolbarIsOverlay && !StatusBarIsOverlay)
                return;
            UiTimers.Cancel(_chromeHideTimer);
            _chromeHideTimer = null;
            SetChromeVisible(false);
        }

        private void HideChromeIfIdle()
        {
            _chromeHideTimer = null;
            if (!ToolbarIsOverlay && !StatusBarIsOverlay)
                return;
            // Keep the bars while one of their menus or flyouts is open; closing it hides them.
            if (MoreMenu.IsOpen || SaveMenu.IsOpen || ZoomMenu.IsOpen || ViewFlyout.IsOpen)
                return;
            SetChromeVisible(false);
        }

        private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!ToolbarIsOverlay && !StatusBarIsOverlay)
                return;
            _lastPointerY = e.GetCurrentPoint(RootGrid).Position.Y;
            if (IsInChromeBand(_lastPointerY))
                HoldChrome();
            else if (_chromeHideTimer == null && (Toolbar.Opacity > 0 || StatusBar.Opacity > 0))
                ScheduleChromeHide(ChromeLeaveDelay);
        }

        private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!ToolbarIsOverlay && !StatusBarIsOverlay)
                return;
            var y = e.GetCurrentPoint(RootGrid).Position.Y;
            var onToolbar = ToolbarIsOverlay && y <= Toolbar.ActualHeight;
            var onStatusBar = StatusBarIsOverlay && StatusBar.Visibility == Visibility.Visible && y >= RootGrid.ActualHeight - StatusBar.ActualHeight;
            if (!onToolbar && !onStatusBar)
                HideChromeNow();
        }

        private bool IsInChromeBand(double y) =>
            (ToolbarIsOverlay && y <= Math.Max(ChromeRevealBand, Toolbar.ActualHeight)) ||
            (StatusBarIsOverlay && y >= RootGrid.ActualHeight - ChromeRevealBand);
    }
}

using System;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// A drawer/sidebar control styled after DaisyUI's Drawer component.
    /// Wraps SplitView with sensible defaults for drawer behavior.
    /// Supports responsive mode to auto-open/close based on available width.
    /// </summary>
    public partial class DaisyDrawer : SplitView
    {
        private readonly DaisyControlLifecycle _lifecycle;

        public DaisyDrawer()
        {
            IsTabStop = false;
            // DaisyUI Drawer defaults
            DisplayMode = SplitViewDisplayMode.Inline;
            PanePlacement = SplitViewPanePlacement.Left;
            OpenPaneLength = 300; // Typical drawer width
            CompactPaneLength = 0;

            _lifecycle = new DaisyControlLifecycle(
                this,
                ApplyTheme,
                () => DaisySize.Medium,
                _ => { },
                handleLifecycleEvents: false,
                subscribeSizeChanges: false);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleLoaded();

            // Sync initial values from dependency properties to base SplitView properties
            OpenPaneLength = DrawerWidth;
            PanePlacement = DrawerSide;
            DisplayMode = OverlayMode ? SplitViewDisplayMode.Overlay : SplitViewDisplayMode.Inline;
            
            // Apply responsive state or initial state
            if (ResponsiveMode)
            {
                UpdateResponsiveState(ActualWidth);
            }
            else
            {
                IsPaneOpen = IsDrawerOpen;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleUnloaded();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ResponsiveMode)
            {
                UpdateResponsiveState(e.NewSize.Width);
            }
        }

        private void UpdateResponsiveState(double width)
        {
            bool shouldBeOpen = width >= ResponsiveThreshold;
            
            // Update IsDrawerOpen if it doesn't match the intended state
            if (IsDrawerOpen != shouldBeOpen)
            {
                IsDrawerOpen = shouldBeOpen;
            }
            
            // Force-sync IsPaneOpen to handle SplitView internal desync (especially in Overlay mode)
            if (IsPaneOpen != shouldBeOpen)
            {
                IsPaneOpen = shouldBeOpen;
            }
        }

        private void ApplyTheme()
        {
            if (ReadLocalValue(BackgroundProperty) == DependencyProperty.UnsetValue)
            {
                var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyDrawer", "Background");
                Background = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush", fallback: new SolidColorBrush(Microsoft.UI.Colors.LightGray));
            }
        }

        #region Dependency Properties

        public static readonly DependencyProperty DrawerWidthProperty =
            DependencyProperty.Register(
                nameof(DrawerWidth),
                typeof(double),
                typeof(DaisyDrawer),
                new PropertyMetadata(300.0, OnDrawerWidthChanged));

        /// <summary>
        /// Gets or sets the width of the drawer pane when open.
        /// </summary>
        public double DrawerWidth
        {
            get => (double)GetValue(DrawerWidthProperty);
            set => SetValue(DrawerWidthProperty, value);
        }

        public static readonly DependencyProperty DrawerSideProperty =
            DependencyProperty.Register(
                nameof(DrawerSide),
                typeof(SplitViewPanePlacement),
                typeof(DaisyDrawer),
                new PropertyMetadata(SplitViewPanePlacement.Left, OnDrawerSideChanged));

        /// <summary>
        /// Gets or sets which side the drawer appears on.
        /// </summary>
        public SplitViewPanePlacement DrawerSide
        {
            get => (SplitViewPanePlacement)GetValue(DrawerSideProperty);
            set => SetValue(DrawerSideProperty, value);
        }

        public static readonly DependencyProperty IsDrawerOpenProperty =
            DependencyProperty.Register(
                nameof(IsDrawerOpen),
                typeof(bool),
                typeof(DaisyDrawer),
                new PropertyMetadata(false, OnIsDrawerOpenChanged));

        /// <summary>
        /// Gets or sets whether the drawer is currently open.
        /// </summary>
        public bool IsDrawerOpen
        {
            get => (bool)GetValue(IsDrawerOpenProperty);
            set => SetValue(IsDrawerOpenProperty, value);
        }

        public static readonly DependencyProperty OverlayModeProperty =
            DependencyProperty.Register(
                nameof(OverlayMode),
                typeof(bool),
                typeof(DaisyDrawer),
                new PropertyMetadata(false, OnOverlayModeChanged));

        /// <summary>
        /// When true, the drawer overlays content instead of pushing it aside.
        /// </summary>
        public bool OverlayMode
        {
            get => (bool)GetValue(OverlayModeProperty);
            set => SetValue(OverlayModeProperty, value);
        }

        public static readonly DependencyProperty ResponsiveModeProperty =
            DependencyProperty.Register(
                nameof(ResponsiveMode),
                typeof(bool),
                typeof(DaisyDrawer),
                new PropertyMetadata(false, OnResponsiveModeChanged));

        /// <summary>
        /// When true, the drawer automatically opens/closes based on available width.
        /// </summary>
        public bool ResponsiveMode
        {
            get => (bool)GetValue(ResponsiveModeProperty);
            set => SetValue(ResponsiveModeProperty, value);
        }

        public static readonly DependencyProperty ResponsiveThresholdProperty =
            DependencyProperty.Register(
                nameof(ResponsiveThreshold),
                typeof(double),
                typeof(DaisyDrawer),
                new PropertyMetadata(500.0));

        /// <summary>
        /// The width threshold for responsive mode. Drawer opens when width >= threshold.
        /// </summary>
        public double ResponsiveThreshold
        {
            get => (double)GetValue(ResponsiveThresholdProperty);
            set => SetValue(ResponsiveThresholdProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnDrawerWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDrawer drawer && e.NewValue is double width)
            {
                drawer.OpenPaneLength = width;
            }
        }

        private static void OnDrawerSideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDrawer drawer && e.NewValue is SplitViewPanePlacement placement)
            {
                drawer.PanePlacement = placement;
            }
        }

        private static void OnIsDrawerOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDrawer drawer && e.NewValue is bool isOpen)
            {
                drawer.IsPaneOpen = isOpen;
            }
        }

        private static void OnOverlayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDrawer drawer && e.NewValue is bool overlay)
            {
                drawer.DisplayMode = overlay ? SplitViewDisplayMode.Overlay : SplitViewDisplayMode.Inline;
                
                // Re-apply pane state after mode change (mode switch can reset pane state)
                drawer.IsPaneOpen = drawer.IsDrawerOpen;
            }
        }

        private static void OnResponsiveModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDrawer drawer && e.NewValue is bool responsive && responsive)
            {
                // Immediately apply responsive state when mode is enabled
                drawer.UpdateResponsiveState(drawer.ActualWidth);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens the drawer.
        /// </summary>
        public void Open()
        {
            IsDrawerOpen = true;
        }

        /// <summary>
        /// Closes the drawer.
        /// </summary>
        public void Close()
        {
            IsDrawerOpen = false;
        }

        /// <summary>
        /// Toggles the drawer open/closed state.
        /// </summary>
        public void Toggle()
        {
            IsDrawerOpen = !IsDrawerOpen;
        }

        #endregion
    }
}

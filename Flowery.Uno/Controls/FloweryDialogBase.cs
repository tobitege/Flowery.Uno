using System;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Flowery.Helpers;
using Windows.Foundation;
using Windows.System;
using System.Collections.Generic;
using System.Linq;

namespace Flowery.Controls
{
    /// <summary>
    /// Base class for Flowery dialogs providing smart sizing, theming, and standardized layout.
    /// Inherits from DaisyModal for modal hosting and automatic theme change subscription.
    /// </summary>
    /// <remarks>
    /// <para><b>Smart Sizing:</b></para>
    /// <list type="bullet">
    ///   <item>Calculates optimal dialog size based on available window area</item>
    ///   <item>Uses 280x400 as absolute minimum (narrow mobile portrait)</item>
    ///   <item>Uses 420x520 as preferred default (mobile-friendly)</item>
    ///   <item>Can scale up to 840x800 for large screens</item>
    ///   <item>Automatically adjusts ratios for small vs large screens</item>
    /// </list>
    /// <para><b>Usage:</b> Create content using <see cref="CreateDialogContent"/> and set it as Content.</para>
    /// </remarks>
    public partial class FloweryDialogBase : DaisyModal
    {
        #region Size Constants

        /// <summary>Absolute minimum width (narrow mobile portrait).</summary>
        public const double AbsoluteMinWidth = 280;

        /// <summary>Absolute minimum height.</summary>
        public const double AbsoluteMinHeight = 400;

        /// <summary>Preferred default width.</summary>
        public const double PreferredWidth = 420;

        /// <summary>Preferred default height.</summary>
        public const double PreferredHeight = 520;

        /// <summary>Maximum width (approximately 2x preferred).</summary>
        public const double MaxDialogWidth = 840;

        /// <summary>Maximum height.</summary>
        public const double MaxDialogHeight = 800;

        /// <summary>Margin reserved for window chrome/overlay when calculating available space.</summary>
        public const double DialogMargin = 40;

        /// <summary>Padding inside ContentDialog (chrome, borders, scrollbar allowance).</summary>
        public const double ContentDialogChrome = 48;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty DialogWidthProperty =
            DependencyProperty.Register(
                nameof(DialogWidth),
                typeof(double),
                typeof(FloweryDialogBase),
                new PropertyMetadata(PreferredWidth));

        /// <summary>
        /// Gets or sets the dialog width. Set automatically by <see cref="ApplySmartSizing"/>.
        /// </summary>
        public double DialogWidth
        {
            get => (double)GetValue(DialogWidthProperty);
            set => SetValue(DialogWidthProperty, value);
        }

        public static readonly DependencyProperty DialogHeightProperty =
            DependencyProperty.Register(
                nameof(DialogHeight),
                typeof(double),
                typeof(FloweryDialogBase),
                new PropertyMetadata(PreferredHeight));

        /// <summary>
        /// Gets or sets the dialog height. Set automatically by <see cref="ApplySmartSizing"/>.
        /// </summary>
        public double DialogHeight
        {
            get => (double)GetValue(DialogHeightProperty);
            set => SetValue(DialogHeightProperty, value);
        }

        public static readonly DependencyProperty IsOutsideClickDismissEnabledProperty =
            DependencyProperty.Register(
                nameof(IsOutsideClickDismissEnabled),
                typeof(bool),
                typeof(FloweryDialogBase),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether clicking outside the dialog closes it.
        /// Default is false for Flowery dialogs.
        /// </summary>
        public bool IsOutsideClickDismissEnabled
        {
            get => (bool)GetValue(IsOutsideClickDismissEnabledProperty);
            set => SetValue(IsOutsideClickDismissEnabledProperty, value);
        }

        public static readonly DependencyProperty IsCloseOnEnterEnabledProperty =
            DependencyProperty.Register(
                nameof(IsCloseOnEnterEnabled),
                typeof(bool),
                typeof(FloweryDialogBase),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether pressing Enter closes the dialog.
        /// Default is false for Flowery dialogs.
        /// </summary>
        public bool IsCloseOnEnterEnabled
        {
            get => (bool)GetValue(IsCloseOnEnterEnabledProperty);
            set => SetValue(IsCloseOnEnterEnabledProperty, value);
        }

        public static readonly DependencyProperty IsDialogResizingEnabledProperty =
            DependencyProperty.Register(
                nameof(IsDialogResizingEnabled),
                typeof(bool),
                typeof(FloweryDialogBase),
                new PropertyMetadata(false, OnIsDialogResizingEnabledChanged));

        /// <summary>
        /// Gets or sets whether the dialog can be resized within its container.
        /// Default is false for Flowery dialogs.
        /// </summary>
        public bool IsDialogResizingEnabled
        {
            get => (bool)GetValue(IsDialogResizingEnabledProperty);
            set => SetValue(IsDialogResizingEnabledProperty, value);
        }

        protected override double DialogBoundsMargin => IsDialogResizingEnabled ? 0 : base.DialogBoundsMargin;

        public static readonly DependencyProperty IsTabNavigationCycleEnabledProperty =
            DependencyProperty.Register(
                nameof(IsTabNavigationCycleEnabled),
                typeof(bool),
                typeof(FloweryDialogBase),
                new PropertyMetadata(true, OnIsTabNavigationCycleEnabledChanged));

        /// <summary>
        /// Gets or sets whether tab navigation cycles within this dialog.
        /// Default is true to keep focus within the dialog.
        /// </summary>
        public bool IsTabNavigationCycleEnabled
        {
            get => (bool)GetValue(IsTabNavigationCycleEnabledProperty);
            set => SetValue(IsTabNavigationCycleEnabledProperty, value);
        }

        #endregion

        #region Constructor

        private const double ResizeBoundsMargin = 16;
        private const double ResizeGripSize = 22;
        private const double ResizeGripMargin = 0;

        private readonly Grid _resizeHost = new Grid();
        private readonly Grid _tabNavigationHost = new Grid();
        private readonly ContentPresenter _tabContentPresenter = new ContentPresenter();
        private readonly Border _resizeGrip;
        private readonly Path _resizeGripIcon;
        private Panel? _resizeGripHost;
        private FrameworkElement? _resizeSurface;
        private bool _isWrappingContent;
        private bool _isAutoHeight;
        private bool _isResizing;
        private Point _resizeStartPoint;
        private double _resizeStartWidth;
        private double _resizeStartHeight;
        private Point _resizeStartOffset;
        private double _resizeStartLeft;
        private double _resizeStartTop;

        private ContentControl? _firstSentinel;
        private ContentControl? _lastSentinel;

        public FloweryDialogBase()
        {
            _resizeHost.HorizontalAlignment = HorizontalAlignment.Stretch;
            _resizeHost.VerticalAlignment = VerticalAlignment.Stretch;
            _tabNavigationHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _tabNavigationHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _tabNavigationHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(_tabContentPresenter, 1);
            _tabNavigationHost.Children.Add(_tabContentPresenter);
            _resizeHost.Children.Add(_tabNavigationHost);

            _resizeGripIcon = CreateResizeGripIcon();
            _resizeGrip = CreateResizeGrip(_resizeGripIcon);
            EnsureResizeGripHost();

            AddHandler(KeyDownEvent, new KeyEventHandler(OnDialogKeyDown), handledEventsToo: true);
        }

        #endregion

        #region Smart Sizing

        /// <summary>
        /// Calculates optimal dialog dimensions based on available window area.
        /// Uses absolute minimums for narrow screens, scales up to max for large screens.
        /// </summary>
        /// <param name="xamlRoot">The XamlRoot to read window size from.</param>
        /// <returns>Tuple of (Width, Height) for the dialog.</returns>
        public static (double Width, double Height) CalculateOptimalDialogSize(XamlRoot? xamlRoot)
        {
            try
            {
                var windowSize = xamlRoot?.Size ?? default;

                // If XamlRoot unavailable, return preferred
                if (windowSize.Width <= 0 || windowSize.Height <= 0)
                {
                    return (PreferredWidth, PreferredHeight);
                }

                // Available space (window minus margins for dialog overlay)
                var availableWidth = windowSize.Width - DialogMargin;
                var availableHeight = windowSize.Height - DialogMargin;

                // Target 90% of available space on small screens, 60% on large screens
                // This ensures the dialog feels appropriately sized regardless of window
                var widthRatio = availableWidth < 600 ? 0.90 : 0.60;
                var heightRatio = availableHeight < 700 ? 0.85 : 0.65;

                var targetWidth = availableWidth * widthRatio;
                var targetHeight = availableHeight * heightRatio;

                // Clamp to absolute min and max
                var finalWidth = Math.Max(AbsoluteMinWidth, Math.Min(MaxDialogWidth, targetWidth));
                var finalHeight = Math.Max(AbsoluteMinHeight, Math.Min(MaxDialogHeight, targetHeight));

                // Ensure reasonable aspect ratio (1:1.2 to 1:1.8 is ideal for forms)
                var aspectRatio = finalHeight / finalWidth;
                if (aspectRatio < 1.0)
                {
                    // Too wide - constrain width based on height
                    finalWidth = Math.Max(AbsoluteMinWidth, finalHeight / 1.2);
                }
                else if (aspectRatio > 1.8)
                {
                    // Too tall - constrain height based on width
                    finalHeight = finalWidth * 1.5;
                }

                return (Math.Round(finalWidth), Math.Round(finalHeight));
            }
            catch
            {
                // Safe fallback
                return (PreferredWidth, PreferredHeight);
            }
        }

        /// <summary>
        /// Applies smart sizing to this dialog based on the provided XamlRoot.
        /// Call this before showing the dialog to set optimal dimensions.
        /// </summary>
        /// <param name="xamlRoot">The XamlRoot to read window size from.</param>
        public void ApplySmartSizing(XamlRoot? xamlRoot)
        {
            var (width, height) = CalculateOptimalDialogSize(xamlRoot);
            DialogWidth = width;
            DialogHeight = height;
            _isAutoHeight = false;
            SetDialogSize(width, height);
        }

        /// <summary>
        /// Applies smart sizing with auto height. Width is fixed; height shrinks to content up to a cap.
        /// </summary>
        /// <param name="xamlRoot">The XamlRoot to read window size from.</param>
        public void ApplySmartSizingWithAutoHeight(XamlRoot? xamlRoot)
        {
            var (width, height) = CalculateOptimalDialogSize(xamlRoot);
            DialogWidth = width;
            DialogHeight = height;
            _isAutoHeight = true;
            SetDialogSize(width, double.NaN);
        }

        /// <summary>
        /// Clamps the dialog width to a maximum while preserving auto height.
        /// </summary>
        /// <param name="maxWidth">Maximum width for the dialog.</param>
        protected void ClampDialogWidth(double maxWidth)
        {
            var targetWidth = Math.Min(DialogWidth, maxWidth);
            if (targetWidth < DialogWidth)
            {
                DialogWidth = targetWidth;
                SetDialogSize(targetWidth, double.NaN);
            }
        }

        #endregion

        #region Dialog Content Factory

        /// <summary>
        /// Creates a standard dialog layout with header, scrollable content area, and button footer.
        /// </summary>
        /// <param name="xamlRoot">XamlRoot for smart sizing.</param>
        /// <param name="headerContent">Content for the header area (typically title + subtitle).</param>
        /// <param name="mainContent">Main scrollable content.</param>
        /// <param name="footerContent">Footer content (typically buttons).</param>
        /// <returns>A Grid configured as the dialog layout.</returns>
        public static Grid CreateDialogContent(
            XamlRoot? xamlRoot,
            FrameworkElement? headerContent,
            FrameworkElement mainContent,
            FrameworkElement? footerContent)
        {
            var (width, height) = CalculateOptimalDialogSize(xamlRoot);

            var container = new Grid
            {
                Width = width,
                Height = height,
                // Single column forces all content to respect the Grid's width
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },                        // Header
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },   // ScrollViewer
                    new RowDefinition { Height = GridLength.Auto }                         // Footer
                }
            };

            // Header
            if (headerContent != null)
            {
                headerContent.Margin = new Thickness(0, 0, 0, 12);
                Grid.SetRow(headerContent, 0);
                container.Children.Add(headerContent);
            }

            // Scrollable main content
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            // Add right margin for scrollbar clearance
            if (mainContent != null)
            {
                mainContent.Margin = new Thickness(
                    mainContent.Margin.Left,
                    mainContent.Margin.Top,
                    Math.Max(mainContent.Margin.Right, 12), // Ensure at least 12px right margin
                    mainContent.Margin.Bottom);
            }

            scrollViewer.Content = mainContent;
            Grid.SetRow(scrollViewer, 1);
            container.Children.Add(scrollViewer);

            // Footer
            if (footerContent != null)
            {
                footerContent.Margin = new Thickness(0, 12, 0, 0);
                Grid.SetRow(footerContent, 2);
                container.Children.Add(footerContent);
            }

            return container;
        }

        /// <summary>
        /// Creates a standard footer panel with Save and Cancel buttons.
        /// Buttons appear in order: Save, Cancel (left to right, right-aligned).
        /// </summary>
        /// <param name="saveButton">Output: The Save button.</param>
        /// <param name="cancelButton">Output: The Cancel button.</param>
        /// <param name="saveText">Text for the Save button (default: localized "Save").</param>
        /// <param name="cancelText">Text for the Cancel button (default: localized "Cancel").</param>
        /// <returns>A StackPanel configured as the button footer.</returns>
        public static StackPanel CreateStandardButtonFooter(
            out DaisyButton saveButton,
            out DaisyButton cancelButton,
            string? saveText = null,
            string? cancelText = null)
        {
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            saveButton = new DaisyButton
            {
                Content = saveText ?? Localization.FloweryLocalization.GetStringInternal("Common_Save"),
                Variant = DaisyButtonVariant.Success,
                Size = Enums.DaisySize.Medium,
                MinWidth = 80,
                IsTabStop = true,
                UseSystemFocusVisuals = true
            };

            cancelButton = new DaisyButton
            {
                Content = cancelText ?? Localization.FloweryLocalization.GetStringInternal("Common_Cancel"),
                Variant = DaisyButtonVariant.Error,
                Size = Enums.DaisySize.Medium,
                MinWidth = 80,
                IsTabStop = true,
                UseSystemFocusVisuals = true
            };

            // Order: Save, Cancel (left to right)
            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);

            return buttonPanel;
        }

        #endregion

        #region Resize Handling

        private static void OnIsDialogResizingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryDialogBase dialog)
            {
                dialog.UpdateResizeGripState();
            }
        }

        private static void OnIsTabNavigationCycleEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloweryDialogBase dialog)
            {
                dialog.UpdateTabNavigationState();
            }
        }

        private Border CreateResizeGrip(Path icon)
        {
            var grip = new Border
            {
                Width = ResizeGripSize,
                Height = ResizeGripSize,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, ResizeGripMargin, ResizeGripMargin),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(2),
                Opacity = 1.0,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            grip.Child = icon;
            Canvas.SetZIndex(grip, 10);
            grip.PointerPressed += OnResizeGripPointerPressed;
            grip.PointerMoved += OnResizeGripPointerMoved;
            grip.PointerReleased += OnResizeGripPointerReleased;
            grip.PointerCanceled += OnResizeGripPointerCanceled;
            grip.PointerCaptureLost += OnResizeGripPointerCaptureLost;

            return grip;
        }

        private void EnsureResizeGripHost()
        {
            var targetHost = DialogOverlayLayer ?? _resizeHost;
            if (!ReferenceEquals(_resizeGripHost, targetHost))
            {
                if (_resizeGrip.Parent is Panel currentParent)
                {
                    currentParent.Children.Remove(_resizeGrip);
                }

                targetHost.Children.Add(_resizeGrip);
                _resizeGripHost = targetHost;
            }

            _resizeSurface = targetHost;
        }

        private static Path CreateResizeGripIcon()
        {
            const double iconSize = 12;
            const double iconMargin = 4;

            return new Path
            {
                Data = FloweryPathHelpers.ParseGeometry("M4,18 L18,4 M8,18 L18,8 M12,18 L18,12"),
                StrokeThickness = 1.5,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Width = iconSize,
                Height = iconSize,
                Margin = new Thickness(iconMargin)
            };
        }

        private void UpdateResizeGripState()
        {
            if (_resizeGrip == null)
                return;

            EnsureResizeGripHost();

            var isEnabled = IsDialogResizingEnabled;
            _resizeGrip.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
            _resizeGrip.IsHitTestVisible = isEnabled;

            if (ReferenceEquals(_resizeGripHost, DialogOverlayLayer))
            {
                _resizeGripHost.IsHitTestVisible = isEnabled;
            }

            if (!isEnabled)
            {
                StopResize();
            }
        }

        private void OnResizeGripPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsDialogResizingEnabled || !IsOpen)
                return;

            var resizeSurface = _resizeSurface ?? _resizeHost;
            var point = e.GetCurrentPoint(resizeSurface);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            _isResizing = true;
            _resizeStartPoint = point.Position;
            _resizeStartWidth = GetEffectiveDialogWidth();
            _resizeStartHeight = GetEffectiveDialogHeight();
            _resizeStartOffset = DialogOffset;
            _resizeStartLeft = GetResizeStartLeft(_resizeStartWidth, _resizeStartOffset.X);
            _resizeStartTop = GetResizeStartTop(_resizeStartHeight, _resizeStartOffset.Y);
            _resizeGrip.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void OnResizeGripPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizing)
                return;

            var resizeSurface = _resizeSurface ?? _resizeHost;
            var point = e.GetCurrentPoint(resizeSurface).Position;
            var deltaX = point.X - _resizeStartPoint.X;
            var deltaY = point.Y - _resizeStartPoint.Y;

            var targetWidth = _resizeStartWidth + deltaX;
            var targetHeight = _resizeStartHeight + deltaY;

            var maxWidth = GetMaxResizeWidth();
            var maxHeight = GetMaxResizeHeight();
            var minWidth = Math.Min(AbsoluteMinWidth, maxWidth);
            var minHeight = Math.Min(AbsoluteMinHeight, maxHeight);

            var clampedWidth = Math.Clamp(targetWidth, minWidth, maxWidth);
            var clampedHeight = Math.Clamp(targetHeight, minHeight, maxHeight);

            DialogWidth = clampedWidth;
            DialogHeight = clampedHeight;
            _isAutoHeight = false;
            SetDialogSize(clampedWidth, clampedHeight);
            UpdateDialogOffsetForResize(clampedWidth, clampedHeight);
            e.Handled = true;
        }

        private void OnResizeGripPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            StopResize();
        }

        private void OnResizeGripPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            StopResize();
        }

        private void OnResizeGripPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            StopResize();
        }

        private void StopResize()
        {
            if (!_isResizing)
                return;

            _isResizing = false;
            _resizeGrip.ReleasePointerCaptures();
        }

        private double GetEffectiveDialogWidth()
        {
            if (!double.IsNaN(DialogWidth) && DialogWidth > 0)
                return DialogWidth;

            var resizeSurface = _resizeSurface ?? _resizeHost;
            var width = resizeSurface.ActualWidth;
            return width > 0 ? width : PreferredWidth;
        }

        private double GetEffectiveDialogHeight()
        {
            if (!_isAutoHeight && !double.IsNaN(DialogHeight) && DialogHeight > 0)
                return DialogHeight;

            var resizeSurface = _resizeSurface ?? _resizeHost;
            var height = resizeSurface.ActualHeight;
            if (height > 0)
                return height;

            if (!double.IsNaN(DialogHeight) && DialogHeight > 0)
                return DialogHeight;

            return PreferredHeight;
        }

        private double GetMaxResizeWidth()
        {
            var hostWidth = ActualWidth;
            if (hostWidth <= 0)
                return MaxDialogWidth;

            var margin = _isResizing ? 0 : ResizeBoundsMargin;
            var max = _isResizing
                ? hostWidth - margin - _resizeStartLeft
                : hostWidth - margin * 2;
            if (max <= 0)
                return AbsoluteMinWidth;

            return _isResizing ? Math.Max(AbsoluteMinWidth, max) : Math.Min(MaxDialogWidth, max);
        }

        private double GetMaxResizeHeight()
        {
            var hostHeight = ActualHeight;
            if (hostHeight <= 0)
                return MaxDialogHeight;

            var margin = _isResizing ? 0 : ResizeBoundsMargin;
            var max = _isResizing
                ? hostHeight - margin - _resizeStartTop
                : hostHeight - margin * 2;
            if (max <= 0)
                return AbsoluteMinHeight;

            return _isResizing ? Math.Max(AbsoluteMinHeight, max) : Math.Min(MaxDialogHeight, max);
        }

        private double GetResizeStartLeft(double dialogWidth, double offsetX)
        {
            var hostWidth = ActualWidth;
            if (hostWidth <= 0)
                return offsetX;

            return (hostWidth - dialogWidth) / 2 + offsetX;
        }

        private double GetResizeStartTop(double dialogHeight, double offsetY)
        {
            var hostHeight = ActualHeight;
            if (hostHeight <= 0)
                return offsetY;

            return (hostHeight - dialogHeight) / 2 + offsetY;
        }

        private void UpdateDialogOffsetForResize(double newWidth, double newHeight)
        {
            var deltaWidth = newWidth - _resizeStartWidth;
            var deltaHeight = newHeight - _resizeStartHeight;
            DialogOffset = new Point(
                _resizeStartOffset.X + deltaWidth / 2,
                _resizeStartOffset.Y + deltaHeight / 2);
        }

        #endregion

        #region Theming

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyTheming();
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            ApplyTheming();
            UpdateResizeGripState();
            UpdateTabNavigationState();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            RemoveHandler(KeyDownEvent, new KeyEventHandler(OnDialogKeyDown));
        }

        /// <summary>
        /// Applies theme colors to the dialog. Override to customize.
        /// </summary>
        protected virtual void ApplyTheming()
        {
            Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");

            if (_resizeGrip != null)
            {
                _resizeGrip.Background = Background ?? DaisyResourceLookup.GetBrush("DaisyBase100Brush");
                _resizeGrip.BorderBrush = new SolidColorBrush(Colors.Transparent);
                _resizeGrip.Opacity = 1.0;
            }

            if (_resizeGripIcon != null)
            {
                _resizeGripIcon.Stroke = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
                _resizeGripIcon.Opacity = 0.65;
            }
        }

        protected override bool ShouldCloseOnOutsideClick() => IsOutsideClickDismissEnabled;

        protected virtual bool OnEnterKeyRequested()
        {
            IsOpen = false;
            return true;
        }

        #endregion

        #region Input Handling

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            if (_isWrappingContent || ReferenceEquals(newContent, _resizeHost) || ReferenceEquals(newContent, base.Content))
            {
                base.OnContentChanged(oldContent, newContent);
                return;
            }

            if (newContent is UIElement element)
            {
                _tabContentPresenter.Content = element;
                EnsureResizeGripHost();

                UpdateTabNavigationState();

                _isWrappingContent = true;
                base.OnContentChanged(oldContent, _resizeHost);
                _isWrappingContent = false;
                return;
            }

            base.OnContentChanged(oldContent, newContent);
        }

        private void UpdateTabNavigationState()
        {
            var mode = IsTabNavigationCycleEnabled
                ? KeyboardNavigationMode.Cycle
                : KeyboardNavigationMode.Local;

            TabFocusNavigation = mode;
            if (_resizeHost != null)
            {
                _resizeHost.TabFocusNavigation = mode;
            }
            _tabNavigationHost.TabFocusNavigation = mode;

            EnsureSentinels();
        }

        private void EnsureSentinels()
        {
            if (!IsTabNavigationCycleEnabled || _tabNavigationHost == null)
                return;

            if (_firstSentinel == null)
            {
                _firstSentinel = CreateFocusSentinel(tabIndex: -10000);
                _firstSentinel.GotFocus += (s, e) => CycleFocus(forward: false);
            }
            if (_lastSentinel == null)
            {
                _lastSentinel = CreateFocusSentinel(tabIndex: 10000);
                _lastSentinel.GotFocus += (s, e) => CycleFocus(forward: true);
            }

            if (!_tabNavigationHost.Children.Contains(_firstSentinel))
            {
                Grid.SetRow(_firstSentinel, 0);
                _tabNavigationHost.Children.Add(_firstSentinel);
            }

            if (!_tabNavigationHost.Children.Contains(_lastSentinel))
            {
                Grid.SetRow(_lastSentinel, 2);
                _tabNavigationHost.Children.Add(_lastSentinel);
            }
        }

        private void CycleFocus(bool forward)
        {
            var focusables = GetFocusableControls(_tabNavigationHost)
                .Where(c => !ReferenceEquals(c, _firstSentinel) && !ReferenceEquals(c, _lastSentinel))
                .ToList();

            if (focusables.Count > 0)
            {
                var target = forward ? focusables[0] : focusables[^1];
                target.Focus(FocusState.Programmatic);
            }
        }

        private void OnDialogKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!IsOpen)
                return;

            if (e.Key == VirtualKey.Tab && IsTabNavigationCycleEnabled)
            {
                if (HandleTabNavigation())
                {
                    e.Handled = true;
                    return;
                }
            }

            if (e.Handled)
                return;

            if (e.Key == VirtualKey.Enter && IsCloseOnEnterEnabled)
            {
                if (OnEnterKeyRequested())
                {
                    e.Handled = true;
                }
            }
        }

        private bool HandleTabNavigation()
        {
            var scope = _tabNavigationHost;
            var focusables = GetFocusableControls(scope);
            if (focusables.Count <= 2) // Only sentinels
                return false;

            var focused = XamlRoot == null
                ? null
                : FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;

            var current = FindFocusedControl(focused, focusables);
            var currentIndex = current != null ? focusables.IndexOf(current) : -1;
            var movePrevious = IsShiftPressed();
            int nextIndex;

            if (movePrevious)
            {
                nextIndex = currentIndex <= 0 ? focusables.Count - 1 : currentIndex - 1;
            }
            else
            {
                nextIndex = currentIndex < 0 || currentIndex >= focusables.Count - 1
                    ? 0
                    : currentIndex + 1;
            }

            var target = focusables[nextIndex];
            if (ReferenceEquals(target, _firstSentinel))
            {
                target = focusables[focusables.Count - 2]; // Skip to last real
            }
            else if (ReferenceEquals(target, _lastSentinel))
            {
                target = focusables[1]; // Skip to first real
            }

            return target.Focus(FocusState.Programmatic);
        }

        private static Control? FindFocusedControl(
            DependencyObject? focused,
            IReadOnlyList<Control> focusables)
        {
            if (focused == null || focusables.Count == 0)
                return null;

            var current = focused;
            while (current != null)
            {
                if (current is Control control && focusables.Contains(control))
                    return control;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static List<Control> GetFocusableControls(DependencyObject root)
        {
            var indexed = new List<(Control control, int tabIndex, int order)>();
            var order = 0;

            foreach (var element in EnumerateVisualTree(root))
            {
                if (element is Control control && IsFocusableControl(control))
                {
                    indexed.Add((control, control.TabIndex, order++));
                }
            }

            return indexed
                .OrderBy(entry => entry.tabIndex)
                .ThenBy(entry => entry.order)
                .Select(entry => entry.control)
                .ToList();
        }

        private static bool IsFocusableControl(Control control)
        {
            if (!control.IsTabStop || !control.IsEnabled)
                return false;

            return control.Visibility == Visibility.Visible && control.IsHitTestVisible;
        }

        private static ContentControl CreateFocusSentinel(int tabIndex)
        {
            var sentinel = new ContentControl
            {
                IsTabStop = true,
                TabIndex = tabIndex,
                IsEnabled = true,
                IsHitTestVisible = true,
                Opacity = 0,
                Width = 1,
                Height = 1,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            AutomationProperties.SetName(sentinel, "Dialog focus boundary");
            return sentinel;
        }

        private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
        {
            yield return root;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                foreach (var descendant in EnumerateVisualTree(child))
                {
                    yield return descendant;
                }
            }
        }

        private static bool IsShiftPressed()
        {
            var state = Microsoft.UI.Input.InputKeyboardSource
                .GetKeyStateForCurrentThread(VirtualKey.Shift);
            return (state & Windows.UI.Core.CoreVirtualKeyStates.Down)
                == Windows.UI.Core.CoreVirtualKeyStates.Down;
        }

        #endregion
    }
}

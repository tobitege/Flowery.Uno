using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using Microsoft.UI.Input;
using Windows.Foundation.Collections;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// Specifies when the resize grip should be visible.
    /// </summary>
    public enum DaisyGripVisibility
    {
        /// <summary>
        /// Show grip only when hovering near a column gap with a mouse.
        /// </summary>
        Auto,
        /// <summary>
        /// Always show grips for all column gaps (not implemented in this version, behaves like Auto).
        /// </summary>
        Visible,
        /// <summary>
        /// Never show the resize grip.
        /// </summary>
        Hidden
    }

    /// <summary>
    /// A reusable host for multi-column layouts that provides a shared resize grip between columns.
    /// Designed to decouple column widths from content and centralize resize logic.
    /// </summary>
    public partial class DaisyResizableColumnsHost : ItemsControl
    {
        private const double KeyboardStep = 8.0;
        private const double LargeKeyboardStep = 32.0;
        private const string DefaultAccessibleText = "Resizable columns";

        private Canvas? _gripCanvas;
        private Thumb? _gripThumb;
        private Grid? _gripVisual;
        private Grid? _dropRoot;
        private Panel? _itemsPanel;
        private long _allowDropChangedToken;
        private bool _dropHandlersAttached;

        private int _currentGripGapIndex = -1;
        private bool _isDragging;
        private bool _isPointerHovering;
        private Windows.Foundation.Point _lastPointerPosition;
        private Windows.Foundation.Point _lastAppliedPointerPosition;
        private bool _hasAppliedPointerPosition;
        private DispatcherQueueTimer? _hoverUpdateTimer;
        private bool _isHoverTrackingAttached;
        private bool _itemsVectorSubscribed;

        /// <summary>
        /// Occurs when the column width is changed via resizing.
        /// </summary>
        public event EventHandler<double>? ColumnWidthChanged;

        /// <summary>
        /// Occurs when a resize drag operation starts.
        /// </summary>
        public event EventHandler? ResizeDragStarted;

        /// <summary>
        /// Occurs when a resize drag operation completes.
        /// </summary>
        public event EventHandler? ResizeDragCompleted;

        public DaisyResizableColumnsHost()
        {
            DefaultStyleKey = typeof(DaisyResizableColumnsHost);

            IsTabStop = true;
            UseSystemFocusVisuals = true;
            IsEnabledChanged += OnIsEnabledChanged;
            _allowDropChangedToken = RegisterPropertyChangedCallback(AllowDropProperty, OnAllowDropChanged);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            KeyDown += OnKeyDown;
            LayoutUpdated += OnLayoutUpdated;
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            // If we don't have the panel yet, try to find it
            if (_itemsPanel == null)
            {
                ApplyAll();
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            DetachDropEventForwarders();

            _gripCanvas = GetTemplateChild("PART_GripCanvas") as Canvas;
            _dropRoot = GetTemplateChild("PART_DropRoot") as Grid;
            if (_gripCanvas != null)
            {
                UpdateDropAllowDrop();
            }

            // Try to find the items panel root if available
            _itemsPanel = ItemsPanelRoot;

            if (_gripCanvas != null)
            {
                BuildGripElements();
            }

            AttachDropEventForwarders();
            ApplyAll();
            UpdateHoverTrackingState();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_itemsPanel == null)
                _itemsPanel = ItemsPanelRoot;

            if (!_itemsVectorSubscribed && Items is IObservableVector<object> observable)
            {
                observable.VectorChanged += OnItemsVectorChanged;
                _itemsVectorSubscribed = true;
            }

            UpdateDropAllowDrop();
            AttachDropEventForwarders();
            ApplyAll();
            UpdateInteractionState();
            UpdateAutomationProperties();
            UpdateHoverTrackingState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _hoverUpdateTimer?.Stop();
            DetachHoverTracking();
            if (_itemsVectorSubscribed && Items is IObservableVector<object> observable)
            {
                observable.VectorChanged -= OnItemsVectorChanged;
                _itemsVectorSubscribed = false;
            }
            if (_gripThumb != null)
            {
                _gripThumb.DragStarted -= OnGripDragStarted;
                _gripThumb.DragDelta -= OnGripDragDelta;
                _gripThumb.DragCompleted -= OnGripDragCompleted;
                _gripThumb.PointerPressed -= OnGripPointerPressed;
            }
            DetachDropEventForwarders();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGripPosition();
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateInteractionState();
        }

        private void OnAllowDropChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateDropAllowDrop();
        }

        private void UpdateDropAllowDrop()
        {
            if (_gripCanvas != null)
            {
                _gripCanvas.AllowDrop = AllowDrop;
            }
            if (_itemsPanel != null)
            {
                _itemsPanel.AllowDrop = AllowDrop;
            }
            if (_dropRoot != null)
            {
                _dropRoot.AllowDrop = AllowDrop;
            }
        }

        private void AttachDropEventForwarders()
        {
            if (_dropHandlersAttached)
                return;

            AttachDropEventForwarder(_dropRoot);
            AttachDropEventForwarder(_itemsPanel);
            AttachDropEventForwarder(_gripCanvas);
            _dropHandlersAttached = true;
        }

        private void DetachDropEventForwarders()
        {
            if (!_dropHandlersAttached)
                return;

            DetachDropEventForwarder(_dropRoot);
            DetachDropEventForwarder(_itemsPanel);
            DetachDropEventForwarder(_gripCanvas);
            _dropHandlersAttached = false;
        }

        private void AttachDropEventForwarder(UIElement? element)
        {
            if (element == null)
                return;

            element.AllowDrop = AllowDrop;
            element.DragOver += OnForwardedDragOver;
            element.Drop += OnForwardedDrop;
            element.DragLeave += OnForwardedDragLeave;
        }

        private void DetachDropEventForwarder(UIElement? element)
        {
            if (element == null)
                return;

            element.DragOver -= OnForwardedDragOver;
            element.Drop -= OnForwardedDrop;
            element.DragLeave -= OnForwardedDragLeave;
        }

        private void OnForwardedDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            OnDragOver(e);
        }

        private void OnForwardedDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            OnDrop(e);
        }

        private void OnForwardedDragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            OnDragLeave(e);
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Handled || !IsEnabled || !IsGripEnabled)
            {
                return;
            }

            var step = e.Key is VirtualKey.PageUp or VirtualKey.PageDown ? LargeKeyboardStep : KeyboardStep;
            var handled = e.Key switch
            {
                VirtualKey.Left when Orientation == Orientation.Horizontal => AdjustColumnWidth(-step),
                VirtualKey.Right when Orientation == Orientation.Horizontal => AdjustColumnWidth(step),
                VirtualKey.Up when Orientation == Orientation.Vertical => AdjustColumnWidth(-step),
                VirtualKey.Down when Orientation == Orientation.Vertical => AdjustColumnWidth(step),
                VirtualKey.Home => SetColumnWidth(MinColumnWidth),
                VirtualKey.End => SetColumnWidth(MaxColumnWidth),
                VirtualKey.PageUp when Orientation == Orientation.Horizontal => AdjustColumnWidth(step),
                VirtualKey.PageDown when Orientation == Orientation.Horizontal => AdjustColumnWidth(-step),
                VirtualKey.PageUp when Orientation == Orientation.Vertical => AdjustColumnWidth(step),
                VirtualKey.PageDown when Orientation == Orientation.Vertical => AdjustColumnWidth(-step),
                _ => false
            };

            if (handled)
            {
                e.Handled = true;
            }
        }

        private void BuildGripElements()
        {
            if (_gripCanvas == null) return;

            _gripThumb = new Thumb
            {
                Width = 12,
                Height = double.NaN, // Stretch
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Opacity = 0.5, // Invisible but present for hit testing
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _gripThumb.DragStarted += OnGripDragStarted;
            _gripThumb.DragDelta += OnGripDragDelta;
            _gripThumb.DragCompleted += OnGripDragCompleted;
            _gripThumb.PointerPressed += OnGripPointerPressed;

            _gripVisual = CreateGripVisual();
            _gripVisual.IsHitTestVisible = false;
            _gripVisual.Opacity = 0; // Hidden by default

            _gripCanvas.Children.Add(_gripVisual);
            _gripCanvas.Children.Add(_gripThumb);
        }

        private Grid CreateGripVisual()
        {
            var grid = new Grid { Width = 12 };

            // Vertical line
            var line = new Rectangle
            {
                Width = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Fill = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"),
                Opacity = 0.8
            };
            grid.Children.Add(line);

            // Handle
            var handle = new Border
            {
                Width = 12,
                Height = 32,
                CornerRadius = new CornerRadius(6),
                Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush"),
                BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = CreateGripIcon("DaisyIconEllipsis")
            };
            grid.Children.Add(handle);

            return grid;
        }

        private static Viewbox CreateGripIcon(string iconKey)
        {
            var pathData = FloweryPathHelpers.GetIconPathData(iconKey);
            var path = new Path
            {
                Stretch = Stretch.Uniform,
                Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                Opacity = 0.8
            };

            if (!string.IsNullOrEmpty(pathData))
            {
                FloweryPathHelpers.TrySetPathData(path, () => FloweryPathHelpers.ParseGeometry(pathData));
            }

            return new Viewbox
            {
                Width = 8,
                Height = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = path
            };
        }

        #region Dependency Properties

        public static readonly DependencyProperty ColumnWidthProperty =
            DependencyProperty.Register(
                nameof(ColumnWidth),
                typeof(double),
                typeof(DaisyResizableColumnsHost),
                new PropertyMetadata(250.0, OnColumnWidthChanged));

        public double ColumnWidth
        {
            get => (double)GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, Math.Clamp(value, MinColumnWidth, MaxColumnWidth));
        }

        private static void OnColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyResizableColumnsHost host)
            {
                host.ApplyAll();
                host.UpdateGripPosition();
                host.ColumnWidthChanged?.Invoke(host, (double)e.NewValue);
            }
        }

        public static readonly DependencyProperty MinColumnWidthProperty =
            DependencyProperty.Register(
                nameof(MinColumnWidth),
                typeof(double),
                typeof(DaisyResizableColumnsHost),
                new PropertyMetadata(100.0));

        public double MinColumnWidth
        {
            get => (double)GetValue(MinColumnWidthProperty);
            set => SetValue(MinColumnWidthProperty, value);
        }

        public static readonly DependencyProperty MaxColumnWidthProperty =
            DependencyProperty.Register(
                nameof(MaxColumnWidth),
                typeof(double),
                typeof(DaisyResizableColumnsHost),
                new PropertyMetadata(1000.0));

        public double MaxColumnWidth
        {
            get => (double)GetValue(MaxColumnWidthProperty);
            set => SetValue(MaxColumnWidthProperty, value);
        }

        public static readonly DependencyProperty ColumnSpacingProperty =
            DependencyProperty.Register(
                nameof(ColumnSpacing),
                typeof(double),
                typeof(DaisyResizableColumnsHost),
                new PropertyMetadata(0.0, OnAppearanceChanged));

        public double ColumnSpacing
        {
            get => (double)GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        public static readonly DependencyProperty GripVisibilityProperty =
            DependencyProperty.Register(
                nameof(GripVisibility),
                typeof(DaisyGripVisibility),
                typeof(DaisyResizableColumnsHost),
                new PropertyMetadata(DaisyGripVisibility.Auto, OnAppearanceChanged));

        public DaisyGripVisibility GripVisibility
        {
            get => (DaisyGripVisibility)GetValue(GripVisibilityProperty);
            set => SetValue(GripVisibilityProperty, value);
        }

        public static readonly DependencyProperty IsGripEnabledProperty =
            DependencyProperty.Register(
                nameof(IsGripEnabled),
                typeof(bool),
                typeof(DaisyResizableColumnsHost),
                new PropertyMetadata(true, OnGripEnabledChanged));

        /// <summary>
        /// Enables the column sizing grip interaction when true.
        /// </summary>
        public bool IsGripEnabled
        {
            get => (bool)GetValue(IsGripEnabledProperty);
            set => SetValue(IsGripEnabledProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyResizableColumnsHost),
                new PropertyMetadata(Orientation.Horizontal, OnAppearanceChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyResizableColumnsHost),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyResizableColumnsHost host)
            {
                host.UpdateAutomationProperties();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyResizableColumnsHost host)
            {
                host.ApplyAll();
                host.UpdateGripPosition();
                host.UpdateHoverTrackingState();
            }
        }

        private static void OnGripEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyResizableColumnsHost host)
            {
                host.UpdateInteractionState();
                host.UpdateHoverTrackingState();
            }
        }

        #endregion

        private void ApplyAll()
        {
            if (_itemsPanel == null)
            {
                _itemsPanel = ItemsPanelRoot;
            }

            // Fallback: try to find the panel in the visual tree if ItemsPanelRoot is null
            if (_itemsPanel == null)
            {
                _itemsPanel = FindItemsPanel(this);
            }

            if (_itemsPanel is StackPanel sp)
            {
                if (sp.Orientation != Orientation) sp.Orientation = Orientation;
                if (Math.Abs(sp.Spacing - ColumnSpacing) > 0.001) sp.Spacing = ColumnSpacing;
            }
            else if (_itemsPanel is ItemsStackPanel isp)
            {
                if (isp.Orientation != Orientation) isp.Orientation = Orientation;
            }

            // Update hover tracking state in case panel or items changed
            UpdateHoverTrackingState();

            // In a real ItemsControl, the items are governed by the ItemsPanel.
            // We expect the consumer to bind their item widths to ColumnWidth or for the host
            // to provide a way to enforce it.
            // For simplicity in this host, we'll assume the ItemsPanel is a StackPanel
            // and we might need to enforce widths on children if they aren't bound.
        }

        private Panel? FindItemsPanel(DependencyObject parent)
        {
            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is ItemsPresenter presenter)
                {
                    // The child of ItemsPresenter is usually the panel
                    int presenterChildren = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(presenter);
                    if (presenterChildren > 0)
                    {
                        var panel = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(presenter, 0) as Panel;
                        if (panel != null) return panel;
                    }
                }

                // If it's already a StackPanel and we are inside the template, it might be our panel
                if (child is StackPanel sp && parent is not DaisyResizableColumnsHost)
                {
                    // In our template, the ItemsPresenter will eventually contain a StackPanel
                    // If we find a StackPanel that isn't the control itself, it's a good candidate
                    return sp;
                }

                var result = FindItemsPanel(child);
                if (result != null) return result;
            }
            return null;
        }

        private void OnItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            UpdateHoverTrackingState();
            if (_currentGripGapIndex != -1)
            {
                UpdateGripPosition();
            }
        }

        private void UpdateHoverTrackingState()
        {
            var shouldAttach = IsGripEnabled
                && GripVisibility != DaisyGripVisibility.Hidden
                && Items.Count >= 2;

            if (shouldAttach)
            {
                AttachHoverTracking();
            }
            else
            {
                _hoverUpdateTimer?.Stop();
                HideGrip();
                DetachHoverTracking();
            }
        }

        private void AttachHoverTracking()
        {
            if (_isHoverTrackingAttached)
                return;

            PointerMoved += OnPointerMoved;
            PointerExited += OnPointerExited;
            _isHoverTrackingAttached = true;
        }

        private void DetachHoverTracking()
        {
            if (!_isHoverTrackingAttached)
                return;

            PointerMoved -= OnPointerMoved;
            PointerExited -= OnPointerExited;
            _isHoverTrackingAttached = false;
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging || GripVisibility == DaisyGripVisibility.Hidden)
                return;

            if (_itemsPanel == null || Items.Count < 2)
                return;

            var ptr = e.GetCurrentPoint(this);
            if (ptr.PointerDeviceType != PointerDeviceType.Mouse)
                return;

            _isPointerHovering = true;
            _lastPointerPosition = ptr.Position;
            ScheduleHoverUpdate();
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerHovering = false;
            _hoverUpdateTimer?.Stop();
            if (!_isDragging)
            {
                HideGrip();
            }
        }

        private void ScheduleHoverUpdate()
        {
            var dispatcher = DispatcherQueue;
            if (dispatcher == null)
            {
                ApplyHoverUpdate();
                return;
            }

            _hoverUpdateTimer ??= dispatcher.CreateTimer();
            _hoverUpdateTimer.Stop();
            _hoverUpdateTimer.Interval = TimeSpan.FromMilliseconds(80);
            _hoverUpdateTimer.Tick -= OnHoverUpdateTimerTick;
            _hoverUpdateTimer.Tick += OnHoverUpdateTimerTick;
            _hoverUpdateTimer.Start();
        }

        private void OnHoverUpdateTimerTick(DispatcherQueueTimer sender, object args)
        {
            sender.Stop();
            ApplyHoverUpdate();
        }

        private void ApplyHoverUpdate()
        {
            if (!_isPointerHovering || _isDragging || GripVisibility == DaisyGripVisibility.Hidden)
            {
                HideGrip();
                return;
            }

            if (_itemsPanel == null || Items.Count < 2)
            {
                HideGrip();
                return;
            }

            if (_hasAppliedPointerPosition)
            {
                var deltaX = _lastPointerPosition.X - _lastAppliedPointerPosition.X;
                var deltaY = _lastPointerPosition.Y - _lastAppliedPointerPosition.Y;
                if ((deltaX * deltaX) + (deltaY * deltaY) < 4)
                    return;
            }

            _lastAppliedPointerPosition = _lastPointerPosition;
            _hasAppliedPointerPosition = true;
            UpdateHoverGrip(_lastPointerPosition);
        }

        private void UpdateHoverGrip(Windows.Foundation.Point position)
        {
            if (_itemsPanel == null || Items.Count < 2)
            {
                HideGrip();
                return;
            }

            double nearestDistance = double.MaxValue;
            int nearestGap = -1;
            double gapPos = 0;

            // Enumerate gaps between items
            for (int i = 0; i < Items.Count - 1; i++)
            {
                var container = ContainerFromIndex(i) as FrameworkElement;
                if (container == null || container.ActualWidth <= 0) continue;

                // Get container bounds relative to the host
                var transform = container.TransformToVisual(this);
                var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));

                double gapCenter = (Orientation == Orientation.Horizontal)
                    ? bounds.Right + (ColumnSpacing / 2)
                    : bounds.Bottom + (ColumnSpacing / 2);

                double dist = (Orientation == Orientation.Horizontal)
                    ? Math.Abs(position.X - gapCenter)
                    : Math.Abs(position.Y - gapCenter);

                if (dist < 20 && dist < nearestDistance) // 20px hover threshold
                {
                    nearestDistance = dist;
                    nearestGap = i;
                    gapPos = gapCenter;
                }
            }

            if (nearestGap != -1)
            {
                ShowGrip(nearestGap, gapPos);
            }
            else
            {
                HideGrip();
            }
        }

        private void ShowGrip(int gapIndex, double position)
        {
            if (_gripVisual == null || _gripThumb == null || _gripCanvas == null) return;

            _currentGripGapIndex = gapIndex;
            _gripVisual.Opacity = 1;
            _gripCanvas.IsHitTestVisible = true; // Enable canvas for the Thumb to work

            const double gripWidth = 12;
            const double thumbWidth = 24;

            double hostWidth = ActualWidth;
            double hostHeight = ActualHeight;

            if (Orientation == Orientation.Horizontal)
            {
                Canvas.SetLeft(_gripVisual, position - (gripWidth / 2));
                Canvas.SetTop(_gripVisual, 0);
                _gripVisual.Height = hostHeight;
                _gripVisual.Width = gripWidth;

                Canvas.SetLeft(_gripThumb, position - (thumbWidth / 2));
                Canvas.SetTop(_gripThumb, 0);
                _gripThumb.Height = hostHeight;
                _gripThumb.Width = thumbWidth; // Wider hit area for the thumb
            }
            else
            {
                Canvas.SetTop(_gripVisual, position - (gripWidth / 2));
                Canvas.SetLeft(_gripVisual, 0);
                _gripVisual.Width = hostWidth;
                _gripVisual.Height = gripWidth;

                Canvas.SetTop(_gripThumb, position - (thumbWidth / 2));
                Canvas.SetLeft(_gripThumb, 0);
                _gripThumb.Width = hostWidth;
                _gripThumb.Height = thumbWidth;
            }
        }

        private void HideGrip()
        {
            if (_gripVisual != null) _gripVisual.Opacity = 0;
            if (_gripCanvas != null) _gripCanvas.IsHitTestVisible = false;
            _currentGripGapIndex = -1;
        }

        private void UpdateGripPosition()
        {
            if (_isDragging || _currentGripGapIndex == -1)
                return;

            // Recalculate based on current layout
            if (_currentGripGapIndex >= 0 && _currentGripGapIndex < Items.Count - 1)
            {
                var container = ContainerFromIndex(_currentGripGapIndex) as FrameworkElement;
                if (container != null)
                {
                    var transform = container.TransformToVisual(this);
                    var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));
                    double gapCenter = (Orientation == Orientation.Horizontal)
                        ? bounds.Right + (ColumnSpacing / 2)
                        : bounds.Bottom + (ColumnSpacing / 2);

                    ShowGrip(_currentGripGapIndex, gapCenter);
                }
            }
        }

        private void OnGripDragStarted(object sender, DragStartedEventArgs e)
        {
            _isDragging = true;
            ResizeDragStarted?.Invoke(this, EventArgs.Empty);
        }

        private void OnGripDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_currentGripGapIndex < 0 || Items.Count < 2)
            {
                return;
            }

            if (Orientation == Orientation.Horizontal)
            {
                double change = e.HorizontalChange;
                // If we are dragging the gap after index i, and all columns have the same width,
                // the change in total width up to this gap is shared among the i+1 columns.
                // But the requirement is "resize all columns at once".
                // This means changing ColumnWidth directly.

                // If we drag the grip by DX, and it's at index 'n' (0-based gap),
                // the total width of 'n+1' columns increases by DX.
                // So ColumnWidth increases by DX / (n + 1).

                var beforeWidth = ColumnWidth;
                double newWidth = beforeWidth + (change / (_currentGripGapIndex + 1));
                ColumnWidth = newWidth;

                var appliedChange = (ColumnWidth - beforeWidth) * (_currentGripGapIndex + 1);
                MoveGripBy(appliedChange);
                if (Math.Abs(appliedChange - change) > 0.001)
                {
                    UpdateGripPosition();
                }
            }
            else
            {
                double change = e.VerticalChange;
                var beforeWidth = ColumnWidth;
                double newWidth = beforeWidth + (change / (_currentGripGapIndex + 1));
                ColumnWidth = newWidth;

                var appliedChange = (ColumnWidth - beforeWidth) * (_currentGripGapIndex + 1);
                MoveGripBy(appliedChange);
                if (Math.Abs(appliedChange - change) > 0.001)
                {
                    UpdateGripPosition();
                }
            }
        }

        private void OnGripCompleted(object sender, DragCompletedEventArgs e)
        {
            _isDragging = false;
            ResizeDragCompleted?.Invoke(this, EventArgs.Empty);

            if (e.Canceled)
            {
                // Optionally revert?
            }
        }

        private void OnGripDragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isDragging = false;
            ResizeDragCompleted?.Invoke(this, EventArgs.Empty);
            UpdateGripPosition();
        }

        private void MoveGripBy(double change)
        {
            if (_gripVisual == null || _gripThumb == null) return;

            if (Orientation == Orientation.Horizontal)
            {
                var visualLeft = Canvas.GetLeft(_gripVisual);
                if (double.IsNaN(visualLeft)) visualLeft = 0;
                Canvas.SetLeft(_gripVisual, visualLeft + change);

                var thumbLeft = Canvas.GetLeft(_gripThumb);
                if (double.IsNaN(thumbLeft)) thumbLeft = 0;
                Canvas.SetLeft(_gripThumb, thumbLeft + change);
            }
            else
            {
                var visualTop = Canvas.GetTop(_gripVisual);
                if (double.IsNaN(visualTop)) visualTop = 0;
                Canvas.SetTop(_gripVisual, visualTop + change);

                var thumbTop = Canvas.GetTop(_gripThumb);
                if (double.IsNaN(thumbTop)) thumbTop = 0;
                Canvas.SetTop(_gripThumb, thumbTop + change);
            }
        }

        private void OnGripPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            DaisyAccessibility.FocusOnPointer(this);
        }

        private bool AdjustColumnWidth(double delta)
        {
            var target = ColumnWidth + delta;
            if (Math.Abs(target - ColumnWidth) < double.Epsilon)
            {
                return false;
            }

            ColumnWidth = target;
            return true;
        }

        private bool SetColumnWidth(double value)
        {
            if (Math.Abs(value - ColumnWidth) < double.Epsilon)
            {
                return false;
            }

            ColumnWidth = value;
            return true;
        }

        private void UpdateInteractionState()
        {
            IsTabStop = IsEnabled && IsGripEnabled;
        }

        private void UpdateAutomationProperties()
        {
            var name = !string.IsNullOrWhiteSpace(AccessibleText)
                ? AccessibleText
                : DefaultAccessibleText;

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }
    }
}

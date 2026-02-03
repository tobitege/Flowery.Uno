using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Controls
{
    /// <summary>
    /// An indicator control styled after DaisyUI's Indicator component.
    /// Positions a badge (like DaisyBadge) over another element using
    /// intuitive position and alignment properties.
    /// </summary>
    public partial class DaisyIndicator : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private ContentPresenter? _mainContent;
        private Canvas? _markerCanvas;
        private ContentPresenter? _markerPresenter;
        private object? _userContent;

        public DaisyIndicator()
        {
            IsTabStop = false;
            SizeChanged += OnSizeChanged;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (Content != null && !ReferenceEquals(Content, _rootGrid))
            {
                _userContent = Content;
            }

            BuildVisualTree();
            RebuildVisual();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionMarker();
        }

        #region Marker
        public static readonly DependencyProperty MarkerProperty =
            DependencyProperty.Register(
                nameof(Marker),
                typeof(object),
                typeof(DaisyIndicator),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the marker/overlay content to display at the specified position.
        /// Can be any control: DaisyBadge, TextBlock, Image, Border, etc.
        /// </summary>
        public object? Marker
        {
            get => GetValue(MarkerProperty);
            set => SetValue(MarkerProperty, value);
        }
        #endregion

        #region MarkerPosition
        public static readonly DependencyProperty MarkerPositionProperty =
            DependencyProperty.Register(
                nameof(MarkerPosition),
                typeof(BadgePosition),
                typeof(DaisyIndicator),
                new PropertyMetadata(BadgePosition.TopRight, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets where the marker appears: TopLeft, TopRight, BottomLeft, BottomRight, etc.
        /// Default is TopRight.
        /// </summary>
        public BadgePosition MarkerPosition
        {
            get => (BadgePosition)GetValue(MarkerPositionProperty);
            set => SetValue(MarkerPositionProperty, value);
        }
        #endregion

        #region MarkerAlignment
        public static readonly DependencyProperty MarkerAlignmentProperty =
            DependencyProperty.Register(
                nameof(MarkerAlignment),
                typeof(BadgeAlignment),
                typeof(DaisyIndicator),
                new PropertyMetadata(BadgeAlignment.Inside, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets how the marker aligns to the corner:
        /// Inside (fully inside), Edge (straddles corner), Outside (mostly outside).
        /// Default is Inside.
        /// </summary>
        public BadgeAlignment MarkerAlignment
        {
            get => (BadgeAlignment)GetValue(MarkerAlignmentProperty);
            set => SetValue(MarkerAlignmentProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyIndicator indicator && indicator.IsLoaded)
                indicator.RebuildVisual();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid();

            _mainContent = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            _mainContent.SizeChanged += (s, e) => PositionMarker();
            _rootGrid.Children.Add(_mainContent);

            // Canvas for marker overlay
            _markerCanvas = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = true,
            };
            _rootGrid.Children.Add(_markerCanvas);

            _markerPresenter = new ContentPresenter();
            _markerPresenter.SizeChanged += (s, e) => PositionMarker();
            _markerCanvas.Children.Add(_markerPresenter);

            Content = _rootGrid;
        }

        private void RebuildVisual()
        {
            if (_rootGrid == null)
                return;

            if (_mainContent != null)
            {
                _mainContent.Content = _userContent;
            }

            if (_markerPresenter != null)
            {
                _markerPresenter.Content = Marker;
                _markerPresenter.Visibility = Marker != null ? Visibility.Visible : Visibility.Collapsed;
            }

            // Defer positioning to ensure layout is complete
            DispatcherQueue?.TryEnqueue(() =>
            {
                UpdateLayout();
                PositionMarker();
            });
        }

        private void PositionMarker()
        {
            if (_markerPresenter == null || _markerCanvas == null || _mainContent == null || Marker == null)
                return;

            // Use _mainContent dimensions (the actual content size)
            var contentWidth = _mainContent.ActualWidth;
            var contentHeight = _mainContent.ActualHeight;

            if (contentWidth <= 0 || contentHeight <= 0)
                return;

            _markerPresenter.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            var markerWidth = _markerPresenter.DesiredSize.Width;
            var markerHeight = _markerPresenter.DesiredSize.Height;

            if (markerWidth <= 0 || markerHeight <= 0)
                return;

            // Calculate position based on MarkerPosition and MarkerAlignment
            var (left, top) = CalculateMarkerPosition(
                contentWidth, contentHeight,
                markerWidth, markerHeight,
                MarkerPosition, MarkerAlignment);

            Canvas.SetLeft(_markerPresenter, left);
            Canvas.SetTop(_markerPresenter, top);
        }

        /// <summary>
        /// Calculates the marker position based on semantic position and alignment.
        /// This logic can be reused by other controls.
        /// </summary>
        public static (double Left, double Top) CalculateMarkerPosition(
            double contentWidth, double contentHeight,
            double markerWidth, double markerHeight,
            BadgePosition position, BadgeAlignment alignment)
        {
            double left = 0;
            double top = 0;

            // Determine alignment offset factor:
            // Inside: marker at content edge (factor=0), then apply fixed inset
            // Edge: marker center on edge (offset = 0.5)
            // Outside: marker fully outside (offset = 1.0)
            double factor = alignment switch
            {
                BadgeAlignment.Inside => 0.0,
                BadgeAlignment.Edge => 0.5,
                BadgeAlignment.Outside => 1.0,
                _ => 0.0
            };

            // Fixed inset for Inside alignment to prevent clipping (accounts for anti-aliasing)
            const double InsideInset = 3.0;

            // Horizontal position
            switch (position)
            {
                case BadgePosition.TopLeft:
                case BadgePosition.CenterLeft:
                case BadgePosition.BottomLeft:
                    // Left side: marker starts at left edge, offset pushes left (outside)
                    left = -markerWidth * factor;
                    if (alignment == BadgeAlignment.Inside) left += InsideInset;
                    break;

                case BadgePosition.TopCenter:
                case BadgePosition.Center:
                case BadgePosition.BottomCenter:
                    // Center: marker centered horizontally
                    left = (contentWidth - markerWidth) / 2;
                    break;

                case BadgePosition.TopRight:
                case BadgePosition.CenterRight:
                case BadgePosition.BottomRight:
                    // Right side: marker ends at right edge, offset pushes right (outside)
                    left = contentWidth - markerWidth + (markerWidth * factor);
                    if (alignment == BadgeAlignment.Inside) left -= InsideInset;
                    break;
            }

            // Vertical position
            switch (position)
            {
                case BadgePosition.TopLeft:
                case BadgePosition.TopCenter:
                case BadgePosition.TopRight:
                    // Top: marker starts at top edge, offset pushes up (outside)
                    top = -markerHeight * factor;
                    if (alignment == BadgeAlignment.Inside) top += InsideInset;
                    break;

                case BadgePosition.CenterLeft:
                case BadgePosition.Center:
                case BadgePosition.CenterRight:
                    // Center: marker centered vertically
                    top = (contentHeight - markerHeight) / 2;
                    break;

                case BadgePosition.BottomLeft:
                case BadgePosition.BottomCenter:
                case BadgePosition.BottomRight:
                    // Bottom: marker ends at bottom edge, offset pushes down (outside)
                    top = contentHeight - markerHeight + (markerHeight * factor);
                    if (alignment == BadgeAlignment.Inside) top -= InsideInset;
                    break;
            }

            return (left, top);
        }
    }
}

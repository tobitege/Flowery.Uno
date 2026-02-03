namespace Flowery.Controls
{
    /// <summary>
    /// Mask shape variants.
    /// </summary>
    public enum DaisyMaskVariant
    {
        Squircle,
        Heart,
        Hexagon,
        Circle,
        Square,
        Diamond,
        Triangle
    }

    /// <summary>
    /// A mask shape container that clips content to various shapes.
    /// Uses a Path with the geometry as clip, working around WinUI's limitation
    /// that UIElement.Clip only supports RectangleGeometry.
    /// </summary>
    public partial class DaisyMask : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Microsoft.UI.Xaml.Shapes.Path? _clipPath;
        private ContentPresenter? _contentPresenter;
        private Border? _clippingBorder;
        private object? _userContent;

        public DaisyMask()
        {
            DefaultStyleKey = typeof(DaisyMask);
            IsTabStop = false;
            SizeChanged += OnSizeChanged;
        }

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyMaskVariant),
                typeof(DaisyMask),
                new PropertyMetadata(DaisyMaskVariant.Squircle, OnVariantChanged));

        public DaisyMaskVariant Variant
        {
            get => (DaisyMaskVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMask mask)
            {
                mask.UpdateClip();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                UpdateClip();
                return;
            }

            BuildVisualTree();
            UpdateClip();
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _clippingBorder ?? base.GetNeumorphicHostElement();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateClip();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Capture user content
            _userContent = Content;
            Content = null;

            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Content presenter
            _contentPresenter = new ContentPresenter
            {
                Content = _userContent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Clip path that provides the shape (for complex shapes)
            _clipPath = new Microsoft.UI.Xaml.Shapes.Path
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Fill
            };

            // Wrap content in a border for rounded corner clipping
            _clippingBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = _contentPresenter
            };

            _rootGrid.Children.Add(_clippingBorder);
            Content = _rootGrid;
        }

        #endregion

        #region Clipping

        private void UpdateClip()
        {
            if (_clippingBorder == null)
                return;

            var w = ActualWidth;
            var h = ActualHeight;

            if (w <= 0 || h <= 0)
            {
                _clippingBorder.Clip = null;
                _clippingBorder.CornerRadius = new CornerRadius(0);
                return;
            }

            var minDim = Math.Min(w, h);

            // Apply shape via Border's CornerRadius (works for Squircle, Circle, Square)
            // and Clip for size constraints
            switch (Variant)
            {
                case DaisyMaskVariant.Square:
                    ClearCompositionClip();
                    _clippingBorder.CornerRadius = new CornerRadius(0);
                    _clippingBorder.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, h) };
                    break;

                case DaisyMaskVariant.Squircle:
                    ClearCompositionClip();
                    // Squircle uses rounded corners (about 20% of the smaller dimension)
                    var squircleRadius = minDim * 0.2;
                    _clippingBorder.CornerRadius = new CornerRadius(squircleRadius);
                    _clippingBorder.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, h) };
                    break;

                case DaisyMaskVariant.Circle:
                    ClearCompositionClip();
                    // Circle uses 50% corner radius for full circle effect
                    var circleRadius = minDim / 2;
                    _clippingBorder.CornerRadius = new CornerRadius(circleRadius);
                    _clippingBorder.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, h) };
                    break;

                case DaisyMaskVariant.Heart:
                case DaisyMaskVariant.Hexagon:
                case DaisyMaskVariant.Triangle:
                case DaisyMaskVariant.Diamond:
                    // Use Composition API for complex polygon clipping
                    _clippingBorder.CornerRadius = new CornerRadius(0);
                    _clippingBorder.Clip = null;
                    ApplyCompositionClip(w, h);
                    break;
            }

            // Apply any additional visual shape effects
            UpdateVisualShape();
        }

        private void UpdateVisualShape()
        {
            // For shapes that need visual representation beyond rectangle clip,
            // we can overlay a shape or use the Path element
            if (_clipPath == null || _rootGrid == null)
                return;

            // Remove old path if present
            if (_rootGrid.Children.Contains(_clipPath))
            {
                _rootGrid.Children.Remove(_clipPath);
            }
        }

        private void ApplyCompositionClip(double w, double h)
        {
            if (_clippingBorder == null)
                return;

            try
            {
                var visual = ElementCompositionPreview.GetElementVisual(_clippingBorder);
                var compositor = visual.Compositor;

                // Create the geometry based on variant
                var geometry = CreateCompositionGeometry(compositor, Variant, (float)w, (float)h);
                if (geometry == null)
                {
                    visual.Clip = null;
                    _clippingBorder.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, h) };
                    return;
                }

                // Create geometric clip and apply to visual
                var clip = compositor.CreateGeometricClip(geometry);
                visual.Clip = clip;
            }
            catch
            {
                // Composition API not available on this platform, fall back to rectangle
                _clippingBorder.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, h) };
            }
        }

        private void ClearCompositionClip()
        {
            if (_clippingBorder == null)
                return;

            try
            {
                var visual = ElementCompositionPreview.GetElementVisual(_clippingBorder);
                visual.Clip = null;
            }
            catch
            {
                // Ignore if composition not available
            }
        }

        private static CompositionGeometry? CreateCompositionGeometry(Compositor compositor, DaisyMaskVariant variant, float w, float h)
        {
            return DaisyMaskWin2DInterop.TryCreateGeometry(compositor, variant, w, h);
        }

        /// <summary>
        /// Creates geometry for the specified shape. Note: Due to WinUI limitations,
        /// complex geometries cannot be used directly with UIElement.Clip.
        /// These are provided for potential use with composition effects.
        /// </summary>
        private static Geometry CreateShapeGeometry(DaisyMaskVariant variant, double w, double h)
        {
            return variant switch
            {
                DaisyMaskVariant.Circle => CreateEllipseGeometry(w, h),
                DaisyMaskVariant.Heart => CreateHeartGeometry(w, h),
                DaisyMaskVariant.Hexagon => CreateHexagonGeometry(w, h),
                DaisyMaskVariant.Triangle => CreateTriangleGeometry(w, h),
                DaisyMaskVariant.Diamond => CreateDiamondGeometry(w, h),
                _ => new RectangleGeometry { Rect = new Rect(0, 0, w, h) }
            };
        }

        private static Geometry CreateEllipseGeometry(double w, double h)
        {
            return FloweryPathHelpers.CreateEllipseGeometry(w / 2, h / 2, w / 2, h / 2);
        }

        private static PathGeometry CreateHeartGeometry(double w, double h)
        {
            var scaleX = w / 100.0;
            var scaleY = h / 100.0;

            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Point(50 * scaleX, 90 * scaleY),
                IsClosed = true
            };

            figure.Segments.Add(new BezierSegment
            {
                Point1 = new Point(10 * scaleX, 50 * scaleY),
                Point2 = new Point(10 * scaleX, 30 * scaleY),
                Point3 = new Point(25 * scaleX, 15 * scaleY)
            });
            figure.Segments.Add(new BezierSegment
            {
                Point1 = new Point(40 * scaleX, 5 * scaleY),
                Point2 = new Point(50 * scaleX, 10 * scaleY),
                Point3 = new Point(50 * scaleX, 20 * scaleY)
            });
            figure.Segments.Add(new BezierSegment
            {
                Point1 = new Point(50 * scaleX, 10 * scaleY),
                Point2 = new Point(60 * scaleX, 5 * scaleY),
                Point3 = new Point(75 * scaleX, 15 * scaleY)
            });
            figure.Segments.Add(new BezierSegment
            {
                Point1 = new Point(90 * scaleX, 30 * scaleY),
                Point2 = new Point(90 * scaleX, 50 * scaleY),
                Point3 = new Point(50 * scaleX, 90 * scaleY)
            });

            geometry.Figures.Add(figure);
            return geometry;
        }

        private static PathGeometry CreateHexagonGeometry(double w, double h)
        {
            var scaleX = w / 100.0;
            var scaleY = h / 100.0;

            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Point(50 * scaleX, 0),
                IsClosed = true
            };

            figure.Segments.Add(new LineSegment { Point = new Point(100 * scaleX, 25 * scaleY) });
            figure.Segments.Add(new LineSegment { Point = new Point(100 * scaleX, 75 * scaleY) });
            figure.Segments.Add(new LineSegment { Point = new Point(50 * scaleX, 100 * scaleY) });
            figure.Segments.Add(new LineSegment { Point = new Point(0, 75 * scaleY) });
            figure.Segments.Add(new LineSegment { Point = new Point(0, 25 * scaleY) });

            geometry.Figures.Add(figure);
            return geometry;
        }

        private static PathGeometry CreateTriangleGeometry(double w, double h)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Point(w / 2, 0),
                IsClosed = true
            };

            figure.Segments.Add(new LineSegment { Point = new Point(w, h) });
            figure.Segments.Add(new LineSegment { Point = new Point(0, h) });

            geometry.Figures.Add(figure);
            return geometry;
        }

        private static PathGeometry CreateDiamondGeometry(double w, double h)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Point(w / 2, 0),
                IsClosed = true
            };

            figure.Segments.Add(new LineSegment { Point = new Point(w, h / 2) });
            figure.Segments.Add(new LineSegment { Point = new Point(w / 2, h) });
            figure.Segments.Add(new LineSegment { Point = new Point(0, h / 2) });

            geometry.Figures.Add(figure);
            return geometry;
        }

        #endregion
    }
}

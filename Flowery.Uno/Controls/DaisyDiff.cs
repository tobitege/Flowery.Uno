using System;
using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// Specifies the orientation of the diff split line.
    /// </summary>
    public enum DiffOrientation
    {
        /// <summary>
        /// Horizontal split (left-to-right).
        /// </summary>
        Horizontal,
        /// <summary>
        /// Vertical split (top-to-bottom).
        /// </summary>
        Vertical
    }

    /// <summary>
    /// An image diff/comparison control styled after DaisyUI's Diff component.
    /// Displays two layers and allows dragging a grip to reveal more/less of the top layer.
    /// </summary>
    public partial class DaisyDiff : DaisyBaseContentControl
    {
        private const double KeyboardStep = 1.0;
        private const double LargeKeyboardStep = 10.0;

        private Grid? _rootGrid;
        private ContentPresenter? _image2Presenter;
        private Border? _topImageContainer;
        private ContentPresenter? _image1Presenter;
        private Thumb? _gripThumb;
        private UIElement? _gripVisual;
        private Canvas? _overlayCanvas;
        private RectangleGeometry? _clipGeometry;

        private bool _layoutApplied;

        public DaisyDiff()
        {
            // Ensure content stretches to fill the control
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            IsTabStop = true;
            UseSystemFocusVisuals = true;

            SizeChanged += OnSizeChanged;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplyAll();

            // Subscribe to LayoutUpdated to ensure initial positioning after measure pass
            _layoutApplied = false;
            LayoutUpdated += OnLayoutUpdated;
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            // Apply layout once we have valid dimensions
            if (!_layoutApplied && ActualWidth > 0 && ActualHeight > 0)
            {
                _layoutApplied = true;
                UpdateLayoutForOffset();
                LayoutUpdated -= OnLayoutUpdated;
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            if (_gripThumb != null)
                _gripThumb.DragDelta -= OnGripDragDelta;
            if (_gripThumb != null)
                _gripThumb.PointerPressed -= OnGripPointerPressed;
            LayoutUpdated -= OnLayoutUpdated;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            var step = e.Key is VirtualKey.PageUp or VirtualKey.PageDown ? LargeKeyboardStep : KeyboardStep;
            var handled = e.Key switch
            {
                VirtualKey.Left when Orientation == DiffOrientation.Horizontal => AdjustOffset(-step),
                VirtualKey.Right when Orientation == DiffOrientation.Horizontal => AdjustOffset(step),
                VirtualKey.Up when Orientation == DiffOrientation.Vertical => AdjustOffset(-step),
                VirtualKey.Down when Orientation == DiffOrientation.Vertical => AdjustOffset(step),
                VirtualKey.Home => SetOffset(0),
                VirtualKey.End => SetOffset(100),
                VirtualKey.PageUp when Orientation == DiffOrientation.Horizontal => AdjustOffset(step),
                VirtualKey.PageDown when Orientation == DiffOrientation.Horizontal => AdjustOffset(-step),
                _ => false
            };

            if (handled)
            {
                e.Handled = true;
            }
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            RebuildGripVisual();
            ApplyAll();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayoutForOffset();
        }

        #region Image1
        public static readonly DependencyProperty Image1Property =
            DependencyProperty.Register(
                nameof(Image1),
                typeof(object),
                typeof(DaisyDiff),
                new PropertyMetadata(null, OnContentChanged));

        public object? Image1
        {
            get => GetValue(Image1Property);
            set => SetValue(Image1Property, value);
        }
        #endregion

        #region Image2
        public static readonly DependencyProperty Image2Property =
            DependencyProperty.Register(
                nameof(Image2),
                typeof(object),
                typeof(DaisyDiff),
                new PropertyMetadata(null, OnContentChanged));

        public object? Image2
        {
            get => GetValue(Image2Property);
            set => SetValue(Image2Property, value);
        }
        #endregion

        #region Offset
        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register(
                nameof(Offset),
                typeof(double),
                typeof(DaisyDiff),
                new PropertyMetadata(50.0, OnOffsetChanged));

        /// <summary>
        /// Gets or sets the diff offset in percent (0-100).
        /// </summary>
        public double Offset
        {
            get => (double)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, ValueCoercion.Percent100(value));
        }
        #endregion

        #region Orientation
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(DiffOrientation),
                typeof(DaisyDiff),
                new PropertyMetadata(DiffOrientation.Horizontal, OnOrientationChanged));

        /// <summary>
        /// Gets or sets the orientation of the diff split line.
        /// </summary>
        public DiffOrientation Orientation
        {
            get => (DiffOrientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        #endregion

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDiff diff)
                diff.ApplyAll();
        }

        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDiff diff)
                diff.UpdateLayoutForOffset();
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDiff diff)
            {
                // Rebuild visual tree to change grip orientation
                diff.RebuildGripVisual();
                diff.UpdateLayoutForOffset();
            }
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _image2Presenter = new ContentPresenter
            {
                Content = Image2,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            _rootGrid.Children.Add(_image2Presenter);

            _clipGeometry = new RectangleGeometry();

            _image1Presenter = new ContentPresenter
            {
                Content = Image1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            _topImageContainer = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = _image1Presenter,
                Clip = _clipGeometry
            };
            _rootGrid.Children.Add(_topImageContainer);

            _overlayCanvas = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = true
            };

            _gripThumb = new Thumb
            {
                Width = double.NaN,
                Height = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _gripThumb.DragDelta += OnGripDragDelta;
            _gripThumb.PointerPressed += OnGripPointerPressed;

            // Thumb has no Content in WinUI/Uno; render the grip as a separate overlay element.
            _gripVisual = BuildGripVisual(Orientation);
            _gripVisual.IsHitTestVisible = false;

            _overlayCanvas.Children.Add(_gripVisual);
            _overlayCanvas.Children.Add(_gripThumb);
            _rootGrid.Children.Add(_overlayCanvas);

            // Configure thumb and grip based on orientation
            RebuildGripVisual();

            Content = _rootGrid;
        }

        private static Grid BuildGripVisual(DiffOrientation orientation)
        {
            var grid = new Grid();

            if (orientation == DiffOrientation.Vertical)
            {
                // Horizontal line for vertical grip
                var line = new Rectangle
                {
                    Height = 2,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                    Opacity = 0.6
                };
                grid.Children.Add(line);

                var handle = new Border
                {
                    Width = 28,
                    Height = 14,
                    CornerRadius = new CornerRadius(8),
                    Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush"),
                    BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Child = CreateGripIcon("DaisyIconDotsHorizontal")
                };
                grid.Children.Add(handle);
            }
            else
            {
                // Vertical line for horizontal grip
                var line = new Rectangle
                {
                    Width = 2,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                    Opacity = 0.6
                };
                grid.Children.Add(line);

                var handle = new Border
                {
                    Width = 14,
                    Height = 28,
                    CornerRadius = new CornerRadius(8),
                    Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush"),
                    BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                    BorderThickness = new Thickness(1),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = CreateGripIcon("DaisyIconEllipsis")
                };
                grid.Children.Add(handle);
            }

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
                Width = 12,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = path
            };
        }

        private void OnGripDragDelta(object sender, DragDeltaEventArgs e)
        {
            var width = ActualWidth;
            var height = ActualHeight;

            if (Orientation == DiffOrientation.Horizontal)
            {
                if (width <= 0)
                    return;

                var current = width * (Offset / 100.0);
                var next = current + e.HorizontalChange;
                Offset = (next / width) * 100.0;
            }
            else // Vertical
            {
                if (height <= 0)
                    return;

                var current = height * (Offset / 100.0);
                var next = current + e.VerticalChange;
                Offset = (next / height) * 100.0;
            }
        }

        private void OnGripPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            DaisyAccessibility.FocusOnPointer(this);
        }

        private bool AdjustOffset(double delta)
        {
            Offset += delta;
            return true;
        }

        private bool SetOffset(double value)
        {
            Offset = value;
            return true;
        }

        private void ApplyAll()
        {
            if (_image1Presenter != null)
                _image1Presenter.Content = Image1;
            if (_image2Presenter != null)
                _image2Presenter.Content = Image2;

            UpdateLayoutForOffset();
        }

        private void UpdateLayoutForOffset()
        {
            if (_clipGeometry == null || _topImageContainer == null || _gripThumb == null)
                return;

            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 0 || h <= 0)
                return;

            if (Orientation == DiffOrientation.Horizontal)
            {
                var clipWidth = w * (Offset / 100.0);

                _clipGeometry.Rect = new Windows.Foundation.Rect(0, 0, clipWidth, h);

                // Keep the thumb centered on the split line.
                Canvas.SetLeft(_gripThumb, clipWidth - (_gripThumb.Width / 2));
                _gripThumb.Height = h;

                if (_gripVisual is FrameworkElement fe)
                {
                    if (double.IsNaN(fe.Width) || fe.Width <= 0)
                        fe.Width = _gripThumb.Width;
                    fe.Height = h;

                    Canvas.SetLeft(fe, clipWidth - (fe.Width / 2));
                    Canvas.SetTop(fe, 0);
                }
            }
            else // Vertical
            {
                var clipHeight = h * (Offset / 100.0);

                _clipGeometry.Rect = new Windows.Foundation.Rect(0, 0, w, clipHeight);

                // Keep the thumb centered on the split line.
                Canvas.SetTop(_gripThumb, clipHeight - (_gripThumb.Height / 2));
                _gripThumb.Width = w;

                if (_gripVisual is FrameworkElement fe)
                {
                    fe.Width = w;
                    if (double.IsNaN(fe.Height) || fe.Height <= 0)
                        fe.Height = _gripThumb.Height;

                    Canvas.SetLeft(fe, 0);
                    Canvas.SetTop(fe, clipHeight - (fe.Height / 2));
                }
            }
        }

        private void RebuildGripVisual()
        {
            if (_overlayCanvas == null || _gripThumb == null)
                return;

            // Update thumb configuration based on orientation
            if (Orientation == DiffOrientation.Vertical)
            {
                _gripThumb.Width = double.NaN;
                _gripThumb.Height = 18;
                _gripThumb.HorizontalAlignment = HorizontalAlignment.Stretch;
                _gripThumb.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                _gripThumb.Width = 18;
                _gripThumb.Height = double.NaN;
                _gripThumb.HorizontalAlignment = HorizontalAlignment.Left;
                _gripThumb.VerticalAlignment = VerticalAlignment.Stretch;
            }

            // Remove old grip visual
            if (_gripVisual != null)
            {
                _overlayCanvas.Children.Remove(_gripVisual);
            }

            // Create new grip visual with current orientation
            _gripVisual = BuildGripVisual(Orientation);
            _gripVisual.IsHitTestVisible = false;
            _overlayCanvas.Children.Insert(0, _gripVisual);
        }
    }
}

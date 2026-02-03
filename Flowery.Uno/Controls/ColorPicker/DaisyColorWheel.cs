using System;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Colors = Microsoft.UI.Colors;

namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// A circular color wheel control for selecting hue and saturation values.
    /// Displays colors in HSL color space with the selected color indicated by a marker.
    /// Supports automatic font scaling when contained within a FloweryScaleManager.EnableScaling="True" container.
    /// </summary>
    public partial class DaisyColorWheel : DaisyBaseContentControl, IScalableControl
    {
        private const double BaseTextFontSize = 14.0;

        private WriteableBitmap? _wheelBitmap;
        private int _wheelBitmapSize;
        private bool _isDragging;
        private Point _centerPoint;
        private double _radius;
        private bool _lockUpdates;

        private Grid? _root;
        private Image? _wheelImage;
        private Canvas? _overlayCanvas;
        private Ellipse? _markerOuter;
        private Ellipse? _markerInner;
        private Ellipse? _markerFill;
        private Line? _centerLineHorizontal;
        private Line? _centerLineVertical;
        private const double KeyboardHueStep = 5.0;
        private const double KeyboardSaturationStep = 0.05;

        /// <inheritdoc/>
        public void ApplyScaleFactor(double scaleFactor)
        {
            FontSize = FloweryScaleManager.ApplyScale(BaseTextFontSize, 11.0, scaleFactor);
        }

        #region Dependency Properties

        /// <summary>
        /// Gets or sets the selected color.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(DaisyColorWheel),
                new PropertyMetadata(Colors.Red, OnColorPropertyChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the HSL color representation.
        /// </summary>
        public static readonly DependencyProperty HslColorProperty =
            DependencyProperty.Register(
                nameof(HslColor),
                typeof(HslColor),
                typeof(DaisyColorWheel),
                new PropertyMetadata(new HslColor(0, 1, 0.5), OnHslColorPropertyChanged));

        public HslColor HslColor
        {
            get => (HslColor)GetValue(HslColorProperty);
            set => SetValue(HslColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the lightness value (0-1). The wheel displays colors at this lightness level.
        /// </summary>
        public static readonly DependencyProperty LightnessProperty =
            DependencyProperty.Register(
                nameof(Lightness),
                typeof(double),
                typeof(DaisyColorWheel),
                new PropertyMetadata(0.5, OnWheelPropertyChanged));

        public double Lightness
        {
            get => (double)GetValue(LightnessProperty);
            set => SetValue(LightnessProperty, Math.Min(1, Math.Max(0, value)));
        }

        /// <summary>
        /// Gets or sets the size of the selection marker.
        /// </summary>
        public static readonly DependencyProperty SelectionSizeProperty =
            DependencyProperty.Register(
                nameof(SelectionSize),
                typeof(double),
                typeof(DaisyColorWheel),
                new PropertyMetadata(10.0, OnSelectionPropertyChanged));

        public double SelectionSize
        {
            get => (double)GetValue(SelectionSizeProperty);
            set => SetValue(SelectionSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the selection marker outline.
        /// </summary>
        public static readonly DependencyProperty SelectionOutlineColorProperty =
            DependencyProperty.Register(
                nameof(SelectionOutlineColor),
                typeof(Color),
                typeof(DaisyColorWheel),
                new PropertyMetadata(Colors.White, OnSelectionPropertyChanged));

        public Color SelectionOutlineColor
        {
            get => (Color)GetValue(SelectionOutlineColorProperty);
            set => SetValue(SelectionOutlineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show center crosshairs.
        /// </summary>
        public static readonly DependencyProperty ShowCenterLinesProperty =
            DependencyProperty.Register(
                nameof(ShowCenterLines),
                typeof(bool),
                typeof(DaisyColorWheel),
                new PropertyMetadata(false, OnCenterLinePropertyChanged));

        public bool ShowCenterLines
        {
            get => (bool)GetValue(ShowCenterLinesProperty);
            set => SetValue(ShowCenterLinesProperty, value);
        }

        /// <summary>
        /// Gets or sets the color step for generating the wheel (lower = smoother, higher = faster).
        /// </summary>
        public static readonly DependencyProperty ColorStepProperty =
            DependencyProperty.Register(
                nameof(ColorStep),
                typeof(int),
                typeof(DaisyColorWheel),
                new PropertyMetadata(4, OnWheelPropertyChanged));

        public int ColorStep
        {
            get => (int)GetValue(ColorStepProperty);
            set => SetValue(ColorStepProperty, Math.Max(1, value));
        }

        /// <summary>
        /// Gets or sets an optional callback invoked when the color changes.
        /// This provides a simpler alternative to the ColorChanged event.
        /// </summary>
        public static readonly DependencyProperty OnColorChangedProperty =
            DependencyProperty.Register(
                nameof(OnColorChanged),
                typeof(Action<Color>),
                typeof(DaisyColorWheel),
                new PropertyMetadata(null));

        public Action<Color>? OnColorChanged
        {
            get => (Action<Color>?)GetValue(OnColorChangedProperty);
            set => SetValue(OnColorChangedProperty, value);
        }

        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyColorWheel),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        #endregion

        /// <summary>
        /// Occurs when the color changes.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        public DaisyColorWheel()
        {
            DefaultStyleKey = typeof(DaisyColorWheel);
            BuildVisualTree();

            SizeChanged += OnSizeChanged;
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += OnPointerCaptureLost;
            IsTabStop = true;
            UseSystemFocusVisuals = true;
            UpdateAutomationProperties();
        }

        private static void OnColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorWheel wheel)
            {
                wheel.HandleColorPropertyChanged((Color)e.NewValue);
            }
        }

        private static void OnHslColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorWheel wheel)
            {
                wheel.HandleHslColorPropertyChanged((HslColor)e.NewValue);
            }
        }

        private static void OnWheelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorWheel wheel)
            {
                wheel.InvalidateWheel();
                wheel.UpdateWheelBitmap();
            }
        }

        private static void OnSelectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorWheel wheel)
            {
                wheel.UpdateGeometry();
                wheel.UpdateSelectionMarker();
            }
        }

        private static void OnCenterLinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorWheel wheel)
            {
                wheel.UpdateCenterLines();
            }
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorWheel wheel)
            {
                wheel.UpdateAutomationProperties();
            }
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            var handled = true;
            switch (e.Key)
            {
                case VirtualKey.Left:
                    AdjustHue(-KeyboardHueStep);
                    break;
                case VirtualKey.Right:
                    AdjustHue(KeyboardHueStep);
                    break;
                case VirtualKey.Up:
                    AdjustSaturation(KeyboardSaturationStep);
                    break;
                case VirtualKey.Down:
                    AdjustSaturation(-KeyboardSaturationStep);
                    break;
                case VirtualKey.Home:
                    SetHue(0);
                    break;
                case VirtualKey.End:
                    SetHue(359);
                    break;
                case VirtualKey.PageUp:
                    AdjustSaturation(KeyboardSaturationStep * 2);
                    break;
                case VirtualKey.PageDown:
                    AdjustSaturation(-KeyboardSaturationStep * 2);
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    AdjustHue(KeyboardHueStep);
                    break;
                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        private void HandleColorPropertyChanged(Color color)
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                var hsl = new HslColor(color)
                {
                    L = Lightness
                };
                HslColor = hsl;
                OnColorChangedRaised(new ColorChangedEventArgs(color));
            }
            finally
            {
                _lockUpdates = false;
            }

            UpdateSelectionMarker();
            UpdateAutomationProperties();
        }

        private void HandleHslColorPropertyChanged(HslColor hsl)
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                Color = hsl.ToRgbColor();
            }
            finally
            {
                _lockUpdates = false;
            }

            UpdateSelectionMarker();
            UpdateAutomationProperties();
        }

        private void BuildVisualTree()
        {
            _root = new Grid();
            _wheelImage = new Image { Stretch = Stretch.Fill };
            _overlayCanvas = new Canvas();

            _centerLineHorizontal = new Line
            {
                Stroke = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)),
                StrokeThickness = 1,
                Visibility = Visibility.Collapsed
            };

            _centerLineVertical = new Line
            {
                Stroke = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)),
                StrokeThickness = 1,
                Visibility = Visibility.Collapsed
            };

            _markerOuter = new Ellipse
            {
                Stroke = new SolidColorBrush(SelectionOutlineColor),
                StrokeThickness = 2
            };

            _markerInner = new Ellipse
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1
            };

            _markerFill = new Ellipse
            {
                Fill = new SolidColorBrush(Color)
            };

            _overlayCanvas.Children.Add(_centerLineHorizontal);
            _overlayCanvas.Children.Add(_centerLineVertical);
            _overlayCanvas.Children.Add(_markerOuter);
            _overlayCanvas.Children.Add(_markerInner);
            _overlayCanvas.Children.Add(_markerFill);

            _root.Children.Add(_wheelImage);
            _root.Children.Add(_overlayCanvas);

            Content = _root;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGeometry();
            InvalidateWheel();
            UpdateWheelBitmap();
            UpdateSelectionMarker();
        }

        private void UpdateGeometry()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0)
                return;

            var size = Math.Min(ActualWidth, ActualHeight);
            _centerPoint = new Point(ActualWidth / 2, ActualHeight / 2);
            _radius = (size / 2) - (SelectionSize / 2) - 2;
            if (_radius < 0) _radius = 0;

            UpdateCenterLines();
        }

        private void UpdateCenterLines()
        {
            if (_centerLineHorizontal == null || _centerLineVertical == null)
                return;

            var visibility = ShowCenterLines ? Visibility.Visible : Visibility.Collapsed;
            _centerLineHorizontal.Visibility = visibility;
            _centerLineVertical.Visibility = visibility;

            if (visibility == Visibility.Collapsed)
                return;

            _centerLineHorizontal.X1 = _centerPoint.X - _radius;
            _centerLineHorizontal.X2 = _centerPoint.X + _radius;
            _centerLineHorizontal.Y1 = _centerLineHorizontal.Y2 = _centerPoint.Y;

            _centerLineVertical.Y1 = _centerPoint.Y - _radius;
            _centerLineVertical.Y2 = _centerPoint.Y + _radius;
            _centerLineVertical.X1 = _centerLineVertical.X2 = _centerPoint.X;
        }

        private void UpdateWheelBitmap()
        {
            if (_wheelImage == null || ActualWidth <= 0 || ActualHeight <= 0)
                return;

            int size = (int)Math.Min(ActualWidth, ActualHeight);
            if (size <= 0)
                return;

            if (_wheelBitmap == null || _wheelBitmapSize != size)
            {
                _wheelBitmap = new WriteableBitmap(size, size);
                _wheelBitmapSize = size;
            }

            double center = size / 2.0;
            double radius = center - (SelectionSize / 2) - 2;
            if (radius <= 0)
                return;

            var pixels = new byte[size * size * 4];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double dx = x - center;
                    double dy = y - center;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    int offset = (y * size + x) * 4;

                    if (distance <= radius)
                    {
                        double angle = Math.Atan2(dy, dx);
                        double hue = (angle * 180 / Math.PI + 360) % 360;
                        double saturation = distance / radius;

                        var color = HslColor.HslToRgb(hue, saturation, Lightness);

                        pixels[offset] = color.B;
                        pixels[offset + 1] = color.G;
                        pixels[offset + 2] = color.R;
                        pixels[offset + 3] = color.A;
                    }
                    else
                    {
                        pixels[offset] = 0;
                        pixels[offset + 1] = 0;
                        pixels[offset + 2] = 0;
                        pixels[offset + 3] = 0;
                    }
                }
            }

            ColorPickerRendering.WritePixels(_wheelBitmap, pixels);
            _wheelImage.Source = _wheelBitmap;
        }

        private void InvalidateWheel()
        {
            _wheelBitmap = null;
            _wheelBitmapSize = 0;
        }

        private void UpdateSelectionMarker()
        {
            if (_markerOuter == null || _markerInner == null || _markerFill == null)
                return;

            var hsl = HslColor;
            var angle = hsl.H * Math.PI / 180;
            var distance = hsl.S * _radius;

            var markerX = _centerPoint.X + Math.Cos(angle) * distance;
            var markerY = _centerPoint.Y + Math.Sin(angle) * distance;

            var outerSize = Math.Max(0, SelectionSize + 2);
            var innerSize = Math.Max(0, SelectionSize - 2);
            var fillSize = Math.Max(0, SelectionSize - 4);

            _markerOuter.Width = outerSize;
            _markerOuter.Height = outerSize;
            Canvas.SetLeft(_markerOuter, markerX - outerSize / 2);
            Canvas.SetTop(_markerOuter, markerY - outerSize / 2);
            _markerOuter.Stroke = new SolidColorBrush(SelectionOutlineColor);

            _markerInner.Width = innerSize;
            _markerInner.Height = innerSize;
            Canvas.SetLeft(_markerInner, markerX - innerSize / 2);
            Canvas.SetTop(_markerInner, markerY - innerSize / 2);

            _markerFill.Width = fillSize;
            _markerFill.Height = fillSize;
            Canvas.SetLeft(_markerFill, markerX - fillSize / 2);
            Canvas.SetTop(_markerFill, markerY - fillSize / 2);
            _markerFill.Fill = new SolidColorBrush(Color);
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDragging = CapturePointer(e.Pointer);
                UpdateColorFromPoint(e.GetCurrentPoint(this).Position);
                e.Handled = true;
                DaisyAccessibility.FocusOnPointer(this);
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                UpdateColorFromPoint(e.GetCurrentPoint(this).Position);
                e.Handled = true;
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleasePointerCapture(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
        }

        private void UpdateColorFromPoint(Point point)
        {
            if (_radius <= 0)
                return;

            var dx = point.X - _centerPoint.X;
            var dy = point.Y - _centerPoint.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            var angle = Math.Atan2(dy, dx);
            var hue = (angle * 180 / Math.PI + 360) % 360;
            var saturation = Math.Min(1, distance / _radius);

            _lockUpdates = true;
            try
            {
                var hsl = new HslColor(hue, saturation, Lightness);
                HslColor = hsl;
                Color = hsl.ToRgbColor();
                OnColorChangedRaised(new ColorChangedEventArgs(Color));
            }
            finally
            {
                _lockUpdates = false;
            }

            UpdateSelectionMarker();
        }

        /// <summary>
        /// Gets the color at the specified point on the wheel.
        /// </summary>
        public Color GetColorAtPoint(Point point)
        {
            var dx = point.X - _centerPoint.X;
            var dy = point.Y - _centerPoint.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > _radius)
                return Colors.Transparent;

            var angle = Math.Atan2(dy, dx);
            var hue = (angle * 180 / Math.PI + 360) % 360;
            var saturation = distance / _radius;

            return HslColor.HslToRgb(hue, saturation, Lightness);
        }

        protected virtual void OnColorChangedRaised(ColorChangedEventArgs e)
        {
            ColorChanged?.Invoke(this, e);
            OnColorChanged?.Invoke(e.Color);
        }

        private void AdjustHue(double delta)
        {
            var hsl = HslColor;
            var hue = (hsl.H + delta) % 360;
            if (hue < 0) hue += 360;
            UpdateHsl(hue, hsl.S);
        }

        private void SetHue(double hue)
        {
            var hsl = HslColor;
            UpdateHsl(Math.Clamp(hue, 0, 359), hsl.S);
        }

        private void AdjustSaturation(double delta)
        {
            var hsl = HslColor;
            UpdateHsl(hsl.H, Math.Clamp(hsl.S + delta, 0, 1));
        }

        private void UpdateHsl(double hue, double saturation)
        {
            _lockUpdates = true;
            try
            {
                var hsl = HslColor;
                hsl.H = hue;
                hsl.S = saturation;
                HslColor = hsl;
                Color = hsl.ToRgbColor();
                OnColorChangedRaised(new ColorChangedEventArgs(Color));
            }
            finally
            {
                _lockUpdates = false;
            }

            UpdateSelectionMarker();
            UpdateAutomationProperties();
        }

        private void UpdateAutomationProperties()
        {
            var name = AccessibleText;
            if (string.IsNullOrWhiteSpace(name))
            {
                var hsl = HslColor;
                name = $"Color wheel H {hsl.H:0} S {hsl.S:0.##}";
            }

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }
    }
}

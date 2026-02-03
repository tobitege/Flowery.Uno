using System;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;
using Windows.UI;
using Colors = Microsoft.UI.Colors;

namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// A slider control for selecting individual color channel values.
    /// Supports automatic font scaling when contained within a FloweryScaleManager.EnableScaling="True" container.
    /// </summary>
    public partial class DaisyColorSlider : DaisyBaseContentControl, IScalableControl
    {
        private const double BaseTextFontSize = 12.0;

        private WriteableBitmap? _gradientBitmap;
        private int _gradientWidth;
        private int _gradientHeight;
        private WriteableBitmap? _checkerboardBitmap;

        private bool _isDragging;
        private bool _lockUpdates;

        private Grid? _root;
        private Image? _checkerboardImage;
        private Image? _gradientImage;
        private Border? _border;
        private Canvas? _markerCanvas;
        private Line? _markerMain;
        private Line? _markerLeft;
        private Line? _markerRight;

        /// <inheritdoc/>
        public void ApplyScaleFactor(double scaleFactor)
        {
            FontSize = FloweryScaleManager.ApplyScale(BaseTextFontSize, 10.0, scaleFactor);
        }

        #region Dependency Properties

        /// <summary>
        /// Gets or sets the color channel this slider controls.
        /// </summary>
        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register(
                nameof(Channel),
                typeof(ColorSliderChannel),
                typeof(DaisyColorSlider),
                new PropertyMetadata(ColorSliderChannel.Hue, OnChannelPropertyChanged));

        public ColorSliderChannel Channel
        {
            get => (ColorSliderChannel)GetValue(ChannelProperty);
            set => SetValue(ChannelProperty, value);
        }

        /// <summary>
        /// Gets or sets the current color.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(DaisyColorSlider),
                new PropertyMetadata(Colors.Red, OnColorPropertyChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the orientation of the slider.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyColorSlider),
                new PropertyMetadata(Orientation.Horizontal, OnLayoutPropertyChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the thumb/nub.
        /// </summary>
        public static readonly DependencyProperty NubSizeProperty =
            DependencyProperty.Register(
                nameof(NubSize),
                typeof(double),
                typeof(DaisyColorSlider),
                new PropertyMetadata(8.0, OnLayoutPropertyChanged));

        public double NubSize
        {
            get => (double)GetValue(NubSizeProperty);
            set => SetValue(NubSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the checkerboard pattern for alpha channel.
        /// </summary>
        public static readonly DependencyProperty ShowCheckerboardProperty =
            DependencyProperty.Register(
                nameof(ShowCheckerboard),
                typeof(bool),
                typeof(DaisyColorSlider),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool ShowCheckerboard
        {
            get => (bool)GetValue(ShowCheckerboardProperty);
            set => SetValue(ShowCheckerboardProperty, value);
        }

        /// <summary>
        /// Gets or sets an optional callback invoked when the color changes.
        /// This provides a simpler alternative to the ColorChanged event.
        /// </summary>
        public static readonly DependencyProperty OnColorChangedProperty =
            DependencyProperty.Register(
                nameof(OnColorChanged),
                typeof(Action<Color>),
                typeof(DaisyColorSlider),
                new PropertyMetadata(null));

        public Action<Color>? OnColorChanged
        {
            get => (Action<Color>?)GetValue(OnColorChangedProperty);
            set => SetValue(OnColorChangedProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(DaisyColorSlider),
                new PropertyMetadata(0.0, OnLayoutPropertyChanged));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(DaisyColorSlider),
                new PropertyMetadata(255.0, OnLayoutPropertyChanged));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(DaisyColorSlider),
                new PropertyMetadata(0.0, OnValuePropertyChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Clamp(value, Minimum, Maximum));
        }

        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyColorSlider),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        #endregion

        /// <summary>
        /// Occurs when the color value changes.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        public DaisyColorSlider()
        {
            DefaultStyleKey = typeof(DaisyColorSlider);
            BuildVisualTree();

            SizeChanged += OnSizeChanged;
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += OnPointerCaptureLost;

            UpdateMaximum();
            IsTabStop = true;
            UseSystemFocusVisuals = true;
            UpdateAutomationProperties();
        }

        private static void OnChannelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorSlider slider)
            {
                slider.UpdateMaximum();
                slider.UpdateValueFromColor();
                slider.UpdateGradient();
                slider.UpdateMarkerPosition();
                slider.UpdateAutomationProperties();
            }
        }

        private static void OnColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorSlider slider)
            {
                slider.UpdateValueFromColor();
                slider.UpdateGradient();
                slider.UpdateMarkerPosition();
                slider.UpdateAutomationProperties();
            }
        }

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorSlider slider)
            {
                slider.HandleValueChanged((double)e.NewValue);
                slider.UpdateAutomationProperties();
            }
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorSlider slider)
            {
                slider.UpdateGradient();
                slider.UpdateMarkerPosition();
            }
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorSlider slider)
            {
                slider.UpdateAutomationProperties();
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
                case VirtualKey.Down:
                    AdjustValue(-GetKeyboardStep());
                    break;
                case VirtualKey.Right:
                case VirtualKey.Up:
                    AdjustValue(GetKeyboardStep());
                    break;
                case VirtualKey.Home:
                    Value = Minimum;
                    break;
                case VirtualKey.End:
                    Value = Maximum;
                    break;
                case VirtualKey.PageDown:
                    AdjustValue(-GetKeyboardStep() * 10);
                    break;
                case VirtualKey.PageUp:
                    AdjustValue(GetKeyboardStep() * 10);
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    AdjustValue(GetKeyboardStep());
                    break;
                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        private void BuildVisualTree()
        {
            _root = new Grid();

            _checkerboardImage = new Image
            {
                Stretch = Stretch.Fill,
                Visibility = Visibility.Collapsed
            };

            _gradientImage = new Image
            {
                Stretch = Stretch.Fill
            };

            _border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160)),
                BorderThickness = new Thickness(1)
            };

            _markerCanvas = new Canvas();

            _markerMain = new Line
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2
            };

            _markerLeft = new Line
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1
            };

            _markerRight = new Line
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1
            };

            _markerCanvas.Children.Add(_markerMain);
            _markerCanvas.Children.Add(_markerLeft);
            _markerCanvas.Children.Add(_markerRight);

            _root.Children.Add(_checkerboardImage);
            _root.Children.Add(_gradientImage);
            _root.Children.Add(_border);
            _root.Children.Add(_markerCanvas);

            Content = _root;
        }

        private void UpdateMaximum()
        {
            Maximum = Channel switch
            {
                ColorSliderChannel.Hue => 359,
                ColorSliderChannel.Saturation or ColorSliderChannel.Lightness => 100,
                _ => 255
            };
            Minimum = 0;
        }

        private void UpdateValueFromColor()
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                Value = GetChannelValue(Color);
            }
            finally
            {
                _lockUpdates = false;
            }
        }

        private void HandleValueChanged(double newValue)
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                var newColor = SetChannelValue(Color, newValue);
                Color = newColor;
                OnColorChangedRaised(new ColorChangedEventArgs(newColor));
            }
            finally
            {
                _lockUpdates = false;
            }

            UpdateGradient();
            UpdateMarkerPosition();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGradient();
            UpdateMarkerPosition();
        }

        private void UpdateGradient()
        {
            if (_gradientImage == null || ActualWidth <= 0 || ActualHeight <= 0)
                return;

            int width = Math.Max(1, (int)Math.Round(ActualWidth));
            int height = Math.Max(1, (int)Math.Round(ActualHeight));

            if (_gradientBitmap == null || _gradientWidth != width || _gradientHeight != height)
            {
                _gradientBitmap = new WriteableBitmap(width, height);
                _gradientWidth = width;
                _gradientHeight = height;
            }

            var pixels = new byte[width * height * 4];
            bool isHorizontal = Orientation == Orientation.Horizontal;
            double range = Maximum - Minimum;
            if (range <= 0) range = 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double ratio = isHorizontal
                        ? (width <= 1 ? 0 : x / (double)(width - 1))
                        : (height <= 1 ? 0 : y / (double)(height - 1));

                    double value = Minimum + ratio * range;
                    var color = SetChannelValue(Color, value);

                    byte a = color.A;
                    byte r = (byte)(color.R * a / 255);
                    byte g = (byte)(color.G * a / 255);
                    byte b = (byte)(color.B * a / 255);

                    int offset = (y * width + x) * 4;
                    pixels[offset] = b;
                    pixels[offset + 1] = g;
                    pixels[offset + 2] = r;
                    pixels[offset + 3] = a;
                }
            }

            ColorPickerRendering.WritePixels(_gradientBitmap, pixels);
            _gradientImage.Source = _gradientBitmap;

            UpdateCheckerboard(width, height);
        }

        private void UpdateCheckerboard(int width, int height)
        {
            if (_checkerboardImage == null)
                return;

            bool show = Channel == ColorSliderChannel.Alpha && ShowCheckerboard;
            _checkerboardImage.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!show)
                return;

            if (_checkerboardBitmap == null || _checkerboardBitmap.PixelWidth != width || _checkerboardBitmap.PixelHeight != height)
            {
                _checkerboardBitmap = ColorPickerRendering.CreateCheckerboardBitmap(
                    width,
                    height,
                    4,
                    Colors.White,
                    Color.FromArgb(255, 204, 204, 204));
            }

            _checkerboardImage.Source = _checkerboardBitmap;
        }

        private void UpdateMarkerPosition()
        {
            if (_markerMain == null || _markerLeft == null || _markerRight == null)
                return;

            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            double range = Maximum - Minimum;
            if (range <= 0) range = 1;

            double ratio = Clamp((Value - Minimum) / range, 0, 1);
            bool isHorizontal = Orientation == Orientation.Horizontal;

            if (isHorizontal)
            {
                double x = ratio * width;
                _markerMain.X1 = x;
                _markerMain.X2 = x;
                _markerMain.Y1 = 0;
                _markerMain.Y2 = height;

                _markerLeft.X1 = x - 1;
                _markerLeft.X2 = x - 1;
                _markerLeft.Y1 = 0;
                _markerLeft.Y2 = height;

                _markerRight.X1 = x + 1;
                _markerRight.X2 = x + 1;
                _markerRight.Y1 = 0;
                _markerRight.Y2 = height;
            }
            else
            {
                double y = ratio * height;
                _markerMain.X1 = 0;
                _markerMain.X2 = width;
                _markerMain.Y1 = y;
                _markerMain.Y2 = y;

                _markerLeft.X1 = 0;
                _markerLeft.X2 = width;
                _markerLeft.Y1 = y - 1;
                _markerLeft.Y2 = y - 1;

                _markerRight.X1 = 0;
                _markerRight.X2 = width;
                _markerRight.Y1 = y + 1;
                _markerRight.Y2 = y + 1;
            }
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDragging = CapturePointer(e.Pointer);
                UpdateValueFromPoint(e.GetCurrentPoint(this).Position);
                e.Handled = true;
                DaisyAccessibility.FocusOnPointer(this);
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                UpdateValueFromPoint(e.GetCurrentPoint(this).Position);
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

        private void UpdateValueFromPoint(Windows.Foundation.Point point)
        {
            double length = Orientation == Orientation.Horizontal ? ActualWidth : ActualHeight;
            if (length <= 0)
                return;

            double position = Orientation == Orientation.Horizontal ? point.X : point.Y;
            double ratio = Clamp(position / length, 0, 1);
            Value = Minimum + ratio * (Maximum - Minimum);
        }

        private double GetChannelValue(Color color)
        {
            return Channel switch
            {
                ColorSliderChannel.Red => color.R,
                ColorSliderChannel.Green => color.G,
                ColorSliderChannel.Blue => color.B,
                ColorSliderChannel.Alpha => color.A,
                ColorSliderChannel.Hue => new HslColor(color).H,
                ColorSliderChannel.Saturation => new HslColor(color).S * 100,
                ColorSliderChannel.Lightness => new HslColor(color).L * 100,
                _ => 0
            };
        }

        private Color SetChannelValue(Color color, double value)
        {
            return Channel switch
            {
                ColorSliderChannel.Red => Color.FromArgb(color.A, (byte)Clamp(value, 0, 255), color.G, color.B),
                ColorSliderChannel.Green => Color.FromArgb(color.A, color.R, (byte)Clamp(value, 0, 255), color.B),
                ColorSliderChannel.Blue => Color.FromArgb(color.A, color.R, color.G, (byte)Clamp(value, 0, 255)),
                ColorSliderChannel.Alpha => Color.FromArgb((byte)Clamp(value, 0, 255), color.R, color.G, color.B),
                ColorSliderChannel.Hue => GetHslModifiedColor(color, h: value),
                ColorSliderChannel.Saturation => GetHslModifiedColor(color, s: value / 100),
                ColorSliderChannel.Lightness => GetHslModifiedColor(color, l: value / 100),
                _ => color
            };
        }

        private static Color GetHslModifiedColor(Color color, double? h = null, double? s = null, double? l = null)
        {
            var hsl = new HslColor(color);
            if (h.HasValue) hsl.H = h.Value;
            if (s.HasValue) hsl.S = s.Value;
            if (l.HasValue) hsl.L = l.Value;
            return hsl.ToRgbColor(color.A);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        protected virtual void OnColorChangedRaised(ColorChangedEventArgs e)
        {
            ColorChanged?.Invoke(this, e);
            OnColorChanged?.Invoke(e.Color);
        }

        private void AdjustValue(double delta)
        {
            Value = Clamp(Value + delta, Minimum, Maximum);
        }

        private double GetKeyboardStep()
        {
            return Channel == ColorSliderChannel.Hue ? 1 : 1;
        }

        private void UpdateAutomationProperties()
        {
            var name = AccessibleText;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"{GetChannelLabel()} {Value:0.##}";
            }

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

        private string GetChannelLabel()
        {
            return Channel switch
            {
                ColorSliderChannel.Red => "Red",
                ColorSliderChannel.Green => "Green",
                ColorSliderChannel.Blue => "Blue",
                ColorSliderChannel.Alpha => "Alpha",
                ColorSliderChannel.Hue => "Hue",
                ColorSliderChannel.Saturation => "Saturation",
                ColorSliderChannel.Lightness => "Lightness",
                _ => "Channel"
            };
        }
    }
}


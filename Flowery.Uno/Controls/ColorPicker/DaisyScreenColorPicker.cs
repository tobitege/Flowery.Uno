using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Flowery.Services;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Colors = Microsoft.UI.Colors;

namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// A control that allows picking colors from anywhere on the screen using an eyedropper tool.
    /// Click and hold, drag anywhere on screen, release to pick the color.
    /// Supports automatic font scaling when contained within a FloweryScaleManager.EnableScaling="True" container.
    /// </summary>
    public partial class DaisyScreenColorPicker : DaisyBaseContentControl, IScalableControl
    {
        private const double BaseTextFontSize = 10.0;
        private const double HexTextBaseSize = 12.0;
        private const double RgbTextBaseSize = 9.0;
        private const double StatusTextBaseSize = 10.0;

        private bool _isCapturing;
        private Color _previewColor = Colors.Black;
        private ImageBrush? _checkerboardBrush;

        private Border? _rootBorder;
        private Border? _previewBorder;
        private Border? _previewFill;
        private TextBlock? _hexText;
        private TextBlock? _rgbText;
        private TextBlock? _statusText;
        private FrameworkElement? _eyedropperIcon;

        /// <inheritdoc/>
        public void ApplyScaleFactor(double scaleFactor)
        {
            FontSize = FloweryScaleManager.ApplyScale(BaseTextFontSize, 9.0, scaleFactor);
            if (_hexText != null)
                _hexText.FontSize = FloweryScaleManager.ApplyScale(HexTextBaseSize, 10.0, scaleFactor);
            if (_rgbText != null)
                _rgbText.FontSize = FloweryScaleManager.ApplyScale(RgbTextBaseSize, 8.0, scaleFactor);
            if (_statusText != null)
                _statusText.FontSize = FloweryScaleManager.ApplyScale(StatusTextBaseSize, 9.0, scaleFactor);
        }

        #region Dependency Properties

        /// <summary>
        /// Gets or sets the selected color.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(DaisyScreenColorPicker),
                new PropertyMetadata(Colors.Black, OnColorPropertyChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the picker is currently capturing.
        /// </summary>
        public static readonly DependencyProperty IsCapturingProperty =
            DependencyProperty.Register(
                nameof(IsCapturing),
                typeof(bool),
                typeof(DaisyScreenColorPicker),
                new PropertyMetadata(false, OnCapturePropertyChanged));

        public bool IsCapturing
        {
            get => (bool)GetValue(IsCapturingProperty);
            private set => SetValue(IsCapturingProperty, value);
        }

        /// <summary>
        /// Gets or sets an optional callback invoked when the color changes.
        /// </summary>
        public static readonly DependencyProperty OnColorChangedProperty =
            DependencyProperty.Register(
                nameof(OnColorChanged),
                typeof(Action<Color>),
                typeof(DaisyScreenColorPicker),
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
                typeof(DaisyScreenColorPicker),
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

        /// <summary>
        /// Occurs when color picking is completed.
        /// </summary>
        public event EventHandler? PickingCompleted;

        /// <summary>
        /// Occurs when color picking is cancelled.
        /// </summary>
        public event EventHandler? PickingCancelled;

        public DaisyScreenColorPicker()
        {
            DefaultStyleKey = typeof(DaisyScreenColorPicker);
            Width = 140;
            Height = 80;

            BuildVisualTree();

            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += OnPointerCaptureLost;
            KeyDown += OnKeyDown;
            IsTabStop = true;
            UseSystemFocusVisuals = true;
            UpdateAutomationProperties();
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _rootBorder ?? base.GetNeumorphicHostElement();
        }

        private static void OnColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyScreenColorPicker picker)
            {
                picker.UpdateDisplay();
            }
        }

        private static void OnCapturePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyScreenColorPicker picker)
            {
                picker.UpdateDisplay();
            }
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyScreenColorPicker picker)
            {
                picker.UpdateAutomationProperties();
            }
        }

        private void BuildVisualTree()
        {
            _rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 45, 45, 48)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };

            var rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            var topGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(58) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            _previewBorder = new Border
            {
                Width = 50,
                Height = 40,
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
                BorderThickness = new Thickness(1)
            };

            _previewFill = new Border();
            _previewBorder.Child = _previewFill;

            BuildCheckerboardBrush();
            if (_checkerboardBrush != null)
                _previewBorder.Background = _checkerboardBrush;

            _hexText = new TextBlock
            {
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = HexTextBaseSize
            };

            _rgbText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)),
                FontSize = RgbTextBaseSize,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var textPanel = new StackPanel
            {
                Margin = new Thickness(6, 0, 0, 0)
            };
            textPanel.Children.Add(_hexText);
            textPanel.Children.Add(_rgbText);

            Grid.SetColumn(_previewBorder, 0);
            Grid.SetColumn(textPanel, 1);

            topGrid.Children.Add(_previewBorder);
            topGrid.Children.Add(textPanel);

            _statusText = new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = StatusTextBaseSize
            };

            Grid.SetRow(topGrid, 0);
            Grid.SetRow(_statusText, 1);
            Grid.SetColumnSpan(_statusText, 2);

            rootGrid.Children.Add(topGrid);
            rootGrid.Children.Add(_statusText);

            _eyedropperIcon = BuildEyedropperIcon();

            rootGrid.Children.Add(_eyedropperIcon);

            _rootBorder.Child = rootGrid;
            Content = _rootBorder;

            UpdateDisplay();
        }

        private static Canvas BuildEyedropperIcon()
        {
            var stroke = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));
            const double size = 16;
            const double half = size / 2;
            const double strokeThickness = 1.5;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 2, 2)
            };

            var line1 = new Line
            {
                X1 = half - (half * 0.6),
                Y1 = half + (half * 0.6),
                X2 = half - (half * 0.1),
                Y2 = half + (half * 0.1),
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            var line2 = new Line
            {
                X1 = half - (half * 0.1),
                Y1 = half + (half * 0.1),
                X2 = half + (half * 0.4),
                Y2 = half - (half * 0.4),
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            var bulb = new Ellipse
            {
                Width = half * 0.6,
                Height = half * 0.6,
                Stroke = stroke,
                StrokeThickness = strokeThickness
            };

            Canvas.SetLeft(bulb, half + (half * 0.2));
            Canvas.SetTop(bulb, half - (half * 0.8));

            canvas.Children.Add(line1);
            canvas.Children.Add(line2);
            canvas.Children.Add(bulb);

            return canvas;
        }

        private void BuildCheckerboardBrush()
        {
            var bitmap = ColorPickerRendering.CreateCheckerboardBitmap(50, 40, 6, Colors.White, Color.FromArgb(255, 200, 200, 200));
            _checkerboardBrush = new ImageBrush
            {
                ImageSource = bitmap,
                Stretch = Stretch.Fill
            };
        }

        private void UpdateDisplay()
        {
            if (_rootBorder == null || _previewFill == null || _hexText == null || _rgbText == null || _statusText == null)
                return;

            var displayColor = _isCapturing ? _previewColor : Color;
            _previewFill.Background = new SolidColorBrush(displayColor);

            _hexText.Text = $"#{displayColor.R:X2}{displayColor.G:X2}{displayColor.B:X2}";
            _rgbText.Text = $"R:{displayColor.R} G:{displayColor.G} B:{displayColor.B}";

            _statusText.Text = _isCapturing ? "Release to pick" : "Click and drag to pick";
            _statusText.Foreground = new SolidColorBrush(_isCapturing ? Color.FromArgb(255, 100, 200, 100) : Color.FromArgb(255, 150, 150, 150));

            if (_rootBorder.BorderBrush is SolidColorBrush borderBrush)
            {
                borderBrush.Color = _isCapturing ? Color.FromArgb(255, 100, 200, 100) : Color.FromArgb(255, 80, 80, 80);
            }

            if (_eyedropperIcon != null)
                _eyedropperIcon.Visibility = _isCapturing ? Visibility.Collapsed : Visibility.Visible;

            UpdateAutomationProperties();
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                StartCapture();
                CapturePointer(e.Pointer);
                e.Handled = true;
                DaisyAccessibility.FocusOnPointer(this);
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isCapturing)
            {
                UpdateColorFromScreen();
                e.Handled = true;
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isCapturing)
            {
                CompleteCapture();
                ReleasePointerCapture(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (_isCapturing)
            {
                CancelCapture();
            }
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_isCapturing && e.Key == VirtualKey.Escape)
            {
                CancelCapture();
                e.Handled = true;
                return;
            }

            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                if (_isCapturing)
                {
                    UpdateColorFromScreen();
                    CompleteCapture();
                }
                else
                {
                    StartCapture();
                }
                e.Handled = true;
            }
        }

        private void StartCapture()
        {
            if (_isCapturing) return;

            _isCapturing = true;
            IsCapturing = true;
            _previewColor = Color;
            Focus(FocusState.Programmatic);
            UpdateDisplay();
        }

        private void UpdateColorFromScreen()
        {
            if (!_isCapturing) return;

#if WINDOWS
            if (OperatingSystem.IsWindows())
            {
                var pos = GetCursorPosWin32();
                if (pos.HasValue)
                {
                    _previewColor = GetScreenPixelColorWin32((int)pos.Value.X, (int)pos.Value.Y);
                    UpdateDisplay();
                }
            }
#endif
        }

        private void CompleteCapture()
        {
            if (!_isCapturing) return;

            Color = _previewColor;
            StopCapture();

            ColorChanged?.Invoke(this, new ColorChangedEventArgs(Color));
            OnColorChanged?.Invoke(Color);
            PickingCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void CancelCapture()
        {
            if (!_isCapturing) return;

            StopCapture();
            PickingCancelled?.Invoke(this, EventArgs.Empty);
        }

        private void StopCapture()
        {
            _isCapturing = false;
            IsCapturing = false;
            UpdateDisplay();
        }

        private void UpdateAutomationProperties()
        {
            var name = AccessibleText;
            if (string.IsNullOrWhiteSpace(name))
            {
                var displayColor = _isCapturing ? _previewColor : Color;
                name = $"Screen color picker #{displayColor.R:X2}{displayColor.G:X2}{displayColor.B:X2}";
            }

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

#if WINDOWS
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [SupportedOSPlatform("windows")]
        private static Point? GetCursorPosWin32()
        {
            if (GetCursorPos(out POINT pt))
            {
                return new Point(pt.X, pt.Y);
            }
            return null;
        }

        [SupportedOSPlatform("windows")]
        private static Color GetScreenPixelColorWin32(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            try
            {
                uint pixel = GetPixel(hdc, x, y);
                if (pixel == 0xFFFFFFFF)
                {
                    return Colors.Black;
                }
                byte r = (byte)(pixel & 0xFF);
                byte g = (byte)((pixel >> 8) & 0xFF);
                byte b = (byte)((pixel >> 16) & 0xFF);
                return Color.FromArgb(255, r, g, b);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }
#endif
    }
}


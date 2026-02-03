using System;
using Flowery.Controls;
using Flowery.Helpers;
using Flowery.Services;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Colors = Microsoft.UI.Colors;

namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// A comprehensive color editor control with RGB/HSL sliders and numeric inputs.
    /// Supports automatic font scaling when contained within a FloweryScaleManager.EnableScaling="True" container.
    /// </summary>
    public partial class DaisyColorEditor : DaisyBaseContentControl, IScalableControl
    {
        private const double BaseTextFontSize = 12.0;
        private const double SliderRowColumnSpacing = 6.0;

        private bool _lockUpdates;

        private StackPanel? _rootPanel;
        private Grid? _hexPanel;
        private StackPanel? _rgbPanel;
        private StackPanel? _hslPanel;

        private DaisyColorSlider? _redSlider;
        private DaisyColorSlider? _greenSlider;
        private DaisyColorSlider? _blueSlider;
        private DaisyColorSlider? _alphaSlider;
        private DaisyColorSlider? _hueSlider;
        private DaisyColorSlider? _saturationSlider;
        private DaisyColorSlider? _lightnessSlider;
        private DaisyNumericUpDown? _redInput;
        private DaisyNumericUpDown? _greenInput;
        private DaisyNumericUpDown? _blueInput;
        private DaisyNumericUpDown? _alphaInput;
        private DaisyNumericUpDown? _hueInput;
        private DaisyNumericUpDown? _saturationInput;
        private DaisyNumericUpDown? _lightnessInput;
        private DaisyNumericUpDown? _hexInput;
        private Grid? _alphaRow;

        private long _redInputToken;
        private long _greenInputToken;
        private long _blueInputToken;
        private long _alphaInputToken;
        private long _hueInputToken;
        private long _saturationInputToken;
        private long _lightnessInputToken;
        private long _hexInputToken;

        /// <inheritdoc/>
        public void ApplyScaleFactor(double scaleFactor)
        {
            FontSize = FloweryScaleManager.ApplyScale(BaseTextFontSize, 10.0, scaleFactor);
        }

        #region Dependency Properties

        /// <summary>
        /// Gets or sets the selected color.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(DaisyColorEditor),
                new PropertyMetadata(Colors.Red, OnColorPropertyChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the HSL representation of the color.
        /// </summary>
        public static readonly DependencyProperty HslColorProperty =
            DependencyProperty.Register(
                nameof(HslColor),
                typeof(HslColor),
                typeof(DaisyColorEditor),
                new PropertyMetadata(new HslColor(0, 1, 0.5), OnHslColorPropertyChanged));

        public HslColor HslColor
        {
            get => (HslColor)GetValue(HslColorProperty);
            set => SetValue(HslColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the editing mode (RGB or HSL).
        /// </summary>
        public static readonly DependencyProperty EditingModeProperty =
            DependencyProperty.Register(
                nameof(EditingMode),
                typeof(ColorEditingMode),
                typeof(DaisyColorEditor),
                new PropertyMetadata(ColorEditingMode.Rgb));

        public ColorEditingMode EditingMode
        {
            get => (ColorEditingMode)GetValue(EditingModeProperty);
            set => SetValue(EditingModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the alpha channel controls.
        /// </summary>
        public static readonly DependencyProperty ShowAlphaChannelProperty =
            DependencyProperty.Register(
                nameof(ShowAlphaChannel),
                typeof(bool),
                typeof(DaisyColorEditor),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool ShowAlphaChannel
        {
            get => (bool)GetValue(ShowAlphaChannelProperty);
            set => SetValue(ShowAlphaChannelProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the hex input field.
        /// </summary>
        public static readonly DependencyProperty ShowHexInputProperty =
            DependencyProperty.Register(
                nameof(ShowHexInput),
                typeof(bool),
                typeof(DaisyColorEditor),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool ShowHexInput
        {
            get => (bool)GetValue(ShowHexInputProperty);
            set => SetValue(ShowHexInputProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the RGB sliders.
        /// </summary>
        public static readonly DependencyProperty ShowRgbSlidersProperty =
            DependencyProperty.Register(
                nameof(ShowRgbSliders),
                typeof(bool),
                typeof(DaisyColorEditor),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool ShowRgbSliders
        {
            get => (bool)GetValue(ShowRgbSlidersProperty);
            set => SetValue(ShowRgbSlidersProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the HSL sliders.
        /// </summary>
        public static readonly DependencyProperty ShowHslSlidersProperty =
            DependencyProperty.Register(
                nameof(ShowHslSliders),
                typeof(bool),
                typeof(DaisyColorEditor),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool ShowHslSliders
        {
            get => (bool)GetValue(ShowHslSlidersProperty);
            set => SetValue(ShowHslSlidersProperty, value);
        }

        public static readonly DependencyProperty RedProperty =
            DependencyProperty.Register(
                nameof(Red),
                typeof(byte),
                typeof(DaisyColorEditor),
                new PropertyMetadata((byte)255, OnRgbComponentChanged));

        public byte Red
        {
            get => (byte)GetValue(RedProperty);
            set => SetValue(RedProperty, value);
        }

        public static readonly DependencyProperty GreenProperty =
            DependencyProperty.Register(
                nameof(Green),
                typeof(byte),
                typeof(DaisyColorEditor),
                new PropertyMetadata((byte)0, OnRgbComponentChanged));

        public byte Green
        {
            get => (byte)GetValue(GreenProperty);
            set => SetValue(GreenProperty, value);
        }

        public static readonly DependencyProperty BlueProperty =
            DependencyProperty.Register(
                nameof(Blue),
                typeof(byte),
                typeof(DaisyColorEditor),
                new PropertyMetadata((byte)0, OnRgbComponentChanged));

        public byte Blue
        {
            get => (byte)GetValue(BlueProperty);
            set => SetValue(BlueProperty, value);
        }

        public static readonly DependencyProperty AlphaProperty =
            DependencyProperty.Register(
                nameof(Alpha),
                typeof(byte),
                typeof(DaisyColorEditor),
                new PropertyMetadata((byte)255, OnRgbComponentChanged));

#if __ANDROID__ || __IOS__
        public new byte Alpha
#else
        public byte Alpha
#endif
        {
            get => (byte)GetValue(AlphaProperty);
            set => SetValue(AlphaProperty, value);
        }

        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register(
                nameof(Hue),
                typeof(double),
                typeof(DaisyColorEditor),
                new PropertyMetadata(0.0, OnHslComponentChanged));

        public double Hue
        {
            get => (double)GetValue(HueProperty);
            set => SetValue(HueProperty, value);
        }

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register(
                nameof(Saturation),
                typeof(double),
                typeof(DaisyColorEditor),
                new PropertyMetadata(100.0, OnHslComponentChanged));

        public double Saturation
        {
            get => (double)GetValue(SaturationProperty);
            set => SetValue(SaturationProperty, value);
        }

        public static readonly DependencyProperty LightnessProperty =
            DependencyProperty.Register(
                nameof(Lightness),
                typeof(double),
                typeof(DaisyColorEditor),
                new PropertyMetadata(50.0, OnHslComponentChanged));

        public double Lightness
        {
            get => (double)GetValue(LightnessProperty);
            set => SetValue(LightnessProperty, value);
        }

        public static readonly DependencyProperty HexValueProperty =
            DependencyProperty.Register(
                nameof(HexValue),
                typeof(string),
                typeof(DaisyColorEditor),
                new PropertyMetadata("#FF0000", OnHexValueChanged));

        public string HexValue
        {
            get => (string)GetValue(HexValueProperty);
            set => SetValue(HexValueProperty, value ?? string.Empty);
        }

        /// <summary>
        /// Gets or sets an optional callback invoked when the color changes.
        /// This provides a simpler alternative to the ColorChanged event.
        /// </summary>
        public static readonly DependencyProperty OnColorChangedCallbackProperty =
            DependencyProperty.Register(
                nameof(OnColorChangedCallback),
                typeof(Action<Color>),
                typeof(DaisyColorEditor),
                new PropertyMetadata(null));

        public Action<Color>? OnColorChangedCallback
        {
            get => (Action<Color>?)GetValue(OnColorChangedCallbackProperty);
            set => SetValue(OnColorChangedCallbackProperty, value);
        }

        #endregion

        /// <summary>
        /// Occurs when the color changes.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        public DaisyColorEditor()
        {
            DefaultStyleKey = typeof(DaisyColorEditor);
            IsTabStop = false;
            BuildVisualTree();
            UpdateComponentsFromColor(Color);
        }

        private static void OnColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorEditor editor)
            {
                editor.HandleColorChanged((Color)e.NewValue);
            }
        }

        private static void OnHslColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorEditor editor)
            {
                editor.HandleHslColorChanged((HslColor)e.NewValue);
            }
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorEditor editor)
            {
                editor.UpdateVisibility();
                editor.UpdateHexInputConfig();
                editor.SyncHexInputFromColor(editor.Color);
            }
        }

        private static void OnRgbComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorEditor editor)
            {
                editor.UpdateColorFromRgb();
            }
        }

        private static void OnHslComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorEditor editor)
            {
                editor.UpdateColorFromHsl();
            }
        }

        private static void OnHexValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorEditor editor)
            {
                editor.UpdateColorFromHex();
            }
        }

        private void BuildVisualTree()
        {
            _rootPanel = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _hexPanel = BuildHexPanel();
            _rgbPanel = BuildRgbPanel();
            _hslPanel = BuildHslPanel();

            _rootPanel.Children.Add(_hexPanel);
            _rootPanel.Children.Add(_rgbPanel);
            _rootPanel.Children.Add(_hslPanel);

            Content = _rootPanel;

            UpdateVisibility();
            UpdateHexInputConfig();
            SubscribeToControls();
        }

        private Grid BuildHexPanel()
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = SliderRowColumnSpacing
            };

            var label = new TextBlock
            {
                Text = "Hex:",
                VerticalAlignment = VerticalAlignment.Center
            };
            FlowerySizeManager.SetResponsiveFont(label, ResponsiveFontTier.Primary);

            _hexInput = CreateHexInput();

            Grid.SetColumn(label, 0);
            Grid.SetColumn(_hexInput, 1);

            grid.Children.Add(label);
            grid.Children.Add(_hexInput);

            return grid;
        }

        private StackPanel BuildRgbPanel()
        {
            var panel = new StackPanel { Spacing = 3 };

            var header = new TextBlock
            {
                Text = "RGB",
                FontWeight = FontWeights.SemiBold,
                Opacity = 0.8
            };
            FlowerySizeManager.SetResponsiveFont(header, ResponsiveFontTier.Primary);
            panel.Children.Add(header);

            _redSlider = new DaisyColorSlider { Channel = ColorSliderChannel.Red, Height = 18 };
            _greenSlider = new DaisyColorSlider { Channel = ColorSliderChannel.Green, Height = 18 };
            _blueSlider = new DaisyColorSlider { Channel = ColorSliderChannel.Blue, Height = 18 };
            _alphaSlider = new DaisyColorSlider { Channel = ColorSliderChannel.Alpha, Height = 18 };

            _redInput = CreateNumericInput(0, 255);
            _greenInput = CreateNumericInput(0, 255);
            _blueInput = CreateNumericInput(0, 255);
            _alphaInput = CreateNumericInput(0, 255);

            panel.Children.Add(BuildSliderRow("R:", _redSlider, _redInput));
            panel.Children.Add(BuildSliderRow("G:", _greenSlider, _greenInput));
            panel.Children.Add(BuildSliderRow("B:", _blueSlider, _blueInput));
            _alphaRow = BuildSliderRow("A:", _alphaSlider, _alphaInput);
            panel.Children.Add(_alphaRow);

            return panel;
        }

        private StackPanel BuildHslPanel()
        {
            var panel = new StackPanel { Spacing = 3 };

            var header = new TextBlock
            {
                Text = "HSL",
                FontWeight = FontWeights.SemiBold,
                Opacity = 0.8
            };
            FlowerySizeManager.SetResponsiveFont(header, ResponsiveFontTier.Primary);
            panel.Children.Add(header);

            _hueSlider = new DaisyColorSlider { Channel = ColorSliderChannel.Hue, Height = 18 };
            _saturationSlider = new DaisyColorSlider { Channel = ColorSliderChannel.Saturation, Height = 18 };
            _lightnessSlider = new DaisyColorSlider { Channel = ColorSliderChannel.Lightness, Height = 18 };

            _hueInput = CreateNumericInput(0, 359);
            _saturationInput = CreateNumericInput(0, 100);
            _lightnessInput = CreateNumericInput(0, 100);

            panel.Children.Add(BuildSliderRow("H:", _hueSlider, _hueInput));
            panel.Children.Add(BuildSliderRow("S:", _saturationSlider, _saturationInput));
            panel.Children.Add(BuildSliderRow("L:", _lightnessSlider, _lightnessInput));

            return panel;
        }

        private DaisyNumericUpDown CreateHexInput()
        {
            var input = new DaisyNumericUpDown
            {
                NumberBase = DaisyNumberBase.ColorHex,
                HexCase = DaisyHexCase.Upper,
                ShowBasePrefix = true,
                ShowButtons = false,
                Minimum = 0
            };
            ApplyNumericInputSizing(input);
            input.RegisterPropertyChangedCallback(DaisyNumericUpDown.SizeProperty, OnNumericInputSizeChanged);
            return input;
        }

        private DaisyNumericUpDown CreateNumericInput(decimal minimum, decimal maximum)
        {
            var input = new DaisyNumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum
            };
            ApplyNumericInputSizing(input);
            input.RegisterPropertyChangedCallback(DaisyNumericUpDown.SizeProperty, OnNumericInputSizeChanged);
            return input;
        }

        private static Grid BuildSliderRow(string label, DaisyColorSlider slider, DaisyNumericUpDown input)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(24) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = SliderRowColumnSpacing
            };

            var labelBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center
            };
            FlowerySizeManager.SetResponsiveFont(labelBlock, ResponsiveFontTier.Primary);

            slider.VerticalAlignment = VerticalAlignment.Center;
            input.VerticalAlignment = VerticalAlignment.Center;

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(slider, 1);
            Grid.SetColumn(input, 2);

            grid.Children.Add(labelBlock);
            grid.Children.Add(slider);
            grid.Children.Add(input);

            return grid;
        }

        private static double GetNumericInputMinWidth(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 72,
            DaisySize.Small => 84,
            DaisySize.Medium => 96,
            DaisySize.Large => 108,
            DaisySize.ExtraLarge => 128,
            _ => 96
        };

        private static void ApplyNumericInputSizing(DaisyNumericUpDown input)
        {
            input.MinWidth = GetNumericInputMinWidth(input.Size);
        }

        private void OnNumericInputSizeChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is DaisyNumericUpDown input)
            {
                ApplyNumericInputSizing(input);
            }
        }

        private void SubscribeToControls()
        {
            if (_redSlider != null) _redSlider.ColorChanged += OnSliderColorChanged;
            if (_greenSlider != null) _greenSlider.ColorChanged += OnSliderColorChanged;
            if (_blueSlider != null) _blueSlider.ColorChanged += OnSliderColorChanged;
            if (_alphaSlider != null) _alphaSlider.ColorChanged += OnSliderColorChanged;
            if (_hueSlider != null) _hueSlider.ColorChanged += OnSliderColorChanged;
            if (_saturationSlider != null) _saturationSlider.ColorChanged += OnSliderColorChanged;
            if (_lightnessSlider != null) _lightnessSlider.ColorChanged += OnSliderColorChanged;

            if (_redInput != null) _redInputToken = _redInput.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnNumericValueChanged);
            if (_greenInput != null) _greenInputToken = _greenInput.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnNumericValueChanged);
            if (_blueInput != null) _blueInputToken = _blueInput.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnNumericValueChanged);
            if (_alphaInput != null) _alphaInputToken = _alphaInput.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnNumericValueChanged);
            if (_hueInput != null) _hueInputToken = _hueInput.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnNumericValueChanged);
            if (_saturationInput != null) _saturationInputToken = _saturationInput.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnNumericValueChanged);
            if (_lightnessInput != null) _lightnessInputToken = _lightnessInput.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnNumericValueChanged);

            if (_hexInput != null)
            {
                _hexInputToken = _hexInput.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnHexNumericValueChanged);
            }
        }

        private void HandleColorChanged(Color color)
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                UpdateComponentsFromColor(color);
                OnColorChangedRaised(new ColorChangedEventArgs(color));
            }
            finally
            {
                _lockUpdates = false;
            }
        }

        private void HandleHslColorChanged(HslColor hsl)
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
        }

        private void UpdateComponentsFromColor(Color color)
        {
            Red = color.R;
            Green = color.G;
            Blue = color.B;
            Alpha = color.A;

            var hsl = new HslColor(color);
            Hue = hsl.H;
            Saturation = hsl.S * 100;
            Lightness = hsl.L * 100;

            HslColor = hsl;
            UpdateHexFromRgb(color);
            UpdateTemplateControls(color);
        }

        private void UpdateTemplateControls(Color color)
        {
            if (_redSlider != null) _redSlider.Color = color;
            if (_greenSlider != null) _greenSlider.Color = color;
            if (_blueSlider != null) _blueSlider.Color = color;
            if (_alphaSlider != null) _alphaSlider.Color = color;
            if (_hueSlider != null) _hueSlider.Color = color;
            if (_saturationSlider != null) _saturationSlider.Color = color;
            if (_lightnessSlider != null) _lightnessSlider.Color = color;

            if (_redInput != null) _redInput.Value = color.R;
            if (_greenInput != null) _greenInput.Value = color.G;
            if (_blueInput != null) _blueInput.Value = color.B;
            if (_alphaInput != null) _alphaInput.Value = color.A;
            if (_hueInput != null) _hueInput.Value = (decimal)Hue;
            if (_saturationInput != null) _saturationInput.Value = (decimal)Saturation;
            if (_lightnessInput != null) _lightnessInput.Value = (decimal)Lightness;

            if (_hexInput != null) _hexInput.Value = GetHexNumericValue(color, ShowAlphaChannel);
        }

        private void UpdateColorFromRgb()
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                var color = Color.FromArgb(Alpha, Red, Green, Blue);
                Color = color;
                UpdateHslFromRgb(color);
                UpdateHexFromRgb(color);
                UpdateTemplateControls(color);
                OnColorChangedRaised(new ColorChangedEventArgs(color));
            }
            finally
            {
                _lockUpdates = false;
            }
        }

        private void UpdateColorFromHsl()
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                var hsl = new HslColor(Alpha, Hue, Saturation / 100, Lightness / 100);
                var color = hsl.ToRgbColor();
                Color = color;
                UpdateRgbFromColor(color);
                UpdateHexFromRgb(color);
                HslColor = hsl;
                UpdateTemplateControls(color);
                OnColorChangedRaised(new ColorChangedEventArgs(color));
            }
            finally
            {
                _lockUpdates = false;
            }
        }

        private void UpdateColorFromHex()
        {
            if (_lockUpdates) return;

            var input = HexValue ?? string.Empty;
            if (!FloweryColorHelpers.TryParseColor(input, out var color))
                return;

            _lockUpdates = true;
            try
            {
                Color = color;
                UpdateRgbFromColor(color);
                UpdateHslFromRgb(color);
                UpdateTemplateControls(color);
                OnColorChangedRaised(new ColorChangedEventArgs(color));
            }
            finally
            {
                _lockUpdates = false;
            }
        }

        private void UpdateHslFromRgb(Color color)
        {
            var hsl = new HslColor(color);
            Hue = hsl.H;
            Saturation = hsl.S * 100;
            Lightness = hsl.L * 100;
            HslColor = hsl;
        }

        private void UpdateRgbFromColor(Color color)
        {
            Red = color.R;
            Green = color.G;
            Blue = color.B;
            Alpha = color.A;
        }

        private void UpdateHexFromRgb(Color color)
        {
            var hex = FloweryColorHelpers.ColorToHex(color, ShowAlphaChannel);

            if (_lockUpdates)
            {
                HexValue = hex;
                return;
            }

            _lockUpdates = true;
            try
            {
                HexValue = hex;
            }
            finally
            {
                _lockUpdates = false;
            }
        }

        private void UpdateVisibility()
        {
            if (_hexPanel != null)
                _hexPanel.Visibility = ShowHexInput ? Visibility.Visible : Visibility.Collapsed;

            if (_rgbPanel != null)
                _rgbPanel.Visibility = ShowRgbSliders ? Visibility.Visible : Visibility.Collapsed;

            if (_hslPanel != null)
                _hslPanel.Visibility = ShowHslSliders ? Visibility.Visible : Visibility.Collapsed;

            if (_alphaSlider != null)
                _alphaSlider.Visibility = ShowAlphaChannel ? Visibility.Visible : Visibility.Collapsed;

            if (_alphaInput != null)
                _alphaInput.Visibility = ShowAlphaChannel ? Visibility.Visible : Visibility.Collapsed;

            if (_alphaRow != null)
                _alphaRow.Visibility = ShowAlphaChannel ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnSliderColorChanged(object? sender, ColorChangedEventArgs e)
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                Color = e.Color;
                UpdateComponentsFromColor(e.Color);
                OnColorChangedRaised(new ColorChangedEventArgs(e.Color));
            }
            finally
            {
                _lockUpdates = false;
            }
        }

        private void OnNumericValueChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_lockUpdates) return;

            if (sender == _redInput && _redInput?.Value != null)
                Red = (byte)_redInput.Value.Value;
            else if (sender == _greenInput && _greenInput?.Value != null)
                Green = (byte)_greenInput.Value.Value;
            else if (sender == _blueInput && _blueInput?.Value != null)
                Blue = (byte)_blueInput.Value.Value;
            else if (sender == _alphaInput && _alphaInput?.Value != null)
                Alpha = (byte)_alphaInput.Value.Value;
            else if (sender == _hueInput && _hueInput?.Value != null)
                Hue = (double)_hueInput.Value.Value;
            else if (sender == _saturationInput && _saturationInput?.Value != null)
                Saturation = (double)_saturationInput.Value.Value;
            else if (sender == _lightnessInput && _lightnessInput?.Value != null)
                Lightness = (double)_lightnessInput.Value.Value;
        }

        private void OnHexNumericValueChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_lockUpdates || _hexInput?.Value == null)
                return;

            var digits = Math.Max(1, _hexInput.ColorHexDigits);
            var hex = ((long)_hexInput.Value.Value).ToString($"X{digits}");
            HexValue = "#" + hex;
        }

        private void UpdateHexInputConfig()
        {
            if (_hexInput == null)
                return;

            _hexInput.NumberBase = DaisyNumberBase.ColorHex;
            _hexInput.HexCase = DaisyHexCase.Upper;
            _hexInput.ShowBasePrefix = true;
            _hexInput.ShowButtons = false;
            _hexInput.Minimum = 0;
            _hexInput.Maximum = ShowAlphaChannel ? 0xFFFFFFFF : 0xFFFFFF;
            _hexInput.ColorHexDigits = ShowAlphaChannel ? 8 : 6;
        }

        private void SyncHexInputFromColor(Color color)
        {
            if (_hexInput == null)
                return;

            var hex = FloweryColorHelpers.ColorToHex(color, ShowAlphaChannel);
            var numeric = GetHexNumericValue(color, ShowAlphaChannel);

            var wasLocked = _lockUpdates;
            _lockUpdates = true;
            try
            {
                HexValue = hex;
                _hexInput.Value = numeric;
            }
            finally
            {
                _lockUpdates = wasLocked;
            }
        }

        private static decimal GetHexNumericValue(Color color, bool includeAlpha)
        {
            uint value = includeAlpha
                ? ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B
                : ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
            return value;
        }

        protected virtual void OnColorChangedRaised(ColorChangedEventArgs e)
        {
            ColorChanged?.Invoke(this, e);
            OnColorChangedCallback?.Invoke(e.Color);
        }
    }
}


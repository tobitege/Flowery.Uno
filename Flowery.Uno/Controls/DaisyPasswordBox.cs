using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Flowery.Theming;
using Flowery.Enums;
using Flowery.Localization;
using Windows.Foundation;

namespace Flowery.Controls
{
    /// <summary>
    /// A PasswordBox control styled after DaisyUI's Input component.
    /// Supports labels, helper text, icons, and floating label mode.
    /// </summary>
    public partial class DaisyPasswordBox : DaisyBaseContentControl, IFocusableInput
    {
        private Grid? _rootGrid;
        private StackPanel? _labelPanel;
        private TextBlock? _labelText;
        private TextBlock? _requiredIndicator;
        private TextBlock? _optionalText;
        private TextBlock? _hintTextBlock;
        private Border? _inputBorder;
        private Grid? _inputHost;
        private Grid? _row2Container;
        private Grid? _inputGrid;
        private Viewbox? _startIconViewbox;
        private Path? _startIconPath;
        private Viewbox? _endIconViewbox;
        private Path? _endIconPath;
        private PasswordBox? _passwordBox;
        private Button? _revealButton;
        private Path? _revealIconPath;
        private TextBlock? _helperTextBlock;
        private Border? _floatingLabelBorder;
        private TextBlock? _floatingLabelText;

        private bool _isLoaded;
        private bool _isPasswordBoxFocused;
        private bool _isPointerOverInput;
        private CompositeTransform? _floatingLabelTransform;
        private Storyboard? _floatingLabelStoryboard;
        private long _paddingChangedToken;
        private long _verticalContentAlignmentChangedToken;
        private CancellationTokenSource? _floatingPlaceholderCts;
        private Brush? _baseInputBackground;
        private Brush? _baseInputBorderBrush;
        private bool _pendingFocusRequest;
        private FocusState _pendingFocusState = FocusState.Programmatic;
        public DaisyPasswordBox()
        {
            DefaultStyleKey = typeof(DaisyPasswordBox);
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                ApplyAll();
                return;
            }

            _isLoaded = true;
            BuildVisualTree();
            ApplyAll();
            _paddingChangedToken = RegisterPropertyChangedCallback(Control.PaddingProperty, OnPaddingChanged);
            _verticalContentAlignmentChangedToken = RegisterPropertyChangedCallback(Control.VerticalContentAlignmentProperty, OnVerticalContentAlignmentChanged);
            if (_pendingFocusRequest)
            {
                AttachPendingFocusHandler();
                TryFocusPasswordBox(_pendingFocusState);
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            _isLoaded = false;
            CancelFloatingPlaceholderDelay();

            if (_paddingChangedToken != 0)
            {
                UnregisterPropertyChangedCallback(Control.PaddingProperty, _paddingChangedToken);
                _paddingChangedToken = 0;
            }

            if (_verticalContentAlignmentChangedToken != 0)
            {
                UnregisterPropertyChangedCallback(Control.VerticalContentAlignmentProperty, _verticalContentAlignmentChangedToken);
                _verticalContentAlignmentChangedToken = 0;
            }

            _pendingFocusRequest = false;
            DetachPendingFocusHandler();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _inputHost ?? base.GetNeumorphicHostElement();
        }

        public void FocusInput(bool selectAll = false)
        {
            if (TryFocusPasswordBox(FocusState.Programmatic))
                return;

            _pendingFocusRequest = true;
            _pendingFocusState = FocusState.Programmatic;

            if (IsLoaded)
            {
                AttachPendingFocusHandler();
            }
        }

        private void AttachPendingFocusHandler()
        {
            LayoutUpdated -= OnPendingFocusLayoutUpdated;
            LayoutUpdated += OnPendingFocusLayoutUpdated;
        }

        private void DetachPendingFocusHandler()
        {
            LayoutUpdated -= OnPendingFocusLayoutUpdated;
        }

        private void OnPendingFocusLayoutUpdated(object? sender, object e)
        {
            if (!_pendingFocusRequest)
            {
                DetachPendingFocusHandler();
                return;
            }

            if (TryFocusPasswordBox(_pendingFocusState))
            {
                DetachPendingFocusHandler();
            }
        }

        private bool TryFocusPasswordBox(FocusState focusState)
        {
            if (_passwordBox is not { } passwordBox)
                return false;

            if (!IsLoaded || !passwordBox.IsLoaded || passwordBox.Visibility != Visibility.Visible)
                return false;

            var dispatcher = passwordBox.DispatcherQueue;
            if (dispatcher == null)
                return false;

            _pendingFocusRequest = false;
            dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (!IsLoaded || passwordBox.Visibility != Visibility.Visible)
                    return;

                passwordBox.Focus(focusState);
            });
            return true;
        }

        private void OnPaddingChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_passwordBox == null)
                return;

            _passwordBox.Padding = Padding;
            UpdateFloatingLabelMargin();
        }

        private void OnVerticalContentAlignmentChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_passwordBox == null)
                return;

            _passwordBox.VerticalContentAlignment = VerticalContentAlignment;
        }


        private void BuildVisualTree()
        {
            _rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }, // Label
                    new RowDefinition { Height = GridLength.Auto }, // Hint
                    new RowDefinition { Height = GridLength.Auto }, // Input
                    new RowDefinition { Height = GridLength.Auto }  // Helper
                }
            };

            // Row 0: Label panel
            _labelPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(0, 0, 0, 4)
            };

            _labelText = new TextBlock
            {
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            _labelPanel.Children.Add(_labelText);

            _requiredIndicator = new TextBlock
            {
                Text = "*",
                Visibility = Visibility.Collapsed
            };
            _labelPanel.Children.Add(_requiredIndicator);

            _optionalText = new TextBlock
            {
                Text = FloweryLocalization.GetStringInternal("Input_Optional", "Optional"),
                Opacity = 0.7,
                Visibility = Visibility.Collapsed
            };
            _labelPanel.Children.Add(_optionalText);

            Grid.SetRow(_labelPanel, 0);
            _rootGrid.Children.Add(_labelPanel);

            // Row 1: Hint text
            _hintTextBlock = new TextBlock
            {
                Opacity = 0.7,
                Margin = new Thickness(0, 0, 0, 4),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(_hintTextBlock, 1);
            _rootGrid.Children.Add(_hintTextBlock);

            // Row 2: Input area
            _inputBorder = new Border
            {
                CornerRadius = new CornerRadius(8)
            };

            _inputGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // Start icon
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // PasswordBox
                    new ColumnDefinition { Width = GridLength.Auto }, // Reveal Button
                    new ColumnDefinition { Width = GridLength.Auto }  // End icon
                }
            };

            // Start icon
            _startIconViewbox = new Viewbox
            {
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            _startIconPath = new Path
            {
                Width = 24,
                Height = 24,
                Stretch = Stretch.Fill,
                StrokeThickness = 1.5
            };
            _startIconViewbox.Child = _startIconPath;
            Grid.SetColumn(_startIconViewbox, 0);
            _inputGrid.Children.Add(_startIconViewbox);

            // PasswordBox
            _passwordBox = new PasswordBox
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(12, 0, 12, 0)
            };

            var transparentBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            _passwordBox.BorderBrush = transparentBrush;
            _passwordBox.Resources["TextControlBorderBrush"] = transparentBrush;
            _passwordBox.Resources["TextControlBorderBrushPointerOver"] = transparentBrush;
            _passwordBox.Resources["TextControlBorderBrushFocused"] = transparentBrush;
            _passwordBox.Resources["TextControlBorderBrushDisabled"] = transparentBrush;
            _passwordBox.Resources["TextControlBackground"] = transparentBrush;
            _passwordBox.Resources["TextControlBackgroundPointerOver"] = transparentBrush;
            _passwordBox.Resources["TextControlBackgroundFocused"] = transparentBrush;
            _passwordBox.Resources["TextControlBackgroundDisabled"] = transparentBrush;
            _passwordBox.Resources["TextControlBorderThemeThickness"] = new Thickness(0);
            _passwordBox.Resources["TextControlBorderThemeThicknessPointerOver"] = new Thickness(0);
            _passwordBox.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0);
            _passwordBox.Resources["TextControlBorderThemeThicknessDisabled"] = new Thickness(0);

            _passwordBox.PasswordChanged += OnInnerPasswordChanged;
            _passwordBox.GotFocus += OnInnerGotFocus;
            _passwordBox.LostFocus += OnInnerLostFocus;

            Grid.SetColumn(_passwordBox, 1);
            _inputGrid.Children.Add(_passwordBox);

            // Custom Reveal Button (Scales correctly matching DaisyNumericUpDown pattern)
            // Use standard Button with stripped properties instead of ToggleButton to avoid template bloat
            _revealButton = new Button
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 4, 0), // Use Margin for visual spacing
                MinHeight = 0,
                MinWidth = 0,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            // Remove native hover/pressed backgrounds
            _revealButton.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Colors.Transparent);
            _revealButton.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Colors.Transparent);
            _revealButton.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Colors.Transparent);
            _revealButton.Resources["ButtonBorderBrushPressed"] = new SolidColorBrush(Colors.Transparent);

            _revealButton.Click += OnRevealButtonClick;

            // Reveal Icon Path
            _revealIconPath = new Path
            {
                Stretch = Stretch.Uniform,
                Fill = new SolidColorBrush(Colors.Black) // Will be theming updated
            };
            _revealButton.Content = _revealIconPath;

            Grid.SetColumn(_revealButton, 2);
            _inputGrid.Children.Add(_revealButton);

            // End icon (moved to column 3)
            _endIconViewbox = new Viewbox
            {
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            _endIconPath = new Path
            {
                Width = 24,
                Height = 24,
                Stretch = Stretch.Fill,
                StrokeThickness = 1.5
            };
            _endIconViewbox.Child = _endIconPath;
            Grid.SetColumn(_endIconViewbox, 3);
            _inputGrid.Children.Add(_endIconViewbox);

            // Floating label
            _floatingLabelBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(4, 0, 4, 0),
                CornerRadius = new CornerRadius(4),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                RenderTransformOrigin = new Windows.Foundation.Point(0, 0.5)
            };
            _floatingLabelTransform = new CompositeTransform();
            _floatingLabelBorder.RenderTransform = _floatingLabelTransform;

            var floatingPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };

            _floatingLabelText = new TextBlock();
            floatingPanel.Children.Add(_floatingLabelText);

            var floatingRequired = new TextBlock
            {
                Text = "*",
                Visibility = Visibility.Collapsed
            };
            floatingPanel.Children.Add(floatingRequired);

            _floatingLabelBorder.Child = floatingPanel;

            _inputHost = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _inputHost.Children.Add(_inputBorder);
            _inputHost.Children.Add(_inputGrid);

            _inputHost.PointerEntered += OnInputPointerEntered;
            _inputHost.PointerExited += OnInputPointerExited;

            _row2Container = new Grid();
            Grid.SetRow(_row2Container, 2);
            _rootGrid.Children.Add(_row2Container);

            _row2Container.Children.Add(_inputHost);
            _row2Container.Children.Add(_floatingLabelBorder);

            // Row 3: Helper text
            _helperTextBlock = new TextBlock
            {
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(_helperTextBlock, 3);
            _rootGrid.Children.Add(_helperTextBlock);

            Content = _rootGrid;
            SyncPasswordBoxProperties();
        }

        private void OnInnerPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_passwordBox != null)
            {
                Password = _passwordBox.Password;
                HasText = !string.IsNullOrEmpty(_passwordBox.Password);
                UpdateFloatingLabelState();
                PasswordChanged?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Occurs when the password content changes.
        /// </summary>
        public event RoutedEventHandler? PasswordChanged;

        private void OnInnerGotFocus(object sender, RoutedEventArgs e)
        {
            _isPasswordBoxFocused = true;
            UpdateFloatingLabelState(true);
            UpdateFocusBorder();
        }

        private void OnInnerLostFocus(object sender, RoutedEventArgs e)
        {
            _isPasswordBoxFocused = false;
            UpdateFloatingLabelState(true);
            UpdateFocusBorder();
        }

        private void OnInputPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverInput = true;
            ApplyInputBorderInteractionState();
        }

        private void OnInputPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverInput = false;
            ApplyInputBorderInteractionState();
        }

        private void UpdateFloatingLabelState(bool animate = false)
        {
            if (_floatingLabelBorder == null || _floatingLabelTransform == null || _passwordBox == null)
                return;

            if (LabelPosition != DaisyLabelPosition.Floating)
                return;

            UpdateFloatingLabelMargin();

            var isFocused = _passwordBox.FocusState != FocusState.Unfocused;
            _isPasswordBoxFocused = isFocused;
            var shouldFloat = isFocused || HasText;

            UpdateFloatingPlaceholder();

            var resources = Application.Current?.Resources;
            var baseContent = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") ?? new SolidColorBrush(Colors.Black);
            var primary = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush") ?? new SolidColorBrush(Colors.Blue);

            var inputHeight = _inputBorder?.MinHeight > 0 ? _inputBorder.MinHeight : DaisyResourceLookup.GetDefaultHeight(Size);
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            var floatingHeight = resources != null
                ? DaisyResourceLookup.GetDouble(resources, $"DaisyInputFloating{sizeKey}Height", inputHeight)
                : inputHeight;

            var fontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
            var labelFontSize = fontSize * 0.85;
            var labelHeight = labelFontSize * 1.5;
            var minFloatingTopSpace = labelHeight + 6;
            var floatingTopSpace = Math.Max(minFloatingTopSpace, floatingHeight - inputHeight);

            double targetScale;
            double targetTranslateY;
            double targetOpacity;
            TimeSpan transformDelay;

            if (shouldFloat)
            {
                targetScale = Size == DaisySize.ExtraSmall ? 0.80 : 0.85;
                targetTranslateY = 0.0;
                targetOpacity = 1.0;
                transformDelay = TimeSpan.FromMilliseconds(50);

                _floatingLabelBorder.BorderBrush = primary;
                _floatingLabelBorder.BorderThickness = new Thickness(1);
                _floatingLabelBorder.Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");

                if (_floatingLabelText != null)
                {
                    _floatingLabelText.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                    _floatingLabelText.Foreground = primary;
                }
            }
            else
            {
                targetScale = 1.0;
                var availableInputHeight = inputHeight - 2;
                var fontLineHeight = Math.Ceiling(fontSize * 1.5);
                var verticalPadding = Math.Max(0, Math.Floor((availableInputHeight - fontLineHeight) / 2));
                var topPadding = Math.Max(0, verticalPadding - 1);

                var borderTop = _inputBorder?.BorderThickness.Top ?? 0;
                targetTranslateY = floatingTopSpace + borderTop + topPadding + 1;

                targetOpacity = 0.5;
                transformDelay = TimeSpan.Zero;

                _floatingLabelBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
                _floatingLabelBorder.BorderThickness = new Thickness(0);
                _floatingLabelBorder.Background = new SolidColorBrush(Colors.Transparent);

                if (_floatingLabelText != null)
                {
                    _floatingLabelText.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                    _floatingLabelText.Foreground = baseContent;
                }
            }

            if (!animate)
            {
                _floatingLabelStoryboard?.Stop();
                _floatingLabelTransform.ScaleX = targetScale;
                _floatingLabelTransform.ScaleY = targetScale;
                _floatingLabelTransform.TranslateY = targetTranslateY;
                _floatingLabelBorder.Opacity = targetOpacity;
            }
            else
            {
                AnimateFloatingLabelTo(targetScale, targetTranslateY, targetOpacity, transformDelay);
            }
        }

        private void AnimateFloatingLabelTo(double targetScale, double targetTranslateY, double targetOpacity, TimeSpan transformDelay)
        {
            if (_floatingLabelBorder == null || _floatingLabelTransform == null)
                return;

            _floatingLabelStoryboard?.Stop();

            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            var sb = new Storyboard();

            var scaleX = new DoubleAnimation
            {
                To = targetScale,
                Duration = new Duration(TimeSpan.FromMilliseconds(280)),
                BeginTime = transformDelay,
                EasingFunction = easing,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(scaleX, _floatingLabelTransform);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");
            sb.Children.Add(scaleX);

            var scaleY = new DoubleAnimation
            {
                To = targetScale,
                Duration = new Duration(TimeSpan.FromMilliseconds(280)),
                BeginTime = transformDelay,
                EasingFunction = easing,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(scaleY, _floatingLabelTransform);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");
            sb.Children.Add(scaleY);

            var translateY = new DoubleAnimation
            {
                To = targetTranslateY,
                Duration = new Duration(TimeSpan.FromMilliseconds(280)),
                BeginTime = transformDelay,
                EasingFunction = easing,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(translateY, _floatingLabelTransform);
            Storyboard.SetTargetProperty(translateY, "TranslateY");
            sb.Children.Add(translateY);

            var opacity = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(opacity, _floatingLabelBorder);
            Storyboard.SetTargetProperty(opacity, "Opacity");
            sb.Children.Add(opacity);

            _floatingLabelStoryboard = sb;
            sb.Begin();
        }

        private void UpdateFocusBorder()
        {
            ApplyInputBorderInteractionState();
        }

        private void ApplyInputBorderInteractionState()
        {
            if (_inputBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            var isFocused = _passwordBox != null && _passwordBox.FocusState != FocusState.Unfocused;
            if (isFocused)
            {
                _inputBorder.BorderBrush = GetVariantFocusBrush();
                if (_baseInputBackground != null)
                    _inputBorder.Background = _baseInputBackground;
                return;
            }

            if (_isPointerOverInput && (Variant == DaisyInputVariant.Bordered || Variant == DaisyInputVariant.Filled))
            {
                var baseContent = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") ?? new SolidColorBrush(Colors.Black);
                _inputBorder.BorderBrush = baseContent;

                if (Variant == DaisyInputVariant.Filled)
                {
                    var base300 = DaisyResourceLookup.GetBrush("DaisyBase300Brush") ?? new SolidColorBrush(Colors.Gray);
                    _inputBorder.Background = base300;
                }

                return;
            }

            if (_baseInputBorderBrush != null)
                _inputBorder.BorderBrush = _baseInputBorderBrush;
            if (_baseInputBackground != null)
                _inputBorder.Background = _baseInputBackground;
        }

        private Brush GetVariantFocusBrush()
        {
            if (BorderRingBrush != null)
                return BorderRingBrush;

            var primary = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush") ?? new SolidColorBrush(Colors.Blue);

            return Variant switch
            {
                DaisyInputVariant.Primary => primary,
                DaisyInputVariant.Secondary => DaisyResourceLookup.GetBrush("DaisySecondaryBrush") ?? primary,
                DaisyInputVariant.Accent => DaisyResourceLookup.GetBrush("DaisyAccentBrush") ?? primary,
                DaisyInputVariant.Info => DaisyResourceLookup.GetBrush("DaisyInfoBrush") ?? primary,
                DaisyInputVariant.Success => DaisyResourceLookup.GetBrush("DaisySuccessBrush") ?? primary,
                DaisyInputVariant.Warning => DaisyResourceLookup.GetBrush("DaisyWarningBrush") ?? primary,
                DaisyInputVariant.Error => DaisyResourceLookup.GetBrush("DaisyErrorBrush") ?? primary,
                _ => primary
            };
        }

        #region HasText (read-only)
        public static readonly DependencyProperty HasTextProperty =
            DependencyProperty.Register(
                nameof(HasText),
                typeof(bool),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(false));

        public bool HasText
        {
            get => (bool)GetValue(HasTextProperty);
            private set => SetValue(HasTextProperty, value);
        }
        #endregion

        #region Password
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPasswordBox input && input._passwordBox != null)
            {
                var newText = (string?)e.NewValue ?? string.Empty;
                if (input._passwordBox.Password != newText)
                {
                    input._passwordBox.Password = newText;
                }
                input.HasText = !string.IsNullOrEmpty(newText);
            }
        }
        #endregion

        #region PlaceholderText
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnPlaceholderTextChanged));

        public string? PlaceholderText
        {
            get => (string?)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(
                nameof(Watermark),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnWatermarkChanged));

        public string? Watermark
        {
            get => PlaceholderText;
            set => SetValue(WatermarkProperty, value);
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnAppearancePropertyChanged(d, e);

            if (d is DaisyPasswordBox input)
            {
                var newValue = (string?)e.NewValue;
                if (!Equals(input.GetValue(WatermarkProperty), newValue))
                {
                    input.SetValue(WatermarkProperty, newValue);
                }
            }
        }

        private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPasswordBox input)
            {
                var newValue = (string?)e.NewValue;
                if (!Equals(input.PlaceholderText, newValue))
                {
                    input.SetValue(PlaceholderTextProperty, newValue);
                }
            }
        }
        #endregion

        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyInputVariant),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(DaisyInputVariant.Bordered, OnAppearancePropertyChanged));

        public DaisyInputVariant Variant
        {
            get => (DaisyInputVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(DaisySize.Medium, OnAppearancePropertyChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region Label
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        public string? Label
        {
            get => (string?)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
        #endregion

        #region LabelPosition
        public static readonly DependencyProperty LabelPositionProperty =
            DependencyProperty.Register(
                nameof(LabelPosition),
                typeof(DaisyLabelPosition),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(DaisyLabelPosition.Top, OnAppearancePropertyChanged));

        public DaisyLabelPosition LabelPosition
        {
            get => (DaisyLabelPosition)GetValue(LabelPositionProperty);
            set => SetValue(LabelPositionProperty, value);
        }
        #endregion

        #region IsRequired
        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(
                nameof(IsRequired),
                typeof(bool),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(false, OnAppearancePropertyChanged));

        public bool IsRequired
        {
            get => (bool)GetValue(IsRequiredProperty);
            set => SetValue(IsRequiredProperty, value);
        }
        #endregion

        #region IsOptional
        public static readonly DependencyProperty IsOptionalProperty =
            DependencyProperty.Register(
                nameof(IsOptional),
                typeof(bool),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(false, OnAppearancePropertyChanged));

        public bool IsOptional
        {
            get => (bool)GetValue(IsOptionalProperty);
            set => SetValue(IsOptionalProperty, value);
        }
        #endregion

        #region HintText
        public static readonly DependencyProperty HintTextProperty =
            DependencyProperty.Register(
                nameof(HintText),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        public string? HintText
        {
            get => (string?)GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }
        #endregion

        #region HelperText
        public static readonly DependencyProperty HelperTextProperty =
            DependencyProperty.Register(
                nameof(HelperText),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        public string? HelperText
        {
            get => (string?)GetValue(HelperTextProperty);
            set => SetValue(HelperTextProperty, value);
        }
        #endregion

        #region StartIconData
        public static readonly DependencyProperty StartIconDataProperty =
            DependencyProperty.Register(
                nameof(StartIconData),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        public string? StartIconData
        {
            get => (string?)GetValue(StartIconDataProperty);
            set => SetValue(StartIconDataProperty, value);
        }

        public static readonly DependencyProperty StartIconProperty =
            DependencyProperty.Register(
                nameof(StartIcon),
                typeof(object),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        public object? StartIcon
        {
            get => GetValue(StartIconProperty);
            set => SetValue(StartIconProperty, value);
        }
        #endregion

        #region EndIconData
        public static readonly DependencyProperty EndIconDataProperty =
            DependencyProperty.Register(
                nameof(EndIconData),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        public string? EndIconData
        {
            get => (string?)GetValue(EndIconDataProperty);
            set => SetValue(EndIconDataProperty, value);
        }

        public static readonly DependencyProperty EndIconProperty =
            DependencyProperty.Register(
                nameof(EndIcon),
                typeof(object),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        public object? EndIcon
        {
            get => GetValue(EndIconProperty);
            set => SetValue(EndIconProperty, value);
        }
        #endregion

        #region BorderRingBrush
        public static readonly DependencyProperty BorderRingBrushProperty =
            DependencyProperty.Register(
                nameof(BorderRingBrush),
                typeof(Brush),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(null));

        public Brush? BorderRingBrush
        {
            get => (Brush?)GetValue(BorderRingBrushProperty);
            set => SetValue(BorderRingBrushProperty, value);
        }
        #endregion

        #region PasswordBox Specific Properties

        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.Register(
                nameof(PasswordChar),
                typeof(string),
                typeof(DaisyPasswordBox),
                new PropertyMetadata("\u25CF", OnPasswordBoxPropertyChanged));

        public string PasswordChar
        {
            get => (string)GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public static readonly DependencyProperty IsPasswordRevealButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(IsPasswordRevealButtonEnabled),
                typeof(bool),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(true, OnPasswordBoxPropertyChanged));

        public bool IsPasswordRevealButtonEnabled
        {
            get => (bool)GetValue(IsPasswordRevealButtonEnabledProperty);
            set => SetValue(IsPasswordRevealButtonEnabledProperty, value);
        }

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(DaisyPasswordBox),
                new PropertyMetadata(0, OnPasswordBoxPropertyChanged));

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        #endregion

        private static void OnPasswordBoxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPasswordBox input && input._passwordBox != null)
            {
                input.SyncPasswordBoxProperties();
            }
        }

        private void SyncPasswordBoxProperties()
        {
            if (_passwordBox == null) return;

            if (!string.IsNullOrEmpty(PasswordChar))
            {
                _passwordBox.PasswordChar = PasswordChar;
            }
            else
            {
                // Reset to default character.
                // We avoid ClearValue(PasswordCharProperty) because it can throw E_RUNTIME_SETVALUE on some platforms
                // when the property is already in its default state or during initialization.
                if (_passwordBox.PasswordChar != "\u25CF")
                {
                    _passwordBox.PasswordChar = "\u25CF";
                }
            }

            _passwordBox.IsPasswordRevealButtonEnabled = IsPasswordRevealButtonEnabled;
            _passwordBox.PlaceholderText = PlaceholderText;
            _passwordBox.MaxLength = MaxLength;

            if (!string.IsNullOrEmpty(Password) && _passwordBox.Password != Password)
            {
                _passwordBox.Password = Password;
                HasText = true;
            }
        }

        private static void OnAppearancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPasswordBox input && input._isLoaded)
            {
                input.ApplyAll();
            }
        }

        private void ApplyAll()
        {
            if (!_isLoaded || _rootGrid == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            ApplyTheme();
            ApplySizing(resources);
            ApplyInputBorderInteractionState();
            ApplyContent();
            UpdateFloatingLabelState();
        }

        private void ApplySizing(ResourceDictionary? resources)
        {
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            var height = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "Height",
                DaisyResourceLookup.GetDefaultHeight(Size));
            var fontSize = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "FontSize",
                DaisyResourceLookup.GetDefaultFontSize(Size));
            var labelFontSize = fontSize * 0.85;
            var effectiveMinHeight = Math.Max(height, MinHeight);
            var hasFloatingLabel = LabelPosition == DaisyLabelPosition.Floating && !string.IsNullOrEmpty(Label);
            var floatingHeight = resources != null
                ? DaisyResourceLookup.GetDouble(resources, $"DaisyInputFloating{sizeKey}Height", height)
                : height;

            var labelHeight = labelFontSize * 1.5;
            var minFloatingTopSpace = labelHeight + 6;
            var floatingTopSpace = hasFloatingLabel ? Math.Max(minFloatingTopSpace, floatingHeight - height) : 0;

            if (_inputHost != null)
            {
                _inputHost.MinHeight = effectiveMinHeight;
                _inputHost.Height = effectiveMinHeight;
                _inputHost.Margin = floatingTopSpace > 0 ? new Thickness(0, floatingTopSpace, 0, 0) : new Thickness(0);
            }

            if (_inputBorder != null)
            {
                _inputBorder.MinHeight = effectiveMinHeight;
                _inputBorder.Height = effectiveMinHeight;
            }

            // Size the reveal button and icon based on DaisySize (mimicking DaisyNumericUpDown scaling)
            if (_revealButton != null && _revealIconPath != null)
            {
                // Adjust for PasswordBox specific visuals
                double revealIconSize = Size switch
                {
                    DaisySize.ExtraSmall => 12,
                    DaisySize.Small => 14,
                    DaisySize.Medium => 16,
                    _ => 20
                };

                _revealIconPath.Width = revealIconSize;
                _revealIconPath.Height = revealIconSize;

                // CRITICAL: Do NOT manually size the ToggleButton!
                // Let it shrink-wrap exactly around the icon (MinHeight=0/Padding=0 enforced in BuildVisualTree).
                // This ensures it never forces the container to grow, avoiding height stretching issues in XS/S sizes.

                UpdateRevealIconData();
            }

            if (_passwordBox != null)
            {
                // Disable native reveal button to prevent height stretching
                _passwordBox.IsPasswordRevealButtonEnabled = false;
                _passwordBox.FontSize = fontSize;

                var availableHeight = effectiveMinHeight - 2;

                // For PasswordBox, we rely on VerticalContentAlignment=Center and 0 vertical padding
                // to achieve the best alignment and ensure the reveal button is accessible.
                // We disregard the 'DaisyTokenDefaults' vertical padding (6px) because strict height controls (e.g. 28px)
                // combined with font height (~18px) leaves insufficient space for 12px total vertical padding.

                // CRITICAL: The Reveal Button inside PasswordBox has a minimum height of 32px in standard styles.
                // If our control height is smaller (e.g. Small=28px, XS=24px), the reveal button will force the
                // PasswordBox to be taller than intended, breaking vertical centering.
                // We MUST let the PasswordBox take the full height, and rely on VerticalContentAlignment="Center"
                // to position the text. The Reveal Button will center itself within that height.

                // Compute vertical padding to center text properly.
                // WinUI TextBox internal layout doesn't reliably center text when
                // both MinHeight and MaxHeight are constrained. Instead, we calculate
                // explicit padding to position the text in the vertical center.
                //
                // Line height for Segoe UI is approximately 1.5 × font size (matches DaisyNumericUpDown).
                // This accounts for ascenders, descenders, and internal template spacing.
                // We subtract 2px from control height to account for typical border.
                var fontLineHeight = Math.Ceiling(fontSize * 1.5);

                // Platform-specific vertical centering:
                // Windows: Add extra top padding for smaller sizes to push text down.
                // Floating labels have more height, so they need less adjustment.
                // Skia Desktop: symmetric padding centers correctly without adjustment.
                var verticalPadding = Math.Max(0, Math.Floor((availableHeight - fontLineHeight) / 2));
                double topPadding;
                if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
                {
                    topPadding = verticalPadding;
                }
                else if (hasFloatingLabel)
                {
                    // Floating labels have taller height - just need +1 for all sizes
                    topPadding = verticalPadding + 1;
                }
                else if (Size <= DaisySize.Small)
                {
                    // XS, S need +2 adjustment (tight height)
                    topPadding = verticalPadding + 2;
                }
                else if (Size == DaisySize.Medium)
                {
                    // M needs +1 adjustment
                    topPadding = verticalPadding + 1;
                }
                else
                {
                    // L, XL use symmetric
                    topPadding = verticalPadding;
                }
                var bottomPadding = verticalPadding;

                var hasLocalPadding = ReadLocalValue(Control.PaddingProperty) != DependencyProperty.UnsetValue;
                var defaultPadding = DaisyResourceLookup.GetDefaultPadding(Size);
                var tokenPadding = resources != null
                    ? DaisyResourceLookup.GetThickness(resources, $"DaisyInput{sizeKey}Padding", defaultPadding)
                    : defaultPadding;

                if (hasLocalPadding)
                {
                    _passwordBox.Padding = Padding;
                }
                else
                {
                    // Combine horizontal padding from tokens with computed vertical centering padding.
                    _passwordBox.Padding = new Thickness(tokenPadding.Left, topPadding, tokenPadding.Right, bottomPadding);
                }

                // Override the theme's min height in all backends to avoid template-enforced stretching.
                _passwordBox.Resources["TextControlThemeMinHeight"] = availableHeight;

                if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
                {
                    _passwordBox.ClearValue(FrameworkElement.MinHeightProperty);
                    _passwordBox.ClearValue(FrameworkElement.MaxHeightProperty);
                    _passwordBox.Height = availableHeight;
                }
                else
                {
                    _passwordBox.ClearValue(FrameworkElement.HeightProperty);
                    _passwordBox.ClearValue(FrameworkElement.MinHeightProperty);
                    _passwordBox.ClearValue(FrameworkElement.MaxHeightProperty);
                }

                _passwordBox.VerticalContentAlignment = VerticalAlignment.Top;

                UpdateFloatingLabelMargin();
            }

            if (_labelText != null) _labelText.FontSize = labelFontSize;
            if (_requiredIndicator != null) _requiredIndicator.FontSize = labelFontSize;
            if (_optionalText != null) _optionalText.FontSize = labelFontSize;
            if (_hintTextBlock != null) _hintTextBlock.FontSize = labelFontSize;
            if (_helperTextBlock != null) _helperTextBlock.FontSize = labelFontSize;
            if (_floatingLabelText != null) _floatingLabelText.FontSize = fontSize;

            var iconSize = Size switch
            {
                DaisySize.ExtraSmall => 12.0,
                DaisySize.Small => 14.0,
                DaisySize.Large => 18.0,
                DaisySize.ExtraLarge => 20.0,
                _ => 16.0
            };

            if (_startIconViewbox != null) { _startIconViewbox.Width = iconSize; _startIconViewbox.Height = iconSize; }
            if (_endIconViewbox != null) { _endIconViewbox.Width = iconSize; _endIconViewbox.Height = iconSize; }
        }

        private void ApplyTheme()
        {
            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            var base100 = DaisyResourceLookup.GetBrush("DaisyBase100Brush") ?? new SolidColorBrush(Colors.White);
            var base200 = DaisyResourceLookup.GetBrush("DaisyBase200Brush") ?? new SolidColorBrush(Colors.LightGray);
            var base300 = DaisyResourceLookup.GetBrush("DaisyBase300Brush") ?? new SolidColorBrush(Colors.Gray);
            var baseContent = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") ?? new SolidColorBrush(Colors.Black);
            var error = DaisyResourceLookup.GetBrush("DaisyErrorBrush") ?? new SolidColorBrush(Colors.IndianRed);

            var variantName = Variant switch
            {
                DaisyInputVariant.Primary => "Primary",
                DaisyInputVariant.Secondary => "Secondary",
                DaisyInputVariant.Accent => "Accent",
                DaisyInputVariant.Info => "Info",
                DaisyInputVariant.Success => "Success",
                DaisyInputVariant.Warning => "Warning",
                DaisyInputVariant.Error => "Error",
                DaisyInputVariant.Ghost => "Ghost",
                DaisyInputVariant.Filled => "Filled",
                DaisyInputVariant.Bordered => "Bordered",
                _ => "Bordered"
            };

            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", $"{variantName}BorderBrush")
                ?? DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "BorderBrush");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "Foreground");
            var placeholderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "PlaceholderForeground");

            Brush background;
            Brush border;
            Thickness borderThickness;
            CornerRadius cornerRadius = new(8);

            switch (Variant)
            {
                case DaisyInputVariant.Primary:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Secondary:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush("DaisySecondaryBrush");
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Accent:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyAccentBrush");
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Info:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyInfoBrush");
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Success:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush("DaisySuccessBrush");
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Warning:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyWarningBrush");
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Error:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? error;
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Ghost:
                    background = bgOverride ?? transparent;
                    border = borderOverride ?? transparent;
                    borderThickness = new Thickness(0);
                    break;
                case DaisyInputVariant.Filled:
                    background = bgOverride ?? base200;
                    border = borderOverride ?? base300;
                    borderThickness = new Thickness(0, 0, 0, 2);
                    cornerRadius = new CornerRadius(8, 8, 0, 0);
                    break;
                default:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? base300;
                    borderThickness = new Thickness(1);
                    break;
            }

            if (_inputBorder != null)
            {
                _inputBorder.Background = background;
                _inputBorder.BorderBrush = border;
                _inputBorder.BorderThickness = borderThickness;
                _inputBorder.CornerRadius = cornerRadius;
                _baseInputBackground = background;
                _baseInputBorderBrush = border;
            }

            var textForeground = fgOverride ?? baseContent;
            if (_passwordBox != null)
            {
                _passwordBox.Foreground = textForeground;
                _passwordBox.Resources["TextControlForegroundPointerOver"] = textForeground;
                _passwordBox.Resources["TextControlForegroundFocused"] = textForeground;
                _passwordBox.Resources["TextControlForegroundDisabled"] = textForeground;

                Brush? placeholderBrush = null;
                if (placeholderOverride != null)
                {
                    placeholderBrush = placeholderOverride;
                }
                else if (textForeground is SolidColorBrush scb)
                {
                    var placeholderColor = Color.FromArgb(153, scb.Color.R, scb.Color.G, scb.Color.B);
                    placeholderBrush = new SolidColorBrush(placeholderColor);
                }

                if (placeholderBrush != null)
                {
                    _passwordBox.Resources["PasswordBoxPlaceholderTextForeground"] = placeholderBrush;
                    _passwordBox.Resources["PasswordBoxPlaceholderTextForegroundPointerOver"] = placeholderBrush;
                    _passwordBox.Resources["PasswordBoxPlaceholderTextForegroundFocused"] = placeholderBrush;
                    _passwordBox.Resources["PasswordBoxPlaceholderTextForegroundDisabled"] = placeholderBrush;
                    _passwordBox.Resources["TextControlPlaceholderForeground"] = placeholderBrush;
                    _passwordBox.Resources["TextControlPlaceholderForegroundPointerOver"] = placeholderBrush;
                    _passwordBox.Resources["TextControlPlaceholderForegroundFocused"] = placeholderBrush;
                    _passwordBox.Resources["TextControlPlaceholderForegroundDisabled"] = placeholderBrush;
                }

                // Theme the custom reveal icon
                if (_revealIconPath != null)
                {
                    _revealIconPath.Fill = textForeground;
                }

                // Keep native override just in case, though we disabled it
                _passwordBox.Resources["PasswordBoxRevealButtonForeground"] = textForeground;
                _passwordBox.Resources["PasswordBoxRevealButtonForegroundPointerOver"] = textForeground;
                _passwordBox.Resources["PasswordBoxRevealButtonForegroundPressed"] = textForeground;
                _passwordBox.Resources["PasswordBoxRevealButtonForegroundFocused"] = textForeground;

                // Generic control button resources (used as fallback in some themes)
                _passwordBox.Resources["TextControlButtonForeground"] = textForeground;
                _passwordBox.Resources["TextControlButtonForegroundPointerOver"] = textForeground;
                _passwordBox.Resources["TextControlButtonForegroundPressed"] = textForeground;
            }

            if (_startIconPath != null)
            {
                _startIconPath.Fill = textForeground;
                _startIconPath.Stroke = textForeground;
            }
            if (_endIconPath != null)
            {
                _endIconPath.Fill = textForeground;
                _endIconPath.Stroke = textForeground;
            }

            if (_labelText != null) _labelText.Foreground = textForeground;
            if (_requiredIndicator != null) _requiredIndicator.Foreground = error;
            if (_optionalText != null) _optionalText.Foreground = textForeground;
            if (_hintTextBlock != null) _hintTextBlock.Foreground = textForeground;
            if (_helperTextBlock != null) _helperTextBlock.Foreground = textForeground;
            if (_floatingLabelBorder != null) _floatingLabelBorder.Background = bgOverride ?? base100;
            if (_floatingLabelText != null) _floatingLabelText.Foreground = textForeground;
        }

        private void ApplyContent()
        {
            if (_labelPanel == null || _labelText == null)
                return;

            var hasLabel = !string.IsNullOrEmpty(Label);
            var isFloating = LabelPosition == DaisyLabelPosition.Floating;

            _labelPanel.Visibility = hasLabel && !isFloating ? Visibility.Visible : Visibility.Collapsed;
            _labelText.Text = Label ?? string.Empty;

            if (_requiredIndicator != null)
            {
                _requiredIndicator.Visibility = IsRequired && hasLabel && !isFloating ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_optionalText != null)
            {
                _optionalText.Visibility = IsOptional && hasLabel && !isFloating ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_hintTextBlock != null)
            {
                _hintTextBlock.Visibility = !string.IsNullOrEmpty(HintText) ? Visibility.Visible : Visibility.Collapsed;
                _hintTextBlock.Text = HintText ?? string.Empty;
            }

            if (_helperTextBlock != null)
            {
                _helperTextBlock.Visibility = !string.IsNullOrEmpty(HelperText) ? Visibility.Visible : Visibility.Collapsed;
                _helperTextBlock.Text = HelperText ?? string.Empty;
            }

            if (_floatingLabelBorder != null && _floatingLabelText != null)
            {
                _floatingLabelBorder.Visibility = hasLabel && isFloating ? Visibility.Visible : Visibility.Collapsed;
                _floatingLabelText.Text = Label ?? string.Empty;
            }

            var hasStartIcon = false;
            if (_startIconViewbox != null && _startIconPath != null)
            {
                var startIconGeometry = ResolveIconGeometry(StartIconData, StartIcon);
                hasStartIcon = startIconGeometry != null;
                _startIconViewbox.Visibility = hasStartIcon ? Visibility.Visible : Visibility.Collapsed;
                if (hasStartIcon)
                {
                    _startIconPath.Data = startIconGeometry;
                }
            }

            var hasEndIcon = false;
            if (_endIconViewbox != null && _endIconPath != null)
            {
                var endIconGeometry = ResolveIconGeometry(EndIconData, EndIcon);
                hasEndIcon = endIconGeometry != null;
                _endIconViewbox.Visibility = hasEndIcon ? Visibility.Visible : Visibility.Collapsed;
                if (hasEndIcon)
                {
                    _endIconPath.Data = endIconGeometry;
                }
            }

            // Custom Reveal Button Visibility
            if (_revealButton != null)
            {
                 _revealButton.Visibility = IsPasswordRevealButtonEnabled ? Visibility.Visible : Visibility.Collapsed;
            }

            // Margin Adjustment
            // Adjust PasswordBox margin to account for icons and reveal button
            if (_passwordBox != null)
            {
                var leftMargin = hasStartIcon ? 4 : 0;

                // Right margin depends on Reveal Button AND End Icon
                var revealEffective = IsPasswordRevealButtonEnabled && _revealButton != null && _revealButton.Visibility == Visibility.Visible;
                var rightMargin = hasEndIcon ? 4 : 0;

                // If reveal button is visible, it takes up space.
                // However, since it's in a separate column (Col 2), we might NOT need margin on the PasswordBox (Col 1)
                // for the reveal button itself, unlike icons which might be overlaying or tightly packed.
                // But we want a little breathing room if text gets long.
                if (revealEffective) rightMargin += 4;

                _passwordBox.Margin = new Thickness(leftMargin, 0, rightMargin, 0);
            }
        }

        private static Geometry? ResolveIconGeometry(string? iconData, object? icon)
        {
            if (!string.IsNullOrEmpty(iconData))
            {
                return Flowery.Helpers.FloweryPathHelpers.ParseGeometry(iconData);
            }

            if (icon is string pathString && !string.IsNullOrEmpty(pathString))
            {
                return Flowery.Helpers.FloweryPathHelpers.ParseGeometry(pathString);
            }

            return null;
        }


        private void UpdateFloatingLabelMargin()
        {
            if (_floatingLabelBorder == null || _passwordBox == null)
                return;

            var leftOffset = _startIconViewbox != null && _startIconViewbox.Visibility == Visibility.Visible ? 36.0 : 12.0;
            _floatingLabelBorder.Margin = new Thickness(leftOffset, 0, 0, 0);
        }

        private void UpdateFloatingPlaceholder()
        {
            if (_passwordBox == null) return;

            var isFocused = _passwordBox.FocusState != FocusState.Unfocused;
            var shouldFloat = isFocused || HasText;

            if (LabelPosition == DaisyLabelPosition.Floating)
            {
                if (shouldFloat)
                {
                    CancelFloatingPlaceholderDelay();
                    _passwordBox.PlaceholderText = PlaceholderText;
                }
                else
                {
                    _passwordBox.PlaceholderText = null;
                }
            }
            else
            {
                _passwordBox.PlaceholderText = PlaceholderText;
            }
        }

        private void CancelFloatingPlaceholderDelay()
        {
            _floatingPlaceholderCts?.Cancel();
            _floatingPlaceholderCts = null;
        }

        private void OnRevealButtonClick(object sender, RoutedEventArgs e)
        {
            if (_passwordBox == null) return;

            _passwordBox.PasswordRevealMode = _passwordBox.PasswordRevealMode == PasswordRevealMode.Visible
                ? PasswordRevealMode.Hidden
                : PasswordRevealMode.Visible;

            UpdateRevealIconData();
        }

        private void UpdateRevealIconData()
        {
            if (_revealIconPath == null || _passwordBox == null) return;

            var isVisible = _passwordBox.PasswordRevealMode == PasswordRevealMode.Visible;

            // Eye Open
            const string eyePath = "M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z";

            // Eye Closed (Slash)
            const string eyeSlashPath = "M11.83 9L15 12.17V12a3 3 0 00-3-3h-.17zm-4.3.8l1.55 1.55c-.05.21-.08.43-.08.65 0 1.66 1.34 3 3 3 .22 0 .44-.03.65-.08l1.55 1.55c-.67.33-1.41.53-2.2.53-2.76 0-5-2.24-5-5 0-.79.2-1.53.53-2.2zm-3.46-3.46L2 8.39l2.45 2.45C4.16 11.23 4.06 11.61 4 12c1.73 4.39 6 7.5 11 7.5 1.52 0 2.97-.3 4.31-.82l3.47 3.47 1.41-1.41L4.07 6.34zM12 4.5c3.6 0 6.88 1.86 9.17 4.67l-1.48 1.48C18.3 8.85 15.35 7.5 12 7.5c-.88 0-1.73.1-2.55.28l-1.44-1.44C9.13 5.17 10.53 4.5 12 4.5z";

            _revealIconPath.Data = Flowery.Helpers.FloweryPathHelpers.ParseGeometry(isVisible ? eyeSlashPath : eyePath);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ApplyAll();
        }
    }
}

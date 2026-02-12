namespace Flowery.Controls
{
    /// <summary>
    /// A TextBox control styled after DaisyUI's Input component.
    /// Supports labels, helper text, icons, and floating label mode.
    /// </summary>
    public partial class DaisyInput : DaisyBaseContentControl, IFocusableInput
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
        private bool _pendingFocusRequest;
        private bool _pendingFocusSelectAll;
        private FocusState _pendingFocusState = FocusState.Programmatic;
        private Viewbox? _endIconViewbox;
        private Path? _endIconPath;
        private TextBox? _textBox;
        private TextBlock? _helperTextBlock;
        private Border? _floatingLabelBorder;
        private TextBlock? _floatingLabelText;

        private bool _isLoaded;
        private bool _isTextBoxFocused;
        private bool _isPointerOverInput;
        private CompositeTransform? _floatingLabelTransform;
        private Storyboard? _floatingLabelStoryboard;
        private long _paddingChangedToken;
        private long _verticalContentAlignmentChangedToken;
        private CancellationTokenSource? _floatingPlaceholderCts;
        private Brush? _baseInputBackground;
        private Brush? _baseInputBorderBrush;
        public DaisyInput()
        {
            DefaultStyleKey = typeof(DaisyInput);
            IsTabStop = false;

            // Apply global size early if allowed and not explicitly ignored.
            if (FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.GetIgnoreGlobalSize(this))
            {
                Size = FlowerySizeManager.CurrentSize;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                _isLoaded = true;
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
                TryFocusInnerTextBox(_pendingFocusState, _pendingFocusSelectAll);
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

        /// <summary>
        /// Focuses the inner TextBox when available and optionally selects all text.
        /// </summary>
        /// <param name="focusState">The focus state to use.</param>
        /// <param name="selectAll">Whether to select all text after focus.</param>
        public void FocusInnerTextBox(FocusState focusState = FocusState.Programmatic, bool selectAll = false)
        {
            if (TryFocusInnerTextBox(focusState, selectAll))
                return;

            _pendingFocusRequest = true;
            _pendingFocusSelectAll = selectAll;
            _pendingFocusState = focusState;

            if (IsLoaded)
            {
                AttachPendingFocusHandler();
            }
        }

        public void FocusInput(bool selectAll = false)
        {
            FocusInnerTextBox(selectAll: selectAll);
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

            if (TryFocusInnerTextBox(_pendingFocusState, _pendingFocusSelectAll))
            {
                DetachPendingFocusHandler();
            }
        }

        private bool TryFocusInnerTextBox(FocusState focusState, bool selectAll)
        {
            if (_textBox is not { } textBox)
                return false;

            if (!IsLoaded || !textBox.IsLoaded || textBox.Visibility != Visibility.Visible)
                return false;

            var dispatcher = textBox.DispatcherQueue;
            if (dispatcher == null)
                return false;

            _pendingFocusRequest = false;
            dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (!IsLoaded || textBox.Visibility != Visibility.Visible)
                    return;

                textBox.Focus(focusState);
                if (selectAll)
                    textBox.SelectAll();
            });
            return true;
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _inputBorder ?? base.GetNeumorphicHostElement();
        }

        private void OnPaddingChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_textBox == null)
                return;

            _textBox.Padding = Padding;
            UpdateFloatingLabelMargin();
        }

        private void OnVerticalContentAlignmentChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_textBox == null)
                return;

            _textBox.VerticalContentAlignment = GetEffectiveInnerVerticalContentAlignment();
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

            // Row 2: Input area - no clipping to allow floating label to extend above
            _inputBorder = new Border
            {
                CornerRadius = new CornerRadius(8)
            };
            // Note: WinUI Border doesn't have ClipToBounds, clipping is handled by the container

            _inputGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // Start icon
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // TextBox
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

            // TextBox + floating label overlay (column 1)
            // TODO: STRUCTURAL INVESTIGATION NEEDED
            // This intermediate Grid wrapper (textAreaPanel) may be unnecessary - DaisyNumericUpDown
            // adds its TextBox directly to the root grid. However, this extra container depth causes
            // opposite baseline drift on Windows vs NumericUpDown, requiring +1 top padding here
            // but -1 top padding there. Before removing this wrapper, thoroughly test:
            // - Floating labels, icons, AcceptsReturn, focus states, all size tiers.
            // See Pattern 5.12 (DaisyInputBase) for potential unified refactor.
            var textAreaPanel = new Grid
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0)
            };

            _textBox = new TextBox
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(12, 0, 12, 0)
            };
            // Prevent the inner TextBox from drawing its own focus/hover border.
            // We render the border ourselves via _inputBorder so the focus color applies to the whole control (incl. icons).
            var transparentBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            _textBox.BorderBrush = transparentBrush;
            _textBox.Resources["TextControlBorderBrush"] = transparentBrush;
            _textBox.Resources["TextControlBorderBrushPointerOver"] = transparentBrush;
            _textBox.Resources["TextControlBorderBrushFocused"] = transparentBrush;
            _textBox.Resources["TextControlBorderBrushDisabled"] = transparentBrush;
            _textBox.Resources["TextControlBackground"] = transparentBrush;
            _textBox.Resources["TextControlBackgroundPointerOver"] = transparentBrush;
            _textBox.Resources["TextControlBackgroundFocused"] = transparentBrush;
            _textBox.Resources["TextControlBackgroundDisabled"] = transparentBrush;
            _textBox.Resources["TextControlBorderThemeThickness"] = new Thickness(0);
            _textBox.Resources["TextControlBorderThemeThicknessPointerOver"] = new Thickness(0);
            _textBox.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0);
            _textBox.Resources["TextControlBorderThemeThicknessDisabled"] = new Thickness(0);

            // Theme the clear button (DeleteButton) to use proper DaisyUI colors
            // WinUI uses TextControlButton*, Uno Platform uses TextBoxDeleteButton*
            var resources = Application.Current?.Resources;
            var baseContentBrush = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.Black));
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "Foreground");
            var textForeground = fgOverride ?? baseContentBrush;
            // WinUI resource keys
            _textBox.Resources["TextControlButtonForeground"] = textForeground;
            _textBox.Resources["TextControlButtonForegroundPointerOver"] = textForeground;
            _textBox.Resources["TextControlButtonForegroundPressed"] = textForeground;
            // Uno Platform resource keys
            _textBox.Resources["TextBoxDeleteButtonForeground"] = textForeground;
            _textBox.Resources["TextBoxDeleteButtonForegroundPointerOver"] = textForeground;
            _textBox.Resources["TextBoxDeleteButtonForegroundPressed"] = textForeground;
            _textBox.Resources["TextBoxDeleteButtonForegroundFocused"] = textForeground;
            _textBox.TextChanged += OnInnerTextChanged;
            _textBox.TextChanging += OnInnerTextChanging;
            _textBox.GotFocus += OnInnerGotFocus;
            _textBox.LostFocus += OnInnerLostFocus;
            textAreaPanel.Children.Add(_textBox);

            Grid.SetColumn(textAreaPanel, 1);
            _inputGrid.Children.Add(textAreaPanel);

            // End icon
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
            Grid.SetColumn(_endIconViewbox, 2);
            _inputGrid.Children.Add(_endIconViewbox);

            // Floating label - created here but added to _rootGrid (not _inputHost)
            // This prevents clipping on Skia when the label animates above the input row.
            _floatingLabelBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top, // Top-aligned, uses TranslateY to position
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(4, 0, 4, 0),
                CornerRadius = new CornerRadius(4),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                RenderTransformOrigin = new Windows.Foundation.Point(0, 0.5) // Left-center origin for scale
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

            _inputBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
            _inputBorder.VerticalAlignment = VerticalAlignment.Stretch;
            _inputGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            _inputGrid.VerticalAlignment = VerticalAlignment.Stretch;

            // _inputHost contains border and interactive content only (no floating label)
            _inputHost.Children.Add(_inputBorder);
            _inputHost.Children.Add(_inputGrid);

            _inputHost.PointerEntered += OnInputPointerEntered;
            _inputHost.PointerExited += OnInputPointerExited;

            // Wrap the input area and floating label in a container to ensure Row 2 sizes correctly
            // to the union of both elements. This fixes the bottom border cut-off issue on Skia.
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
            SyncTextBoxProperties();
        }

        private void OnInnerTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_textBox != null)
            {
                Text = _textBox.Text;
                HasText = !string.IsNullOrEmpty(_textBox.Text);
                UpdateFloatingLabelState();
                TextChanged?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Occurs when the text content changes.
        /// </summary>
        public event TypedEventHandler<DaisyInput, TextChangedEventArgs>? TextChanged;

        /// <summary>
        /// Occurs when the text is about to change.
        /// </summary>
        public event TypedEventHandler<DaisyInput, TextBoxTextChangingEventArgs>? TextChanging;

        private void OnInnerTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            TextChanging?.Invoke(this, args);
        }

        /// <summary>
        /// Gets or sets the starting position of the text selection.
        /// </summary>
        public int SelectionStart
        {
            get => _textBox?.SelectionStart ?? 0;
            set { if (_textBox != null) _textBox.SelectionStart = value; }
        }

        /// <summary>
        /// Gets or sets the length of the text selection.
        /// </summary>
        public int SelectionLength
        {
            get => _textBox?.SelectionLength ?? 0;
            set { if (_textBox != null) _textBox.SelectionLength = value; }
        }

        private void OnInnerGotFocus(object sender, RoutedEventArgs e)
        {
            _isTextBoxFocused = true;
            UpdateFloatingLabelState(true);
            UpdateFocusBorder();
        }

        private void OnInnerLostFocus(object sender, RoutedEventArgs e)
        {
            _isTextBoxFocused = false;
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
            if (_floatingLabelBorder == null || _floatingLabelTransform == null || _textBox == null)
                return;

            if (LabelPosition != DaisyLabelPosition.Floating)
                return;

            UpdateFloatingLabelMargin();

            var isFocused = _textBox.FocusState != FocusState.Unfocused;
            _isTextBoxFocused = isFocused;
            var shouldFloat = isFocused || HasText;

            UpdateFloatingPlaceholder();

            var resources = Application.Current?.Resources;
            var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.Black));
            var primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Colors.Blue));

            // Calculate the vertical offset to center the label in the input when not floating.
            // Input row has top margin (floatingTopSpace) + input height.
            // The label should be centered in the input area, not at the top of the row.
            var inputHeight = _inputBorder?.MinHeight > 0 ? _inputBorder.MinHeight : DaisyResourceLookup.GetDefaultHeight(Size);
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            var floatingHeight = resources != null
                ? DaisyResourceLookup.GetDouble(resources, $"DaisyInputFloating{sizeKey}Height", inputHeight)
                : inputHeight;

            // SYNC-CRITICAL: Must match ApplySizing logic precisely
            var fontSize = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "FontSize",
                DaisyResourceLookup.GetDefaultFontSize(Size));
            var lineHeight = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "LineHeight",
                DaisyResourceLookup.GetDefaultLineHeight(Size));
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
                // Floated state: label stays at top (TranslateY = 0), scaled down
                targetScale = Size == DaisySize.ExtraSmall ? 0.80 : 0.85;
                targetTranslateY = 0.0;  // At top of row (above the input border)
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

                // Align Unfloated Label exactly with the text input position.
                // We replicate the padding calculation from ApplySizing to match the cursor position.
                // Available height inside border:
                var availableInputHeight = inputHeight - 2;
                var fontLineHeight = Math.Ceiling(lineHeight);
                var verticalPadding = Math.Max(0, Math.Floor((availableInputHeight - fontLineHeight) / 2));
                var topPadding = verticalPadding;
                if (!PlatformCompatibility.IsSkiaBackend && !PlatformCompatibility.IsWasmBackend && !PlatformCompatibility.IsAndroid)
                {
                    topPadding = verticalPadding + 1;
                }

                // Y = Top Margin + Border Top + Top Padding
                // We add +1px fudge factor to ensure it visually clears the top border
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

            var isFocused = _textBox != null && _textBox.FocusState != FocusState.Unfocused;
            if (isFocused)
            {
                _inputBorder.BorderBrush = GetVariantFocusBrush(resources);
                if (_baseInputBackground != null)
                    _inputBorder.Background = _baseInputBackground;
                return;
            }

            // Pointer-over behavior (matches Avalonia default input)
            if (_isPointerOverInput && (Variant == DaisyInputVariant.Bordered || Variant == DaisyInputVariant.Filled))
            {
                var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.Black));
                _inputBorder.BorderBrush = baseContent;

                // Filled: pointerover darkens background slightly
                if (Variant == DaisyInputVariant.Filled)
                {
                    var base300 = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Colors.Gray));
                    _inputBorder.Background = base300;
                }

                return;
            }

            // Default (no focus, no hover): revert to base theme values.
            if (_baseInputBorderBrush != null)
                _inputBorder.BorderBrush = _baseInputBorderBrush;
            if (_baseInputBackground != null)
                _inputBorder.Background = _baseInputBackground;
        }

        private Brush GetVariantFocusBrush(ResourceDictionary? resources)
        {
            if (BorderRingBrush != null)
                return BorderRingBrush;

            var primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Colors.Blue));

            return Variant switch
            {
                DaisyInputVariant.Primary => primary,
                DaisyInputVariant.Secondary => DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", primary),
                DaisyInputVariant.Accent => DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", primary),
                DaisyInputVariant.Info => DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", primary),
                DaisyInputVariant.Success => DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", primary),
                DaisyInputVariant.Warning => DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", primary),
                DaisyInputVariant.Error => DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", primary),
                _ => primary
            };
        }

        #region HasText (read-only)
        public static readonly DependencyProperty HasTextProperty =
            DependencyProperty.Register(
                nameof(HasText),
                typeof(bool),
                typeof(DaisyInput),
                new PropertyMetadata(false));

        public bool HasText
        {
            get => (bool)GetValue(HasTextProperty);
            private set => SetValue(HasTextProperty, value);
        }
        #endregion

        #region Text
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(DaisyInput),
                new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyInput input && input._textBox != null)
            {
                var newText = (string?)e.NewValue ?? string.Empty;
                if (input._textBox.Text != newText)
                {
                    // On Android, setting TextBox.Text can trigger IME focus events that interfere
                    // with focus transitions. Temporarily disable focus capture during the update.
                    bool isAndroid = PlatformCompatibility.IsAndroid;
                    if (isAndroid)
                    {
                        input._textBox.AllowFocusOnInteraction = false;
                    }

                    input._textBox.Text = newText;

                    if (isAndroid)
                    {
                        // Use dispatcher to re-enable focus after the text change event settles
                        input.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (input._textBox != null)
                                input._textBox.AllowFocusOnInteraction = true;
                        });
                    }
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
                typeof(DaisyInput),
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
                typeof(DaisyInput),
                new PropertyMetadata(null, OnWatermarkChanged));

        public string? Watermark
        {
            get => PlaceholderText;
            set => SetValue(WatermarkProperty, value);
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnAppearancePropertyChanged(d, e);

            if (d is DaisyInput input)
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
            if (d is DaisyInput input)
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
                typeof(DaisyInput),
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
                typeof(DaisyInput),
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
                typeof(DaisyInput),
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
                typeof(DaisyInput),
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
                typeof(DaisyInput),
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
                typeof(DaisyInput),
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
                typeof(DaisyInput),
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
                typeof(DaisyInput),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        public string? HelperText
        {
            get => (string?)GetValue(HelperTextProperty);
            set => SetValue(HelperTextProperty, value);
        }
        #endregion

        #region StartIcon
        public static readonly DependencyProperty StartIconProperty =
            DependencyProperty.Register(
                nameof(StartIcon),
                typeof(object),
                typeof(DaisyInput),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        /// <summary>
        /// Icon to display at the start of the input. Accepts Geometry or string path data.
        /// For x:String resources, the string is automatically parsed into a Geometry.
        /// </summary>
        public object? StartIcon
        {
            get => GetValue(StartIconProperty);
            set => SetValue(StartIconProperty, value);
        }
        #endregion

        #region EndIcon
        public static readonly DependencyProperty EndIconProperty =
            DependencyProperty.Register(
                nameof(EndIcon),
                typeof(object),
                typeof(DaisyInput),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        /// <summary>
        /// Icon to display at the end of the input. Accepts Geometry or string path data.
        /// For x:String resources, the string is automatically parsed into a Geometry.
        /// </summary>
        public object? EndIcon
        {
            get => GetValue(EndIconProperty);
            set => SetValue(EndIconProperty, value);
        }
        #endregion

        #region StartIconData
        public static readonly DependencyProperty StartIconDataProperty =
            DependencyProperty.Register(
                nameof(StartIconData),
                typeof(string),
                typeof(DaisyInput),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        /// <summary>
        /// Path data string for the start icon. Takes precedence over StartIcon (Geometry).
        /// Use this for reliable cross-platform icon rendering (especially on Skia).
        /// </summary>
        public string? StartIconData
        {
            get => (string?)GetValue(StartIconDataProperty);
            set => SetValue(StartIconDataProperty, value);
        }
        #endregion

        #region EndIconData
        public static readonly DependencyProperty EndIconDataProperty =
            DependencyProperty.Register(
                nameof(EndIconData),
                typeof(string),
                typeof(DaisyInput),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        /// <summary>
        /// Path data string for the end icon. Takes precedence over EndIcon (Geometry).
        /// Use this for reliable cross-platform icon rendering (especially on Skia).
        /// </summary>
        public string? EndIconData
        {
            get => (string?)GetValue(EndIconDataProperty);
            set => SetValue(EndIconDataProperty, value);
        }
        #endregion

        #region BorderRingBrush
        public static readonly DependencyProperty BorderRingBrushProperty =
            DependencyProperty.Register(
                nameof(BorderRingBrush),
                typeof(Brush),
                typeof(DaisyInput),
                new PropertyMetadata(null));

        public Brush? BorderRingBrush
        {
            get => (Brush?)GetValue(BorderRingBrushProperty);
            set => SetValue(BorderRingBrushProperty, value);
        }
        #endregion

        #region MaxLength
        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(DaisyInput),
                new PropertyMetadata(0, OnTextBoxPropertyChanged));

        /// <summary>
        /// Maximum number of characters allowed.
        /// </summary>
        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }
        #endregion

        #region AcceptsReturn
        public static readonly DependencyProperty AcceptsReturnProperty =
            DependencyProperty.Register(
                nameof(AcceptsReturn),
                typeof(bool),
                typeof(DaisyInput),
                new PropertyMetadata(false, OnTextBoxPropertyChanged));

        /// <summary>
        /// Whether the TextBox accepts the Return key (multiline).
        /// </summary>
        public bool AcceptsReturn
        {
            get => (bool)GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }
        #endregion

        #region TextWrapping
        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(
                nameof(TextWrapping),
                typeof(TextWrapping),
                typeof(DaisyInput),
                new PropertyMetadata(TextWrapping.NoWrap, OnTextBoxPropertyChanged));

        /// <summary>
        /// Text wrapping mode.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }
        #endregion

        #region IsReadOnly
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(DaisyInput),
                new PropertyMetadata(false, OnTextBoxPropertyChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the input is read-only.
        /// When true, the text cannot be edited but can still be selected and copied.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }
        #endregion

        #region Description
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(DaisyInput),
                new PropertyMetadata(null, OnAppearancePropertyChanged));

        /// <summary>
        /// Description text (displayed as helper text, e.g., character count).
        /// </summary>
#if __IOS__ || __MACOS__
        public new string? Description
#else
        public string? Description
#endif
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
        #endregion

        /// <summary>
        /// Gets the inner TextBox for advanced configuration.
        /// </summary>
        protected TextBox? InnerTextBox => _textBox;

        private static void OnTextBoxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyInput input && input._textBox != null)
            {
                input.SyncTextBoxProperties();
            }
        }

        private void SyncTextBoxProperties()
        {
            if (_textBox == null) return;
            _textBox.MaxLength = MaxLength;
            _textBox.AcceptsReturn = AcceptsReturn;
            _textBox.TextWrapping = TextWrapping;
            _textBox.IsReadOnly = IsReadOnly;
            _textBox.VerticalContentAlignment = GetEffectiveInnerVerticalContentAlignment();

            // Sync Text property - important because OnTextPropertyChanged may have fired
            // before BuildVisualTree() when Text is set in XAML
            if (!string.IsNullOrEmpty(Text) && _textBox.Text != Text)
            {
                // On Android, setting TextBox.Text can trigger IME focus events that interfere
                // with focus transitions. Temporarily disable focus capture during the update.
                bool isAndroid = PlatformCompatibility.IsAndroid;
                if (isAndroid)
                {
                    _textBox.AllowFocusOnInteraction = false;
                }

                _textBox.Text = Text;
                HasText = true;

                if (isAndroid)
                {
                    // Use dispatcher to re-enable focus after the text change event settles
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_textBox != null)
                            _textBox.AllowFocusOnInteraction = true;
                    });
                }
            }
        }

        private VerticalAlignment GetEffectiveInnerVerticalContentAlignment()
        {
            var hasLocalAlignment = ReadLocalValue(Control.VerticalContentAlignmentProperty) != DependencyProperty.UnsetValue;
            if (hasLocalAlignment)
                return VerticalContentAlignment;

            return AcceptsReturn ? VerticalAlignment.Top : VerticalAlignment.Center;
        }

        private static void OnAppearancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyInput input && input._isLoaded)
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

            ApplyTheme(resources);
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
            var lineHeight = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "LineHeight",
                DaisyResourceLookup.GetDefaultLineHeight(Size));
            var labelFontSize = fontSize * 0.85;
            var effectiveMinHeight = Math.Max(height, MinHeight);
            var hasFloatingLabel = LabelPosition == DaisyLabelPosition.Floating && !string.IsNullOrEmpty(Label);
            var floatingHeight = resources != null
                ? DaisyResourceLookup.GetDouble(resources, $"DaisyInputFloating{sizeKey}Height", height)
                : height;
            // Ensure reserved space for floating label is at least label height + gap (6px)
            // This prevents the label from "hanging low" over the input text on Skia.
            var labelHeight = labelFontSize * 1.5;
            var minFloatingTopSpace = labelHeight + 6;
            var floatingTopSpace = hasFloatingLabel ? Math.Max(minFloatingTopSpace, floatingHeight - height) : 0;

            if (_inputHost != null)
            {
                _inputHost.MinHeight = effectiveMinHeight;
                _inputHost.Margin = floatingTopSpace > 0 ? new Thickness(0, floatingTopSpace, 0, 0) : new Thickness(0);
            }

            // Rely on children sizing (input host + label) within the Auto row.
            _row2Container?.ClearValue(FrameworkElement.MinHeightProperty);

            if (_inputBorder != null)
            {
                _inputBorder.MinHeight = effectiveMinHeight;
            }

            // Note: _floatingLabelBorder and _inputHost are now wrapped in row2Panel.
            // The row height is determined by the union of their heights (including top margin).

            if (_textBox != null)
            {
                _textBox.FontSize = fontSize;

                // Compute vertical padding to center text properly.
                // WinUI TextBox internal layout doesn't reliably center text when
                // both MinHeight and MaxHeight are constrained. Instead, we calculate
                // explicit padding to position the text in the vertical center.
                //
                // Line height comes from size tokens for cross-platform consistency.
                // We subtract 2px from control height to account for typical border.
                var availableHeight = effectiveMinHeight - 2;
                var fontLineHeight = Math.Ceiling(lineHeight);

                // Platform-specific vertical centering:
                // Windows: Add extra top padding for smaller sizes to push text down.
                // Floating labels have more height, so they need less adjustment.
                // Skia Desktop: symmetric padding centers correctly without adjustment.
                var verticalPadding = Math.Max(0, Math.Floor((availableHeight - fontLineHeight) / 2));
                double topPadding;
                if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend || PlatformCompatibility.IsAndroid)
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
                    _textBox.Padding = Padding;
                }
                else
                {
                    // Combine horizontal padding from tokens with computed vertical centering padding.
                    _textBox.Padding = new Thickness(tokenPadding.Left, topPadding, tokenPadding.Right, bottomPadding);
                }

                // Platform-specific height handling:
                // - Windows: Override TextControlThemeMinHeight to allow compact sizes
                // - Skia: Explicit Height needed to enforce our compact sizes
                if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
                {
                    _textBox.ClearValue(FrameworkElement.MinHeightProperty);
                    _textBox.ClearValue(FrameworkElement.MaxHeightProperty);
                    _textBox.Height = availableHeight;
                }
                else
                {
                    // Override the theme's 32px minimum height to allow compact XS/S sizes
                    _textBox.Resources["TextControlThemeMinHeight"] = availableHeight;
                    _textBox.ClearValue(FrameworkElement.HeightProperty);
                    _textBox.ClearValue(FrameworkElement.MinHeightProperty);
                    _textBox.ClearValue(FrameworkElement.MaxHeightProperty);
                }

                // Android centers better with native center alignment; other platforms keep top + tokenized padding.
                _textBox.VerticalContentAlignment = AcceptsReturn
                    ? GetEffectiveInnerVerticalContentAlignment()
                    : (PlatformCompatibility.IsAndroid ? VerticalAlignment.Center : VerticalAlignment.Top);

                UpdateFloatingLabelMargin();
            }

            if (_labelText != null)
            {
                _labelText.FontSize = labelFontSize;
            }

            if (_requiredIndicator != null)
            {
                _requiredIndicator.FontSize = labelFontSize;
            }

            if (_optionalText != null)
            {
                _optionalText.FontSize = labelFontSize;
            }

            if (_hintTextBlock != null)
            {
                _hintTextBlock.FontSize = labelFontSize;
            }

            if (_helperTextBlock != null)
            {
                _helperTextBlock.FontSize = labelFontSize;
            }

            if (_floatingLabelText != null)
            {
                _floatingLabelText.FontSize = fontSize;
            }

            // Icon sizes based on Size
            var iconSize = Size switch
            {
                DaisySize.ExtraSmall => 12.0,
                DaisySize.Small => 14.0,
                DaisySize.Large => 18.0,
                DaisySize.ExtraLarge => 20.0,
                _ => 16.0
            };

            if (_startIconViewbox != null)
            {
                _startIconViewbox.Width = iconSize;
                _startIconViewbox.Height = iconSize;
            }

            if (_endIconViewbox != null)
            {
                _endIconViewbox.Width = iconSize;
                _endIconViewbox.Height = iconSize;
            }

            if (_startIconPath != null)
            {
                _startIconPath.Width = 24;
                _startIconPath.Height = 24;
            }

            if (_endIconPath != null)
            {
                _endIconPath.Width = 24;
                _endIconPath.Height = 24;
            }

        }

        private void ApplyTheme(ResourceDictionary? resources)
        {
            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            var base100 = DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush", new SolidColorBrush(Colors.White));
            var base200 = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush", new SolidColorBrush(Colors.LightGray));
            var base300 = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Colors.Gray));
            var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.Black));
            var neutral = DaisyResourceLookup.GetBrush(resources, "DaisyNeutralBrush", base300);
            var error = DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", new SolidColorBrush(Colors.IndianRed));

            // Get variant name for resource lookup
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

            // Check for lightweight styling overrides first
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", $"{variantName}BorderBrush")
                ?? DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "BorderBrush");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "Foreground");
            var placeholderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyInput", "PlaceholderForeground");

            Brush background;
            Brush border;
            Thickness borderThickness;
            CornerRadius cornerRadius = new(8);

            // Compute defaults based on variant, then apply overrides
            switch (Variant)
            {
                case DaisyInputVariant.Primary:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Colors.Blue));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Secondary:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", new SolidColorBrush(Colors.HotPink));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Accent:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", new SolidColorBrush(Colors.Teal));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Info:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", new SolidColorBrush(Colors.DeepSkyBlue));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Success:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", new SolidColorBrush(Colors.LimeGreen));
                    borderThickness = new Thickness(1);
                    break;
                case DaisyInputVariant.Warning:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", new SolidColorBrush(Colors.Gold));
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
                    border = borderOverride ?? neutral;
                    borderThickness = new Thickness(0, 0, 0, 2);
                    cornerRadius = new CornerRadius(8, 8, 0, 0);
                    break;
                case DaisyInputVariant.Bordered:
                default:
                    background = bgOverride ?? base100;
                    border = borderOverride ?? neutral;
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

            if (_inputHost != null)
            {
                _inputHost.Background = background;
                _inputHost.SetValue(Grid.CornerRadiusProperty, cornerRadius);
            }

            // Text foreground with override support
            var textForeground = fgOverride ?? baseContent;
            if (_textBox != null)
            {
                _textBox.Foreground = textForeground;

                // Override internal TextBox states to prevent them from using system highlight colors
                _textBox.Resources["TextControlForegroundPointerOver"] = textForeground;
                _textBox.Resources["TextControlForegroundFocused"] = textForeground;
                _textBox.Resources["TextControlForegroundDisabled"] = textForeground;

                // Set placeholder color
                if (placeholderOverride != null)
                {
                    _textBox.PlaceholderForeground = placeholderOverride;
                }
                else if (textForeground is SolidColorBrush scb)
                {
                    var placeholderColor = Color.FromArgb(153, scb.Color.R, scb.Color.G, scb.Color.B); // ~60% opacity
                    _textBox.PlaceholderForeground = new SolidColorBrush(placeholderColor);
                }
            }

            if (_labelText != null)
            {
                _labelText.Foreground = textForeground;
            }

            if (_requiredIndicator != null)
            {
                _requiredIndicator.Foreground = error;
            }

            if (_optionalText != null)
            {
                _optionalText.Foreground = textForeground;
            }

            if (_hintTextBlock != null)
            {
                _hintTextBlock.Foreground = textForeground;
            }

            if (_helperTextBlock != null)
            {
                _helperTextBlock.Foreground = textForeground;
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

            if (_floatingLabelBorder != null)
            {
                _floatingLabelBorder.Background = bgOverride ?? base100;
            }

            if (_floatingLabelText != null)
            {
                _floatingLabelText.Foreground = textForeground;
            }

            // Update clear button (DeleteButton) foreground for theme changes
            // WinUI uses TextControlButton*, Uno Platform uses TextBoxDeleteButton*
            if (_textBox != null)
            {
                // WinUI resource keys
                _textBox.Resources["TextControlButtonForeground"] = textForeground;
                _textBox.Resources["TextControlButtonForegroundPointerOver"] = textForeground;
                _textBox.Resources["TextControlButtonForegroundPressed"] = textForeground;
                // Uno Platform resource keys
                _textBox.Resources["TextBoxDeleteButtonForeground"] = textForeground;
                _textBox.Resources["TextBoxDeleteButtonForegroundPointerOver"] = textForeground;
                _textBox.Resources["TextBoxDeleteButtonForegroundPressed"] = textForeground;
                _textBox.Resources["TextBoxDeleteButtonForegroundFocused"] = textForeground;
            }
        }

        private void ApplyContent()
        {
            if (_labelPanel == null || _labelText == null)
                return;

            var hasLabel = !string.IsNullOrEmpty(Label);
            var isFloating = LabelPosition == DaisyLabelPosition.Floating;

            // Standard label (non-floating)
            _labelPanel.Visibility = hasLabel && !isFloating ? Visibility.Visible : Visibility.Collapsed;
            _labelText.Text = Label ?? string.Empty;

            // Required/Optional indicators (for standard label)
            if (_requiredIndicator != null)
            {
                _requiredIndicator.Visibility = IsRequired && hasLabel && !isFloating ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_optionalText != null)
            {
                _optionalText.Visibility = IsOptional && hasLabel && !isFloating ? Visibility.Visible : Visibility.Collapsed;
            }

            // Floating label
            if (_floatingLabelBorder != null && _floatingLabelText != null)
            {
                _floatingLabelBorder.Visibility = hasLabel && isFloating ? Visibility.Visible : Visibility.Collapsed;
                _floatingLabelText.Text = Label ?? string.Empty;

                // Get the required indicator in floating label panel
                if (_floatingLabelBorder.Child is StackPanel floatingPanel && floatingPanel.Children.Count > 1)
                {
                    if (floatingPanel.Children[1] is TextBlock floatingRequired)
                    {
                        floatingRequired.Visibility = IsRequired ? Visibility.Visible : Visibility.Collapsed;
                    }
                }

                UpdateFloatingLabelState();
            }

            // Hint text
            if (_hintTextBlock != null)
            {
                var hasHint = !string.IsNullOrEmpty(HintText);
                _hintTextBlock.Visibility = hasHint ? Visibility.Visible : Visibility.Collapsed;
                _hintTextBlock.Text = HintText ?? string.Empty;
            }

            // Helper text (Description takes precedence if set)
            if (_helperTextBlock != null)
            {
                var displayText = !string.IsNullOrEmpty(Description) ? Description : HelperText;
                var hasHelper = !string.IsNullOrEmpty(displayText);
                _helperTextBlock.Visibility = hasHelper ? Visibility.Visible : Visibility.Collapsed;
                _helperTextBlock.Text = displayText ?? string.Empty;
            }

            // Placeholder
            if (_textBox != null)
            {
                var isFloatingMode = LabelPosition == DaisyLabelPosition.Floating && !string.IsNullOrEmpty(Label);
                if (isFloatingMode)
                {
                    _textBox.PlaceholderText = string.Empty;
                }
                else
                {
                    _textBox.PlaceholderText = PlaceholderText ?? string.Empty;
                }
            }

            if (_startIconViewbox != null && _startIconPath != null)
            {
                // Prefer string-based IconData (works reliably on all platforms including Skia).
                // StartIcon can be either a string (from x:String resource) or a Geometry object.
                var startIconGeometry = ResolveIconGeometry(StartIconData, StartIcon);
                var hasStartIcon = startIconGeometry != null;
                _startIconViewbox.Visibility = hasStartIcon ? Visibility.Visible : Visibility.Collapsed;
                if (hasStartIcon)
                {
                    _startIconPath.Data = startIconGeometry;
                }

                // Adjust TextBox margin when icon is present
                if (_textBox != null)
                {
                    var endIconGeometry = ResolveIconGeometry(EndIconData, EndIcon);
                    var hasEndIcon = endIconGeometry != null;
                    var leftMargin = hasStartIcon ? 4 : 0;
                    var rightMargin = hasEndIcon ? 4 : 0;
                    _textBox.Margin = new Thickness(leftMargin, 0, rightMargin, 0);
                }
            }

            if (_endIconViewbox != null && _endIconPath != null)
            {
                var endIconGeometry = ResolveIconGeometry(EndIconData, EndIcon);
                var hasEndIcon = endIconGeometry != null;
                _endIconViewbox.Visibility = hasEndIcon ? Visibility.Visible : Visibility.Collapsed;
                if (hasEndIcon)
                {
                    _endIconPath.Data = endIconGeometry;
                }
            }

            UpdateFloatingLabelMargin();
        }

        /// <summary>
        /// Resolves icon geometry from either explicit IconData string or Icon object.
        /// Icon can be a string path data (from x:String XAML resource).
        /// </summary>
        private static Geometry? ResolveIconGeometry(string? iconData, object? icon)
        {
            // 1. Prefer explicit string path data (IconData property)
            if (!string.IsNullOrEmpty(iconData))
            {
                return FloweryPathHelpers.ParseGeometry(iconData);
            }

            // 2. Check if icon is a string (from x:String XAML resource binding)
            if (icon is string pathString && !string.IsNullOrEmpty(pathString))
            {
                return FloweryPathHelpers.ParseGeometry(pathString);
            }

            return null;
        }

        private void UpdateFloatingLabelMargin()
        {
            if (_floatingLabelBorder == null || _textBox == null)
                return;

            // The floating label is a direct child of _rootGrid (same row as _inputHost).
            // We need to account for the start icon's width when calculating the left margin
            // to align the label with the text input area.
            var startIconOffset = 0.0;
            if (_startIconViewbox != null && _startIconViewbox.Visibility == Visibility.Visible)
            {
                // Icon width + its margin (12px left margin from the icon setup)
                startIconOffset = _startIconViewbox.Width + _startIconViewbox.Margin.Left;
            }

            var labelPaddingLeft = _floatingLabelBorder.Padding.Left;
            var left = Math.Max(0, startIconOffset + _textBox.Margin.Left + _textBox.Padding.Left - labelPaddingLeft);
            _floatingLabelBorder.Margin = new Thickness(left, 0, 0, 0);
        }

        private void UpdateFloatingPlaceholder()
        {
            if (_textBox == null)
                return;

            var isFloatingMode = LabelPosition == DaisyLabelPosition.Floating && !string.IsNullOrEmpty(Label);
            if (!isFloatingMode)
            {
                CancelFloatingPlaceholderDelay();
                return;
            }

            var placeholder = PlaceholderText ?? string.Empty;
            var shouldShow = _isTextBoxFocused && !HasText && !string.IsNullOrEmpty(placeholder);
            if (!shouldShow)
            {
                CancelFloatingPlaceholderDelay();
                _textBox.PlaceholderText = string.Empty;
                return;
            }

            if (_textBox.PlaceholderText == placeholder)
                return;

            // Hide immediately; show after a short delay to avoid overlapping the floating label transition.
            _textBox.PlaceholderText = string.Empty;
            CancelFloatingPlaceholderDelay();

            _floatingPlaceholderCts = new CancellationTokenSource();
            _ = ShowFloatingPlaceholderAfterDelayAsync(placeholder, _floatingPlaceholderCts.Token);
        }

        private async Task ShowFloatingPlaceholderAfterDelayAsync(string placeholder, CancellationToken ct)
        {
            try
            {
                await Task.Delay(150, ct);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested)
                return;

            var dq = DispatcherQueue;
            if (dq == null)
                return;

            dq.TryEnqueue(() =>
            {
                if (_textBox == null)
                    return;

                if (LabelPosition != DaisyLabelPosition.Floating)
                    return;

                if (!_isTextBoxFocused || HasText)
                    return;

                _textBox.PlaceholderText = placeholder;
            });
        }

        private void CancelFloatingPlaceholderDelay()
        {
            _floatingPlaceholderCts?.Cancel();
            _floatingPlaceholderCts?.Dispose();
            _floatingPlaceholderCts = null;
        }

    }
}

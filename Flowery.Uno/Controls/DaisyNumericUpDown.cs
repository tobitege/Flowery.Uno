namespace Flowery.Controls
{
    /// <summary>
    /// A NumericUpDown control styled after DaisyUI's Input component with spin buttons.
    /// Supports alternate bases (Hex, Binary, Octal, ColorHex, IPAddress) and input filtering.
    /// </summary>
    public partial class DaisyNumericUpDown : DaisyBaseContentControl, IFocusableInput
    {
        private Grid? _rootHost;
        private Border? _rootBorder;
        private Grid? _rootGrid;
        private StackPanel? _buttonsPanel;
        private Button? _clearButton;
        private TextBlock? _prefixTextBlock;
        private TextBox? _textBox;
        private TextBlock? _suffixTextBlock;
        private RepeatButton? _increaseButton;
        private RepeatButton? _decreaseButton;
        private Path? _increaseIconPath;
        private Path? _decreaseIconPath;

        private bool _isEditing;
        private bool _isUpdatingText;
        private bool _isUpdatingValue;
        private DispatcherTimer? _errorFlashTimer;
        private Brush? _normalBorderBrush;
        private bool _pendingFocusRequest;
        private bool _pendingFocusSelectAll;
        private FocusState _pendingFocusState = FocusState.Programmatic;
        public DaisyNumericUpDown()
        {
            DefaultStyleKey = typeof(DaisyNumericUpDown);
            IsTabStop = false;

            // Apply global size early if allowed and not explicitly ignored.
            if (FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.GetIgnoreGlobalSize(this))
            {
                Size = FlowerySizeManager.CurrentSize;
            }
        }

        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(decimal?),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(0m, OnValueChanged));

        /// <summary>
        /// Gets or sets the numeric value.
        /// </summary>
        public decimal? Value
        {
            get => (decimal?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(decimal),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(0m, OnStyleOrValueConfigChanged));

        public decimal Minimum
        {
            get => (decimal)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(decimal),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(100m, OnStyleOrValueConfigChanged));

        public decimal Maximum
        {
            get => (decimal)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(
                nameof(Increment),
                typeof(decimal),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(1m));

        public decimal Increment
        {
            get => (decimal)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(false, OnStyleOrValueConfigChanged));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly DependencyProperty FormatStringProperty =
            DependencyProperty.Register(
                nameof(FormatString),
                typeof(string),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(null, OnStyleOrValueConfigChanged));

        /// <summary>
        /// Gets or sets the decimal format string (used when <see cref="NumberBase"/> is Decimal).
        /// </summary>
        public string? FormatString
        {
            get => (string?)GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        public static readonly DependencyProperty ShowInputErrorProperty =
            DependencyProperty.Register(
                nameof(ShowInputError),
                typeof(bool),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to show visual error feedback on invalid input.
        /// </summary>
        public bool ShowInputError
        {
            get => (bool)GetValue(ShowInputErrorProperty);
            set => SetValue(ShowInputErrorProperty, value);
        }

        public static readonly DependencyProperty InputErrorDurationProperty =
            DependencyProperty.Register(
                nameof(InputErrorDuration),
                typeof(int),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(150));

        /// <summary>
        /// Gets or sets the duration in milliseconds for the error flash. Default is 150ms.
        /// </summary>
        public int InputErrorDuration
        {
            get => (int)GetValue(InputErrorDurationProperty);
            set => SetValue(InputErrorDurationProperty, value);
        }

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyInputVariant),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(DaisyInputVariant.Bordered, OnStyleOrValueConfigChanged));

        public DaisyInputVariant Variant
        {
            get => (DaisyInputVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(DaisySize.Medium, OnStyleOrValueConfigChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ShowButtonsProperty =
            DependencyProperty.Register(
                nameof(ShowButtons),
                typeof(bool),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(true, OnStyleOrValueConfigChanged));

        public bool ShowButtons
        {
            get => (bool)GetValue(ShowButtonsProperty);
            set => SetValue(ShowButtonsProperty, value);
        }

        public static readonly DependencyProperty ShowClearButtonProperty =
            DependencyProperty.Register(
                nameof(ShowClearButton),
                typeof(bool),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(false, OnStyleOrValueConfigChanged));

        public bool ShowClearButton
        {
            get => (bool)GetValue(ShowClearButtonProperty);
            set => SetValue(ShowClearButtonProperty, value);
        }

        public static readonly DependencyProperty NumberBaseProperty =
            DependencyProperty.Register(
                nameof(NumberBase),
                typeof(DaisyNumberBase),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(DaisyNumberBase.Decimal, OnStyleOrValueConfigChanged));

        public DaisyNumberBase NumberBase
        {
            get => (DaisyNumberBase)GetValue(NumberBaseProperty);
            set => SetValue(NumberBaseProperty, value);
        }

        public static readonly DependencyProperty ShowThousandSeparatorsProperty =
            DependencyProperty.Register(
                nameof(ShowThousandSeparators),
                typeof(bool),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(false, OnStyleOrValueConfigChanged));

        public bool ShowThousandSeparators
        {
            get => (bool)GetValue(ShowThousandSeparatorsProperty);
            set => SetValue(ShowThousandSeparatorsProperty, value);
        }

        public static readonly DependencyProperty ShowBasePrefixProperty =
            DependencyProperty.Register(
                nameof(ShowBasePrefix),
                typeof(bool),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(true, OnStyleOrValueConfigChanged));

        public bool ShowBasePrefix
        {
            get => (bool)GetValue(ShowBasePrefixProperty);
            set => SetValue(ShowBasePrefixProperty, value);
        }

        public static readonly DependencyProperty HexCaseProperty =
            DependencyProperty.Register(
                nameof(HexCase),
                typeof(DaisyHexCase),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(DaisyHexCase.Upper, OnStyleOrValueConfigChanged));

        public DaisyHexCase HexCase
        {
            get => (DaisyHexCase)GetValue(HexCaseProperty);
            set => SetValue(HexCaseProperty, value);
        }

        public static readonly DependencyProperty ColorHexDigitsProperty =
            DependencyProperty.Register(
                nameof(ColorHexDigits),
                typeof(int),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(6, OnStyleOrValueConfigChanged));

        public int ColorHexDigits
        {
            get => (int)GetValue(ColorHexDigitsProperty);
            set => SetValue(ColorHexDigitsProperty, value);
        }

        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register(
                nameof(Prefix),
                typeof(string),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(null, OnStyleOrValueConfigChanged));

        public string? Prefix
        {
            get => (string?)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register(
                nameof(Suffix),
                typeof(string),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(null, OnStyleOrValueConfigChanged));

        public string? Suffix
        {
            get => (string?)GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        public static readonly DependencyProperty MaxDecimalPlacesProperty =
            DependencyProperty.Register(
                nameof(MaxDecimalPlaces),
                typeof(int),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(-1, OnStyleOrValueConfigChanged));

        public int MaxDecimalPlaces
        {
            get => (int)GetValue(MaxDecimalPlacesProperty);
            set => SetValue(MaxDecimalPlacesProperty, value);
        }

        public static readonly DependencyProperty MaxIntegerDigitsProperty =
            DependencyProperty.Register(
                nameof(MaxIntegerDigits),
                typeof(int),
                typeof(DaisyNumericUpDown),
                new PropertyMetadata(-1, OnStyleOrValueConfigChanged));

        public int MaxIntegerDigits
        {
            get => (int)GetValue(MaxIntegerDigitsProperty);
            set => SetValue(MaxIntegerDigitsProperty, value);
        }

        #endregion

        #region Value Conversion Helpers

        public string? ToHexString(bool includePrefix = true)
        {
            if (Value == null) return null;
            var format = HexCase == DaisyHexCase.Upper ? "X" : "x";
            var hex = ((long)Value.Value).ToString(format);
            return includePrefix ? "0x" + hex : hex;
        }

        public string? ToBinaryString(bool includePrefix = true)
        {
            if (Value == null) return null;
            var binary = Convert.ToString((long)Value.Value, 2);
            return includePrefix ? "0b" + binary : binary;
        }

        public string? ToOctalString(bool includePrefix = true)
        {
            if (Value == null) return null;
            var octal = Convert.ToString((long)Value.Value, 8);
            return includePrefix ? "0o" + octal : octal;
        }

        public string? ToColorHexString(bool includePrefix = true)
        {
            if (Value == null) return null;
            var format = HexCase == DaisyHexCase.Upper ? "X" : "x";
            var hex = ((long)Value.Value).ToString(format).PadLeft(GetColorHexDigits(), '0');
            return includePrefix ? "#" + hex : hex;
        }

        public string? ToIPAddressString()
        {
            if (Value == null) return null;
            var ipValue = (uint)Math.Max(0, Math.Min(uint.MaxValue, Value.Value));
            return $"{(ipValue >> 24) & 0xFF}.{(ipValue >> 16) & 0xFF}.{(ipValue >> 8) & 0xFF}.{ipValue & 0xFF}";
        }

        public string? ToFormattedString(bool includePrefix = true)
        {
            if (Value == null) return null;
            return NumberBase switch
            {
                DaisyNumberBase.Hexadecimal => ToHexString(includePrefix && ShowBasePrefix),
                DaisyNumberBase.ColorHex => ToColorHexString(includePrefix && ShowBasePrefix),
                DaisyNumberBase.Binary => ToBinaryString(includePrefix && ShowBasePrefix),
                DaisyNumberBase.Octal => ToOctalString(includePrefix && ShowBasePrefix),
                DaisyNumberBase.IPAddress => ToIPAddressString(),
                _ => Value.Value.ToString(FormatString ?? "0", CultureInfo.CurrentCulture)
            };
        }

        #endregion

        #region Lifecycle

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplyAll();

            // On Android, setting TextBox.Text during initialization can trigger IME focus events,
            // causing multiple controls to fight for focus. Prevent this by temporarily disabling
            // focus capability during the initial text update.
            if (PlatformCompatibility.IsAndroid && _textBox != null)
            {
                _textBox.IsTabStop = false;
                _textBox.AllowFocusOnInteraction = false;
            }

            UpdateDisplayText();

            // Re-enable focus after initial text is set
            if (PlatformCompatibility.IsAndroid && _textBox != null)
            {
                // Use dispatcher to re-enable focus after the current layout pass completes
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (_textBox != null)
                    {
                        _textBox.IsTabStop = true;
                        _textBox.AllowFocusOnInteraction = true;
                    }
                });
            }

            if (_pendingFocusRequest)
            {
                AttachPendingFocusHandler();
                TryFocusTextBox(_pendingFocusState, _pendingFocusSelectAll);
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
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

        public void FocusInput(bool selectAll = false)
        {
            if (TryFocusTextBox(FocusState.Programmatic, selectAll))
                return;

            _pendingFocusRequest = true;
            _pendingFocusSelectAll = selectAll;
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

            if (TryFocusTextBox(_pendingFocusState, _pendingFocusSelectAll))
            {
                DetachPendingFocusHandler();
            }
        }

        private bool TryFocusTextBox(FocusState focusState, bool selectAll)
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

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _rootBorder ?? base.GetNeumorphicHostElement();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            if (_rootBorder != null)
                return;

            _rootGrid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Columns are always present; visibility toggles handle optional elements.
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // clear
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // prefix
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // textbox
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // suffix
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // buttons

            // Note: Clear button is not added here - it will be handled by the textbox overlay
            // Column 0 is now empty/unused for future expansion

            _clearButton = new Button
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = Visibility.Collapsed, // Hidden by default, shown when focused + ShowClearButton
                Content = FloweryPathHelpers.CreateClose(size: 12, strokeThickness: 2)
            };
            _clearButton.Click += OnClearClick;
            // Don't add the clear button to the grid - the default TextBox clear button works fine

            _prefixTextBlock = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0),
                Opacity = 0.8
            };
            Grid.SetColumn(_prefixTextBlock, 1);
            _rootGrid.Children.Add(_prefixTextBlock);

            _textBox = new TextBox
            {
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                VerticalAlignment = VerticalAlignment.Stretch
                // VerticalContentAlignment set in ApplySizing with calculated padding
            };
            _textBox.GotFocus += OnTextBoxGotFocus;
            _textBox.LostFocus += OnTextBoxLostFocus;
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.BeforeTextChanging += OnBeforeTextChanging;
            _textBox.TextChanged += OnTextBoxTextChanged;
            _textBox.Paste += OnTextBoxPaste;

            // Theme the native TextBox clear button (DeleteButton) to use proper DaisyUI colors
            // WinUI uses TextControlButton*, Uno Platform uses TextBoxDeleteButton*
            var baseContentBrush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") ?? new SolidColorBrush(Colors.Black);
            // WinUI resource keys
            _textBox.Resources["TextControlButtonForeground"] = baseContentBrush;
            _textBox.Resources["TextControlButtonForegroundPointerOver"] = baseContentBrush;
            _textBox.Resources["TextControlButtonForegroundPressed"] = baseContentBrush;
            // Uno Platform resource keys
            _textBox.Resources["TextBoxDeleteButtonForeground"] = baseContentBrush;
            _textBox.Resources["TextBoxDeleteButtonForegroundPointerOver"] = baseContentBrush;
            _textBox.Resources["TextBoxDeleteButtonForegroundPressed"] = baseContentBrush;
            _textBox.Resources["TextBoxDeleteButtonForegroundFocused"] = baseContentBrush;

            Grid.SetColumn(_textBox, 2);
            _rootGrid.Children.Add(_textBox);

            _suffixTextBlock = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0),
                Opacity = 0.8
            };
            Grid.SetColumn(_suffixTextBlock, 3);
            _rootGrid.Children.Add(_suffixTextBlock);

            // Use StackPanel for compact, centered chevrons with minimal spacing
            _buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 0 // Minimal gap between chevrons
            };

            _increaseButton = new RepeatButton
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2, 0, 2, 0),
                MinHeight = 0,
                MinWidth = 0
            };
            _increaseIconPath = FloweryPathHelpers.CreateChevronUp(size: 12, strokeThickness: 2);
            _increaseButton.Content = _increaseIconPath;
            _increaseButton.Click += OnIncreaseClick;
            _buttonsPanel.Children.Add(_increaseButton);

            _decreaseButton = new RepeatButton
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2, 0, 2, 0),
                MinHeight = 0,
                MinWidth = 0
            };
            _decreaseIconPath = FloweryPathHelpers.CreateChevronDown(size: 12, strokeThickness: 2);
            _decreaseButton.Content = _decreaseIconPath;
            _decreaseButton.Click += OnDecreaseClick;
            _buttonsPanel.Children.Add(_decreaseButton);

            Grid.SetColumn(_buttonsPanel, 4);
            _rootGrid.Children.Add(_buttonsPanel);

            _rootBorder = new Border
            {
                Child = _rootGrid,
                Padding = new Thickness(0)
            };

            _rootHost = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _rootHost.Children.Add(_rootBorder);

            Content = _rootHost;
        }

        private static Path CreateIconPathFromResource(string key, double width, double height, FrameworkElement source)
        {
            var path = new Path
            {
                Width = width,
                Height = height,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            try
            {
                var geometry = FindGeometryResource(key);
                path.Data = geometry ?? new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 1, 1) };
            }
            catch
            {
                path.Data = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 1, 1) };
            }

            path.SetBinding(Shape.FillProperty, new Binding { Source = source, Path = new PropertyPath("Foreground") });
            return path;
        }

        private static Geometry? FindGeometryResource(string key)
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
                return null;

            var found = FindResourceRecursive(resources, key);
            return found as Geometry;
        }

        private static object? FindResourceRecursive(ResourceDictionary dictionary, string key)
        {
            if (dictionary.TryGetValue(key, out var value))
                return value;

            // Search merged dictionaries (this is where Flowery.Uno/Themes/DaisyIcons.xaml lives)
            foreach (var merged in dictionary.MergedDictionaries)
            {
                var found = FindResourceRecursive(merged, key);
                if (found != null)
                    return found;
            }

            return null;
        }

        #endregion

        #region Apply Styling

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyNumericUpDown control)
            {
                if (control._isUpdatingValue)
                    return;

                if (!control._isEditing)
                {
                    control.UpdateDisplayText();
                }
            }
        }

        private static void OnStyleOrValueConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyNumericUpDown control)
            {
                control.ApplyAll();
                if (!control._isEditing)
                    control.UpdateDisplayText();
            }
        }

        private void ApplyAll()
        {
            if (_rootBorder == null || _textBox == null || _prefixTextBlock == null || _suffixTextBlock == null || _clearButton == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            ApplySizing(resources);
            ApplyTheme(resources);
            ApplyLayoutParts();
        }

        private void ApplySizing(ResourceDictionary? resources)
        {
            if (_rootBorder == null || _textBox == null)
                return;

            // NumericUpDown uses standard heights to match DaisySelect and other controls.
            // Chevrons are compact and centered (via StackPanel), requiring no extra height.
            var controlHeight = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "Height",
                DaisyResourceLookup.GetDefaultHeight(Size));
            var fontSize = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "FontSize",
                DaisyResourceLookup.GetDefaultFontSize(Size));
            var padding = DaisyResourceLookup.GetSizeThickness(resources, "DaisyButton", Size, "Padding",
                DaisyResourceLookup.GetDefaultPadding(Size));

            Height = controlHeight;
            FontSize = fontSize;

            // TextBox uses horizontal padding from container, but needs explicit vertical centering.
            _rootBorder.Padding = new Thickness(padding.Left, 0, padding.Right, 0);
            _textBox.FontSize = fontSize;

            // Compute vertical padding to center text properly.
            // WinUI TextBox internal layout doesn't reliably center text when constrained.
            // Instead, calculate explicit padding to position the text in the vertical center.
            //
            // Line height for Segoe UI is approximately 1.5 × font size.
            // This accounts for ascenders, descenders, and WinUI TextBox internal template spacing.
            // We subtract 2px from control height to account for the border.
            var availableHeight = controlHeight - 2;
            var fontLineHeight = Math.Ceiling(fontSize * 1.5);

            // Platform-specific vertical centering:
            // Windows: Text sits 1px too low, reduce top padding to push UP (opposite of DaisyInput).
            // Skia Desktop: symmetric padding centers correctly without adjustment.
            var verticalPadding = Math.Max(0, Math.Floor((availableHeight - fontLineHeight) / 2));
            var needsAdjustment = !PlatformCompatibility.IsSkiaBackend && !PlatformCompatibility.IsWasmBackend && Size <= DaisySize.Medium;
            var topPadding = needsAdjustment ? Math.Max(0, verticalPadding - 1) : verticalPadding;
            var bottomPadding = verticalPadding;

            // Platform-specific height handling:
            // - Windows: ClearValue to avoid text clipping (parent container controls size)
            // - Skia: Explicit Height needed to enforce our compact sizes
            if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
            {
                _textBox.ClearValue(FrameworkElement.MinHeightProperty);
                _textBox.ClearValue(FrameworkElement.MaxHeightProperty);
                _textBox.Height = availableHeight;
            }
            else
            {
                _textBox.ClearValue(FrameworkElement.HeightProperty);
                _textBox.ClearValue(FrameworkElement.MinHeightProperty);
                _textBox.ClearValue(FrameworkElement.MaxHeightProperty);
            }

            _textBox.Padding = new Thickness(0, topPadding, 0, bottomPadding);
            // Use Top alignment since we're manually controlling vertical positioning via padding
            _textBox.VerticalContentAlignment = VerticalAlignment.Top;

            // Apply font size to prefix/suffix and size-aware margins
            if (_prefixTextBlock != null)
            {
                _prefixTextBlock.FontSize = FontSize;
                _prefixTextBlock.Margin = new Thickness(padding.Left / 2, 0, padding.Left / 2, 0);
            }
            if (_suffixTextBlock != null)
            {
                _suffixTextBlock.FontSize = FontSize;
                _suffixTextBlock.Margin = new Thickness(padding.Right / 2, 0, padding.Right / 2, 0);
            }

            var radius = resources != null
                ? DaisyResourceLookup.GetCornerRadius(resources, "DaisyRoundedBtn", new CornerRadius(8))
                : new CornerRadius(8);
            _rootBorder.CornerRadius = radius;

            _textBox.MaxLength = GetMaxLength();

            // Size the up/down button icons based on Size.
            // Use spinner-specific icon size (smaller since two are stacked compactly).
            // Recreate icons with proportional stroke thickness for crisp rendering.
            double iconSize = DaisyResourceLookup.GetDefaultSpinnerIconSize(Size);
            double strokeThickness = Size switch
            {
                DaisySize.ExtraSmall => 1.4,
                DaisySize.Small => 1.5,
                DaisySize.Medium => 1.6,
                DaisySize.Large => 1.8,
                DaisySize.ExtraLarge => 1.8,
                _ => 1.6
            };

            if (_increaseButton != null)
            {
                _increaseIconPath = FloweryPathHelpers.CreateChevronUp(size: iconSize, strokeThickness: strokeThickness);
                _increaseButton.Content = _increaseIconPath;
                // Horizontal padding only - vertical spacing handled by StackPanel
                _increaseButton.Padding = new Thickness(padding.Left / 2, 0, padding.Right / 2, 0);
            }
            if (_decreaseButton != null)
            {
                _decreaseIconPath = FloweryPathHelpers.CreateChevronDown(size: iconSize, strokeThickness: strokeThickness);
                _decreaseButton.Content = _decreaseIconPath;
                _decreaseButton.Padding = new Thickness(padding.Left / 2, 0, padding.Right / 2, 0);
            }

            // Buttons panel width
            if (_buttonsPanel != null)
            {
                _buttonsPanel.Width = DaisyResourceLookup.GetDefaultSpinnerGridWidth(Size);
            }
        }

        private void ApplyTheme(ResourceDictionary? resources)
        {
            if (_rootBorder == null || _textBox == null || _prefixTextBlock == null || _suffixTextBlock == null)
                return;

            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            var base100 = DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush", new SolidColorBrush(Microsoft.UI.Colors.White));
            var base200 = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush", new SolidColorBrush(Color.FromArgb(255, 211, 211, 211)));
            var base300 = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)));
            var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)));

            var primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)));
            var secondary = DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", new SolidColorBrush(Color.FromArgb(255, 255, 105, 180)));
            var accent = DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", new SolidColorBrush(Color.FromArgb(255, 0, 128, 128)));
            var info = DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", new SolidColorBrush(Color.FromArgb(255, 0, 191, 255)));
            var success = DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", new SolidColorBrush(Color.FromArgb(255, 50, 205, 50)));
            var warning = DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)));
            var error = DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", new SolidColorBrush(Color.FromArgb(255, 205, 92, 92)));

            Brush background;
            Brush foreground;
            Brush border;
            Thickness borderThickness;

            switch (Variant)
            {
                case DaisyInputVariant.Primary:
                    background = base100;
                    foreground = baseContent;
                    border = primary;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
                case DaisyInputVariant.Secondary:
                    background = base100;
                    foreground = baseContent;
                    border = secondary;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
                case DaisyInputVariant.Accent:
                    background = base100;
                    foreground = baseContent;
                    border = accent;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
                case DaisyInputVariant.Info:
                    background = base100;
                    foreground = baseContent;
                    border = info;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
                case DaisyInputVariant.Success:
                    background = base100;
                    foreground = baseContent;
                    border = success;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
                case DaisyInputVariant.Warning:
                    background = base100;
                    foreground = baseContent;
                    border = warning;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
                case DaisyInputVariant.Error:
                    background = base100;
                    foreground = baseContent;
                    border = error;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
                case DaisyInputVariant.Ghost:
                    background = transparent;
                    foreground = baseContent;
                    border = transparent;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessNone", new Thickness(0));
                    break;
                case DaisyInputVariant.Filled:
                    background = base200;
                    foreground = baseContent;
                    border = base200;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessNone", new Thickness(0));
                    break;
                case DaisyInputVariant.Bordered:
                default:
                    background = base100;
                    foreground = baseContent;
                    border = base300;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
            }

            // Check for lightweight styling overrides (instance-level takes precedence)
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyNumericUpDown", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyNumericUpDown", "BorderBrush");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyNumericUpDown", "Foreground");

            if (_rootBorder != null)
            {
                var isFocused = _textBox != null && _textBox.FocusState != FocusState.Unfocused;
                if (isFocused)
                {
                    _rootBorder.BorderBrush = GetVariantFocusBrush(resources);
                }
                else
                {
                    _rootBorder.BorderBrush = borderOverride ?? border;
                }
                _rootBorder.Background = bgOverride ?? background;
                _rootBorder.BorderThickness = borderThickness;

                _normalBorderBrush = borderOverride ?? border;
            }

            var effectiveForeground = fgOverride ?? foreground;
            var textBox = _textBox;
            if (textBox != null)
                textBox.Foreground = effectiveForeground;

            var prefixTextBlock = _prefixTextBlock;
            if (prefixTextBlock != null)
                prefixTextBlock.Foreground = effectiveForeground;

            var suffixTextBlock = _suffixTextBlock;
            if (suffixTextBlock != null)
                suffixTextBlock.Foreground = effectiveForeground;

            if (_clearButton != null)
                _clearButton.Foreground = effectiveForeground;
            if (_increaseButton != null)
                _increaseButton.Foreground = effectiveForeground;
            if (_decreaseButton != null)
                _decreaseButton.Foreground = effectiveForeground;

            // Update TextBox state resources to follow Daisy theme colors instead of system defaults.
            // WinUI uses TextControl* keys; Uno Platform uses TextBoxDeleteButton* keys.
            if (textBox != null)
            {
                textBox.Resources["TextControlForegroundPointerOver"] = effectiveForeground;
                textBox.Resources["TextControlForegroundFocused"] = effectiveForeground;
                textBox.Resources["TextControlForegroundDisabled"] = effectiveForeground;

                textBox.Resources["TextControlButtonForeground"] = effectiveForeground;
                textBox.Resources["TextControlButtonForegroundPointerOver"] = effectiveForeground;
                textBox.Resources["TextControlButtonForegroundPressed"] = effectiveForeground;

                textBox.Resources["TextBoxDeleteButtonForeground"] = effectiveForeground;
                textBox.Resources["TextBoxDeleteButtonForegroundPointerOver"] = effectiveForeground;
                textBox.Resources["TextBoxDeleteButtonForegroundPressed"] = effectiveForeground;
                textBox.Resources["TextBoxDeleteButtonForegroundFocused"] = effectiveForeground;
            }
        }

        private Brush GetVariantFocusBrush(ResourceDictionary? resources)
        {
            var primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Microsoft.UI.Colors.Blue));

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

        private void ApplyLayoutParts()
        {
            if (_clearButton == null || _prefixTextBlock == null || _suffixTextBlock == null || _rootGrid == null)
                return;

            _clearButton.Visibility = ShowClearButton ? Visibility.Visible : Visibility.Collapsed;
            _prefixTextBlock.Text = Prefix ?? string.Empty;
            _prefixTextBlock.Visibility = string.IsNullOrWhiteSpace(Prefix) ? Visibility.Collapsed : Visibility.Visible;

            _suffixTextBlock.Text = Suffix ?? string.Empty;
            _suffixTextBlock.Visibility = string.IsNullOrWhiteSpace(Suffix) ? Visibility.Collapsed : Visibility.Visible;

            if (_increaseButton != null)
                _increaseButton.Visibility = ShowButtons ? Visibility.Visible : Visibility.Collapsed;
            if (_decreaseButton != null)
                _decreaseButton.Visibility = ShowButtons ? Visibility.Visible : Visibility.Collapsed;

            if (_textBox != null)
                _textBox.IsReadOnly = IsReadOnly;
        }

        #endregion

        #region Input / Parsing / Formatting

        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            _isEditing = true;
            ApplyTheme(Application.Current?.Resources);
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            _isEditing = false;
            TryParseAndSetValue();
            UpdateDisplayText();
            ApplyTheme(Application.Current?.Resources);
        }

        private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsReadOnly)
                return;

            switch (e.Key)
            {
                case Windows.System.VirtualKey.Up:
                    Increase();
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.Down:
                    Decrease();
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.Enter:
                    TryParseAndSetValue();
                    UpdateDisplayText(forceFormatted: true);
                    e.Handled = true;
                    return;
            }
        }

        private void OnBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            if (_textBox == null || IsReadOnly || _isUpdatingText)
                return;

            var oldText = _textBox.Text ?? string.Empty;
            var newText = args.NewText ?? string.Empty;

            // Always allow deletes.
            if (newText.Length < oldText.Length)
                return;
            if (!IsValidFullText(newText))
            {
                args.Cancel = true;
                FlashInputError();
            }
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_textBox == null || _isUpdatingText)
                return;

            // Enforce casing post-change (covers paste and IME cases).
            // Apply HexCase to digits, but keep prefix ("#" or "0x") lowercase.
            if (NumberBase == DaisyNumberBase.Hexadecimal || NumberBase == DaisyNumberBase.ColorHex)
            {
                var text = _textBox.Text ?? string.Empty;
                var prefixLen = NumberBase == DaisyNumberBase.ColorHex ? 1 : 2; // "#" vs "0x"
                var cased = text.Length <= prefixLen
                    ? text.ToLowerInvariant()
                    : text[..prefixLen].ToLowerInvariant() + (HexCase == DaisyHexCase.Upper ? text[prefixLen..].ToUpperInvariant() : text[prefixLen..].ToLowerInvariant());

                if (cased != text)
                {
                    var caret = _textBox.SelectionStart;
                    _isUpdatingText = true;
                    try
                    {
                        _textBox.Text = cased;
                        _textBox.SelectionStart = Math.Min(caret, cased.Length);
                    }
                    finally
                    {
                        _isUpdatingText = false;
                    }
                }
            }
        }

        private async void OnTextBoxPaste(object sender, TextControlPasteEventArgs e)
        {
            if (_textBox == null || IsReadOnly)
                return;

            e.Handled = true;

            try
            {
                var view = Clipboard.GetContent();
                var pastedText = await view.GetTextAsync();
                if (string.IsNullOrEmpty(pastedText))
                    return;

                var filtered = FilterPastedText(pastedText);
                if (string.IsNullOrEmpty(filtered))
                {
                    FlashInputError();
                    return;
                }

                var start = _textBox.SelectionStart;
                var len = _textBox.SelectionLength;
                var current = _textBox.Text ?? string.Empty;

                var newText = len > 0 && start + len <= current.Length
                    ? current.Remove(start, len).Insert(start, filtered)
                    : current.Insert(start, filtered);

                if (!IsWithinMaxLength(newText))
                {
                    var maxLen = GetMaxLength();
                    if (maxLen > 0 && newText.Length > maxLen)
                        newText = newText[..maxLen];
                }

                _isUpdatingText = true;
                try
                {
                    _textBox.Text = newText;
                    _textBox.SelectionStart = Math.Min(start + filtered.Length, newText.Length);
                }
                finally
                {
                    _isUpdatingText = false;
                }

                if (filtered != pastedText)
                    FlashInputError();
            }
            catch
            {
                // ignore paste failures
            }
        }

        private void TryParseAndSetValue()
        {
            if (_textBox == null || _isUpdatingValue)
                return;

            var text = _textBox.Text ?? string.Empty;

            // Handle empty/cleared text - set to minimum (or 0 if minimum is negative)
            if (string.IsNullOrWhiteSpace(text))
            {
                var clearValue = Math.Max(0, Minimum);
                if (clearValue >= Minimum && clearValue <= Maximum && Value != clearValue)
                {
                    _isUpdatingValue = true;
                    try
                    {
                        Value = clearValue;
                    }
                    finally
                    {
                        _isUpdatingValue = false;
                    }
                }
                return;
            }

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text[2..];
            else if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                text = text[2..];
            else if (text.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
                text = text[2..];
            else if (text.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                text = text[1..];

            try
            {
                decimal newValue;
                switch (NumberBase)
                {
                    case DaisyNumberBase.Hexadecimal:
                    case DaisyNumberBase.ColorHex:
                        if (long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexVal))
                            newValue = hexVal;
                        else
                            return;
                        break;
                    case DaisyNumberBase.Binary:
                        newValue = Convert.ToInt64(text, 2);
                        break;
                    case DaisyNumberBase.Octal:
                        newValue = Convert.ToInt64(text, 8);
                        break;
                    case DaisyNumberBase.IPAddress:
                        var parts = text.Split('.');
                        if (parts.Length != 4)
                            return;
                        uint ipValue = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            if (!byte.TryParse(parts[i], out var octet))
                                return;
                            ipValue = (ipValue << 8) | octet;
                        }
                        newValue = ipValue;
                        break;
                    default:
                        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
                        text = text.Replace(separator, string.Empty);
                        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var decVal))
                            newValue = decVal;
                        else
                            return;
                        break;
                }

                if (newValue >= Minimum && newValue <= Maximum)
                {
                    _isUpdatingValue = true;
                    try
                    {
                        Value = newValue;
                    }
                    finally
                    {
                        _isUpdatingValue = false;
                    }
                }
            }
            catch
            {
                // ignore invalid input
            }
        }

        private void UpdateDisplayText(bool forceFormatted = false)
        {
            var textBox = _textBox;
            if (textBox == null || Value == null || _isUpdatingText)
                return;

            var value = Value.Value;
            var showFormatting = forceFormatted || !_isEditing;

            string displayText;
            switch (NumberBase)
            {
                case DaisyNumberBase.Hexadecimal:
                    var hexFormat = HexCase == DaisyHexCase.Upper ? "X" : "x";
                    displayText = ((long)value).ToString(hexFormat);
                    if (ShowBasePrefix && showFormatting)
                        displayText = "0x" + displayText;
                    break;
                case DaisyNumberBase.ColorHex:
                    var colorFormat = HexCase == DaisyHexCase.Upper ? "X" : "x";
                    displayText = ((long)value).ToString(colorFormat);
                    var colorHexDigits = GetColorHexDigits();
                    if (displayText.Length < colorHexDigits)
                        displayText = displayText.PadLeft(colorHexDigits, '0');
                    if (ShowBasePrefix && showFormatting)
                        displayText = "#" + displayText;
                    break;
                case DaisyNumberBase.Binary:
                    displayText = Convert.ToString((long)value, 2);
                    if (ShowBasePrefix && showFormatting)
                        displayText = "0b" + displayText;
                    break;
                case DaisyNumberBase.Octal:
                    displayText = Convert.ToString((long)value, 8);
                    if (ShowBasePrefix && showFormatting)
                        displayText = "0o" + displayText;
                    break;
                case DaisyNumberBase.IPAddress:
                    var ipValue = (uint)Math.Max(0, Math.Min(uint.MaxValue, value));
                    displayText = $"{(ipValue >> 24) & 0xFF}.{(ipValue >> 16) & 0xFF}.{(ipValue >> 8) & 0xFF}.{ipValue & 0xFF}";
                    break;
                default:
                    if (ShowThousandSeparators && showFormatting)
                        displayText = value.ToString("N0", CultureInfo.CurrentCulture);
                    else
                        displayText = value.ToString(FormatString ?? "0", CultureInfo.CurrentCulture);
                    break;
            }

            if (textBox.Text == displayText)
                return;

            var caret = textBox.SelectionStart;

            // On Android, setting TextBox.Text can trigger IME focus events that interfere
            // with focus transitions. Temporarily disable focus capture during the update.
            bool isAndroid = PlatformCompatibility.IsAndroid;
            if (isAndroid)
            {
                textBox.AllowFocusOnInteraction = false;
            }

            _isUpdatingText = true;
            try
            {
                textBox.Text = displayText;
                textBox.SelectionStart = Math.Min(caret, displayText.Length);
            }
            finally
            {
                _isUpdatingText = false;
                if (isAndroid)
                {
                    // Use dispatcher to re-enable focus after the text change event settles
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (textBox != null)
                            textBox.AllowFocusOnInteraction = true;
                    });
                }
            }
        }

        private void Increase()
        {
            if (NumberBase == DaisyNumberBase.IPAddress)
            {
                IncrementIPOctet(1);
                return;
            }

            var newValue = (Value ?? 0) + Increment;
            if (newValue <= Maximum)
            {
                _isUpdatingValue = true;
                try
                {
                    Value = newValue;
                }
                finally
                {
                    _isUpdatingValue = false;
                }
                UpdateDisplayText(forceFormatted: true);
            }
        }

        private void Decrease()
        {
            if (NumberBase == DaisyNumberBase.IPAddress)
            {
                IncrementIPOctet(-1);
                return;
            }

            var newValue = (Value ?? 0) - Increment;
            if (newValue >= Minimum)
            {
                _isUpdatingValue = true;
                try
                {
                    Value = newValue;
                }
                finally
                {
                    _isUpdatingValue = false;
                }
                UpdateDisplayText(forceFormatted: true);
            }
        }

        private void OnIncreaseClick(object sender, RoutedEventArgs e) => Increase();

        private void OnDecreaseClick(object sender, RoutedEventArgs e) => Decrease();

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            _isUpdatingValue = true;
            try
            {
                Value = Math.Max(0, Minimum);
            }
            finally
            {
                _isUpdatingValue = false;
            }
            UpdateDisplayText(forceFormatted: true);
            _textBox?.Focus(FocusState.Programmatic);
        }

        private void IncrementIPOctet(int delta)
        {
            if (_textBox == null || Value == null)
                return;

            var text = _textBox.Text ?? string.Empty;
            var caretIndex = _textBox.SelectionStart;
            var parts = text.Split('.');
            if (parts.Length != 4)
                return;

            var octetIndex = 0;
            var pos = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                pos += parts[i].Length;
                if (caretIndex <= pos)
                {
                    octetIndex = i;
                    break;
                }
                pos++;
                octetIndex = i + 1;
            }
            octetIndex = Math.Min(octetIndex, 3);

            if (!int.TryParse(parts[octetIndex], out var octetValue))
                return;

            var newOctetValue = octetValue + delta;
            if (newOctetValue < 0)
                newOctetValue = 255;
            else if (newOctetValue > 255)
                newOctetValue = 0;

            parts[octetIndex] = newOctetValue.ToString(CultureInfo.InvariantCulture);

            var newText = string.Join(".", parts);
            uint ipValue = 0;
            for (int i = 0; i < 4; i++)
            {
                if (byte.TryParse(parts[i], out var b))
                    ipValue = (ipValue << 8) | b;
            }

            _isUpdatingValue = true;
            try
            {
                Value = ipValue;
            }
            finally
            {
                _isUpdatingValue = false;
            }

            _isUpdatingText = true;
            try
            {
                _textBox.Text = newText;
                _textBox.SelectionStart = Math.Min(caretIndex, newText.Length);
            }
            finally
            {
                _isUpdatingText = false;
            }
        }

        private string FilterPastedText(string text)
        {
            var result = new System.Text.StringBuilder();

            foreach (var c0 in text)
            {
                var c = c0;

                if ((NumberBase == DaisyNumberBase.Hexadecimal || NumberBase == DaisyNumberBase.ColorHex) &&
                    ((c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                {
                    c = HexCase == DaisyHexCase.Upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
                }

                if (IsValidForPaste(c))
                    result.Append(c);
            }

            return result.ToString();
        }

        private bool IsValidForPaste(char c)
        {
            switch (NumberBase)
            {
                case DaisyNumberBase.Hexadecimal:
                    return char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f') || c == 'x' || c == 'X';
                case DaisyNumberBase.ColorHex:
                    return char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f') || c == '#';
                case DaisyNumberBase.Binary:
                    return c == '0' || c == '1' || c == 'b' || c == 'B';
                case DaisyNumberBase.Octal:
                    return (c >= '0' && c <= '7') || c == 'o' || c == 'O';
                case DaisyNumberBase.IPAddress:
                    return char.IsDigit(c) || c == '.';
                default:
                    var decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    var negSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
                    return char.IsDigit(c) || decSep.Contains(c.ToString()) || negSign.Contains(c.ToString()) || c == '+' || c == '-';
            }
        }

        private bool IsWithinMaxLength(string text)
        {
            var maxLen = GetMaxLength();
            return maxLen <= 0 || text.Length <= maxLen;
        }

        private int GetMaxLength()
        {
            return NumberBase switch
            {
                DaisyNumberBase.ColorHex => GetColorHexDigits() + 1,
                DaisyNumberBase.IPAddress => 15,
                DaisyNumberBase.Hexadecimal => 18,
                DaisyNumberBase.Binary => 66,
                DaisyNumberBase.Octal => 24,
                DaisyNumberBase.Decimal => GetDecimalMaxLength(),
                _ => 20
            };
        }

        private int GetColorHexDigits()
        {
            return Math.Clamp(ColorHexDigits, 1, 16);
        }

        private int GetDecimalMaxLength()
        {
            var intDigits = MaxIntegerDigits >= 0 ? MaxIntegerDigits : 15;
            var decDigits = MaxDecimalPlaces >= 0 ? MaxDecimalPlaces : 6;
            return intDigits + decDigits + 6;
        }
        private bool IsValidNumericChar(char c, string currentText, int caretIndex)
        {
            return NumberBase switch
            {
                DaisyNumberBase.Hexadecimal => IsValidHexChar(c, currentText, caretIndex),
                DaisyNumberBase.ColorHex => IsValidColorHexChar(c, currentText, caretIndex),
                DaisyNumberBase.Binary => IsValidBinaryChar(c, currentText, caretIndex),
                DaisyNumberBase.Octal => IsValidOctalChar(c, currentText, caretIndex),
                DaisyNumberBase.IPAddress => IsValidIPAddressChar(c, currentText, caretIndex),
                _ => IsValidDecimalChar(c, currentText, caretIndex)
            };
        }

        private bool IsValidFullText(string text)
        {
            var maxLen = GetMaxLength();
            if (maxLen > 0 && text.Length > maxLen)
                return false;

            var current = string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (!IsValidNumericChar(c, current, current.Length))
                    return false;

                current += c;
            }

            return true;
        }

        private static bool IsValidHexChar(char c, string currentText, int caretIndex)
        {
            if (char.IsDigit(c))
                return true;
            if ((c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                return true;
            if ((c == 'x' || c == 'X') && caretIndex == 1 && currentText.StartsWith('0'))
                return !currentText.Contains('x') && !currentText.Contains('X');
            return false;
        }

        private bool IsValidColorHexChar(char c, string currentText, int caretIndex)
        {
            var colorHexDigits = GetColorHexDigits();
            var maxLength = currentText.StartsWith('#') ? colorHexDigits + 1 : colorHexDigits;
            if (currentText.Length >= maxLength)
                return false;

            if (char.IsDigit(c))
                return true;
            if ((c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                return true;
            if (c == '#' && caretIndex == 0)
                return !currentText.Contains('#');
            return false;
        }

        private static bool IsValidOctalChar(char c, string currentText, int caretIndex)
        {
            if (c >= '0' && c <= '7')
                return true;
            if ((c == 'o' || c == 'O') && caretIndex == 1 && currentText.StartsWith('0'))
                return !currentText.Contains('o') && !currentText.Contains('O');
            return false;
        }

        private static bool IsValidBinaryChar(char c, string currentText, int caretIndex)
        {
            if (c == '0' || c == '1')
                return true;
            if ((c == 'b' || c == 'B') && caretIndex == 1 && currentText.StartsWith('0'))
                return !currentText.Contains('b') && !currentText.Contains('B');
            return false;
        }

        private static bool IsValidIPAddressChar(char c, string currentText, int caretIndex)
        {
            if (currentText.Length >= 15)
                return false;

            if (char.IsDigit(c))
            {
                var parts = currentText.Split('.');
                var currentPartIndex = 0;
                var pos = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    pos += parts[i].Length;
                    if (caretIndex <= pos)
                    {
                        currentPartIndex = i;
                        break;
                    }
                    pos++;
                    currentPartIndex = i + 1;
                }

                var currentOctet = currentPartIndex < parts.Length ? parts[currentPartIndex] : string.Empty;
                var newOctet = currentOctet + c;
                if (int.TryParse(newOctet, out var octetValue))
                    return octetValue <= 255;
                return false;
            }

            if (c == '.')
            {
                var dotCount = 0;
                foreach (var ch in currentText)
                    if (ch == '.') dotCount++;
                return dotCount < 3;
            }

            return false;
        }

        private bool IsValidDecimalChar(char c, string currentText, int caretIndex)
        {
            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
            var positiveSign = CultureInfo.CurrentCulture.NumberFormat.PositiveSign;

            if (char.IsLetter(c))
                return false;

            if (char.IsDigit(c))
            {
                var decSepIndex = currentText.IndexOf(decimalSeparator, StringComparison.Ordinal);

                var isInDecimalPart = decSepIndex >= 0 && caretIndex > decSepIndex;
                var isInIntegerPart = decSepIndex < 0 || caretIndex <= decSepIndex;

                if (isInDecimalPart && MaxDecimalPlaces >= 0)
                {
                    var currentDecimalPlaces = currentText.Length - decSepIndex - decimalSeparator.Length;
                    if (currentDecimalPlaces >= MaxDecimalPlaces)
                        return false;
                }

                if (isInIntegerPart && MaxIntegerDigits >= 0)
                {
                    var integerEndIndex = decSepIndex >= 0 ? decSepIndex : currentText.Length;
                    var startIndex = currentText.StartsWith(negativeSign, StringComparison.Ordinal) ? negativeSign.Length : 0;
                    var currentIntegerDigits = 0;
                    for (int i = startIndex; i < integerEndIndex; i++)
                        if (char.IsDigit(currentText[i])) currentIntegerDigits++;
                    if (currentIntegerDigits >= MaxIntegerDigits)
                        return false;
                }

                return true;
            }

            if (decimalSeparator.Contains(c.ToString()))
            {
                return !currentText.Contains(decimalSeparator, StringComparison.Ordinal);
            }

            if (negativeSign.Contains(c.ToString()) || c == '-')
            {
                if (caretIndex == 0 && !currentText.StartsWith(negativeSign, StringComparison.Ordinal) && Minimum < 0)
                    return true;

                return false;
            }

            if (positiveSign.Contains(c.ToString()) || c == '+')
            {
                return false;
            }

            return false;
        }

        private void FlashInputError()
        {
            if (!ShowInputError || _rootBorder == null)
                return;

            var errorBrush = DaisyResourceLookup.GetBrush("DaisyErrorBrush");
            _rootBorder.BorderBrush = errorBrush;

            _errorFlashTimer?.Stop();
            _errorFlashTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(InputErrorDuration)
            };
            _errorFlashTimer.Tick += (s, e) =>
            {
                _rootBorder.BorderBrush = _normalBorderBrush;
                _errorFlashTimer?.Stop();
            };
            _errorFlashTimer.Start();
        }

        #endregion

        #region Helpers (Sizing/Theme lookups)


        private static Thickness GetThickness(ResourceDictionary? resources, string key, Thickness fallback)
        {
            if (resources == null)
                return fallback;

            return DaisyResourceLookup.GetThickness(resources, key, fallback);
        }

        #endregion
    }
}

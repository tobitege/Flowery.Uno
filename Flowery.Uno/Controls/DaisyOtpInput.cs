using System;
using System.Collections.Generic;
using Flowery.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// An OTP/verification code input composed of multiple single-character slots.
    /// </summary>
    public partial class DaisyOtpInput : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private StackPanel? _slotsPanel;
        private readonly List<TextBox> _slots = [];
        private readonly List<Border> _slotBorders = [];
        private bool _isUpdating;
        public DaisyOtpInput()
        {
            DefaultStyleKey = typeof(DaisyOtpInput);
            IsTabStop = false;

            // Apply global size early if allowed and not explicitly ignored.
            if (FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.GetIgnoreGlobalSize(this))
            {
                Size = FlowerySizeManager.CurrentSize;
            }

        }

        #region Dependency Properties

        public static readonly DependencyProperty LengthProperty =
            DependencyProperty.Register(
                nameof(Length),
                typeof(int),
                typeof(DaisyOtpInput),
                new PropertyMetadata(6, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the number of OTP slots.
        /// </summary>
        public int Length
        {
            get => (int)GetValue(LengthProperty);
            set => SetValue(LengthProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(DaisyOtpInput),
                new PropertyMetadata(null, OnValueChanged));

        /// <summary>
        /// Gets or sets the current OTP value.
        /// </summary>
        public string? Value
        {
            get => (string?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty AcceptsOnlyDigitsProperty =
            DependencyProperty.Register(
                nameof(AcceptsOnlyDigits),
                typeof(bool),
                typeof(DaisyOtpInput),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether only digits are allowed.
        /// </summary>
        public bool AcceptsOnlyDigits
        {
            get => (bool)GetValue(AcceptsOnlyDigitsProperty);
            set => SetValue(AcceptsOnlyDigitsProperty, value);
        }

        public static readonly DependencyProperty AutoAdvanceProperty =
            DependencyProperty.Register(
                nameof(AutoAdvance),
                typeof(bool),
                typeof(DaisyOtpInput),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether focus automatically advances to the next slot.
        /// </summary>
        public bool AutoAdvance
        {
            get => (bool)GetValue(AutoAdvanceProperty);
            set => SetValue(AutoAdvanceProperty, value);
        }

        public static readonly DependencyProperty AutoSelectOnFocusProperty =
            DependencyProperty.Register(
                nameof(AutoSelectOnFocus),
                typeof(bool),
                typeof(DaisyOtpInput),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether slot content is automatically selected when focused.
        /// </summary>
        public bool AutoSelectOnFocus
        {
            get => (bool)GetValue(AutoSelectOnFocusProperty);
            set => SetValue(AutoSelectOnFocusProperty, value);
        }

        public static readonly DependencyProperty SeparatorIntervalProperty =
            DependencyProperty.Register(
                nameof(SeparatorInterval),
                typeof(int),
                typeof(DaisyOtpInput),
                new PropertyMetadata(0, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the slot interval at which a separator is inserted (0 = no separator).
        /// </summary>
        public int SeparatorInterval
        {
            get => (int)GetValue(SeparatorIntervalProperty);
            set => SetValue(SeparatorIntervalProperty, value);
        }

        public static readonly DependencyProperty SeparatorTextProperty =
            DependencyProperty.Register(
                nameof(SeparatorText),
                typeof(string),
                typeof(DaisyOtpInput),
                new PropertyMetadata(string.Empty, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the separator text.
        /// </summary>
        public string SeparatorText
        {
            get => (string)GetValue(SeparatorTextProperty);
            set => SetValue(SeparatorTextProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyOtpInput),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty IsJoinedProperty =
            DependencyProperty.Register(
                nameof(IsJoined),
                typeof(bool),
                typeof(DaisyOtpInput),
                new PropertyMetadata(true, OnLayoutChanged));

        /// <summary>
        /// Gets or sets whether digits within each group are visually joined (shared borders, outer corners rounded).
        /// Default is true.
        /// </summary>
        public bool IsJoined
        {
            get => (bool)GetValue(IsJoinedProperty);
            set => SetValue(IsJoinedProperty, value);
        }

        /// <summary>
        /// Raised when the input becomes complete (Value.Length == Length).
        /// </summary>
        public event EventHandler<string>? Completed;

        #endregion

        #region Callbacks

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyOtpInput otp)
            {
                otp.RebuildSlots();
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyOtpInput otp)
            {
                otp.ApplyValueToSlots();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyOtpInput otp)
            {
                otp.ApplySizing();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                ApplyAll();
                return;
            }

            BuildVisualTree();
            ApplyAll();
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

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            _rootGrid = new Grid();

            _slotsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            _rootGrid.Children.Add(_slotsPanel);
            Content = _rootGrid;

            RebuildSlots();
        }

        private void RebuildSlots()
        {
            if (_slotsPanel == null)
                return;

            _isUpdating = true;
            try
            {
                _slotsPanel.Children.Clear();

                foreach (var slot in _slots)
                {
                    slot.TextChanged -= OnSlotTextChanged;
                    slot.KeyDown -= OnSlotKeyDown;
                    slot.GotFocus -= OnSlotGotFocus;
                }

                _slots.Clear();
                _slotBorders.Clear();

                // Calculate group size for join borders
                int groupSize = SeparatorInterval > 0 ? SeparatorInterval : Length;

                for (int i = 0; i < Length; i++)
                {
                    // Add separator between groups
                    if (SeparatorInterval > 0 && i > 0 && (i % SeparatorInterval) == 0)
                    {
                        if (!string.IsNullOrEmpty(SeparatorText))
                        {
                            var separator = new TextBlock
                            {
                                Text = SeparatorText,
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(8, 0, 8, 0),
                                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                            };
                            _slotsPanel.Children.Add(separator);
                        }
                        else
                        {
                            _slotsPanel.Children.Add(CreateSeparatorIcon());
                        }
                    }

                    var box = CreateSlot(i);
                    _slots.Add(box);

                    // Wrap TextBox in a Grid to properly center it
                    var centeringGrid = new Grid();
                    centeringGrid.Children.Add(box);

                    // Wrap in a border for reliable visual styling
                    var border = new Border
                    {
                        Child = centeringGrid,
                        BorderThickness = new Thickness(1)
                    };

                    // Apply join styling if enabled
                    if (IsJoined)
                    {
                        int positionInGroup = i % groupSize;
                        bool isFirstInGroup = positionInGroup == 0;
                        bool isLastInGroup = positionInGroup == groupSize - 1 || i == Length - 1;

                        // Apply negative margin for border overlap (except first in group)
                        if (!isFirstInGroup)
                        {
                            border.Margin = new Thickness(-1, 0, 0, 0);
                        }

                        // Corner radius only on outer edges of the group
                        border.CornerRadius = GetJoinCornerRadius(isFirstInGroup, isLastInGroup);
                    }
                    else
                    {
                        // All corners rounded when not joined
                        border.CornerRadius = new CornerRadius(8);
                    }

                    _slotBorders.Add(border);
                    _slotsPanel.Children.Add(border);
                }
            }
            finally
            {
                _isUpdating = false;
            }

            ApplyValueToSlots();
            ApplySizing();
            ApplyColors();
        }

        /// <summary>
        /// Gets the corner radius for a slot based on its position in a joined group.
        /// </summary>
        private static CornerRadius GetJoinCornerRadius(bool isFirst, bool isLast, double radius = 8)
        {
            if (isFirst && isLast)
                return new CornerRadius(radius);

            if (isFirst)
                return new CornerRadius(radius, 0, 0, radius); // Left corners only

            if (isLast)
                return new CornerRadius(0, radius, radius, 0); // Right corners only

            return new CornerRadius(0); // No rounded corners for middle slots
        }

        private static Viewbox CreateSeparatorIcon()
        {
            var pathData = FloweryPathHelpers.GetIconPathData("DaisyIconMinus");
            var path = new Microsoft.UI.Xaml.Shapes.Path
            {
                Stretch = Stretch.Uniform,
                Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };

            if (!string.IsNullOrEmpty(pathData))
            {
                FloweryPathHelpers.TrySetPathData(path, () => FloweryPathHelpers.ParseGeometry(pathData));
            }

            return new Viewbox
            {
                Width = 12,
                Height = 12,
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = path
            };
        }

        private TextBox CreateSlot(int index)
        {
            var box = new TextBox
            {
                Tag = index,
                TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxLength = 1,
                MinWidth = 0,
                MinHeight = 0,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                IsSpellCheckEnabled = false,
                IsTextPredictionEnabled = false
            };

            if (AcceptsOnlyDigits)
            {
                box.InputScope = new InputScope
                {
                    Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } }
                };
            }

            // Theme the native TextBox clear button (DeleteButton) to use proper DaisyUI colors
            // WinUI uses TextControlButton*, Uno Platform uses TextBoxDeleteButton*
            var baseContentBrush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") ?? new SolidColorBrush(Microsoft.UI.Colors.Black);
            // WinUI resource keys
            box.Resources["TextControlButtonForeground"] = baseContentBrush;
            box.Resources["TextControlButtonForegroundPointerOver"] = baseContentBrush;
            box.Resources["TextControlButtonForegroundPressed"] = baseContentBrush;
            // Uno Platform resource keys
            box.Resources["TextBoxDeleteButtonForeground"] = baseContentBrush;
            box.Resources["TextBoxDeleteButtonForegroundPointerOver"] = baseContentBrush;
            box.Resources["TextBoxDeleteButtonForegroundPressed"] = baseContentBrush;
            box.Resources["TextBoxDeleteButtonForegroundFocused"] = baseContentBrush;

            box.TextChanged += OnSlotTextChanged;
            box.KeyDown += OnSlotKeyDown;
            box.GotFocus += OnSlotGotFocus;

            return box;
        }

        private void OnSlotGotFocus(object sender, RoutedEventArgs e)
        {
            if (AutoSelectOnFocus && sender is TextBox box)
            {
                box.SelectAll();
            }
        }

        private void OnSlotKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (box.Tag is not int index) return;

            switch (e.Key)
            {
                case Windows.System.VirtualKey.Left:
                    FocusSlot(index - 1);
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Right:
                    FocusSlot(index + 1);
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Back:
                    if (string.IsNullOrEmpty(box.Text) && index > 0)
                    {
                        _isUpdating = true;
                        try
                        {
                            _slots[index - 1].Text = string.Empty;
                        }
                        finally
                        {
                            _isUpdating = false;
                        }
                        UpdateValueFromSlots();
                        FocusSlot(index - 1);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void OnSlotTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (sender is not TextBox box) return;
            if (box.Tag is not int index) return;

            var text = Normalize(box.Text);

            _isUpdating = true;
            try
            {
                if (text.Length == 0)
                {
                    box.Text = string.Empty;
                }
                else if (text.Length == 1)
                {
                    box.Text = text;

                    if (AutoAdvance)
                    {
                        FocusSlot(index + 1);
                    }
                }
                else
                {
                    // Multi-character paste - distribute across slots
                    DistributeText(index, text);
                }
            }
            finally
            {
                _isUpdating = false;
            }

            UpdateValueFromSlots();
        }

        private void DistributeText(int startIndex, string text)
        {
            var slotIndex = startIndex;
            foreach (char c in text)
            {
                if (slotIndex >= _slots.Count) break;
                _slots[slotIndex].Text = c.ToString();
                slotIndex++;
            }

            if (AutoAdvance)
            {
                FocusSlot(Math.Min(startIndex + text.Length, _slots.Count - 1));
            }
        }

        private void FocusSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return;

            var box = _slots[index];

            // Use DispatcherQueue to ensure focus transition works reliably on all platforms, especially Android.
            // Synchronous Focus() often fails or is ignored during TextChanged on Android.
            DispatcherQueue.TryEnqueue(() =>
            {
                box.Focus(FocusState.Programmatic);
                if (AutoSelectOnFocus)
                {
                    box.SelectAll();
                }
            });
        }

        private void ApplyValueToSlots()
        {
            if (_isUpdating) return;

            _isUpdating = true;
            try
            {
                var value = Normalize(Value);
                if (value.Length > Length)
                {
                    value = value[..Length];
                }

                for (int i = 0; i < _slots.Count; i++)
                {
                    _slots[i].Text = i < value.Length ? value[i].ToString() : string.Empty;
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void UpdateValueFromSlots()
        {
            var chars = new List<char>();
            foreach (var slot in _slots)
            {
                var t = Normalize(slot.Text);
                if (t.Length == 0) break;
                chars.Add(t[0]);
            }

            var newValue = new string([.. chars]);
            if (!string.Equals(Value, newValue, StringComparison.Ordinal))
            {
                Value = newValue;
            }

            // Raise completed event
            if (newValue.Length == Length)
            {
                Completed?.Invoke(this, newValue);
            }
        }

        private string Normalize(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            if (!AcceptsOnlyDigits)
            {
                return input!;
            }

            var result = new char[input!.Length];
            var count = 0;
            foreach (char c in input)
            {
                if (c >= '0' && c <= '9')
                {
                    result[count++] = c;
                }
            }

            return new string(result, 0, count);
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            if (_slotsPanel == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);
            var lookupResources = resources ?? [];

            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);

            // Get slot size from tokens
            double slotSize = DaisyResourceLookup.GetDouble(
                lookupResources, $"DaisyOtp{sizeKey}SlotSize",
                DaisyResourceLookup.GetDefaultOtpSlotSize(Size));

            // Get font size from tokens (OTP uses header-style larger fonts)
            double fontSize = DaisyResourceLookup.GetDouble(
                lookupResources, $"DaisyOtp{sizeKey}FontSize",
                DaisyResourceLookup.GetDefaultHeaderFontSize(Size));

            // Get corner radius from standard tokens
            CornerRadius cornerRadiusValue = DaisyResourceLookup.GetCornerRadius(
                lookupResources, $"DaisySize{sizeKey}CornerRadius",
                DaisyResourceLookup.GetDefaultCornerRadius(Size));
            double cornerRadius = cornerRadiusValue.TopLeft;

            // Get spacing from tokens (0 when joined)
            double spacing = DaisyResourceLookup.GetDouble(
                lookupResources, $"DaisyOtp{sizeKey}Spacing",
                DaisyResourceLookup.GetDefaultOtpSpacing(Size));
            _slotsPanel.Spacing = IsJoined ? 0 : spacing;

            // Calculate group size for join borders
            int groupSize = SeparatorInterval > 0 ? SeparatorInterval : Length;

            // Style the wrapper borders
            for (int i = 0; i < _slotBorders.Count; i++)
            {
                var border = _slotBorders[i];
                border.Width = slotSize;
                border.Height = slotSize;

                // Apply join-aware corner radius
                if (IsJoined)
                {
                    int positionInGroup = i % groupSize;
                    bool isFirstInGroup = positionInGroup == 0;
                    bool isLastInGroup = positionInGroup == groupSize - 1 || i == _slotBorders.Count - 1;
                    border.CornerRadius = GetJoinCornerRadius(isFirstInGroup, isLastInGroup, cornerRadius);
                }
                else
                {
                    border.CornerRadius = new CornerRadius(cornerRadius);
                }
            }

            // Make the TextBox minimal - it will be centered in the Grid
            foreach (var slot in _slots)
            {
                slot.FontSize = fontSize;
                slot.BorderThickness = new Thickness(0);
                slot.Padding = new Thickness(0);
                slot.MinWidth = 0;
                slot.MinHeight = 0;
                slot.Width = double.NaN; // Auto-size
                slot.Height = double.NaN; // Auto-size
            }
        }

        private void ApplyColors()
        {
            if (_slotsPanel == null)
                return;

            // Check for lightweight styling overrides
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyOtpInput", "BorderBrush");
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyOtpInput", "Background");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyOtpInput", "Foreground");

            var borderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            var bgBrush = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            var fgBrush = fgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

            // Style wrapper borders
            foreach (var border in _slotBorders)
            {
                border.BorderBrush = borderBrush;
                border.Background = bgBrush;
            }

            // Make TextBoxes transparent so they don't conflict with the border styling
            foreach (var slot in _slots)
            {
                slot.BorderBrush = transparent;
                slot.Background = transparent;
                slot.Foreground = fgBrush;

                // Update clear button (DeleteButton) foreground for theme changes
                // WinUI uses TextControlButton*, Uno Platform uses TextBoxDeleteButton*
                // WinUI resource keys
                slot.Resources["TextControlButtonForeground"] = fgBrush;
                slot.Resources["TextControlButtonForegroundPointerOver"] = fgBrush;
                slot.Resources["TextControlButtonForegroundPressed"] = fgBrush;
                // Uno Platform resource keys
                slot.Resources["TextBoxDeleteButtonForeground"] = fgBrush;
                slot.Resources["TextBoxDeleteButtonForegroundPointerOver"] = fgBrush;
                slot.Resources["TextBoxDeleteButtonForegroundPressed"] = fgBrush;
                slot.Resources["TextBoxDeleteButtonForegroundFocused"] = fgBrush;
            }
        }

        #endregion
    }
}

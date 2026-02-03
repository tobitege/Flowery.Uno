using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Flowery.Helpers;
using Flowery.Localization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Flowery.Theming;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// A control that displays a numeric value with a smooth scrolling animation for individual digits.
    /// Inspired by SmoothUI's Number Flow.
    /// </summary>
    public partial class DaisyNumberFlow : DaisyBaseContentControl
    {
        private static string GetDefaultAccessibleText() => FloweryLocalization.GetStringInternal("Accessibility_NumberFlow", "Number Flow");

        private Border? _outerContainer;
        private StackPanel? _rootRow;
        private TextBlock? _prefixText;
        private StackPanel? _partsPanel;
        private TextBlock? _suffixText;
        private StackPanel? _controlButtons;
        private RepeatButton? _incrementButton;
        private RepeatButton? _decrementButton;
        private Viewbox? _incrementIconViewbox;
        private Viewbox? _decrementIconViewbox;
        private Microsoft.UI.Xaml.Shapes.Path? _incrementIconPath;
        private Microsoft.UI.Xaml.Shapes.Path? _decrementIconPath;

        private readonly List<PartElement> _parts = [];
        private string _previousFormatted = string.Empty;
        private decimal? _previousValue;
        public DaisyNumberFlow()
        {
            DefaultStyleKey = typeof(DaisyNumberFlow);

            IncrementCommand = new SimpleCommand(_ => IncrementSelected(), _ => CanIncrement);
            DecrementCommand = new SimpleCommand(_ => DecrementSelected(), _ => CanDecrement);

            IsTabStop = true;
            TabIndex = 0;

            AutomationProperties.SetName(this, GetDefaultAccessibleText());
        }

        #region Dependency Properties

        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(decimal?),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(0m, OnValueOrFormatChanged));

        /// <summary>
        /// Gets or sets the numeric value to display.
        /// </summary>
        public decimal? Value
        {
            get => (decimal?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty FormatStringProperty =
            DependencyProperty.Register(
                nameof(FormatString),
                typeof(string),
                typeof(DaisyNumberFlow),
                new PropertyMetadata("N0", OnValueOrFormatChanged));

        public string FormatString
        {
            get => (string)GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(TimeSpan),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(TimeSpan.FromMilliseconds(200)));

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty EasingProperty =
            DependencyProperty.Register(
                nameof(Easing),
                typeof(EasingFunctionBase),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(new CubicEase { EasingMode = EasingMode.EaseOut }));

        public EasingFunctionBase? Easing
        {
            get => (EasingFunctionBase?)GetValue(EasingProperty);
            set => SetValue(EasingProperty, value);
        }

        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register(
                nameof(Prefix),
                typeof(string),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(null, OnLayoutOrValueConfigChanged));

        public string? Prefix
        {
            get => (string?)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register(
                nameof(Suffix),
                typeof(string),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(null, OnLayoutOrValueConfigChanged));

        public string? Suffix
        {
            get => (string?)GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        public static readonly DependencyProperty CultureProperty =
            DependencyProperty.Register(
                nameof(Culture),
                typeof(CultureInfo),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(CultureInfo.InvariantCulture, OnValueOrFormatChanged));

        public CultureInfo Culture
        {
            get => (CultureInfo)GetValue(CultureProperty);
            set => SetValue(CultureProperty, value);
        }

        public static readonly DependencyProperty ShowDigitBoxesProperty =
            DependencyProperty.Register(
                nameof(ShowDigitBoxes),
                typeof(bool),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(false, OnLayoutOrValueConfigChanged));

        public bool ShowDigitBoxes
        {
            get => (bool)GetValue(ShowDigitBoxesProperty);
            set => SetValue(ShowDigitBoxesProperty, value);
        }

        public static readonly DependencyProperty ShowControlsProperty =
            DependencyProperty.Register(
                nameof(ShowControls),
                typeof(bool),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(false, OnLayoutOrValueConfigChanged));

        public bool ShowControls
        {
            get => (bool)GetValue(ShowControlsProperty);
            set => SetValue(ShowControlsProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(decimal?),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(null, OnLayoutOrValueConfigChanged));

        public decimal? Minimum
        {
            get => (decimal?)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(decimal?),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(null, OnLayoutOrValueConfigChanged));

        public decimal? Maximum
        {
            get => (decimal?)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(decimal),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(1m));

        public decimal Step
        {
            get => (decimal)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        public static readonly DependencyProperty WrapAroundProperty =
            DependencyProperty.Register(
                nameof(WrapAround),
                typeof(bool),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(false));

        public bool WrapAround
        {
            get => (bool)GetValue(WrapAroundProperty);
            set => SetValue(WrapAroundProperty, value);
        }

        public static readonly DependencyProperty RepeatIntervalProperty =
            DependencyProperty.Register(
                nameof(RepeatInterval),
                typeof(TimeSpan),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(TimeSpan.FromMilliseconds(100)));

        public TimeSpan RepeatInterval
        {
            get => (TimeSpan)GetValue(RepeatIntervalProperty);
            set => SetValue(RepeatIntervalProperty, value);
        }

        public static readonly DependencyProperty RepeatDelayProperty =
            DependencyProperty.Register(
                nameof(RepeatDelay),
                typeof(TimeSpan),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(TimeSpan.FromMilliseconds(400)));

        public TimeSpan RepeatDelay
        {
            get => (TimeSpan)GetValue(RepeatDelayProperty);
            set => SetValue(RepeatDelayProperty, value);
        }

        public static readonly DependencyProperty RepeatRampInitialIntervalProperty =
            DependencyProperty.Register(
                nameof(RepeatRampInitialInterval),
                typeof(TimeSpan),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(TimeSpan.FromMilliseconds(320)));

        /// <summary>
        /// Gets or sets the first repeat interval after the initial delay. Larger values create a slower start before speeding up to the steady repeat interval.
        /// </summary>
        public TimeSpan RepeatRampInitialInterval
        {
            get => (TimeSpan)GetValue(RepeatRampInitialIntervalProperty);
            set => SetValue(RepeatRampInitialIntervalProperty, value);
        }

        public static readonly DependencyProperty RepeatRampStepsProperty =
            DependencyProperty.Register(
                nameof(RepeatRampSteps),
                typeof(int),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(3));

        /// <summary>
        /// Gets or sets how many ramped repeat intervals are used before settling on the steady repeat interval.
        /// </summary>
        public int RepeatRampSteps
        {
            get => (int)GetValue(RepeatRampStepsProperty);
            set => SetValue(RepeatRampStepsProperty, value);
        }

        public static readonly DependencyProperty AllowDigitSelectionProperty =
            DependencyProperty.Register(
                nameof(AllowDigitSelection),
                typeof(bool),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(false, OnLayoutOrValueConfigChanged));

        public bool AllowDigitSelection
        {
            get => (bool)GetValue(AllowDigitSelectionProperty);
            set => SetValue(AllowDigitSelectionProperty, value);
        }

        public static readonly DependencyProperty SelectedDigitIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedDigitIndex),
                typeof(int?),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(null, OnLayoutOrValueConfigChanged));

        public int? SelectedDigitIndex
        {
            get => (int?)GetValue(SelectedDigitIndexProperty);
            set => SetValue(SelectedDigitIndexProperty, value);
        }

        public static readonly DependencyProperty CanIncrementValueProperty =
            DependencyProperty.Register(
                nameof(CanIncrementValue),
                typeof(bool),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(true));

        public bool CanIncrementValue
        {
            get => (bool)GetValue(CanIncrementValueProperty);
            private set => SetValue(CanIncrementValueProperty, value);
        }

        public static readonly DependencyProperty CanDecrementValueProperty =
            DependencyProperty.Register(
                nameof(CanDecrementValue),
                typeof(bool),
                typeof(DaisyNumberFlow),
                new PropertyMetadata(true));

        public bool CanDecrementValue
        {
            get => (bool)GetValue(CanDecrementValueProperty);
            private set => SetValue(CanDecrementValueProperty, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to increment the value.
        /// </summary>
        public ICommand IncrementCommand { get; }

        /// <summary>
        /// Command to decrement the value.
        /// </summary>
        public ICommand DecrementCommand { get; }

        private bool CanIncrement =>
            WrapAround
                ? (Minimum.HasValue && Maximum.HasValue) || !Maximum.HasValue || (Value ?? 0) < Maximum.Value
                : !Maximum.HasValue || (Value ?? 0) < Maximum.Value;

        private bool CanDecrement =>
            WrapAround
                ? (Minimum.HasValue && Maximum.HasValue) || !Minimum.HasValue || (Value ?? 0) > Minimum.Value
                : !Minimum.HasValue || (Value ?? 0) > Minimum.Value;

        private void UpdateCanExecuteStates()
        {
            CanIncrementValue = CanIncrement;
            CanDecrementValue = CanDecrement;
            (IncrementCommand as SimpleCommand)?.RaiseCanExecuteChanged();
            (DecrementCommand as SimpleCommand)?.RaiseCanExecuteChanged();

            if (_incrementButton != null)
                _incrementButton.IsEnabled = CanIncrementValue;
            if (_decrementButton != null)
                _decrementButton.IsEnabled = CanDecrementValue;
        }

        #endregion

        #region Lifecycle

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplyAll();
            UpdateParts(forceReset: true);
            UpdateCanExecuteStates();

            KeyDown += OnKeyDown;
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            KeyDown -= OnKeyDown;
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            if (_outerContainer != null)
                return;

            _prefixText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };

            _partsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _suffixText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };

            _controlButtons = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Create scalable chevron icons using Viewbox (same pattern as DaisyNumericUpDown)
            _incrementIconPath = FloweryPathHelpers.CreateChevronUp(size: 12, strokeThickness: 2);
            _incrementIconViewbox = new Viewbox
            {
                Child = _incrementIconPath,
                Stretch = Stretch.Uniform
            };

            _decrementIconPath = FloweryPathHelpers.CreateChevronDown(size: 12, strokeThickness: 2);
            _decrementIconViewbox = new Viewbox
            {
                Child = _decrementIconPath,
                Stretch = Stretch.Uniform
            };

            // RepeatButton handles repeat-on-hold natively across all platforms
            _incrementButton = new RepeatButton
            {
                Content = _incrementIconViewbox,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                MinWidth = 0,
                MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Delay = (int)RepeatDelay.TotalMilliseconds,
                Interval = (int)RepeatInterval.TotalMilliseconds,
                IsTabStop = false
            };
            _decrementButton = new RepeatButton
            {
                Content = _decrementIconViewbox,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                MinWidth = 0,
                MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Delay = (int)RepeatDelay.TotalMilliseconds,
                Interval = (int)RepeatInterval.TotalMilliseconds,
                IsTabStop = false
            };

            _incrementButton.Click += (_, _) => IncrementSelected();
            _decrementButton.Click += (_, _) => DecrementSelected();

            _controlButtons.Children.Add(_incrementButton);
            _controlButtons.Children.Add(_decrementButton);

            _rootRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            _rootRow.Children.Add(_prefixText);
            _rootRow.Children.Add(_partsPanel);
            _rootRow.Children.Add(_suffixText);
            _rootRow.Children.Add(_controlButtons);

            _outerContainer = new Border
            {
                Child = _rootRow
            };

            Content = _outerContainer;

            PointerPressed += OnPointerPressed;
            GotFocus += OnGotFocus;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_outerContainer == null || _rootRow == null || _prefixText == null || _suffixText == null || _controlButtons == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            ApplyContainer(resources);
            ApplyTypography(resources);
            ApplyControlsSizing(resources);
            ApplyPartSizing(resources);
            ApplyPrefixSuffix();
            ApplyControlVisibility(resources);

            // Keep parts in sync with size/theme-dependent tokens and flags like ShowDigitBoxes.
            foreach (var part in _parts)
            {
                part.ApplySizing();
                part.ApplyTypography();
                part.ApplyDigitBoxVisibility();
            }

            UpdateSelectedDigitVisuals();
        }

        private void ApplyContainer(ResourceDictionary? resources)
        {
            if (_outerContainer == null)
                return;

            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            if (ShowControls)
            {
                _outerContainer.Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
                _outerContainer.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
                _outerContainer.BorderThickness = resources != null
                    ? DaisyResourceLookup.GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1))
                    : new Thickness(1);
                _outerContainer.Padding = resources != null
                    ? DaisyResourceLookup.GetThickness(resources, "LargeDisplayContainerPadding", new Thickness(16))
                    : new Thickness(16);
                _outerContainer.CornerRadius = resources != null
                    ? DaisyResourceLookup.GetCornerRadius(resources, "LargeDisplayContainerCornerRadius", new CornerRadius(16))
                    : new CornerRadius(16);
            }
            else
            {
                _outerContainer.Background = transparent;
                _outerContainer.BorderThickness = new Thickness(0);
                _outerContainer.Padding = new Thickness(0);
                _outerContainer.CornerRadius = new CornerRadius(0);
            }
        }

        private void ApplyTypography(ResourceDictionary? resources)
        {
            var foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");

            if (_prefixText != null) _prefixText.Foreground = foreground;
            if (_suffixText != null) _suffixText.Foreground = foreground;

            // Default typography matches Avalonia theme defaults unless overridden by user styling.
            if (resources != null && ReadLocalValue(FontSizeProperty) == DependencyProperty.UnsetValue)
            {
                FontSize = DaisyResourceLookup.GetDouble(resources, "LargeDisplayFontSize", 36d);
            }

            if (ReadLocalValue(FontFamilyProperty) == DependencyProperty.UnsetValue || FontFamily == null)
                FontFamily = new FontFamily("Consolas");

            if (ReadLocalValue(FontWeightProperty) == DependencyProperty.UnsetValue)
                FontWeight = FontWeights.SemiBold;
        }

        private void ApplyControlsSizing(ResourceDictionary? resources)
        {
            if (_incrementButton == null || _decrementButton == null)
                return;

            if (resources == null)
                return;

            var buttonSize = DaisyResourceLookup.GetDouble(resources, "LargeDisplayButtonSize", 42d);
            var spacing = DaisyResourceLookup.GetDouble(resources, "LargeDisplayButtonSpacing", 2d);

            // Scale chevron icons proportionally to button size
            var iconSize = Math.Max(12, buttonSize * 0.35);

            if (_controlButtons != null)
                _controlButtons.Spacing = spacing;

            // Update RepeatButton delay/interval from dependency properties
            _incrementButton.Delay = (int)RepeatDelay.TotalMilliseconds;
            _incrementButton.Interval = (int)RepeatInterval.TotalMilliseconds;
            _decrementButton.Delay = (int)RepeatDelay.TotalMilliseconds;
            _decrementButton.Interval = (int)RepeatInterval.TotalMilliseconds;

            foreach (var b in new[] { _incrementButton, _decrementButton })
            {
                b.Width = buttonSize;
                b.Height = buttonSize;
                b.MinWidth = buttonSize;
                b.MinHeight = buttonSize;
            }

            // Apply icon sizing to viewboxes
            if (_incrementIconViewbox != null)
            {
                _incrementIconViewbox.Width = iconSize;
                _incrementIconViewbox.Height = iconSize;
            }
            if (_decrementIconViewbox != null)
            {
                _decrementIconViewbox.Width = iconSize;
                _decrementIconViewbox.Height = iconSize;
            }

            // Apply chevron foreground color
            var iconBrush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            if (_incrementIconPath != null)
                _incrementIconPath.Stroke = iconBrush;
            if (_decrementIconPath != null)
                _decrementIconPath.Stroke = iconBrush;
        }

        private void ApplyPartSizing(ResourceDictionary? resources)
        {
            if (_partsPanel == null || resources == null)
                return;

            _partsPanel.Spacing = DaisyResourceLookup.GetDouble(resources, "LargeDisplayElementSpacing", 4d);
        }

        private void ApplyPrefixSuffix()
        {
            if (_prefixText != null)
            {
                _prefixText.Text = Prefix ?? string.Empty;
                _prefixText.Visibility = string.IsNullOrEmpty(Prefix) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (_suffixText != null)
            {
                _suffixText.Text = Suffix ?? string.Empty;
                _suffixText.Visibility = string.IsNullOrEmpty(Suffix) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void ApplyControlVisibility(ResourceDictionary? resources)
        {
            if (_controlButtons == null)
                return;

            _controlButtons.Visibility = ShowControls ? Visibility.Visible : Visibility.Collapsed;

            if (resources != null && _rootRow != null)
            {
                _rootRow.Spacing = ShowControls
                    ? 8
                    : 0;
            }
        }

        #endregion

        #region Value / Parts

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyNumberFlow nf)
            {
                AutomationProperties.SetName(nf, e.NewValue as string ?? GetDefaultAccessibleText());
            }
        }

        private static void OnValueOrFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyNumberFlow nf)
            {
                nf.UpdateParts(forceReset: e.Property == FormatStringProperty || e.Property == CultureProperty);
                nf.UpdateCanExecuteStates();
            }
        }

        private static void OnLayoutOrValueConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyNumberFlow nf)
            {
                nf.ApplyAll();
                if (e.Property == SelectedDigitIndexProperty || e.Property == AllowDigitSelectionProperty)
                {
                    nf.UpdateSelectedDigitVisuals();
                }
            }
        }

        private void UpdateParts(bool forceReset)
        {
            if (_partsPanel == null)
                return;

            if (Value == null)
            {
                _previousFormatted = string.Empty;
                _previousValue = null;
                _parts.Clear();
                _partsPanel.Children.Clear();
                return;
            }

            var newFormatted = Value.Value.ToString(FormatString, Culture);
            var isIncreasing = _previousValue.HasValue && Value.Value > _previousValue.Value;

            if (forceReset || _previousFormatted.Length == 0)
            {
                RebuildParts(newFormatted, fadeIn: false);
                _previousFormatted = newFormatted;
                _previousValue = Value;
                return;
            }

            if (_previousFormatted.Length != newFormatted.Length)
            {
                RebuildParts(newFormatted, fadeIn: true);
                _previousFormatted = newFormatted;
                _previousValue = Value;
                return;
            }

            for (int i = 0; i < _parts.Count && i < newFormatted.Length; i++)
            {
                _parts[i].Update(newFormatted[i], isIncreasing, Duration, Easing);
            }

            AssignDigitIndices();
            UpdateSelectedDigitVisuals();

            _previousFormatted = newFormatted;
            _previousValue = Value;
        }

        private void RebuildParts(string formatted, bool fadeIn)
        {
            if (_partsPanel == null)
                return;

            foreach (var p in _parts)
            {
                p.Dispose();
            }
            _parts.Clear();
            _partsPanel.Children.Clear();

            foreach (var c in formatted)
            {
                var part = new PartElement(this, c, char.IsDigit(c));
                _parts.Add(part);
                _partsPanel.Children.Add(part.Container);

                if (fadeIn)
                    part.StartFadeIn(Duration, Easing);
            }

            AssignDigitIndices();
            UpdateSelectedDigitVisuals();
        }

        private void AssignDigitIndices()
        {
            var digitIndex = 0;
            for (int i = _parts.Count - 1; i >= 0; i--)
            {
                if (_parts[i].IsDigit)
                {
                    _parts[i].DigitIndex = digitIndex++;
                }
                else
                {
                    _parts[i].DigitIndex = -1;
                }
            }
        }

        private void UpdateSelectedDigitVisuals()
        {
            if (!AllowDigitSelection)
            {
                foreach (var p in _parts)
                {
                    p.SetSelected(false);
                }
                return;
            }

            var digitParts = _parts.Where(p => p.IsDigit).Reverse().ToList();
            for (int i = 0; i < digitParts.Count; i++)
            {
                digitParts[i].SetSelected(SelectedDigitIndex == i);
            }
        }

        #endregion

        #region Increment / Decrement

        private void IncrementSelected()
        {
            if (AllowDigitSelection && SelectedDigitIndex.HasValue)
            {
                IncrementDigitAtPosition(SelectedDigitIndex.Value);
            }
            else
            {
                Increment();
            }
        }

        private void DecrementSelected()
        {
            if (AllowDigitSelection && SelectedDigitIndex.HasValue)
            {
                DecrementDigitAtPosition(SelectedDigitIndex.Value);
            }
            else
            {
                Decrement();
            }
        }

        private void IncrementDigitAtPosition(int position)
        {
            var currentValue = Value ?? 0;
            var divisor = (decimal)Math.Pow(10, position);
            var newValue = currentValue + divisor;
            newValue = ApplyBoundsWithWrap(newValue, isIncrement: true);
            Value = newValue;
        }

        private void DecrementDigitAtPosition(int position)
        {
            var currentValue = Value ?? 0;
            var divisor = (decimal)Math.Pow(10, position);
            var newValue = currentValue - divisor;
            newValue = ApplyBoundsWithWrap(newValue, isIncrement: false);
            Value = newValue;
        }

        private decimal ApplyBoundsWithWrap(decimal newValue, bool isIncrement)
        {
            if (isIncrement && Maximum.HasValue && newValue > Maximum.Value)
            {
                if (WrapAround && Minimum.HasValue)
                    return Minimum.Value;
                return Maximum.Value;
            }

            if (!isIncrement && Minimum.HasValue && newValue < Minimum.Value)
            {
                if (WrapAround && Maximum.HasValue)
                    return Maximum.Value;
                return Minimum.Value;
            }

            return newValue;
        }

        public void Increment()
        {
            var newValue = (Value ?? 0) + Step;
            newValue = ApplyBoundsWithWrap(newValue, isIncrement: true);
            Value = newValue;
        }

        public void Decrement()
        {
            var newValue = (Value ?? 0) - Step;
            newValue = ApplyBoundsWithWrap(newValue, isIncrement: false);
            Value = newValue;
        }

        #endregion

        #region Input Handling (Digit Selection + Keyboard)

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!AllowDigitSelection) return;
            var point = e.GetCurrentPoint(this);
            // Check for mouse left button OR touch contact (for Android/touch support)
            if (!point.Properties.IsLeftButtonPressed && !point.IsInContact) return;

            var source = e.OriginalSource as FrameworkElement;
            while (source != null && source != this)
            {
                if (source.Tag is int digitIndex && digitIndex >= 0)
                {
                    SelectDigit(digitIndex);
                    e.Handled = true;
                    return;
                }

                source = source.Parent as FrameworkElement;
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (AllowDigitSelection && !SelectedDigitIndex.HasValue)
            {
                SelectDigit(0);
            }
        }

        public void SelectDigit(int index)
        {
            if (!AllowDigitSelection) return;
            SelectedDigitIndex = index;
            UpdateSelectedDigitVisuals();
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Handled) return;
            if (!AllowDigitSelection) return;

            switch (e.Key)
            {
                case VirtualKey.Left:
                    MoveDigitSelection(1);
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    MoveDigitSelection(-1);
                    e.Handled = true;
                    break;
                case VirtualKey.Up:
                    if (SelectedDigitIndex.HasValue && CanIncrement)
                    {
                        IncrementDigitAtPosition(SelectedDigitIndex.Value);
                        e.Handled = true;
                    }
                    break;
                case VirtualKey.Down:
                    if (SelectedDigitIndex.HasValue && CanDecrement)
                    {
                        DecrementDigitAtPosition(SelectedDigitIndex.Value);
                        e.Handled = true;
                    }
                    break;
                case VirtualKey.Number0:
                case VirtualKey.NumberPad0:
                case VirtualKey.Number1:
                case VirtualKey.NumberPad1:
                case VirtualKey.Number2:
                case VirtualKey.NumberPad2:
                case VirtualKey.Number3:
                case VirtualKey.NumberPad3:
                case VirtualKey.Number4:
                case VirtualKey.NumberPad4:
                case VirtualKey.Number5:
                case VirtualKey.NumberPad5:
                case VirtualKey.Number6:
                case VirtualKey.NumberPad6:
                case VirtualKey.Number7:
                case VirtualKey.NumberPad7:
                case VirtualKey.Number8:
                case VirtualKey.NumberPad8:
                case VirtualKey.Number9:
                case VirtualKey.NumberPad9:
                    var digit = KeyToDigit(e.Key);
                    if (digit >= 0)
                    {
                        SetSelectedDigitTo(digit);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void MoveDigitSelection(int offset)
        {
            var digitCount = _parts.Count(p => p.IsDigit);
            if (digitCount == 0) return;

            var currentIndex = SelectedDigitIndex ?? 0;
            var newIndex = currentIndex + offset;

            if (newIndex < 0) newIndex = 0;
            if (newIndex >= digitCount) newIndex = digitCount - 1;

            SelectDigit(newIndex);
        }

        private void SetSelectedDigitTo(int newDigitValue)
        {
            if (!SelectedDigitIndex.HasValue) return;
            if (newDigitValue < 0 || newDigitValue > 9) return;

            var position = SelectedDigitIndex.Value;
            var currentDigitValue = GetDigitValueAtPosition(position);
            if (currentDigitValue == newDigitValue) return;

            var delta = newDigitValue - currentDigitValue;
            var multiplier = (decimal)Math.Pow(10, position);
            var newValue = (Value ?? 0) + (delta * multiplier);
            newValue = ApplyBoundsWithWrap(newValue, delta > 0);
            Value = newValue;
        }

        private int GetDigitValueAtPosition(int position)
        {
            var currentValue = Math.Abs(Value ?? 0);
            var divisor = (decimal)Math.Pow(10, position);
            return (int)(Math.Floor(currentValue / divisor) % 10);
        }

        private static int KeyToDigit(VirtualKey key)
        {
            return key switch
            {
                VirtualKey.Number0 => 0,
                VirtualKey.NumberPad0 => 0,
                VirtualKey.Number1 => 1,
                VirtualKey.NumberPad1 => 1,
                VirtualKey.Number2 => 2,
                VirtualKey.NumberPad2 => 2,
                VirtualKey.Number3 => 3,
                VirtualKey.NumberPad3 => 3,
                VirtualKey.Number4 => 4,
                VirtualKey.NumberPad4 => 4,
                VirtualKey.Number5 => 5,
                VirtualKey.NumberPad5 => 5,
                VirtualKey.Number6 => 6,
                VirtualKey.NumberPad6 => 6,
                VirtualKey.Number7 => 7,
                VirtualKey.NumberPad7 => 7,
                VirtualKey.Number8 => 8,
                VirtualKey.NumberPad8 => 8,
                VirtualKey.Number9 => 9,
                VirtualKey.NumberPad9 => 9,
                _ => -1
            };
        }

        #endregion

        #region Part Element

        private sealed partial class PartElement : IDisposable
        {
            private readonly DaisyNumberFlow _owner;
            private CancellationTokenSource? _cts;

            public Border Container { get; }
            public bool IsDigit { get; }
            public int DigitIndex { get; set; } = -1;

            private readonly Grid _grid;
            private readonly Border _digitBox;
            private readonly Border _selectionIndicator;
            private readonly Grid _clipPanel;
            private readonly TextBlock _prevText;
            private readonly TextBlock _currentText;
            private readonly TranslateTransform _prevTransform;
            private readonly TranslateTransform _currentTransform;

            private char _currentChar;
            private char _previousChar;

            public PartElement(DaisyNumberFlow owner, char initialChar, bool isDigit)
            {
                _owner = owner;
                IsDigit = isDigit;
                _currentChar = initialChar;
                _previousChar = initialChar;

                _prevTransform = new TranslateTransform();
                _currentTransform = new TranslateTransform();

                _digitBox = new Border
                {
                    Background = DaisyResourceLookup.GetBrush("DaisyNeutralBrush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 1
                };

                _selectionIndicator = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    BorderBrush = DaisyResourceLookup.GetBrush("DaisyAccentBrush"),
                    BorderThickness = new Thickness(0, 0, 0, 3),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Visibility = Visibility.Collapsed
                };

                _prevText = new TextBlock
                {
                    Text = _previousChar.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransform = _prevTransform,
                    Opacity = 0
                };

                _currentText = new TextBlock
                {
                    Text = _currentChar.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransform = _currentTransform,
                    Opacity = 1
                };

                _clipPanel = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                _clipPanel.Children.Add(_prevText);
                _clipPanel.Children.Add(_currentText);
                _clipPanel.SizeChanged += (s, e) =>
                {
                    _clipPanel.Clip = new RectangleGeometry { Rect = new Rect(0, 0, _clipPanel.ActualWidth, _clipPanel.ActualHeight) };
                };

                _grid = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                _grid.Children.Add(_digitBox);
                _grid.Children.Add(_selectionIndicator);
                _grid.Children.Add(_clipPanel);

                Container = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    Child = _grid,
                    Tag = -1
                };

                ApplySizing();
                ApplyTypography();
                ApplyDigitBoxVisibility();
            }

            public void Dispose()
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }

            public void ApplySizing()
            {
                var resources = Application.Current?.Resources;
                if (resources == null)
                    return;

                var width = DaisyResourceLookup.GetDouble(resources, "LargeDisplayElementWidth", 58d);
                var height = DaisyResourceLookup.GetDouble(resources, "LargeDisplayElementHeight", 86d);
                var radius = DaisyResourceLookup.GetCornerRadius(resources, "LargeDisplayElementCornerRadius", new CornerRadius(12));

                _digitBox.Width = width;
                _digitBox.Height = height;
                _digitBox.CornerRadius = radius;

                _selectionIndicator.Width = width;
                _selectionIndicator.Height = height;
                _selectionIndicator.CornerRadius = radius;

                _clipPanel.Width = width;
                _clipPanel.Height = _owner.FontSize * 1.4;
            }

            public void ApplyTypography()
            {
                var fg = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
                if (_owner.ShowDigitBoxes)
                {
                    if (_digitBox.Background is SolidColorBrush boxBrush)
                    {
                        var (_, content) = DaisyResourceLookup.GetPaletteBrushesForColor(boxBrush.Color);
                        if (content != null)
                        {
                            fg = content;
                        }
                    }
                    else
                    {
                        fg = DaisyResourceLookup.GetBrush("DaisyNeutralContentBrush");
                    }
                }
                foreach (var tb in new[] { _prevText, _currentText })
                {
                    tb.FontSize = _owner.FontSize;
                    tb.FontWeight = _owner.FontWeight;
                    tb.FontFamily = _owner.FontFamily;
                    tb.Foreground = fg;
                    tb.TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center;
                }
            }

            public void ApplyDigitBoxVisibility()
            {
                _digitBox.Visibility = _owner.ShowDigitBoxes ? Visibility.Visible : Visibility.Collapsed;
            }

            public void SetSelected(bool selected)
            {
                Container.Tag = IsDigit ? DigitIndex : -1;
                _selectionIndicator.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;

                ApplyDigitBoxVisibility();
                ApplyTypography();
                ApplySizing();
            }

            public void StartFadeIn(TimeSpan duration, EasingFunctionBase? easing)
            {
                Enqueue(() =>
                {
                    _grid.Opacity = 0;
                    var easingSpec = CreateEasingSpec(easing);
                    Observe(FadeAsync(0, 1, duration, easingSpec, CancellationToken.None));
                });
            }

            public void Update(char newChar, bool isIncreasing, TimeSpan duration, EasingFunctionBase? easing)
            {
                Enqueue(() =>
                {
                    if (_currentChar == newChar)
                        return;

                    _cts?.Cancel();
                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();

                    _previousChar = _currentChar;
                    _currentChar = newChar;

                    _prevText.Text = _previousChar.ToString();
                    _currentText.Text = _currentChar.ToString();

                    _prevText.Opacity = 1;
                    _currentText.Opacity = 1;

                    var distance = _clipPanel.ActualHeight > 0 ? _clipPanel.ActualHeight : _owner.FontSize * 1.4;

                    _prevTransform.Y = 0;
                    _currentTransform.Y = isIncreasing ? distance : -distance;

                    var easingSpec = CreateEasingSpec(easing);
                    Observe(AnimateSlideAsync(isIncreasing, distance, duration, easingSpec, _cts.Token));
                });
            }

            private async Task AnimateSlideAsync(bool isIncreasing, double distance, TimeSpan duration, EasingSpec easing, CancellationToken token)
            {
                var start = DateTime.UtcNow;
                while (DateTime.UtcNow - start < duration)
                {
                    if (token.IsCancellationRequested)
                        return;

                    var t = (DateTime.UtcNow - start).TotalMilliseconds / Math.Max(1, duration.TotalMilliseconds);
                    if (t > 1) t = 1;
                    var eased = EvaluateEasing(easing, t);

                    _currentTransform.Y = (isIncreasing ? distance : -distance) * (1 - eased);
                    _prevTransform.Y = (isIncreasing ? -distance : distance) * eased;
                    _prevText.Opacity = 1 - eased;
                    _currentText.Opacity = eased;

                    // Stay on UI thread to ensure UI updates work correctly in Uno
                    try
                    {
                        await Task.Delay(16, token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                    if (token.IsCancellationRequested)
                        return;
                }

                if (!token.IsCancellationRequested)
                {
                    _currentTransform.Y = 0;
                    _prevTransform.Y = 0;
                    _prevText.Opacity = 0;
                    _currentText.Opacity = 1;
                }
            }

            private async Task FadeAsync(double from, double to, TimeSpan duration, EasingSpec easing, CancellationToken token)
            {
                var start = DateTime.UtcNow;
                while (DateTime.UtcNow - start < duration)
                {
                    if (token.IsCancellationRequested)
                        return;

                    var t = (DateTime.UtcNow - start).TotalMilliseconds / Math.Max(1, duration.TotalMilliseconds);
                    if (t > 1) t = 1;
                    var eased = EvaluateEasing(easing, t);

                    _grid.Opacity = from + (to - from) * eased;
                    // Stay on UI thread to ensure UI updates work correctly in Uno
                    try
                    {
                        await Task.Delay(16, token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                    if (token.IsCancellationRequested)
                        return;
                }

                if (!token.IsCancellationRequested)
                    _grid.Opacity = to;
            }

            private enum EasingKind
            {
                DefaultCubicOut,
                Cubic,
                Quadratic,
                Quartic,
                Quintic,
                Sine,
                Circle,
                Exponential,
                Power
            }

            private readonly record struct EasingSpec(EasingKind Kind, EasingMode Mode, double Power);

            private static EasingSpec CreateEasingSpec(EasingFunctionBase? easing)
            {
                if (easing == null)
                    return new EasingSpec(EasingKind.DefaultCubicOut, EasingMode.EaseOut, Power: 0);

                return easing switch
                {
                    CubicEase ce => new EasingSpec(EasingKind.Cubic, ce.EasingMode, Power: 0),
                    QuadraticEase qe => new EasingSpec(EasingKind.Quadratic, qe.EasingMode, Power: 0),
                    QuarticEase qe => new EasingSpec(EasingKind.Quartic, qe.EasingMode, Power: 0),
                    QuinticEase qe => new EasingSpec(EasingKind.Quintic, qe.EasingMode, Power: 0),
                    SineEase se => new EasingSpec(EasingKind.Sine, se.EasingMode, Power: 0),
                    CircleEase ce2 => new EasingSpec(EasingKind.Circle, ce2.EasingMode, Power: 0),
                    ExponentialEase ee => new EasingSpec(EasingKind.Exponential, ee.EasingMode, Power: 0),
                    PowerEase pe => new EasingSpec(EasingKind.Power, pe.EasingMode, pe.Power),
                    _ => new EasingSpec(EasingKind.DefaultCubicOut, EasingMode.EaseOut, Power: 0)
                };
            }

            private static double EvaluateEasing(EasingSpec easing, double t)
            {
                if (t <= 0) return 0;
                if (t >= 1) return 1;

                static double ApplyMode(EasingMode mode, Func<double, double> easeIn, double x)
                {
                    return mode switch
                    {
                        EasingMode.EaseIn => easeIn(x),
                        EasingMode.EaseOut => 1 - easeIn(1 - x),
                        EasingMode.EaseInOut => x < 0.5
                            ? easeIn(x * 2) / 2
                            : 1 - (easeIn((1 - x) * 2) / 2),
                        _ => 1 - Math.Pow(1 - x, 3)
                    };
                }

                return easing.Kind switch
                {
                    EasingKind.Cubic => ApplyMode(easing.Mode, x => x * x * x, t),
                    EasingKind.Quadratic => ApplyMode(easing.Mode, x => x * x, t),
                    EasingKind.Quartic => ApplyMode(easing.Mode, x => x * x * x * x, t),
                    EasingKind.Quintic => ApplyMode(easing.Mode, x => x * x * x * x * x, t),
                    EasingKind.Sine => ApplyMode(easing.Mode, x => 1 - Math.Cos((x * Math.PI) / 2), t),
                    EasingKind.Circle => ApplyMode(easing.Mode, x => 1 - Math.Sqrt(1 - (x * x)), t),
                    EasingKind.Exponential => ApplyMode(easing.Mode, x => x <= 0 ? 0 : Math.Pow(2, 10 * (x - 1)), t),
                    EasingKind.Power => ApplyMode(easing.Mode, x => Math.Pow(x, Math.Max(1, easing.Power)), t),
                    _ => 1 - Math.Pow(1 - t, 3)
                };
            }

            private void Enqueue(Action action)
            {
                var dispatcherQueue = _owner.DispatcherQueue;
                if (dispatcherQueue == null || dispatcherQueue.HasThreadAccess)
                {
                    action();
                    return;
                }

                dispatcherQueue.TryEnqueue(() => action());
            }

            private static void Observe(Task task)
            {
                _ = task.ContinueWith(
                    static t => _ = t.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        #endregion
    }
}

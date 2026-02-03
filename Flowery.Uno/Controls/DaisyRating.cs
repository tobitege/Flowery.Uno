using System;
using System.Collections.Generic;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// A star rating control styled after DaisyUI's Rating component.
    /// Supports click/drag to rate and different precision modes.
    /// </summary>
    public partial class DaisyRating : DaisyBaseContentControl
    {
        private StackPanel? _starsPanel;
        private readonly List<Microsoft.UI.Xaml.Shapes.Path> _stars = [];
        private bool _isDragging;

        public DaisyRating()
        {
            DefaultStyleKey = typeof(DaisyRating);
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += OnPointerCaptureLost;
            IsEnabledChanged += OnIsEnabledChanged;
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyRating),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(DaisyRating),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, ValueCoercion.Clamp(value, 0, Maximum));
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(int),
                typeof(DaisyRating),
                new PropertyMetadata(5, OnStarCountChanged));

        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, ValueCoercion.Count(value, min: 1, max: 100));
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(DaisyRating),
                new PropertyMetadata(false, OnReadOnlyChanged));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyRating),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register(
                nameof(Precision),
                typeof(RatingPrecision),
                typeof(DaisyRating),
                new PropertyMetadata(RatingPrecision.Full));

        public RatingPrecision Precision
        {
            get => (RatingPrecision)GetValue(PrecisionProperty);
            set => SetValue(PrecisionProperty, value);
        }

        public static readonly DependencyProperty FilledColorProperty =
            DependencyProperty.Register(
                nameof(FilledColor),
                typeof(DaisyColor),
                typeof(DaisyRating),
                new PropertyMetadata(DaisyColor.Warning, OnAppearanceChanged));

        public DaisyColor FilledColor
        {
            get => (DaisyColor)GetValue(FilledColorProperty);
            set => SetValue(FilledColorProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRating r) r.ApplyAll();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRating r)
            {
                r.UpdateStarFills();
                r.UpdateAutomationProperties();
            }
        }

        private static void OnStarCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRating r)
            {
                r.BuildStars();
                r.ApplyAll();
                r.UpdateAutomationProperties();
            }
        }

        private static void OnReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRating rating)
            {
                rating.UpdateInteractionState();
            }
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRating rating)
            {
                rating.UpdateAutomationProperties();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_starsPanel != null)
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

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateInteractionState();
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || IsReadOnly)
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
                    Value = 0;
                    break;
                case VirtualKey.End:
                    Value = Maximum;
                    break;
                case VirtualKey.PageDown:
                    AdjustValue(-GetKeyboardStep() * 5);
                    break;
                case VirtualKey.PageUp:
                    AdjustValue(GetKeyboardStep() * 5);
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

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        #endregion

        #region Pointer Handling

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (IsReadOnly) return;
            _isDragging = CapturePointer(e.Pointer);
            if (_isDragging) UpdateValueFromPointer(e);
            DaisyAccessibility.FocusOnPointer(this);
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (IsReadOnly || !_isDragging) return;
            UpdateValueFromPointer(e);
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                ReleasePointerCapture(e.Pointer);
                _isDragging = false;
            }
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
        }

        private void UpdateValueFromPointer(PointerRoutedEventArgs e)
        {
            if (_starsPanel == null || _stars.Count == 0) return;

            var pos = e.GetCurrentPoint(_starsPanel).Position;
            var panelWidth = _starsPanel.ActualWidth;
            if (panelWidth <= 0) return;

            var percent = ValueCoercion.Ratio(pos.X / panelWidth);
            var rawValue = percent * Maximum;

            Value = SnapValue(rawValue);
        }

        private double SnapValue(double rawValue)
        {
            return Precision switch
            {
                RatingPrecision.Half => Math.Ceiling(rawValue * 2) / 2.0,
                RatingPrecision.Precise => Math.Ceiling(rawValue * 10) / 10.0,
                _ => Math.Ceiling(rawValue)
            };
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            _starsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };

            BuildStars();
            Content = _starsPanel;
        }

        private void BuildStars()
        {
            if (_starsPanel == null) return;

            _starsPanel.Children.Clear();
            _stars.Clear();

            for (int i = 0; i < Maximum; i++)
            {
                var star = new Microsoft.UI.Xaml.Shapes.Path
                {
                    Data = CreateStarGeometry(),
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _stars.Add(star);
                _starsPanel.Children.Add(star);
            }
        }

        private static Geometry CreateStarGeometry()
        {
            // 5-pointed star path
            var pathData = "M12,2 L15.09,8.26 L22,9.27 L17,14.14 L18.18,21.02 L12,17.77 L5.82,21.02 L7,14.14 L2,9.27 L8.91,8.26 Z";
            return (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), pathData);
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            ApplySizing();
            UpdateStarFills();
            UpdateInteractionState();
            UpdateAutomationProperties();
        }

        private void ApplySizing()
        {
            double starSize = DaisyResourceLookup.GetDefaultRatingStarSize(Size);

            foreach (var star in _stars)
            {
                star.Width = starSize;
                star.Height = starSize;
            }

            if (_starsPanel != null)
            {
                _starsPanel.Spacing = DaisyResourceLookup.GetDefaultRatingSpacing(Size);
            }
        }

        private void UpdateStarFills()
        {
            var filledBrushKey = FilledColor switch
            {
                DaisyColor.Primary => "DaisyPrimaryBrush",
                DaisyColor.Secondary => "DaisySecondaryBrush",
                DaisyColor.Accent => "DaisyAccentBrush",
                DaisyColor.Success => "DaisySuccessBrush",
                DaisyColor.Warning => "DaisyWarningBrush",
                DaisyColor.Info => "DaisyInfoBrush",
                DaisyColor.Error => "DaisyErrorBrush",
                _ => "DaisyWarningBrush"
            };

            var filledBrush = DaisyResourceLookup.GetBrush(filledBrushKey);
            var emptyBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            for (int i = 0; i < _stars.Count; i++)
            {
                var starIndex = i + 1;
                if (Value >= starIndex)
                {
                    // Fully filled
                    _stars[i].Fill = filledBrush;
                }
                else if (Value > i && Value < starIndex)
                {
                    // Partially filled
                    var ratio = Value - i;
                    var filledColor = filledBrush is SolidColorBrush scbFilled ? scbFilled.Color : Colors.Transparent;
                    var emptyColor = emptyBrush is SolidColorBrush scbEmpty ? scbEmpty.Color : Colors.Transparent;

                    var partialBrush = new LinearGradientBrush
                    {
                        StartPoint = new Windows.Foundation.Point(0, 0.5),
                        EndPoint = new Windows.Foundation.Point(1, 0.5)
                    };
                    partialBrush.GradientStops.Add(new GradientStop { Color = filledColor, Offset = ratio });
                    partialBrush.GradientStops.Add(new GradientStop { Color = emptyColor, Offset = ratio });
                    _stars[i].Fill = partialBrush;
                }
                else
                {
                    // Empty
                    _stars[i].Fill = emptyBrush;
                }
            }
        }

        private void UpdateInteractionState()
        {
            IsTabStop = !IsReadOnly && IsEnabled;
        }

        private void AdjustValue(double delta)
        {
            var newValue = SnapValue(Value + delta);
            Value = ValueCoercion.Clamp(newValue, 0, Maximum);
        }

        private double GetKeyboardStep()
        {
            return Precision switch
            {
                RatingPrecision.Half => 0.5,
                RatingPrecision.Precise => 0.1,
                _ => 1.0
            };
        }

        private void UpdateAutomationProperties()
        {
            var name = !string.IsNullOrWhiteSpace(AccessibleText)
                ? AccessibleText
                : $"Rating {Value:0.##} of {Maximum}";

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

        #endregion
    }
}

using System;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// A Slider control styled after DaisyUI's Range component.
    /// Supports variant colors and multiple sizes.
    /// </summary>
    public partial class DaisyRange : DaisyBaseContentControl
    {
        private Border? _trackBorder;
        private Border? _fillBorder;
        private Ellipse? _thumb;
        private bool _isDragging;
        public DaisyRange()
        {
            DefaultStyleKey = typeof(DaisyRange);
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += OnPointerCaptureLost;
            KeyDown += OnKeyDown;

            // Make the control focusable for keyboard navigation
            IsTabStop = true;
        }

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyRangeVariant),
                typeof(DaisyRange),
                new PropertyMetadata(DaisyRangeVariant.Default, OnAppearanceChanged));

        public DaisyRangeVariant Variant
        {
            get => (DaisyRangeVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyRange),
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
                typeof(DaisyRange),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, ValueCoercion.Clamp(value, Minimum, Maximum));
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(DaisyRange),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(DaisyRange),
                new PropertyMetadata(100.0, OnValueChanged));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register(
                nameof(ShowProgress),
                typeof(bool),
                typeof(DaisyRange),
                new PropertyMetadata(true, OnAppearanceChanged));

        public bool ShowProgress
        {
            get => (bool)GetValue(ShowProgressProperty);
            set => SetValue(ShowProgressProperty, value);
        }

        public static readonly DependencyProperty StepFrequencyProperty =
            DependencyProperty.Register(
                nameof(StepFrequency),
                typeof(double),
                typeof(DaisyRange),
                new PropertyMetadata(0.0));

        /// <summary>
        /// Gets or sets the value part of a value range that steps should be created for.
        /// For example, if StepFrequency is 5 and range is 0-100, values snap to 0, 5, 10, ... 100.
        /// Set to 0 (default) to disable snapping.
        /// </summary>
        public double StepFrequency
        {
            get => (double)GetValue(StepFrequencyProperty);
            set => SetValue(StepFrequencyProperty, value);
        }

        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(
                nameof(SmallChange),
                typeof(double),
                typeof(DaisyRange),
                new PropertyMetadata(1.0));

        /// <summary>
        /// Gets or sets the value to be added to or subtracted from the Value for small changes.
        /// Used for keyboard arrow key navigation.
        /// </summary>
        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(
                nameof(LargeChange),
                typeof(double),
                typeof(DaisyRange),
                new PropertyMetadata(10.0));

        /// <summary>
        /// Gets or sets the value to be added to or subtracted from the Value for large changes.
        /// Used for Page Up/Page Down keyboard navigation.
        /// </summary>
        public double LargeChange
        {
            get => (double)GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRange r)
            {
                r.ApplyAll();
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRange r)
            {
                r.UpdateThumbPosition();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_trackBorder != null)
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

        #region Pointer Handling

        private void OnPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _isDragging = CapturePointer(e.Pointer);
            if (_isDragging) UpdateValueFromPointer(e);
        }

        private void OnPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_isDragging) UpdateValueFromPointer(e);
        }

        private void OnPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _isDragging = false;
            ReleasePointerCapture(e.Pointer);
        }

        private void OnPointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _isDragging = false;
        }

        private void UpdateValueFromPointer(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_trackBorder == null) return;

            var pos = e.GetCurrentPoint(this).Position;
            var trackWidth = ActualWidth;
            if (trackWidth <= 0) return;

            var percent = ValueCoercion.Ratio(pos.X / trackWidth);
            var rawValue = Minimum + (percent * (Maximum - Minimum));
            Value = SnapToStep(rawValue);
        }

        /// <summary>
        /// Snaps a raw value to the nearest step if StepFrequency is set.
        /// </summary>
        private double SnapToStep(double rawValue)
        {
            if (StepFrequency <= 0)
                return rawValue;

            // Round to nearest step
            var steps = Math.Round((rawValue - Minimum) / StepFrequency);
            var snapped = Minimum + (steps * StepFrequency);

            // Clamp to range
            return ValueCoercion.Clamp(snapped, Minimum, Maximum);
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Home)
            {
                Value = Minimum;
                e.Handled = true;
                return;
            }

            if (e.Key == VirtualKey.End)
            {
                Value = Maximum;
                e.Handled = true;
                return;
            }

            var change = e.Key switch
            {
                VirtualKey.Left or VirtualKey.Down => -SmallChange,
                VirtualKey.Right or VirtualKey.Up => SmallChange,
                VirtualKey.PageDown => -LargeChange,
                VirtualKey.PageUp => LargeChange,
                _ => 0
            };

            if (change == 0)
                return;

            var newValue = SnapToStep(Value + change);
            Value = ValueCoercion.Clamp(newValue, Minimum, Maximum);
            e.Handled = true;
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            var root = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };

            _trackBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            _fillBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            _thumb = new Ellipse
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            root.Children.Add(_trackBorder);
            root.Children.Add(_fillBorder);
            root.Children.Add(_thumb);

            Content = root;
            SizeChanged += (s, e) => UpdateThumbPosition();
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_trackBorder == null || _fillBorder == null || _thumb == null)
                return;

            ApplySizing();
            ApplyColors();
            UpdateThumbPosition();
        }

        private void ApplySizing()
        {
            if (_trackBorder == null || _fillBorder == null || _thumb == null)
                return;

            double trackHeight = DaisyResourceLookup.GetDefaultRangeTrackHeight(Size);
            double thumbSize = DaisyResourceLookup.GetDefaultRangeThumbSize(Size);

            _trackBorder.Height = trackHeight;
            _trackBorder.CornerRadius = new CornerRadius(trackHeight / 2);

            _fillBorder.Height = trackHeight;
            _fillBorder.CornerRadius = new CornerRadius(trackHeight / 2);

            _thumb.Width = thumbSize;
            _thumb.Height = thumbSize;

            MinHeight = thumbSize;
        }

        private void ApplyColors()
        {
            if (_trackBorder == null || _fillBorder == null || _thumb == null)
                return;

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyRangeVariant.Primary => "Primary",
                DaisyRangeVariant.Secondary => "Secondary",
                DaisyRangeVariant.Accent => "Accent",
                DaisyRangeVariant.Success => "Success",
                DaisyRangeVariant.Warning => "Warning",
                DaisyRangeVariant.Info => "Info",
                DaisyRangeVariant.Error => "Error",
                _ => ""
            };

            var accentBrushKey = Variant switch
            {
                DaisyRangeVariant.Primary => "DaisyPrimaryBrush",
                DaisyRangeVariant.Secondary => "DaisySecondaryBrush",
                DaisyRangeVariant.Accent => "DaisyAccentBrush",
                DaisyRangeVariant.Success => "DaisySuccessBrush",
                DaisyRangeVariant.Warning => "DaisyWarningBrush",
                DaisyRangeVariant.Info => "DaisyInfoBrush",
                DaisyRangeVariant.Error => "DaisyErrorBrush",
                _ => "DaisyBaseContentBrush"
            };

            // Check for lightweight styling overrides
            var trackOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyRange", "TrackBrush");
            var fillOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyRange", $"{variantName}FillBrush")
                : null;
            fillOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyRange", "FillBrush");
            var thumbOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyRange", $"{variantName}ThumbBrush")
                : null;
            thumbOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyRange", "ThumbBrush");

            var accentBrush = DaisyResourceLookup.GetBrush(accentBrushKey);

            _trackBorder.Background = trackOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _fillBorder.Background = ShowProgress ? (fillOverride ?? accentBrush) : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            _thumb.Fill = thumbOverride ?? accentBrush;
        }

        private void UpdateThumbPosition()
        {
            if (_thumb == null || _fillBorder == null) return;

            var range = Maximum - Minimum;
            if (range <= 0) return;

            var percent = ValueCoercion.Ratio((Value - Minimum) / range);

            var trackWidth = ActualWidth;
            var thumbWidth = _thumb.Width;

            if (trackWidth <= 0) return;

            var thumbX = (trackWidth - thumbWidth) * percent;
            _thumb.Margin = new Thickness(thumbX, 0, 0, 0);

            if (ShowProgress)
            {
                _fillBorder.Width = thumbX + thumbWidth / 2;
            }
        }

        #endregion
    }
}

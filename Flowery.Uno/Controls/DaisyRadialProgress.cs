using System;
using System.Diagnostics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A radial (circular) progress indicator styled after DaisyUI's Radial Progress component.
    /// </summary>
    /// <remarks>
    /// <para><b>Text Display:</b> Use <see cref="ShowValue"/> to toggle the percentage text (default: true).</para>
    /// <para><b>Auto-Sizing:</b> When <see cref="ShowValue"/> is true, the control auto-sizes to fit the text content,
    /// treating <see cref="Size"/> as a minimum size. When text is hidden, <see cref="Size"/> is the exact size.</para>
    /// </remarks>
    public partial class DaisyRadialProgress : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Ellipse? _trackEllipse;
        private Microsoft.UI.Xaml.Shapes.Path? _progressArc;
        private TextBlock? _valueTextBlock;
        private ScalarKeyFrameAnimation? _shimmerAnimation;
        private Visual? _arcVisual;

        public DaisyRadialProgress()
        {
            DefaultStyleKey = typeof(DaisyRadialProgress);
            IsTabStop = false;

            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(DaisyRadialProgress),
                new PropertyMetadata(0.0, OnValueChanged));

        /// <summary>
        /// Gets or sets the current value. The value is clamped between Minimum and Maximum.
        /// The arc fills proportionally based on the value relative to Maximum.
        /// </summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(DaisyRadialProgress),
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
                typeof(DaisyRadialProgress),
                new PropertyMetadata(100.0, OnValueChanged));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyProgressVariant),
                typeof(DaisyRadialProgress),
                new PropertyMetadata(DaisyProgressVariant.Default, OnAppearanceChanged));

        public DaisyProgressVariant Variant
        {
            get => (DaisyProgressVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyRadialProgress),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of the control.
        /// When <see cref="ShowValue"/> is true, this acts as a minimum size.
        /// When <see cref="ShowValue"/> is false, this is the exact size.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(
                nameof(Thickness),
                typeof(double?),
                typeof(DaisyRadialProgress),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the stroke thickness of the progress arc.
        /// If null, thickness is derived from the Size property using design tokens.
        /// </summary>
        public double? Thickness
        {
            get => (double?)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public static readonly DependencyProperty ShowValueProperty =
            DependencyProperty.Register(
                nameof(ShowValue),
                typeof(bool),
                typeof(DaisyRadialProgress),
                new PropertyMetadata(true, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets whether to display the percentage value in the center.
        /// When true, the control auto-sizes to fit the text, treating Size as a minimum.
        /// </summary>
        public bool ShowValue
        {
            get => (bool)GetValue(ShowValueProperty);
            set => SetValue(ShowValueProperty, value);
        }

        public static readonly DependencyProperty ShimmerProperty =
            DependencyProperty.Register(
                nameof(Shimmer),
                typeof(bool),
                typeof(DaisyRadialProgress),
                new PropertyMetadata(false, OnShimmerChanged));

        /// <summary>
        /// Gets or sets whether the progress arc should animate with a subtle breathing effect.
        /// </summary>
        public bool Shimmer
        {
            get => (bool)GetValue(ShimmerProperty);
            set => SetValue(ShimmerProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRadialProgress progress)
            {
                progress.UpdateProgressArc();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRadialProgress progress)
            {
                progress.ApplyAll();
            }
        }

        private static void OnShimmerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyRadialProgress progress)
            {
                progress.UpdateShimmerAnimation();
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

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            StopShimmerAnimation();
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

            // Track circle (background)
            _trackEllipse = new Ellipse
            {
                Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            _rootGrid.Children.Add(_trackEllipse);

            // Progress arc
            _progressArc = new Microsoft.UI.Xaml.Shapes.Path
            {
                Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            _rootGrid.Children.Add(_progressArc);

            // Value text
            _valueTextBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            _rootGrid.Children.Add(_valueTextBlock);

            Content = _rootGrid;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_rootGrid == null || _trackEllipse == null || _progressArc == null || _valueTextBlock == null)
                return;
            ApplySizing();
            ApplyColors();
            UpdateProgressArc();
            UpdateShimmerAnimation();
        }

        private void ApplySizing()
        {
            if (_rootGrid == null || _trackEllipse == null || _progressArc == null || _valueTextBlock == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Get base size from design tokens (this is the minimum size)
            double baseSize = DaisyResourceLookup.GetDefaultRadialProgressSize(Size);

            // Get font size from design tokens
            double fontSize = DaisyResourceLookup.GetDefaultRadialProgressFontSize(Size);

            // Get stroke thickness from explicit property or design tokens
            double strokeThickness = Thickness ?? DaisyResourceLookup.GetDefaultRadialProgressThickness(Size);

            // Set font properties first so we can measure
            _valueTextBlock.FontSize = fontSize;
            _valueTextBlock.Visibility = ShowValue ? Visibility.Visible : Visibility.Collapsed;

            double finalSize = baseSize;

            // Auto-size when showing text: measure text and ensure circle is large enough
            if (ShowValue)
            {
                // Calculate the text that will be displayed (same logic as UpdateProgressArc)
                var displayText = $"{(int)Math.Round(Value)}%";

                // Set the text so we can measure it accurately
                _valueTextBlock.Text = displayText;

                // Calculate required size based on text content
                // Use estimated text width based on character count and font size as a fallback
                double estimatedTextWidth = displayText.Length * fontSize * 0.6; // Approximate character width ratio
                double estimatedTextHeight = fontSize * 1.2; // Line height approximation

                // Try to measure the text for more accurate sizing
                _valueTextBlock.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                var textSize = _valueTextBlock.DesiredSize;

                // Only use measured size if it's valid (non-zero)
                if (textSize.Width > 0 && textSize.Height > 0)
                {
                    estimatedTextWidth = textSize.Width;
                    estimatedTextHeight = textSize.Height;
                }

                // Calculate minimum circle diameter to contain text:
                // The diagonal of the text bounds must fit inside the circle (minus stroke thickness and padding)
                double textDiagonal = Math.Sqrt(estimatedTextWidth * estimatedTextWidth + estimatedTextHeight * estimatedTextHeight);

                // Add padding: stroke on both sides plus a comfortable margin (20% of text diagonal)
                double requiredSize = textDiagonal + (strokeThickness * 2) + (textDiagonal * 0.2);

                // Use the larger of base size or required size
                finalSize = Math.Max(baseSize, requiredSize);
            }

            // Apply the computed size
            _rootGrid.Width = finalSize;
            _rootGrid.Height = finalSize;
            _trackEllipse.Width = finalSize;
            _trackEllipse.Height = finalSize;
            _trackEllipse.StrokeThickness = strokeThickness;
            _progressArc.StrokeThickness = strokeThickness;
        }

        private void ApplyColors()
        {
            if (_trackEllipse == null || _progressArc == null || _valueTextBlock == null)
                return;

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyProgressVariant.Primary => "Primary",
                DaisyProgressVariant.Secondary => "Secondary",
                DaisyProgressVariant.Accent => "Accent",
                DaisyProgressVariant.Info => "Info",
                DaisyProgressVariant.Success => "Success",
                DaisyProgressVariant.Warning => "Warning",
                DaisyProgressVariant.Error => "Error",
                _ => ""
            };

            var progressBrushKey = Variant switch
            {
                DaisyProgressVariant.Primary => "DaisyPrimaryBrush",
                DaisyProgressVariant.Secondary => "DaisySecondaryBrush",
                DaisyProgressVariant.Accent => "DaisyAccentBrush",
                DaisyProgressVariant.Info => "DaisyInfoBrush",
                DaisyProgressVariant.Success => "DaisySuccessBrush",
                DaisyProgressVariant.Warning => "DaisyWarningBrush",
                DaisyProgressVariant.Error => "DaisyErrorBrush",
                _ => "DaisyNeutralBrush"
            };

            // Check for lightweight styling overrides
            var trackOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyRadialProgress", "TrackBrush");
            var arcOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyRadialProgress", $"{variantName}ArcBrush")
                : null;
            arcOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyRadialProgress", "ArcBrush");
            var textOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyRadialProgress", "TextBrush");

            _trackEllipse.Stroke = trackOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _progressArc.Stroke = arcOverride ?? DaisyResourceLookup.GetBrush(progressBrushKey);
            _valueTextBlock.Foreground = textOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
        }

        private void UpdateProgressArc()
        {
            if (_progressArc == null || _valueTextBlock == null || _rootGrid == null || _trackEllipse == null)
                return;

            var range = Maximum - Minimum;
            if (range <= 0)
            {
                _progressArc.Data = null;
                _valueTextBlock.Text = "0%";
                return;
            }

            // Clamp value to valid range for arc calculation
            var clampedValue = Math.Max(Minimum, Math.Min(Maximum, Value));
            var percentage = (clampedValue - Minimum) / range;
            percentage = Math.Max(0, Math.Min(1, percentage));

            // Display actual value (e.g., "150%" when Value=150)
            _valueTextBlock.Text = $"{(int)Math.Round(Value)}%";

            // Calculate arc geometry
            var size = _rootGrid.Width;
            var strokeThickness = _trackEllipse.StrokeThickness;
            var radius = (size - strokeThickness) / 2;
            var centerX = size / 2;
            var centerY = size / 2;

            // Start from top (12 o'clock position)
            var startAngle = -90;
            var sweepAngle = percentage * 360;

            if (sweepAngle <= 0)
            {
                _progressArc.Data = null;
                return;
            }

            // For nearly complete circles, use a slightly smaller angle to avoid visual glitches
            if (sweepAngle >= 359.9)
            {
                sweepAngle = 359.9;
            }

            var startRad = startAngle * Math.PI / 180;
            var endRad = (startAngle + sweepAngle) * Math.PI / 180;

            var startX = centerX + radius * Math.Cos(startRad);
            var startY = centerY + radius * Math.Sin(startRad);
            var endX = centerX + radius * Math.Cos(endRad);
            var endY = centerY + radius * Math.Sin(endRad);

            var isLargeArc = sweepAngle > 180;

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(startX, startY),
                IsClosed = false
            };

            var arcSegment = new ArcSegment
            {
                Point = new Windows.Foundation.Point(endX, endY),
                Size = new Windows.Foundation.Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };

            pathFigure.Segments.Add(arcSegment);
            pathGeometry.Figures.Add(pathFigure);
            _progressArc.Data = pathGeometry;
        }

        #endregion

        #region Shimmer Animation

        private void UpdateShimmerAnimation()
        {
            if (Shimmer)
                StartShimmerAnimation();
            else
                StopShimmerAnimation();
        }

        private void StartShimmerAnimation()
        {
            if (_progressArc == null)
                return;

            try
            {
                _arcVisual = ElementCompositionPreview.GetElementVisual(_progressArc);
                if (_arcVisual == null)
                    return;

                var compositor = _arcVisual.Compositor;

                // Create a subtle breathing animation on opacity (0.60 - 1.0)
                _shimmerAnimation = compositor.CreateScalarKeyFrameAnimation();
                _shimmerAnimation.InsertKeyFrame(0.0f, 1.0f);
                _shimmerAnimation.InsertKeyFrame(0.5f, 0.60f);
                _shimmerAnimation.InsertKeyFrame(1.0f, 1.0f);
                _shimmerAnimation.Duration = TimeSpan.FromSeconds(2.0);
                _shimmerAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

                PlatformCompatibility.StartAnimation(_arcVisual, "Opacity", _shimmerAnimation);
            }
            catch
            {
                // Composition API may not be available on all platforms
            }
        }

        private void StopShimmerAnimation()
        {
            if (_arcVisual != null)
            {
                try
                {
                    _arcVisual.StopAnimation("Opacity");
                    _arcVisual.Opacity = 1.0f;
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            _shimmerAnimation = null;
        }

        #endregion
    }
}

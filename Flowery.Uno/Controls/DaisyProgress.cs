using System;
using Microsoft.UI;
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
    /// A Progress bar control styled after DaisyUI's Progress component.
    /// Supports variant colors and multiple sizes.
    /// </summary>
    public partial class DaisyProgress : DaisyBaseContentControl
    {
        private Border? _trackBorder;
        private Border? _fillBorder;
        private ScalarKeyFrameAnimation? _shimmerAnimation;
        private Visual? _fillVisual;

        public DaisyProgress()
        {
            DefaultStyleKey = typeof(DaisyProgress);
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyProgressVariant),
                typeof(DaisyProgress),
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
                typeof(DaisyProgress),
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
                typeof(DaisyProgress),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Max(0, Math.Min(100, value)));
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(DaisyProgress),
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
                typeof(DaisyProgress),
                new PropertyMetadata(100.0, OnValueChanged));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty ShimmerProperty =
            DependencyProperty.Register(
                nameof(Shimmer),
                typeof(bool),
                typeof(DaisyProgress),
                new PropertyMetadata(false, OnShimmerChanged));

        /// <summary>
        /// Gets or sets whether the progress fill should animate with a subtle breathing effect.
        /// </summary>
        public bool Shimmer
        {
            get => (bool)GetValue(ShimmerProperty);
            set => SetValue(ShimmerProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyProgress p)
            {
                p.ApplyAll();
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyProgress p)
            {
                p.UpdateFillWidth();
            }
        }

        private static void OnShimmerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyProgress p)
            {
                p.UpdateShimmerAnimation();
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

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _trackBorder ?? base.GetNeumorphicHostElement();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            _trackBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _fillBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _trackBorder.Child = _fillBorder;
            Content = _trackBorder;

            // Subscribe to size changes to update fill width
            SizeChanged += (s, e) => UpdateFillWidth();
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_trackBorder == null || _fillBorder == null)
                return;

            ApplySizing();
            ApplyColors();
            UpdateFillWidth();
            UpdateShimmerAnimation();
        }

        private void ApplySizing()
        {
            if (_trackBorder == null || _fillBorder == null)
                return;

            double height = Size switch
            {
                DaisySize.ExtraSmall => DaisyResourceLookup.GetDouble("DaisyProgressExtraSmallHeight", 2),
                DaisySize.Small => DaisyResourceLookup.GetDouble("DaisyProgressSmallHeight", 4),
                DaisySize.Medium => DaisyResourceLookup.GetDouble("DaisyProgressMediumHeight", 8),
                DaisySize.Large => DaisyResourceLookup.GetDouble("DaisyProgressLargeHeight", 16),
                DaisySize.ExtraLarge => DaisyResourceLookup.GetDouble("DaisyProgressLargeHeight", 16),
                _ => 8
            };

            var cornerRadius = Size switch
            {
                DaisySize.ExtraSmall => DaisyResourceLookup.GetCornerRadius("DaisyProgressExtraSmallCornerRadius", new CornerRadius(1)),
                DaisySize.Small => DaisyResourceLookup.GetCornerRadius("DaisyProgressSmallCornerRadius", new CornerRadius(2)),
                DaisySize.Medium => DaisyResourceLookup.GetCornerRadius("DaisyProgressMediumCornerRadius", new CornerRadius(4)),
                DaisySize.Large => DaisyResourceLookup.GetCornerRadius("DaisyProgressLargeCornerRadius", new CornerRadius(8)),
                DaisySize.ExtraLarge => DaisyResourceLookup.GetCornerRadius("DaisyProgressLargeCornerRadius", new CornerRadius(8)),
                _ => new CornerRadius(4)
            };

            _trackBorder.Height = height;
            _trackBorder.CornerRadius = cornerRadius;

            _fillBorder.Height = height;
            _fillBorder.CornerRadius = cornerRadius;
        }

        private void ApplyColors()
        {
            if (_trackBorder == null || _fillBorder == null)
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

            var fillBrushKey = Variant switch
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
            var trackOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyProgress", "TrackBrush");
            var fillOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyProgress", $"{variantName}FillBrush")
                : null;
            fillOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyProgress", "FillBrush");

            _trackBorder.Background = trackOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _fillBorder.Background = fillOverride ?? DaisyResourceLookup.GetBrush(fillBrushKey);
        }

        private void UpdateFillWidth()
        {
            if (_trackBorder == null || _fillBorder == null)
                return;

            var range = Maximum - Minimum;
            if (range <= 0)
            {
                _fillBorder.Width = 0;
                return;
            }

            var percentage = (Value - Minimum) / range;
            percentage = Math.Max(0, Math.Min(1, percentage));

            var trackWidth = _trackBorder.ActualWidth;
            if (trackWidth > 0)
            {
                _fillBorder.Width = trackWidth * percentage;
            }
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
            if (_fillBorder == null)
                return;

            try
            {
                _fillVisual = ElementCompositionPreview.GetElementVisual(_fillBorder);
                if (_fillVisual == null)
                    return;

                var compositor = _fillVisual.Compositor;

                // Create a subtle breathing animation on opacity (0.60 - 1.0)
                _shimmerAnimation = compositor.CreateScalarKeyFrameAnimation();
                _shimmerAnimation.InsertKeyFrame(0.0f, 1.0f);
                _shimmerAnimation.InsertKeyFrame(0.5f, 0.60f);
                _shimmerAnimation.InsertKeyFrame(1.0f, 1.0f);
                _shimmerAnimation.Duration = TimeSpan.FromSeconds(2.0);
                _shimmerAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

                PlatformCompatibility.StartAnimation(_fillVisual, "Opacity", _shimmerAnimation);
            }
            catch
            {
                // Composition API may not be available on all platforms
            }
        }

        private void StopShimmerAnimation()
        {
            if (_fillVisual != null)
            {
                try
                {
                    _fillVisual.StopAnimation("Opacity");
                    _fillVisual.Opacity = 1.0f;
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

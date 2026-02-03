using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using Flowery.Skia;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// Blur rendering mode for DaisyGlass.
    /// </summary>
    public enum GlassBlurMode
    {
        /// <summary>
        /// Simulated glass using gradient overlays (no real blur).
        /// </summary>
        Simulated,

        /// <summary>
        /// Captures bitmap and applies blur effect (one-time capture).
        /// </summary>
        BitmapCapture,

        /// <summary>
        /// Uses SkiaSharp for GPU-accelerated blur (experimental, requires Skia backend).
        /// </summary>
        SkiaSharp
    }

    /// <summary>
    /// A glass/frosted effect container control styled after DaisyUI's glass effect.
    /// Supports multiple blur modes: Simulated, BitmapCapture, and SkiaSharp.
    /// </summary>
    public partial class DaisyGlass : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _backgroundBorder;
        private Border? _tintBorder;
        private Border? _highlightBorder;
        private Image? _blurredBackdropImage;
        private ContentPresenter? _contentPresenter;
        private object? _userContent;
        private bool _isCapturing;
        private bool _needsUpdate = true;

        public DaisyGlass()
        {
            DefaultStyleKey = typeof(DaisyGlass);
            CornerRadius = new CornerRadius(16);
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty GlassBlurProperty =
            DependencyProperty.Register(
                nameof(GlassBlur),
                typeof(double),
                typeof(DaisyGlass),
                new PropertyMetadata(40.0, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the blur amount for the glass effect.
        /// Only effective when BlurMode is BitmapCapture or SkiaSharp.
        /// </summary>
        public double GlassBlur
        {
            get => (double)GetValue(GlassBlurProperty);
            set => SetValue(GlassBlurProperty, value);
        }

        public static readonly DependencyProperty GlassSaturationProperty =
            DependencyProperty.Register(
                nameof(GlassSaturation),
                typeof(double),
                typeof(DaisyGlass),
                new PropertyMetadata(1.0, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the saturation of the glass background (0.0 = grayscale, 1.0 = normal).
        /// Only effective when BlurMode is SkiaSharp.
        /// </summary>
        public double GlassSaturation
        {
            get => (double)GetValue(GlassSaturationProperty);
            set => SetValue(GlassSaturationProperty, value);
        }

        public static readonly DependencyProperty EnableBackdropBlurProperty =
            DependencyProperty.Register(
                nameof(EnableBackdropBlur),
                typeof(bool),
                typeof(DaisyGlass),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets whether to enable real backdrop blur (performance intensive).
        /// When false, uses the simulated glass effect.
        /// </summary>
        public bool EnableBackdropBlur
        {
            get => (bool)GetValue(EnableBackdropBlurProperty);
            set => SetValue(EnableBackdropBlurProperty, value);
        }

        public static readonly DependencyProperty BlurModeProperty =
            DependencyProperty.Register(
                nameof(BlurMode),
                typeof(GlassBlurMode),
                typeof(DaisyGlass),
                new PropertyMetadata(GlassBlurMode.Simulated, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the blur rendering mode.
        /// </summary>
        public GlassBlurMode BlurMode
        {
            get => (GlassBlurMode)GetValue(BlurModeProperty);
            set => SetValue(BlurModeProperty, value);
        }

        public static readonly DependencyProperty GlassOpacityProperty =
            DependencyProperty.Register(
                nameof(GlassOpacity),
                typeof(double),
                typeof(DaisyGlass),
                new PropertyMetadata(0.25, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the opacity of the glass effect.
        /// </summary>
        public double GlassOpacity
        {
            get => (double)GetValue(GlassOpacityProperty);
            set => SetValue(GlassOpacityProperty, value);
        }

        public static readonly DependencyProperty GlassTintProperty =
            DependencyProperty.Register(
                nameof(GlassTint),
                typeof(Color),
                typeof(DaisyGlass),
                new PropertyMetadata(Microsoft.UI.Colors.White, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the tint color for the glass effect.
        /// </summary>
        public Color GlassTint
        {
            get => (Color)GetValue(GlassTintProperty);
            set => SetValue(GlassTintProperty, value);
        }

        public static readonly DependencyProperty GlassTintOpacityProperty =
            DependencyProperty.Register(
                nameof(GlassTintOpacity),
                typeof(double),
                typeof(DaisyGlass),
                new PropertyMetadata(0.5, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the opacity of the tint overlay.
        /// </summary>
        public double GlassTintOpacity
        {
            get => (double)GetValue(GlassTintOpacityProperty);
            set => SetValue(GlassTintOpacityProperty, value);
        }

        public static readonly DependencyProperty GlassBorderOpacityProperty =
            DependencyProperty.Register(
                nameof(GlassBorderOpacity),
                typeof(double),
                typeof(DaisyGlass),
                new PropertyMetadata(0.2, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the opacity of the glass border.
        /// </summary>
        public double GlassBorderOpacity
        {
            get => (double)GetValue(GlassBorderOpacityProperty);
            set => SetValue(GlassBorderOpacityProperty, value);
        }

        public static readonly DependencyProperty GlassReflectOpacityProperty =
            DependencyProperty.Register(
                nameof(GlassReflectOpacity),
                typeof(double),
                typeof(DaisyGlass),
                new PropertyMetadata(0.1, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the opacity of the glass reflection highlight.
        /// </summary>
        public double GlassReflectOpacity
        {
            get => (double)GetValue(GlassReflectOpacityProperty);
            set => SetValue(GlassReflectOpacityProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyGlass glass)
            {
                glass.ApplyAll();

                // Trigger recapture if blur-related properties changed
                if (e.Property == EnableBackdropBlurProperty ||
                    e.Property == BlurModeProperty ||
                    e.Property == GlassBlurProperty ||
                    e.Property == GlassSaturationProperty)
                {
                    glass._needsUpdate = true;
                    glass.ScheduleBackdropCapture();
                }
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            if (_rootGrid != null)
            {
                ApplyAll();
                ScheduleBackdropCapture();
                return;
            }

            // Capture user content
            _userContent = Content;
            Content = null;

            BuildVisualTree();
            ApplyAll();
            ScheduleBackdropCapture();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            _blurredBackdropImage?.ClearValue(Image.SourceProperty);
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
            ScheduleBackdropCapture();
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _backgroundBorder ?? base.GetNeumorphicHostElement();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Capture user content if needed
            if (Content != null && !ReferenceEquals(Content, _rootGrid))
            {
                _userContent = Content;
                Content = null;
            }

            _rootGrid = new Grid();

            // Blurred backdrop image (for BitmapCapture/SkiaSharp modes)
            _blurredBackdropImage = new Image
            {
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Collapsed
            };
            _rootGrid.Children.Add(_blurredBackdropImage);

            // Background layer (simulated glass effect - used when not using blur modes)
            _backgroundBorder = new Border
            {
                CornerRadius = CornerRadius
            };
            _rootGrid.Children.Add(_backgroundBorder);

            // Tint overlay
            _tintBorder = new Border
            {
                CornerRadius = CornerRadius
            };
            _rootGrid.Children.Add(_tintBorder);

            // Highlight border for glass reflection effect
            _highlightBorder = new Border
            {
                CornerRadius = CornerRadius,
                BorderThickness = new Thickness(1, 1, 0, 0)
            };
            _rootGrid.Children.Add(_highlightBorder);

            // Content presenter
            _contentPresenter = new ContentPresenter
            {
                Content = _userContent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _rootGrid.Children.Add(_contentPresenter);

            Content = _rootGrid;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_backgroundBorder == null || _tintBorder == null || _highlightBorder == null)
                return;

            ApplyCornerRadius();
            ApplyGlassEffect();
        }

        private void ApplyCornerRadius()
        {
            if (_backgroundBorder == null || _tintBorder == null || _highlightBorder == null)
                return;

            _backgroundBorder.CornerRadius = CornerRadius;
            _tintBorder.CornerRadius = CornerRadius;
            _highlightBorder.CornerRadius = CornerRadius;
        }

        private void ApplyGlassEffect()
        {
            if (_backgroundBorder == null || _tintBorder == null || _highlightBorder == null)
                return;

            // Background with glass opacity
            var baseBrush = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            if (baseBrush is SolidColorBrush solidBrush)
            {
                var baseColor = solidBrush.Color;
                _backgroundBorder.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)(255 * GlassOpacity),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B
                ));
            }
            else
            {
                _backgroundBorder.Background = baseBrush;
                _backgroundBorder.Opacity = GlassOpacity;
            }

            // Tint overlay
            _tintBorder.Background = new SolidColorBrush(Color.FromArgb(
                (byte)(255 * GlassTintOpacity),
                GlassTint.R,
                GlassTint.G,
                GlassTint.B
            ));

            // Border and highlight
            _highlightBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(
                (byte)(255 * GlassReflectOpacity),
                255, 255, 255
            ));

            // Add subtle outer border
            var borderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            if (borderBrush is SolidColorBrush borderSolid)
            {
                _backgroundBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(
                    (byte)(255 * GlassBorderOpacity),
                    borderSolid.Color.R,
                    borderSolid.Color.G,
                    borderSolid.Color.B
                ));
                _backgroundBorder.BorderThickness = new Thickness(1);
            }

            // Update visibility based on blur mode
            UpdateBlurModeVisibility();
        }

        private void UpdateBlurModeVisibility()
        {
            if (_blurredBackdropImage == null || _backgroundBorder == null)
                return;

            bool useBlurMode = EnableBackdropBlur && BlurMode != GlassBlurMode.Simulated;

            // Show blurred backdrop image when using blur modes, hide simulated background
            _blurredBackdropImage.Visibility = useBlurMode ? Visibility.Visible : Visibility.Collapsed;
            _backgroundBorder.Visibility = useBlurMode ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion

        #region Backdrop Capture

        private void ScheduleBackdropCapture()
        {
            if (!EnableBackdropBlur || BlurMode == GlassBlurMode.Simulated)
                return;

            if (_isCapturing || !_needsUpdate)
                return;

            // Defer capture to allow layout to complete
            DispatcherQueue?.TryEnqueue(async () => await CaptureAndBlurBackdropAsync());
        }

        private async System.Threading.Tasks.Task CaptureAndBlurBackdropAsync()
        {
            if (_isCapturing || !EnableBackdropBlur || BlurMode == GlassBlurMode.Simulated)
                return;

            if (_blurredBackdropImage == null)
                return;

            _isCapturing = true;
            _needsUpdate = false;

            try
            {
                // Convert Windows.UI.Color to the format needed by Skia
                var tintColor = new Windows.UI.Color
                {
                    A = GlassTint.A,
                    R = GlassTint.R,
                    G = GlassTint.G,
                    B = GlassTint.B
                };

                var glassImageSource = await SkiaBackdropCapture.CaptureAndApplyGlassEffectAsync(
                    this,
                    GlassBlur,
                    GlassSaturation,
                    tintColor,
                    GlassTintOpacity,
                    CornerRadius.TopLeft);

                if (glassImageSource != null)
                {
                    _blurredBackdropImage.Source = glassImageSource;
                    UpdateBlurModeVisibility();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DaisyGlass] Backdrop capture failed: {ex.Message}");
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// Manually refresh the backdrop blur effect.
        /// Call this after the background behind the glass has changed.
        /// </summary>
        public void RefreshBackdrop()
        {
            if (EnableBackdropBlur && BlurMode != GlassBlurMode.Simulated)
            {
                _needsUpdate = true;
                ScheduleBackdropCapture();
            }
        }

        #endregion
    }
}

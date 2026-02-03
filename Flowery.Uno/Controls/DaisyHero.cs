using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A hero section control styled after DaisyUI's Hero component.
    /// Used for showcasing prominent content with background and centered layout.
    /// </summary>
    public partial class DaisyHero : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _backgroundBorder;
        private Border? _overlayBorder;
        private ContentPresenter? _contentPresenter;

        public DaisyHero()
        {
            DefaultStyleKey = typeof(DaisyHero);
            IsTabStop = false;

            // Default alignment to stretch for hero sections
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
        }

        #region Dependency Properties

        public static readonly DependencyProperty OverlayOpacityProperty =
            DependencyProperty.Register(
                nameof(OverlayOpacity),
                typeof(double),
                typeof(DaisyHero),
                new PropertyMetadata(0.0, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the opacity of the overlay. Set to > 0 for a dark overlay effect.
        /// </summary>
        public double OverlayOpacity
        {
            get => (double)GetValue(OverlayOpacityProperty);
            set => SetValue(OverlayOpacityProperty, Math.Max(0, Math.Min(1, value)));
        }

        public static readonly DependencyProperty MinimumHeightProperty =
            DependencyProperty.Register(
                nameof(MinimumHeight),
                typeof(double),
                typeof(DaisyHero),
                new PropertyMetadata(300.0, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the minimum height for the hero section.
        /// </summary>
        // Android's underlying View has MinimumHeight, so we must hide it on that target.
#if __ANDROID__
        public new double MinimumHeight
#else
        public double MinimumHeight
#endif
        {
            get => (double)GetValue(MinimumHeightProperty);
            set => SetValue(MinimumHeightProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyHero hero)
            {
                hero.ApplyAll();
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

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Capture user-provided content
            var userContent = Content;
            Content = null;

            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Background layer - managed by ApplyColors for dynamic theme support
            _backgroundBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _rootGrid.Children.Add(_backgroundBorder);

            // Overlay layer (for dimming effect)
            _overlayBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
                Opacity = 0
            };
            _rootGrid.Children.Add(_overlayBorder);

            // Content layer
            _contentPresenter = new ContentPresenter
            {
                Content = userContent,
                HorizontalAlignment = HorizontalContentAlignment,
                VerticalAlignment = VerticalContentAlignment,
                HorizontalContentAlignment = HorizontalContentAlignment,
                VerticalContentAlignment = VerticalContentAlignment
            };
            _rootGrid.Children.Add(_contentPresenter);

            Content = _rootGrid;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_rootGrid == null || _backgroundBorder == null || _overlayBorder == null || _contentPresenter == null)
                return;

            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            if (_rootGrid == null)
                return;

            _rootGrid.MinHeight = MinimumHeight;

            // Apply padding to content presenter
            if (_contentPresenter != null)
            {
                _contentPresenter.Margin = Padding;
            }
        }

        private void ApplyColors()
        {
            if (_backgroundBorder == null || _overlayBorder == null || _contentPresenter == null)
                return;

            // Apply overlay opacity
            _overlayBorder.Opacity = OverlayOpacity;

            // Get the background color from the control's Background brush
            var bgColor = (Background as SolidColorBrush)?.Color;
            if (bgColor == null)
            {
                // No background set - use defaults
                _backgroundBorder.Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
                ApplyContentForeground(DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"));
                return;
            }

            // Detect palette name once and cache it locally (color doesn't change, only theme does)
            _detectedPaletteName ??= DaisyResourceLookup.GetPaletteNameForColor(bgColor.Value);

            // Get fresh brushes from the current theme using the detected palette name
            var (freshBackground, freshContentBrush) = DaisyResourceLookup.GetPaletteBrushes(_detectedPaletteName);
            
            // Apply fresh background (fallback to Base200 if not found)
            _backgroundBorder.Background = freshBackground ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");

            // Apply fresh content foreground to nested TextBlocks
            ApplyContentForeground(freshContentBrush ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"));
        }

        // Cache the detected palette name (color doesn't change, only the theme brushes do)
        private string? _detectedPaletteName;

        /// <summary>
        /// Detects the background brush type and applies the corresponding content foreground
        /// to all TextBlocks inside this Hero via an injected style.
        /// </summary>
        private void ApplyContentForeground(Brush? contentBrush)
        {
            if (_contentPresenter == null || contentBrush == null)
                return;

            // Create a Style targeting TextBlock with the detected foreground
            var textBlockStyle = new Style(typeof(TextBlock));
            textBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, contentBrush));

            // Inject the style into the content presenter's resources
            // This will be inherited by all TextBlocks within
            _contentPresenter.Resources["DaisyHeroTextBlockStyle"] = textBlockStyle;

            // Also set implicit style (no x:Key) so it applies automatically
            _contentPresenter.Resources[typeof(TextBlock)] = textBlockStyle;
        }

        #endregion
    }
}

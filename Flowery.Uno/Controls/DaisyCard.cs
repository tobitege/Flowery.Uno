using System;
using Flowery.Skia;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Dispatching;

namespace Flowery.Controls
{
    /// <summary>
    /// A Card control styled after DaisyUI's Card component.
    /// Supports glass effects with optional Skia-based backdrop blur.
    /// </summary>
    public partial class DaisyCard : DaisyBaseContentControl
    {
        private Border? _solidBackground;
        private Grid? _root;
        private Image? _blurredBackdrop;

        private Border? _beveledShadow;
        private Border? _innerHighlight;
        private Border? _innerShadow;
        private Brush? _originalBackground;
        private bool _isTemplateApplied;
        private bool _isInternalColorSet;
        private bool _isCapturing;
        private bool _needsUpdate = true;

        public DaisyCard()
        {
            DefaultStyleKey = typeof(DaisyCard);
            IsTabStop = false;
        }


        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _solidBackground = GetTemplateChild("PART_SolidBackground") as Border;
            _root = GetTemplateChild("PART_Root") as Grid;
            _blurredBackdrop = GetTemplateChild("PART_BlurredBackdrop") as Image;

            _beveledShadow = GetTemplateChild("PART_BeveledShadow") as Border;
            _innerHighlight = GetTemplateChild("PART_InnerHighlight") as Border;
            _innerShadow = GetTemplateChild("PART_InnerShadow") as Border;

            if (_root != null)
            {
                _root.SizeChanged -= OnRootSizeChanged;
                _root.SizeChanged += OnRootSizeChanged;
            }

            _isTemplateApplied = true;
            ApplyAll();
            UpdateBuiltInContentState();
            ScheduleBackdropCapture();
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            UpdateBuiltInContentState();
        }



        private void OnRootSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Size changed - derived classes can override for pattern/ornament rebuilding
        }

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyCard),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// The size/scale of the card.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyCardVariant),
                typeof(DaisyCard),
                new PropertyMetadata(DaisyCardVariant.Normal, OnAppearanceChanged));

        /// <summary>
        /// The layout variant of the card.
        /// </summary>
        public DaisyCardVariant Variant
        {
            get => (DaisyCardVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }
        #endregion

        #region ColorVariant
        public static readonly DependencyProperty ColorVariantProperty =
            DependencyProperty.Register(
                nameof(ColorVariant),
                typeof(DaisyColor),
                typeof(DaisyCard),
                new PropertyMetadata(DaisyColor.Default, (d, _) => ((DaisyCard)d).ApplyAll()));

        public DaisyColor ColorVariant
        {
            get => (DaisyColor)GetValue(ColorVariantProperty);
            set => SetValue(ColorVariantProperty, value);
        }
        #endregion

        #region BodyPadding
        public static readonly DependencyProperty BodyPaddingProperty =
            DependencyProperty.Register(
                nameof(BodyPadding),
                typeof(Thickness),
                typeof(DaisyCard),
                new PropertyMetadata(new Thickness(32)));

        /// <summary>
        /// The padding of the card body.
        /// </summary>
        public Thickness BodyPadding
        {
            get => (Thickness)GetValue(BodyPaddingProperty);
            set => SetValue(BodyPaddingProperty, value);
        }
        #endregion

        #region BodyFontSize
        public static readonly DependencyProperty BodyFontSizeProperty =
            DependencyProperty.Register(
                nameof(BodyFontSize),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(14.0, OnAppearanceChanged));

        /// <summary>
        /// The font size of the card body.
        /// </summary>
        public double BodyFontSize
        {
            get => (double)GetValue(BodyFontSizeProperty);
            set => SetValue(BodyFontSizeProperty, value);
        }
        #endregion

        #region TitleFontSize
        public static readonly DependencyProperty TitleFontSizeProperty =
            DependencyProperty.Register(
                nameof(TitleFontSize),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(20.0));

        /// <summary>
        /// The font size of the card title.
        /// </summary>
        public double TitleFontSize
        {
            get => (double)GetValue(TitleFontSizeProperty);
            set => SetValue(TitleFontSizeProperty, value);
        }
        #endregion

        #region BuiltInContent
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(DaisyCard),
                new PropertyMetadata(null, OnBuiltInContentChanged));

        /// <summary>
        /// Built-in title text for the card header.
        /// </summary>
        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(DaisyCard),
                new PropertyMetadata(null, OnBuiltInContentChanged));

        /// <summary>
        /// Built-in description text for the card body.
        /// </summary>
        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty IconImageSourceProperty =
            DependencyProperty.Register(
                nameof(IconImageSource),
                typeof(ImageSource),
                typeof(DaisyCard),
                new PropertyMetadata(null, OnBuiltInContentChanged));

        /// <summary>
        /// Image source for the built-in card icon.
        /// </summary>
        public ImageSource? IconImageSource
        {
            get => (ImageSource?)GetValue(IconImageSourceProperty);
            set => SetValue(IconImageSourceProperty, value);
        }

        public static readonly DependencyProperty IconBackgroundProperty =
            DependencyProperty.Register(
                nameof(IconBackground),
                typeof(Brush),
                typeof(DaisyCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Background brush for the built-in icon container.
        /// </summary>
        public Brush? IconBackground
        {
            get => (Brush?)GetValue(IconBackgroundProperty);
            set => SetValue(IconBackgroundProperty, value);
        }

        public static readonly DependencyProperty IconBorderBrushProperty =
            DependencyProperty.Register(
                nameof(IconBorderBrush),
                typeof(Brush),
                typeof(DaisyCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Border brush for the built-in icon container.
        /// </summary>
        public Brush? IconBorderBrush
        {
            get => (Brush?)GetValue(IconBorderBrushProperty);
            set => SetValue(IconBorderBrushProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                nameof(IconSize),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(56.0));

        /// <summary>
        /// Size of the built-in icon container.
        /// </summary>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }
        #endregion

        #region BuiltInContentState
        public static readonly DependencyProperty HasCardTitleProperty =
            DependencyProperty.Register(
                nameof(HasCardTitle),
                typeof(bool),
                typeof(DaisyCard),
                new PropertyMetadata(false));

        public bool HasCardTitle
        {
            get => (bool)GetValue(HasCardTitleProperty);
            private set => SetValue(HasCardTitleProperty, value);
        }

        public static readonly DependencyProperty HasCardDescriptionProperty =
            DependencyProperty.Register(
                nameof(HasCardDescription),
                typeof(bool),
                typeof(DaisyCard),
                new PropertyMetadata(false));

        public bool HasCardDescription
        {
            get => (bool)GetValue(HasCardDescriptionProperty);
            private set => SetValue(HasCardDescriptionProperty, value);
        }

        public static readonly DependencyProperty HasCardIconProperty =
            DependencyProperty.Register(
                nameof(HasCardIcon),
                typeof(bool),
                typeof(DaisyCard),
                new PropertyMetadata(false));

        public bool HasCardIcon
        {
            get => (bool)GetValue(HasCardIconProperty);
            private set => SetValue(HasCardIconProperty, value);
        }

        public static readonly DependencyProperty HasCardHeaderProperty =
            DependencyProperty.Register(
                nameof(HasCardHeader),
                typeof(bool),
                typeof(DaisyCard),
                new PropertyMetadata(false));

        public bool HasCardHeader
        {
            get => (bool)GetValue(HasCardHeaderProperty);
            private set => SetValue(HasCardHeaderProperty, value);
        }

        public static readonly DependencyProperty HasCardContentProperty =
            DependencyProperty.Register(
                nameof(HasCardContent),
                typeof(bool),
                typeof(DaisyCard),
                new PropertyMetadata(false));

        public bool HasCardContent
        {
            get => (bool)GetValue(HasCardContentProperty);
            private set => SetValue(HasCardContentProperty, value);
        }
        #endregion

        #region CardStyle
        public static readonly DependencyProperty CardStyleProperty =
            DependencyProperty.Register(
                nameof(CardStyle),
                typeof(DaisyCardStyle),
                typeof(DaisyCard),
                new PropertyMetadata(DaisyCardStyle.Default, OnAppearanceChanged));

        /// <summary>
        /// The visual style of the card (Flat, Beveled, Inset, Panel, Glass).
        /// </summary>
        public DaisyCardStyle CardStyle
        {
            get => (DaisyCardStyle)GetValue(CardStyleProperty);
            set => SetValue(CardStyleProperty, value);
        }
        #endregion

        #region IsGlass
        public static readonly DependencyProperty IsGlassProperty =
            DependencyProperty.Register(
                nameof(IsGlass),
                typeof(bool),
                typeof(DaisyCard),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// Whether the card uses a glass effect.
        /// </summary>
        public bool IsGlass
        {
            get => (bool)GetValue(IsGlassProperty);
            set => SetValue(IsGlassProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCard card)
                card.ApplyAll();
        }

        private static void OnBuiltInContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCard card)
                card.UpdateBuiltInContentState();
        }

        private void UpdateBuiltInContentState()
        {
            HasCardTitle = !string.IsNullOrWhiteSpace(Title);
            HasCardDescription = !string.IsNullOrWhiteSpace(Description);
            HasCardIcon = IconImageSource != null;
            HasCardHeader = HasCardTitle || HasCardIcon;
            HasCardContent = Content != null;
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
            ScheduleBackdropCapture();
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
            return (FrameworkElement?)_solidBackground ?? this;
        }

        #region GlassBlur
        public static readonly DependencyProperty GlassBlurProperty =
            DependencyProperty.Register(
                nameof(GlassBlur),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(40.0));

        /// <summary>
        /// The blur amount for the glass effect.
        /// </summary>
        public double GlassBlur
        {
            get => (double)GetValue(GlassBlurProperty);
            set => SetValue(GlassBlurProperty, value);
        }
        #endregion

        #region GlassOpacity
        public static readonly DependencyProperty GlassOpacityProperty =
            DependencyProperty.Register(
                nameof(GlassOpacity),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(0.3));

        /// <summary>
        /// The opacity of the glass effect.
        /// </summary>
        public double GlassOpacity
        {
            get => (double)GetValue(GlassOpacityProperty);
            set => SetValue(GlassOpacityProperty, value);
        }
        #endregion

        #region GlassTint
        public static readonly DependencyProperty GlassTintProperty =
            DependencyProperty.Register(
                nameof(GlassTint),
                typeof(Color),
                typeof(DaisyCard),
                new PropertyMetadata(Microsoft.UI.Colors.White));

        /// <summary>
        /// The tint color for the glass effect.
        /// </summary>
        public Color GlassTint
        {
            get => (Color)GetValue(GlassTintProperty);
            set => SetValue(GlassTintProperty, value);
        }
        #endregion

        #region GlassTintOpacity
        public static readonly DependencyProperty GlassTintOpacityProperty =
            DependencyProperty.Register(
                nameof(GlassTintOpacity),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(0.5));

        /// <summary>
        /// The tint opacity for the glass effect.
        /// </summary>
        public double GlassTintOpacity
        {
            get => (double)GetValue(GlassTintOpacityProperty);
            set => SetValue(GlassTintOpacityProperty, value);
        }
        #endregion

        #region GlassBorderOpacity
        public static readonly DependencyProperty GlassBorderOpacityProperty =
            DependencyProperty.Register(
                nameof(GlassBorderOpacity),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(0.2));

        /// <summary>
        /// The opacity of the glass border.
        /// </summary>
        public double GlassBorderOpacity
        {
            get => (double)GetValue(GlassBorderOpacityProperty);
            set => SetValue(GlassBorderOpacityProperty, value);
        }
        #endregion

        #region GlassReflectOpacity
        public static readonly DependencyProperty GlassReflectOpacityProperty =
            DependencyProperty.Register(
                nameof(GlassReflectOpacity),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(0.15, OnAppearanceChanged));

        /// <summary>
        /// The opacity of the glass reflection highlight.
        /// </summary>
        public double GlassReflectOpacity
        {
            get => (double)GetValue(GlassReflectOpacityProperty);
            set => SetValue(GlassReflectOpacityProperty, value);
        }
        #endregion

        #region EnableBackdropBlur
        public static readonly DependencyProperty EnableBackdropBlurProperty =
            DependencyProperty.Register(
                nameof(EnableBackdropBlur),
                typeof(bool),
                typeof(DaisyCard),
                new PropertyMetadata(false, OnBlurPropertyChanged));

        /// <summary>
        /// Whether to enable real backdrop blur using Skia (performance intensive).
        /// When false, uses simulated glass effect with overlays.
        /// </summary>
        public bool EnableBackdropBlur
        {
            get => (bool)GetValue(EnableBackdropBlurProperty);
            set => SetValue(EnableBackdropBlurProperty, value);
        }
        #endregion

        #region BlurMode
        public static readonly DependencyProperty BlurModeProperty =
            DependencyProperty.Register(
                nameof(BlurMode),
                typeof(GlassBlurMode),
                typeof(DaisyCard),
                new PropertyMetadata(GlassBlurMode.Simulated, OnBlurPropertyChanged));

        /// <summary>
        /// The blur rendering mode for glass effect.
        /// </summary>
        public GlassBlurMode BlurMode
        {
            get => (GlassBlurMode)GetValue(BlurModeProperty);
            set => SetValue(BlurModeProperty, value);
        }
        #endregion

        #region GlassSaturation
        public static readonly DependencyProperty GlassSaturationProperty =
            DependencyProperty.Register(
                nameof(GlassSaturation),
                typeof(double),
                typeof(DaisyCard),
                new PropertyMetadata(1.0, OnBlurPropertyChanged));

        /// <summary>
        /// The saturation of the glass background (0.0 = grayscale, 1.0 = normal).
        /// Only effective when BlurMode is BitmapCapture or SkiaSharp.
        /// </summary>
        public double GlassSaturation
        {
            get => (double)GetValue(GlassSaturationProperty);
            set => SetValue(GlassSaturationProperty, value);
        }
        #endregion

        private static void OnBlurPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCard card)
            {
                card._needsUpdate = true;
                card.ApplyAll();
                card.ScheduleBackdropCapture();
            }
        }

        private void ApplyAll()
        {
            if (!_isTemplateApplied) return;

            _isInternalColorSet = false;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // WinUI/Uno doesn't support TextElement.FontSize attached properties.
            // If Size is set, we prefer its scaled font size over BodyFontSize.
            if (ReadLocalValue(SizeProperty) != DependencyProperty.UnsetValue || FlowerySizeManager.UseGlobalSizeByDefault)
            {
                FontSize = FlowerySizeManager.GetFontSizeForTier(ResponsiveFontTier.Primary, Size);
            }
            else
            {
                FontSize = BodyFontSize;
            }

            if (ShouldApplyThemeColors())
            {
                ApplyThemeColors();
            }
            ApplyVariantDefaults();
            ApplyStyleEffects();
            ApplyGlassMode();
            _isInternalColorSet = true;
        }

        protected virtual bool ShouldApplyThemeColors()
        {
            return true;
        }

        private void ApplyStyleEffects()
        {
            if (_solidBackground == null) return;

            // Handle IsGlass legacy property integration
            if (IsGlass && CardStyle == DaisyCardStyle.Default)
            {
                // Temporarily treat as Glass style if IsGlass is set
            }

            var effectiveStyle = IsGlass ? DaisyCardStyle.Glass : CardStyle;

            switch (effectiveStyle)
            {
                case DaisyCardStyle.Beveled:
                    _solidBackground.BorderThickness = new Thickness(1);
                    _solidBackground.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255)); // Subtle light rim
                    _solidBackground.CornerRadius = new CornerRadius(4);

                    if (ReadLocalValue(BackgroundProperty) == DependencyProperty.UnsetValue)
                        Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");

                    if (_beveledShadow != null)
                    {
                        _beveledShadow.CornerRadius = _solidBackground.CornerRadius;
                        _beveledShadow.Visibility = Visibility.Visible;
                    }

                    if (_innerHighlight != null)
                    {
                        _innerHighlight.Visibility = Visibility.Visible;
                        _innerHighlight.BorderThickness = new Thickness(1.5, 1.5, 0, 0);
                        _innerHighlight.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(64, 255, 255, 255)); // 0.25 highlight
                        _innerHighlight.CornerRadius = _solidBackground.CornerRadius;
                    }

                    if (_innerShadow != null)
                    {
                        _innerShadow.Visibility = Visibility.Visible;
                        _innerShadow.BorderThickness = new Thickness(0, 0, 1.5, 1.5);
                        _innerShadow.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(90, 0, 0, 0)); // 0.35 shadow
                        _innerShadow.CornerRadius = _solidBackground.CornerRadius;
                    }
                    break;

                case DaisyCardStyle.Inset:
                    _solidBackground.BorderThickness = new Thickness(1);
                    _solidBackground.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
                    _solidBackground.CornerRadius = new CornerRadius(4);

                    if (ReadLocalValue(BackgroundProperty) == DependencyProperty.UnsetValue)
                        Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");

                    if (_beveledShadow != null) _beveledShadow.Visibility = Visibility.Collapsed;

                    if (_innerHighlight != null)
                    {
                        _innerHighlight.Visibility = Visibility.Visible;
                        _innerHighlight.BorderThickness = new Thickness(0, 0, 1.5, 1.5);
                        _innerHighlight.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(38, 255, 255, 255)); // 0.15 highlight
                        _innerHighlight.CornerRadius = _solidBackground.CornerRadius;
                    }

                    if (_innerShadow != null)
                    {
                        _innerShadow.Visibility = Visibility.Visible;
                        _innerShadow.BorderThickness = new Thickness(1.5, 1.5, 0, 0);
                        _innerShadow.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(115, 0, 0, 0)); // 0.45 shadow
                        _innerShadow.CornerRadius = _solidBackground.CornerRadius;
                    }
                    break;

                case DaisyCardStyle.Panel:
                    _solidBackground.BorderThickness = new Thickness(1);
                    _solidBackground.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
                    _solidBackground.CornerRadius = new CornerRadius(2);

                    if (ReadLocalValue(BackgroundProperty) == DependencyProperty.UnsetValue)
                        Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");

                    if (_beveledShadow != null) _beveledShadow.Visibility = Visibility.Collapsed;

                    // Panels get a very subtle inner rim to look "machined"
                    if (_innerHighlight != null)
                    {
                        _innerHighlight.Visibility = Visibility.Visible;
                        _innerHighlight.BorderThickness = new Thickness(1);
                        _innerHighlight.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(20, 255, 255, 255));
                        _innerHighlight.CornerRadius = _solidBackground.CornerRadius;
                    }
                    if (_innerShadow != null) _innerShadow.Visibility = Visibility.Collapsed;
                    break;

                case DaisyCardStyle.Glass:
                    // Handled in ApplyGlassMode
                    break;

                default: // Default / Flat
                    if (ReadLocalValue(CornerRadiusProperty) == DependencyProperty.UnsetValue)
                        _solidBackground.CornerRadius = new CornerRadius(16);
                    if (ReadLocalValue(BorderThicknessProperty) == DependencyProperty.UnsetValue)
                        _solidBackground.BorderThickness = new Thickness(0);

                    if (_beveledShadow != null) _beveledShadow.Visibility = Visibility.Collapsed;
                    if (_innerHighlight != null) _innerHighlight.Visibility = Visibility.Collapsed;
                    if (_innerShadow != null) _innerShadow.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        // Cache the detected palette name for custom backgrounds (color is constant, only brushes change with theme)
        private string? _detectedPaletteName;

        private void ApplyThemeColors()
        {
            if (ColorVariant == DaisyColor.Default)
            {
                // Default variant follows the standard Base100 background and BaseContent foreground.
                // We prefer ClearValue to let the ControlTheme/Style's ThemeResources handle this
                // so it reflows naturally with theme switching.

                // Check if user has set a custom Background - if so, auto-detect foreground
                // We detect this by checking if Background is set AND we've already detected a palette name,
                // OR if this is a fresh Background that matches a known palette color
                var hasUserBackground = ReadLocalValue(BackgroundProperty) != DependencyProperty.UnsetValue;
                
                if (hasUserBackground && Background is SolidColorBrush customBgBrush)
                {
                    // Detect palette from custom background and apply matching foreground
                    var detectedPalette = _detectedPaletteName ?? DaisyResourceLookup.GetPaletteNameForColor(customBgBrush.Color);
                    
                    if (detectedPalette != null)
                    {
                        // Cache for future theme changes
                        _detectedPaletteName = detectedPalette;
                        
                        // Get fresh brushes from current theme using detected palette
                        var (freshBackground, freshContentBrush) = DaisyResourceLookup.GetPaletteBrushes(detectedPalette);
                        
                        // Update background brush with fresh theme value
                        if (freshBackground != null)
                            Background = freshBackground;
                        
                        // Auto-apply foreground if user hasn't explicitly set it
                        if (ReadLocalValue(ForegroundProperty) == DependencyProperty.UnsetValue)
                        {
                            Foreground = freshContentBrush ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
                        }
                        return;
                    }
                }

                if (ReadLocalValue(BackgroundProperty) == DependencyProperty.UnsetValue || !_isInternalColorSet)
                {
                    var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyCard", "Background");
                    if (bgOverride != null) Background = bgOverride;
                    else ClearValue(BackgroundProperty);
                }

                if (ReadLocalValue(ForegroundProperty) == DependencyProperty.UnsetValue || !_isInternalColorSet)
                {
                    var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyCard", "Foreground");
                    if (fgOverride != null) Foreground = fgOverride;
                    else ClearValue(ForegroundProperty);
                }
                return;
            }

            // Map the variant to its semantic brush names.
            // Base200 and Base300 don't have their own content brushes; they use BaseContent.
            string variantName = ColorVariant.ToString();
            string contentVariantName = ColorVariant switch
            {
                DaisyColor.Base200 or DaisyColor.Base300 => "Base",
                _ => variantName
            };

            if (ReadLocalValue(BackgroundProperty) == DependencyProperty.UnsetValue || !_isInternalColorSet)
            {
                Background = DaisyResourceLookup.GetBrush($"Daisy{variantName}Brush");
            }

            if (ReadLocalValue(ForegroundProperty) == DependencyProperty.UnsetValue || !_isInternalColorSet)
            {
                Foreground = DaisyResourceLookup.GetBrush($"Daisy{contentVariantName}ContentBrush");
            }
        }

        private void ApplyVariantDefaults()
        {
            // Compact variant sets BodyPadding based on Size (unless overridden locally).
            if (Variant == DaisyCardVariant.Compact)
            {
                if (ReadLocalValue(BodyPaddingProperty) == DependencyProperty.UnsetValue)
                {
                    double p = Size switch
                    {
                        DaisySize.ExtraSmall => 8,
                        DaisySize.Small => 12,
                        DaisySize.Large => 20,
                        DaisySize.ExtraLarge => 24,
                        _ => 16
                    };
                    BodyPadding = new Thickness(p);
                }
            }
            else
            {
                if (ReadLocalValue(BodyPaddingProperty) == DependencyProperty.UnsetValue)
                {
                    double p = Size switch
                    {
                        DaisySize.ExtraSmall => 16,
                        DaisySize.Small => 24,
                        DaisySize.Large => 40,
                        DaisySize.ExtraLarge => 48,
                        _ => 32
                    };
                    BodyPadding = new Thickness(p);
                }
            }
        }

        private void ApplyGlassMode()
        {
            if (_solidBackground == null) return;

            bool shouldBeGlass = IsGlass || CardStyle == DaisyCardStyle.Glass;

            // Manage blur backdrop visibility
            UpdateBlurModeVisibility();

            if (shouldBeGlass)
            {
                // Store original background if not already stored
                _originalBackground ??= _solidBackground.Background;

                // Apply glass effect using a semi-transparent brush with tint
                // We use GlassTint and GlassOpacity for the main fill
                _solidBackground.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)(255 * GlassOpacity),
                    GlassTint.R,
                    GlassTint.G,
                    GlassTint.B
                ));

                // Apply glass border
                _solidBackground.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White)
                {
                    Opacity = GlassBorderOpacity
                };
                _solidBackground.BorderThickness = new Thickness(1);

                // High-fidelity glass reflection (Top-Left Highlight)
                if (_innerHighlight != null)
                {
                    _innerHighlight.Visibility = Visibility.Visible;
                    _innerHighlight.BorderThickness = new Thickness(1, 1, 0, 0);
                    _innerHighlight.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White)
                    {
                        Opacity = GlassReflectOpacity
                    };
                    _innerHighlight.CornerRadius = _solidBackground.CornerRadius;
                }

                // High-fidelity glass depth (Bottom-Right Shadow)
                if (_innerShadow != null)
                {
                    _innerShadow.Visibility = Visibility.Visible;
                    _innerShadow.BorderThickness = new Thickness(0, 0, 1, 1);
                    _innerShadow.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black)
                    {
                        Opacity = 0.05 // Very subtle depth
                    };
                    _innerShadow.CornerRadius = _solidBackground.CornerRadius;
                }
            }
            else
            {
                // Restore original background if we were previously in glass mode
                if (_originalBackground != null)
                {
                    _solidBackground.Background = _originalBackground;
                }

                // ApplyStyleEffects already handles non-glass styles,
                // but we should ensure the inner layers are hidden if not needed
                if (CardStyle != DaisyCardStyle.Beveled && CardStyle != DaisyCardStyle.Inset && CardStyle != DaisyCardStyle.Panel)
                {
                    if (_innerHighlight != null) _innerHighlight.Visibility = Visibility.Collapsed;
                    if (_innerShadow != null) _innerShadow.Visibility = Visibility.Collapsed;
                }
            }
        }

        #region Backdrop Blur

        private void UpdateBlurModeVisibility()
        {
            if (_blurredBackdrop == null || _solidBackground == null)
                return;

            bool shouldBeGlass = IsGlass || CardStyle == DaisyCardStyle.Glass;
            bool useBlurMode = shouldBeGlass && EnableBackdropBlur && BlurMode != GlassBlurMode.Simulated;

            // Show blurred backdrop image when using blur modes
            _blurredBackdrop.Visibility = useBlurMode ? Visibility.Visible : Visibility.Collapsed;

            // When using blur mode with a captured background, we can make the solid background
            // more transparent since the blur provides the visual effect
            if (useBlurMode && _blurredBackdrop.Source != null)
            {
                _solidBackground.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)(255 * GlassTintOpacity),
                    GlassTint.R,
                    GlassTint.G,
                    GlassTint.B
                ));
            }
        }

        private void ScheduleBackdropCapture()
        {
            bool shouldBeGlass = IsGlass || CardStyle == DaisyCardStyle.Glass;
            if (!shouldBeGlass || !EnableBackdropBlur || BlurMode == GlassBlurMode.Simulated)
                return;

            if (_isCapturing || !_needsUpdate)
                return;

            // Defer capture to allow layout to complete
            DispatcherQueue?.TryEnqueue(async () => await CaptureAndBlurBackdropAsync());
        }

        private async System.Threading.Tasks.Task CaptureAndBlurBackdropAsync()
        {
            bool shouldBeGlass = IsGlass || CardStyle == DaisyCardStyle.Glass;
            if (_isCapturing || !shouldBeGlass || !EnableBackdropBlur || BlurMode == GlassBlurMode.Simulated)
                return;

            if (_blurredBackdrop == null)
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

                var cornerRadius = _solidBackground?.CornerRadius.TopLeft ?? CornerRadius.TopLeft;
                var glassImageSource = await SkiaBackdropCapture.CaptureAndApplyGlassEffectAsync(
                    this,
                    GlassBlur,
                    GlassSaturation,
                    tintColor,
                    GlassTintOpacity,
                    cornerRadius);

                if (glassImageSource != null)
                {
                    _blurredBackdrop.Source = glassImageSource;
                    UpdateBlurModeVisibility();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DaisyCard] Backdrop capture failed: {ex.Message}");
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// Manually refresh the backdrop blur effect.
        /// Call this after the background behind the card has changed.
        /// </summary>
        public void RefreshBackdrop()
        {
            bool shouldBeGlass = IsGlass || CardStyle == DaisyCardStyle.Glass;
            if (shouldBeGlass && EnableBackdropBlur && BlurMode != GlassBlurMode.Simulated)
            {
                _needsUpdate = true;
                ScheduleBackdropCapture();
            }
        }

        #endregion
    }
}

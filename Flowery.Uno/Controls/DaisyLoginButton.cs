namespace Flowery.Controls
{
    public enum DaisyLoginBrand
    {
        Email,
        GitHub,
        Google,
        Facebook,
        X,
        Kakao,
        Apple,
        Amazon,
        Microsoft,
        Line,
        Slack,
        LinkedIn,
        VK,
        WeChat,
        MetaMask
    }

    /// <summary>
    /// A DaisyButton preconfigured for brand login actions (icon + label + colors).
    /// </summary>
    public partial class DaisyLoginButton : DaisyButton
    {
        private static readonly string[] BrandResourceKeys =
        {
            "DaisyButtonBackground",
            "DaisyButtonBackgroundPointerOver",
            "DaisyButtonBackgroundPressed",
            "DaisyButtonForeground",
            "DaisyButtonForegroundPointerOver",
            "DaisyButtonForegroundPressed",
            "DaisyButtonBorderBrush",
            "DaisyButtonBorderBrushPointerOver",
            "DaisyButtonBorderBrushPressed"
        };

        private TextBlock? _labelTextBlock;
        private List<Path>? _iconPaths;
        public static readonly DependencyProperty BrandProperty =
            DependencyProperty.Register(
                nameof(Brand),
                typeof(DaisyLoginBrand),
                typeof(DaisyLoginButton),
                new PropertyMetadata(DaisyLoginBrand.Email, OnBrandChanged));

        /// <summary>
        /// Gets or sets the brand preset used for icon and colors.
        /// </summary>
        public DaisyLoginBrand Brand
        {
            get => (DaisyLoginBrand)GetValue(BrandProperty);
            set => SetValue(BrandProperty, value);
        }

        public static readonly DependencyProperty LoginTextProperty =
            DependencyProperty.Register(
                nameof(LoginText),
                typeof(string),
                typeof(DaisyLoginButton),
                new PropertyMetadata(null, OnContentPropertyChanged));

        /// <summary>
        /// Gets or sets the button label. If null, a brand default label is used.
        /// </summary>
        public string? LoginText
        {
            get => (string?)GetValue(LoginTextProperty);
            set => SetValue(LoginTextProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                nameof(IconSize),
                typeof(double),
                typeof(DaisyLoginButton),
                new PropertyMetadata(double.NaN, OnContentPropertyChanged));

        /// <summary>
        /// Gets or sets the icon size in pixels. If NaN, uses the default size for the button Size.
        /// </summary>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty IconSpacingProperty =
            DependencyProperty.Register(
                nameof(IconSpacing),
                typeof(double),
                typeof(DaisyLoginButton),
                new PropertyMetadata(double.NaN, OnContentPropertyChanged));

        /// <summary>
        /// Gets or sets the spacing between icon and label. If NaN, uses the default spacing for the button Size.
        /// </summary>
        public double IconSpacing
        {
            get => (double)GetValue(IconSpacingProperty);
            set => SetValue(IconSpacingProperty, value);
        }

        public static readonly DependencyProperty UseBrandColorsProperty =
            DependencyProperty.Register(
                nameof(UseBrandColors),
                typeof(bool),
                typeof(DaisyLoginButton),
                new PropertyMetadata(true, OnAppearancePropertyChanged));

        /// <summary>
        /// Gets or sets whether the button uses brand-specific colors or the current theme.
        /// </summary>
        public bool UseBrandColors
        {
            get => (bool)GetValue(UseBrandColorsProperty);
            set => SetValue(UseBrandColorsProperty, value);
        }

        public DaisyLoginButton()
        {
            BorderThickness = new Thickness(1);
            RegisterPropertyChangedCallback(SizeProperty, (_, _) => BuildContent());
            RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundPropertyChanged);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            UpdateBrandPresentation();
        }

        private static void OnBrandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyLoginButton button)
            {
                button.UpdateBrandPresentation();
            }
        }

        private static void OnAppearancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyLoginButton button)
            {
                button.UpdateBrandPresentation();
            }
        }

        private static void OnContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyLoginButton button)
            {
                button.BuildContent();
            }
        }

        private void UpdateBrandPresentation()
        {
            ApplyBrandResources();
            BuildContent();
            RefreshButtonVisuals();
            if (!UseBrandColors)
            {
                OnForegroundPropertyChanged(this, ForegroundProperty);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            FloweryLocalization.CultureChanged += OnCultureChanged;
            UpdateBrandPresentation();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;
            FloweryLocalization.CultureChanged -= OnCultureChanged;
        }

        private void OnThemeChanged(object? sender, string themeName)
        {
            if (UseBrandColors)
                return;

            UpdateBrandPresentation();
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            if (LoginText != null)
                return;

            BuildContent();
        }

        private void ApplyBrandResources()
        {
            if (!UseBrandColors)
            {
                ClearBrandResources();
                return;
            }

            var definition = GetBrandDefinition(Brand);

            SetBrandBrush("DaisyButtonBackground", definition.Background);
            SetBrandBrush("DaisyButtonBackgroundPointerOver", definition.Background);
            SetBrandBrush("DaisyButtonBackgroundPressed", definition.Background);

            SetBrandBrush("DaisyButtonForeground", definition.Foreground);
            SetBrandBrush("DaisyButtonForegroundPointerOver", definition.Foreground);
            SetBrandBrush("DaisyButtonForegroundPressed", definition.Foreground);

            SetBrandBrush("DaisyButtonBorderBrush", definition.Border);
            SetBrandBrush("DaisyButtonBorderBrushPointerOver", definition.Border);
            SetBrandBrush("DaisyButtonBorderBrushPressed", definition.Border);
        }

        private void ClearBrandResources()
        {
            foreach (var key in BrandResourceKeys)
            {
                Resources.Remove(key);
            }
        }

        private void SetBrandBrush(string resourceKey, Color color)
        {
            Resources[resourceKey] = new SolidColorBrush(color);
        }

        private void BuildContent()
        {
            var definition = GetBrandDefinition(Brand);
            var label = GetEffectiveLabel(definition);
            var iconSize = GetEffectiveIconSize();
            var spacing = GetEffectiveIconSpacing();
            var foregroundBrush = GetEffectiveForegroundBrush(definition);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = spacing
            };

            _iconPaths = null;
            var icon = CreateBrandIconElement(definition, iconSize, foregroundBrush, out var iconPaths);
            if (icon != null)
            {
                _iconPaths = iconPaths;
                panel.Children.Add(icon);
            }

            if (!string.IsNullOrWhiteSpace(label))
            {
                _labelTextBlock = new TextBlock
                {
                    Text = label,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = foregroundBrush
                };
                panel.Children.Add(_labelTextBlock);
            }
            else
            {
                _labelTextBlock = null;
            }

            Content = panel;
        }

        private string? GetEffectiveLabel(BrandDefinition definition)
        {
            if (LoginText is { } customText)
                return customText;

            return FloweryLocalization.GetStringInternal(definition.DefaultTextKey, definition.DefaultTextFallback);
        }

        private double GetEffectiveIconSize()
        {
            if (!double.IsNaN(IconSize))
                return IconSize;

            return DaisyResourceLookup.GetDefaultIconSize(Size);
        }

        private double GetEffectiveIconSpacing()
        {
            if (!double.IsNaN(IconSpacing))
                return IconSpacing;

            return DaisyResourceLookup.GetDefaultIconSpacing(Size);
        }

        private Brush GetEffectiveForegroundBrush(BrandDefinition definition)
        {
            if (UseBrandColors)
                return new SolidColorBrush(definition.Foreground);

            return Foreground
                   ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush",
                       new SolidColorBrush(Colors.Black));
        }

        private Viewbox? CreateBrandIconElement(BrandDefinition definition, double iconSize, Brush foregroundBrush,
            out List<Path> iconPaths)
        {
            iconPaths = new List<Path>();
            if (iconSize <= 0)
                return null;

            var viewbox = new Viewbox
            {
                Width = iconSize,
                Height = iconSize,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false
            };

            var canvas = new Canvas
            {
                Width = 96,
                Height = 96,
                RenderTransform = FloweryPathHelpers.CreateNormalizedTransform(
                    definition.Icon.ViewBoxWidth,
                    definition.Icon.ViewBoxHeight,
                    96)
            };

            viewbox.Child = canvas;

            foreach (var part in definition.Icon.Parts)
            {
                var path = CreateBrandPartPath(part, foregroundBrush);
                if (path != null)
                {
                    canvas.Children.Add(path);
                    iconPaths.Add(path);
                }
            }

            return viewbox;
        }

        private Path? CreateBrandPartPath(BrandIconPart part, Brush foregroundBrush)
        {
            var pathData = FloweryPathHelpers.GetIconPathData(part.PathKey);
            if (string.IsNullOrWhiteSpace(pathData))
                return null;

            var path = new Path
            {
                Data = FloweryPathHelpers.ParseGeometry(pathData),
                IsHitTestVisible = false
            };

            if (UseBrandColors && part.Fill is { } fillColor)
            {
                path.Fill = new SolidColorBrush(fillColor);
            }
            else if (part.StrokeThickness <= 0)
            {
                path.Fill = foregroundBrush;
            }

            if (part.StrokeThickness > 0)
            {
                path.StrokeThickness = part.StrokeThickness;
                path.Stroke = UseBrandColors && part.Stroke is { } strokeColor
                    ? new SolidColorBrush(strokeColor)
                    : foregroundBrush;

                if (part.RoundJoin)
                    path.StrokeLineJoin = PenLineJoin.Round;

                if (part.RoundCaps)
                {
                    path.StrokeStartLineCap = PenLineCap.Round;
                    path.StrokeEndLineCap = PenLineCap.Round;
                }
            }

            if (part.FillRule is { } fillRule)
            {
                switch (path.Data)
                {
                    case PathGeometry pathGeometry:
                        pathGeometry.FillRule = fillRule;
                        break;
                    case GeometryGroup geometryGroup:
                        geometryGroup.FillRule = fillRule;
                        break;
                }
            }

            return path;
        }

        private void RefreshButtonVisuals()
        {
            if (!IsLoaded)
                return;

            var currentVariant = Variant;
            var temporaryVariant = currentVariant == DaisyButtonVariant.Default
                ? DaisyButtonVariant.Primary
                : DaisyButtonVariant.Default;

            SetValue(VariantProperty, temporaryVariant);
            SetValue(VariantProperty, currentVariant);
        }

        private void OnForegroundPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (UseBrandColors)
                return;

            var definition = GetBrandDefinition(Brand);
            var foregroundBrush = GetEffectiveForegroundBrush(definition);

            if (_labelTextBlock != null)
                _labelTextBlock.Foreground = foregroundBrush;

            if (_iconPaths != null)
            {
                foreach (var path in _iconPaths)
                {
                    if (path.StrokeThickness > 0)
                        path.Stroke = foregroundBrush;
                    else
                        path.Fill = foregroundBrush;
                }
            }
        }

        private static BrandDefinition GetBrandDefinition(DaisyLoginBrand brand)
        {
            return BrandDefinitions.TryGetValue(brand, out var definition)
                ? definition
                : BrandDefinitions[DaisyLoginBrand.Email];
        }

        private static readonly IReadOnlyDictionary<DaisyLoginBrand, BrandDefinition> BrandDefinitions =
            new Dictionary<DaisyLoginBrand, BrandDefinition>
            {
                [DaisyLoginBrand.Email] = new BrandDefinition(
                    "LoginButton_Email",
                    "Login with Email",
                    "#FFFFFF",
                    "#000000",
                    "#E5E5E5",
                    new BrandIconDefinition(24, 24,
                        new BrandIconPart("DaisyIconBrandEmailOutline", strokeThickness: 2, roundCaps: true, roundJoin: true),
                        new BrandIconPart("DaisyIconBrandEmailFlap", strokeThickness: 2, roundCaps: true, roundJoin: true))),
                [DaisyLoginBrand.GitHub] = new BrandDefinition(
                    "LoginButton_GitHub",
                    "Login with GitHub",
                    "#000000",
                    "#FFFFFF",
                    "#000000",
                    new BrandIconDefinition(24, 24,
                        new BrandIconPart("DaisyIconBrandGitHub"))),
                [DaisyLoginBrand.Google] = new BrandDefinition(
                    "LoginButton_Google",
                    "Login with Google",
                    "#FFFFFF",
                    "#000000",
                    "#E5E5E5",
                    new BrandIconDefinition(512, 512,
                        new BrandIconPart("DaisyIconBrandGoogleGreen", fillHex: "#34A853"),
                        new BrandIconPart("DaisyIconBrandGoogleBlue", fillHex: "#4285F4"),
                        new BrandIconPart("DaisyIconBrandGoogleYellow", fillHex: "#FBBC02"),
                        new BrandIconPart("DaisyIconBrandGoogleRed", fillHex: "#EA4335"))),
                [DaisyLoginBrand.Facebook] = new BrandDefinition(
                    "LoginButton_Facebook",
                    "Login with Facebook",
                    "#1A77F2",
                    "#FFFFFF",
                    "#005FD8",
                    new BrandIconDefinition(32, 32,
                        new BrandIconPart("DaisyIconBrandFacebook"))),
                [DaisyLoginBrand.X] = new BrandDefinition(
                    "LoginButton_X",
                    "Login with X",
                    "#000000",
                    "#FFFFFF",
                    "#000000",
                    new BrandIconDefinition(300, 271,
                        new BrandIconPart("DaisyIconBrandX"))),
                [DaisyLoginBrand.Kakao] = new BrandDefinition(
                    "LoginButton_Kakao",
                    "카카오 로그인",
                    "#FEE502",
                    "#181600",
                    "#F1D800",
                    new BrandIconDefinition(512, 512,
                        new BrandIconPart("DaisyIconBrandKakao"))),
                [DaisyLoginBrand.Apple] = new BrandDefinition(
                    "LoginButton_Apple",
                    "Login with Apple",
                    "#000000",
                    "#FFFFFF",
                    "#000000",
                    new BrandIconDefinition(1195, 1195,
                        new BrandIconPart("DaisyIconBrandApple"))),
                [DaisyLoginBrand.Amazon] = new BrandDefinition(
                    "LoginButton_Amazon",
                    "Login with Amazon",
                    "#FF9900",
                    "#000000",
                    "#E17D00",
                    new BrandIconDefinition(16, 16,
                        new BrandIconPart("DaisyIconBrandAmazon"))),
                [DaisyLoginBrand.Microsoft] = new BrandDefinition(
                    "LoginButton_Microsoft",
                    "Login with Microsoft",
                    "#2F2F2F",
                    "#FFFFFF",
                    "#000000",
                    new BrandIconDefinition(512, 512,
                        new BrandIconPart("DaisyIconBrandMicrosoftRed", fillHex: "#F24F23"),
                        new BrandIconPart("DaisyIconBrandMicrosoftGreen", fillHex: "#7EBA03"),
                        new BrandIconPart("DaisyIconBrandMicrosoftBlue", fillHex: "#3CA4EF"),
                        new BrandIconPart("DaisyIconBrandMicrosoftYellow", fillHex: "#F9BA00"))),
                [DaisyLoginBrand.Line] = new BrandDefinition(
                    "LoginButton_Line",
                    "LINEでログイン",
                    "#03C755",
                    "#FFFFFF",
                    "#00B544",
                    new BrandIconDefinition(16, 16,
                        new BrandIconPart("DaisyIconBrandLine"))),
                [DaisyLoginBrand.Slack] = new BrandDefinition(
                    "LoginButton_Slack",
                    "Login with Slack",
                    "#622069",
                    "#FFFFFF",
                    "#591660",
                    new BrandIconDefinition(512, 512,
                        new BrandIconPart("DaisyIconBrandSlackBlue", strokeHex: "#36C5F0", strokeThickness: 78, roundCaps: true, roundJoin: true),
                        new BrandIconPart("DaisyIconBrandSlackGreen", strokeHex: "#2EB67D", strokeThickness: 78, roundCaps: true, roundJoin: true),
                        new BrandIconPart("DaisyIconBrandSlackYellow", strokeHex: "#ECB22E", strokeThickness: 78, roundCaps: true, roundJoin: true),
                        new BrandIconPart("DaisyIconBrandSlackPink", strokeHex: "#E01E5A", strokeThickness: 78, roundCaps: true, roundJoin: true))),
                [DaisyLoginBrand.LinkedIn] = new BrandDefinition(
                    "LoginButton_LinkedIn",
                    "Login with LinkedIn",
                    "#0967C2",
                    "#FFFFFF",
                    "#0059B3",
                    new BrandIconDefinition(32, 32,
                        new BrandIconPart("DaisyIconBrandLinkedIn", fillRule: FillRule.EvenOdd))),
                [DaisyLoginBrand.VK] = new BrandDefinition(
                    "LoginButton_VK",
                    "Login with VK",
                    "#47698F",
                    "#FFFFFF",
                    "#35567B",
                    new BrandIconDefinition(2240, 2240,
                        new BrandIconPart("DaisyIconBrandVK"))),
                [DaisyLoginBrand.WeChat] = new BrandDefinition(
                    "LoginButton_WeChat",
                    "Login with WeChat",
                    "#5EBB2B",
                    "#FFFFFF",
                    "#4EAA0C",
                    new BrandIconDefinition(32, 32,
                        new BrandIconPart("DaisyIconBrandWeChat"))),
                [DaisyLoginBrand.MetaMask] = new BrandDefinition(
                    "LoginButton_MetaMask",
                    "Login with MetaMask",
                    "#FFFFFF",
                    "#000000",
                    "#E5E5E5",
                    new BrandIconDefinition(507.83, 470.86,
                        new BrandIconPart("DaisyIconBrandMetaMask01", fillHex: "#E2761B"),
                        new BrandIconPart("DaisyIconBrandMetaMask02", fillHex: "#E4761B"),
                        new BrandIconPart("DaisyIconBrandMetaMask03", fillHex: "#E4761B"),
                        new BrandIconPart("DaisyIconBrandMetaMask04", fillHex: "#D7C1B3"),
                        new BrandIconPart("DaisyIconBrandMetaMask05", fillHex: "#233447"),
                        new BrandIconPart("DaisyIconBrandMetaMask06", fillHex: "#CD6116"),
                        new BrandIconPart("DaisyIconBrandMetaMask07", fillHex: "#E4751F"),
                        new BrandIconPart("DaisyIconBrandMetaMask08", fillHex: "#F6851B"),
                        new BrandIconPart("DaisyIconBrandMetaMask09", fillHex: "#C0AD9E"),
                        new BrandIconPart("DaisyIconBrandMetaMask10", fillHex: "#161616"),
                        new BrandIconPart("DaisyIconBrandMetaMask11", fillHex: "#763D16"),
                        new BrandIconPart("DaisyIconBrandMetaMask12", fillHex: "#F6851B")))
            };

        private sealed class BrandDefinition
        {
            public BrandDefinition(string defaultTextKey, string defaultTextFallback, string backgroundHex,
                string foregroundHex, string borderHex,
                BrandIconDefinition icon)
            {
                DefaultTextKey = defaultTextKey;
                DefaultTextFallback = defaultTextFallback;
                Background = FloweryColorHelpers.ColorFromHex(backgroundHex);
                Foreground = FloweryColorHelpers.ColorFromHex(foregroundHex);
                Border = FloweryColorHelpers.ColorFromHex(borderHex);
                Icon = icon;
            }

            public string DefaultTextKey { get; }
            public string DefaultTextFallback { get; }
            public Color Background { get; }
            public Color Foreground { get; }
            public Color Border { get; }
            public BrandIconDefinition Icon { get; }
        }

        private sealed class BrandIconDefinition
        {
            public BrandIconDefinition(double viewBoxWidth, double viewBoxHeight, params BrandIconPart[] parts)
            {
                ViewBoxWidth = viewBoxWidth;
                ViewBoxHeight = viewBoxHeight;
                Parts = parts;
            }

            public double ViewBoxWidth { get; }
            public double ViewBoxHeight { get; }
            public BrandIconPart[] Parts { get; }
        }

        private sealed class BrandIconPart
        {
            public BrandIconPart(string pathKey, string? fillHex = null, string? strokeHex = null,
                double strokeThickness = 0, bool roundCaps = false, bool roundJoin = false, FillRule? fillRule = null)
            {
                PathKey = pathKey;
                Fill = string.IsNullOrWhiteSpace(fillHex) ? null : FloweryColorHelpers.ColorFromHex(fillHex);
                Stroke = string.IsNullOrWhiteSpace(strokeHex) ? null : FloweryColorHelpers.ColorFromHex(strokeHex);
                StrokeThickness = strokeThickness;
                RoundCaps = roundCaps;
                RoundJoin = roundJoin;
                FillRule = fillRule;
            }

            public string PathKey { get; }
            public Color? Fill { get; }
            public Color? Stroke { get; }
            public double StrokeThickness { get; }
            public bool RoundCaps { get; }
            public bool RoundJoin { get; }
            public FillRule? FillRule { get; }
        }
    }
}

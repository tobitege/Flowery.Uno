using Flowery.Enums;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// A themed wrapper around WinUI's BreadcrumbBar that uses Daisy tokens for styling and sizing.
    /// </summary>
    public partial class DaisyBreadcrumbBar : DaisyBaseContentControl
    {
        private Border? _rootBorder;
        private BreadcrumbBar? _breadcrumbBar;
        private long _paddingChangedToken;
        private long _backgroundChangedToken;
        private long _borderBrushChangedToken;
        private long _borderThicknessChangedToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="DaisyBreadcrumbBar"/> class.
        /// </summary>
        public DaisyBreadcrumbBar()
        {
            DefaultStyleKey = typeof(DaisyBreadcrumbBar);
            IsTabStop = false;

            if (FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.GetIgnoreGlobalSize(this))
            {
                Size = FlowerySizeManager.CurrentSize;
            }
        }

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="ItemsSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(object),
                typeof(DaisyBreadcrumbBar),
                new PropertyMetadata(null, OnItemsSourceChanged));

        /// <summary>
        /// Gets or sets the items source for the breadcrumb bar.
        /// </summary>
        public object? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(
                nameof(ItemTemplate),
                typeof(DataTemplate),
                typeof(DaisyBreadcrumbBar),
                new PropertyMetadata(null, OnItemTemplateChanged));

        /// <summary>
        /// Gets or sets the data template used to display breadcrumb items.
        /// </summary>
        public DataTemplate? ItemTemplate
        {
            get => (DataTemplate?)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyBreadcrumbBar),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of the breadcrumb bar.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        /// <summary>
        /// Occurs when an item is clicked in the breadcrumb bar.
        /// </summary>
        public event TypedEventHandler<DaisyBreadcrumbBar, BreadcrumbBarItemClickedEventArgs>? ItemClicked;

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_breadcrumbBar == null)
            {
                BuildVisualTree();
            }

            ApplyAll();

            if (_paddingChangedToken == 0)
            {
                _paddingChangedToken = RegisterPropertyChangedCallback(Control.PaddingProperty, OnPaddingChanged);
            }

            if (_backgroundChangedToken == 0)
            {
                _backgroundChangedToken = RegisterPropertyChangedCallback(Control.BackgroundProperty, OnBackgroundChanged);
            }

            if (_borderBrushChangedToken == 0)
            {
                _borderBrushChangedToken = RegisterPropertyChangedCallback(Control.BorderBrushProperty, OnBorderBrushChanged);
            }

            if (_borderThicknessChangedToken == 0)
            {
                _borderThicknessChangedToken = RegisterPropertyChangedCallback(Control.BorderThicknessProperty, OnBorderThicknessChanged);
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            if (_paddingChangedToken != 0)
            {
                UnregisterPropertyChangedCallback(Control.PaddingProperty, _paddingChangedToken);
                _paddingChangedToken = 0;
            }

            if (_backgroundChangedToken != 0)
            {
                UnregisterPropertyChangedCallback(Control.BackgroundProperty, _backgroundChangedToken);
                _backgroundChangedToken = 0;
            }

            if (_borderBrushChangedToken != 0)
            {
                UnregisterPropertyChangedCallback(Control.BorderBrushProperty, _borderBrushChangedToken);
                _borderBrushChangedToken = 0;
            }

            if (_borderThicknessChangedToken != 0)
            {
                UnregisterPropertyChangedCallback(Control.BorderThicknessProperty, _borderThicknessChangedToken);
                _borderThicknessChangedToken = 0;
            }
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyTheme(Application.Current?.Resources);
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
            return _rootBorder ?? base.GetNeumorphicHostElement();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyBreadcrumbBar bar)
            {
                bar.ApplyContent();
            }
        }

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyBreadcrumbBar bar)
            {
                bar.ApplyContent();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyBreadcrumbBar bar)
            {
                bar.ApplyAll();
            }
        }

        private void BuildVisualTree()
        {
            _rootBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            _breadcrumbBar = new BreadcrumbBar
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            _breadcrumbBar.SetBinding(Control.IsEnabledProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(IsEnabled))
            });

            _breadcrumbBar.ItemClicked += OnBreadcrumbBarItemClicked;

            _rootBorder.Child = _breadcrumbBar;
            Content = _rootBorder;
        }

        private void ApplyAll()
        {
            if (_breadcrumbBar == null || _rootBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
            {
                DaisyTokenDefaults.EnsureDefaults(resources);
            }

            ApplySizing(resources);
            ApplyTheme(resources);
            ApplyContent();
        }

        private void ApplyContent()
        {
            if (_breadcrumbBar == null)
                return;

            _breadcrumbBar.ItemsSource = ItemsSource;

            if (ItemTemplate == null)
            {
                _breadcrumbBar.ClearValue(BreadcrumbBar.ItemTemplateProperty);
            }
            else
            {
                _breadcrumbBar.ItemTemplate = ItemTemplate;
            }
        }

        private void ApplySizing(ResourceDictionary? resources)
        {
            if (_breadcrumbBar == null || _rootBorder == null)
                return;

            var effectiveSize = FlowerySizeManager.ShouldIgnoreGlobalSize(this)
                ? Size
                : FlowerySizeManager.CurrentSize;

            var fontSize = DaisyResourceLookup.GetSizeDouble(
                resources,
                "DaisySize",
                effectiveSize,
                "FontSize",
                DaisyResourceLookup.GetDefaultFontSize(effectiveSize));

            var iconSize = DaisyResourceLookup.GetDefaultIconSize(effectiveSize) + 2;

            _breadcrumbBar.FontSize = fontSize;
            _breadcrumbBar.Resources["BreadcrumbBarItemThemeFontSize"] = fontSize;
            _breadcrumbBar.Resources["BreadcrumbBarChevronFontSize"] = iconSize;

            var defaultPadding = DaisyResourceLookup.GetDefaultPadding(effectiveSize);
            var hasLocalPadding = ReadLocalValue(Control.PaddingProperty) != DependencyProperty.UnsetValue;
            _rootBorder.Padding = hasLocalPadding ? Padding : defaultPadding;
        }

        private void ApplyTheme(ResourceDictionary? resources)
        {
            if (_breadcrumbBar == null || _rootBorder == null)
                return;

            var transparent = new SolidColorBrush(Colors.Transparent);

            var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.Black));
            var base100 = DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush", new SolidColorBrush(Colors.White));
            var base200 = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush", new SolidColorBrush(Colors.LightGray));
            var base300 = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Colors.Gray));
            var primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Colors.Blue));
            var primaryFocus = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryFocusBrush", primary);

            var foregroundOverride = TryGetControlBrush("DaisyBreadcrumbBar", "Foreground");
            var linkForegroundOverride = TryGetControlBrush("DaisyBreadcrumbBar", "LinkForeground");
            var currentForegroundOverride = TryGetControlBrush("DaisyBreadcrumbBar", "CurrentForeground");
            var hoverForegroundOverride = TryGetControlBrush("DaisyBreadcrumbBar", "HoverForeground");
            var pressedForegroundOverride = TryGetControlBrush("DaisyBreadcrumbBar", "PressedForeground");
            var disabledForegroundOverride = TryGetControlBrush("DaisyBreadcrumbBar", "DisabledForeground");
            var itemBackgroundOverride = TryGetControlBrush("DaisyBreadcrumbBar", "ItemBackground");
            var itemBorderOverride = TryGetControlBrush("DaisyBreadcrumbBar", "ItemBorderBrush");

            var foreground = foregroundOverride ?? baseContent;
            var linkForeground = linkForegroundOverride ?? primary;
            var linkHover = hoverForegroundOverride ?? primaryFocus;
            var linkPressed = pressedForegroundOverride ?? primaryFocus;
            var currentForeground = currentForegroundOverride ?? foreground;
            var disabledForeground = disabledForegroundOverride ?? CreateDisabledBrush(foreground);

            var itemBackground = itemBackgroundOverride ?? transparent;
            var itemBorder = itemBorderOverride ?? transparent;

            _breadcrumbBar.Resources["BreadcrumbBarForegroundBrush"] = foreground;
            _breadcrumbBar.Resources["BreadcrumbBarNormalForegroundBrush"] = linkForeground;
            _breadcrumbBar.Resources["BreadcrumbBarHoverForegroundBrush"] = linkHover;
            _breadcrumbBar.Resources["BreadcrumbBarPressedForegroundBrush"] = linkPressed;
            _breadcrumbBar.Resources["BreadcrumbBarDisabledForegroundBrush"] = disabledForeground;
            _breadcrumbBar.Resources["BreadcrumbBarFocusForegroundBrush"] = linkHover;

            _breadcrumbBar.Resources["BreadcrumbBarCurrentNormalForegroundBrush"] = currentForeground;
            _breadcrumbBar.Resources["BreadcrumbBarCurrentHoverForegroundBrush"] = currentForeground;
            _breadcrumbBar.Resources["BreadcrumbBarCurrentPressedForegroundBrush"] = currentForeground;
            _breadcrumbBar.Resources["BreadcrumbBarCurrentDisabledForegroundBrush"] = disabledForeground;
            _breadcrumbBar.Resources["BreadcrumbBarCurrentFocusForegroundBrush"] = currentForeground;

            _breadcrumbBar.Resources["BreadcrumbBarBackgroundBrush"] = itemBackground;
            _breadcrumbBar.Resources["BreadcrumbBarBorderBrush"] = itemBorder;

            _breadcrumbBar.Resources["BreadcrumbBarEllipsisDropDownItemBackground"] = base100;
            _breadcrumbBar.Resources["BreadcrumbBarEllipsisDropDownItemBackgroundPointerOver"] = base200;
            _breadcrumbBar.Resources["BreadcrumbBarEllipsisDropDownItemBackgroundPressed"] = base300;
            _breadcrumbBar.Resources["BreadcrumbBarEllipsisDropDownItemBackgroundDisabled"] = base100;
            _breadcrumbBar.Resources["BreadcrumbBarEllipsisDropDownItemForegroundPointerOver"] = foreground;
            _breadcrumbBar.Resources["BreadcrumbBarEllipsisDropDownItemForegroundPressed"] = foreground;
            _breadcrumbBar.Resources["BreadcrumbBarEllipsisDropDownItemForegroundDisabled"] = disabledForeground;

            _breadcrumbBar.Resources["BreadcrumbBarEllipsisFlyoutPresenterBackground"] = base100;
            _breadcrumbBar.Resources["BreadcrumbBarEllipsisFlyoutPresenterBorderBrush"] = base300;
            _breadcrumbBar.Resources["BreadcrumbBarEllipsisFlyoutPresenterBorderThemeThickness"] = new Thickness(1);

            var controlBackground = ReadLocalValue(Control.BackgroundProperty) != DependencyProperty.UnsetValue
                ? Background
                : transparent;
            var controlBorder = ReadLocalValue(Control.BorderBrushProperty) != DependencyProperty.UnsetValue
                ? BorderBrush
                : transparent;

            _rootBorder.Background = controlBackground ?? transparent;
            _rootBorder.BorderBrush = controlBorder ?? transparent;
            _rootBorder.BorderThickness = BorderThickness;
        }

        private void OnPaddingChanged(DependencyObject sender, DependencyProperty dp)
        {
            ApplySizing(Application.Current?.Resources);
        }

        private void OnBackgroundChanged(DependencyObject sender, DependencyProperty dp)
        {
            ApplyTheme(Application.Current?.Resources);
        }

        private void OnBorderBrushChanged(DependencyObject sender, DependencyProperty dp)
        {
            ApplyTheme(Application.Current?.Resources);
        }

        private void OnBorderThicknessChanged(DependencyObject sender, DependencyProperty dp)
        {
            ApplyTheme(Application.Current?.Resources);
        }

        private void OnBreadcrumbBarItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            ItemClicked?.Invoke(this, args);
        }

        private static Brush CreateDisabledBrush(Brush baseBrush)
        {
            if (baseBrush is SolidColorBrush scb)
            {
                return new SolidColorBrush(Color.FromArgb(153, scb.Color.R, scb.Color.G, scb.Color.B));
            }

            return baseBrush;
        }
    }
}

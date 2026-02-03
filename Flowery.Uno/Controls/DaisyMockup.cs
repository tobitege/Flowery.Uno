using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Flowery.Localization;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// Variant for DaisyMockup display style.
    /// </summary>
    public enum DaisyMockupVariant
    {
        Code,
        Window,
        Browser
    }

    /// <summary>
    /// Chrome style for window/browser decorations.
    /// </summary>
    public enum DaisyChromeStyle
    {
        /// <summary>macOS-style traffic light buttons on the left (red/yellow/green circles).</summary>
        Mac,
        /// <summary>Windows-style buttons on the right (minimize/maximize/close).</summary>
        Windows,
        /// <summary>Linux/GNOME-style buttons on the right (minimize/maximize/close circles).</summary>
        Linux
    }

    /// <summary>
    /// A mockup frame control styled after DaisyUI's Mockup components.
    /// Displays content in Code IDE, Window, or Browser frames.
    /// </summary>
    public partial class DaisyMockup : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _outerBorder;
        private Border? _innerBorder;
        private Border? _headerBorder;
        private Border? _contentBorder;
        private TextBlock? _urlTextBlock;
        private object? _userContent;
        private bool _isRebuilding;

        public DaisyMockup()
        {
            DefaultStyleKey = typeof(DaisyMockup);
            IsTabStop = false;
        }

        /// <summary>
        /// Called when size changes - need to rebuild visual tree with new dimensions.
        /// </summary>
        private void OnSizeChanged()
        {
            if (_rootGrid != null && !_isRebuilding)
            {
                RebuildVisualTree();
            }
        }

        private void RebuildVisualTree()
        {
            if (_isRebuilding) return;

            try
            {
                _isRebuilding = true;
                BuildVisualTree();
                ApplyAll();
            }
            finally
            {
                _isRebuilding = false;
            }
        }

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyMockupVariant),
                typeof(DaisyMockup),
                new PropertyMetadata(DaisyMockupVariant.Code, OnAppearanceChanged));

        public DaisyMockupVariant Variant
        {
            get => (DaisyMockupVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register(
                nameof(Url),
                typeof(string),
                typeof(DaisyMockup),
                new PropertyMetadata("https://daisyui.com", OnUrlChanged));

        public string Url
        {
            get => (string)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        public static readonly DependencyProperty ChromeStyleProperty =
            DependencyProperty.Register(
                nameof(ChromeStyle),
                typeof(DaisyChromeStyle),
                typeof(DaisyMockup),
                new PropertyMetadata(DaisyChromeStyle.Mac, OnAppearanceChanged));

        public DaisyChromeStyle ChromeStyle
        {
            get => (DaisyChromeStyle)GetValue(ChromeStyleProperty);
            set => SetValue(ChromeStyleProperty, value);
        }

        public static readonly DependencyProperty AppIconProperty =
            DependencyProperty.Register(
                nameof(AppIcon),
                typeof(object),
                typeof(DaisyMockup),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Optional app icon for Windows chrome style. If not set, a default generic icon is shown.
        /// Accepts any content (PathIcon, Image, etc.).
        /// </summary>
        public object? AppIcon
        {
            get => GetValue(AppIconProperty);
            set => SetValue(AppIconProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyMockup),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Size Scaling

        /// <summary>
        /// Gets the effective size (Size property is updated by lifecycle for global size changes).
        /// </summary>
        private DaisySize EffectiveSize => Size;

        /// <summary>
        /// Size scale factors for different DaisySize values.
        /// </summary>
        private double SizeScale => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 0.65,
            DaisySize.Small => 0.8,
            DaisySize.Medium => 1.0,
            DaisySize.Large => 1.2,
            _ => 1.0
        };

        private double HeaderHeight => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 22,
            DaisySize.Small => 26,
            DaisySize.Medium => 32,
            DaisySize.Large => 40,
            _ => 32
        };

        private double BrowserHeaderHeight => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 36,
            DaisySize.Small => 44,
            DaisySize.Medium => 52,
            DaisySize.Large => 64,
            _ => 52
        };

        private double TrafficDotSize => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 8,
            DaisySize.Small => 10,
            DaisySize.Medium => 12,
            DaisySize.Large => 14,
            _ => 12
        };

        private double CornerRadiusValue => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 10,
            DaisySize.Small => 12,
            DaisySize.Medium => 16,
            DaisySize.Large => 20,
            _ => 16
        };

        private double UrlBarCornerRadius => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 5,
            DaisySize.Small => 6,
            DaisySize.Medium => 8,
            DaisySize.Large => 10,
            _ => 8
        };

        private double UrlFontSize => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 10,
            DaisySize.Small => 11,
            DaisySize.Medium => 13,
            DaisySize.Large => 15,
            _ => 13
        };

        private double WindowsButtonWidth => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 28,  // Minimum for visibility
            DaisySize.Small => 34,
            DaisySize.Medium => 42,
            DaisySize.Large => 50,
            _ => 42
        };

        private double LinuxButtonSize => EffectiveSize switch
        {
            DaisySize.ExtraSmall => 18,  // Minimum for visibility
            DaisySize.Small => 20,
            DaisySize.Medium => 24,
            DaisySize.Large => 28,
            _ => 24
        };

        #endregion

        #region RTL Support

        /// <summary>
        /// Determines if the layout should be flipped for RTL.
        /// Uses the current culture's RTL setting from FloweryLocalization.
        /// For Mac: controls move to right in RTL.
        /// For Windows/Linux: controls move to left in RTL.
        /// </summary>
        private static bool IsRightToLeft => FloweryLocalization.Instance.IsRtl;

        /// <summary>
        /// Gets whether window controls should be on the left side.
        /// Mac is left in LTR, right in RTL.
        /// Windows/Linux is right in LTR, left in RTL.
        /// </summary>
        private bool AreControlsOnLeft =>
            (ChromeStyle == DaisyChromeStyle.Mac && !IsRightToLeft) ||
            (ChromeStyle != DaisyChromeStyle.Mac && IsRightToLeft);

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMockup mockup && mockup._rootGrid != null)
            {
                mockup.BuildVisualTree();
                mockup.ApplyAll();
            }
        }

        private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMockup mockup && mockup._urlTextBlock != null)
            {
                mockup._urlTextBlock.Text = e.NewValue as string ?? "https://daisyui.com";
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null && ReferenceEquals(Content, _rootGrid))
            {
                FloweryLocalization.CultureChanged += OnCultureChanged;
                return;
            }

            BuildVisualTree();
            ApplyAll();
            FloweryLocalization.CultureChanged += OnCultureChanged;
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            FloweryLocalization.CultureChanged -= OnCultureChanged;
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
            OnSizeChanged();
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _outerBorder ?? base.GetNeumorphicHostElement();
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            // Culture changed (e.g., switched to Arabic) - update FlowDirection
            // This causes the layout to reflow for RTL/LTR automatically
            UpdateFlowDirection();
        }

        private void UpdateFlowDirection()
        {
            var flowDirection = FloweryLocalization.Instance.IsRtl
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            if (_headerBorder != null)
            {
                _headerBorder.FlowDirection = flowDirection;
            }
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Capture user content if not yet captured
            if (Content != null && !ReferenceEquals(Content, _rootGrid))
            {
                _userContent = Content;
                Content = null;
            }

            // Completely dismantle old tree before rebuilding
            // This ensures all elements are detached from their parents
            if (_contentBorder?.Child is ContentPresenter oldPresenter)
            {
                oldPresenter.Content = null;
                _contentBorder.Child = null;
            }
            if (_innerBorder != null) _innerBorder.Child = null;
            if (_outerBorder != null) _outerBorder.Child = null;
            _rootGrid?.Children.Clear();

            // Clear old tree from control
            Content = null;

            // Create new tree
            _rootGrid = new Grid();

            // Outer border with corner radius
            _outerBorder = new Border
            {
                CornerRadius = new CornerRadius(CornerRadiusValue),
                BorderThickness = new Thickness(1)
            };

            // Inner container with slightly smaller corner radius
            _innerBorder = new Border
            {
                CornerRadius = new CornerRadius(CornerRadiusValue - 1)
            };

            var innerGrid = new Grid();

            if (Variant == DaisyMockupVariant.Browser)
            {
                BuildBrowserLayout(innerGrid);
            }
            else
            {
                BuildStandardLayout(innerGrid);
            }

            _innerBorder.Child = innerGrid;
            _outerBorder.Child = _innerBorder;
            _rootGrid.Children.Add(_outerBorder);

            Content = _rootGrid;

            // Apply correct FlowDirection for current culture
            UpdateFlowDirection();
        }

        private void BuildStandardLayout(Grid innerGrid)
        {
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header with window controls
            _headerBorder = new Border { Height = HeaderHeight };
            var controls = CreateWindowControls(forBrowser: false);
            DetachFromParentIfNeeded(controls);
            _headerBorder.Child = controls;

            Grid.SetRow(_headerBorder, 0);
            innerGrid.Children.Add(_headerBorder);

            // Content area
            _contentBorder = new Border();
            var contentPresenter = new ContentPresenter
            {
                Content = _userContent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _contentBorder.Child = contentPresenter;

            Grid.SetRow(_contentBorder, 1);
            innerGrid.Children.Add(_contentBorder);
        }

        private void BuildBrowserLayout(Grid innerGrid)
        {
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Browser toolbar
            _headerBorder = new Border
            {
                Height = BrowserHeaderHeight,
                Padding = new Thickness(8 * SizeScale, 0, 8 * SizeScale, 0)
            };

            // Use 3-column grid: [Chrome/Space] [URL Bar] [Space/Chrome]
            var toolbarPanel = new Grid();

            if (AreControlsOnLeft)
            {
                // Mac-style: Chrome | URL | Space
                toolbarPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                toolbarPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                toolbarPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8 * SizeScale) });
            }
            else
            {
                // Windows/Linux-style: Space | URL | Chrome
                toolbarPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8 * SizeScale) });
                toolbarPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                toolbarPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            // Add chrome controls
            var chromeControls = CreateWindowControls(forBrowser: true);
            DetachFromParentIfNeeded(chromeControls);
            Grid.SetColumn(chromeControls, AreControlsOnLeft ? 0 : 2);
            toolbarPanel.Children.Add(chromeControls);

            // URL address bar (in center column)
            var urlBar = new Border
            {
                CornerRadius = new CornerRadius(UrlBarCornerRadius),
                Padding = new Thickness(12 * SizeScale, 6 * SizeScale, 12 * SizeScale, 6 * SizeScale),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _urlTextBlock = new TextBlock
            {
                Text = Url,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = UrlFontSize,
                Opacity = 0.8
            };
            urlBar.Child = _urlTextBlock;
            Grid.SetColumn(urlBar, 1);
            toolbarPanel.Children.Add(urlBar);

            _headerBorder.Child = toolbarPanel;

            Grid.SetRow(_headerBorder, 0);
            innerGrid.Children.Add(_headerBorder);

            // Content area with top border line
            _contentBorder = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            var contentPresenter = new ContentPresenter
            {
                Content = _userContent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _contentBorder.Child = contentPresenter;

            Grid.SetRow(_contentBorder, 1);
            innerGrid.Children.Add(_contentBorder);
        }

        private static void DetachFromParentIfNeeded(UIElement? element)
        {
            if (element == null) return;
            var parent = VisualTreeHelper.GetParent(element);
            if (parent is Panel panel)
            {
                panel.Children.Remove(element);
            }
            else if (parent is Border border)
            {
                if (ReferenceEquals(border.Child, element)) border.Child = null;
            }
            else if (parent is ContentControl contentControl)
            {
                if (ReferenceEquals(contentControl.Content, element)) contentControl.Content = null;
            }
            else if (parent is ContentPresenter presenter)
            {
                if (ReferenceEquals(presenter.Content, element)) presenter.Content = null;
            }
        }

        private FrameworkElement CreateWindowControls(bool forBrowser)
        {
            return ChromeStyle switch
            {
                DaisyChromeStyle.Mac => CreateMacControls(forBrowser),
                DaisyChromeStyle.Windows => CreateWindowsControls(forBrowser),
                DaisyChromeStyle.Linux => CreateLinuxControls(forBrowser),
                _ => CreateMacControls(forBrowser)
            };
        }

        private StackPanel CreateMacControls(bool forBrowser)
        {
            // Mac controls are always positioned on the LEFT side in LTR
            // FlowDirection on the parent header will flip them to the RIGHT in RTL
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = Math.Max(3, 4 * SizeScale),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = forBrowser ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                Margin = forBrowser ? new Thickness(0) : new Thickness(12 * SizeScale, 0, 0, 0)
            };
            panel.Children.Add(CreateTrafficDot("#FF5F56")); // Red - Close
            panel.Children.Add(CreateTrafficDot("#FFBD2E")); // Yellow - Minimize
            panel.Children.Add(CreateTrafficDot("#27C93F")); // Green - Maximize
            return panel;
        }

        private Grid CreateWindowsControls(bool forBrowser)
        {
            // Windows controls: icon on LEFT, buttons on RIGHT in LTR
            // FlowDirection on the parent header will flip them for RTL
            var iconSize = Math.Max(16, 24 * SizeScale);

            // Create the main container grid
            var container = new Grid();

            // Only show app icon for window mode (not browser mode)
            if (!forBrowser)
            {
                // Create app icon (always on left in LTR, FlowDirection flips for RTL)
                var iconContainer = new Border
                {
                    Width = iconSize,
                    Height = iconSize,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(6 * SizeScale, 0, 6 * SizeScale, 0),
                    // Prevent icon mirroring when parent has FlowDirection.RightToLeft
                    FlowDirection = FlowDirection.LeftToRight
                };

                // Use provided AppIcon or default generic icon
                if (AppIcon != null)
                {
                    if (AppIcon is FrameworkElement fe)
                    {
                        DetachFromParentIfNeeded(fe);
                        iconContainer.Child = fe;
                    }
                    else
                    {
                        iconContainer.Child = new ContentPresenter { Content = AppIcon };
                    }
                }
                else
                {
                    // Default generic app icon (simple window shape with 0-based coords)
                    var iconPathSize = Math.Max(10, 12 * SizeScale);
                    var defaultIcon = new Microsoft.UI.Xaml.Shapes.Path
                    {
                        Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                            typeof(Geometry), "M0,0 L12,0 L12,12 L0,12 Z M0,3 L12,3"),
                        Stroke = new SolidColorBrush(ColorFromHex("#858585")),
                        StrokeThickness = 1,
                        Width = iconPathSize,
                        Height = iconPathSize,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    iconContainer.Child = defaultIcon;
                }

                container.Children.Add(iconContainer);
            }

            // Create window buttons panel (always on right in LTR, FlowDirection flips for RTL)
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = forBrowser ? HorizontalAlignment.Center : HorizontalAlignment.Right
            };

            // Windows-style: Minimize, Maximize, Close
            buttonsPanel.Children.Add(CreateWindowsButton(WindowsChromeIcon.Minimize, "#858585"));
            buttonsPanel.Children.Add(CreateWindowsButton(WindowsChromeIcon.Maximize, "#858585"));
            buttonsPanel.Children.Add(CreateWindowsButton(WindowsChromeIcon.Close, "#858585"));

            container.Children.Add(buttonsPanel);

            return container;
        }

        private enum WindowsChromeIcon { Minimize, Maximize, Close }

        private Border CreateWindowsButton(WindowsChromeIcon icon, string foregroundColor)
        {
            // Simple path geometries for window controls
            var pathData = icon switch
            {
                WindowsChromeIcon.Minimize => "M0,5 L10,5",                           // Horizontal line
                WindowsChromeIcon.Maximize => "M0,0 L10,0 L10,10 L0,10 Z",            // Square outline
                WindowsChromeIcon.Close => "M0,0 L10,10 M10,0 L0,10",                 // X shape
                _ => ""
            };

            var iconSize = Math.Max(6, 8 * SizeScale);
            var path = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                    typeof(Geometry), pathData),
                Stroke = new SolidColorBrush(ColorFromHex(foregroundColor)),
                StrokeThickness = 1,
                Width = iconSize,
                Height = iconSize,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return new Border
            {
                Width = WindowsButtonWidth,
                Height = HeaderHeight,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                Child = path,
                // Prevent icon mirroring when parent has FlowDirection.RightToLeft
                FlowDirection = FlowDirection.LeftToRight
            };
        }

        private StackPanel CreateLinuxControls(bool forBrowser)
        {
            // GNOME-style: Minimize, Maximize, Close circles
            // Linux controls are always on the RIGHT side in LTR
            // FlowDirection on the parent header will flip them to the LEFT in RTL
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = Math.Max(4, 6 * SizeScale),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = forBrowser ? HorizontalAlignment.Center : HorizontalAlignment.Right,
                Margin = forBrowser ? new Thickness(0) : new Thickness(0, 0, 12 * SizeScale, 0)
            };

            // GNOME uses subtle colored circles with icons inside
            panel.Children.Add(CreateLinuxButton(WindowsChromeIcon.Minimize, "#3D3846"));
            panel.Children.Add(CreateLinuxButton(WindowsChromeIcon.Maximize, "#3D3846"));
            panel.Children.Add(CreateLinuxButton(WindowsChromeIcon.Close, "#C01C28"));
            return panel;
        }

        private Border CreateLinuxButton(WindowsChromeIcon icon, string bgColor)
        {
            // Simple path geometries for window controls
            // All use 8x8 bounding box for consistent scaling
            var pathData = icon switch
            {
                WindowsChromeIcon.Minimize => "M0,6 L8,6",                              // Horizontal line at bottom
                WindowsChromeIcon.Maximize => "M0,0 L8,0 L8,8 L0,8 Z",                  // Square outline
                WindowsChromeIcon.Close => "M0,0 L8,8 M8,0 L0,8",                       // X shape
                _ => ""
            };

            var buttonSize = LinuxButtonSize;
            var iconSize = Math.Max(8, 8 * SizeScale);  // Match geometry size
            var path = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                    typeof(Geometry), pathData),
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                StrokeThickness = 1.5,
                Width = iconSize,
                Height = iconSize,
                Stretch = Stretch.None,  // Don't stretch - geometry is pre-sized
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return new Border
            {
                Width = buttonSize,
                Height = buttonSize,
                CornerRadius = new CornerRadius(buttonSize / 2),
                Background = new SolidColorBrush(ColorFromHex(bgColor)),
                Child = path,
                // Prevent icon mirroring when parent has FlowDirection.RightToLeft
                FlowDirection = FlowDirection.LeftToRight
            };
        }

        private Border CreateTrafficDot(string hexColor)
        {
            var dotSize = TrafficDotSize;
            return new Border
            {
                Width = dotSize,
                Height = dotSize,
                CornerRadius = new CornerRadius(dotSize / 2),
                Background = new SolidColorBrush(ColorFromHex(hexColor))
            };
        }

        private static Windows.UI.Color ColorFromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Windows.UI.Color.FromArgb(
                255,
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_outerBorder == null || _innerBorder == null)
                return;

            ApplyVariantColors();
        }

        private void ApplyVariantColors()
        {
            if (_outerBorder == null || _innerBorder == null || _contentBorder == null || _headerBorder == null)
                return;

            var base100Brush = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            var base200Brush = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            var base300Brush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            var baseContentBrush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");

            switch (Variant)
            {
                case DaisyMockupVariant.Code:
                    // Dark code theme
                    var codeBgColor = Windows.UI.Color.FromArgb(255, 42, 48, 60); // #2a303c
                    _outerBorder.Background = new SolidColorBrush(codeBgColor);
                    _outerBorder.BorderThickness = new Thickness(0);
                    _innerBorder.Background = new SolidColorBrush(codeBgColor);
                    _headerBorder.Background = new SolidColorBrush(codeBgColor);
                    _contentBorder.Padding = new Thickness(24);
                    _contentBorder.Background = new SolidColorBrush(codeBgColor);
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                    break;

                case DaisyMockupVariant.Window:
                    _outerBorder.Background = base300Brush;
                    _outerBorder.BorderBrush = base300Brush;
                    _outerBorder.BorderThickness = new Thickness(1);
                    _innerBorder.Background = base200Brush;
                    _headerBorder.Background = base200Brush;
                    _contentBorder.Background = base100Brush;
                    _contentBorder.Padding = new Thickness(20);
                    Foreground = baseContentBrush;
                    break;

                case DaisyMockupVariant.Browser:
                    _outerBorder.Background = base300Brush;
                    _outerBorder.BorderBrush = base300Brush;
                    _outerBorder.BorderThickness = new Thickness(1);
                    _innerBorder.Background = base100Brush;
                    _headerBorder.Background = base100Brush;

                    // Find and style the URL bar background
                    if (_headerBorder.Child is Grid toolbar)
                    {
                        foreach (var child in toolbar.Children)
                        {
                            if (child is Border urlBar && urlBar.Child == _urlTextBlock)
                            {
                                urlBar.Background = base200Brush;
                            }
                        }
                    }

                    _contentBorder.Background = base100Brush;
                    _contentBorder.BorderBrush = base300Brush;
                    _contentBorder.Padding = new Thickness(0);
                    Foreground = baseContentBrush;
                    break;
            }
        }

        #endregion
    }
}

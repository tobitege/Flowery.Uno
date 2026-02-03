using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using Flowery.Theming;
using Flowery.Services;

namespace Flowery.Controls
{
    /// <summary>
    /// An Avatar control styled after DaisyUI's Avatar component.
    /// Supports size, shape, status indicator, and ring options.
    /// </summary>
    public partial class DaisyAvatar : DaisyBaseContentControl
    {
        private Border? _outerBorder;
        private Border? _innerBorder;
        private Ellipse? _statusIndicator;
        private Image? _imageElement;
        private TextBlock? _placeholderText;
        private SymbolIcon? _symbolIcon;
        private PathIcon? _pathIcon;
        private Viewbox? _iconViewbox;

        public DaisyAvatar()
        {
            DefaultStyleKey = typeof(DaisyAvatar);
            IsTabStop = false;

            BuildVisualTree();
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyAvatar),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ShapeProperty =
            DependencyProperty.Register(
                nameof(Shape),
                typeof(DaisyAvatarShape),
                typeof(DaisyAvatar),
                new PropertyMetadata(DaisyAvatarShape.Circle, OnAppearanceChanged));

        public DaisyAvatarShape Shape
        {
            get => (DaisyAvatarShape)GetValue(ShapeProperty);
            set => SetValue(ShapeProperty, value);
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(DaisyStatus),
                typeof(DaisyAvatar),
                new PropertyMetadata(DaisyStatus.None, OnAppearanceChanged));

        public DaisyStatus Status
        {
            get => (DaisyStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public static readonly DependencyProperty IsPlaceholderProperty =
            DependencyProperty.Register(
                nameof(IsPlaceholder),
                typeof(bool),
                typeof(DaisyAvatar),
                new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsPlaceholder
        {
            get => (bool)GetValue(IsPlaceholderProperty);
            set => SetValue(IsPlaceholderProperty, value);
        }

        public static readonly DependencyProperty HasRingProperty =
            DependencyProperty.Register(
                nameof(HasRing),
                typeof(bool),
                typeof(DaisyAvatar),
                new PropertyMetadata(false, OnAppearanceChanged));

        public bool HasRing
        {
            get => (bool)GetValue(HasRingProperty);
            set => SetValue(HasRingProperty, value);
        }

        public static readonly DependencyProperty RingColorProperty =
            DependencyProperty.Register(
                nameof(RingColor),
                typeof(DaisyColor),
                typeof(DaisyAvatar),
                new PropertyMetadata(DaisyColor.Primary, OnAppearanceChanged));

        public DaisyColor RingColor
        {
            get => (DaisyColor)GetValue(RingColorProperty);
            set => SetValue(RingColorProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(ImageSource),
                typeof(DaisyAvatar),
                new PropertyMetadata(null, OnSourceChanged));

        public ImageSource? Source
        {
            get => (ImageSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty InitialsProperty =
            DependencyProperty.Register(
                nameof(Initials),
                typeof(string),
                typeof(DaisyAvatar),
                new PropertyMetadata(null, OnAppearanceChanged));

        public string? Initials
        {
            get => (string?)GetValue(InitialsProperty);
            set => SetValue(InitialsProperty, value);
        }

        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.Register(
                nameof(IconData),
                typeof(string),
                typeof(DaisyAvatar),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Path data for displaying an icon instead of text or image.
        /// Use SVG path data string (e.g., "M12 2C6.48 2 2 6.48 2 12...").
        /// </summary>
        public string? IconData
        {
            get => (string?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public static readonly DependencyProperty PlaceholderSymbolProperty =
            DependencyProperty.Register(
                nameof(PlaceholderSymbol),
                typeof(Symbol?),
                typeof(DaisyAvatar),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Windows Symbol to use as placeholder icon. Simpler alternative to IconData.
        /// Common values: Symbol.Contact (user silhouette), Symbol.People, Symbol.Account.
        /// When set, takes priority over IconData for placeholder display.
        /// </summary>
        public Symbol? PlaceholderSymbol
        {
            get => (Symbol?)GetValue(PlaceholderSymbolProperty);
            set => SetValue(PlaceholderSymbolProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAvatar a) a.ApplyAll();
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAvatar a)
            {
                if (a._imageElement != null)
                {
                    a._imageElement.Source = e.NewValue as ImageSource;
                }
                a.ApplyAll();
            }
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
            return _outerBorder ?? base.GetNeumorphicHostElement();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            var root = new Grid();

            _outerBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _innerBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var contentGrid = new Grid();

            _imageElement = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (Source != null)
            {
                _imageElement.Source = Source;
            }

            _placeholderText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };

            // SymbolIcon for simple Windows symbols (e.g., Contact for user placeholder)
            _symbolIcon = new SymbolIcon
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // PathIcon for custom SVG path data
            _pathIcon = new PathIcon
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Wrap PathIcon in Viewbox for proper scaling of SVG paths (which use 24x24 coordinates)
            _iconViewbox = new Viewbox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform,
                Child = _pathIcon
            };

            contentGrid.Children.Add(_imageElement);
            contentGrid.Children.Add(_placeholderText);
            contentGrid.Children.Add(_symbolIcon);
            contentGrid.Children.Add(_iconViewbox);
            _innerBorder.Child = contentGrid;
            _outerBorder.Child = _innerBorder;

            // Status indicator
            _statusIndicator = new Ellipse
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            root.Children.Add(_outerBorder);
            root.Children.Add(_statusIndicator);

            Content = root;
        }

        #endregion

        #region Content Routing

        /// <summary>
        /// Routes user-provided Content to the placeholder text or grid.
        /// This prevents the Content property from overwriting our custom visual tree.
        /// </summary>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            // If the new content is our root grid, we set it ourselves - don't do anything
            var root = GetRootGrid();
            if (ReferenceEquals(newContent, root))
                return;

            // If the content is just a string, update Initials
            if (newContent is string s)
            {
                Initials = s;
                IsPlaceholder = true;
            }

            // Restore our root as the actual Content
            if (root != null && !ReferenceEquals(base.Content, root))
            {
                base.Content = root;
            }
        }

        private Grid? GetRootGrid()
        {
            if (_outerBorder?.Parent is Grid g) return g;
            return base.Content as Grid;
        }

        #endregion

        #region Styling Application

        private void ApplyAll()
        {
            if (_outerBorder == null || _innerBorder == null || _statusIndicator == null ||
                _imageElement == null || _placeholderText == null)
                return;

            ApplySizing();
            ApplyShape();
            ApplyColors();
            ApplyContent();
            ApplyStatus();
            ApplyRing();
        }

        private void ApplySizing()
        {
            if (_outerBorder == null || _innerBorder == null || _statusIndicator == null ||
                _placeholderText == null || _symbolIcon == null || _pathIcon == null || _iconViewbox == null)
                return;

            // Get sizing from centralized helpers (tokens guaranteed by EnsureDefaults)
            double avatarSize = DaisyResourceLookup.GetAvatarSize(Size);
            double ringThickness = HasRing ? GetRingThickness() : 0;
            double innerSize = avatarSize - (ringThickness * 2);
            double statusSize = DaisyResourceLookup.GetAvatarStatusSize(Size);
            double fontSize = DaisyResourceLookup.GetAvatarFontSize(Size);
            double iconSize = DaisyResourceLookup.GetAvatarIconSize(Size);

            // Set size on the control itself
            Width = avatarSize;
            Height = avatarSize;
            MinWidth = avatarSize;
            MinHeight = avatarSize;

            _outerBorder.Width = avatarSize;
            _outerBorder.Height = avatarSize;
            _innerBorder.Width = innerSize;
            _innerBorder.Height = innerSize;

            _statusIndicator.Width = statusSize;
            _statusIndicator.Height = statusSize;

            _placeholderText.FontSize = fontSize;

            // Set size on the Viewbox which will scale the icon to fit
            _iconViewbox.Width = iconSize;
            _iconViewbox.Height = iconSize;
        }

        private void ApplyShape()
        {
            if (_outerBorder == null || _innerBorder == null)
                return;

            double ringThickness = HasRing ? GetRingThickness() : 0;

            var outerCornerRadius = Shape switch
            {
                DaisyAvatarShape.Square => new CornerRadius(0),
                DaisyAvatarShape.Rounded => new CornerRadius(8),
                DaisyAvatarShape.Circle => new CornerRadius(1000),
                _ => new CornerRadius(1000)
            };

            // Inner corner radius should be slightly smaller when there's a ring
            // to properly fit inside the outer border's ring area
            var innerCornerRadius = Shape switch
            {
                DaisyAvatarShape.Square => new CornerRadius(0),
                DaisyAvatarShape.Rounded => new CornerRadius(Math.Max(0, 8 - ringThickness)),
                DaisyAvatarShape.Circle => new CornerRadius(1000),
                _ => new CornerRadius(1000)
            };

            _outerBorder.CornerRadius = outerCornerRadius;
            _innerBorder.CornerRadius = innerCornerRadius;
        }

        private void ApplyColors()
        {
            if (_innerBorder == null || _placeholderText == null || _symbolIcon == null || _pathIcon == null)
                return;

            // 1. Check for explicit properties on the control itself
            // 2. Check for lightweight styling overrides in Resources
            // 3. Fallback to Neutral theme colors
            var bgBrush = Background ?? DaisyResourceLookup.TryGetControlBrush(this, "DaisyAvatar", "Background") ?? DaisyResourceLookup.GetBrush("DaisyNeutralBrush");
            var fgBrush = Foreground ?? DaisyResourceLookup.TryGetControlBrush(this, "DaisyAvatar", "Foreground") ?? DaisyResourceLookup.GetBrush("DaisyNeutralContentBrush");

            _innerBorder.Background = bgBrush;
            _placeholderText.Foreground = fgBrush;
            _symbolIcon.Foreground = fgBrush;
            _pathIcon.Foreground = fgBrush;
        }

        private void ApplyContent()
        {
            if (_imageElement == null || _placeholderText == null || _symbolIcon == null || _pathIcon == null || _iconViewbox == null)
                return;

            bool hasSymbol = PlaceholderSymbol.HasValue;
            bool hasPathIcon = !string.IsNullOrWhiteSpace(IconData);
            bool showImage = Source != null && !IsPlaceholder && !hasSymbol && !hasPathIcon;
            bool showSymbol = hasSymbol && (IsPlaceholder || Source == null);
            bool showPathIcon = hasPathIcon && !showSymbol && (IsPlaceholder || Source == null);
            bool showText = !showImage && !showSymbol && !showPathIcon && (IsPlaceholder || Source == null);

            _imageElement.Visibility = showImage ? Visibility.Visible : Visibility.Collapsed;
            _placeholderText.Visibility = showText ? Visibility.Visible : Visibility.Collapsed;

            _placeholderText.Text = Initials ?? "";

            // For PlaceholderSymbol: use cross-platform path rendering
            // SymbolIcon uses Segoe MDL2 Assets font which is not available in browser/WASM
            if (showSymbol && PlaceholderSymbol.HasValue)
            {
                if (OperatingSystem.IsWindows())
                {
                    // On Windows, SymbolIcon works natively
                    _symbolIcon.Symbol = PlaceholderSymbol.Value;
                    _symbolIcon.Visibility = Visibility.Visible;
                    _iconViewbox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // On non-Windows (browser, mobile), fall back to path data
                    var pathData = Helpers.FloweryPathHelpers.GetSymbolPathData(PlaceholderSymbol.Value);
                    if (!string.IsNullOrEmpty(pathData))
                    {
                        try
                        {
                            var geometry = Helpers.FloweryPathHelpers.ParseGeometry(pathData);
                            _pathIcon.Data = geometry;
                            _symbolIcon.Visibility = Visibility.Collapsed;
                            _iconViewbox.Visibility = Visibility.Visible;
                        }
                        catch
                        {
                            // Fallback to SymbolIcon even if it might not render correctly
                            _symbolIcon.Symbol = PlaceholderSymbol.Value;
                            _symbolIcon.Visibility = Visibility.Visible;
                            _iconViewbox.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        // No path data available, use SymbolIcon as fallback
                        _symbolIcon.Symbol = PlaceholderSymbol.Value;
                        _symbolIcon.Visibility = Visibility.Visible;
                        _iconViewbox.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                _symbolIcon.Visibility = Visibility.Collapsed;
            }

            // Parse and set PathIcon data (for custom SVG paths via IconData property)
            if (showPathIcon && !string.IsNullOrWhiteSpace(IconData))
            {
                try
                {
                    var geometry = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                        typeof(Geometry), IconData);
                    _pathIcon.Data = geometry;
                    _iconViewbox.Visibility = Visibility.Visible;
                }
                catch
                {
                    // If parsing fails, hide the icon
                    _iconViewbox.Visibility = Visibility.Collapsed;
                }
            }
            else if (!showSymbol)
            {
                // Hide iconViewbox when not showing a path icon or symbol via path fallback
                _iconViewbox.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyStatus()
        {
            if (_statusIndicator == null)
                return;

            if (Status == DaisyStatus.None)
            {
                _statusIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            _statusIndicator.Visibility = Visibility.Visible;
            _statusIndicator.Fill = Status switch
            {
                DaisyStatus.Online => DaisyResourceLookup.GetBrush("DaisySuccessBrush"),
                DaisyStatus.Offline => DaisyResourceLookup.GetBrush("DaisyErrorBrush"),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        private void ApplyRing()
        {
            if (_outerBorder == null)
                return;

            if (HasRing)
            {
                // Get brush key and fallback color for each ring color
                var (ringBrushKey, fallbackColor) = RingColor switch
                {
                    DaisyColor.Primary => ("DaisyPrimaryBrush", Color.FromArgb(255, 99, 102, 241)),    // Indigo
                    DaisyColor.Secondary => ("DaisySecondaryBrush", Color.FromArgb(255, 244, 114, 182)), // Pink
                    DaisyColor.Accent => ("DaisyAccentBrush", Color.FromArgb(255, 45, 212, 191)),      // Teal
                    DaisyColor.Neutral => ("DaisyNeutralBrush", Color.FromArgb(255, 64, 64, 64)),      // Gray
                    DaisyColor.Success => ("DaisySuccessBrush", Color.FromArgb(255, 34, 197, 94)),     // Green
                    DaisyColor.Warning => ("DaisyWarningBrush", Color.FromArgb(255, 251, 191, 36)),    // Amber
                    DaisyColor.Info => ("DaisyInfoBrush", Color.FromArgb(255, 56, 189, 248)),          // Sky blue
                    DaisyColor.Error => ("DaisyErrorBrush", Color.FromArgb(255, 248, 113, 113)),       // Red
                    DaisyColor.Default => ("DaisyPrimaryBrush", Color.FromArgb(255, 99, 102, 241)),
                    _ => ("DaisyPrimaryBrush", Color.FromArgb(255, 99, 102, 241))
                };

                var fallbackBrush = new SolidColorBrush(fallbackColor);
                var brush = DaisyResourceLookup.GetBrush(ringBrushKey, fallbackBrush);
                _outerBorder.BorderBrush = brush;
                _outerBorder.BorderThickness = new Thickness(GetRingThickness());
            }
            else
            {
                _outerBorder.BorderBrush = null;
                _outerBorder.BorderThickness = new Thickness(0);
            }
        }

        /// <summary>
        /// Gets the ring thickness based on the current Size.
        /// Smaller avatars get thinner rings for visual proportion.
        /// </summary>
        private double GetRingThickness() => Size switch
        {
            DaisySize.ExtraSmall => 1,
            DaisySize.Small => 2,
            _ => 3
        };

        #endregion
    }
}

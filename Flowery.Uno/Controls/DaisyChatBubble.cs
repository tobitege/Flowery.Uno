using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// Color variant for DaisyChatBubble.
    /// </summary>
    public enum DaisyChatBubbleVariant
    {
        Default,
        Neutral,
        Primary,
        Secondary,
        Accent,
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// A chat bubble control styled after DaisyUI's Chat component.
    /// Supports left/right alignment (IsEnd), avatar images, headers, and footers.
    /// </summary>
    public partial class DaisyChatBubble : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _avatarContainer;
        private Image? _avatarImage;
        private TextBlock? _headerTextBlock;
        private Border? _bubbleBorder;
        private ContentPresenter? _contentPresenter;
        private TextBlock? _footerTextBlock;
        private object? _userContent;

        public DaisyChatBubble()
        {
            DefaultStyleKey = typeof(DaisyChatBubble);
            IsTabStop = false;

            // Default alignment
            HorizontalAlignment = HorizontalAlignment.Left;

            // Chat bubbles should not render neumorphic shadows by default.
            DaisyNeumorphic.SetIsEnabled(this, false);
        }

        #region Dependency Properties

        public static readonly DependencyProperty IsEndProperty =
            DependencyProperty.Register(
                nameof(IsEnd),
                typeof(bool),
                typeof(DaisyChatBubble),
                new PropertyMetadata(false, OnLayoutChanged));

        /// <summary>
        /// When true, the bubble is aligned to the right (end of chat).
        /// </summary>
        public bool IsEnd
        {
            get => (bool)GetValue(IsEndProperty);
            set => SetValue(IsEndProperty, value);
        }

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(
                nameof(ImageSource),
                typeof(ImageSource),
                typeof(DaisyChatBubble),
                new PropertyMetadata(null, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the avatar image source.
        /// </summary>
        public ImageSource? ImageSource
        {
            get => (ImageSource?)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(DaisyChatBubble),
                new PropertyMetadata(null, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the header text (typically the user name).
        /// </summary>
        public string? Header
        {
            get => (string?)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register(
                nameof(Footer),
                typeof(string),
                typeof(DaisyChatBubble),
                new PropertyMetadata(null, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the footer text (typically timestamp or delivery status).
        /// </summary>
        public string? Footer
        {
            get => (string?)GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyChatBubbleVariant),
                typeof(DaisyChatBubble),
                new PropertyMetadata(DaisyChatBubbleVariant.Default, OnAppearanceChanged));

        public DaisyChatBubbleVariant Variant
        {
            get => (DaisyChatBubbleVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyChatBubble),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyChatBubble bubble && bubble._rootGrid != null)
            {
                bubble.BuildVisualTree();
                bubble.ApplyAll();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyChatBubble bubble)
            {
                bubble.ApplyAll();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null && ReferenceEquals(Content, _rootGrid))
            {
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

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _bubbleBorder ?? base.GetNeumorphicHostElement();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Check if we need to capture user content
            if (Content != null && !ReferenceEquals(Content, _rootGrid))
            {
                _userContent = Content;
                Content = null;
            }

            // Grid: Column 0 = Avatar (if IsEnd=false), Column 1 = Content
            // Grid: Column 0 = Content (if IsEnd=true), Column 1 = Avatar
            _rootGrid = new Grid();
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Bubble
            _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // Avatar container
            _avatarContainer = new()
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                VerticalAlignment = VerticalAlignment.Bottom,
                Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 40, 40) }
            };

            _avatarImage = new Image { Stretch = Stretch.UniformToFill };
            _avatarContainer.Child = _avatarImage;

            int avatarColumn = IsEnd ? 1 : 0;
            int contentColumn = IsEnd ? 0 : 1;

            _avatarContainer.Margin = IsEnd ? new Thickness(12, 0, 0, 0) : new Thickness(0, 0, 12, 0);
            Grid.SetColumn(_avatarContainer, avatarColumn);
            Grid.SetRow(_avatarContainer, 0);
            Grid.SetRowSpan(_avatarContainer, 3);
            _rootGrid.Children.Add(_avatarContainer);

            // Update avatar visibility and source
            _avatarContainer.Visibility = ImageSource != null ? Visibility.Visible : Visibility.Collapsed;
            if (ImageSource != null)
            {
                _avatarImage.Source = ImageSource;
            }

            // Header
            _headerTextBlock = new TextBlock
            {
                Text = Header ?? string.Empty,
                Opacity = 0.7,
                Margin = new Thickness(0, 0, 0, 4),
                HorizontalAlignment = IsEnd ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Visibility = string.IsNullOrEmpty(Header) ? Visibility.Collapsed : Visibility.Visible
            };
            Grid.SetColumn(_headerTextBlock, contentColumn);
            Grid.SetRow(_headerTextBlock, 0);
            _rootGrid.Children.Add(_headerTextBlock);

            // Bubble
            _contentPresenter = new ContentPresenter
            {
                Content = _userContent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            _bubbleBorder = new Border
            {
                MinHeight = 32,
                MinWidth = 40,
                MaxWidth = 400,
                CornerRadius = IsEnd ? new CornerRadius(16, 16, 0, 16) : new CornerRadius(16, 16, 16, 0),
                Child = _contentPresenter
            };

            Grid.SetColumn(_bubbleBorder, contentColumn);
            Grid.SetRow(_bubbleBorder, 1);
            _rootGrid.Children.Add(_bubbleBorder);

            // Footer
            _footerTextBlock = new TextBlock
            {
                Text = Footer ?? string.Empty,
                Opacity = 0.5,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = IsEnd ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Visibility = string.IsNullOrEmpty(Footer) ? Visibility.Collapsed : Visibility.Visible
            };
            Grid.SetColumn(_footerTextBlock, contentColumn);
            Grid.SetRow(_footerTextBlock, 2);
            _rootGrid.Children.Add(_footerTextBlock);

            Content = _rootGrid;

            // Update control alignment
            HorizontalAlignment = IsEnd ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_rootGrid == null || _bubbleBorder == null || _contentPresenter == null)
                return;

            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            if (_bubbleBorder == null || _headerTextBlock == null || _footerTextBlock == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Get sizing from tokens (guaranteed by EnsureDefaults)
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            FontSize = DaisyResourceLookup.GetDouble($"DaisySize{sizeKey}FontSize",
                DaisyResourceLookup.GetDefaultFontSize(Size));
            double tertiaryFontSize = DaisyResourceLookup.GetDouble($"DaisySize{sizeKey}TertiaryFontSize", 9);

            // Chat padding uses spacing tokens
            double padding = DaisyResourceLookup.GetDouble("DaisySpacingMedium", 10);
            _bubbleBorder.Padding = new Thickness(padding, padding / 2, padding, padding / 2);
            _headerTextBlock.FontSize = tertiaryFontSize;
            _footerTextBlock.FontSize = tertiaryFontSize;
        }

        private void ApplyColors()
        {
            if (_bubbleBorder == null || _headerTextBlock == null || _footerTextBlock == null)
                return;

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyChatBubbleVariant.Neutral => "Neutral",
                DaisyChatBubbleVariant.Primary => "Primary",
                DaisyChatBubbleVariant.Secondary => "Secondary",
                DaisyChatBubbleVariant.Accent => "Accent",
                DaisyChatBubbleVariant.Info => "Info",
                DaisyChatBubbleVariant.Success => "Success",
                DaisyChatBubbleVariant.Warning => "Warning",
                DaisyChatBubbleVariant.Error => "Error",
                _ => ""
            };

            var (bgBrushKey, fgBrushKey) = Variant switch
            {
                DaisyChatBubbleVariant.Neutral => ("DaisyNeutralBrush", "DaisyNeutralContentBrush"),
                DaisyChatBubbleVariant.Primary => ("DaisyPrimaryBrush", "DaisyPrimaryContentBrush"),
                DaisyChatBubbleVariant.Secondary => ("DaisySecondaryBrush", "DaisySecondaryContentBrush"),
                DaisyChatBubbleVariant.Accent => ("DaisyAccentBrush", "DaisyAccentContentBrush"),
                DaisyChatBubbleVariant.Info => ("DaisyInfoBrush", "DaisyInfoContentBrush"),
                DaisyChatBubbleVariant.Success => ("DaisySuccessBrush", "DaisySuccessContentBrush"),
                DaisyChatBubbleVariant.Warning => ("DaisyWarningBrush", "DaisyWarningContentBrush"),
                DaisyChatBubbleVariant.Error => ("DaisyErrorBrush", "DaisyErrorContentBrush"),
                _ => ("DaisyBase300Brush", "DaisyBaseContentBrush")
            };

            // Check for lightweight styling overrides
            var bgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyChatBubble", $"{variantName}Background")
                : null;
            bgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyChatBubble", "Background");

            var fgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyChatBubble", $"{variantName}Foreground")
                : null;
            fgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyChatBubble", "Foreground");

            var headerFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyChatBubble", "HeaderForeground");
            var footerFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyChatBubble", "FooterForeground");

            _bubbleBorder.Background = bgOverride ?? DaisyResourceLookup.GetBrush(bgBrushKey);
            Foreground = fgOverride ?? DaisyResourceLookup.GetBrush(fgBrushKey);
            _headerTextBlock.Foreground = headerFgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            _footerTextBlock.Foreground = footerFgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
        }

        #endregion
    }
}

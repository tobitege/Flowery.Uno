using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;
using Flowery.Services;
using System;

namespace Flowery.Controls
{
    /// <summary>
    /// A control that displays an icon and/or text with proper scaling.
    /// Encapsulates the Viewbox-wrapping pattern for icons and FontSize for text.
    /// Can be used standalone or as content for buttons, labels, menu items, etc.
    /// </summary>
    /// <remarks>
    /// Usage examples:
    /// <code>
    /// &lt;!-- Icon only --&gt;
    /// &lt;daisy:DaisyIconText IconData="M12 2C8.13..." Size="Medium" /&gt;
    /// 
    /// &lt;!-- Symbol only --&gt;
    /// &lt;daisy:DaisyIconText IconSymbol="Home" Size="Large" /&gt;
    /// 
    /// &lt;!-- Text only --&gt;
    /// &lt;daisy:DaisyIconText Text="Hello" Size="Small" /&gt;
    /// 
    /// &lt;!-- Icon + Text --&gt;
    /// &lt;daisy:DaisyIconText IconSymbol="Save" Text="Save" IconPlacement="Left" /&gt;
    /// </code>
    /// </remarks>
    public partial class DaisyIconText : DaisyBaseContentControl
    {
        protected StackPanel? _rootPanel;
        protected Viewbox? _iconViewbox;
        protected TextBlock? _textBlock;

        #region Dependency Properties

        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.Register(
                nameof(IconData),
                typeof(string),
                typeof(DaisyIconText),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the path data string for a custom icon (24x24 coordinate system).
        /// Takes precedence over IconSymbol if both are set.
        /// </summary>
        public string? IconData
        {
            get => (string?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public static readonly DependencyProperty IconSymbolProperty =
            DependencyProperty.Register(
                nameof(IconSymbol),
                typeof(Symbol?),
                typeof(DaisyIconText),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets a Windows Symbol to display as the icon.
        /// Used when IconData is not set.
        /// </summary>
        public Symbol? IconSymbol
        {
            get => (Symbol?)GetValue(IconSymbolProperty);
            set => SetValue(IconSymbolProperty, value);
        }

        public static readonly DependencyProperty IconGeometryProperty =
            DependencyProperty.Register(
                nameof(IconGeometry),
                typeof(Geometry),
                typeof(DaisyIconText),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets a direct Geometry object to display as the icon.
        /// Takes precedence over IconSymbol but is lower precedence than IconData.
        /// </summary>
        public Geometry? IconGeometry
        {
            get => (Geometry?)GetValue(IconGeometryProperty);
            set => SetValue(IconGeometryProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(DaisyIconText),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the text to display.
        /// </summary>
        public string? Text
        {
            get => (string?)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyIconText),
                new PropertyMetadata(DaisySize.Medium, OnSizeChanged));

        /// <summary>
        /// Gets or sets the size preset. Controls both icon dimensions and font size.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.Register(
                nameof(IconPlacement),
                typeof(IconPlacement),
                typeof(DaisyIconText),
                new PropertyMetadata(IconPlacement.Left, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the placement of the icon relative to the text.
        /// </summary>
        public IconPlacement IconPlacement
        {
            get => (IconPlacement)GetValue(IconPlacementProperty);
            set => SetValue(IconPlacementProperty, value);
        }

        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(DaisyIconText),
                new PropertyMetadata(double.NaN, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the spacing between icon and text.
        /// If NaN (default), auto-computed based on Size.
        /// </summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                nameof(IconSize),
                typeof(double),
                typeof(DaisyIconText),
                new PropertyMetadata(double.NaN, OnSizeChanged));

        /// <summary>
        /// Gets or sets an explicit icon size override.
        /// If NaN (default), auto-computed based on Size.
        /// </summary>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty FontSizeOverrideProperty =
            DependencyProperty.Register(
                nameof(FontSizeOverride),
                typeof(double),
                typeof(DaisyIconText),
                new PropertyMetadata(double.NaN, OnSizeChanged));

        /// <summary>
        /// Gets or sets an explicit font size override.
        /// If NaN (default), auto-computed based on Size.
        /// </summary>
        public double FontSizeOverride
        {
            get => (double)GetValue(FontSizeOverrideProperty);
            set => SetValue(FontSizeOverrideProperty, value);
        }

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyBadgeVariant),
                typeof(DaisyIconText),
                new PropertyMetadata(DaisyBadgeVariant.Default, OnVariantChanged));

        /// <summary>
        /// Gets or sets the color variant of the control.
        /// Resolves the Foreground color based on the theme.
        /// </summary>
        public DaisyBadgeVariant Variant
        {
            get => (DaisyBadgeVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the effective icon size, computing from Size if not explicitly set.
        /// </summary>
        public double EffectiveIconSize =>
            !double.IsNaN(IconSize) ? IconSize : DaisyResourceLookup.GetDefaultIconSize(Size);

        /// <summary>
        /// Gets the effective font size, computing from Size if not explicitly set.
        /// </summary>
        public double EffectiveFontSize =>
            !double.IsNaN(FontSizeOverride) ? FontSizeOverride : DaisyResourceLookup.GetDefaultFontSize(Size);

        /// <summary>
        /// Gets the effective spacing, computing from Size if not explicitly set.
        /// </summary>
        public double EffectiveSpacing =>
            !double.IsNaN(Spacing) ? Spacing : DaisyResourceLookup.GetDefaultIconSpacing(Size);

        /// <summary>
        /// Returns true if an icon should be displayed.
        /// </summary>
        public bool HasIcon => !string.IsNullOrEmpty(IconData) || IconSymbol.HasValue || IconGeometry != null;

        /// <summary>
        /// Returns true if text should be displayed.
        /// </summary>
        public bool HasText => !string.IsNullOrEmpty(Text);

        #endregion

        public DaisyIconText()
        {
            DefaultStyleKey = typeof(DaisyIconText);
            IsTabStop = false;
        }

        private long _foregroundCallbackToken;

        #region Lifecycle

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_foregroundCallbackToken == 0)
            {
                _foregroundCallbackToken = RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundChanged);
            }

            if (_rootPanel != null)
            {
                ApplyTheme();
                return;
            }

            // Watch for Foreground changes to sync internal elements
            BuildVisualTree();
            ApplyTheme();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            if (_foregroundCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(ForegroundProperty, _foregroundCallbackToken);
                _foregroundCallbackToken = 0;
            }
        }

        private void OnForegroundChanged(DependencyObject sender, DependencyProperty dp)
        {
            SyncInternalForeground();
        }

        private void SyncInternalForeground()
        {
            if (_textBlock != null)
                _textBlock.Foreground = Foreground;

            if (_iconViewbox?.Child is Microsoft.UI.Xaml.Shapes.Path path)
                path.Fill = Foreground;
            else if (_iconViewbox?.Child is IconElement icon)
                icon.Foreground = Foreground;
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

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyTheme();
        }

        #endregion

        #region Property Callbacks

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyIconText iconText && iconText.IsLoaded)
            {
                iconText.BuildVisualTree();
            }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyIconText iconText && iconText.IsLoaded)
            {
                iconText.ApplySizing();
            }
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyIconText iconText && iconText.IsLoaded)
            {
                iconText.ApplyLayout();
            }
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyIconText iconText && iconText.IsLoaded)
            {
                iconText.ApplyTheme();
            }
        }

        #endregion

        #region Visual Tree Building

        protected virtual void BuildVisualTree()
        {
            // Create root panel
            _rootPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Build icon if needed
            if (HasIcon)
            {
                _iconViewbox = new Viewbox
                {
                    Stretch = Stretch.Uniform
                };

                UIElement iconContent;
                if (!string.IsNullOrEmpty(IconData))
                {
                    // Custom path data
                    var path = new Microsoft.UI.Xaml.Shapes.Path
                    {
                        Data = FloweryPathHelpers.ParseGeometry(IconData),
                        Width = 24,
                        Height = 24,
                        Stretch = Stretch.Fill
                    };
                    iconContent = path;
                }
                else if (IconGeometry != null)
                {
                    // Clone the geometry to avoid shared-instance issues (see rule #18 in uno.mdc)
                    var clonedGeometry = FloweryPathHelpers.CloneGeometry(IconGeometry);
                    var path = new Microsoft.UI.Xaml.Shapes.Path
                    {
                        Data = clonedGeometry,
                        Width = 24,
                        Height = 24,
                        Stretch = Stretch.Fill
                    };
                    iconContent = path;
                }
                else
                {
                    if (OperatingSystem.IsWindows())
                    {
                        var symbolIcon = new SymbolIcon(IconSymbol!.Value);
                        iconContent = symbolIcon;
                    }
                    else
                    {
                        var pathData = FloweryPathHelpers.GetSymbolPathData(IconSymbol!.Value);
                        if (!string.IsNullOrEmpty(pathData))
                        {
                            var path = new Microsoft.UI.Xaml.Shapes.Path
                            {
                                Data = FloweryPathHelpers.ParseGeometry(pathData),
                                Width = 24,
                                Height = 24,
                                Stretch = Stretch.Fill
                            };
                            iconContent = path;
                        }
                        else
                        {
                            var symbolIcon = new SymbolIcon(IconSymbol!.Value);
                            iconContent = symbolIcon;
                        }
                    }
                }

                _iconViewbox.Child = iconContent;
                _rootPanel.Children.Add(_iconViewbox);
            }

            // Build text if needed
            if (HasText)
            {
                _textBlock = new TextBlock
                {
                    Text = Text,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                _rootPanel.Children.Add(_textBlock);
            }

            // Apply sizing, layout, and theme
            ApplySizing();
            ApplyLayout();
            ApplyTheme();

            // Set as content
            Content = _rootPanel;
        }

        protected virtual void ApplyTheme()
        {
            // If Foreground was externally set (e.g., by parent DaisyButton), respect that value.
            // Only auto-compute foreground when no external value has been set.
            if (ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue)
            {
                // Foreground is externally controlled – just sync to internal elements
                SyncInternalForeground();
                return;
            }

            // Smart Contrast: If background is essentially transparent, 
            // use the variant brush itself (e.g. PrimaryBrush) because it's designed to contrast with Base.
            // If background is solid, use the content brush (e.g. PrimaryContentBrush) designed for that accent.
            var bgBrush = Background as SolidColorBrush;
            var bgColor = bgBrush?.Color;
            bool isTransparent = Background == null || (bgColor.HasValue && bgColor.Value.A < 128);
            string suffix = isTransparent ? "Brush" : "ContentBrush";

            // Resolve foreground from the variant
            var fgKey = Variant switch
            {
                DaisyBadgeVariant.Default => "DaisyBaseContentBrush",
                DaisyBadgeVariant.Neutral => $"DaisyNeutral{suffix}",
                DaisyBadgeVariant.Primary => $"DaisyPrimary{suffix}",
                DaisyBadgeVariant.Secondary => $"DaisySecondary{suffix}",
                DaisyBadgeVariant.Accent => $"DaisyAccent{suffix}",
                DaisyBadgeVariant.Info => $"DaisyInfo{suffix}",
                DaisyBadgeVariant.Success => $"DaisySuccess{suffix}",
                DaisyBadgeVariant.Warning => $"DaisyWarning{suffix}",
                DaisyBadgeVariant.Error => $"DaisyError{suffix}",
                DaisyBadgeVariant.Ghost => "DaisyBaseContentBrush",
                _ => "DaisyBaseContentBrush"
            };

            var brush = DaisyResourceLookup.GetBrush(fgKey);

            // Universal Contrast Guard: If we have a solid background, ensure the foreground actually stands out.
            // This protects against themes with poorly matched ContentBrushes.
            if (!isTransparent && bgColor.HasValue && brush is SolidColorBrush fcb)
            {
                var bgLum = FloweryColorHelpers.GetLuminance(bgColor.Value);
                var fgLum = FloweryColorHelpers.GetLuminance(fcb.Color);

                // If the contrast is too low (delta < 0.3), force a high-contrast fallback
                if (Math.Abs(bgLum - fgLum) < 0.3)
                {
                    // If background is dark, use light text. If light, use dark text.
                    fgKey = bgLum < 0.5 ? "White" : "Black";
                    brush = DaisyResourceLookup.GetBrush(fgKey);
                }
            }

            Foreground = brush;
            SyncInternalForeground();
        }

        protected virtual void ApplySizing()
        {
            if (_iconViewbox != null)
            {
                var iconSize = EffectiveIconSize;
                _iconViewbox.Width = iconSize;
                _iconViewbox.Height = iconSize;
            }

            if (_textBlock != null)
            {
                _textBlock.FontSize = EffectiveFontSize;
                _textBlock.FontWeight = FontWeight;
            }
        }

        protected virtual void ApplyLayout()
        {
            if (_rootPanel == null)
                return;

            var isHorizontal = IconPlacement == IconPlacement.Left || IconPlacement == IconPlacement.Right;
            _rootPanel.Orientation = isHorizontal ? Orientation.Horizontal : Orientation.Vertical;
            _rootPanel.Spacing = HasIcon && HasText ? EffectiveSpacing : 0;

            // Reorder children based on placement
            if (_rootPanel.Children.Count == 2 && _iconViewbox != null && _textBlock != null)
            {
                _rootPanel.Children.Clear();

                if (IconPlacement == IconPlacement.Right || IconPlacement == IconPlacement.Bottom)
                {
                    _rootPanel.Children.Add(_textBlock);
                    _rootPanel.Children.Add(_iconViewbox);
                }
                else
                {
                    _rootPanel.Children.Add(_iconViewbox);
                    _rootPanel.Children.Add(_textBlock);
                }
            }

            if (_rootPanel != null)
            {
                _rootPanel.HorizontalAlignment = HorizontalContentAlignment;
            }
        }

        #endregion
    }
}

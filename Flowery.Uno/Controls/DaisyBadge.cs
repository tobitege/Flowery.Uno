using System;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// A Badge control styled after DaisyUI's Badge component.
    /// Uses a programmatic Border wrapper for cross-platform Background rendering.
    /// </summary>
    public partial class DaisyBadge : DaisyBaseContentControl
    {
        private Border? _border;
        private ContentPresenter? _contentPresenter;

        public DaisyBadge()
        {
            // Set base background to transparent to avoid artifacts behind our pill border
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            IsTabStop = false;
            BuildLayout();
        }

        /// <summary>
        /// Creates the visual tree programmatically to ensure Background renders on all platforms.
        /// On Skia/WASM, ContentControl's default template doesn't render Background properly.
        /// </summary>
        private void BuildLayout()
        {
            _contentPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _border = new Border
            {
                Child = _contentPresenter
            };

            // Set the border as our Content - this is our actual visual tree
            base.Content = _border;
        }


        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyBadgeVariant),
                typeof(DaisyBadge),
                new PropertyMetadata(DaisyBadgeVariant.Default, OnAppearanceChanged));

        /// <summary>
        /// The color variant of the badge.
        /// </summary>
        public DaisyBadgeVariant Variant
        {
            get => (DaisyBadgeVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyBadge),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// The size of the badge.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region IsOutline
        public static readonly DependencyProperty IsOutlineProperty =
            DependencyProperty.Register(
                nameof(IsOutline),
                typeof(bool),
                typeof(DaisyBadge),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// Whether the badge uses outline style.
        /// </summary>
        public bool IsOutline
        {
            get => (bool)GetValue(IsOutlineProperty);
            set => SetValue(IsOutlineProperty, value);
        }
        #endregion

        #region CornerRadius
        public static new readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(DaisyBadge),
                new PropertyMetadata(default(CornerRadius), OnAppearanceChanged));

        /// <summary>
        /// The corner radius of the badge. If not set, sizing tokens are used.
        /// </summary>
        public new CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyBadge badge)
                badge.ApplyAll();
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
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
            return _border ?? base.GetNeumorphicHostElement();
        }

        private void ApplyAll()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            ApplySizing();
            ApplyTheme(resources);
        }

        private void ApplySizing()
        {
            if (_border == null) return;

            // Get sizing from tokens (guaranteed by EnsureDefaults)
            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            var tokenHeight = DaisyResourceLookup.GetDouble($"DaisyBadge{sizeKey}Height", 20);
            var tokenPadding = DaisyResourceLookup.GetThickness($"DaisyBadge{sizeKey}Padding", new Thickness(8, 0, 8, 0));
            var tokenCornerRadius = DaisyResourceLookup.GetDefaultCornerRadius(Size);

            // Apply to _border for cross-platform rendering
            // BUT respect explicit user-set values (check if property was set locally)
            if (ReadLocalValue(MinHeightProperty) == DependencyProperty.UnsetValue)
            {
                _border.MinHeight = tokenHeight;
            }
            else
            {
                _border.MinHeight = MinHeight;
            }
            
            // Pass through explicit Height/Width if set
            if (ReadLocalValue(HeightProperty) != DependencyProperty.UnsetValue)
            {
                _border.Height = Height;
            }
            if (ReadLocalValue(WidthProperty) != DependencyProperty.UnsetValue)
            {
                _border.Width = Width;
            }
            
            if (ReadLocalValue(PaddingProperty) == DependencyProperty.UnsetValue)
            {
                _border.Padding = tokenPadding;
            }
            else
            {
                _border.Padding = Padding;
            }
            
            if (ReadLocalValue(CornerRadiusProperty) != DependencyProperty.UnsetValue)
            {
                _border.CornerRadius = CornerRadius;
            }
            else
            {
                _border.CornerRadius = tokenCornerRadius;
            }

            // FontSize applies to the control for text inheritance
            FontSize = DaisyResourceLookup.GetDouble($"DaisyBadge{sizeKey}FontSize", 10);
        }

        private void ApplyTheme(ResourceDictionary? resources)
        {

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyBadgeVariant.Neutral => "Neutral",
                DaisyBadgeVariant.Primary => "Primary",
                DaisyBadgeVariant.Secondary => "Secondary",
                DaisyBadgeVariant.Accent => "Accent",
                DaisyBadgeVariant.Ghost => "Ghost",
                DaisyBadgeVariant.Info => "Info",
                DaisyBadgeVariant.Success => "Success",
                DaisyBadgeVariant.Warning => "Warning",
                DaisyBadgeVariant.Error => "Error",
                _ => ""
            };

            // Check for lightweight styling overrides
            var bgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", $"{variantName}Background")
                : null;
            bgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", "Background");

            var fgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", $"{variantName}Foreground")
                : null;
            fgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", "Foreground");

            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", "BorderBrush");

            // Base (Default) - get palette colors
            var (bgKey, fgKey) = Variant switch
            {
                DaisyBadgeVariant.Neutral => ("DaisyNeutralBrush", "DaisyNeutralContentBrush"),
                DaisyBadgeVariant.Primary => ("DaisyPrimaryBrush", "DaisyPrimaryContentBrush"),
                DaisyBadgeVariant.Secondary => ("DaisySecondaryBrush", "DaisySecondaryContentBrush"),
                DaisyBadgeVariant.Accent => ("DaisyAccentBrush", "DaisyAccentContentBrush"),
                DaisyBadgeVariant.Ghost => ("DaisyBase200Brush", "DaisyBaseContentBrush"),
                DaisyBadgeVariant.Info => ("DaisyInfoBrush", "DaisyInfoContentBrush"),
                DaisyBadgeVariant.Success => ("DaisySuccessBrush", "DaisySuccessContentBrush"),
                DaisyBadgeVariant.Warning => ("DaisyWarningBrush", "DaisyWarningContentBrush"),
                DaisyBadgeVariant.Error => ("DaisyErrorBrush", "DaisyErrorContentBrush"),
                _ => ("DaisyBase200Brush", "DaisyBaseContentBrush")
            };

            // Apply to _border for cross-platform rendering
            if (_border != null)
            {
                _border.Background = bgOverride ?? DaisyResourceLookup.GetBrush(resources, bgKey);
                _border.BorderBrush = borderOverride ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                _border.BorderThickness = new Thickness(1);
            }
            Foreground = fgOverride ?? DaisyResourceLookup.GetBrush(resources, fgKey);

            // Outline overrides
            if (IsOutline)
            {
                // Check for outline-specific overrides
                var outlineBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", "OutlineBackground");
                var outlineBorderOverride = !string.IsNullOrEmpty(variantName)
                    ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", $"{variantName}OutlineBorderBrush")
                    : null;
                outlineBorderOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", "OutlineBorderBrush");
                var outlineFgOverride = !string.IsNullOrEmpty(variantName)
                    ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", $"{variantName}OutlineForeground")
                    : null;
                outlineFgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyBadge", "OutlineForeground");

                // For outline, border and foreground use the variant color
                var outlineBrush = Variant switch
                {
                    DaisyBadgeVariant.Primary => DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush"),
                    DaisyBadgeVariant.Secondary => DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush"),
                    DaisyBadgeVariant.Accent => DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush"),
                    _ => DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush")
                };

                if (_border != null)
                {
                    _border.Background = outlineBgOverride ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                    _border.BorderBrush = outlineBorderOverride ?? outlineBrush;
                }
                Foreground = outlineFgOverride ?? outlineBrush;
            }
        }


        private static double GetDouble(ResourceDictionary? resources, string key, double fallback)
        {
            if (resources == null)
                return fallback;

            return DaisyResourceLookup.GetDouble(resources, key, fallback);
        }

        private static Thickness GetThickness(ResourceDictionary? resources, string key, Thickness fallback)
        {
            if (resources == null)
                return fallback;

            return DaisyResourceLookup.GetThickness(resources, key, fallback);
        }

        /// <summary>
        /// Routes user-provided Content to the ContentPresenter inside our Border wrapper.
        /// This prevents the Content property from overwriting our custom visual tree.
        /// </summary>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            // If the new content is our border, we set it ourselves - don't do anything
            if (ReferenceEquals(newContent, _border))
                return;

            // Route user content to the ContentPresenter
            if (_contentPresenter != null)
            {
                _contentPresenter.Content = newContent;
            }

            // Restore our Border as the actual Content (this will trigger OnContentChanged again,
            // but the check above will prevent infinite recursion)
            if (_border != null && !ReferenceEquals(base.Content, _border))
            {
                base.Content = _border;
            }
        }

        /// <summary>
        /// Gets or sets the badge content (text or other content).
        /// </summary>
        public object? BadgeContent
        {
            get => _contentPresenter?.Content;
            set
            {
                if (_contentPresenter != null)
                {
                    _contentPresenter.Content = value;
                }
            }
        }
    }
}

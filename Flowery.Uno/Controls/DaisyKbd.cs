using System;
using Flowery.Theming;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// Style variants for DaisyKbd appearance.
    /// </summary>
    public enum DaisyKbdStyle
    {
        /// <summary> No background or border. </summary>
        Ghost,
        /// <summary> Standard 1px border. </summary>
        Flat,
        /// <summary> Classic keyboard look with bottom shadow (0,0,0,3). </summary>
        ThreeDimensional,
        /// <summary> Deep keycap look with thicker bottom shadow (0,0,0,5). </summary>
        Keycap
    }

    /// <summary>
    /// A keyboard key display control styled after DaisyUI's Kbd component.
    /// Inherits from DaisyIconText to handle icon and text layout naturally.
    /// </summary>
    public partial class DaisyKbd : DaisyIconText
    {
        #region Dependency Properties

        public static readonly DependencyProperty KbdStyleProperty =
            DependencyProperty.Register(
                nameof(KbdStyle),
                typeof(DaisyKbdStyle),
                typeof(DaisyKbd),
                new PropertyMetadata(DaisyKbdStyle.ThreeDimensional, OnKbdStyleChanged));

        /// <summary>
        /// Gets or sets the visual style of the keyboard key.
        /// </summary>
        public DaisyKbdStyle KbdStyle
        {
            get => (DaisyKbdStyle)GetValue(KbdStyleProperty);
            set => SetValue(KbdStyleProperty, value);
        }

        private static void OnKbdStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyKbd kbd)
            {
                kbd.ApplyKbdStyle();
                kbd.ApplyTheme();
            }
        }

        #endregion

        public DaisyKbd()
        {
            DefaultStyleKey = typeof(DaisyKbd);
            
            // Set sensible defaults for keyboard keys
            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
            FontFamily = new FontFamily("Consolas, Courier New, monospace");
        }

        protected override void ApplySizing()
        {
            // Call base to handle basic sizing
            base.ApplySizing();

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);

            // Use specialized KBD font sizes if available
            var fontSize = DaisyResourceLookup.GetDouble($"DaisyKbd{sizeKey}FontSize", EffectiveFontSize);
            if (_textBlock != null)
            {
                _textBlock.FontSize = fontSize;
            }

            // Height is controlled by the DaisyKbd Style/Template in DaisyControls.xaml
            // but we can ensure the internal Well reflects the token height
            var height = DaisyResourceLookup.GetDouble($"DaisyKbd{sizeKey}Height", DaisyResourceLookup.GetDefaultHeight(Size));
            Height = height;
            MinHeight = height;
            
            ApplyKbdStyle();
        }

        protected override void ApplyTheme()
        {
            if (KbdStyle == DaisyKbdStyle.Ghost)
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                // Do not return; still need to call ApplyTheme logic for Foreground
            }

            // In light themes, Base100 is light and Base300 is dark.
            // In dark themes, Base100 is dark and Base300 is light.
            // We want Face to be LIGHTER than the Side (Body) shadow for a natural 3D look.
            var b100 = (DaisyResourceLookup.GetBrush("DaisyBase100Brush") as SolidColorBrush)?.Color;
            var b300 = (DaisyResourceLookup.GetBrush("DaisyBase300Brush") as SolidColorBrush)?.Color;
            
            bool isDarkTheme = true; // In dark themes, B100 is darker than B300 (or generally dark)
            if (b100.HasValue && b300.HasValue)
            {
                // We compare relative luminance to definitively determine if it's a light or dark theme context
                isDarkTheme = FloweryColorHelpers.GetLuminance(b100.Value) < FloweryColorHelpers.GetLuminance(b300.Value);
            }

            var bgKey = Variant switch
            {
                DaisyBadgeVariant.Default => isDarkTheme ? "DaisyBase300Brush" : "DaisyBase100Brush",
                DaisyBadgeVariant.Primary => "DaisyPrimaryBrush",
                DaisyBadgeVariant.Secondary => "DaisySecondaryBrush",
                DaisyBadgeVariant.Accent => "DaisyAccentBrush",
                DaisyBadgeVariant.Info => "DaisyInfoBrush",
                DaisyBadgeVariant.Success => "DaisySuccessBrush",
                DaisyBadgeVariant.Warning => "DaisyWarningBrush",
                DaisyBadgeVariant.Error => "DaisyErrorBrush",
                _ => isDarkTheme ? "DaisyBase300Brush" : "DaisyBase100Brush"
            };

            var borderKey = Variant switch
            {
                DaisyBadgeVariant.Default => isDarkTheme ? "DaisyBase100Brush" : "DaisyBase300Brush",
                _ => bgKey
            };

            if (KbdStyle != DaisyKbdStyle.Ghost)
            {
                Background = DaisyResourceLookup.GetBrush(bgKey);
                BorderBrush = DaisyResourceLookup.GetBrush(borderKey);
            }

            // Let base class handle Foreground resolution and internal element syncing
            base.ApplyTheme();
            
            // For themed variants, darken the "Side" slightly to preserve 3D effect
            // But only if it's not a Ghost key (Ghost keys should stay transparent)
            if (Variant != DaisyBadgeVariant.Default && KbdStyle != DaisyKbdStyle.Ghost)
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black) { Opacity = 0.5 };
            }
        }

        private void ApplyKbdStyle()
        {
            switch (KbdStyle)
            {
                case DaisyKbdStyle.Ghost:
                    BorderThickness = new Thickness(0);
                    break;
                case DaisyKbdStyle.Flat:
                    BorderThickness = new Thickness(1);
                    break;
                case DaisyKbdStyle.ThreeDimensional:
                    BorderThickness = new Thickness(0, 0, 0, 3);
                    break;
                case DaisyKbdStyle.Keycap:
                    BorderThickness = new Thickness(0, 0, 0, 5);
                    break;
            }
        }
    }
}

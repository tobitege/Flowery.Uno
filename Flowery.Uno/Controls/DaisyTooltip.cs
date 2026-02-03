using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A tooltip control styled after DaisyUI's Tooltip component.
    /// Wraps content and shows a tooltip on hover.
    /// </summary>
    public partial class DaisyTooltip : DaisyBaseContentControl
    {
        private ToolTip? _toolTip;

        public DaisyTooltip()
        {
            DefaultStyleKey = typeof(DaisyTooltip);
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty TipProperty =
            DependencyProperty.Register(
                nameof(Tip),
                typeof(string),
                typeof(DaisyTooltip),
                new PropertyMetadata(string.Empty, OnTipChanged));

        /// <summary>
        /// The tooltip text to display.
        /// </summary>
        public string Tip
        {
            get => (string)GetValue(TipProperty);
            set => SetValue(TipProperty, value);
        }

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyColor),
                typeof(DaisyTooltip),
                new PropertyMetadata(DaisyColor.Default, OnAppearanceChanged));

        public DaisyColor Variant
        {
            get => (DaisyColor)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(
                nameof(Placement),
                typeof(PlacementMode),
                typeof(DaisyTooltip),
                new PropertyMetadata(PlacementMode.Top, OnPlacementChanged));

        /// <summary>
        /// Where to place the tooltip relative to the content.
        /// </summary>
        public PlacementMode Placement
        {
            get => (PlacementMode)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(DaisyTooltip),
                new PropertyMetadata(false, OnIsOpenChanged));

        /// <summary>
        /// Whether the tooltip is currently visible.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTooltip t) t.UpdateToolTip();
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTooltip t) t.ApplyColors();
        }

        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTooltip t) t.UpdatePlacement();
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTooltip t && t._toolTip != null)
            {
                t._toolTip.IsOpen = (bool)e.NewValue;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_toolTip != null)
            {
                ApplyColors();
                return;
            }

            SetupToolTip();
            ApplyColors();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyColors();
        }

        #endregion

        #region ToolTip Setup

        private void SetupToolTip()
        {
            _toolTip = new ToolTip
            {
                Content = Tip,
                Placement = Placement
            };

            ToolTipService.SetToolTip(this, _toolTip);
            ApplyColors();
        }

        private void UpdateToolTip()
        {
            if (_toolTip != null)
            {
                _toolTip.Content = Tip;
            }
        }

        private void UpdatePlacement()
        {
            if (_toolTip != null)
            {
                _toolTip.Placement = Placement;
            }
        }

        private void ApplyColors()
        {
            if (_toolTip == null) return;

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyColor.Primary => "Primary",
                DaisyColor.Secondary => "Secondary",
                DaisyColor.Accent => "Accent",
                DaisyColor.Neutral => "Neutral",
                DaisyColor.Info => "Info",
                DaisyColor.Success => "Success",
                DaisyColor.Warning => "Warning",
                DaisyColor.Error => "Error",
                _ => ""
            };

            var (bgKey, fgKey) = Variant switch
            {
                DaisyColor.Primary => ("DaisyPrimaryBrush", "DaisyPrimaryContentBrush"),
                DaisyColor.Secondary => ("DaisySecondaryBrush", "DaisySecondaryContentBrush"),
                DaisyColor.Accent => ("DaisyAccentBrush", "DaisyAccentContentBrush"),
                DaisyColor.Neutral => ("DaisyNeutralBrush", "DaisyNeutralContentBrush"),
                DaisyColor.Info => ("DaisyInfoBrush", "DaisyInfoContentBrush"),
                DaisyColor.Success => ("DaisySuccessBrush", "DaisySuccessContentBrush"),
                DaisyColor.Warning => ("DaisyWarningBrush", "DaisyWarningContentBrush"),
                DaisyColor.Error => ("DaisyErrorBrush", "DaisyErrorContentBrush"),
                _ => ("DaisyNeutralBrush", "DaisyNeutralContentBrush")
            };

            // Check for lightweight styling overrides
            var bgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyTooltip", $"{variantName}Background")
                : null;
            bgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyTooltip", "Background");

            var fgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyTooltip", $"{variantName}Foreground")
                : null;
            fgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyTooltip", "Foreground");

            _toolTip.Background = bgOverride ?? DaisyResourceLookup.GetBrush(bgKey);
            _toolTip.Foreground = fgOverride ?? DaisyResourceLookup.GetBrush(fgKey);
        }

        #endregion
    }
}

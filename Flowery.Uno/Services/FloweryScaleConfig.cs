using System.Collections.Generic;

namespace Flowery.Services
{
    /// <summary>
    /// Predefined scale presets for common UI element sizes.
    /// Use these semantic names instead of raw "baseValue,minValue" strings.
    /// </summary>
    public enum FloweryScalePreset
    {
        /// <summary>Custom value - use BaseValue and MinValue properties</summary>
        Custom,

        // ═══════════════════════════════════════════════════════════════
        // FONT SIZES
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Display/Hero text: 32pt base, 20pt minimum</summary>
        FontDisplay,

        /// <summary>Page title: 28pt base, 18pt minimum</summary>
        FontTitle,

        /// <summary>Section heading: 20pt base, 14pt minimum</summary>
        FontHeading,

        /// <summary>Subheading: 16pt base, 11pt minimum</summary>
        FontSubheading,

        /// <summary>Body text: 14pt base, 10pt minimum</summary>
        FontBody,

        /// <summary>Caption/secondary text: 12pt base, 9pt minimum</summary>
        FontCaption,

        /// <summary>Small/fine print: 11pt base, 8pt minimum</summary>
        FontSmall,

        // ═══════════════════════════════════════════════════════════════
        // SPACING / PADDING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Extra large spacing: 40px base</summary>
        SpacingXL,

        /// <summary>Large spacing: 28px base</summary>
        SpacingLarge,

        /// <summary>Medium spacing: 20px base</summary>
        SpacingMedium,

        /// <summary>Small spacing: 12px base</summary>
        SpacingSmall,

        /// <summary>Extra small spacing: 8px base</summary>
        SpacingXS,

        // ═══════════════════════════════════════════════════════════════
        // DIMENSIONS / SIZES
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Card/container width: 400px base</summary>
        CardWidth,

        /// <summary>Card/container height: 120px base, 100px minimum</summary>
        CardHeight,

        /// <summary>Large icon container: 140px base</summary>
        IconContainerLarge,

        /// <summary>Medium icon container: 40px base</summary>
        IconContainerMedium,

        /// <summary>Small icon container: 24px base</summary>
        IconContainerSmall,

        /// <summary>Large icon: 32px base</summary>
        IconLarge,

        /// <summary>Medium icon: 24px base</summary>
        IconMedium,

        /// <summary>Small icon: 16px base</summary>
        IconSmall,

        /// <summary>Button/input height: 40px base</summary>
        ControlHeight,

        /// <summary>Thumbnail size: 80px base</summary>
        Thumbnail,

        /// <summary>Avatar size: 48px base</summary>
        Avatar,

        // ═══════════════════════════════════════════════════════════════
        // SPECIAL COMBINATIONS (from observed usage)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Large panel/dialog: 250px base, 200px minimum</summary>
        PanelLarge,

        /// <summary>Medium panel: 140px base</summary>
        PanelMedium,

        /// <summary>Theme selector/preview: 13pt base, 11pt minimum</summary>
        FontThemeLabel,
    }

    /// <summary>
    /// Configuration class for customizing scale preset values.
    /// Apps can set custom values at startup to override defaults.
    /// </summary>
    public static class FloweryScaleConfig
    {
        // Reference dimensions for 100% scaling (Full HD - fonts at base size at this resolution)
        public static double ReferenceWidth { get; set; } = 1920;
        public static double ReferenceHeight { get; set; } = 1080;
        public static double MinScaleFactor { get; set; } = 0.6;
        public static double MaxScaleFactor { get; set; } = 1.0;

        // Preset value overrides - apps can modify these
        private static readonly Dictionary<FloweryScalePreset, (double baseValue, double? minValue)> _presetValues = new()
        {
            // Fonts
            [FloweryScalePreset.FontDisplay] = (32, 20),
            [FloweryScalePreset.FontTitle] = (28, 18),
            [FloweryScalePreset.FontHeading] = (20, 14),
            [FloweryScalePreset.FontSubheading] = (16, 11),
            [FloweryScalePreset.FontBody] = (14, 10),
            [FloweryScalePreset.FontCaption] = (12, 9),
            [FloweryScalePreset.FontSmall] = (11, 8),

            // Spacing
            [FloweryScalePreset.SpacingXL] = (40, null),
            [FloweryScalePreset.SpacingLarge] = (28, null),
            [FloweryScalePreset.SpacingMedium] = (20, null),
            [FloweryScalePreset.SpacingSmall] = (12, null),
            [FloweryScalePreset.SpacingXS] = (8, null),

            // Dimensions
            [FloweryScalePreset.CardWidth] = (400, null),
            [FloweryScalePreset.CardHeight] = (120, 100),
            [FloweryScalePreset.IconContainerLarge] = (140, null),
            [FloweryScalePreset.IconContainerMedium] = (40, null),
            [FloweryScalePreset.IconContainerSmall] = (24, null),
            [FloweryScalePreset.IconLarge] = (32, null),
            [FloweryScalePreset.IconMedium] = (24, null),
            [FloweryScalePreset.IconSmall] = (16, null),
            [FloweryScalePreset.ControlHeight] = (40, null),
            [FloweryScalePreset.Thumbnail] = (80, null),
            [FloweryScalePreset.Avatar] = (48, null),

            // Special
            [FloweryScalePreset.PanelLarge] = (250, 200),
            [FloweryScalePreset.PanelMedium] = (140, null),
            [FloweryScalePreset.FontThemeLabel] = (13, 11),
        };

        /// <summary>
        /// Gets the base and minimum values for a preset.
        /// </summary>
        public static (double baseValue, double? minValue) GetPresetValues(FloweryScalePreset preset)
        {
            return _presetValues.TryGetValue(preset, out var values) ? values : (0, null);
        }

        /// <summary>
        /// Sets custom values for a preset. Call at app startup.
        /// </summary>
        public static void SetPresetValues(FloweryScalePreset preset, double baseValue, double? minValue = null)
        {
            _presetValues[preset] = (baseValue, minValue);
        }

        /// <summary>
        /// Resets a preset to its default value.
        /// </summary>
        public static void ResetPreset(FloweryScalePreset preset)
        {
            // Re-apply defaults
            var defaults = GetDefaultValues(preset);
            _presetValues[preset] = defaults;
        }

        private static (double, double?) GetDefaultValues(FloweryScalePreset preset) => preset switch
        {
            FloweryScalePreset.FontDisplay => (32, 20),
            FloweryScalePreset.FontTitle => (28, 18),
            FloweryScalePreset.FontHeading => (20, 14),
            FloweryScalePreset.FontSubheading => (16, 11),
            FloweryScalePreset.FontBody => (14, 10),
            FloweryScalePreset.FontCaption => (12, 9),
            FloweryScalePreset.FontSmall => (11, 8),
            FloweryScalePreset.SpacingXL => (40, null),
            FloweryScalePreset.SpacingLarge => (28, null),
            FloweryScalePreset.SpacingMedium => (20, null),
            FloweryScalePreset.SpacingSmall => (12, null),
            FloweryScalePreset.SpacingXS => (8, null),
            FloweryScalePreset.CardWidth => (400, null),
            FloweryScalePreset.CardHeight => (120, 100),
            FloweryScalePreset.IconContainerLarge => (140, null),
            FloweryScalePreset.IconContainerMedium => (40, null),
            FloweryScalePreset.IconContainerSmall => (24, null),
            FloweryScalePreset.IconLarge => (32, null),
            FloweryScalePreset.IconMedium => (24, null),
            FloweryScalePreset.IconSmall => (16, null),
            FloweryScalePreset.ControlHeight => (40, null),
            FloweryScalePreset.Thumbnail => (80, null),
            FloweryScalePreset.Avatar => (48, null),
            FloweryScalePreset.PanelLarge => (250, 200),
            FloweryScalePreset.PanelMedium => (140, null),
            FloweryScalePreset.FontThemeLabel => (13, 11),
            _ => (0, null)
        };
    }
}

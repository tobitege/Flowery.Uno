namespace Flowery.Enums
{
    /// <summary>
    /// Shared color variants matching DaisyUI theme colors.
    /// Used across multiple controls for consistent theming.
    /// </summary>
    public enum DaisyColor
    {
        /// <summary>Default color (typically base content or neutral)</summary>
        Default,
        /// <summary>Primary theme color</summary>
        Primary,
        /// <summary>Secondary theme color</summary>
        Secondary,
        /// <summary>Accent theme color</summary>
        Accent,
        /// <summary>Neutral theme color</summary>
        Neutral,
        /// <summary>Info theme color (informational messages)</summary>
        Info,
        /// <summary>Success theme color (positive/success states)</summary>
        Success,
        /// <summary>Warning theme color (caution/warning states)</summary>
        Warning,
        /// <summary>Error theme color (error/danger states)</summary>
        Error,
        /// <summary>Secondary background color (slightly different from base100)</summary>
        Base200,
        /// <summary>Tertiary background color (different from base200)</summary>
        Base300
    }

    /// <summary>
    /// Shared size variants for DaisyUI controls.
    /// </summary>
    public enum DaisySize
    {
        /// <summary>Extra small size</summary>
        ExtraSmall,
        /// <summary>Small size</summary>
        Small,
        /// <summary>Medium size (default)</summary>
        Medium,
        /// <summary>Large size</summary>
        Large,
        /// <summary>Extra large size</summary>
        ExtraLarge
    }

    /// <summary>
    /// Transition effect for DaisySwap controls.
    /// </summary>
    public enum SwapEffect
    {
        None,
        Rotate,
        Flip
    }

    /// <summary>
    /// Display mode for DaisyThemeController.
    /// </summary>
    public enum ThemeControllerMode
    {
        Toggle,
        Checkbox,
        Swap,
        ToggleWithText,
        ToggleWithIcons
    }

    /// <summary>
    /// Display mode for DaisyThemeRadio.
    /// </summary>
    public enum ThemeRadioMode
    {
        Radio,
        Button
    }

    /// <summary>
    /// Color variant for DaisyToggle.
    /// </summary>
    public enum DaisyToggleVariant
    {
        Default,
        Primary,
        Secondary,
        Accent,
        Success,
        Warning,
        Info,
        Error
    }

    /// <summary>
    /// Visual variant for DaisyInput controls.
    /// </summary>
    public enum DaisyInputVariant
    {
        Bordered,
        Ghost,
        Filled,
        Primary,
        Secondary,
        Accent,
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Specifies the number base for display and input.
    /// </summary>
    public enum DaisyNumberBase
    {
        /// <summary>Decimal (base 10)</summary>
        Decimal,
        /// <summary>Hexadecimal (base 16, prefix 0x)</summary>
        Hexadecimal,
        /// <summary>Binary (base 2, prefix 0b)</summary>
        Binary,
        /// <summary>Octal (base 8, prefix 0o)</summary>
        Octal,
        /// <summary>Color hex (base 16, prefix #, e.g., #FF5733)</summary>
        ColorHex,
        /// <summary>IPv4 address format (e.g., 192.168.1.1)</summary>
        IPAddress
    }

    /// <summary>
    /// Specifies the letter case for hexadecimal digits (A-F).
    /// </summary>
    public enum DaisyHexCase
    {
        /// <summary>Uppercase (0xFF)</summary>
        Upper,
        /// <summary>Lowercase (0xff)</summary>
        Lower
    }

    /// <summary>
    /// Label positioning mode for DaisyInput controls.
    /// </summary>
    public enum DaisyLabelPosition
    {
        /// <summary>No label displayed.</summary>
        None,
        /// <summary>Standard label above input.</summary>
        Top,
        /// <summary>Label floats to top on focus.</summary>
        Floating,
        /// <summary>Label inside border, top-aligned.</summary>
        Inset
    }

    /// <summary>
    /// Variant for DaisyCard layout.
    /// </summary>
    public enum DaisyCardVariant
    {
        Normal,
        Compact,
        Side
    }

    /// <summary>
    /// Visual style variants for DaisyCard.
    /// </summary>
    public enum DaisyCardStyle
    {
        /// <summary>Standard flat card with rounded corners.</summary>
        Default,
        /// <summary>Classic 3D raised panel effect.</summary>
        Beveled,
        /// <summary>3D sunken/inset panel effect.</summary>
        Inset,
        /// <summary>Sharp-edged professional business panel.</summary>
        Panel,
        /// <summary>Modern glassmorphism effect.</summary>
        Glass
    }

    /// <summary>
    /// Background patterns for DaisyCard.
    /// </summary>
    public enum DaisyCardPattern
    {
        None,
        /// <summary>Carbon fiber weave pattern</summary>
        CarbonFiber,
        /// <summary>Subtle dot grid</summary>
        Dots,
        /// <summary>Small square grid</summary>
        Grid,
        /// <summary>Diagonal stripes</summary>
        Stripes,
        /// <summary>Fine grain/noise</summary>
        Noise,
        /// <summary>Hexagonal industrial mesh</summary>
        Honeycomb,
        /// <summary>Technical circuit board lines</summary>
        Circuit,
        /// <summary>2x2 Twill weave pattern</summary>
        Twill,
        /// <summary>Industrial diamond plate metal texture</summary>
        DiamondPlate,
        /// <summary>Fine metal mesh/screen</summary>
        Mesh,
        /// <summary>Perforated metal sheet with holes</summary>
        Perforated,
        /// <summary>Raised circular bumps/grips</summary>
        Bumps,
        /// <summary>Overlapping scale pattern</summary>
        Scales
    }

    /// <summary>
    /// Decorative ornaments for DaisyCard.
    /// </summary>
    public enum DaisyCardOrnament
    {
        None,
        /// <summary>Triangular corner markers</summary>
        Corners,
        /// <summary>Modern decorative corner brackets</summary>
        Brackets,
        /// <summary>Industrial/Tech corner decals</summary>
        Industrial
    }

    /// <summary>
    /// Color variant for DaisyBadge.
    /// </summary>
    public enum DaisyBadgeVariant
    {
        Default,
        Neutral,
        Primary,
        Secondary,
        Accent,
        Ghost,
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Variant for DaisyAlert.
    /// </summary>
    public enum DaisyAlertVariant
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Variant for DaisyCheckBox (with matching color).
    /// </summary>
    public enum DaisyCheckBoxVariant
    {
        Default,
        Primary,
        Secondary,
        Accent,
        Neutral,
        Success,
        Warning,
        Info,
        Error
    }

    /// <summary>
    /// Variant for DaisyRadio (with matching color).
    /// </summary>
    public enum DaisyRadioVariant
    {
        Default,
        Primary,
        Secondary,
        Accent,
        Success,
        Warning,
        Info,
        Error
    }

    /// <summary>
    /// Variant for DaisyProgress.
    /// </summary>
    public enum DaisyProgressVariant
    {
        Default,
        Primary,
        Secondary,
        Accent,
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Color variant for DaisyDivider.
    /// </summary>
    public enum DaisyDividerColor
    {
        Default,
        Neutral,
        Primary,
        Secondary,
        Accent,
        Success,
        Warning,
        Info,
        Error
    }

    /// <summary>
    /// Placement of content for DaisyDivider.
    /// </summary>
    public enum DaisyDividerPlacement
    {
        Default,
        Start,
        End
    }

    /// <summary>
    /// Visual style variants for DaisyDivider.
    /// </summary>
    public enum DaisyDividerStyle
    {
        /// <summary>Default single solid line</summary>
        Solid,
        /// <summary>3D embossed dual-line effect (groove/ridge)</summary>
        Inset,
        /// <summary>Fades from center outward (or start to end)</summary>
        Gradient,
        /// <summary>Decorative geometric shape in center (diamond, circle, etc.)</summary>
        Ornament,
        /// <summary>Curved/wavy line pattern</summary>
        Wave,
        /// <summary>Glowing/neon effect with soft blur</summary>
        Glow,
        /// <summary>Dashed line pattern (- - - -)</summary>
        Dashed,
        /// <summary>Dotted line pattern (• • • •)</summary>
        Dotted,
        /// <summary>Thick center tapering to points at ends</summary>
        Tapered,
        /// <summary>Two parallel lines with gap</summary>
        Double
    }

    /// <summary>
    /// Ornament shape for DaisyDivider when Style is Ornament.
    /// </summary>
    public enum DaisyDividerOrnament
    {
        /// <summary>Diamond shape ◆</summary>
        Diamond,
        /// <summary>Circle shape ●</summary>
        Circle,
        /// <summary>Star shape ✦</summary>
        Star,
        /// <summary>Square shape ■</summary>
        Square
    }

    /// <summary>
    /// Variant for DaisyRange slider.
    /// </summary>
    public enum DaisyRangeVariant
    {
        Default,
        Primary,
        Secondary,
        Accent,
        Success,
        Warning,
        Info,
        Error
    }

    /// <summary>
    /// Shape for DaisyAvatar.
    /// </summary>
    public enum DaisyAvatarShape
    {
        Square,
        Rounded,
        Circle
    }

    /// <summary>
    /// Status indicator for DaisyAvatar.
    /// </summary>
    public enum DaisyStatus
    {
        None,
        Online,
        Offline
    }

    /// <summary>
    /// Active signal for traffic light indicators.
    /// </summary>
    public enum DaisyTrafficLightState
    {
        Green,
        Yellow,
        Red
    }

    /// <summary>
    /// Precision mode for DaisyRating.
    /// </summary>
    public enum RatingPrecision
    {
        /// <summary>Only whole star values (1, 2, 3, 4, 5)</summary>
        Full,
        /// <summary>Half-star increments (0.5, 1, 1.5, 2, ...)</summary>
        Half,
        /// <summary>One decimal place (0.1 increments)</summary>
        Precise
    }

    public enum DaisySelectVariant
    {
        Bordered,
        Ghost,
        Primary,
        Secondary,
        Accent,
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Color variant for DaisySlideToConfirm.
    /// </summary>
    public enum DaisySlideToConfirmVariant
    {
        /// <summary>Default variant (uses Primary color)</summary>
        Default,
        /// <summary>Primary theme color</summary>
        Primary,
        /// <summary>Secondary theme color</summary>
        Secondary,
        /// <summary>Accent theme color</summary>
        Accent,
        /// <summary>Success theme color (positive/confirmation actions)</summary>
        Success,
        /// <summary>Warning theme color (caution actions)</summary>
        Warning,
        /// <summary>Info theme color (informational actions)</summary>
        Info,
        /// <summary>Error theme color (destructive/danger actions)</summary>
        Error
    }

    /// <summary>
    /// 3D depth style for controls like DaisySlideToConfirm and DaisyKbd.
    /// </summary>
    public enum DaisyDepthStyle
    {
        /// <summary>No shadow, standard flat appearance.</summary>
        Flat,
        /// <summary>Subtle 3D effect with a small bottom shadow.</summary>
        ThreeDimensional,
        /// <summary>More pronounced 3D effect with a larger bottom shadow.</summary>
        Raised
    }
}

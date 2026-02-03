namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// Specifies the color channel to display in a color slider.
    /// </summary>
    public enum ColorSliderChannel
    {
        Red,
        Green,
        Blue,
        Alpha,
        Hue,
        Saturation,
        Lightness
    }

    /// <summary>
    /// Specifies the editing mode for the color editor.
    /// </summary>
    public enum ColorEditingMode
    {
        /// <summary>RGB color editing (Red, Green, Blue).</summary>
        Rgb,
        /// <summary>HSL color editing (Hue, Saturation, Lightness).</summary>
        Hsl
    }
}

using System;
using Windows.UI;

namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// Event arguments for color change events.
    /// </summary>
    public sealed class ColorChangedEventArgs(Color color) : EventArgs
    {
        public Color Color { get; } = color;
    }
}

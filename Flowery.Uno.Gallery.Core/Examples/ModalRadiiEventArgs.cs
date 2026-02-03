using System;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed class ModalRadiiEventArgs : EventArgs
    {
        public double TopLeft { get; set; } = 16;
        public double TopRight { get; set; } = 16;
        public double BottomLeft { get; set; } = 16;
        public double BottomRight { get; set; } = 16;
        public string Title { get; set; } = "Modal";
    }
}

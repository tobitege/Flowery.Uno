using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;

namespace Flowery.Uno.Photos.Views
{
    public delegate void ViewerCanvasDrawHandler(ViewerCanvas sender, SKCanvas canvas, Size area);

    /// <summary>
    /// Drawing surface of the viewer. Skia: replaces the Win2D CanvasControl; <see cref="Draw"/> plays the role of its Draw event.
    /// The canvas origin is the top left of the element and drawing outside <c>area</c> is clipped.
    /// </summary>
    public sealed class ViewerCanvas : SKCanvasElement
    {
        public event ViewerCanvasDrawHandler? Draw;

        protected override void RenderOverride(SKCanvas canvas, Size area) => Draw?.Invoke(this, canvas, area);
    }
}

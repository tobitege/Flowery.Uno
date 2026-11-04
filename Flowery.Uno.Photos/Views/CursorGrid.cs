using Microsoft.UI.Input;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Grid that exposes the protected cursor so a cursor can be scoped to one area
    /// (instead of the whole view, where it would leak onto toolbars and panels).
    /// </summary>
    public sealed partial class CursorGrid : Grid
    {
        private InputSystemCursorShape? _shape;

        public void SetCursor(InputSystemCursorShape shape)
        {
            if (_shape == shape)
                return;
            _shape = shape;
            ProtectedCursor = InputSystemCursor.Create(shape);
        }
    }
}

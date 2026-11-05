using Flowery.Uno.Photos.Services.Platform;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>The size the window reopens with, in physical pixels.</summary>
    public readonly record struct RestoredSize(int Width, int Height);

    /// <summary>
    /// The size the window reopens with: the saved size, at least the minimum and at most the work area of the monitor it
    /// opens on. Only the size: when the app moved its window onto a monitor with another scaling before it showed, Uno kept
    /// mapping pointer input with the first monitor's scaling and clicks landed off, so the window opens where the system
    /// places it.
    /// </summary>
    public static class WindowPlacement
    {
        public const int MinimumWidth = 520;
        public const int MinimumHeight = 400;

        /// <summary>The size to reopen with, or null for the default size.</summary>
        /// <param name="saved">The bounds saved on the last close, or null.</param>
        /// <param name="workArea">Work area of the monitor the window opens on, or null when unknown.</param>
        public static RestoredSize? Restore(WindowBounds? saved, DisplayRect? workArea)
        {
            if (saved is not { } s)
                return null;

            var width = Math.Max(s.Width, MinimumWidth);
            var height = Math.Max(s.Height, MinimumHeight);
            if (workArea is { } area)
            {
                // Never behind the taskbar.
                width = Math.Min(width, area.Width);
                height = Math.Min(height, area.Height);
            }
            return new RestoredSize(width, height);
        }
    }
}

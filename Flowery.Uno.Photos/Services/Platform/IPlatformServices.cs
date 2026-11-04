using System.Threading.Tasks;

namespace Flowery.Uno.Photos.Services.Platform
{
    /// <summary>
    /// Everything the app needs from the operating system that Uno does not provide on the Skia desktop head.
    /// Shared code and views only talk to this interface; the three implementations are chosen at runtime.
    /// An action that has no working mechanism on the current OS is reported through <see cref="Features"/> so the UI can hide it.
    /// </summary>
    public interface IPlatformServices
    {
        PlatformFeatures Features { get; }

        /// <summary>Extended only on Windows; macOS and Linux keep the system title bar (spike: no effect / no caption buttons).</summary>
        TitleBarMode TitleBarMode { get; }

        bool MoveToTrash(string path);

        void RevealInFileManager(string path);

        void OpenFolder(string folder);

        void OpenUri(string uri);

        /// <summary>Apps that can open the file. Empty when the OS offers its own chooser instead (<see cref="PlatformFeatures.OpenWithChooser"/>).</summary>
        Task<IReadOnlyList<OpenWithApp>> GetOpenWithAppsAsync(string path);

        /// <summary>Opens the file with <paramref name="app"/>, or shows the system chooser when it is null and the OS has one.</summary>
        Task<bool> OpenWithAsync(string path, OpenWithApp? app = null);

        bool SetAsWallpaper(string path);

        Task<bool> CopyFilesToClipboardAsync(IReadOnlyList<string> paths);

        /// <summary>Copies PNG-encoded bitmap bytes to the clipboard.</summary>
        Task<bool> CopyBitmapToClipboardAsync(byte[] pngBytes);

        /// <summary>The system accent, or null when the OS has no readable one (callers fall back to the theme accent).</summary>
        SystemAccent? GetSystemAccent();

        /// <summary>
        /// Display rectangles, or empty when unavailable (callers then restore the window without clamping).
        /// What a rectangle means differs per OS and is named by <see cref="DisplayGeometry"/>; convert before comparing
        /// with window bounds from another OS.
        /// </summary>
        IReadOnlyList<DisplayRect> GetDisplays();

        /// <summary>The unit and area of the rectangles returned by <see cref="GetDisplays"/>.</summary>
        DisplayGeometry DisplayGeometry { get; }
    }

    [Flags]
    public enum PlatformFeatures
    {
        None = 0,
        Trash = 1 << 0,
        RevealInFileManager = 1 << 1,
        SetWallpaper = 1 << 2,
        /// <summary>The OS shows its own "Open with" dialog (Windows).</summary>
        OpenWithChooser = 1 << 3,
        /// <summary>The app lists the candidate apps itself (macOS, Linux).</summary>
        OpenWithList = 1 << 4,
        CopyFiles = 1 << 5,
        SystemAccent = 1 << 6,
        DisplayInfo = 1 << 7,
    }

    /// <summary>What <see cref="IPlatformServices.GetDisplays"/> returns on this OS.</summary>
    public enum DisplayGeometry
    {
        /// <summary>No display information.</summary>
        None,
        /// <summary>Work area (without the taskbar) in physical pixels. Windows: GetMonitorInfo.</summary>
        WorkAreaPixels,
        /// <summary>Full monitor bounds in physical pixels, panels and docks included. Linux: xrandr.</summary>
        BoundsPixels,
        /// <summary>Full display bounds in points (logical units, multiply by the backing scale for pixels). macOS: NSScreen frame.</summary>
        BoundsPoints,
    }

    public enum TitleBarMode
    {
        /// <summary>System title bar; the app bar is a normal row below it.</summary>
        System,
        /// <summary>Content extends into the title bar (Windows).</summary>
        Extended,
    }

    /// <param name="Name">Display name.</param>
    /// <param name="Id">What the OS needs to launch it: app bundle path on macOS, desktop file id on Linux.</param>
    public sealed record OpenWithApp(string Name, string Id);

    public readonly record struct SystemAccent(byte R, byte G, byte B);

    public readonly record struct DisplayRect(int X, int Y, int Width, int Height, bool IsPrimary);
}

using System.IO;
using System.Threading.Tasks;
using Flowery.Uno.Photos.Services.Platform;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>
    /// File and shell helpers. Same member names as Flowery.WinUI.Photos so the views port unchanged;
    /// everything OS specific goes through <see cref="IPlatformServices"/>.
    /// </summary>
    public static class FileOperations
    {
        public static bool MoveToRecycleBin(string path) => PlatformServices.Current.MoveToTrash(path);

        public static string GetUniquePath(string path)
        {
            if (!File.Exists(path))
                return path;

            var dir = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            var ext = System.IO.Path.GetExtension(path);
            for (var i = 1; i < 10000; i++)
            {
                var candidate = System.IO.Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate))
                    return candidate;
            }
            return System.IO.Path.Combine(dir, $"{name} ({Guid.NewGuid():N}){ext}");
        }

        /// <summary>
        /// Adds <paramref name="extension"/> (with the dot) when the picked path has none.
        /// Skia: the Linux save picker does not append the extension of the chosen file type.
        /// </summary>
        public static string EnsureExtension(string path, string extension) =>
            System.IO.Path.HasExtension(path) ? path : path + extension;

        public static void ShowInExplorer(string path) => PlatformServices.Current.RevealInFileManager(path);

        public static void OpenFolder(string folder) => PlatformServices.Current.OpenFolder(folder);

        public static void OpenUri(string uri) => PlatformServices.Current.OpenUri(uri);

        public static Task<IReadOnlyList<OpenWithApp>> GetOpenWithAppsAsync(string path) =>
            PlatformServices.Current.GetOpenWithAppsAsync(path);

        public static Task<bool> OpenWithAsync(string path, OpenWithApp? app = null) =>
            PlatformServices.Current.OpenWithAsync(path, app);

        public static bool SetAsWallpaper(string path) => PlatformServices.Current.SetAsWallpaper(path);

        public static Task<bool> CopyFilesToClipboardAsync(IReadOnlyList<string> paths) =>
            PlatformServices.Current.CopyFilesToClipboardAsync(paths);

        public static Task<bool> CopyBitmapToClipboardAsync(byte[] pngBytes) =>
            PlatformServices.Current.CopyBitmapToClipboardAsync(pngBytes);

        public static string FormatFileSize(long bytes)
        {
            var culture = Localization.PhotosLocalization.CurrentCulture;
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            double size = bytes;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return unit == 0
                ? string.Format(culture, "{0} {1}", bytes, units[unit])
                : string.Format(culture, "{0:0.#} {1}", size, units[unit]);
        }
    }
}

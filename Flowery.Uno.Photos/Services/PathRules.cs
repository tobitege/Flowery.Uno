using IOPath = System.IO.Path;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>
    /// Path comparison and separator rules. Skia: Windows and macOS file systems ignore case and use different
    /// separators than Linux, which is case sensitive, so the WinUI hard-coded '\' and OrdinalIgnoreCase do not carry over.
    /// </summary>
    public static class PathRules
    {
        public static StringComparer Comparer => OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        public static StringComparison Comparison => OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        /// <summary>Removes trailing separators; a root such as "/" stays as it is.</summary>
        public static string TrimEnd(string path)
        {
            var trimmed = path.TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
            return trimmed.Length == 0 ? path : trimmed;
        }

        /// <summary>True when <paramref name="path"/> is inside <paramref name="folder"/> (not the folder itself).</summary>
        public static bool IsUnder(string path, string folder) =>
            path.StartsWith(TrimEnd(folder) + IOPath.DirectorySeparatorChar, Comparison);

        public static string FolderName(string folder)
        {
            var name = IOPath.GetFileName(TrimEnd(folder));
            return string.IsNullOrEmpty(name) ? folder : name;
        }
    }
}

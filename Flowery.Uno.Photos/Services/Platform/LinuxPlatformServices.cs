using System.Globalization;
using IOPath = System.IO.Path;
using System.Threading.Tasks;

namespace Flowery.Uno.Photos.Services.Platform
{
    /// <summary>
    /// Linux: gio / gsettings / D-Bus. Checked on GNOME (Wayland session, app on X11 through Xwayland); other desktops
    /// get whatever the same freedesktop tools offer and the matching feature flag stays off when a tool is missing.
    /// </summary>
    public sealed class LinuxPlatformServices(ICommandRunner runner, Func<string, bool>? fileExists = null, IReadOnlyList<string>? dataDirs = null)
        : PlatformServicesBase(runner)
    {
        private readonly Func<string, bool> _fileExists = fileExists ?? File.Exists;
        private readonly IReadOnlyList<string> _dataDirs = dataDirs ?? GetDataDirs();

        private PlatformFeatures? _features;

        // Probed once: it looks for tools and asks gsettings about its schemas, which should not repeat on every access.
        public override PlatformFeatures Features => _features ??= DetectFeatures(Runner);

        private static PlatformFeatures DetectFeatures(ICommandRunner runner)
        {
            var features = PlatformFeatures.CopyFiles | PlatformFeatures.DisplayInfo;
            if (runner.IsAvailable("gio"))
                features |= PlatformFeatures.Trash | PlatformFeatures.OpenWithList;
            if (runner.IsAvailable("dbus-send") || runner.IsAvailable("xdg-open"))
                features |= PlatformFeatures.RevealInFileManager;

            // gsettings is installed on KDE and others too, where the GNOME keys do not exist: check the keys, not the tool.
            if (runner.IsAvailable("gsettings"))
            {
                if (HasKey(runner, "org.gnome.desktop.background", "picture-uri"))
                    features |= PlatformFeatures.SetWallpaper;
                if (HasKey(runner, "org.gnome.desktop.interface", "accent-color"))
                    features |= PlatformFeatures.SystemAccent;
            }
            return features;
        }

        private static bool HasKey(ICommandRunner runner, string schema, string key)
        {
            var result = runner.Run(Commands.ListKeys(schema));
            return result.Success && result.Output.Split('\n', StringSplitOptions.TrimEntries).Contains(key);
        }

        public override bool MoveToTrash(string path)
        {
            if (!File.Exists(path))
                return false;
            return Runner.Run(Commands.Trash(path)).Success && !File.Exists(path);
        }

        public override void RevealInFileManager(string path)
        {
            if (Runner.IsAvailable("dbus-send") && Runner.Run(Commands.Reveal(path)).Success)
                return;
            // No FileManager1 on the bus: open the containing folder instead.
            OpenFolder(DirectoryOf(path));
        }

        public override void OpenFolder(string folder) => Runner.Start(Commands.Open(folder));

        public override void OpenUri(string uri) => Runner.Start(Commands.Open(uri));

        public override Task<IReadOnlyList<OpenWithApp>> GetOpenWithAppsAsync(string path)
        {
            var result = Runner.Run(Commands.MimeApps(Commands.MimeTypeFor(path)));
            IReadOnlyList<OpenWithApp> apps = result.Success ? ParseMimeApps(result.Output) : [];
            return Task.FromResult(apps);
        }

        public override Task<bool> OpenWithAsync(string path, OpenWithApp? app = null)
        {
            if (app == null)
                return Task.FromResult(false);
            var desktopFile = FindDesktopFile(app.Id);
            return Task.FromResult(desktopFile != null && Runner.Start(Commands.Launch(desktopFile, path)));
        }

        public override bool SetAsWallpaper(string path)
        {
            var uri = ToFileUri(path);
            // GNOME has a light and a dark key; set both so the change shows in either style.
            var light = Runner.Run(Commands.SetWallpaper("picture-uri", uri));
            var dark = Runner.Run(Commands.SetWallpaper("picture-uri-dark", uri));
            return light.Success && dark.Success;
        }

        public override Task<bool> CopyFilesToClipboardAsync(IReadOnlyList<string> paths) => CopyStorageItemsAsync(paths);

        public override SystemAccent? GetSystemAccent()
        {
            var result = Runner.Run(Commands.GetAccentColor());
            return result.Success ? ParseAccent(result.Output) : null;
        }

        public override DisplayGeometry DisplayGeometry => DisplayGeometry.BoundsPixels;

        public override IReadOnlyList<DisplayRect> GetDisplays()
        {
            var result = Runner.Run(Commands.ListMonitors());
            return result.Success ? ParseMonitors(result.Output) : [];
        }

        private string? FindDesktopFile(string desktopId)
        {
            foreach (var dir in _dataDirs)
            {
                var candidate = $"{dir}/applications/{desktopId}";
                if (_fileExists(candidate))
                    return candidate;
            }
            return null;
        }

        private static List<string> GetDataDirs()
        {
            var dirs = new List<string>();
            var home = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            dirs.Add(string.IsNullOrWhiteSpace(home)
                ? IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share")
                : home);
            var system = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
            dirs.AddRange((string.IsNullOrWhiteSpace(system) ? "/usr/local/share:/usr/share" : system)
                .Split(':', StringSplitOptions.RemoveEmptyEntries));
            return dirs;
        }

        // Paths here are always POSIX, whatever OS the code is compiled or tested on, so no System.Uri / Path calls.
        private static string DirectoryOf(string path)
        {
            var slash = path.LastIndexOf('/');
            return slash > 0 ? path[..slash] : slash == 0 ? "/" : path;
        }

        public static string ToFileUri(string path) =>
            "file://" + string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

        /// <summary>The command lines. Kept in one place so tests can pin them.</summary>
        public static class Commands
        {
            public static CommandSpec Trash(string path) => new("gio", "trash", path);

            public static CommandSpec Reveal(string path) => new("dbus-send", "--session", "--print-reply",
                "--dest=org.freedesktop.FileManager1", "--type=method_call", "/org/freedesktop/FileManager1",
                "org.freedesktop.FileManager1.ShowItems", $"array:string:{ToFileUri(path)}", "string:");

            public static CommandSpec Open(string target) => new("xdg-open", target);

            public static CommandSpec MimeApps(string mimeType) => new("gio", "mime", mimeType);

            public static CommandSpec Launch(string desktopFile, string path) => new("gio", "launch", desktopFile, path);

            public static CommandSpec SetWallpaper(string key, string uri) =>
                new("gsettings", "set", "org.gnome.desktop.background", key, uri);

            public static CommandSpec ListKeys(string schema) => new("gsettings", "list-keys", schema);

            public static CommandSpec GetAccentColor() => new("gsettings", "get", "org.gnome.desktop.interface", "accent-color");

            public static CommandSpec ListMonitors() => new("xrandr", "--listmonitors");

            public static string MimeTypeFor(string path) => IOPath.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".jpe" or ".jfif" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".ico" => "image/vnd.microsoft.icon",
                ".tif" or ".tiff" => "image/tiff",
                ".heic" or ".heif" => "image/heif",
                ".avif" => "image/avif",
                ".jxr" or ".wdp" => "image/jxr",
                _ => "image/jpeg",
            };
        }

        /// <summary>
        /// Parses <c>gio mime image/jpeg</c>:
        /// <code>
        /// Registered applications:
        ///     org.gnome.Loupe.desktop
        ///     gimp.desktop
        /// Recommended applications:
        ///     org.gnome.Loupe.desktop
        /// </code>
        /// Registered apps first, recommended ones only if the first section is missing; duplicates removed.
        /// </summary>
        public static IReadOnlyList<OpenWithApp> ParseMimeApps(string output)
        {
            List<OpenWithApp> apps = [];
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (var raw in output.Split('\n'))
            {
                var line = raw.Trim();
                // Only bare desktop ids; "Default application for ...: id" repeats one of them.
                if (!line.EndsWith(".desktop", StringComparison.Ordinal) || line.Any(char.IsWhiteSpace) || !seen.Add(line))
                    continue;
                apps.Add(new OpenWithApp(ToDisplayName(line), line));
            }
            return apps;
        }

        private static string ToDisplayName(string desktopId)
        {
            var name = desktopId[..^".desktop".Length];
            // org.gnome.Loupe -> Loupe
            var lastDot = name.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < name.Length - 1)
                name = name[(lastDot + 1)..];
            return name.Length == 0 ? desktopId : char.ToUpperInvariant(name[0]) + name[1..];
        }

        /// <summary>GNOME 47+ accent names. Older GNOME and other desktops have no such key and get null.</summary>
        public static SystemAccent? ParseAccent(string output) => output.Trim().Trim('\'') switch
        {
            "blue" => new SystemAccent(0x35, 0x84, 0xE4),
            "teal" => new SystemAccent(0x21, 0x90, 0xA4),
            "green" => new SystemAccent(0x3A, 0x94, 0x4A),
            "yellow" => new SystemAccent(0xC8, 0x88, 0x00),
            "orange" => new SystemAccent(0xED, 0x5B, 0x00),
            "red" => new SystemAccent(0xE6, 0x2D, 0x42),
            "pink" => new SystemAccent(0xD5, 0x61, 0x99),
            "purple" => new SystemAccent(0x91, 0x41, 0xAC),
            "slate" => new SystemAccent(0x6F, 0x83, 0x96),
            _ => null,
        };

        /// <summary>
        /// Parses <c>xrandr --listmonitors</c> lines such as <c> 0: +*eDP-1 1920/344x1080/194+0+0  eDP-1</c>.
        /// The leading <c>*</c> marks the primary monitor.
        /// </summary>
        public static IReadOnlyList<DisplayRect> ParseMonitors(string output)
        {
            List<DisplayRect> displays = [];
            foreach (var raw in output.Split('\n'))
            {
                var line = raw.Trim();
                var colon = line.IndexOf(':');
                if (colon <= 0 || !int.TryParse(line[..colon], NumberStyles.None, CultureInfo.InvariantCulture, out _))
                    continue;

                var parts = line[(colon + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;

                if (TryParseGeometry(parts[1], out var x, out var y, out var width, out var height))
                    displays.Add(new DisplayRect(x, y, width, height, parts[0].Contains('*')));
            }
            return displays;
        }

        // 1920/344x1080/194+0+0 : width[/mm]xheight[/mm]+x+y
        private static bool TryParseGeometry(string text, out int x, out int y, out int width, out int height)
        {
            x = y = width = height = 0;
            var plus = text.IndexOfAny(['+', '-'], 1);
            var size = plus < 0 ? text : text[..plus];
            var xIndex = size.IndexOf('x');
            if (plus < 0 || xIndex < 0)
                return false;

            var position = text[plus..];
            var secondSign = position.IndexOfAny(['+', '-'], 1);
            if (secondSign < 0)
                return false;

            return TryParseDimension(size[..xIndex], out width)
                && TryParseDimension(size[(xIndex + 1)..], out height)
                && int.TryParse(position[..secondSign], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out x)
                // xrandr prints a negative offset as "+-200".
                && int.TryParse(position[secondSign..].Replace("+-", "-"), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out y);
        }

        private static bool TryParseDimension(string text, out int value)
        {
            var slash = text.IndexOf('/');
            return int.TryParse(slash < 0 ? text : text[..slash], NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }
    }
}

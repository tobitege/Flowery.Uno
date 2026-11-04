using System.Threading.Tasks;

namespace Flowery.Uno.Photos.Services.Platform
{
    /// <summary>The macOS calls that have no command-line equivalent. Faked in tests.</summary>
    public interface IMacNative
    {
        bool TrashItem(string path);

        bool SetDesktopImage(string path);

        IReadOnlyList<OpenWithApp> GetAppsToOpen(string path);

        IReadOnlyList<DisplayRect> GetDisplays();
    }

    /// <summary>
    /// macOS: <c>open</c> and <c>defaults</c> for what they cover, Objective-C interop (<see cref="MacNative"/>) for the rest.
    /// Uno cannot copy file lists to the clipboard here, so that action stays hidden.
    /// </summary>
    public sealed class MacPlatformServices(ICommandRunner runner, IMacNative native) : PlatformServicesBase(runner)
    {
        public override PlatformFeatures Features =>
            PlatformFeatures.Trash
            | PlatformFeatures.RevealInFileManager
            | PlatformFeatures.SetWallpaper
            | PlatformFeatures.OpenWithList
            | PlatformFeatures.SystemAccent
            | PlatformFeatures.DisplayInfo;

        public override bool MoveToTrash(string path) => File.Exists(path) && native.TrashItem(path) && !File.Exists(path);

        public override void RevealInFileManager(string path) => Runner.Start(Commands.Reveal(path));

        public override void OpenFolder(string folder) => Runner.Start(Commands.Open(folder));

        public override void OpenUri(string uri) => Runner.Start(Commands.Open(uri));

        public override Task<IReadOnlyList<OpenWithApp>> GetOpenWithAppsAsync(string path) =>
            Task.FromResult(native.GetAppsToOpen(path));

        public override Task<bool> OpenWithAsync(string path, OpenWithApp? app = null) =>
            Task.FromResult(app != null && Runner.Start(Commands.OpenWith(app.Id, path)));

        public override bool SetAsWallpaper(string path) => native.SetDesktopImage(path);

        public override SystemAccent? GetSystemAccent()
        {
            var result = Runner.Run(Commands.GetAccentColor());
            // The key is absent in the default multicolor setting, which is blue.
            return ParseAccent(result.Success ? result.Output : null);
        }

        public override IReadOnlyList<DisplayRect> GetDisplays() => native.GetDisplays();

        public override DisplayGeometry DisplayGeometry => DisplayGeometry.BoundsPoints;

        public static class Commands
        {
            public static CommandSpec Reveal(string path) => new("/usr/bin/open", "-R", path);

            public static CommandSpec Open(string target) => new("/usr/bin/open", target);

            public static CommandSpec OpenWith(string appPath, string path) => new("/usr/bin/open", "-a", appPath, path);

            public static CommandSpec GetAccentColor() => new("/usr/bin/defaults", "read", "-g", "AppleAccentColor");
        }

        /// <summary>
        /// <c>defaults read -g AppleAccentColor</c> prints -1 (graphite) or 0 to 6; no output or an error means the default blue.
        /// </summary>
        public static SystemAccent ParseAccent(string? output) => output?.Trim() switch
        {
            "-1" => new SystemAccent(0x8C, 0x8C, 0x8C),
            "0" => new SystemAccent(0xFF, 0x52, 0x57),
            "1" => new SystemAccent(0xF7, 0x82, 0x1B),
            "2" => new SystemAccent(0xFF, 0xC6, 0x00),
            "3" => new SystemAccent(0x62, 0xBA, 0x46),
            "5" => new SystemAccent(0xA5, 0x50, 0xA7),
            "6" => new SystemAccent(0xF7, 0x4F, 0x9E),
            _ => new SystemAccent(0x00, 0x7A, 0xFF),
        };
    }
}

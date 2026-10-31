namespace Flowery.Uno.Photos.Services.Platform
{
    /// <summary>Picks the implementation for the running OS. Tests replace it through <see cref="Override"/>.</summary>
    public static class PlatformServices
    {
        public static IPlatformServices Current { get; private set; } = Create();

        public static void Override(IPlatformServices services) => Current = services;

        private static IPlatformServices Create()
        {
            var runner = new ProcessCommandRunner();
            if (OperatingSystem.IsWindows())
                return new WindowsPlatformServices(runner);
            if (OperatingSystem.IsMacOS())
                return new MacPlatformServices(runner, new MacNative());
            return new LinuxPlatformServices(runner);
        }
    }
}

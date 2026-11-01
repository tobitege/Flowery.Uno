using IOPath = System.IO.Path;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>Where the app keeps its state (settings, caches, crash log). Public so the tests need no InternalsVisibleTo.</summary>
    public static class StateDirectory
    {
        public static string Resolve()
        {
            var configuredRoot = Environment.GetEnvironmentVariable("FLOWERY_STATE_ROOT");
            if (!string.IsNullOrWhiteSpace(configuredRoot))
            {
                if (!IOPath.IsPathFullyQualified(configuredRoot))
                {
                    throw new ArgumentException("FLOWERY_STATE_ROOT must be an absolute directory path.");
                }

                return IOPath.GetFullPath(configuredRoot);
            }

            return IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flowery.Uno.Photos");
        }
    }
}

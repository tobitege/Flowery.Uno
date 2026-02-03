namespace Flowery.Uno.Gallery
{
    internal static class GalleryDiagnostics
    {
        private const string Category = "Gallery";

        public static event EventHandler<string>? MessageLogged;

        static GalleryDiagnostics()
        {
            FloweryLogging.Logged += OnLogged;
        }

        public static void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            FloweryLogging.Log(FloweryLogLevel.Info, Category, message);
        }

        private static void OnLogged(object? sender, FloweryLogEntry entry)
        {
            if (!string.Equals(entry.Category, Category, StringComparison.Ordinal))
            {
                return;
            }

            var formatted = FloweryLogging.Format(entry);
            MessageLogged?.Invoke(null, formatted);
        }
    }
}

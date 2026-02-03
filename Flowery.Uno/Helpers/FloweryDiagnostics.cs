namespace Flowery.Helpers
{
    public static class FloweryDiagnostics
    {
        public static event EventHandler<string>? MessageLogged;

        static FloweryDiagnostics()
        {
            FloweryLogging.Logged += OnLogged;
        }

        public static void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            FloweryLogging.Log(FloweryLogLevel.Info, FloweryLogging.DefaultCategory, message);
        }

        private static void OnLogged(object? sender, FloweryLogEntry entry)
        {
            if (!string.Equals(entry.Category, FloweryLogging.DefaultCategory, StringComparison.Ordinal))
            {
                return;
            }

            var formatted = FloweryLogging.Format(entry);
            MessageLogged?.Invoke(null, formatted);
        }
    }
}

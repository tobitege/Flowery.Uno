using System.Text;

namespace Flowery.Services
{
    public enum FloweryLogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    public sealed class FloweryLogEntry
    {
        public FloweryLogEntry(DateTimeOffset timestamp, FloweryLogLevel level, string category, string message, Exception? exception)
        {
            Timestamp = timestamp;
            Level = level;
            Category = category;
            Message = message;
            Exception = exception;
        }

        public DateTimeOffset Timestamp { get; }
        public FloweryLogLevel Level { get; }
        public string Category { get; }
        public string Message { get; }
        public Exception? Exception { get; }
    }

    public sealed class FloweryLoggingOptions
    {
        public bool Enabled { get; set; }
        public FloweryLogLevel MinimumLevel { get; set; }
        public bool IncludeTimestamp { get; set; }
        public bool IncludeLevel { get; set; }
        public bool IncludeCategory { get; set; }
    }

    public interface IFloweryLogSink
    {
        void Log(FloweryLogEntry entry);
    }

    public static class FloweryLogging
    {
        public const string DefaultCategory = "Flowery";

        private static readonly object s_gate = new();
        private static readonly List<IFloweryLogSink> s_sinks = [];
        private static FloweryLoggingOptions s_options = CreateDefaultOptions();

        static FloweryLogging()
        {
            s_sinks.Add(new FloweryPlatformLogSink());
        }

        public static event EventHandler<FloweryLogEntry>? Logged;

        public static FloweryLoggingOptions Options => s_options;

        public static void Configure(Action<FloweryLoggingOptions> configure)
        {
            if (configure == null)
            {
                return;
            }

            lock (s_gate)
            {
                configure(s_options);
            }
        }

        public static void AddSink(IFloweryLogSink sink)
        {
            if (sink == null)
            {
                return;
            }

            lock (s_gate)
            {
                if (!s_sinks.Contains(sink))
                {
                    s_sinks.Add(sink);
                }
            }
        }

        public static bool RemoveSink(IFloweryLogSink sink)
        {
            if (sink == null)
            {
                return false;
            }

            lock (s_gate)
            {
                return s_sinks.Remove(sink);
            }
        }

        public static bool IsEnabled(FloweryLogLevel level)
        {
            return s_options.Enabled && level >= s_options.MinimumLevel;
        }

        public static void Log(FloweryLogLevel level, string category, string message, Exception? exception = null)
        {
            if (!IsEnabled(level))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var entry = new FloweryLogEntry(
                DateTimeOffset.Now,
                level,
                string.IsNullOrWhiteSpace(category) ? DefaultCategory : category,
                message,
                exception);

            List<IFloweryLogSink> sinks;
            lock (s_gate)
            {
                sinks = new List<IFloweryLogSink>(s_sinks);
            }

            foreach (var sink in sinks)
            {
                try
                {
                    sink.Log(entry);
                }
                catch
                {
                    // Ignore sink failures to avoid crashing the app while logging.
                }
            }

            Logged?.Invoke(null, entry);
        }

        public static string Format(FloweryLogEntry entry)
        {
            var builder = new StringBuilder(128);

            if (s_options.IncludeTimestamp)
            {
                builder.Append(entry.Timestamp.ToString("O")).Append(' ');
            }

            if (s_options.IncludeLevel)
            {
                builder.Append('[').Append(entry.Level).Append(']').Append(' ');
            }

            if (s_options.IncludeCategory && !string.IsNullOrWhiteSpace(entry.Category))
            {
                builder.Append('[').Append(entry.Category).Append(']').Append(' ');
            }

            builder.Append(entry.Message);

            if (entry.Exception != null)
            {
                builder.Append(' ').Append(entry.Exception);
            }

            return builder.ToString();
        }

        private static FloweryLoggingOptions CreateDefaultOptions()
        {
            var options = new FloweryLoggingOptions
            {
#if DEBUG
                Enabled = true,
                MinimumLevel = FloweryLogLevel.Debug,
#else
                Enabled = false,
                MinimumLevel = FloweryLogLevel.Warning,
#endif
                IncludeTimestamp = false,
                IncludeLevel = true,
                IncludeCategory = true
            };

            return options;
        }
    }
}

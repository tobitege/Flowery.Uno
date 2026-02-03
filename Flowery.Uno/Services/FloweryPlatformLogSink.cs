using System;
#if __ANDROID__
using Android.Util;
#endif
#if __IOS__ || __MACOS__
using System.Diagnostics;
#endif

namespace Flowery.Services
{
    internal sealed class FloweryPlatformLogSink : IFloweryLogSink
    {
        public void Log(FloweryLogEntry entry)
        {
            var message = FloweryLogging.Format(entry);
            FloweryPlatformLogger.WriteLine(entry.Level, entry.Category, message);
        }
    }

    internal static class FloweryPlatformLogger
    {
        internal static void WriteLine(FloweryLogLevel level, string category, string message)
        {
#if __ANDROID__
            var tag = string.IsNullOrWhiteSpace(category) ? FloweryLogging.DefaultCategory : category;
            Log.WriteLine(ToAndroidPriority(level), tag, message);
#elif __IOS__ || __MACOS__
            Debug.WriteLine(message);
#elif __WASM__ || HAS_UNO_WASM
            System.Console.WriteLine(message);
#elif HAS_UNO_SKIA
            System.Console.WriteLine(message);
            System.Diagnostics.Debug.WriteLine(message);
#else
            System.Diagnostics.Debug.WriteLine(message);
#endif
        }

#if __ANDROID__
        private static LogPriority ToAndroidPriority(FloweryLogLevel level)
        {
            return level switch
            {
                FloweryLogLevel.Trace => LogPriority.Verbose,
                FloweryLogLevel.Debug => LogPriority.Debug,
                FloweryLogLevel.Info => LogPriority.Info,
                FloweryLogLevel.Warning => LogPriority.Warn,
                FloweryLogLevel.Error => LogPriority.Error,
                FloweryLogLevel.Critical => LogPriority.Assert,
                _ => LogPriority.Debug
            };
        }
#endif
    }
}

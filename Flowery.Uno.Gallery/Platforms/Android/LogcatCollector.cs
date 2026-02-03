using System;
using System.IO;
using Android.Util;

namespace Flowery.Uno.Gallery.Droid
{
    public static class LogcatCollector
    {
        public static void CaptureToFile(string fileName = "logcat_dump.txt")
        {
            try
            {
                Log.Info("LogcatCollector", $"Attempting to dump logcat to {fileName}");

                // Get a safe path for Android
                string? dir = null;
                try
                {
                    // Use global:: to avoid confusion with the project namespace "Flowery.Uno.Gallery.Android"
                    dir = global::Android.App.Application.Context?.FilesDir?.AbsolutePath;
                }
                catch { /* Context might not be ready */ }

                if (string.IsNullOrEmpty(dir))
                {
                    dir = "/data/user/0/com.companyname.flowery.uno.gallery/files";
                }

                dir = System.IO.Path.Combine(dir, "Flowery.Uno.Gallery");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var outputPath = System.IO.Path.Combine(dir, fileName);

                // Run logcat -d (dump current log and exit)
                // We use a simplified command to minimize JNI overhead
                using var process = Java.Lang.Runtime.GetRuntime()?.Exec("logcat -d -t 200 *:E");
                if (process?.InputStream != null)
                {
                    using var reader = new StreamReader(process.InputStream);
                    var logContent = reader.ReadToEnd();

                    if (!string.IsNullOrWhiteSpace(logContent))
                    {
                        File.WriteAllText(outputPath, logContent);
                        Log.Info("LogcatCollector", $"Logcat successfully dumped to {outputPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                // NEVER throw from here, just log to system log
                Log.Error("LogcatCollector", $"Critical failure in CaptureToFile: {ex.Message}");
            }
        }
    }
}

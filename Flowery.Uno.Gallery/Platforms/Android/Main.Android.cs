using System;
using System.Reflection;
using Android.App;
using Android.Runtime;
using Android.Util;

namespace Flowery.Uno.Gallery.Droid;

[Application(
    Label = "@string/ApplicationName",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/AppTheme"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => CreateApp(), javaReference, transfer)
    {
    }

    private static Microsoft.UI.Xaml.Application CreateApp()
    {
        try
        {
            Log.Info("FloweryApp", "Creating App instance...");
            return new App();
        }
        catch (Exception ex)
        {
            // Recursively unwrap the exception to find the root cause
            Exception root = ex;
            while (root.InnerException != null)
            {
                Log.Error("FloweryApp", $"Unwrapping: {root.GetType().Name}: {root.Message}");
                root = root.InnerException;
            }

            Log.Error("FloweryApp", $"ROOT CAUSE: {root.GetType().Name}: {root.Message}");
            Log.Error("FloweryApp", $"Stack Trace: {root.StackTrace}");

            // Final attempt to dump logcat for additional context
            LogcatCollector.CaptureToFile("logcat_root_cause.txt");
            throw root;
        }
    }
}

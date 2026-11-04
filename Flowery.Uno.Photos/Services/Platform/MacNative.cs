using System.Runtime.InteropServices;
using IOPath = System.IO.Path;

namespace Flowery.Uno.Photos.Services.Platform
{
    /// <summary>
    /// Objective-C and CoreGraphics interop for macOS, the mechanisms verified in the phase 0 spike.
    /// Thin on purpose: no decisions here, so the logic stays testable in <see cref="MacPlatformServices"/>.
    /// </summary>
    public sealed class MacNative : IMacNative
    {
        private const string ObjC = "/usr/lib/libobjc.dylib";
        private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";
        private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

        [DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
        [DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Msg(IntPtr receiver, IntPtr selector);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Msg(IntPtr receiver, IntPtr selector, IntPtr a);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Msg(IntPtr receiver, IntPtr selector, nuint index);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr MsgStr(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.LPUTF8Str)] string a);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern byte MsgBool(IntPtr receiver, IntPtr selector, IntPtr a, IntPtr b, IntPtr c);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern byte MsgBool(IntPtr receiver, IntPtr selector, IntPtr a, IntPtr b, IntPtr c, IntPtr d);

        [StructLayout(LayoutKind.Sequential)]
        private struct CGRect
        {
            public double X, Y, Width, Height;
        }

        [DllImport(CoreGraphics)] private static extern int CGGetActiveDisplayList(uint maxDisplays, [Out] uint[] displays, out uint count);
        [DllImport(CoreGraphics)] private static extern CGRect CGDisplayBounds(uint display);
        [DllImport(CoreGraphics)] private static extern uint CGMainDisplayID();

        private static bool _appKitLoaded;

        private static IntPtr Sel(string name) => sel_registerName(name);

        private static void EnsureAppKit()
        {
            if (_appKitLoaded)
                return;
            // NSWorkspace and NSScreen live in AppKit, which is not loaded until something asks for it.
            NativeLibrary.TryLoad(AppKit, out _);
            _appKitLoaded = true;
        }

        private static IntPtr FileUrl(string path)
        {
            var nsString = MsgStr(objc_getClass("NSString"), Sel("stringWithUTF8String:"), path);
            return Msg(objc_getClass("NSURL"), Sel("fileURLWithPath:"), nsString);
        }

        private static string? ToManagedString(IntPtr nsString) =>
            nsString == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(Msg(nsString, Sel("UTF8String")));

        public bool TrashItem(string path)
        {
            try
            {
                EnsureAppKit();
                var manager = Msg(objc_getClass("NSFileManager"), Sel("defaultManager"));
                return MsgBool(manager, Sel("trashItemAtURL:resultingItemURL:error:"), FileUrl(path), IntPtr.Zero, IntPtr.Zero) != 0;
            }
            catch
            {
                return false;
            }
        }

        public bool SetDesktopImage(string path)
        {
            try
            {
                EnsureAppKit();
                var workspace = Msg(objc_getClass("NSWorkspace"), Sel("sharedWorkspace"));
                var screens = Msg(objc_getClass("NSScreen"), Sel("screens"));
                var count = (nuint)Msg(screens, Sel("count"));
                if (count == 0)
                    return false;

                var url = FileUrl(path);
                var allSet = true;
                for (nuint i = 0; i < count; i++)
                {
                    var screen = Msg(screens, Sel("objectAtIndex:"), i);
                    allSet &= MsgBool(workspace, Sel("setDesktopImageURL:forScreen:options:error:"), url, screen, IntPtr.Zero, IntPtr.Zero) != 0;
                }
                return allSet;
            }
            catch
            {
                return false;
            }
        }

        public IReadOnlyList<OpenWithApp> GetAppsToOpen(string path)
        {
            try
            {
                EnsureAppKit();
                var workspace = Msg(objc_getClass("NSWorkspace"), Sel("sharedWorkspace"));
                var urls = Msg(workspace, Sel("URLsForApplicationsToOpenURL:"), FileUrl(path));
                var count = (nuint)Msg(urls, Sel("count"));

                List<OpenWithApp> apps = [];
                for (nuint i = 0; i < count; i++)
                {
                    var appPath = ToManagedString(Msg(Msg(urls, Sel("objectAtIndex:"), i), Sel("path")));
                    if (!string.IsNullOrEmpty(appPath))
                        apps.Add(new OpenWithApp(IOPath.GetFileNameWithoutExtension(appPath), appPath));
                }
                return apps;
            }
            catch
            {
                return [];
            }
        }

        public IReadOnlyList<DisplayRect> GetDisplays()
        {
            try
            {
                var ids = new uint[16];
                if (CGGetActiveDisplayList((uint)ids.Length, ids, out var count) != 0)
                    return [];

                var main = CGMainDisplayID();
                List<DisplayRect> displays = [];
                for (var i = 0; i < count; i++)
                {
                    var bounds = CGDisplayBounds(ids[i]);
                    displays.Add(new DisplayRect((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height, ids[i] == main));
                }
                return displays;
            }
            catch
            {
                return [];
            }
        }
    }
}

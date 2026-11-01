using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Flowery.Uno.Photos.Services.Platform
{
    /// <summary>Windows: the same shell calls as Flowery.WinUI.Photos, minus the Windows App SDK APIs Uno does not implement.</summary>
    [SupportedOSPlatform("windows")]
    public sealed class WindowsPlatformServices(ICommandRunner runner) : PlatformServicesBase(runner)
    {
        public override PlatformFeatures Features =>
            PlatformFeatures.Trash
            | PlatformFeatures.RevealInFileManager
            | PlatformFeatures.SetWallpaper
            | PlatformFeatures.OpenWithChooser
            | PlatformFeatures.CopyFiles
            | PlatformFeatures.SystemAccent
            | PlatformFeatures.DisplayInfo;

        // The only OS where AppWindowTitleBar.IsCustomizationSupported() is true on the Skia head.
        public override TitleBarMode TitleBarMode => TitleBarMode.Extended;

        public override bool MoveToTrash(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                return !File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        public override void RevealInFileManager(string path) => Runner.Start(Commands.Reveal(path));

        public override void OpenFolder(string folder) => Runner.Start(Commands.Open(folder));

        public override void OpenUri(string uri) => Runner.Start(Commands.OpenUri(uri));

        // Skia: Launcher.LaunchFileAsync(file, options) is not implemented; the Open With dialog is started through shell32.
        public override Task<bool> OpenWithAsync(string path, OpenWithApp? app = null) =>
            Task.FromResult(File.Exists(path) && Runner.Start(Commands.OpenWith(path)));

        public override bool SetAsWallpaper(string path)
        {
            try
            {
                return Native.SystemParametersInfo(Native.SpiSetDeskWallpaper, 0, path, Native.SpifUpdateIniFile | Native.SpifSendChange);
            }
            catch
            {
                return false;
            }
        }

        // Skia: Uno does not write file lists to the Windows clipboard, so CF_HDROP is set directly.
        public override Task<bool> CopyFilesToClipboardAsync(IReadOnlyList<string> paths) =>
            Task.FromResult(paths.Count > 0 && Native.SetClipboardFileDrop(DropFiles.Build(paths)));

        // Skia: UISettings.GetColorValue(Accent) returns a fixed #3399FF here, so the DWM value is read.
        public override SystemAccent? GetSystemAccent()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                return key?.GetValue("AccentColor") is int value ? ParseAccent(unchecked((uint)value)) : null;
            }
            catch
            {
                return null;
            }
        }

        public override IReadOnlyList<DisplayRect> GetDisplays() => Native.GetWorkAreas();

        public override DisplayGeometry DisplayGeometry => DisplayGeometry.WorkAreaPixels;

        /// <summary>The DWM accent is stored as 0xAABBGGRR.</summary>
        public static SystemAccent ParseAccent(uint abgr) =>
            new((byte)(abgr & 0xFF), (byte)((abgr >> 8) & 0xFF), (byte)((abgr >> 16) & 0xFF));

        public static class Commands
        {
            // explorer parses /select, itself; a quoted /select,"path" is the form that handles spaces.
            public static CommandSpec Reveal(string path) => CommandSpec.Raw("explorer.exe", $"/select,\"{path}\"");

            public static CommandSpec Open(string target) => CommandSpec.Raw("explorer.exe", $"\"{target}\"");

            public static CommandSpec OpenUri(string uri) => new("rundll32.exe", "url.dll,FileProtocolHandler", uri);

            // No quotes: rundll32 hands the rest of the line to OpenAs_RunDLL as the path, spaces included. With quotes the dialog
            // never appears on Windows 11 (checked on the VM, paths with and without spaces).
            public static CommandSpec OpenWith(string path) => CommandSpec.Raw("rundll32.exe", $"shell32.dll,OpenAs_RunDLL {path}");
        }

        /// <summary>Builds the CF_HDROP payload (a DROPFILES header followed by double-null terminated UTF-16 paths).</summary>
        public static class DropFiles
        {
            private const int HeaderSize = 20;

            public static byte[] Build(IReadOnlyList<string> paths)
            {
                var list = string.Concat(paths.Select(p => p + '\0')) + '\0';
                var data = new byte[HeaderSize + Encoding.Unicode.GetByteCount(list)];
                BitConverter.TryWriteBytes(data.AsSpan(0, 4), HeaderSize); // pFiles: offset of the file list
                BitConverter.TryWriteBytes(data.AsSpan(16, 4), 1);        // fWide: UTF-16
                Encoding.Unicode.GetBytes(list, 0, list.Length, data, HeaderSize);
                return data;
            }
        }

        private static class Native
        {
            public const int SpiSetDeskWallpaper = 0x0014;
            public const int SpifUpdateIniFile = 0x01;
            public const int SpifSendChange = 0x02;

            private const uint CfHDrop = 15;
            private const uint GmemMoveable = 0x0002;
            private const uint GmemZeroInit = 0x0040;
            private const uint MonitorInfoPrimary = 1;
            private static readonly IntPtr HwndMessage = new(-3);

            private static IntPtr _clipboardOwner;

            [StructLayout(LayoutKind.Sequential)]
            private struct Rect
            {
                public int Left, Top, Right, Bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct MonitorInfo
            {
                public int Size;
                public Rect Monitor;
                public Rect Work;
                public uint Flags;
            }

            private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref Rect rect, IntPtr data);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SystemParametersInfo(int action, int param, string value, int winIni);

            [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfoW(IntPtr monitor, ref MonitorInfo info);
            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr CreateWindowExW(uint exStyle, string className, string? windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
            [DllImport("user32.dll", SetLastError = true)] private static extern bool OpenClipboard(IntPtr owner);
            [DllImport("user32.dll", SetLastError = true)] private static extern bool EmptyClipboard();
            [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetClipboardData(uint format, IntPtr memory);
            [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseClipboard();
            [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalLock(IntPtr memory);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalUnlock(IntPtr memory);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalFree(IntPtr memory);

            public static IReadOnlyList<DisplayRect> GetWorkAreas()
            {
                List<DisplayRect> displays = [];
                try
                {
                    EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr hdc, ref Rect rect, IntPtr data) =>
                    {
                        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                        if (GetMonitorInfoW(monitor, ref info))
                        {
                            displays.Add(new DisplayRect(info.Work.Left, info.Work.Top,
                                info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top,
                                (info.Flags & MonitorInfoPrimary) != 0));
                        }
                        return true;
                    }, IntPtr.Zero);
                }
                catch
                {
                    return [];
                }
                return displays;
            }

            public static bool SetClipboardFileDrop(byte[] dropFiles)
            {
                // SetClipboardData fails when the clipboard has no owner window, so a message-only window owns it.
                if (_clipboardOwner == IntPtr.Zero)
                    _clipboardOwner = CreateWindowExW(0, "STATIC", null, 0, 0, 0, 0, 0, HwndMessage, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                var memory = GlobalAlloc(GmemMoveable | GmemZeroInit, (UIntPtr)dropFiles.Length);
                if (memory == IntPtr.Zero)
                    return false;

                var locked = GlobalLock(memory);
                if (locked == IntPtr.Zero)
                {
                    GlobalFree(memory);
                    return false;
                }
                Marshal.Copy(dropFiles, 0, locked, dropFiles.Length);
                GlobalUnlock(memory);

                if (!OpenClipboard(_clipboardOwner))
                {
                    GlobalFree(memory);
                    return false;
                }

                try
                {
                    // On success the system owns the memory; on failure it is still ours to free.
                    if (EmptyClipboard() && SetClipboardData(CfHDrop, memory) != IntPtr.Zero)
                        return true;
                    GlobalFree(memory);
                    return false;
                }
                finally
                {
                    CloseClipboard();
                }
            }
        }
    }
}

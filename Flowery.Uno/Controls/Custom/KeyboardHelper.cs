using System.Runtime.InteropServices;

namespace Flowery.Controls
{
    internal static partial class KeyboardHelper
    {
        internal const int VK_SHIFT = 0x10;
        internal const int VK_CONTROL = 0x11;
        internal const int VK_MENU = 0x12;       // Alt
        internal const int VK_CAPITAL = 0x14;    // Caps Lock
        internal const int VK_NUMLOCK = 0x90;
        internal const int VK_SCROLL = 0x91;

        [LibraryImport("user32.dll")]
        private static partial short GetKeyState(int nVirtKey);

        // GetKeyState is message-queue based and can be stale when polling.
        // GetAsyncKeyState reflects the current hardware state and works for polling loops.
        [LibraryImport("user32.dll")]
        private static partial short GetAsyncKeyState(int vKey);

        internal static bool IsKeyToggled(int vkCode) => (GetKeyState(vkCode) & 0x0001) != 0;
        internal static bool IsKeyPressed(int vkCode) => (GetAsyncKeyState(vkCode) & 0x8000) != 0;
    }
}

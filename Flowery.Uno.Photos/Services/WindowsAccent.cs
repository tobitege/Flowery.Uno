using System.Threading.Tasks;
using Flowery.Uno.Photos.Services.Platform;
using Microsoft.UI.Dispatching;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>
    /// System accent color for borders (window frame, hover, focus and selection frames).
    /// <see cref="Brush"/> is one shared instance whose color follows the system setting, so elements using it update live.
    /// The class keeps its Flowery.WinUI.Photos name; on macOS and Linux it reads that OS's accent.
    /// Skia: reading the accent can start a process (gsettings, defaults), so it never happens on the UI thread: the brush
    /// starts with the theme color, a background read replaces it, and repeated refreshes are rate limited.
    /// </summary>
    public static class WindowsAccent
    {
        private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromSeconds(10);

        private static DispatcherQueue? _dispatcher;
        private static int _reading;
        private static long _lastReadTicks;

        public static SolidColorBrush Brush { get; } = new(FallbackAccent());

        public static Color Color => Brush.Color;

        /// <summary>Raised on the UI thread after the accent color changed.</summary>
        public static event EventHandler? Changed;

        public static void Initialize(DispatcherQueue dispatcher)
        {
            if (_dispatcher != null)
                return;
            _dispatcher = dispatcher;
            Refresh();
        }

        // Skia: there is no ColorValuesChanged event, so the window calls this when it is activated.
        public static void Refresh()
        {
            if (_dispatcher == null)
                return;

            var now = Environment.TickCount64;
            if (_lastReadTicks != 0 && now - _lastReadTicks < MinRefreshInterval.TotalMilliseconds)
                return;
            if (Interlocked.Exchange(ref _reading, 1) == 1)
                return;

            _ = Task.Run(() =>
            {
                try
                {
                    var accent = PlatformServices.Current.GetSystemAccent();
                    _dispatcher.TryEnqueue(() => Apply(accent));
                }
                finally
                {
                    _lastReadTicks = Environment.TickCount64;
                    Volatile.Write(ref _reading, 0);
                }
            });
        }

        private static void Apply(SystemAccent? accent)
        {
            var color = accent is { } a ? Color.FromArgb(0xFF, a.R, a.G, a.B) : FallbackAccent();
            if (color == Brush.Color)
                return;
            Brush.Color = color;
            Changed?.Invoke(null, EventArgs.Empty);
        }

        // No readable system accent: the theme's primary color.
        private static Color FallbackAccent() =>
            DaisyResourceLookup.GetBrush("DaisyPrimaryBrush") is SolidColorBrush primary
                ? primary.Color
                : Color.FromArgb(0xFF, 0x33, 0x99, 0xFF);
    }
}

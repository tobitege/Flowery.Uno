namespace Flowery.Controls
{
    public static class DaisyMaskWin2DInterop
    {
        private static readonly Lock Sync = new();
        private static bool _loadAttempted;
        private static Func<Compositor, DaisyMaskVariant, float, float, CompositionGeometry?>? _geometryFactory;

        public static void RegisterGeometryFactory(Func<Compositor, DaisyMaskVariant, float, float, CompositionGeometry?> geometryFactory)
        {
            _geometryFactory = geometryFactory ?? throw new ArgumentNullException(nameof(geometryFactory));
        }

        internal static CompositionGeometry? TryCreateGeometry(Compositor compositor, DaisyMaskVariant variant, float width, float height)
        {
            if (_geometryFactory == null)
            {
                EnsureWin2DLoaded();
            }

            return _geometryFactory?.Invoke(compositor, variant, width, height);
        }

        private static void EnsureWin2DLoaded()
        {
            if (_loadAttempted)
            {
                return;
            }

            lock (Sync)
            {
                if (_loadAttempted)
                {
                    return;
                }

                _loadAttempted = true;
                try
                {
                    _ = Type.GetType("Flowery.Win2D.DaisyWin2DModuleInitializer, Flowery.Uno.Win2D");
                }
                catch
                {
                    // Win2D extension not available.
                }
            }
        }
    }
}

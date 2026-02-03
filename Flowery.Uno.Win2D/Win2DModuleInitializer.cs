using System.Runtime.CompilerServices;

#pragma warning disable CA2255 // Module initializer registers the optional Win2D extension on assembly load.

namespace Flowery.Win2D
{
    internal static class DaisyWin2DModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DaisyMaskExtensions.RegisterWin2DGeometry();
        }
    }
}

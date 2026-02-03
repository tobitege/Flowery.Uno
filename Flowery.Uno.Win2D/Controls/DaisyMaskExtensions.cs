namespace Flowery.Win2D
{
    public static class DaisyMaskExtensions
    {
        public static void RegisterWin2DGeometry()
        {
            DaisyMaskWin2DInterop.RegisterGeometryFactory(CompositionHelpers.CreateMaskGeometry);
        }
    }
}

using Microsoft.UI.Xaml;

namespace Flowery.Controls
{
    // Standardized elevation path. Uses Uno's elevation pipeline (Skia ShadowState when available).
    public sealed partial class DaisyNeumorphicHelper
    {
        public static double SkiaElevationScale { get; set; } = 10.0;

        partial void OnApplyDirectElevation(DaisyNeumorphicMode mode, double intensity, double elevation, ref bool handled)
        {
            if (_owner is not UIElement ownerElement)
            {
                handled = false;
                return;
            }

            var baseElevation = GetDirectElevation(mode, intensity, elevation);
            double finalElevation = baseElevation * SkiaElevationScale;
            global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(ownerElement, finalElevation);
            handled = true;
        }
    }
}

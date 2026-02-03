// Preserved Android-specific implementation. Disabled while standardizing on Skia.
// To re-enable, restore the original #if __ANDROID__ guard.
#if false
using System;
using Microsoft.UI.Xaml;

namespace Flowery.Controls
{
    public sealed partial class DaisyNeumorphicHelper
    {
        public static double AndroidElevationScale { get; set; } = 2.5;
        public static double AndroidInsetIntensityBoost { get; set; } = 1.5;

        partial void OnApplyDirectElevation(DaisyNeumorphicMode mode, double intensity, double elevation, ref bool handled)
        {
            if (_owner is not UIElement ownerElement)
            {
                handled = false;
                return;
            }

            var baseElevation = GetDirectElevation(mode, intensity, elevation);
            double finalElevation = baseElevation * AndroidElevationScale;
            global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(ownerElement, finalElevation);
            handled = true;
        }

        partial void OnAdjustInsetIntensity(ref double intensity)
        {
            intensity = Math.Clamp(intensity * AndroidInsetIntensityBoost, 0.0, 1.0);
        }
    }
}
#endif

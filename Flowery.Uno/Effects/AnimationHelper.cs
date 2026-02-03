using System;

namespace Flowery.Effects
{
    internal static class AnimationHelper
    {
        internal static double Lerp(double from, double to, double t) => from + (to - from) * t;
    }
}

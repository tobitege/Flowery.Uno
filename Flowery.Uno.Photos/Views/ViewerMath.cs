namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Zoom steps and ruler steps of the viewer, kept free of UI types so they can be tested.
    /// </summary>
    public static class ViewerMath
    {
        public const double ZoomStepFactor = 1.2;

        public static double ZoomGrid(double percent) => percent < 10 ? 1 : percent < 100 ? 10 : 25;

        public static double SnapZoomPercent(double percent)
        {
            var grid = ZoomGrid(percent);
            return Math.Max(1, Math.Round(percent / grid, MidpointRounding.AwayFromZero) * grid);
        }

        public static double NextZoomPercentUp(double percent)
        {
            var grid = ZoomGrid(percent);
            return Math.Floor(percent / grid + 1e-6) * grid + grid;
        }

        public static double NextZoomPercentDown(double percent)
        {
            var grid = ZoomGrid(percent - 1e-6);
            return Math.Max(1, Math.Ceiling(percent / grid - 1e-6) * grid - grid);
        }

        /// <summary>
        /// The zoom percentage one step in (<paramref name="direction"/> &gt; 0) or out (&lt; 0) from <paramref name="current"/>.
        /// Lands on round percentages: every 1 % below 10 %, every 10 % below 100 %, and from 100 % about 20 % per step
        /// rounded to 25 %.
        /// </summary>
        public static double StepZoomPercent(double current, int direction)
        {
            var belowHundred = direction > 0 ? current < 100 - 0.01 : current <= 100 + 0.01;
            var target = belowHundred
                ? (direction > 0 ? NextZoomPercentUp(current) : NextZoomPercentDown(current))
                : SnapZoomPercent(direction > 0 ? current * ZoomStepFactor : current / ZoomStepFactor);
            if (direction > 0 && target <= current + 0.01)
                target = NextZoomPercentUp(current);
            else if (direction < 0 && target >= current - 0.01)
                target = NextZoomPercentDown(current);
            return target;
        }

        /// <summary>Picks a "nice" step (1, 2, 5 × 10^n) of at least <paramref name="minimum"/>.</summary>
        public static double NiceStep(double minimum)
        {
            if (minimum <= 0 || double.IsNaN(minimum) || double.IsInfinity(minimum))
                return 100;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(minimum)));
            foreach (var factor in new[] { 1.0, 2.0, 5.0, 10.0 })
            {
                if (factor * magnitude >= minimum)
                    return factor * magnitude;
            }
            return 10 * magnitude;
        }
    }
}

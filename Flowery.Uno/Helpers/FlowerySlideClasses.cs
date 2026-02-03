using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Flowery.Helpers
{
    /// <summary>
    /// Parameters for slide effect animations.
    /// </summary>
    public record FlowerySlideEffectParams
    {
        /// <summary>Zoom scale factor (0.0 to 0.5). Default 0.2 = zoom to 120%.</summary>
        public double ZoomIntensity { get; init; } = 0.2;

        /// <summary>Pan distance in pixels. Default 50.</summary>
        public double PanDistance { get; init; } = 50.0;

        /// <summary>Pulse amplitude (0.0 to 0.2). Default 0.08 = pulse to 108%.</summary>
        public double PulseIntensity { get; init; } = 0.08;

        /// <summary>Vertical pan multiplier (relative to horizontal). Default 0.6.</summary>
        public double VerticalPanRatio { get; init; } = 0.6;

        /// <summary>Subtle zoom multiplier for pan effects. Default 0.1.</summary>
        public double SubtleZoomRatio { get; init; } = 0.1;

        /// <summary>Pan And Zoom pan speed in pixels per second. Default 4.</summary>
        public double PanAndZoomPanSpeed { get; init; } = 4.0;

        /// <summary>Pan And Zoom intensity (0.0 to 0.2). Default 0.2 = zoom to 120%.</summary>
        public double PanAndZoomZoom { get; init; } = 0.2;

        /// <summary>Locks Pan And Zoom zoom (pan only). Default false.</summary>
        public bool PanAndZoomLockZoom { get; init; }

        /// <summary>
        /// When true, portrait images (much taller than the viewport) only pan downward
        /// to avoid cutting off faces/heads at the top. Default true.
        /// </summary>
        public bool VerticalLock { get; init; } = true;

        /// <summary>
        /// Height multiplier threshold for VerticalLock to apply.
        /// Image is considered "very tall" when height > viewport height * this value. Default 1.5.
        /// </summary>
        public double VerticalLockRatio { get; init; } = 1.5;

        /// <summary>
        /// Optimization ratio for very long/tall images. When an image extends far beyond
        /// the viewport, only pan this fraction of the total delta to prevent dizzying motion.
        /// Applied when delta > viewport * 0.7. Default 0.6 (pan only 60% of total delta).
        /// </summary>
        public double OptimizeRatio { get; init; } = 0.6;

        /// <summary>Breath effect peak intensity. Default 0.3 = zoom to 130% at peak.</summary>
        public double BreathIntensity { get; init; } = 0.3;

        /// <summary>Throw effect scale at center. Default 1.0 (full size at center, 0.7 at edges).</summary>
        public double ThrowScale { get; init; } = 1.0;
    }

    /// <summary>
    /// Helper methods for parsing FlowerySlideEffect from strings.
    /// </summary>
    public static class FlowerySlideEffectParser
    {
        /// <summary>
        /// Parses a string to FlowerySlideEffect. Returns None for invalid/null strings.
        /// </summary>
        public static FlowerySlideEffect Parse(string? value)
        {
            return value switch
            {
                "PanAndZoom" => FlowerySlideEffect.PanAndZoom,
                "ZoomIn" => FlowerySlideEffect.ZoomIn,
                "ZoomOut" => FlowerySlideEffect.ZoomOut,
                "PanLeft" => FlowerySlideEffect.PanLeft,
                "PanRight" => FlowerySlideEffect.PanRight,
                "PanUp" => FlowerySlideEffect.PanUp,
                "PanDown" => FlowerySlideEffect.PanDown,
                "Drift" => FlowerySlideEffect.Drift,
                "Pulse" => FlowerySlideEffect.Pulse,
                "Breath" => FlowerySlideEffect.Breath,
                "Throw" => FlowerySlideEffect.Throw,
                _ => FlowerySlideEffect.None
            };
        }

        /// <summary>
        /// Tries to parse a string to FlowerySlideEffect.
        /// </summary>
        public static bool TryParse(string? value, out FlowerySlideEffect effect)
        {
            effect = Parse(value);
            return value is not null && Enum.IsDefined(effect);
        }
    }

    /// <summary>
    /// Helper methods for parsing FlowerySlideshowMode from strings.
    /// </summary>
    public static class FlowerySlideshowModeParser
    {
        /// <summary>
        /// Parses a string to FlowerySlideshowMode. Returns Manual for invalid/null strings.
        /// </summary>
        public static FlowerySlideshowMode Parse(string? value)
        {
            return value switch
            {
                "Manual" => FlowerySlideshowMode.Manual,
                "Slideshow" => FlowerySlideshowMode.Slideshow,
                "Random" => FlowerySlideshowMode.Random,
                "Kiosk" => FlowerySlideshowMode.Kiosk,
                _ => FlowerySlideshowMode.Manual
            };
        }

        /// <summary>
        /// Tries to parse a string to FlowerySlideshowMode.
        /// </summary>
        public static bool TryParse(string? value, out FlowerySlideshowMode mode)
        {
            mode = Parse(value);
            return value is not null && Enum.IsDefined(mode);
        }
    }

    /// <summary>
    /// Event args for slide change events in slideshow controls.
    /// </summary>
    public class FlowerySlideChangedEventArgs(int oldIndex, int newIndex, FlowerySlideEffect appliedEffect, UIElement? currentElement = null) : EventArgs
    {
        /// <summary>Index before the change, or -1 if this is the first load.</summary>
        public int OldIndex { get; } = oldIndex;

        /// <summary>Current index after the change.</summary>
        public int NewIndex { get; } = newIndex;

        /// <summary>The visual effect that was applied to the new slide.</summary>
        public FlowerySlideEffect AppliedEffect { get; } = appliedEffect;

        /// <summary>The current slide element (can be used for dimension debugging).</summary>
        public UIElement? CurrentElement { get; } = currentElement;

        public override string ToString()
        {
            return $"OldIndex={OldIndex}, NewIndex={NewIndex}, AppliedEffect={AppliedEffect}";
        }
    }
}

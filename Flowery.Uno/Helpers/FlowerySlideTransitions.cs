using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Flowery.Helpers
{
    #region Slide Transitions (Between Slides)

    /// <summary>
    /// Specifies visual transitions that occur between slides during navigation.
    /// Unlike SlideEffects (which animate the currently visible slide), SlideTransitions
    /// animate the change FROM one slide TO another.
    /// </summary>
    /// <remarks>
    /// Transitions are categorized by implementation complexity:
    /// - Tier 1 (CompositeTransform): Fade, Slide, Push, Zoom, Flip - works everywhere
    /// - Tier 2 (Clip-based): Wipe, Blinds, Slices - uses animated clip geometries
    /// - Tier 3 (SkiaSharp): Blur, Dissolve, Pixelate - requires Skia-enabled targets
    /// </remarks>
    public enum FlowerySlideTransition
    {
        // === SPECIAL ===
        /// <summary>No transition - instant snap (default behavior).</summary>
        None,
        /// <summary>Pick a random transition for each navigation.</summary>
        Random,

        // === TIER 1: FADE FAMILY (Opacity-based) ===
        /// <summary>Crossfade - old fades out while new fades in.</summary>
        Fade,
        /// <summary>Fade through black - old fades to black, then new fades in from black.</summary>
        FadeThroughBlack,
        /// <summary>Fade through white - old fades to white, then new fades in from white.</summary>
        FadeThroughWhite,

        // === TIER 1: SLIDE FAMILY (New enters, old exits) ===
        /// <summary>New slide enters from right, old exits to left.</summary>
        SlideLeft,
        /// <summary>New slide enters from left, old exits to right.</summary>
        SlideRight,
        /// <summary>New slide enters from bottom, old exits to top.</summary>
        SlideUp,
        /// <summary>New slide enters from top, old exits to bottom.</summary>
        SlideDown,

        // === TIER 1: PUSH FAMILY (New pushes old out) ===
        /// <summary>New slide pushes old to the left.</summary>
        PushLeft,
        /// <summary>New slide pushes old to the right.</summary>
        PushRight,
        /// <summary>New slide pushes old upward.</summary>
        PushUp,
        /// <summary>New slide pushes old downward.</summary>
        PushDown,

        // === TIER 1: ZOOM FAMILY (Scale transitions) ===
        /// <summary>New slide zooms in from small to full size.</summary>
        ZoomIn,
        /// <summary>Old slide zooms out to small, revealing new.</summary>
        ZoomOut,
        /// <summary>Old zooms out while new zooms in (crossover).</summary>
        ZoomCross,

        // === TIER 1: FLIP FAMILY (3D rotation using PlaneProjection) ===
        /// <summary>Card flip on horizontal axis (top/bottom).</summary>
        FlipHorizontal,
        /// <summary>Card flip on vertical axis (left/right).</summary>
        FlipVertical,
        /// <summary>3D cube rotation to the left.</summary>
        CubeLeft,
        /// <summary>3D cube rotation to the right.</summary>
        CubeRight,

        // === TIER 1: COVER/REVEAL FAMILY ===
        /// <summary>New slide covers old from the right (old stays in place).</summary>
        CoverLeft,
        /// <summary>New slide covers old from the left.</summary>
        CoverRight,
        /// <summary>New slide covers old from the bottom.</summary>
        CoverUp,
        /// <summary>New slide covers old from the top.</summary>
        CoverDown,
        /// <summary>Old slide reveals new underneath by exiting left.</summary>
        RevealLeft,
        /// <summary>Old slide reveals new underneath by exiting right.</summary>
        RevealRight,

        // === TIER 2: WIPE FAMILY (Animated clip geometry) ===
        /// <summary>Rectangular wipe from right to left.</summary>
        WipeLeft,
        /// <summary>Rectangular wipe from left to right.</summary>
        WipeRight,
        /// <summary>Rectangular wipe from bottom to top.</summary>
        WipeUp,
        /// <summary>Rectangular wipe from top to bottom.</summary>
        WipeDown,

        // === TIER 2: BLINDS FAMILY (Multiple animated clips) ===
        /// <summary>Horizontal venetian blinds reveal.</summary>
        BlindsHorizontal,
        /// <summary>Vertical blinds reveal.</summary>
        BlindsVertical,

        // === TIER 2: SLICES FAMILY (Random-order strip reveals) ===
        /// <summary>Horizontal slices reveal in random order.</summary>
        SlicesHorizontal,
        /// <summary>Vertical slices reveal in random order.</summary>
        SlicesVertical,
        /// <summary>Grid squares reveal in random order.</summary>
        Checkerboard,
        /// <summary>Grid squares reveal in a spiral pattern from center. </summary>
        Spiral,
        /// <summary>Columns fall down in a digital rain pattern.</summary>
        MatrixRain,
        /// <summary>Extreme radial stretch and vortex effect.</summary>
        Wormhole,

        // === TIER 3: SKIA-BASED (Requires SkiaSharp) ===
        /// <summary>Pixel noise dissolve pattern. Requires SkiaSharp.</summary>
        Dissolve,
        /// <summary>Mosaic pixelation transition. Requires SkiaSharp.</summary>
        Pixelate,
    }

    /// <summary>
    /// Categorizes slide transitions by their implementation requirements.
    /// </summary>
    public enum FloweryTransitionTier
    {
        /// <summary>Uses CompositeTransform/PlaneProjection - works everywhere.</summary>
        Transform,
        /// <summary>Uses animated RectangleGeometry clips - works on most platforms.</summary>
        Clip,
        /// <summary>Requires SkiaSharp rendering - only on Skia-enabled targets.</summary>
        Skia
    }

    /// <summary>
    /// Parameters for slide transition animations.
    /// </summary>
    public record FlowerySlideTransitionParams
    {
        /// <summary>Duration of the transition animation. Default 400ms.</summary>
        public TimeSpan Duration { get; init; } = TimeSpan.FromMilliseconds(400);

        /// <summary>Easing mode for the transition. Default EaseInOut.</summary>
        public EasingMode EasingMode { get; init; } = EasingMode.EaseInOut;

        /// <summary>Number of slices for Slices/Blinds transitions. Default 8.</summary>
        public int SliceCount { get; init; } = 8;

        /// <summary>Whether slice animations should be staggered or simultaneous. Default true.</summary>
        public bool StaggerSlices { get; init; } = true;

        /// <summary>Base stagger delay between slices in ms. Default 50ms.</summary>
        public double SliceStaggerMs { get; init; } = 50;

        /// <summary>Grid size for Checkerboard transition (squares per row/column). Default 6.</summary>
        public int CheckerboardSize { get; init; } = 6;

        /// <summary>Pixel block size for Pixelate transition at peak. Default 20.</summary>
        public int PixelateSize { get; init; } = 20;

        /// <summary>Noise density for Dissolve transition (0.0 to 1.0). Default 0.5.</summary>
        public double DissolveDensity { get; init; } = 0.5;

        /// <summary>Rotation angle for flip transitions in degrees. Default 90.</summary>
        public double FlipAngle { get; init; } = 90;

        /// <summary>Perspective depth for 3D transitions. Default 1000.</summary>
        public double PerspectiveDepth { get; init; } = 1000;

        /// <summary>Scale factor for zoom transitions. Default 0.8 (zoom from 80%).</summary>
        public double ZoomScale { get; init; } = 0.8;

        /// <summary>Color for FadeThroughBlack/White transitions. Auto-detected if not set.</summary>
        public Windows.UI.Color? FadeThroughColor { get; init; }
    }

    /// <summary>
    /// Helper methods and utilities for FlowerySlideTransition.
    /// </summary>
    public static class FlowerySlideTransitionParser
    {
        private static readonly Random _random = new();

        /// <summary>
        /// Parses a string to FlowerySlideTransition. Returns None for invalid/null strings.
        /// </summary>
        public static FlowerySlideTransition Parse(string? value)
        {
            return value switch
            {
                // Special
                "Random" => FlowerySlideTransition.Random,

                // Fade family
                "Fade" => FlowerySlideTransition.Fade,
                "FadeThroughBlack" => FlowerySlideTransition.FadeThroughBlack,
                "FadeThroughWhite" => FlowerySlideTransition.FadeThroughWhite,

                // Slide family
                "SlideLeft" => FlowerySlideTransition.SlideLeft,
                "SlideRight" => FlowerySlideTransition.SlideRight,
                "SlideUp" => FlowerySlideTransition.SlideUp,
                "SlideDown" => FlowerySlideTransition.SlideDown,

                // Push family
                "PushLeft" => FlowerySlideTransition.PushLeft,
                "PushRight" => FlowerySlideTransition.PushRight,
                "PushUp" => FlowerySlideTransition.PushUp,
                "PushDown" => FlowerySlideTransition.PushDown,

                // Zoom family
                "ZoomIn" => FlowerySlideTransition.ZoomIn,
                "ZoomOut" => FlowerySlideTransition.ZoomOut,
                "ZoomCross" => FlowerySlideTransition.ZoomCross,

                // Flip family
                "FlipHorizontal" => FlowerySlideTransition.FlipHorizontal,
                "FlipVertical" => FlowerySlideTransition.FlipVertical,
                "CubeLeft" => FlowerySlideTransition.CubeLeft,
                "CubeRight" => FlowerySlideTransition.CubeRight,

                // Cover/Reveal family
                "CoverLeft" => FlowerySlideTransition.CoverLeft,
                "CoverRight" => FlowerySlideTransition.CoverRight,
                "CoverUp" => FlowerySlideTransition.CoverUp,
                "CoverDown" => FlowerySlideTransition.CoverDown,
                "RevealLeft" => FlowerySlideTransition.RevealLeft,
                "RevealRight" => FlowerySlideTransition.RevealRight,

                // Wipe family
                "WipeLeft" => FlowerySlideTransition.WipeLeft,
                "WipeRight" => FlowerySlideTransition.WipeRight,
                "WipeUp" => FlowerySlideTransition.WipeUp,
                "WipeDown" => FlowerySlideTransition.WipeDown,

                // Blinds family
                "BlindsHorizontal" => FlowerySlideTransition.BlindsHorizontal,
                "BlindsVertical" => FlowerySlideTransition.BlindsVertical,

                // Slices family
                "SlicesHorizontal" => FlowerySlideTransition.SlicesHorizontal,
                "SlicesVertical" => FlowerySlideTransition.SlicesVertical,
                "Checkerboard" => FlowerySlideTransition.Checkerboard,
                "Spiral" => FlowerySlideTransition.Spiral,
                "MatrixRain" => FlowerySlideTransition.MatrixRain,
                "Wormhole" => FlowerySlideTransition.Wormhole,

                // Skia-based
                "Dissolve" => FlowerySlideTransition.Dissolve,
                "Pixelate" => FlowerySlideTransition.Pixelate,

                _ => FlowerySlideTransition.None
            };
        }

        /// <summary>
        /// Tries to parse a string to FlowerySlideTransition.
        /// </summary>
        public static bool TryParse(string? value, out FlowerySlideTransition transition)
        {
            transition = Parse(value);
            return value is not null && Enum.IsDefined(transition);
        }

        /// <summary>
        /// Gets the implementation tier for a transition.
        /// </summary>
        public static FloweryTransitionTier GetTier(FlowerySlideTransition transition)
        {
            return transition switch
            {
                // Tier 1: Transform-based
                FlowerySlideTransition.None or
                FlowerySlideTransition.Random or
                FlowerySlideTransition.Fade or
                FlowerySlideTransition.FadeThroughBlack or
                FlowerySlideTransition.FadeThroughWhite or
                FlowerySlideTransition.SlideLeft or
                FlowerySlideTransition.SlideRight or
                FlowerySlideTransition.SlideUp or
                FlowerySlideTransition.SlideDown or
                FlowerySlideTransition.PushLeft or
                FlowerySlideTransition.PushRight or
                FlowerySlideTransition.PushUp or
                FlowerySlideTransition.PushDown or
                FlowerySlideTransition.ZoomIn or
                FlowerySlideTransition.ZoomOut or
                FlowerySlideTransition.ZoomCross or
                FlowerySlideTransition.FlipHorizontal or
                FlowerySlideTransition.FlipVertical or
                FlowerySlideTransition.CubeLeft or
                FlowerySlideTransition.CubeRight or
                FlowerySlideTransition.CoverLeft or
                FlowerySlideTransition.CoverRight or
                FlowerySlideTransition.CoverUp or
                FlowerySlideTransition.CoverDown or
                FlowerySlideTransition.RevealLeft or
                FlowerySlideTransition.RevealRight
                    => FloweryTransitionTier.Transform,

                // Tier 2: Clip-based
                FlowerySlideTransition.WipeLeft or
                FlowerySlideTransition.WipeRight or
                FlowerySlideTransition.WipeUp or
                FlowerySlideTransition.WipeDown or
                FlowerySlideTransition.BlindsHorizontal or
                FlowerySlideTransition.BlindsVertical or
                FlowerySlideTransition.SlicesHorizontal or
                FlowerySlideTransition.SlicesVertical or
                FlowerySlideTransition.Checkerboard or
                FlowerySlideTransition.Spiral or
                FlowerySlideTransition.MatrixRain or
                FlowerySlideTransition.Wormhole
                    => FloweryTransitionTier.Clip,

                // Tier 3: Skia-based
                FlowerySlideTransition.Dissolve or
                FlowerySlideTransition.Pixelate
                    => FloweryTransitionTier.Skia,

                _ => FloweryTransitionTier.Transform
            };
        }

        /// <summary>
        /// Gets all transitions of a specific tier.
        /// </summary>
        public static FlowerySlideTransition[] GetTransitionsByTier(FloweryTransitionTier tier)
        {
            return [.. Enum.GetValues<FlowerySlideTransition>()
                .Where(t => t is not FlowerySlideTransition.None and not FlowerySlideTransition.Random)
                .Where(t => GetTier(t) == tier)];
        }

        /// <summary>
        /// Gets all Tier 1 (Transform) transitions for random selection on all platforms.
        /// </summary>
        public static FlowerySlideTransition[] GetUniversalTransitions()
        {
            return GetTransitionsByTier(FloweryTransitionTier.Transform);
        }

        /// <summary>
        /// Picks a random transition, optionally filtered by tier.
        /// </summary>
        public static FlowerySlideTransition PickRandom(FloweryTransitionTier? maxTier = null)
        {
            FlowerySlideTransition[] available = maxTier switch
            {
                FloweryTransitionTier.Transform => GetTransitionsByTier(FloweryTransitionTier.Transform),
                FloweryTransitionTier.Clip => [.. GetTransitionsByTier(FloweryTransitionTier.Transform),
                                                .. GetTransitionsByTier(FloweryTransitionTier.Clip)],
                FloweryTransitionTier.Skia => [.. Enum.GetValues<FlowerySlideTransition>().Where(static t => t is not FlowerySlideTransition.None and not FlowerySlideTransition.Random)],
                _ => GetUniversalTransitions() // Default to safe universal set
            };

            return available[_random.Next(available.Length)];
        }

        /// <summary>
        /// Checks if a transition requires SkiaSharp.
        /// </summary>
        public static bool RequiresSkia(FlowerySlideTransition transition)
        {
            return GetTier(transition) == FloweryTransitionTier.Skia;
        }

        /// <summary>
        /// Gets a display-friendly name for the transition.
        /// </summary>
        public static string GetDisplayName(FlowerySlideTransition transition)
        {
            return transition switch
            {
                FlowerySlideTransition.None => "None",
                FlowerySlideTransition.Random => "Random",
                FlowerySlideTransition.Fade => "Fade",
                FlowerySlideTransition.FadeThroughBlack => "Fade (Black)",
                FlowerySlideTransition.FadeThroughWhite => "Fade (White)",
                FlowerySlideTransition.SlideLeft => "Slide Left",
                FlowerySlideTransition.SlideRight => "Slide Right",
                FlowerySlideTransition.SlideUp => "Slide Up",
                FlowerySlideTransition.SlideDown => "Slide Down",
                FlowerySlideTransition.PushLeft => "Push Left",
                FlowerySlideTransition.PushRight => "Push Right",
                FlowerySlideTransition.PushUp => "Push Up",
                FlowerySlideTransition.PushDown => "Push Down",
                FlowerySlideTransition.ZoomIn => "Zoom In",
                FlowerySlideTransition.ZoomOut => "Zoom Out",
                FlowerySlideTransition.ZoomCross => "Zoom Cross",
                FlowerySlideTransition.FlipHorizontal => "Flip Horizontal",
                FlowerySlideTransition.FlipVertical => "Flip Vertical",
                FlowerySlideTransition.CubeLeft => "Cube Left",
                FlowerySlideTransition.CubeRight => "Cube Right",
                FlowerySlideTransition.CoverLeft => "Cover Left",
                FlowerySlideTransition.CoverRight => "Cover Right",
                FlowerySlideTransition.CoverUp => "Cover Up",
                FlowerySlideTransition.CoverDown => "Cover Down",
                FlowerySlideTransition.RevealLeft => "Reveal Left",
                FlowerySlideTransition.RevealRight => "Reveal Right",
                FlowerySlideTransition.WipeLeft => "Wipe Left",
                FlowerySlideTransition.WipeRight => "Wipe Right",
                FlowerySlideTransition.WipeUp => "Wipe Up",
                FlowerySlideTransition.WipeDown => "Wipe Down",
                FlowerySlideTransition.BlindsHorizontal => "Blinds Horizontal",
                FlowerySlideTransition.BlindsVertical => "Blinds Vertical",
                FlowerySlideTransition.SlicesHorizontal => "Slices Horizontal",
                FlowerySlideTransition.SlicesVertical => "Slices Vertical",
                FlowerySlideTransition.Checkerboard => "Checkerboard",
                FlowerySlideTransition.Spiral => "Spiral",
                FlowerySlideTransition.MatrixRain => "Matrix Rain",
                FlowerySlideTransition.Wormhole => "Wormhole",
                FlowerySlideTransition.Dissolve => "Dissolve ⚡",
                FlowerySlideTransition.Pixelate => "Pixelate ⚡",
                _ => transition.ToString()
            };
        }
    }

    #endregion
}

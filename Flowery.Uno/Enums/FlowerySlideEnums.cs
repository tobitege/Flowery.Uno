using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Flowery.Services
{
    /// <summary>
    /// Specifies visual effects for slide/image animations.
    /// Usable by any control that displays content with cinematic transitions.
    /// </summary>
    public enum FlowerySlideEffect
    {
        /// <summary>No effect - static display.</summary>
        None,
        /// <summary>Pan And Zoom effect - randomized pan and zoom combination for documentary-style motion.</summary>
        PanAndZoom,
        /// <summary>Slow zoom in from 1.0x to 1.0x + intensity.</summary>
        ZoomIn,
        /// <summary>Slow zoom out from 1.0x + intensity to 1.0x.</summary>
        ZoomOut,
        /// <summary>Slow pan to the left.</summary>
        PanLeft,
        /// <summary>Slow pan to the right.</summary>
        PanRight,
        /// <summary>Slow pan upward.</summary>
        PanUp,
        /// <summary>Slow pan downward.</summary>
        PanDown,
        /// <summary>Random direction pan per slide.</summary>
        Drift,
        /// <summary>Subtle breathing/pulsing zoom effect.</summary>
        Pulse,
        /// <summary>Breathing zoom with bounce: zooms in to peak then settles back slightly.</summary>
        Breath,
        /// <summary>Dramatic parallax fly-through: scales up at center while panning across.</summary>
        Throw
    }

    /// <summary>
    /// Specifies the navigation mode for slideshow controls.
    /// Usable by carousels, galleries, and other slideshow-style components.
    /// </summary>
    public enum FlowerySlideshowMode
    {
        /// <summary>Manual navigation via user interaction only.</summary>
        Manual,
        /// <summary>Automatic sequential advancement that loops infinitely.</summary>
        Slideshow,
        /// <summary>Automatic random advancement with non-repeating order until all items shown.</summary>
        Random,
        /// <summary>Automatic random advancement with random effects per slide (full kiosk/screensaver mode).</summary>
        Kiosk
    }
}

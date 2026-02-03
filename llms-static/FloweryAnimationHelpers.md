<!-- Supplementary documentation for FloweryAnimationHelpers -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

FloweryAnimationHelpers provides a comprehensive, reusable animation and slideshow infrastructure for building cinematic UI experiences. It includes enums for visual effects, a controller for slideshow logic, and attached properties for declarative animations.

**Namespace**: `Flowery.Services`

## Components

| Component | Purpose |
| --- | --- |
| `FlowerySlideEffect` | Enum for continuous visual animations (Pan And Zoom, zoom, pan, etc.) |
| `FlowerySlideTransition` | Enum for between-slide transitions (Fade, Cube, Wormhole, etc.) |
| `FlowerySlideEffectParams` | Configuration for continuous effect animations |
| `FlowerySlideTransitionParams` | Configuration for between-slide transitions |
| `FlowerySlideshowMode` | Enum for navigation modes (Manual, Slideshow, Random, Kiosk) |
| `FlowerySlideshowController` | Reusable timer, shuffle, and navigation logic |
| `FlowerySlideEffects` | Attached properties for XAML-based effects |
| `FloweryAnimationHelpers` | Static methods for building slide effects |
| `FlowerySlideTransitionHelpers` | Static methods for executing slide transitions |

---

## FlowerySlideEffect Enum

Specifies visual effects for slide/image animations. Usable by any control that displays content with cinematic transitions.

| Value | Description |
| --- | --- |
| `None` | No effect - static display. |
| `PanAndZoom` | Randomized pan and zoom combination (documentary-style). |
| `ZoomIn` | Slow zoom in from 1.0x to 1.0x + intensity. |
| `ZoomOut` | Slow zoom out from 1.0x + intensity to 1.0x. |
| `PanLeft` | Slow pan to the left. |
| `PanRight` | Slow pan to the right. |
| `PanUp` | Slow pan upward. |
| `PanDown` | Slow pan downward. |
| `Drift` | Random direction pan (different each slide). |
| `Breath` | Subtle rhythmic "breathing" zoom effect. |
| `Throw` | High-speed parallax fly-through effect. |
| `Pulse` | Subtle breathing/pulsing zoom effect. |

### FlowerySlideEffectParser

```csharp
// Parse string to enum (returns None for invalid/null)
FlowerySlideEffect effect = FlowerySlideEffectParser.Parse("PanAndZoom");

// Try-parse pattern
if (FlowerySlideEffectParser.TryParse(value, out var effect))
{
    // Valid effect
}
```

---

## FlowerySlideshowMode Enum

Specifies navigation modes for slideshow controls. Usable by carousels, galleries, and other slideshow-style components.

| Value | Description |
| --- | --- |
| `Manual` | User navigates via prev/next buttons only. Default behavior. |
| `Slideshow` | Automatic sequential advancement that loops infinitely. |
| `Random` | Automatic random advancement with non-repeating order (Fisher-Yates). |
| `Kiosk` | Random advance that also randomizes the visual effect and transition per slide. |

### FlowerySlideshowModeParser

```csharp
// Parse string to enum (returns Manual for invalid/null)
FlowerySlideshowMode mode = FlowerySlideshowModeParser.Parse("Kiosk");
```

---

## FlowerySlideTransition Enum

Specifies visual animations that occur when navigating **between** slides.

| Tier | Category | Values |
| --- | --- | --- |
| **Tier 1** | Transform | `Fade`, `Slide`, `Push`, `Zoom`, `Flip`, `Cube`, `Cover`, `Reveal` |
| **Tier 2** | Clip-based | `Wipe`, `Blinds`, `Slices`, `Checkerboard`, `Spiral`, `MatrixRain`, `Wormhole` |
| **Tier 3** | Skia-based | `Dissolve`, `Pixelate` (Requires Skia-enabled target) |

### FlowerySlideTransitionParams

Configuration record for transition animation properties.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Duration` | `TimeSpan` | `400ms` | Total animation time. |
| `SliceCount` | `int` | `8` | Grid/Blind density for Tier 2. |
| `StaggerSlices` | `bool` | `true` | Use sequential vs. random reveal order. |
| `FlipAngle` | `double` | `90.0` | Rotation angle for 3D transitions. |
| `PixelateSize` | `int` | `20` | Block size for Pixelate transition. |

```csharp
var myTransition = FlowerySlideTransitionHelpers.ApplyTransition(
    container, oldElement, newElement, 
    FlowerySlideTransition.CubeRight,
    new FlowerySlideTransitionParams { Duration = TimeSpan.FromSeconds(0.6) }
);
```

---

## FlowerySlideshowController

A batteries-included controller for building slideshow-style controls. Encapsulates timer management, shuffle algorithms, and navigation logic.

### Constructor

```csharp
var controller = new FlowerySlideshowController(
    navigateAction: index => SelectedIndex = index,  // Called when navigating
    getItemCount: () => Items.Count,                  // Returns item count
    getCurrentIndex: () => SelectedIndex);            // Returns current index
```

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Mode` | `FlowerySlideshowMode` | `Manual` | Navigation mode. Changes restart the timer. |
| `Interval` | `double` | `3.0` | Seconds between auto-advances. Minimum 0.5s. |
| `WrapAround` | `bool` | `false` | Whether navigation wraps (last→first, first→last). |
| `IsRunning` | `bool` | (read-only) | Whether the timer is currently running. |
| `CanGoPrevious` | `bool` | (read-only) | Whether previous navigation is possible. |
| `CanGoNext` | `bool` | (read-only) | Whether next navigation is possible. |

### Methods

| Method | Description |
| --- | --- |
| `Start()` | Starts or restarts the slideshow timer. |
| `Stop()` | Stops the slideshow timer. |
| `Restart()` | Restarts the timer (useful after mode/interval changes). |
| `Previous()` | Navigate to the previous item (respects WrapAround). |
| `Next()` | Navigate to the next item (respects WrapAround). |
| `GoTo(int index)` | Navigate to a specific index (clamped to valid range). |
| `OnItemCountChanged()` | Call when items change to reinitialize shuffle order. |
| `Dispose()` | Stops the timer and releases resources. |

### Example: Custom Gallery Control

```csharp
public class MyGallery : Control, IDisposable
{
    private FlowerySlideshowController _controller;

    public MyGallery()
    {
        _controller = new FlowerySlideshowController(
            index => SelectedIndex = index,
            () => Items.Count,
            () => SelectedIndex);
    }

    public void StartSlideshow()
    {
        _controller.Mode = FlowerySlideshowMode.Random;
        _controller.Interval = 5.0;
        _controller.WrapAround = true;
        _controller.Start();
    }

    private void OnPreviousClick() => _controller.Previous();
    private void OnNextClick() => _controller.Next();

    public void Dispose() => _controller.Dispose();
}
```

---

## FlowerySlideEffects (Attached Properties)

Apply cinematic effects to any `UIElement` declaratively in XAML.

### Attached Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Effect` | `FlowerySlideEffect` | `None` | The slide effect to apply. |
| `Duration` | `double` | `3.0` | Effect duration in seconds. |
| `ZoomIntensity` | `double` | `0.15` | Zoom scale factor (0.0 to 0.5). |
| `PanDistance` | `double` | `50` | Pan distance in pixels (0 to 200). |
| `PulseIntensity` | `double` | `0.08` | Pulse amplitude (0.0 to 0.2). |
| `AutoStart` | `bool` | `true` | Whether to start when the element loads. |

### XAML Usage

```xml
xmlns:services="using:Flowery.Services"

<!-- Pan And Zoom effect on a hero image -->
<Image Source="ms-appx:///Assets/hero.jpg"
       services:FlowerySlideEffects.Effect="PanAndZoom"
       services:FlowerySlideEffects.Duration="8"
       services:FlowerySlideEffects.ZoomIntensity="0.2"/>

<!-- Pulsing logo -->
<Image Source="ms-appx:///Assets/logo.png"
       services:FlowerySlideEffects.Effect="Pulse"
       services:FlowerySlideEffects.Duration="2"
       services:FlowerySlideEffects.PulseIntensity="0.05"/>

<!-- Slow pan across a wide image -->
<Image Source="ms-appx:///Assets/panorama.jpg"
       services:FlowerySlideEffects.Effect="PanLeft"
       services:FlowerySlideEffects.Duration="10"
       services:FlowerySlideEffects.PanDistance="100"/>
```

### Programmatic Control

```csharp
// Start effect manually
FlowerySlideEffects.StartEffect(myImage);

// Stop effect
FlowerySlideEffects.StopEffect(myImage);

// Configure and start
FlowerySlideEffects.SetEffect(myImage, FlowerySlideEffect.ZoomIn);
FlowerySlideEffects.SetDuration(myImage, 5.0);
FlowerySlideEffects.SetAutoStart(myImage, false);
FlowerySlideEffects.StartEffect(myImage);
```

---

## FlowerySlideEffectParams

Configuration record for fine-tuning effect parameters.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ZoomIntensity` | `double` | `0.15` | Zoom scale factor. 0.15 = zoom to 115%. |
| `PanDistance` | `double` | `50.0` | Pan distance in pixels. |
| `PulseIntensity` | `double` | `0.08` | Pulse amplitude. 0.08 = pulse to 108%. |
| `VerticalPanRatio` | `double` | `0.6` | Vertical pan multiplier relative to horizontal. |
| `SubtleZoomRatio` | `double` | `0.1` | Subtle zoom multiplier for pan effects. |

```csharp
var customParams = new FlowerySlideEffectParams
{
    ZoomIntensity = 0.25,
    PanDistance = 80,
    PulseIntensity = 0.12
};
```

---

## FloweryAnimationHelpers (Static Methods)

Low-level animation factory methods for building custom effects.

### Key Methods

| Method | Description |
| --- | --- |
| `CreateSlideAnimation(...)` | Creates a Storyboard for slide effects with configurable parameters. |
| `CreateDoubleAnimation(...)` | Creates a DoubleAnimation with easing. |
| `CreateFadeAnimation(...)` | Creates opacity fade animations. |

### Platform Support

Slide effects require `HAS_UNO` or `WINDOWS` compilation symbols. On other platforms, effects gracefully fall back to static display.

---

## Tips & Best Practices

- Use `FlowerySlideshowController` to add slideshow behavior to any control without reimplementing timer/shuffle logic.
- Set `AutoStart="False"` on attached effects when you need programmatic control over when animations begin.
- Combine `Mode="Slideshow"` with short `Interval` values (2-4s) for dynamic hero banners.
- Use `Mode="Random"` for digital signage or kiosks to prevent repetitive patterns.
- Use `PanAndZoom` for photo galleries - it randomly selects pan/zoom combinations per image.
- Use `Throw` for high-energy tech or motion-oriented slide decks.
- Use `Kiosk` mode for hands-off displays (signage) to maximize visual variety.
- Effects automatically stop when elements are unloaded to prevent memory leaks.
- Call `controller.OnItemCountChanged()` when your items collection changes to reinitialize shuffle order.
- Always call `controller.Dispose()` when your control is disposed to clean up timers.

## Technical Reference (Implementation Files)

For deep implementation details, consult the following source files in the `Flowery.Uno` project:

| File | Primary Coverage |
| --- | --- |
| `FloweryAnimationHelpers.cs` | Visual effect logic (`PanAndZoom`, `Breath`, `Throw`, `Pulse`). |
| `FlowerySlideTransitions.cs` | Enums and parameter records for between-slide transitions. |
| `FlowerySlideClasses.cs` | Parameters and parsers for continuous slide effects. |
| `FlowerySlideTransitionHelpers.cs` | Implementation of Tiers 1, 2, and 3 transitions. |
| `FlowerySlideEffects.cs` | XAML attached property system and lifecycle management. |
| `FlowerySlideshowController.cs` | Timer-based navigation, shuffle logic, and Kiosk selection. |
| `FloweryPathHelpers.cs` | Icon geometries often used for navigation controls. |

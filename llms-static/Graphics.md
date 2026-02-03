# Graphics & Visual Effects

This document describes the graphics rendering capabilities, visual effects, and animation systems in Flowery.Uno.

---

## Table of Contents

1. [Overview](#overview)
2. [SkiaSharp Infrastructure](#skiasharp-infrastructure)
3. [Glass Blur Effects](#glass-blur-effects) (DaisyGlass & DaisyCard)
4. [Carousel Slide Effects](#carousel-slide-effects)
5. [Carousel Slide Transitions](#carousel-slide-transitions)
6. [Platform Support Matrix](#platform-support-matrix)
7. [Performance Considerations](#performance-considerations)

---

## Overview

Flowery.Uno provides three layers of graphics capabilities:

| Layer | Technology | Purpose |
| ----- | ---------- | ------- |
| **UI Animations** | WinUI Composition API, Storyboards | Standard control animations, transitions |
| **Skia Processing** | SkiaSharp | Image blur, saturation, glass effects |
| **Carousel GL Effects** | Skia (Browser only) | Advanced carousel transitions/effects |

All effects are designed for cross-platform compatibility across Desktop (Skia), Browser (WASM), Windows, Android, and iOS.

---

## SkiaSharp Infrastructure

### Architecture

The `Flowery.Skia` namespace provides core image processing capabilities:

```txt
Flowery.Uno/Skia/
├── SkiaImageProcessor.cs      # Core blur, saturation, glass effect processing
└── SkiaBackdropCapture.cs     # WinUI ↔ Skia bridge for backdrop capture
```

### SkiaImageProcessor

Static utility class for GPU-accelerated image processing:

| Method | Description |
| ------ | ----------- |
| `ApplyBlur(bytes, sigmaX, sigmaY)` | Gaussian blur on image bytes |
| `ApplyBlurAndSaturation(bytes, sigma, saturation)` | Combined blur + saturation adjustment |
| `ApplyGlassEffect(bitmap, blur, saturation, tint, opacity, radius)` | Full glass effect with tint overlay and rounded corners |
| `CreateSaturationFilter(saturation)` | Creates SKColorFilter for saturation (0 = grayscale, 1 = normal) |
| `FromPixelData(pixels, width, height)` | Convert BGRA pixel data to SKBitmap |

### SkiaBackdropCapture

Bridges WinUI's `RenderTargetBitmap` with Skia processing:

```csharp
// Capture area behind element and apply glass effect
var imageSource = await SkiaBackdropCapture.CaptureAndApplyGlassEffectAsync(
    element,
    blurRadius: 40,
    saturation: 0.8,
    tintColor: Colors.White,
    tintOpacity: 0.3,
    cornerRadius: 16);
```

**How it works:**

1. Finds parent element with background
2. Temporarily hides target element
3. Captures region via `RenderTargetBitmap`
4. Converts to `SKBitmap`
5. Applies blur + saturation + tint via Skia
6. Returns as WinUI `ImageSource`

---

## Glass Blur Effects

Both `DaisyGlass` and `DaisyCard` (with `IsGlass="True"` or `CardStyle="Glass"`) support three blur rendering modes:

### Blur Modes

| Mode | Description | Performance | Quality |
| ---- | ----------- | ----------- | ------- |
| `Simulated` | Gradient overlays only (no real blur) | ⚡ Fastest | Good |
| `BitmapCapture` | One-time capture + Skia blur | ⚡⚡ Medium | Better |
| `SkiaSharp` | GPU blur via SkiaSharp | ⚡⚡⚡ Variable | Best |

### Key Properties

Available on both `DaisyGlass` and `DaisyCard`:

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `EnableBackdropBlur` | `bool` | `false` | Enable real blur capture |
| `BlurMode` | `GlassBlurMode` | `Simulated` | Rendering mode |
| `GlassBlur` | `double` | `40` | Blur strength (higher = more blur) |
| `GlassSaturation` | `double` | `1.0` | Saturation (0 = grayscale, 1 = normal) |
| `GlassTint` | `Color` | `White` | Tint overlay color |
| `GlassTintOpacity` | `double` | `0.5` | Tint opacity |
| `GlassOpacity` | `double` | `0.25`/`0.3` | Base glass opacity |
| `GlassBorderOpacity` | `double` | `0.2` | Border line opacity |
| `GlassReflectOpacity` | `double` | `0.1`/`0.15` | Top highlight opacity |

### DaisyGlass Examples

```xml
<!-- Simulated glass (lightweight) -->
<controls:DaisyGlass>
    <TextBlock Text="Simulated" Margin="16" />
</controls:DaisyGlass>

<!-- Bitmap capture blur -->
<controls:DaisyGlass EnableBackdropBlur="True"
                     BlurMode="BitmapCapture"
                     GlassBlur="30">
    <TextBlock Text="Captured Blur" Margin="16" />
</controls:DaisyGlass>

<!-- SkiaSharp blur with desaturation -->
<controls:DaisyGlass EnableBackdropBlur="True"
                     BlurMode="SkiaSharp"
                     GlassBlur="50"
                     GlassSaturation="0.5">
    <TextBlock Text="Desaturated Blur" Margin="16" />
</controls:DaisyGlass>
```

### DaisyCard Glass Examples

```xml
<!-- Simulated glass card (lightweight) -->
<controls:DaisyCard IsGlass="True">
    <TextBlock Text="Glass Card" Margin="16" />
</controls:DaisyCard>

<!-- Glass card with Skia blur -->
<controls:DaisyCard CardStyle="Glass"
                    EnableBackdropBlur="True"
                    BlurMode="SkiaSharp"
                    GlassBlur="40"
                    GlassSaturation="0.8">
    <StackPanel Margin="16">
        <TextBlock Text="Real Blur Effect" FontWeight="Bold" />
        <TextBlock Text="Content over blurred background" />
    </StackPanel>
</controls:DaisyCard>

<!-- Glass card with custom tint -->
<controls:DaisyCard IsGlass="True"
                    EnableBackdropBlur="True"
                    BlurMode="BitmapCapture"
                    GlassTint="LightBlue"
                    GlassTintOpacity="0.3">
    <TextBlock Text="Tinted Glass" Margin="16" />
</controls:DaisyCard>
```

### Programmatic Refresh

For dynamic backgrounds, manually trigger recapture:

```csharp
// DaisyGlass
myGlass.RefreshBackdrop();

// DaisyCard with glass enabled
myCard.RefreshBackdrop();
```

---

## Carousel Slide Effects

Slide effects animate the **currently visible** slide during display. They create cinematic motion like Ken Burns documentary-style pan/zoom.

### Available Effects

| Effect | Description |
| ------ | ----------- |
| `None` | Static display (default) |
| `PanAndZoom` | Randomized pan + zoom combination |
| `ZoomIn` | Slow zoom from 1.0x to 1.0x + intensity |
| `ZoomOut` | Slow zoom from 1.0x + intensity to 1.0x |
| `PanLeft` | Slow pan leftward |
| `PanRight` | Slow pan rightward |
| `PanUp` | Slow pan upward |
| `PanDown` | Slow pan downward |
| `Drift` | Random direction pan (varies per slide) |
| `Pulse` | Subtle breathing/pulsing zoom |
| `Breath` | Zoom in to peak, settle back |
| `Throw` | Dramatic parallax fly-through |

### Effect Parameters

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `SlideEffect` | `FlowerySlideEffect` | `None` | Active effect |
| `ZoomIntensity` | `double` | `0.15` | Zoom scale (0.15 = 115%) |
| `PanDistance` | `double` | `50` | Pan distance in pixels |
| `PulseIntensity` | `double` | `0.08` | Pulse scale (0.08 = 108%) |
| `PanAndZoomLockZoom` | `bool` | `false` | Pan only (no zoom) |
| `PanAndZoomPanSpeed` | `double` | `4.0` | Pan speed (px/sec) |
| `PanAndZoomCycleSeconds` | `double` | `6.0` | Effect cycle duration |

### Usage

```xml
<controls:DaisyCarousel SlideEffect="PanAndZoom"
                        ZoomIntensity="0.2"
                        PanDistance="60"
                        SlideInterval="5">
    <Image Source="photo1.jpg" />
    <Image Source="photo2.jpg" />
</controls:DaisyCarousel>
```

---

## Carousel Slide Transitions

Slide transitions animate the **change between** slides. They are categorized by implementation complexity.

### Tier 1: Transform-Based (Universal)

Uses `CompositeTransform` and `PlaneProjection`. Works on all platforms.

| Family | Transitions |
| ------ | ----------- |
| **Fade** | `Fade`, `FadeThroughBlack`, `FadeThroughWhite` |
| **Slide** | `SlideLeft`, `SlideRight`, `SlideUp`, `SlideDown` |
| **Push** | `PushLeft`, `PushRight`, `PushUp`, `PushDown` |
| **Zoom** | `ZoomIn`, `ZoomOut`, `ZoomCross` |
| **Flip** | `FlipHorizontal`, `FlipVertical`, `CubeLeft`, `CubeRight` |
| **Cover/Reveal** | `CoverLeft`, `CoverRight`, `CoverUp`, `CoverDown`, `RevealLeft`, `RevealRight` |

### Tier 2: Clip-Based (Advanced)

Uses animated `RectangleGeometry` clips. Works on most platforms.

| Family | Transitions |
| ------ | ----------- |
| **Wipe** | `WipeLeft`, `WipeRight`, `WipeUp`, `WipeDown` |
| **Blinds** | `BlindsHorizontal`, `BlindsVertical` |
| **Slices** | `SlicesHorizontal`, `SlicesVertical`, `Checkerboard`, `Spiral`, `MatrixRain`, `Wormhole` |

### Tier 3: Skia-Based (Experimental)

Requires SkiaSharp rendering. Only on Skia-enabled targets (Desktop, WASM).

| Transition | Description |
| ---------- | ----------- |
| `Dissolve` | Pixel noise dissolve pattern |
| `Pixelate` | Mosaic pixelation effect |

### Special Transitions

| Transition | Description |
| ---------- | ----------- |
| `None` | Instant snap (no animation) |
| `Random` | Random transition per navigation |

### Transition Parameters

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `SlideTransition` | `FlowerySlideTransition` | `None` | Active transition |
| `TransitionDuration` | `double` | `0.4` | Duration in seconds |
| `TransitionSliceCount` | `int` | `8` | Slices for Blinds/Slices |
| `TransitionStaggerSlices` | `bool` | `true` | Stagger slice animations |
| `TransitionSliceStaggerMs` | `double` | `50` | Stagger delay (ms) |
| `TransitionCheckerboardSize` | `int` | `6` | Grid size for Checkerboard |
| `TransitionBlurRadius` | `double` | `20` | Blur for blur transitions |
| `TransitionPixelateSize` | `int` | `20` | Pixel block size |
| `TransitionDissolveDensity` | `double` | `0.5` | Noise density for Dissolve |
| `TransitionFlipAngle` | `double` | `90` | Rotation angle for flips |
| `TransitionPerspectiveDepth` | `double` | `1000` | 3D perspective depth |

### Transition Usage

```xml
<controls:DaisyCarousel SlideTransition="Fade"
                        TransitionDuration="0.6"
                        WrapAround="True">
    <Image Source="slide1.jpg" />
    <Image Source="slide2.jpg" />
</controls:DaisyCarousel>
```

### Slideshow Modes

| Mode | Description |
| ---- | ----------- |
| `Manual` | User navigates via buttons/swipe |
| `Slideshow` | Auto-advance sequentially (loops) |
| `Random` | Auto-advance in random order |
| `Kiosk` | Random order + random effects (screensaver) |

```xml
<controls:DaisyCarousel Mode="Kiosk"
                        SlideInterval="4"
                        SlideTransition="Random">
    <!-- Slides -->
</controls:DaisyCarousel>
```

---

## Helper Classes & APIs

### Slide Effect Helpers

Located in `Flowery.Helpers` namespace:

| Class | Purpose |
| ----- | ------- |
| `FlowerySlideEffects` | Attached properties for applying slide effects to any `UIElement` |
| `FlowerySlideEffectParams` | Record for configuring effect parameters |
| `FlowerySlideEffectParser` | Parses string names to `FlowerySlideEffect` enum |

**Attached Property Usage:**

```xml
xmlns:helpers="using:Flowery.Helpers"

<Image Source="photo.jpg"
       helpers:FlowerySlideEffects.Effect="PanAndZoom"
       helpers:FlowerySlideEffects.Duration="5"
       helpers:FlowerySlideEffects.ZoomIntensity="0.15"
       helpers:FlowerySlideEffects.AutoStart="True" />
```

**Programmatic Usage:**

```csharp
using Flowery.Helpers;

// Apply effect to any UIElement
FlowerySlideEffects.SetEffect(myImage, FlowerySlideEffect.PanAndZoom);
FlowerySlideEffects.SetDuration(myImage, 5.0);
FlowerySlideEffects.SetAutoStart(myImage, true);

// Start/stop manually
FlowerySlideEffect applied = FlowerySlideEffects.StartEffect(myImage);
FlowerySlideEffects.StopEffect(myImage);

// Preserve transform during stop (for smooth transitions)
FlowerySlideEffects.StopEffectPreservingTransform(myImage);
```

### Slide Transition Helpers

| Class | Purpose |
| ----- | ------- |
| `FlowerySlideTransitionHelpers` | Applies transitions between two slide elements |
| `FlowerySlideTransitionParams` | Record for configuring transition parameters |
| `FlowerySlideTransitionParser` | Parses string names to `FlowerySlideTransition` enum |
| `FloweryTransitionTier` | Enum categorizing transitions (Transform/Clip/Skia) |

**Programmatic Transition:**

```csharp
using Flowery.Helpers;

var transitionParams = new FlowerySlideTransitionParams
{
    Duration = TimeSpan.FromMilliseconds(500),
    SliceCount = 10,
    StaggerSlices = true
};

FlowerySlideTransition applied = FlowerySlideTransitionHelpers.ApplyTransition(
    container,
    oldSlideWrapper,
    newSlideWrapper,
    FlowerySlideTransition.Checkerboard,
    transitionParams,
    onComplete: () => { /* cleanup */ });
```

### Slideshow Controller

| Class | Purpose |
| ----- | ------- |
| `FlowerySlideshowController` | Manages auto-advance timing for slideshow/kiosk modes |
| `FlowerySlideshowMode` | Enum: `Manual`, `Slideshow`, `Random`, `Kiosk` |
| `FlowerySlideChangedEventArgs` | Event args with old/new index, applied effect, current element |

### Carousel GL Effects (Browser Only)

Located in `Flowery.Effects` namespace:

| Class | Purpose |
| ----- | ------- |
| `CarouselGlEffectHost` | Control that hosts Skia carousel previews (browser only) |
| `CarouselGlEffectRegistry` | Maps `CarouselGlEffectKind` to effect definitions |
| `CarouselGlRuntime` | Selects browser Skia runtime or no-op |
| `CarouselGlEffectKind` | Enum of all GL effect types |

**Browser Skia Usage:**

```xml
xmlns:fx="using:Flowery.Effects"

<fx:CarouselGlEffectHost Effect="MatrixRain"
                         OverlayEffect="Dissolve"
                         IntervalSeconds="4">
    <Image Source="slide.jpg" />
</fx:CarouselGlEffectHost>
```

**Available Carousel GL Effects:**

| Category | Effects |
| -------- | ------- |
| **Shader Transitions** | `MaskTransition`, `Dissolve`, `Pixelate`, `FlipPlane` |
| **Grid Patterns** | `BlindsHorizontal/Vertical`, `SlicesHorizontal/Vertical`, `Checkerboard`, `Spiral`, `MatrixRain`, `Wormhole` |
| **3D Transforms** | `FlipHorizontal/Vertical`, `CubeLeft/Right` |
| **Motion Effects** | `EffectPanAndZoom`, `EffectZoomIn/Out`, `EffectPan*`, `EffectDrift`, `EffectPulse`, `EffectBreath`, `EffectThrow` |

### Skia Infrastructure

| Class | Purpose |
| ----- | ------- |
| `SkiaImageProcessor` | Core image blur, saturation, glass effect processing |
| `SkiaBackdropCapture` | Captures WinUI elements and processes with Skia |

**Direct Skia Processing:**

```csharp
using Flowery.Skia;

// Apply blur to image bytes
byte[]? blurred = SkiaImageProcessor.ApplyBlur(pngBytes, sigmaX: 5, sigmaY: 5);

// Apply glass effect to bitmap
byte[]? glass = SkiaImageProcessor.ApplyGlassEffect(
    skBitmap,
    blurSigma: 4.0f,
    saturation: 0.8f,
    tintColor: SKColors.White,
    tintOpacity: 0.3f,
    cornerRadius: 16f);
```

---

## Platform Support Matrix

| Feature | Windows | Desktop (Skia) | Browser (WASM) | Android | iOS |
| ------- | ------- | -------------- | -------------- | ------- | --- |
| **DaisyGlass Simulated** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **DaisyGlass BitmapCapture** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **DaisyGlass SkiaSharp** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Tier 1 Transitions** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Tier 2 Transitions** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Tier 3 Transitions** | ⚠️ | ✅ | ✅ | ⚠️ | ⚠️ |
| **Slide Effects** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Carousel GL Effects** | ❌ | ❌ | ✅ | ❌ | ❌ |

Legend: ✅ Full support | ⚠️ Experimental/Limited | ❌ Not supported

---

## Performance Considerations

### DaisyGlass

- **Simulated mode** is always the fastest; use for high-frequency scenarios
- **BitmapCapture** captures once on load; call `RefreshBackdrop()` sparingly
- **SkiaSharp** blur sigma scales with CPU cost; keep `GlassBlur` under 50 for smooth performance
- Backdrop capture temporarily hides the element; avoid during animations

### Carousel Effects

- **Slide effects** run continuously; prefer lower `ZoomIntensity` and `PanDistance` for smooth 60fps
- **PanAndZoom** is the most versatile but most complex; test on target devices
- **Tier 3 transitions** require Skia rendering; may not work on all platforms

### Memory

- SkiaSharp bitmaps are disposed after processing
- `RenderTargetBitmap` captures can be large; scale factor (0.75) reduces memory
- Clear `ImageSource` on Unloaded to free resources

---

## Future Enhancements

- **Real-time blur** via `SKCanvasElement` (live backdrop sampling)
- **Composition API blur** using `GaussianBlurEffect` where available
- **Hardware backdrop** on platforms that support native acrylic
- **Animated glass** with continuous background sampling

# FloweryCarouselGL

Browser-only Skia runtime previews for transitions/effects not available in XAML on WASM. This feature ships as presets that you can attach to any container, with safe fallbacks on non-browser targets.

Namespace: `Flowery.Effects`

## Quick Start

```xml
xmlns:fx="using:Flowery.Effects"
```

```xml
<fx:CarouselGlEffectHost Effect="Checkerboard" HorizontalContentAlignment="Stretch">
    <Grid>
        <TextBlock Text="Fallback content for non-WASM targets."
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
    </Grid>
</fx:CarouselGlEffectHost>
```

On Browser/WASM, the Skia runtime renders using the built-in carousel. On other targets, the host behaves like a normal `ContentControl`.

## Built-in Presets

Mask variants (mode: `mask`):

- `MaskTransition` (checkerboard default)
- `BlindsHorizontal`, `BlindsVertical`
- `SlicesHorizontal`, `SlicesVertical`
- `Checkerboard`
- `Spiral`
- `MatrixRain`
- `Wormhole`
- `Dissolve`
- `Pixelate`

Flip variants (mode: `flip`):

- `FlipPlane`
- `FlipHorizontal`
- `FlipVertical`
- `CubeLeft`
- `CubeRight`

Text fill (mode: `text`):

- `TextFill`

Transition variants (mode: `transition`):

- `Fade`, `FadeThroughBlack`, `FadeThroughWhite`
- `SlideLeft`, `SlideRight`, `SlideUp`, `SlideDown`
- `PushLeft`, `PushRight`, `PushUp`, `PushDown`
- `ZoomIn`, `ZoomOut`, `ZoomCross`
- `CoverLeft`, `CoverRight`
- `RevealLeft`, `RevealRight`
- `WipeLeft`, `WipeRight`, `WipeUp`, `WipeDown`
- `Random`

Effect variants (mode: `effect`):

- `EffectPanAndZoom`
- `EffectZoomIn`, `EffectZoomOut`
- `EffectPanLeft`, `EffectPanRight`, `EffectPanUp`, `EffectPanDown`
- `EffectDrift`, `EffectPulse`, `EffectBreath`, `EffectThrow`

Each preset maps to a distinct shader variant so the look is different for each effect.

## GL Transitions (gl-transitions repo)

The Gallery also ships a separate WebGL-only integration for the `gl-transitions` shader collection. This is used by the `CarouselGlTransitionsExamples` page and is intentionally isolated from the core FloweryCarouselGL presets.

- Shaders live in `Flowery.Uno.Gallery/WasmScripts/gl-transitions`.
- The loader/runtime is `Flowery.Uno.Gallery/WasmScripts/flowery-carousel-gl-transitions.js`.
- The XAML host is `Flowery.Uno.Gallery.Effects.CarouselGlTransitionHost`, which drives the browser overlay service.
- This path is enabled only for Browser/WASM builds via the `FLOWERY_GL_TRANSITIONS` compile define (property `FloweryEnableGlTransitions`).

Credits: Original shader sources are from the `gl-transitions` project (<https://github.com/gl-transitions/gl-transitions>). Local copies include compatibility fixes and default handling for the Flowery Gallery runtime.

## Assets

Presets do not ship with images. Provide your own assets (Content files or absolute URLs) to control the look. If assets are missing or fail to load, the overlay shows a "Not supported" message instead of faking the effect.

For app-local files, include them as Content and reference them with `Assets/...`.

### Asset Resolution Paths

There are two asset loaders depending on which runtime is active:

- **WebGL overlay (Browser/WASM)** uses the JS loader in `WasmScripts/flowery-carousel-gl.js`.
  - Accepts absolute URLs (`http(s)://...`) and `data:` URIs.
  - Any other string is treated as a relative path under `document.baseURI/_framework/`.
  - Example: `Assets/foo.jpg` becomes `_framework/Assets/foo.jpg`.
  - Do **not** use `ms-appx:///` in this path.

- **Gallery-side Skia preview (Browser/WASM)** uses `CarouselGlSkiaRuntime.Browser.cs` and its `CreateImageSource` helper.
  - Accepts absolute URLs and `data:` URIs.
  - `Assets/...` is resolved to `ms-appx:///Flowery.Uno.Gallery/Assets/...`.
  - `ms-appx:///...` is accepted directly and converted to the Browser `_framework/` URL.
  - Other strings are treated as `ms-appx:///` relative paths.

## Asset Keys

When overriding assets, use these keys:

- Mask: `maskA`, `maskB`
- Flip: `flipA`, `flipB`
- Transition: `transitionA`, `transitionB`
- Effect: `effect`
- Text fill: `text`

## Customizing Presets

You can override any preset by re-registering it:

```csharp
using Flowery.Effects;

CarouselGlEffectRegistry.Register(
    new CarouselGlEffectDefinition(
        CarouselGlEffectKind.Checkerboard,
        "mask",
        new Dictionary<string, string>
        {
            ["maskA"] = "Assets/my_mask_a.jpg",
            ["maskB"] = "Assets/my_mask_b.jpg"
        },
        "checkerboard"));
```

Notes:

- `CarouselGlEffectKind` is an enum, so adding brand-new kinds requires a library change.
- Asset paths can be absolute URLs or relative `Assets/...` paths.

## API Reference

### CarouselGlEffectHost

XAML host control that renders the Skia carousel preview on Browser/WASM.

```csharp
public CarouselGlEffectKind Effect { get; set; }
public double IntervalSeconds { get; set; }
public double TransitionSeconds { get; set; }
public int TransitionSliceCount { get; set; }
public bool TransitionStaggerSlices { get; set; }
public double TransitionSliceStaggerMs { get; set; }
public int TransitionPixelateSize { get; set; }
public double TransitionDissolveDensity { get; set; }
public double TransitionFlipAngle { get; set; }
```

### CarouselGlEffectRegistry

Static registry for presets.

```csharp
public static bool TryGet(CarouselGlEffectKind kind, out CarouselGlEffectDefinition? definition);
public static void Register(CarouselGlEffectDefinition definition);
```

### CarouselGlEffectDefinition

```csharp
public CarouselGlEffectDefinition(
    CarouselGlEffectKind kind,
    string mode,
    IReadOnlyDictionary<string, string> assets,
    string? variant = null);
```

## Runtime Notes

- Browser builds use the WebGL overlay runtime (JS shader pipeline).
- The page is browser-only; non-browser builds are no-ops.
- Use explicit sizes (Height/MaxWidth) for predictable aspect ratios.

### Transition Parameters (Browser/WebGL)

These parameters now flow into the WebGL shader:

- `TransitionSliceCount` controls the tile count for mask patterns (blinds/slices/checkerboard/spiral/matrix).
- `TransitionStaggerSlices` + `TransitionSliceStaggerMs` control the per-slice delay for mask patterns.
- `TransitionPixelateSize` controls pixel block size for Pixelate.
- `TransitionDissolveDensity` controls the noise grain for Dissolve.
- `TransitionFlipAngle` controls the maximum flip rotation (degrees) for flip/cube.

### Pixelate Behavior

Pixelate is a two-phase transition:

1. Outgoing image pixelates to the target block size.
2. Switch to the next image at mid-duration, then un-pixelate back to full sharpness.

On Browser/WebGL, the transition duration is dynamically scaled so the pixelation fully completes for the configured pixel size.

## Troubleshooting

- If images do not load, ensure the files are included as Content and referenced with `Assets/...`.

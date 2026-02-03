# Carousel Tutorial

This guide shows how to add DaisyCarousel to your app and wire up common scenarios with clear, practical steps.

## 1) Add Flowery.Uno and theme resources

1. Reference Flowery.Uno (project reference or NuGet package).
2. Ensure the library theme resources are merged in `App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="ms-appx:///Flowery.Uno/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

1. Add the control namespace in your page:

```xml
xmlns:daisy="using:Flowery.Controls"
```

## 2) Basic carousel (manual)

Start with a simple manual carousel. Use explicit `Width` and `Height` so transitions clip cleanly.

```xml
<daisy:DaisyCarousel Width="520" Height="280">
    <Border CornerRadius="12">
        <Image Source="Assets/Wallpapers/hero-1.jpg" Stretch="UniformToFill" />
    </Border>
    <Border CornerRadius="12">
        <Image Source="Assets/Wallpapers/hero-2.jpg" Stretch="UniformToFill" />
    </Border>
    <Border CornerRadius="12">
        <Image Source="Assets/Wallpapers/hero-3.jpg" Stretch="UniformToFill" />
    </Border>
</daisy:DaisyCarousel>
```

Notes:

- Each slide should be a single root element (wrap content in a `Border` if needed).
- Use `Stretch="UniformToFill"` for photos so the image fills the slide.
- Ensure your images are marked as `Content` in the project.

## 3) Slideshow + transition

Enable auto-advance with a transition:

```xml
<daisy:DaisyCarousel Width="520" Height="280"
                     Mode="Slideshow"
                     SlideInterval="4"
                     SlideTransition="Fade"
                     TransitionDuration="0.6"
                     ShowNavigation="False"
                     WrapAround="True">
    <!-- Slides -->
</daisy:DaisyCarousel>
```

How it works:

- `Mode="Slideshow"` or `Mode="Random"` auto-advances.
- `SlideInterval` is the hold time between transitions (min 0.5).
- `TransitionDuration` controls how long the animation runs.

## 4) Slide effects vs transitions

- **SlideTransition** animates the change between slides.
- **SlideEffect** animates the currently visible slide.

Example effect:

```xml
<daisy:DaisyCarousel Width="520" Height="280"
                     Mode="Slideshow"
                     SlideInterval="5"
                     SlideTransition="Fade"
                     SlideEffect="PanAndZoom"
                     ZoomIntensity="0.2"
                     PanDistance="60"
                     PanAndZoomCycleSeconds="6">
    <!-- Slides -->
</daisy:DaisyCarousel>
```

## 5) Transition tuning (advanced)

Some transitions expose extra tuning parameters:

```xml
<daisy:DaisyCarousel Width="520" Height="280"
                     SlideTransition="Checkerboard"
                     TransitionDuration="0.7"
                     TransitionCheckerboardSize="8"
                     TransitionSliceStaggerMs="60"
                     TransitionStaggerSlices="True">
    <!-- Slides -->
</daisy:DaisyCarousel>
```

Useful knobs:

- `TransitionSliceCount`, `TransitionStaggerSlices`, `TransitionSliceStaggerMs`
- `TransitionCheckerboardSize`
- `TransitionPixelateSize`, `TransitionDissolveDensity`
- `TransitionFlipAngle`, `TransitionPerspectiveDepth`

Note: `Dissolve` and `Pixelate` require Skia-enabled targets (Desktop/WASM). On other targets they may fall back.

## 6) Data binding with ItemTemplate

Bind to a collection when your slides come from data:

```xml
<daisy:DaisyCarousel ItemsSource="{Binding Slides}"
                     Width="520"
                     Height="280"
                     SlideTransition="Fade">
    <daisy:DaisyCarousel.ItemTemplate>
        <DataTemplate x:DataType="local:CarouselItem">
            <Border CornerRadius="12">
                <Image Source="{Binding ImagePath}" Stretch="UniformToFill" />
            </Border>
        </DataTemplate>
    </daisy:DaisyCarousel.ItemTemplate>
</daisy:DaisyCarousel>
```

```csharp
public sealed record CarouselItem(string ImagePath, string Title);

public ObservableCollection<CarouselItem> Slides { get; } = new()
{
    new("Assets/Wallpapers/hero-1.jpg", "Mountains"),
    new("Assets/Wallpapers/hero-2.jpg", "Ocean"),
    new("Assets/Wallpapers/hero-3.jpg", "Forest")
};
```

## 7) Interaction tips

- `Orientation="Vertical"` switches navigation to up/down.
- `SwipeThreshold` controls how far the user must drag before a swipe counts.
- `WrapAround="True"` keeps the carousel looping on manual navigation.
- Bind `SelectedIndex` if you need to drive it from view-model state.

## 8) Browser (WASM) and GL versions

DaisyCarousel works normally on the Browser/WASM head using its standard XAML-based transitions and effects. WebGL-based transitions are a separate, browser-only path.

There are two GL implementations to be aware of:

1) **FloweryCarouselGL (built-in presets)**  
   - Use `Flowery.Effects.CarouselGlEffectHost` to render WebGL/Skia shader presets.  
   - Browser-only; on other heads it behaves like a normal `ContentControl`.  
   - This is the recommended GL path for app developers.

2) **gl-transitions repo integration (Gallery reference)**  
   - Optional integration used by the Gallery only.  
   - Requires the `FLOWERY_GL_TRANSITIONS` compile define (property `FloweryEnableGlTransitions` in the Browser head).  
   - Uses `flowery-carousel-gl-transitions.js` and the shader folder under `WasmScripts/gl-transitions`.  
   - If you want this in your app, mirror the Gallery setup and copy those assets into your Browser head.

## References

- `llms-static/DaisyCarousel.md` for the full property list and examples.
- `llms-static/FloweryAnimationHelpers.md` for slide effects and helpers.
- `llms-static/Graphics.md` for platform notes and transition tiers.
- `THEMING.md` if you need theme setup or customization.
- `llms-static/FloweryCarouselGL.md` for browser-only GL preview effects.

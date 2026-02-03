# Overview

Neumorphic (Soft UI) mode is a design style that uses highlights and shadows to create elements that appear to extrude from or be inset into the background surface.

## What is Neumorphism?

Neumorphism (new skeuomorphism) combines the realism of skeuomorphism with the simplicity of flat design. It creates soft, extruded UI elements that appear to push out from or press into the background surface, using carefully positioned light and dark shadows.

### The Three Essential Properties

To apply neumorphism style to any element, you must implement these three main properties:

1. **Background Match**: Element background must be the same as its container (seamless surface)
2. **Dark Shadow**: Applied to one corner (bottom-right for raised, top-left for inset)
3. **Light Shadow**: Applied to the opposite corner (top-left for raised, bottom-right for inset)

### Shadow Positioning (Light Source Model)

The effect assumes a **top-left light source** (like sunlight through a window):

- **Raised Mode**:
  - Light shadow on top-left (where light hits the raised edge)
  - Dark shadow on bottom-right (where light doesn't reach)
- **Inset Mode**: Inverted positioning
  - Dark shadow on top-left (inner edge in shadow)
  - Light shadow on bottom-right (inner edge catching light)

## Technical Implementation

Flowery.Uno implements neumorphism using a cross-platform approach via `DaisyNeumorphicHelper` with platform-specific optimizations.

### Implementation Strategy by Mode

**Raised/Flat Modes** - Platform-specific shadow rendering:

| Platform | Implementation | Notes |
| :--- | :--- | :--- |
| **Windows** | Composition API `DropShadow` | Single shadow (dark) for reliability |
| **Desktop (Skia)** | `UIElementExtensions.SetElevation()` | Single shadow via ShadowState; some templates (e.g., `DaisyButton`) apply elevation to `PART_ShadowContainer` to avoid clipping |
| **WASM** | `UIElementExtensions.SetElevation()` | CSS box-shadow |
| **Android** | `UIElementExtensions.SetElevation()` | ViewCompat.SetElevation |
| **iOS** | `UIElementExtensions.SetElevation()` | CALayer.shadow* |

**Windows-Specific Implementation** (`DaisyNeumorphicHelper.Windows.cs`):

- Uses partial class pattern to isolate Windows code (`#if WINDOWS`)
- Creates a single `SpriteVisual` with `DropShadow` (dark shadow, bottom-right)
- Uses a simple opaque mask to render the shadow shape
- Shadow colors derived from theme tokens (see Theme Integration below)
- **Alternative**: `UIElementExtensions.SetElevation()` also works on Windows and can be used as a fallback or a simpler path when Composition shadows are disabled.
- **Direct elevation routing**: use `DaisyNeumorphicHelper.ShouldUseDirectElevation()` to decide when to bypass the helper and call `SetElevation` directly (Skia/WASM/non-Windows).
- **Elevation target by control (Skia/WASM)**:
  - `DaisyButton`: apply elevation to `PART_ShadowContainer` (template wrapper) to avoid clipping; falls back to `ButtonBorder` when the container is missing.
  - Most other controls: apply elevation to their neumorphic host element (e.g., `DaisyCard` uses `PART_SolidBackground`).
- **Note**: ThemeShadow is intentionally not included due to limited use and compatibility.

### XAML Template Patterns (for custom controls)

**Goal:** make it obvious which element should receive elevation on Skia/WASM and where inset overlays can be injected.

#### Namespace Cheat Sheet (XAML)

- `xmlns:utu="using:Uno.Toolkit.UI"` for `ShadowContainer`
- `xmlns:controls="using:Flowery.Controls"` for Flowery controls (e.g., `DaisyButton`)

#### Platform Differences (Quick Reference)

- **Windows (WinAppSDK)**: prefers Composition `DropShadow` (single dark shadow); optional ThemeShadow for special cases.
- **Desktop (Skia)**: uses `UIElementExtensions.SetElevation()`; apply elevation to `PART_ShadowContainer` if the surface is wrapped (prevents clipping).
- **WASM**: uses CSS `box-shadow` via `SetElevation`; apply elevation to the surface border (or container if used).
- **Android/iOS**: use native elevation APIs via `SetElevation` (single shadow).

#### Platform × Mode Matrix

| Platform | Raised | Flat | Inset |
| :--- | :--- | :--- | :--- |
| **Windows (WinAppSDK)** | Composition `DropShadow` (dark only) | Composition `DropShadow` (reduced) | Gradient overlays (inset borders) |
| **Desktop (Skia)** | `SetElevation` (single shadow) | `SetElevation` (reduced) | Gradient overlays (inset borders) |
| **WASM** | `SetElevation` (CSS box-shadow) | `SetElevation` (reduced) | Gradient overlays (inset borders) |
| **Android** | `SetElevation` (native view elevation) | `SetElevation` (reduced) | Gradient overlays (inset borders) |
| **iOS** | `SetElevation` (CALayer shadow) | `SetElevation` (reduced) | Gradient overlays (inset borders) |

#### Pattern A — Simple Surface (Most Controls)

- Use a single surface element with a stable name (e.g., `PART_SolidBackground`).
- In code, override `GetNeumorphicHostElement()` to return that surface.
- Apply `SetElevation` to that surface on Skia/WASM.

Example (simplified):

```xml
<Border x:Name="PART_SolidBackground"
        Background="{TemplateBinding Background}"
        CornerRadius="{TemplateBinding CornerRadius}">
  <!-- content -->
</Border>
```

#### Pattern B — Shadow Wrapper (DaisyButton Style)

- Wrap the surface in a `ShadowContainer` so the shadow is not clipped by the border.
- On Skia/WASM, apply elevation to the `ShadowContainer`, not the inner `Border`.

Example (simplified):

```xml
<!-- xmlns:utu="using:Uno.Toolkit.UI" -->
<utu:ShadowContainer x:Name="PART_ShadowContainer"
                     CornerRadius="{TemplateBinding CornerRadius}">
  <Border x:Name="ButtonBorder"
          Background="{TemplateBinding Background}"
          CornerRadius="{TemplateBinding CornerRadius}">
    <!-- content -->
  </Border>
</utu:ShadowContainer>
```

#### Inset Overlays

- Inset mode injects overlay `Border` elements into a container grid.
- Ensure the template allows overlay injection (i.e., the host element is part of the visual tree and not re-parented later).

### Full How-To (add neumorphism to a custom control)

#### Step 1: Choose a surface element in XAML

- Pick the element that represents the visible surface (usually a `Border`).
- If the surface can clip shadows, wrap it in `PART_ShadowContainer` (Pattern B).

#### Step 2: Expose the surface in code

- If your control derives from `DaisyBaseContentControl`, override `GetNeumorphicHostElement()` and return the surface element.
- If you add a `ShadowContainer` wrapper, make sure you can access it in code (e.g., `GetTemplateChild("PART_ShadowContainer")`).

Example (surface host override):

```csharp
protected override FrameworkElement? GetNeumorphicHostElement()
{
    return _surfaceBorder ?? this;
}
```

#### Step 3: Apply neumorphic settings

- Use attached properties to opt in or override global settings.

```xml
<local:MyControl
    NeumorphicEnabled="True"
    NeumorphicMode="Raised"
    NeumorphicIntensity="0.8" />
```

#### Step 4: Ensure template parts are ready

- In `OnApplyTemplate`, cache template parts (`_surfaceBorder`, `_shadowContainer`, etc.).
- Call `RequestNeumorphicRefresh()` after template is ready.

#### Step 5: Direct elevation routing

- When `DaisyNeumorphicHelper.ShouldUseDirectElevation()` is true (Skia/WASM/non-Windows), apply `SetElevation` to:
  - `PART_ShadowContainer` (if you use Pattern B), otherwise
  - the surface border (Pattern A).

#### Step 6: Clear on None

- When mode is `None`, clear elevation on the same element you used for direct elevation.

#### Step 7: Avoid template conflicts

- Do not re-parent the surface element after the helper has injected overlays.
- Keep the surface element in the visual tree; avoid replacing it at runtime.

### Worked Example A — Simple Surface (Pattern A)

#### XAML

```xml
<ControlTemplate x:Key="MyCardTemplate" TargetType="local:MyCard">
  <Border x:Name="PART_SolidBackground"
          Background="{TemplateBinding Background}"
          CornerRadius="{TemplateBinding CornerRadius}">
    <ContentPresenter Margin="{TemplateBinding Padding}" />
  </Border>
</ControlTemplate>
```

#### Code-Behind

```csharp
public sealed partial class MyCard : DaisyBaseContentControl
{
    private Border? _surfaceBorder;

    public MyCard()
    {
        DefaultStyleKey = typeof(MyCard);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _surfaceBorder = GetTemplateChild("PART_SolidBackground") as Border;
        RequestNeumorphicRefresh();
    }

    protected override FrameworkElement? GetNeumorphicHostElement()
    {
        return _surfaceBorder ?? this;
    }
}
```

#### Usage

```xml
<local:MyCard NeumorphicEnabled="True"
              NeumorphicMode="Raised"
              NeumorphicIntensity="0.8" />
```

### Worked Example B — Shadow Wrapper (Pattern B / DaisyButton Style)

#### XAML B

```xml
<!-- xmlns:utu="using:Uno.Toolkit.UI" -->
<ControlTemplate x:Key="MyButtonTemplate" TargetType="local:MyButton">
  <utu:ShadowContainer x:Name="PART_ShadowContainer"
                       CornerRadius="{TemplateBinding CornerRadius}">
    <Border x:Name="PART_SurfaceBorder"
            Background="{TemplateBinding Background}"
            CornerRadius="{TemplateBinding CornerRadius}">
      <ContentPresenter Margin="{TemplateBinding Padding}" />
    </Border>
  </utu:ShadowContainer>
</ControlTemplate>
```

#### Code-Behind B

```csharp
public sealed partial class MyButton : DaisyBaseContentControl
{
    private Border? _surfaceBorder;
    private ShadowContainer? _shadowContainer;

    public MyButton()
    {
        DefaultStyleKey = typeof(MyButton);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _surfaceBorder = GetTemplateChild("PART_SurfaceBorder") as Border;
        _shadowContainer = GetTemplateChild("PART_ShadowContainer") as ShadowContainer;
        RequestNeumorphicRefresh();
    }

    protected override FrameworkElement? GetNeumorphicHostElement()
    {
        return _surfaceBorder ?? this;
    }

    private UIElement? GetElevationTarget()
    {
        if (DaisyNeumorphicHelper.ShouldUseDirectElevation() && _shadowContainer != null)
            return _shadowContainer;

        return _surfaceBorder;
    }
}
```

#### Usage B

```xml
<local:MyButton NeumorphicEnabled="True"
                NeumorphicMode="Raised"
                NeumorphicIntensity="0.8" />
```

**Inset Mode** - Injects gradient overlay borders into the visual tree:

- Two semi-transparent `Border` elements with `LinearGradientBrush` backgrounds
- Dark gradient from top-left corner, light gradient from bottom-right corner
- Overlays on top of the owner's content without blocking interaction

### Elevation System (Size-Aware)

Elevation values are based on the control's `DaisySize` via `DaisyResourceLookup.GetDefaultElevation()`:

| DaisySize | Elevation |
| :--- | :--- |
| ExtraSmall | 10 |
| Small | 11 |
| Medium | 12 |
| Large | 14 |
| ExtraLarge | 16 |

The caller (e.g., `DaisyButton`) passes the appropriate elevation based on its Size property.

### DaisyNeumorphicHelper API

```csharp
public void Update(
    bool enabled,
    DaisyNeumorphicMode mode,
    double intensity,
    double elevation)
```

| Parameter | Description |
| :--- | :--- |
| `enabled` | Whether neumorphic effect is active |
| `mode` | `Raised`, `Inset`, `Flat`, or `None` |
| `intensity` | Multiplier for shadow strength (0.0 - 1.0) |
| `elevation` | Base shadow depth in pixels (from `DaisyResourceLookup.GetDefaultElevation()`) |

### Deferred Attachment (Timing Fix)

The neumorphic helper requires the control's visual tree to be fully laid out before it can find the correct injection point. When `Content` is set on a `ContentControl`, the `ContentPresenter` doesn't process it immediately—this happens on the next layout pass.

**The Problem**: If `RefreshNeumorphicEffect()` runs immediately after `OnLoaded()`:

1. The helper calls `VisualTreeHelper.GetChild()` to find the visual tree
2. The `ContentPresenter` has 0 children (not laid out yet)
3. The helper falls back to attaching directly to the `ContentPresenter`
4. When layout completes, the composition surface renders ON TOP of the actual content
5. Controls appear as blank boxes

**The Solution**: Neumorphic attachment is deferred until after the first `LayoutUpdated` event:

```csharp
private void OnBaseLoaded(object sender, RoutedEventArgs e)
{
    // ... subscriptions ...
    OnLoaded();  // Derived control builds its visual tree here

    // Defer until visual tree is laid out
    LayoutUpdated += OnFirstLayoutForNeumorphic;
}

private void OnFirstLayoutForNeumorphic(object? sender, object e)
{
    LayoutUpdated -= OnFirstLayoutForNeumorphic;
    if (_isLoaded) RefreshNeumorphicEffect();
}
```

This ensures dynamically-built controls (like `DaisyInput`, `DaisyTextArea`) work correctly with neumorphic mode.

### Stability Safeguards (Post-Hang Fixes)

The following safeguards were added after real-world hangs (scrolling + page switches) to prevent layout storms and runaway visual tree updates:

1. **Single-shot overlay injection**
   - The helper now attempts to inject the overlay container only once per instance.
   - If the owner is not a direct child of a supported parent (`Panel` or `ContentPresenter`), injection is marked as failed and will not be retried.
   - This avoids repeated `LayoutUpdated` loops when the visual tree cannot be safely re-parented.

2. **Graceful fallback when injection fails**
   - If overlay injection fails, the helper *disables inset overlays* for that instance and falls back to **direct elevation** for Raised/Flat.
   - This keeps the app responsive on complex templates and virtualized panels, at the cost of losing inset on that element.

3. **Layout monitoring is gated**
   - `LayoutUpdated` monitoring is stopped as soon as the overlay is injected *or* injection is known to be impossible.
   - Update calls are ignored while the element is unloaded or the injection has failed, preventing update storms during scroll.

4. **No forced enablement**
   - Controls must respect `IsNeumorphicEffectivelyEnabled()` and **never** force-enable the effect when global mode is `None`.
   - Example fix: `DaisySlideToConfirm` stopped enabling neumorphism unconditionally to avoid background update churn.

5. **Gallery diagnostics guard (runtime only)**
   - Central in-app diagnostic log aggregation was disabled in the Gallery app to avoid runaway memory usage when many controls update simultaneously.
   - This is a Gallery-only safety measure and does not affect library logging APIs.

### Field-Tested Integration Notes (Session Takeaways)

These are proven fixes from real-world testing that eliminated regressions across Windows + Desktop:

1. **Pick the real surface as the host element**
   - For composite controls, attach neumorphism to the element that actually renders the surface.
   - Example: `DaisyCard` now uses `PART_SolidBackground` via `GetNeumorphicHostElement()` to prevent background loss on Windows and Skia.

2. **ComboBox popup anchoring after host changes**
   - When the host is an inner element, anchor the drop-down to the trigger button (not the outer control) so the popup aligns with the input instead of its shadow.

3. **Avoid raw Composition DropShadow on Skia**
   - `Compositor.CreateDropShadow()` is not implemented in Uno Skia and throws `NotImplementedException`.
   - Use `DaisyNeumorphicHelper` or `ShadowContainer` for cross-platform shadows instead of raw Composition APIs.

4. **Do NOT merge Uno.Toolkit.* Generic.xaml**
   - The toolkit does not ship `Styles/Generic.xaml`. Merging it causes startup failures.
   - ShadowContainer only needs the package reference; no Generic.xaml merge.

### Performance & Threading

- **Low-Priority Dispatching**: Updates to the neumorphic visuals are queued with `DispatcherQueuePriority.Low`. This prevents the UI thread from hanging when hundreds of controls update simultaneously (e.g., when toggling mode in the Sidebar).
- **Hardware Acceleration**: Shadow rendering is delegated to platform-native APIs which use GPU acceleration.

## Effects Usage

Neumorphic effects are integrated directly into the `DaisyBaseContentControl` class.

### Global Configuration (Opt-In Defaults)

Global settings provide **default values** for controls that don't have local overrides. They are **opt-in**, not forced:

- Controls with explicit local `NeumorphicEnabled`, `NeumorphicMode`, etc. ignore the global settings.
- `DaisyButton` has a **heuristic** that excludes certain buttons from global neumorphic:
  - Transparent variants (Ghost, Link) are excluded
  - Default variant + Default style buttons are excluded (protects sidebars, menus)
- Container controls (`DaisyButtonGroup`, `DaisyJoin`) explicitly disable neumorphic on their children.

**Automatic state synchronization:**

- Setting `GlobalNeumorphicMode` to a non-None value (Raised, Inset, Flat) automatically sets `GlobalNeumorphicEnabled = true`
- Setting `GlobalNeumorphicMode = None` automatically sets `GlobalNeumorphicEnabled = false`
- Setting `GlobalNeumorphicEnabled = true` when Mode was None **restores the last non-None mode** (defaults to Raised)
- The last non-None mode is remembered, so toggling Enabled on/off preserves the user's mode preference

This bidirectional synchronization ensures the dropdown selector works intuitively.

```csharp
// Set mode preference
DaisyBaseContentControl.GlobalNeumorphicMode = DaisyNeumorphicMode.Inset;

// Disable (remembers Inset)
DaisyBaseContentControl.GlobalNeumorphicEnabled = false;  // or Mode = None

// Re-enable (restores Inset, not Raised!)
DaisyBaseContentControl.GlobalNeumorphicEnabled = true;

// Fires GlobalNeumorphicChanged event so controls can refresh
```

### Per-Control Configuration (XAML)

```xml
<daisy:DaisyButton
    Content="Raised Button"
    NeumorphicEnabled="True"
    NeumorphicMode="Raised" />

<daisy:DaisyCard
    NeumorphicEnabled="True"
    NeumorphicMode="Inset"
    NeumorphicIntensity="0.7" />
```

### Available Modes (`DaisyNeumorphicMode`)

| Mode | Style Guide Term | Description |
| :--- | :--- | :--- |
| `None` | — | No effect applied. |
| `Raised` | Outer | Element appears to "pop out" from the surface. Uses platform elevation API. |
| `Inset` | Inner | Element appears "pressed into" the surface. Uses gradient overlays. |
| `Flat` | — | Single subtle shadow (half of Raised elevation). |

**Button Interaction Pattern** (from style guide): For interactive buttons, animate between Raised (normal) and Inset (pressed) states on MouseDown/MouseUp to create a satisfying click feedback.

## 📐 Properties

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `NeumorphicEnabled` | `bool?` | `null` | Enables/disables the effect. Falls back to global setting. |
| `NeumorphicMode` | `DaisyNeumorphicMode?` | `null` | The visual style (Raised, Inset, etc.). Falls back to global. |
| `NeumorphicIntensity` | `double?` | `null` | Adjusts shadow opacity/strength (0.0-1.0). Falls back to global. |
| `NeumorphicDarkShadowColor` | `Color?` | `null` | Override dark shadow color. Falls back to theme-derived global. |
| `NeumorphicLightShadowColor` | `Color?` | `null` | Override light shadow color. Falls back to theme-derived global. |

### Windows-Specific Properties

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `DaisyNeumorphicHelper.UseRoundedShadowMask` | `bool` | `true` | When true, applies rounded rectangle mask to match button corner radius. |

## Developer Notes

### Partial Class Architecture

`DaisyNeumorphicHelper` uses a partial class pattern for platform-specific implementations:

```diagram
Flowery.Uno/Controls/
├── DaisyNeumorphicHelper.cs          # Shared code, partial method declarations
└── DaisyNeumorphicHelper.Windows.cs  # Windows-only (#if WINDOWS), Composition API
```

**Partial Method Hooks**:

```csharp
// In DaisyNeumorphicHelper.cs:
partial void OnApplyDirectElevation(DaisyNeumorphicMode mode, double intensity, double elevation, ref bool handled);
partial void OnDisposeWindowsShadows();

// In DaisyNeumorphicHelper.Windows.cs (#if WINDOWS):
partial void OnApplyDirectElevation(..., ref bool handled)
{
    ApplyWindowsDropShadows(mode, intensity, elevation);
    handled = true; // Skip default SetElevation path
}
```

This pattern isolates platform-specific code without risking regressions on other platforms.

### Theme Integration (Shadow Colors)

Shadow colors are **automatically derived from the theme** when `DaisyThemeManager.ApplyTheme()` is called:

| Theme Type | Dark Shadow | Light Shadow |
| :--- | :--- | :--- |
| **Dark themes** | Pure black `#000000` | Pure white `#FFFFFF` |
| **Light themes** | Darkened Base300 (40%) | Pure white `#FFFFFF` |

The derivation happens in `DaisyThemeManager.UpdateNeumorphicShadowColors()` and updates:

- `DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor`
- `DaisyBaseContentControl.GlobalNeumorphicLightShadowColor`

**Override Chain** (per-element → global):

```csharp
// Per-element via attached property (highest priority)
var darkColor = DaisyNeumorphic.GetDarkShadowColor(element)
    ?? DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor; // Theme-derived fallback
```

### Platform Considerations

**Windows**: Uses Composition API `DropShadow` via partial class (`DaisyNeumorphicHelper.Windows.cs`). Current implementation uses a single dark shadow for reliability. Falls back to standard `SetElevation` path if Composition API fails.

**Desktop (Skia)**: Shadows are clearly visible and scale well with elevation values. Uses single-shadow `SetElevation` approach.

**WASM**: Uses CSS `box-shadow` via `SetElevation`. May need slightly boosted opacity values for visibility.

### Container Controls (DaisyButtonGroup, DaisyJoin)

**IMPORTANT**: Container controls like `DaisyButtonGroup` and `DaisyJoin` are the **single neumorphic surface** - they draw ONE unified shadow/border around all children.

Child controls inside these containers have neumorphic **explicitly disabled** via `DaisyNeumorphic.SetIsEnabled(child, false)`. This prevents:

- Overlapping shadows between adjacent buttons
- Duplicate borders that look wrong
- Visual artifacts from multiple neumorphic effects competing

The children are flat visual segments inside the container's neumorphic shell. The container itself (which inherits from `DaisyBaseContentControl`) provides the neumorphic effect for the whole group.

### Lifecycle Management

The base class automatically manages the creation and disposal of the neumorphic helper during `Loaded` and `Unloaded` events.

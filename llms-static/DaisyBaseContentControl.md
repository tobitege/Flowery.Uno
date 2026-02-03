<!-- Supplementary documentation for DaisyBaseContentControl -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

`DaisyBaseContentControl` is the internal base class for Flowery.Uno components. It centralizes lifecycle management, theme integration, sizing systems, and provides hardware-accelerated visual effects like neumorphism.

Most controls in the library inherit from this class to ensure consistent behavior and high performance across all platforms (WinUI, Android, iOS, WASM).

## Core Systems

### 1. Lifecycle Management

The base class automatically handles `Loaded` and `Unloaded` events. It ensures that event subscriptions (like theme changes) are properly cleaned up to prevent memory leaks, which is critical for long-running cross-platform applications.

### 2. Theme Integration

Subscribes to `DaisyThemeManager.ThemeChanged` automatically. Derived controls can override `OnThemeChanged()` to update their visual state whenever the application theme (e.g., Light, Dark, Dracula) switches.

### 3. Sizing & Scaling

Integrates with `FlowerySizeManager` to support global sizing tiers (XS to XL).

- Automatically applies the global size if no local `Size` is set.
- Respects the `IgnoreGlobalSize` attached property.
- Provides `OnGlobalSizeChanged()` for derived controls to react to layout changes.

### 4. Visual Effects (Neumorphism)

Hosts the hardware-accelerated neumorphic effect engine using the **Composition API**. This creates 3D-like "Soft UI" highlights and shadows without adding expensive `UIElement` layers to the visual tree.

**Deferred Attachment**: Neumorphic effects are attached after the first `LayoutUpdated` event, not immediately in `OnLoaded()`. This ensures the visual tree is fully populated before the composition helper tries to inject its layers, preventing issues where dynamically-built controls (like `DaisyInput`) appear as blank boxes.

See [neumorphic.md](neumorphic.md) for detailed technical implementation.

### 5. Click/Command Support

The base class provides button-like interaction support:

- `Click` event for simple click handling
- `Command` and `CommandParameter` properties for MVVM binding

## Instance Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `NeumorphicEnabled` | `bool?` | `null` | Enables/disables 3D effects. Falls back to global setting. |
| `NeumorphicMode` | `DaisyNeumorphicMode?` | `null` | Style: `Raised`, `Inset`, `Flat`, or `None`. |
| `NeumorphicIntensity` | `double?` | `null` | Adjusts shadow strength (0.0 to 1.0). |
| `NeumorphicBlurRadius` | `double` | `12.0` | Softness of the effect shadows. |
| `NeumorphicOffset` | `double` | `4.0` | Distance of the 3D effect from the element. |
| `NeumorphicDarkShadowColor` | `Color?` | `null` | Override dark shadow color. Falls back to global. |
| `NeumorphicLightShadowColor` | `Color?` | `null` | Override light shadow color. Falls back to global. |
| `NeumorphicRimLightEnabled` | `bool?` | `null` | Enable rim light effect. Requires global to also be enabled. |
| `NeumorphicSurfaceGradientEnabled` | `bool?` | `null` | Enable surface gradient. Requires global to also be enabled. |
| `Command` | `ICommand?` | `null` | Command to invoke on click. |
| `CommandParameter` | `object?` | `null` | Parameter passed to Command. |

## Scope Overrides

Use the `NeumorphicScopeEnabled` attached property to enable or disable neumorphic effects for a subtree.
This is useful for apps like the Gallery where only certain sections should opt into the global toggle.

```xml
<!-- Disable for the entire shell -->
<Grid daisy:DaisyBaseContentControl.NeumorphicScopeEnabled="False">
    <!-- Re-enable only for the examples area -->
    <Grid x:Name="MainContentHost"
          daisy:DaisyBaseContentControl.NeumorphicScopeEnabled="True" />
</Grid>
```

## Static Configuration (Global)

Configure library-wide defaults via static properties on `DaisyBaseContentControl`:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `GlobalNeumorphicEnabled` | `bool` | `false` | Enables neumorphic effects for all inheriting controls. |
| `GlobalNeumorphicMode` | `DaisyNeumorphicMode` | `None` | Default 3D style for the library. |
| `GlobalNeumorphicIntensity` | `double` | `0.45` | Default shadow strength (0.0 to 1.0). |
| `GlobalNeumorphicRimLightEnabled` | `bool` | `false` | Enables rim light effect globally. |
| `GlobalNeumorphicSurfaceGradientEnabled` | `bool` | `false` | Enables surface gradient effect globally. |
| `GlobalNeumorphicDarkShadowColor` | `Color` | `#0D2750` | Default dark shadow color. |
| `GlobalNeumorphicLightShadowColor` | `Color` | `#FFFFFF` | Default light shadow color. |
| `EnableDebugOverlay` | `bool` | `false` | Enables debug visual overlays (if supported by control). |

### Automatic State Synchronization

The global neumorphic properties have automatic state synchronization:

- Setting `GlobalNeumorphicMode` to a non-None value (Raised, Inset, Flat) automatically sets `GlobalNeumorphicEnabled = true`
- Setting `GlobalNeumorphicMode = None` automatically sets `GlobalNeumorphicEnabled = false`
- Setting `GlobalNeumorphicEnabled = true` when Mode was None **restores the last non-None mode** (defaults to Raised)
- The last non-None mode is remembered, so toggling Enabled on/off preserves the user's mode preference

This bidirectional synchronization ensures the dropdown selector works intuitively - selecting "Raised" immediately enables the effect.

### Transparent Control Heuristic

When picking up from the global flag (no local or scope override), transparent controls are automatically excluded from neumorphic effects. This prevents structural containers (shells, mockups, grids) from creating massive opaque surfaces.

### Events

| Event | Description |
| --- | --- |
| `GlobalNeumorphicChanged` | Fired when any global neumorphic property changes. |
| `Click` | Fired when the control is clicked. |

```csharp
// Example: Enable neumorphism globally
DaisyBaseContentControl.GlobalNeumorphicEnabled = true;
DaisyBaseContentControl.GlobalNeumorphicMode = DaisyNeumorphicMode.Raised;

// Advanced effects (opt-in)
DaisyBaseContentControl.GlobalNeumorphicRimLightEnabled = true;
DaisyBaseContentControl.GlobalNeumorphicSurfaceGradientEnabled = true;
```

## Implementation Guide (for Contributors)

When creating a new control that inherits from `DaisyBaseContentControl`:

### Required Overrides

To participate in the theme and sizing systems, implement these hooks:

```csharp
protected override void OnThemeChanged(string themeName) => ApplyAll();

protected override void OnGlobalSizeChanged(DaisySize size) => Size = size;

protected override DaisySize? GetControlSize() => Size;

protected override void SetControlSize(DaisySize size) => Size = size;
```

### Visual Tree Construction

Use the provided protected helpers to keep code-behind clean:

- `CreateThemedBorder()` (static): Creates a standard border with theme-aware colors.
- `ApplyHoverEffect()`: Sets up PointerEntered/Exited background switching.
- `WrapWithPadding()` (static): Quick utility for layout nesting. It creates a `Grid` containing a `Border` with the specified `Padding`, which then hosts the provided `UIElement`. This is useful for adding spacing around a control's internal parts without modifying the parts themselves.

#### Example: WrapWithPadding Usage

```csharp
// Inside a control's visual tree construction
var myInnerPart = new TextBlock { Text = "Padded Content" };
var paddedContainer = WrapWithPadding(myInnerPart, new Thickness(12));
// paddedContainer is now a Grid -> Border -> TextBlock
```

### Debugging

> [!IMPORTANT]
> Always call `base.OnLoaded()` and `base.OnUnloaded()` if you override these events, as they contain the logic for building and disposing composition visuals.

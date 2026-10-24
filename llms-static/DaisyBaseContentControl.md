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

- Resolves `Size="Auto"` through the current global or composite default without writing a local Size value.
- Exposes the effective concrete tier through the read-only `ResolvedSize` property.
- Respects the `IgnoreGlobalSize` attached property.
- Provides `OnResolvedSizeChanged()` for derived controls to refresh layout.
- Registers while loaded and catches up after reattachment. Explicit sizes, styles and bindings retain their precedence.

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
| `Command` | `ICommand?` | `null` | Command to invoke on click. |
| `CommandParameter` | `object?` | `null` | Parameter passed to Command. |

## Scope Overrides

Use the `NeumorphicScopeEnabled` attached property to enable or disable neumorphic effects for a subtree.
This is useful for apps like the Gallery where only certain sections should opt into the global toggle.
The nearest scope value wins over a control's own `NeumorphicEnabled`.

```xml
<!-- Disable for the entire shell -->
<Grid daisy:DaisyBaseContentControl.NeumorphicScopeEnabled="False">
    <!-- Re-enable only for the examples area -->
    <Grid x:Name="MainContentHost"
          daisy:DaisyBaseContentControl.NeumorphicScopeEnabled="True" />
</Grid>
```

`NeumorphicScopeMode` and `NeumorphicScopeIntensity` set the mode and intensity for a subtree.
The nearest scope value wins over the global default; a control's own `NeumorphicMode` or
`NeumorphicIntensity` still takes precedence.

```xml
<!-- Everything in here is inset at 0.6, without touching each child -->
<StackPanel daisy:DaisyBaseContentControl.NeumorphicScopeEnabled="True"
            daisy:DaisyBaseContentControl.NeumorphicScopeMode="Inset"
            daisy:DaisyBaseContentControl.NeumorphicScopeIntensity="0.6">
    <daisy:DaisyCard />
    <!-- Own value wins -->
    <daisy:DaisyCard NeumorphicMode="Raised" />
</StackPanel>
```

A scope never enables rows, cells or segments that their container keeps flat (see Structural Exclusions).

## Static Configuration (Global)

Configure library-wide defaults via static properties on `DaisyBaseContentControl`:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `GlobalNeumorphicEnabled` | `bool` | `false` | Enables neumorphic effects for all inheriting controls. |
| `GlobalNeumorphicMode` | `DaisyNeumorphicMode` | `None` | Default 3D style for the library. |
| `GlobalNeumorphicIntensity` | `double` | `0.45` | Default shadow strength (0.0 to 1.0). |
| `GlobalNeumorphicDarkShadowColor` | `Color?` | `null` | Default dark shadow color. `null` derives it from the current theme (Base300, darkened). A value set here survives theme changes. |
| `GlobalNeumorphicLightShadowColor` | `Color?` | `null` | Default light shadow color. `null` uses white. A value set here survives theme changes. |
| `EnableDebugOverlay` | `bool` | `false` | Enables debug visual overlays (if supported by control). |

### Automatic State Synchronization

The global neumorphic properties have automatic state synchronization:

- Setting `GlobalNeumorphicMode` to a non-None value (Raised, Inset, Flat) automatically sets `GlobalNeumorphicEnabled = true`
- Setting `GlobalNeumorphicMode = None` automatically sets `GlobalNeumorphicEnabled = false`
- Setting `GlobalNeumorphicEnabled = true` when Mode was None **restores the last non-None mode** (defaults to Raised)
- The last non-None mode is remembered, so toggling Enabled on/off preserves the user's mode preference

This bidirectional synchronization ensures the dropdown selector works intuitively - selecting "Raised" immediately enables the effect.

### Controls Without a Visible Surface

A control that is its own neumorphic host (it overrides neither `GetNeumorphicHostElement` nor
`GetNeumorphicSurfaces`) and paints no visible background (no brush, or a fully transparent one) draws nothing.
This keeps controls without a surface of their own (`DaisyLoading`, `DaisyIndicator`, `DaisyStack`,
`DaisyTextRotate`, `DaisyAnimatedNumber`, plain layout containers) flat in global mode instead of giving
them a raised box. A surface the control declares is drawn even without a background of its own: Raised and
Flat put a Base200 surface behind it. The effect re-applies when the host's background or visibility changes.

### Structural Exclusions

These rules cannot be overridden by `NeumorphicEnabled` or a scope:

- Children of segment containers (`DaisyJoin`, `DaisyButtonGroup`) stay flat; the container is the one surface.
- `DaisyTableRow`, `DaisyTableCell`, `DaisyListRow` and `DaisyListColumn` stay flat; the table or list is the one surface.
- `DaisyButton`s without an opaque face (Ghost, Link, Outline, Dash, Soft) draw nothing; the shadows would show through the face. Changing `Variant` or `ButtonStyle` re-evaluates this.

### Events

| Event | Description |
| --- | --- |
| `GlobalNeumorphicChanged` | Fired when any global neumorphic property changes. |
| `Click` | Fired when the control is clicked. |

```csharp
// Example: Enable neumorphism globally
DaisyBaseContentControl.GlobalNeumorphicEnabled = true;
DaisyBaseContentControl.GlobalNeumorphicMode = DaisyNeumorphicMode.Raised;

// Optional: a fixed dark shadow color; null follows the theme again
DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor = Color.FromArgb(255, 40, 40, 48);
```

## Implementation Guide (for Contributors)

When creating a new control that inherits from `DaisyBaseContentControl`:

### Required Overrides

To participate in the theme and sizing systems, implement these hooks:

```csharp
protected override void OnThemeChanged(string themeName) => ApplyAll();

protected override void OnResolvedSizeChanged()
{
    base.OnResolvedSizeChanged();
    ApplyAll();
}

protected override DaisySize? GetControlSize() => Size;
```

Register the control's Size dependency property with `DaisySize.Auto` metadata. Use `ResolvedSize`
for layout and token lookups. The Size property callback must also refresh layout. Override
`DefaultSize` only for a fallback other than Medium. Do not write Size from the refresh hook.
See [FlowerySizeManager](FlowerySizeManager.md) for the complete precedence and migration contract.

### Neumorphic Surfaces

By default the control itself is the neumorphic surface. Override the hooks below when another element
renders the visible surface, or when the control should stay flat:

```csharp
// One surface on an inner border (cards, inputs, badges, ...)
protected override FrameworkElement? GetNeumorphicHostElement() => _rootBorder ?? base.GetNeumorphicHostElement();

// Several surfaces, optionally with a fixed mode (e.g. inset track, raised handle)
protected override IReadOnlyList<DaisyNeumorphicSurface> GetNeumorphicSurfaces() =>
    _track == null || _handle == null
        ? []
        : [new(_track, DaisyNeumorphicMode.Inset), new(_handle, DaisyNeumorphicMode.Raised)];

// Parts of a larger surface (rows, cells) never draw the effect
protected override bool IsNeumorphicEffectivelyEnabled() => false;

// Appearance details that depend on whether the effect is drawn
protected override void OnNeumorphicRefreshed(bool isEnabled) { /* e.g. transparent track */ }
```

A declared surface draws even when its host paints no background: Raised and Flat put a Base200 surface behind it,
Inset draws on the transparent host (toggle track, checkbox box). Hidden hosts draw nothing. A host is a `Border`, `Panel` or `Control`; for an inset layer below the
host's content, the host needs a `Panel` child (or be one), and content that must stay above the layer needs
`Canvas.ZIndex="2"`. Surfaces that move with a `RenderTransform` keep the transform on a wrapper `Grid`, so the
shadow layer injected next to them moves along.

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

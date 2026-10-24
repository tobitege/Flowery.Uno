<!-- Supplementary documentation for DaisyDock -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyDock arranges items in a pill-shaped bar, similar to a macOS dock. It supports **5 sizes** via the standard `DaisySize` enum, responds to global size changes via `FlowerySizeManager`, and raises an `ItemSelected` event when an item is clicked. Commonly used for app bars or compact nav strips.

## Properties & Events

| Member | Description |
| --- | --- |
| `Size` (`DaisySize`, default `Auto`) | Adjusts dock item size and spacing. Supplies the default tier for automatic children; explicit child tiers remain unchanged. `ResolvedSize` exposes the concrete tier from [global sizing](FlowerySizeManager.md). |
| `AutoSelect` (bool, default `True`) | When true, clicking an item highlights it with the Primary color and deselects siblings. |
| `SelectedIndex` (int, default `-1`) | Gets or sets the index of the currently selected item. |
| `ItemSelected` event | Raised with `DockItemSelectedEventArgs` containing the clicked `UIElement` and its index. |

## Icon and Item Sizing

For `DaisyIconText` children, an unset or `NaN` `IconSize` uses the resolved tier's `DaisyDock{Size}ItemSize` token multiplied by `DaisyDockIconSizeRatio`. Explicit `IconSize` values from local properties, styles, and bindings remain unchanged. Clearing an override or returning it to `NaN` resumes automatic icon sizing.

A child's explicit `Size` takes precedence over the dock tier. Changes to a child `Size` binding update that item's footprint immediately, even when the dock and global sizes do not change. When an icon moves outside the dock, it returns to the standard `DaisyIconText` automatic icon size. Reloading the dock restores its defaults and child-size tracking.

## Quick Examples

```xml
<!-- Default dock with DaisyIconText for proper scaling -->
<daisy:DaisyDock Size="Medium" AutoSelect="True" ItemSelected="Dock_ItemSelected">
    <StackPanel Orientation="Horizontal">
        <daisy:DaisyIconText IconData="{StaticResource DaisyIconHome}" />
        <daisy:DaisyIconText IconData="{StaticResource DaisyIconUser}" />
        <daisy:DaisyIconText IconData="{StaticResource DaisyIconSettings}" />
    </StackPanel>
</daisy:DaisyDock>

<!-- Small dock -->
<daisy:DaisyDock Size="Small">
    <StackPanel Orientation="Horizontal">
        <daisy:DaisyIconText IconData="{StaticResource DaisyIconHome}" />
        <daisy:DaisyIconText IconData="{StaticResource DaisyIconSettings}" />
    </StackPanel>
</daisy:DaisyDock>

<!-- Manual selection with SelectedIndex -->
<daisy:DaisyDock AutoSelect="False" SelectedIndex="{Binding CurrentTab}">
    <StackPanel Orientation="Horizontal">
        <daisy:DaisyIconText IconSymbol="Home" />
        <daisy:DaisyIconText IconSymbol="Document" />
        <daisy:DaisyIconText IconSymbol="Setting" />
    </StackPanel>
</daisy:DaisyDock>
```

## Tips & Best Practices

- **Use `DaisyIconText` for dock items.** It uses Viewbox internally which ensures icons scale correctly when the dock size changes.
- Automatic dock items follow global sizing. An explicit dock tier supplies their default, while an explicit child tier takes precedence.
- Use `ItemSelected` event or bind `SelectedIndex` to route clicks and update navigation state.
- For manual selection control, set `AutoSelect="False"` and manage `SelectedIndex` yourself.
- Center the dock via parent layout; it ships with rounded corners, border, and neutral background by default.

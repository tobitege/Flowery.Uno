<!-- Supplementary documentation for DaisyIconText -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyIconText is a **single-purpose display control** that combines an icon (path data or Symbol) with optional text, handling all the scaling mechanics under the hood. It wraps icons in a `Viewbox` for proper proportional scaling and applies size-based font sizes for text - eliminating the need to manually manage `Width`/`Height` or `FontSize` math.

Use it directly in any UI, or as the `Content` of buttons, menu items, tabs, list items, and other containers.

## When to Use

| Scenario | Use DaisyIconText |
| --- | --- |
| Icon-only label | `<daisy:DaisyIconText IconSymbol="Home" Size="Large"/>` |
| Icon + text pair | `<daisy:DaisyIconText IconData="{StaticResource IconSave}" Text="Save" IconPlacement="Left"/>` |
| Text-only with auto-sizing | `<daisy:DaisyIconText Text="Settings" Size="Small"/>` |
| Consistent scaling in zoomed layouts | Place inside a `ScaleTransform` container - the Viewbox handles proportional icon scaling automatically |

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `IconData` | `string` | `null` | Path data string for a custom icon (24×24 coordinate system). Takes precedence over `IconSymbol`. |
| `IconSymbol` | `Symbol?` | `null` | A Windows Symbol enum value (e.g., `Symbol.Home`, `Symbol.Save`). Used when `IconData` is not set. |
| `Text` | `string` | `null` | Text to display alongside or instead of the icon. |
| `Size` | `DaisySize` | `Medium` | Controls both icon dimensions and font size. Follows global size if `FlowerySizeManager` is active. |
| `IconPlacement` | `IconPlacement` | `Left` | Position of the icon relative to text: `Left`, `Right`, `Top`, or `Bottom`. |
| `Spacing` | `double` | `NaN` (auto) | Gap between icon and text. If `NaN`, auto-computed based on `Size`. |
| `IconSize` | `double` | `NaN` (auto) | Explicit icon size override. If `NaN`, auto-computed based on `Size`. |
| `FontSizeOverride` | `double` | `NaN` (auto) | Explicit font size override. If `NaN`, auto-computed based on `Size`. |
| `FontWeight` | `FontWeight` | `Normal` | Font weight for the text (e.g., `Normal`, `SemiBold`, `Bold`). |

## Size Presets

| Size | Icon Size | Font Size | Spacing |
| --- | --- | --- | --- |
| ExtraSmall | 12px | 10px | 4px |
| Small | 14px | 12px | 6px |
| Medium | 16px | 14px | 8px |
| Large | 20px | 16px | 10px |
| ExtraLarge | 24px | 18px | 12px |

> [!NOTE]
> These values come from `DaisyResourceLookup` and may vary slightly by theme. The control automatically responds to `FlowerySizeManager.SizeChanged` for global size changes, unless `IgnoreGlobalSize="True"` is set on the control or an ancestor.

## Quick Examples

```xml
<!-- Icon only -->
<controls:DaisyIconText IconSymbol="Home" Size="Medium"/>

<!-- Icon from path data -->
<controls:DaisyIconText IconData="{StaticResource DaisyIconSettings}" Size="Large"/>

<!-- Text only -->
<controls:DaisyIconText Text="Hello World" Size="Small"/>

<!-- Icon + Text (icon on left) -->
<controls:DaisyIconText IconSymbol="Save" Text="Save Document" IconPlacement="Left"/>

<!-- Icon + Text (icon on right) -->
<controls:DaisyIconText IconSymbol="ChevronRight" Text="Next" IconPlacement="Right"/>

<!-- Icon above text -->
<controls:DaisyIconText IconSymbol="Mail" Text="Inbox" IconPlacement="Top"/>

<!-- Custom spacing -->
<controls:DaisyIconText IconSymbol="Edit" Text="Edit" Spacing="16"/>

<!-- Override icon size -->
<controls:DaisyIconText IconData="{StaticResource BigLogo}" IconSize="48"/>
```

## Using Inside Other Controls

DaisyIconText works seamlessly as content for buttons and other containers:

```xml
<!-- As DaisyButton content -->
<controls:DaisyButton Variant="Primary">
    <controls:DaisyIconText IconSymbol="Add" Text="New Item" IconPlacement="Left"/>
</controls:DaisyButton>

<!-- In a menu item -->
<MenuFlyoutItem>
    <MenuFlyoutItem.Icon>
        <controls:DaisyIconText IconSymbol="Settings"/>
    </MenuFlyoutItem.Icon>
    <controls:DaisyIconText Text="Settings"/>
</MenuFlyoutItem>

<!-- In a tab header -->
<controls:DaisyTab>
    <controls:DaisyTab.Header>
        <controls:DaisyIconText IconSymbol="Document" Text="Documents" IconPlacement="Left"/>
    </controls:DaisyTab.Header>
</controls:DaisyTab>
```

## Why Viewbox-Based Scaling?

Standard `PathIcon` and `SymbolIcon` controls don't scale their content proportionally when you constrain their `Width`/`Height` - the path data gets clipped instead. DaisyIconText wraps icons in a `Viewbox` which:

1. **Scales proportionally** - The entire icon shrinks or grows uniformly
2. **Maintains aspect ratio** - No distortion at any size
3. **Works with ScaleTransform** - Nested scaling multiplies correctly
4. **Handles any path data** - Whether designed for 24×24 or any other coordinate system

## Tips & Best Practices

- **Prefer `IconData` over `IconSymbol`** when you have custom SVG paths - it gives you more design flexibility.
- **Use `Size` for consistency** - Rely on the size presets rather than overriding `IconSize`/`FontSizeOverride` unless you need precise control.
- **Combine with DaisyButton** - For icon buttons, either use `DaisyButton.IconSymbol`/`IconData` properties directly, or place a `DaisyIconText` as content for complex layouts.
- **Inherit from theme** - `DaisyIconText` inherits its `Foreground` from the parent, so it works with variant colors automatically.
- **Spacing matters** - The default auto-spacing is tuned per size. Only override `Spacing` if you need tighter or looser layouts.

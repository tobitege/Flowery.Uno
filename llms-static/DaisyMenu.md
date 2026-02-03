<!-- Supplementary documentation for DaisyMenu -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyMenu is a styled menu control for navigation menus. It supports vertical or horizontal layout, size presets, and customizable active colors for selected items. Submenus can be nested via another DaisyMenu or Expander. Use it for sidebars, nav bars, and contextual menus that need DaisyUI styling.

## Orientation & Sizes

| Option | Description |
| --- | --- |
| `Orientation` | `Vertical` (default) or `Horizontal`. Horizontal enables horizontal scrolling if needed. |
| `Size` | ExtraSmall, Small, Medium (default), Large, ExtraLarge adjust padding/font on items. |

## Active Styling

| Property | Description |
| --- | --- |
| `ActiveForeground` / `ActiveBackground` | Colors applied to selected items; defaults to Neutral theme colors. |
| Selection | Inherits ListBox selection; `SelectedItem`/`SelectedIndex` works as usual. |

## Item Interaction

| Property | Description |
| --- | --- |
| `Click` | DaisyMenuItem raises a click event and supports keyboard activation. |
| `Command` / `CommandParameter` | DaisyMenuItem inherits the base command pattern; use these for MVVM. |

### Hover Styling

To override hover background on menu items, define:

```txt
DaisyMenuItemHoverBackground
```

## Quick Examples

```xml
<!-- Vertical menu -->
<controls:DaisyMenu Width="200">
    <controls:DaisyMenuItem Content="Dashboard" IsActive="True" />
    <controls:DaisyMenuItem Content="Profile" />
    <controls:DaisyMenuItem Content="Settings" />
</controls:DaisyMenu>

<!-- Horizontal nav -->
<controls:DaisyMenu Orientation="Horizontal" Size="Small">
    <controls:DaisyMenuItem>Home</controls:DaisyMenuItem>
    <controls:DaisyMenuItem>About</controls:DaisyMenuItem>
    <controls:DaisyMenuItem>Contact</controls:DaisyMenuItem>
</controls:DaisyMenu>

<!-- Nested submenu -->
<controls:DaisyMenu>
    <controls:DaisyMenuItem Content="Main" />
    <controls:DaisyMenuItem IsActive="True">Overview</controls:DaisyMenuItem>
    <Expander Header="More">
        <controls:DaisyMenu>
            <controls:DaisyMenuItem>Subitem A</controls:DaisyMenuItem>
            <controls:DaisyMenuItem>Subitem B</controls:DaisyMenuItem>
        </controls:DaisyMenu>
    </Expander>
</controls:DaisyMenu>

<!-- Checked item (toggle-style menu entry) -->
<controls:DaisyMenu>
    <controls:DaisyMenuItem Content="Resize columns" IsChecked="True" />
</controls:DaisyMenu>
```

## Tips & Best Practices

- Use size presets to match surrounding controls; Small/ExtraSmall pair well with toolbars.
- Set `ActiveForeground/ActiveBackground` to align with your brand colors if Neutral isn't desired.
- For horizontal menus, ensure there's enough padding/margin in the parent to avoid crowding.
- Use `Classes="menu-title"` for non-interactive section headers inside the menu.
- Nest DaisyMenu or use `Expander` for collapsible submenus; adjust margins for indentation as needed.
- Use `IsChecked` for toggle-like items so all menu rows keep the same layout.

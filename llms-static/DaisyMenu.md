<!-- Supplementary documentation for DaisyMenu -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyMenu arranges DaisyMenuItem controls vertically or horizontally. It supports size presets, active colors, and non-interactive section titles.
Its Content must contain a panel with the items. The menu moves those items into its internal panel on first load.

## Properties and Interaction

| Member | Description |
| --- | --- |
| `Orientation` | Vertical (default) or Horizontal. |
| `Size` | Auto (default), ExtraSmall, Small, Medium, Large, or ExtraLarge. |
| `ResolvedSize` | The concrete tier from the control, containing scope, or global size. |
| `ActiveForeground` / `ActiveBackground` | Colors for active items. Defaults use PrimaryContent and Primary. |
| `DaisyMenuItem.IsActive` | Marks an item active. Activation clears the other items' active state. |
| `DaisyMenuItem.IsDisabled` | Prevents activation and keyboard focus. Default text uses `DaisyBaseContentDisabledBrush`. |
| `DaisyMenuItem.IsTitle` | Shows a non-interactive section title with Primary text, bold font, and a Base300 bottom divider. |
| `ItemInvoking` / `ItemInvoked` | Events on the menu before and after an item invokes its base command and click behavior. |
| `Command` / `CommandParameter` | Command properties on each item. |

Explicit sizes and bindings retain normal precedence. See [size precedence](FlowerySizeManager.md#size-ownership-and-precedence).

Titles cannot activate through pointer input, Enter, Space, or commands. They receive no selection indicator, even with `IsActive=True`.
Changing `IsTitle` at runtime restores the normal row behavior without replacing its content.
Disabled rows use a text brush instead of opacity on the entire row. Authored child visuals retain their own foreground and opacity.

## Resource Overrides

`DaisyMenuItemForeground` overrides normal, disabled, and title text.
`DaisyMenuItemActiveForeground`, `DaisyMenuItemActiveBackground`, and `DaisyMenuItemHoverBackground` retain their existing behavior.
The menu's explicit active brushes take precedence over the active resources.

## Example

```xml
<controls:DaisyMenu Width="200">
    <StackPanel>
        <controls:DaisyMenuItem Content="Workspace" IsTitle="True" />
        <controls:DaisyMenuItem Content="Dashboard" IsActive="True" />
        <controls:DaisyMenuItem Content="Profile" />
        <controls:DaisyMenuItem Content="Administration" IsTitle="True" />
        <controls:DaisyMenuItem Content="Settings" />
        <controls:DaisyMenuItem Content="Unavailable" IsDisabled="True" />
    </StackPanel>
</controls:DaisyMenu>
```

The Uno API uses `IsTitle=True`. Avalonia's `Classes="menu-title"` syntax does not apply.

## Keyboard Mnemonics

String content shows WinForms-style mnemonics. A single `&` underlines the next character, and `&&` shows a literal `&`.
The automation name contains the display text. The application handles Alt+key activation.
See [DaisyMnemonicText](DaisyMnemonicText.md).

```xml
<controls:DaisyMenuItem Content="&amp;File" />
<controls:DaisyMenuItem Content="Save &amp;&amp; Close" />
```

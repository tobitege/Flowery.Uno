<!-- Supplementary documentation for DaisyThemeController -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyThemeController is a toggle-based switch for applying Daisy themes via `DaisyThemeManager`. It supports multiple display modes (toggle, checkbox, swap, text/icon variants) and syncs `IsChecked` with the current theme. Switching updates `CheckedTheme`/`UncheckedTheme` and can auto-adopt new themes.

## How Theming Works

This control uses `DaisyThemeManager` internally, which manages the application's theme resources:

- Each of the 35+ built-in DaisyUI themes provides a unique color palette
- 96 additional industry-specific product themes are available via `DaisyProductThemeDropdown`
- Theme changes are applied through resource dictionary updates
- For custom themes beyond the built-in set, use `DaisyThemeLoader.ApplyThemeToApplication()` instead

## Properties

| Property | Description |
| --- | --- |
| `Mode` | Visual mode: Toggle, Checkbox, Swap, ToggleWithText, ToggleWithIcons. |
| `Size` | Checkbox size tier: `Auto` (default) or ExtraSmall through ExtraLarge. Concrete local values, styles and bindings take precedence over automatic sizing. |
| `ResolvedSize` | Read-only effective tier from the authored `Size`, containing size scope, global policy or Medium fallback. See [FlowerySizeManager](FlowerySizeManager.md). |
| `UncheckedLabel` / `CheckedLabel` | Labels for light/dark (or custom) modes. |
| `UncheckedTheme` / `CheckedTheme` | Theme names to apply on off/on states (defaults: Light/Dark). |

## Behavior

- Toggling applies the target theme via `DaisyThemeManager.ApplyTheme(...)`.
- Subscribes to `DaisyThemeManager.ThemeChanged` to sync `IsChecked` when theme changes externally.
- When a new theme is applied that isn't the unchecked theme, `CheckedTheme`/`CheckedLabel` update to that theme name.
- Checkbox mode uses the `DaisyCheckbox{tier}Size` and `DaisyCheckmark{tier}Size` tokens inside a transparent `DaisySize{tier}Height` hit area. Global changes, theme refreshes and reloads update these internal elements without writing `Size` or outer dimensions.
- `Size` affects only Checkbox mode. Toggle, Swap and text/icon toggle geometry remains fixed. ToggleWithText labels use the native `FontSize`, including local, style and binding values.

## Quick Examples

In your `.xaml` file (e.g., `MainWindow.xaml`), add the namespace and control:

```xml
<!-- Add at top of file -->
xmlns:controls="using:Flowery.Controls"

<!-- Simple toggle - just drop it in, defaults to Light/Dark -->
<controls:DaisyThemeController Mode="Toggle" />

<!-- Checkbox follows automatic sizing; an explicit tier overrides it -->
<controls:DaisyThemeController Mode="Checkbox" />
<controls:DaisyThemeController Mode="Checkbox" Size="Small" />

<!-- Animated sun/moon swap (as used in Gallery) -->
<controls:DaisyThemeController Mode="Swap" />

<!-- Custom theme pairing -->
<controls:DaisyThemeController Mode="ToggleWithText"
    UncheckedTheme="Light" CheckedTheme="Synthwave"
    UncheckedLabel="Light" CheckedLabel="Synthwave" />
```

**Prerequisite**: Your `App.xaml` must include Flowery.Uno's theme resources in `Application.Resources.MergedDictionaries`:

```xml
<ResourceDictionary Source="ms-appx:///Flowery.Uno/Themes/Generic.xaml" />
```

## Tips & Best Practices

- Keep `UncheckedTheme` aligned with your base theme so `IsChecked=False` reflects the default look.
- If you dynamically load themes, let the controller auto-update `CheckedTheme` to the latest applied theme.
- Choose Mode to match UI density: Swap/ToggleWithIcons for compact, ToggleWithText for clarity.
- Bind `IsChecked` if you need to track theme state elsewhere; the controller will still apply themes.
- For selection from all themes, pair with `DaisyThemeDropdown` (DaisyUI themes) or `DaisyProductThemeDropdown` (96 industry themes).

## Related Controls

- [DaisyThemeDropdown](DaisyThemeDropdown.md) - Dropdown for all 35+ DaisyUI themes
- [DaisyProductThemeDropdown](DaisyProductThemeDropdown.md) - Dropdown for 96 industry-specific product themes
- [DaisyThemeRadio](DaisyThemeRadio.md) - Radio button theme selection
- [DaisyThemeSwap](DaisyThemeSwap.md) - Icon swap theme toggle
- [DaisyThemeManager](DaisyThemeManager.md) - Central theme management

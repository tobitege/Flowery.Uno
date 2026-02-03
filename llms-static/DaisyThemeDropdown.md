<!-- Supplementary documentation for DaisyThemeDropdown -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyThemeDropdown is a ComboBox listing available themes from `DaisyThemeManager`. It previews theme colors in a 2×2 dot grid and applies the selected theme. It syncs selection with the current theme when themes change externally.

## How Theming Works

This control uses `DaisyThemeManager` internally, which manages the application's theme resources:

- Each of the 35+ built-in themes provides a unique color palette
- Theme changes are applied through resource dictionary updates
- For custom themes beyond the built-in set, use `DaisyThemeLoader.ApplyThemeToApplication()` instead

## Properties & Behavior

| Property | Description |
| --- | --- |
| `SelectedTheme` | Name of the currently selected theme. Setting this applies the theme. |
| ItemsSource | Auto-populated from `DaisyThemeManager.AvailableThemes` with preview brushes. |
| Sync | Subscribes to `ThemeChanged` to update selection when themes change elsewhere. |

## Initialization Behavior

The dropdown automatically syncs to the current theme during construction:

- If `DaisyThemeManager.CurrentThemeName` is already set (e.g., app restored theme from settings), the dropdown syncs to that theme **without re-applying it**.
- If no theme is set yet, the dropdown defaults to "Dark" **without triggering `ApplyTheme`**.

This ensures apps can restore persisted theme preferences before constructing UI controls without worrying about dropdowns overriding the saved theme.

## Quick Examples

In your `.xaml` file (e.g., `MainWindow.xaml`), add the namespace and control:

```xml
<!-- Add at top of file -->
xmlns:controls="using:Flowery.Controls"

<!-- Default theme dropdown - shows all 35+ themes with color previews -->
<controls:DaisyThemeDropdown Width="220" />

<!-- Binding selected theme to ViewModel -->
<controls:DaisyThemeDropdown SelectedTheme="{Binding CurrentTheme, Mode=TwoWay}" />
```

**Prerequisite**: Your `App.xaml` must include Flowery.Uno's theme resources in `Application.Resources.MergedDictionaries`:

```xml
<ResourceDictionary Source="ms-appx:///Flowery.Uno/Themes/Generic.xaml" />
```

## Automatic Sizing

The dropdown automatically calculates dimensions based on the `Size` property:

### MinWidth Calculation

`MinWidth` is computed to ensure all content fits:

- **Color swatches**: 5 dots using `DaisyResourceLookup.GetDefaultSwatchSize(Size)` design token
- **Theme name text**: ~60px for typical theme names
- **Dropdown glyph**: ~26px for the arrow button

This prevents the dropdown arrow from being clipped, especially at smaller sizes (ExtraSmall, Small).

### Swatch Scaling

The 5 color preview dots scale with the control size:

| Size | Swatch Diameter |
| --- | --- |
| ExtraSmall | 6px |
| Small | 8px |
| Medium | 10px |
| Large | 12px |
| ExtraLarge | 14px |

> **Note:** Setting `Width` below the computed `MinWidth` will be overridden. If you need a narrower dropdown, also set `MinWidth` explicitly (may cause visual clipping).

## Tips & Best Practices

- Use alongside `DaisyThemeController` for quick toggle + full list selection.
- Ensure theme palette resources (`DaisyBase100Brush`, etc.) are present for accurate previews.
- Set explicit width if you have long theme names; the popup inherits min width from the template (200px).
- For industry-specific themes (SaaS, Fintech, Healthcare, etc.), use [DaisyProductThemeDropdown](DaisyProductThemeDropdown.md) which provides 96 additional product palettes.

## Related Controls

- [DaisyProductThemeDropdown](DaisyProductThemeDropdown.md) - Dropdown for 96 industry-specific product themes
- [DaisyThemeController](DaisyThemeController.md) - Toggle-based theme switching
- [DaisyThemeRadio](DaisyThemeRadio.md) - Radio button theme selection
- [DaisyThemeSwap](DaisyThemeSwap.md) - Icon swap theme toggle
- [DaisyThemeManager](DaisyThemeManager.md) - Central theme management

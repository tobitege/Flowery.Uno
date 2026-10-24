# DaisySizeDropdown

A dropdown control for selecting a global size (`DaisySize`) that affects (almost) all Flowery.Uno controls.

## Overview

`DaisySizeDropdown` lets users choose their preferred UI size. The selection is applied globally via `FlowerySizeManager`. Controls using automatic sizing update; explicit sizes and bindings are preserved. See [size precedence](FlowerySizeManager.md#size-ownership-and-precedence).

## Basic Usage

```xml
xmlns:controls="using:Flowery.Controls"

<controls:DaisySizeDropdown Width="180" />
```

This displays a dropdown with all five sizes (ExtraSmall, Small, Medium, Large, ExtraLarge) using localized display names.

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `SelectedSize` | `DaisySize` | `Medium` | The currently selected size |
| `Size` | `DaisySize` | `Auto` | Requested size of the dropdown itself; `ResolvedSize` provides the concrete tier |
| `ShowAbbreviations` | `bool` | `false` | Show abbreviations (XS, S, M, L, XL) instead of full names |
| `SizeOptions` | `System.Collections.IEnumerable?` | `null` | Custom options containing `SizePreviewInfo` objects |

---

## Customizing Available Sizes

By default, all five sizes are shown. You can customize which sizes appear and what they're called using the `SizeOptions` property.

Only the five concrete sizes can be selected as a global size. Options with `Auto` or an undefined enum value are excluded.
An empty collection or a collection without visible concrete sizes uses the five built-in options.

### SizePreviewInfo Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Size` | `DaisySize` | `ExtraSmall` | Set the concrete tier for this option |
| `Name` | `string` | `""` | Internal name for localization lookup (e.g., "Small") |
| `IsVisible` | `bool` | `true` | Set to `false` to hide this size from the dropdown |
| `DisplayNameOverride` | `string?` | `null` | Custom label that overrides localized text |
| `Abbreviation` | `string` | `""` | Short form for compact display (XS, S, M, L, XL) |

### Example: Show Only 3 Sizes with Custom Names

```csharp
using Flowery.Controls;
using Flowery.Enums;

var sizePicker = new DaisySizeDropdown
{
    SizeOptions = new SizePreviewInfo[]
    {
        new() { Size = DaisySize.Small, Name = "Small", DisplayNameOverride = "Compact", Abbreviation = "C" },
        new() { Size = DaisySize.Medium, Name = "Medium", DisplayNameOverride = "Normal", Abbreviation = "N" },
        new() { Size = DaisySize.Large, Name = "Large", DisplayNameOverride = "Large", Abbreviation = "L" }
    }
};
```

### Example: Hide ExtraSmall and ExtraLarge

```csharp
sizePicker.SizeOptions = new SizePreviewInfo[]
{
    new() { Size = DaisySize.ExtraSmall, Name = "ExtraSmall", IsVisible = false },
    new() { Size = DaisySize.Small, Name = "Small", Abbreviation = "S" },
    new() { Size = DaisySize.Medium, Name = "Medium", Abbreviation = "M" },
    new() { Size = DaisySize.Large, Name = "Large", Abbreviation = "L" },
    new() { Size = DaisySize.ExtraLarge, Name = "ExtraLarge", IsVisible = false }
};
```

---

## Localization

Size names are automatically localized based on the current culture. The display name resolution order is:

1. **DisplayNameOverride** (if set) – your custom label
2. **Localized text** – from `FloweryLocalization` (e.g., "Klein" in German)
3. **Name** (fallback) – the enum name like "ExtraSmall"

### Supported Languages

Size names are localized in 12 languages:

- English, German, French, Spanish, Italian
- Japanese, Korean, Chinese (Simplified)
- Arabic, Turkish, Ukrainian, Hebrew

### Custom Localization Keys

If you need to override the built-in translations, add these keys to your localization:

```json
{
  "Size_ExtraSmall": "Extra Small",
  "Size_Small": "Small", 
  "Size_Medium": "Medium",
  "Size_Large": "Large",
  "Size_ExtraLarge": "Extra Large"
}
```

---

## Styling

The selected option follows the global size. The dropdown's appearance follows global changes when its own `Size` is `Auto`; an explicit size remains in effect.

### Size Variants

```xml
<!-- Use the dropdown's own Size property for its appearance -->
<controls:DaisySizeDropdown Size="Small" Width="120" />
<controls:DaisySizeDropdown Size="Large" Width="200" />
```

### Automatic MinWidth

Sizing sets `MinWidth` to `116` at every tier: 90 for text and 26 for the dropdown glyph.
Shorter labels and `ShowAbbreviations` do not reduce this minimum.
Setting `Width` below `116` does not make the control narrower.
Later size or theme updates also restore `MinWidth` to `116`.

---

## Programmatic Control

If you need to change the size without the UI:

```csharp
using Flowery.Controls;
using Flowery.Enums;

// Apply a size globally
FlowerySizeManager.ApplySize(DaisySize.Large);

// Check current size
DaisySize current = FlowerySizeManager.CurrentSize;
```

The dropdown selects the visible option that matches `FlowerySizeManager.CurrentSize`.
If no option matches, the existing selection remains unchanged.

---

## Integration with FlowerySizeManager

`DaisySizeDropdown` is designed to work seamlessly with the global sizing system:

- **On selection change**: Calls `FlowerySizeManager.ApplySize()`
- **On external size change**: Syncs to match the current global size
- **On culture change**: Refreshes localized display names

For comprehensive documentation on the sizing system, see [SizingAndScaling](https://tobitege.github.io/Flowery.Uno/#SizingAndScaling).

---

## Best Practices

1. **Place in settings or sidebar** – Give users easy access to change the size preference.

2. **Consider visibility** – For most apps, 3 sizes (Small, Medium, Large) may be enough.

3. **Use meaningful names** – If "Small" doesn't fit your domain, use `DisplayNameOverride` (e.g., "Compact", "Dense").

4. **Test all sizes** – Ensure your layouts work at both ExtraSmall and ExtraLarge.

5. **Persist preference** – Save the user's selection and restore it on app startup.

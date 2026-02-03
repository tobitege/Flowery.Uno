<!-- Supplementary documentation for DaisyPasswordBox -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyPasswordBox is a specialized password input field that manages its own reveal button and masked input. It inherits from `ContentControl` but behaves like a `PasswordBox`, providing **10 variants** and **4 size presets**. It supports bordered, ghost, filled, and semantic colored borders, plus labels, helper text, and icons.

## Features

- **Built-in Reveal Button**: Toggle password visibility with a single click.
- **Floating Labels**: Professional label animation that moves when the field is focused or contains text.
- **Unified Icon Support**: Easily add icons to the start or end of the field.
- **Theming & Sizing**: Full integration with the Flowery.Uno design system.

## Variant Options

| Variant | Description |
| --- | --- |
| Bordered (default) | Subtle 30% opacity border; brightens on focus. |
| Ghost | No border and transparent background; adds light fill on focus. |
| Filled | Filled background with bottom border; ideal for material-style forms. |
| Primary / Secondary / Accent | Colored borders with focus states. |
| Info / Success / Warning / Error | Semantic border colors. |

## Size Options

DaisyPasswordBox uses **fixed heights** for each size to match DaisyUI's design.

| Size | Height | Font Size | Floating Height | Use Case |
| --- | --- | --- | --- | --- |
| ExtraSmall | 24 | 10 | 40 | Dense tables/toolbars. |
| Small | 32 | 12 | 48 | Compact forms. |
| Medium (default) | 48 | 14 | 56 | General usage. |
| Large | 64 | 18 | 64 | Prominent inputs/hero sections. |

## Properties

**DaisyPasswordBox-specific properties:**

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Password` | `string` | `""` | The password text. |
| `PlaceholderText` | `string` | `""` | Text shown when the field is empty. |
| `PasswordChar` | `string` | `●` | The character used to mask the password. |
| `IsPasswordRevealButtonEnabled` | `bool` | `true` | Whether the reveal button eye icon is shown. |
| `Variant` | `DaisyInputVariant` | `Bordered` | Visual style variant (see table above). |
| `Size` | `DaisySize` | `Medium` | Size preset (see table above). |
| `Label` | `string` | `""` | Label text shown above the input. |
| `LabelPosition` | `DaisyLabelPosition` | `Top` | Position of the label (`Top`, `Floating`, `None`). |
| `IsRequired` | `bool` | `false` | Shows a red asterisk (*) next to the label. |
| `HintText` | `string` | `""` | Optional text shown next to the label (e.g. "Optional"). |
| `HelperText` | `string` | `""` | Guidance text shown below the input. |
| `StartIconData` | `string` | `null` | Path data for an icon on the left. |
| `EndIconData` | `string` | `null` | Path data for an icon on the right (before reveal button). |

## Quick Examples

```xml
<!-- Basic -->
<daisy:DaisyPasswordBox PlaceholderText="Enter password" />

<!-- Floating Label -->
<daisy:DaisyPasswordBox Label="Password" LabelPosition="Floating" PlaceholderText="Enter password" />

<!-- With Reveal disabled -->
<daisy:DaisyPasswordBox IsPasswordRevealButtonEnabled="False" PlaceholderText="No reveal eye" />

<!-- Primary variant with Icon -->
<daisy:DaisyPasswordBox Variant="Primary" 
                       StartIconData="{StaticResource DaisyIconSettings}" 
                       PlaceholderText="Enter secret key" />

<!-- Error state with helper text -->
<daisy:DaisyPasswordBox Variant="Error" 
                       Label="Password"
                       HelperText="Password must be at least 8 characters" 
                       PlaceholderText="Try again" />

<!-- Specific Size -->
<daisy:DaisyPasswordBox Size="Small" PlaceholderText="Small password field" />
```

## Tips & Best Practices

- Use `LabelPosition="Floating"` for a modern, space-efficient look.
- Use `HelperText` to inform users about password requirements (length, complexity).
- When using `StartIconData`, use a lock icon or key icon to signify security.
- The `Password` property is two-way bindable, making it easy to integrate with ViewModels.
- `IsPasswordRevealButtonEnabled` is enabled by default as it is a standard accessibility/UX pattern for password fields.

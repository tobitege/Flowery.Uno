<!-- Supplementary documentation for DaisyCalendarDatePicker -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyCalendarDatePicker provides a themed CalendarDatePicker that matches Daisy input surfaces while keeping the native calendar flyout behavior.

## Variant Options

| Variant | Description |
| --- | --- |
| **Bordered** | Standard input outline (default). |
| **Ghost** | Transparent background with no border. |
| **Filled** | Subtle filled background with bottom border. |
| **Primary** | Emphasized border using the primary palette. |
| **Secondary** | Emphasized border using the secondary palette. |
| **Accent** | Emphasized border using the accent palette. |
| **Info** | Emphasized border using the info palette. |
| **Success** | Emphasized border using the success palette. |
| **Warning** | Emphasized border using the warning palette. |
| **Error** | Emphasized border using the error palette. |

## Size Options

| Size | Use Case |
| --- | --- |
| ExtraSmall | Compact toolbars or dense forms. |
| Small | Tight layouts. |
| Medium | Default sizing. |
| Large | Touch-friendly inputs. |
| ExtraLarge | Prominent date entry surfaces. |

## Quick Examples

```xml
<!-- Basic usage -->
<daisy:DaisyCalendarDatePicker PlaceholderText="Select date" />

<!-- Variant styling -->
<daisy:DaisyCalendarDatePicker Variant="Primary" PlaceholderText="Primary" />

<!-- Size override -->
<daisy:DaisyCalendarDatePicker Size="Small" PlaceholderText="Small" />
```

## Tips & Best Practices

- Provide a short, action-oriented placeholder when no date is selected.
- Use `Variant="Filled"` for low-contrast, form-heavy layouts.
- Pair with form validation to highlight missing or invalid dates.

---

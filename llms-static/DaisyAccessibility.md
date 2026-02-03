# Overview

A static helper class providing accessibility utilities for Daisy controls. It simplifies setting up screen reader support across all controls.

**Namespace:** `Flowery.Services`

`DaisyAccessibility` provides:

- An attached property for custom screen reader text
- A setup helper for control authors to register accessibility defaults
- Automatic syncing to WinUI's `AutomationProperties.Name`

## Attached Property

### AccessibleText

Set custom screen reader text on any Daisy control:

```xml
<daisy:DaisyButton 
    Content="🗑️"
    services:DaisyAccessibility.AccessibleText="Delete item" />
```

> [!NOTE]
> Add `xmlns:services="using:Flowery.Services"` to your XAML namespace declarations.

This ensures screen readers announce "Delete item" instead of the emoji.

## API Reference

### Methods

| Method | Description |
| --- | --- |
| `GetAccessibleText(obj)` | Gets the accessible text for a control |
| `SetAccessibleText(obj, value)` | Sets the accessible text for a control |
| `SetupAccessibility<T>(defaultText)` | Registers accessibility handling for a control type (for control authors) |
| `GetEffectiveAccessibleText(control, defaultText)` | Gets accessible text with fallback to default |
| `ApplyAccessibility(control, defaultText)` | Applies the effective accessible text to `AutomationProperties.Name` |

## For Control Authors

When creating a new Daisy control, call `SetupAccessibility` in the static constructor and `ApplyAccessibility` in `OnApplyTemplate`:

```csharp
using Flowery.Services;

public class DaisyMyControl : Control
{
    static DaisyMyControl()
    {
        DaisyAccessibility.SetupAccessibility<DaisyMyControl>("My Control");
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        DaisyAccessibility.ApplyAccessibility(this, "My Control");
    }
}
```

This:

1. Registers a default accessible text for the control type
2. Applies it to `AutomationProperties.Name` when the template is applied
3. Automatically syncs any custom `AccessibleText` changes to `AutomationProperties.Name`

## Examples

### Icon-Only Button

```xml
<daisy:DaisyButton 
    services:DaisyAccessibility.AccessibleText="Search">
    <PathIcon Data="{StaticResource SearchIcon}" />
</daisy:DaisyButton>
```

### Status Indicator

```xml
<daisy:DaisyStatusIndicator 
    Status="Online"
    services:DaisyAccessibility.AccessibleText="User is currently online" />
```

### Rating Control

```xml
<daisy:DaisyRating 
    Value="4"
    services:DaisyAccessibility.AccessibleText="Rating: 4 out of 5 stars" />
```

## Best Practices

1. **Always set AccessibleText for icon-only controls** - Screen readers can't interpret icons
2. **Be descriptive but concise** - "Delete" is better than "Click to delete this item from the list"
3. **Include state information** - "Mute (currently unmuted)" is more helpful than just "Mute"
4. **Test with a screen reader** - Windows Narrator (Win+Ctrl+Enter) or NVDA

# Overview

A static helper class providing centralized access to design tokens, resource lookups, and default sizing values for all Flowery.Uno controls.
It provides:

1. **Resource Lookup Methods** - Safe access to `ResourceDictionary` values with fallbacks
2. **Size Key Helpers** - Convert `DaisySize` enum to resource key strings
3. **Default Sizing Values** - Hardcoded fallbacks for all control dimensions

All Flowery.Uno controls use this class internally to ensure consistent sizing and theming.

## Resource Lookup Methods

These methods safely retrieve values from a `ResourceDictionary`, searching merged dictionaries recursively.

### Basic Lookups (with ResourceDictionary)

```csharp
using Flowery.Theming;

var resources = Application.Current.Resources;

// Get a double value with fallback
double height = DaisyResourceLookup.GetDouble(resources, "DaisySizeMediumHeight", 28);

// Get a Thickness
Thickness padding = DaisyResourceLookup.GetThickness(resources, "DaisyButtonMediumPadding", new Thickness(10, 0));

// Get a CornerRadius
CornerRadius radius = DaisyResourceLookup.GetCornerRadius(resources, "DaisyCornerRadiusMedium", new CornerRadius(6));

// Get a Brush
Brush primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Colors.Blue));
```

### Convenience Overloads (uses Application.Current.Resources)

```csharp
// Shorter syntax when using app-level resources
double height = DaisyResourceLookup.GetDouble("DaisySizeMediumHeight", 28);
Brush primary = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
```

### Low-Level Access

```csharp
// Check if a resource exists
if (DaisyResourceLookup.TryGetResource(resources, "MyCustomKey", out var value))
{
    // value is the resource object
}
```

## Size Key Helpers

Convert `DaisySize` enum values to string keys for resource lookups.

### GetSizeKey - Abbreviated (XS, S, M, L, XL)

```csharp
string key = DaisyResourceLookup.GetSizeKey(DaisySize.ExtraSmall); // "XS"
string key = DaisyResourceLookup.GetSizeKey(DaisySize.Medium);     // "M"
string key = DaisyResourceLookup.GetSizeKey(DaisySize.ExtraLarge); // "XL"

// Use for resources like "DaisyBadgeXSHeight"
var resourceKey = $"DaisyBadge{GetSizeKey(size)}Height";
```

### GetSizeKeyFull - Full Name

```csharp
string key = DaisyResourceLookup.GetSizeKeyFull(DaisySize.ExtraSmall); // "ExtraSmall"
string key = DaisyResourceLookup.GetSizeKeyFull(DaisySize.Medium);     // "Medium"

// Use for resources like "DaisySizeExtraSmallHeight"
var resourceKey = $"DaisySize{GetSizeKeyFull(size)}Height";
```

## Default Sizing Methods

These methods return hardcoded default values for each `DaisySize`. They're used as fallbacks when resources aren't defined.

### Available Methods

| Method | Returns | XS | S | M | L | XL |
| ------ | ------- | -- | - | - | - | -- |
| `GetDefaultHeight(size)` | double | 22 | 24 | 28 | 32 | 36 |
| `GetDefaultFloatingInputHeight(size)` | double | 28 | 32 | 36 | 40 | 44 |
| `GetDefaultFontSize(size)` | double | 9 | 10 | 12 | 14 | 16 |
| `GetDefaultHeaderFontSize(size)` | double | 14 | 16 | 20 | 24 | 28 |
| `GetDefaultIconSize(size)` | double | 10 | 12 | 14 | 16 | 18 |
| `GetDefaultIconSpacing(size)` | double | 3 | 4 | 6 | 6 | 8 |
| `GetDefaultSwatchSize(size)` | double | 6 | 8 | 10 | 12 | 14 |
| `GetDefaultPadding(size)` | Thickness | 6,0 | 8,0 | 10,0 | 12,0 | 14,0 |
| `GetDefaultCornerRadius(size)` | CornerRadius | 2 | 4 | 6 | 8 | 10 |
| `GetSizeValue(size)` | double | 14 | 20 | 26 | 38 | 52 |

### Specialized Methods

| Method | Returns | Description |
| --- | --- | --- |
| `GetDefaultSpinnerButtonPadding(size)` | double | NumericUpDown spinner button padding |
| `GetDefaultSpinnerGridWidth(size)` | double | NumericUpDown spinner grid width |
| `GetDefaultRadialProgressSize(size)` | double | Radial progress dimensions (24-128) |
| `GetDefaultRangeTrackHeight(size)` | double | Range slider track height |
| `GetDefaultRangeThumbSize(size)` | double | Range slider thumb size |
| `GetDefaultRatingStarSize(size)` | double | Rating star dimensions |
| `GetDefaultRatingSpacing(size)` | double | Rating star gap |
| `GetDefaultMenuPadding(size)` | Thickness | Menu/list row padding |
| `GetDefaultTagPadding(size)` | Thickness | Tag/chip padding |
| `GetDefaultOtpSlotSize(size)` | double | OTP input slot size |
| `GetDefaultOtpSpacing(size)` | double | OTP slot spacing |

### Avatar-Specific Methods

| Method | Returns | Description |
| --- | --- | --- |
| `GetAvatarSize(size)` | double | Avatar width/height (22-80) |
| `GetAvatarStatusSize(size)` | double | Status indicator size (5-16) |
| `GetAvatarFontSize(size)` | double | Placeholder text font size |
| `GetAvatarIconSize(size)` | double | Avatar icon size |

---

## Usage in Custom Controls

### Pattern 1: Direct Defaults (Simple)

```csharp
private void ApplySizing()
{
    Height = DaisyResourceLookup.GetDefaultHeight(Size);
    FontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
    Padding = DaisyResourceLookup.GetDefaultPadding(Size);
}
```

### Pattern 2: Resource with Fallback (Flexible)

```csharp
private void ApplySizing()
{
    var resources = Application.Current?.Resources;
    var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);

    Height = DaisyResourceLookup.GetDouble(resources, 
        $"DaisySize{sizeKey}Height", 
        DaisyResourceLookup.GetDefaultHeight(Size));

    FontSize = DaisyResourceLookup.GetDouble(resources, 
        $"DaisySize{sizeKey}FontSize", 
        DaisyResourceLookup.GetDefaultFontSize(Size));
}
```

### Pattern 3: Control-Specific Resources

```csharp
private void ApplySizing()
{
    var resources = Application.Current?.Resources;
    var sizeKey = DaisyResourceLookup.GetSizeKey(Size); // "XS", "S", "M", etc.

    // Look for control-specific resource first, fall back to default
    Height = DaisyResourceLookup.GetDouble(resources, 
        $"DaisyBadge{sizeKey}Height", 
        DaisyResourceLookup.GetDefaultHeight(Size));
}
```

## Overriding Defaults via Resources

You can override any default value by defining resources in your app:

```xml
<Application.Resources>
    <!-- Override default heights -->
    <x:Double x:Key="DaisySizeExtraSmallHeight">20</x:Double>
    <x:Double x:Key="DaisySizeSmallHeight">22</x:Double>
    <x:Double x:Key="DaisySizeMediumHeight">26</x:Double>
    
    <!-- Override control-specific values -->
    <x:Double x:Key="DaisyBadgeXSHeight">18</x:Double>
    <x:Double x:Key="DaisyButtonLHeight">40</x:Double>
</Application.Resources>
```

---

## Related

- [DesignTokens](DesignTokens.md) - Complete design token reference
- [FlowerySizeManager](FlowerySizeManager.md) - Global size management
- [SizingAndScaling](SizingAndScaling.md) - Sizing system overview

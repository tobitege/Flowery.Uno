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
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

var resources = Application.Current.Resources;

// Get a double value with fallback
double height = DaisyResourceLookup.GetDouble(resources, "DaisySizeMediumHeight", 32);

// Get a Thickness
Thickness padding = DaisyResourceLookup.GetThickness(resources, "DaisyButtonMediumPadding", new Thickness(12, 0));

// Get a CornerRadius
CornerRadius radius = DaisyResourceLookup.GetCornerRadius(resources, "DaisySizeMediumCornerRadius", new CornerRadius(8));

// Get a Brush
Brush primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Colors.Blue));
```

### Convenience Overloads (uses Application.Current.Resources)

```csharp
// Shorter syntax when using app-level resources
double height = DaisyResourceLookup.GetDouble("DaisySizeMediumHeight", 32);
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
Import `Flowery.Enums` for `DaisySize` and `Flowery.Theming` for `DaisyResourceLookup`.

### GetSizeKey - Abbreviated (XS, S, M, L, XL)

```csharp
string extraSmallKey = DaisyResourceLookup.GetSizeKey(DaisySize.ExtraSmall); // "XS"
string mediumKey = DaisyResourceLookup.GetSizeKey(DaisySize.Medium);         // "M"
string extraLargeKey = DaisyResourceLookup.GetSizeKey(DaisySize.ExtraLarge); // "XL"

// Abbreviated keys are useful for your own resource names.
var resourceKey = $"MyBadge{DaisyResourceLookup.GetSizeKey(DaisySize.ExtraSmall)}Height";
```

### GetSizeKeyFull - Full Name

```csharp
string extraSmallKey = DaisyResourceLookup.GetSizeKeyFull(DaisySize.ExtraSmall); // "ExtraSmall"
string mediumKey = DaisyResourceLookup.GetSizeKeyFull(DaisySize.Medium);         // "Medium"

// Use for resources like "DaisySizeExtraSmallHeight"
var resourceKey = $"DaisySize{DaisyResourceLookup.GetSizeKeyFull(DaisySize.Medium)}Height";
```

## Default Sizing Methods

Pass a concrete tier to these helpers. For a Daisy control with automatic sizing, use `ResolvedSize` instead of its authored `Size`.
The default helpers return fallback dimensions; resource lookup is a separate operation. Avatar helpers also read their named tokens.
Use `DaisyResourceLookup.EnsureTokens()` before reading startup tokens. Built-in sizing tokens use full tier names, such as `DaisyBadgeExtraSmallHeight`.

### Available Methods

| Method | Returns | XS | S | M | L | XL |
| ------ | ------- | -- | - | - | - | -- |
| `GetDefaultHeight(size)` desktop | double | 24 | 28 | 32 | 36 | 40 |
| `GetDefaultHeight(size)` mobile | double | 36 | 40 | 44 | 48 | 56 |
| `GetDefaultFloatingInputHeight(size)` desktop | double | 36 | 40 | 44 | 48 | 50 |
| `GetDefaultFloatingInputHeight(size)` mobile | double | 44 | 48 | 52 | 56 | 64 |
| `GetDefaultFontSize(size)` | double | 10 | 12 | 14 | 18 | 20 |
| `GetDefaultHeaderFontSize(size)` | double | 14 | 16 | 20 | 24 | 28 |
| `GetDefaultIconSize(size)` | double | 8 | 12 | 16 | 20 | 24 |
| `GetDefaultIconSpacing(size)` | double | 4 | 4 | 8 | 8 | 8 |
| `GetDefaultSwatchSize(size)` | double | 6 | 8 | 10 | 12 | 14 |
| `GetDefaultPadding(size)` | Thickness | 8,0 | 8,0 | 12,0 | 12,0 | 16,0 |
| `GetDefaultCornerRadius(size)` | CornerRadius | 4 | 4 | 8 | 8 | 12 |
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
| `GetAvatarSize(size)` | double | Reads the avatar token; fallback width/height 22-80 |
| `GetAvatarStatusSize(size)` | double | Status indicator size (5-16) |
| `GetAvatarFontSize(size)` | double | Placeholder text font size |
| `GetAvatarIconSize(size)` | double | Avatar icon size |

---

## Usage in Custom Controls

These methods assume a `DaisyBaseContentControl` subclass with `ResolvedSize`.
Use the [registration and refresh contract](FlowerySizeManager.md#custom-controls) to keep its visuals current.

### Pattern 1: Direct Defaults (Simple)

```csharp
private void ApplySizing()
{
    Height = DaisyResourceLookup.GetDefaultHeight(ResolvedSize);
    FontSize = DaisyResourceLookup.GetDefaultFontSize(ResolvedSize);
    Padding = DaisyResourceLookup.GetDefaultPadding(ResolvedSize);
}
```

### Pattern 2: Resource with Fallback (Flexible)

```csharp
private void ApplySizing()
{
    var resources = Application.Current?.Resources;
    var size = ResolvedSize;
    var sizeKey = DaisyResourceLookup.GetSizeKeyFull(size);

    Height = DaisyResourceLookup.GetDouble(resources, 
        $"DaisySize{sizeKey}Height", 
        DaisyResourceLookup.GetDefaultHeight(size));

    FontSize = DaisyResourceLookup.GetDouble(resources, 
        $"DaisySize{sizeKey}FontSize", 
        DaisyResourceLookup.GetDefaultFontSize(size));
}
```

### Pattern 3: Control-Specific Resources

Control-specific keys also use full tier names. For example, read a Medium badge height with its own fallback:

```csharp
var resources = Application.Current?.Resources;
double badgeHeight = DaisyResourceLookup.GetDouble(resources, "DaisyBadgeMediumHeight", 24);
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
    <x:Double x:Key="DaisyBadgeExtraSmallHeight">18</x:Double>
    <x:Double x:Key="DaisySizeLargeHeight">40</x:Double>
</Application.Resources>
```

---

## Related

- [DesignTokens](DesignTokens.md) - Complete design token reference
- [FlowerySizeManager](FlowerySizeManager.md) - Global size management
- [SizingAndScaling](SizingAndScaling.md) - Sizing system overview

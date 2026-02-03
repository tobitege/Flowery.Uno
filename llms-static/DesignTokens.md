# Design Tokens

Flowery.Uno uses centralized design tokens to ensure consistent sizing across all controls. Tokens are defined in code via `DaisyTokenDefaults.cs` and can be accessed programmatically through `DaisyResourceLookup`.

> **Note:** This document covers **sizing and layout tokens**. For color and brush customization, see [`StylingResources.md`](StylingResources.md).

## Overview

Design tokens provide a single source of truth for:

- **Control Heights** - Standard heights for buttons, inputs, selects
- **Font Sizes** - Typography scale across controls  
- **Corner Radius** - Rounded corners for controls
- **Padding** - Internal spacing for different control types
- **Spacing** - Layout spacing values
- **Border Thickness** - Standard border widths

## Uno Platform Implementation

Flowery.Uno implements design tokens in C#:

| Component | Purpose |
| --- | --- |
| `DaisyTokenDefaults.cs` | Ensures default tokens are present in `Application.Current.Resources` |
| `DaisyResourceLookup.cs` | Provides helper methods to read tokens and get fallback values |

### How It Works

1. Controls call `DaisyTokenDefaults.EnsureDefaults(resources)` on load
2. This populates `Application.Current.Resources` with default token values
3. Controls use `DaisyResourceLookup.GetDouble()`, `GetThickness()`, etc. to read tokens
4. If a token is missing, centralized `GetDefault*()` methods provide fallbacks

### Ensuring Tokens at Startup (Recommended)

If you need token values before any Daisy control loads (e.g., for layout math at app startup),
call the public helper once during app initialization:

```csharp
using Flowery.Theming;

// App startup (e.g., App.OnLaunched)
DaisyResourceLookup.EnsureTokens();
```

## Programmatic Access via DaisyResourceLookup

For custom controls or code-behind, use `DaisyResourceLookup` helper methods:

```csharp
using Flowery.Theming;
using Flowery.Controls;

// Get default values based on DaisySize
double height = DaisyResourceLookup.GetDefaultHeight(DaisySize.Medium);        // 28
double fontSize = DaisyResourceLookup.GetDefaultFontSize(DaisySize.Medium);    // 12
Thickness padding = DaisyResourceLookup.GetDefaultPadding(DaisySize.Medium);   // 10,0,10,0
CornerRadius radius = DaisyResourceLookup.GetDefaultCornerRadius(DaisySize.Medium); // 8

// Read from ResourceDictionary with fallback
var resources = Application.Current?.Resources;
double actualHeight = DaisyResourceLookup.GetDouble(resources, "DaisySizeMediumHeight", 28);
```

### Available Helper Methods

| Method | Returns | Description |
| --- | --- | --- |
| `GetDefaultHeight(size)` | double | Standard control height (22-36) |
| `GetDefaultFloatingInputHeight(size)` | double | Taller input height (28-46) |
| `GetDefaultFontSize(size)` | double | Primary font size (9-16) |
| `GetDefaultHeaderFontSize(size)` | double | Large display font size (12-24) |
| `GetDefaultPadding(size)` | Thickness | Horizontal button padding |
| `GetDefaultCornerRadius(size)` | CornerRadius | Rounded corners (4-12) |
| `GetDefaultIconSize(size)` | double | Icon dimensions (10-18) |
| `GetDefaultIconSpacing(size)` | double | Gap between icon and text (3-8) |
| `GetDefaultMenuPadding(size)` | Thickness | Menu/list item padding |
| `GetDefaultTagPadding(size)` | Thickness | Tag/chip padding |
| `GetDefaultRangeTrackHeight(size)` | double | Range slider track (4-16) |
| `GetDefaultRangeThumbSize(size)` | double | Range slider thumb (12-36) |
| `GetDefaultRatingStarSize(size)` | double | Rating star dimensions (16-40) |
| `GetDefaultRatingSpacing(size)` | double | Rating star gap (2-8) |
| `GetDefaultRadialProgressSize(size)` | double | Radial progress dimensions (24-128) |
| `GetDefaultOtpSlotSize(size)` | double | OTP input slot size (32-56) |
| `GetDefaultOtpSpacing(size)` | double | OTP slot spacing (4-10) |
| `GetDefaultSwatchSize(size)` | double | Color swatch/dot size for theme pickers (6-14) |
| `GetAvatarSize(size)` | double | Avatar dimensions (20-80) |
| `GetAvatarStatusSize(size)` | double | Status indicator size (5-16) |
| `GetAvatarFontSize(size)` | double | Avatar placeholder font (7-20) |
| `GetAvatarIconSize(size)` | double | Avatar icon size (10-34) |

## Token Reference

### Control Heights

Used for buttons, inputs, selects, and other fixed-height controls.

| Token | XS | S | M | L | XL | Used By |
| ----- | -- | -- | -- | -- | --- | ------- |
| `DaisySize{Size}Height` | 22 | 24 | 28 | 32 | 36 | DaisyButton, DaisyInput, DaisySelect |

### Floating Input Heights

Specific heights for taller input controls like `DaisyNumericUpDown` and `DaisyFileInput`.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyInputFloating{Size}Height` | 28 | 34 | 38 | 42 | 46 |

### Font Sizes

#### Primary Font Sizes

Typography scale used across controls for main content.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}FontSize` | 9 | 10 | 12 | 14 | 16 |

#### Secondary Font Sizes

For hint text, helper text, captions, labels (~0.8x of primary).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}SecondaryFontSize` | 8 | 9 | 10 | 12 | 14 |

#### Tertiary Font Sizes

For very small captions, counters (~0.7x of primary).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}TertiaryFontSize` | 8 | 8 | 9 | 10 | 12 |

#### Section Header Font Sizes

For section titles within pages.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}SectionHeaderFontSize` | 10 | 12 | 13 | 15 | 17 |

#### Header Font Sizes

For large display headings (~1.4x of primary).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}HeaderFontSize` | 12 | 14 | 17 | 20 | 24 |

### Corner Radius

Rounded corners for controls.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}CornerRadius` | 4 | 6 | 8 | 10 | 12 |

### Button Padding

Horizontal-only padding (vertical is controlled by Height).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyButton{Size}Padding` | 8,0 | 12,0 | 14,0 | 16,0 | 16,0 |

### Input Padding

Full padding for text inputs.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyInput{Size}Padding` | 8,4 | 12,5 | 12,6 | 14,6 | 14,7 |

### Tab Padding

Both horizontal and vertical padding (tabs don't use fixed height).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyTab{Size}Padding` | 10,2 | 12,4 | 12,5 | 12,5 | 12,5 |

### Badge Tokens

Badges use a separate, more compact scale.

| Token | XS | S | M | L |
| ----- | -- | -- | -- | -- |
| `DaisyBadge{Size}Height` | 14 | 16 | 20 | 22 |
| `DaisyBadge{Size}FontSize` | 8 | 9 | 10 | 12 |
| `DaisyBadge{Size}Padding` | 4,0 | 6,0 | 8,0 | 12,0 |

### Card Padding

| Token | Value |
| --- | --- |
| `DaisyCardLargePadding` | 24 |
| `DaisyCardMediumPadding` | 18 |
| `DaisyCardSmallPadding` | 12 |
| `DaisyCardCompactPadding` | 8 |

### Spacing

General spacing values for layouts.

| Token | Value |
| --- | --- |
| `DaisySpacingXL` | 20 |
| `DaisySpacingLarge` | 14 |
| `DaisySpacingMedium` | 10 |
| `DaisySpacingSmall` | 6 |
| `DaisySpacingXS` | 3 |

### Border Thickness

| Token | Value |
| --- | --- |
| `DaisyBorderThicknessNone` | 0 |
| `DaisyBorderThicknessThin` | 1 |
| `DaisyBorderThicknessMedium` | 2 |
| `DaisyBorderThicknessThick` | 3 |

---

## Checkbox/Radio Indicator Sizes

Outer border size for checkboxes and radio buttons.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyCheckbox{Size}Size` | 12 | 16 | 18 | 24 | 28 |

### Checkmark Icon Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyCheckmark{Size}Size` | 7 | 10 | 12 | 16 | 18 |

### Radio Inner Dot Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyRadioDot{Size}Size` | 5 | 8 | 10 | 14 | 18 |

---

## Toggle Switch Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyToggle{Size}Width` | 24 | 28 | 36 | 42 | 48 |
| `DaisyToggle{Size}Height` | 14 | 16 | 20 | 24 | 28 |

### Toggle Knob Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyToggleKnob{Size}Size` | 10 | 12 | 16 | 18 | 22 |

---

## Progress Bar Heights

| Token | XS | S | M | L |
| ----- | -- | -- | -- | -- |
| `DaisyProgress{Size}Height` | 2 | 4 | 8 | 16 |

### Progress Corner Radius

| Token | XS | S | M | L |
| ----- | -- | -- | -- | -- |
| `DaisyProgress{Size}CornerRadius` | 1 | 2 | 4 | 8 |

---

## Avatar Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyAvatar{Size}Size` | 20 | 28 | 40 | 56 | 80 |
| `DaisyAvatarStatus{Size}Size` | 5 | 7 | 10 | 14 | 16 |
| `DaisyAvatarFont{Size}Size` | 7 | 9 | 12 | 16 | 20 |
| `DaisyAvatarIcon{Size}Size` | 10 | 14 | 18 | 24 | 34 |

---

## Menu Item Padding

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyMenu{Size}Padding` | 6,3 | 8,4 | 8,5 | 10,6 | 10,6 |

### Menu Font Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyMenu{Size}FontSize` | 9 | 10 | 12 | 13 | 15 |

---

## Kbd (Keyboard) Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyKbd{Size}Height` | 12 | 16 | 20 | 24 | 28 |
| `DaisyKbd{Size}Padding` | 2,0 | 3,0 | 4,0 | 6,0 | 8,0 |
| `DaisyKbd{Size}FontSize` | 8 | 9 | 10 | 12 | 13 |
| `DaisyKbd{Size}CornerRadius` | 2 | 3 | 4 | 5 | 6 |

---

## TextArea Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyTextArea{Size}MinHeight` | 40 | 48 | 64 | 96 | 128 |
| `DaisyTextArea{Size}Padding` | 6 | 10 | 12 | 14 | 18 |

---

## DateTimeline Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyDateTimeline{Size}Height` | 48 | 56 | 64 | 80 | 104 |
| `DaisyDateTimeline{Size}ItemWidth` | 48 | 56 | 64 | 80 | 96 |
| `DaisyDateTimeline{Size}CornerRadius` | 8 | 10 | 12 | 16 | 20 |
| `DaisyDateTimeline{Size}DayNumberFontSize` | 14 | 16 | 20 | 26 | 32 |
| `DaisyDateTimeline{Size}DayNameFontSize` | 9 | 10 | 10 | 13 | 15 |
| `DaisyDateTimeline{Size}MonthNameFontSize` | 8 | 9 | 10 | 12 | 14 |
| `DaisyDateTimeline{Size}HeaderFontSize` | 12 | 14 | 16 | 18 | 20 |

---

## OTP Input Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyOtp{Size}SlotSize` | 32 | 40 | 48 | 56 | 56 |
| `DaisyOtp{Size}FontSize` | 14 | 16 | 18 | 22 | 24 |
| `DaisyOtp{Size}Spacing` | 4 | 5 | 6 | 8 | 10 |

---

## Tag Picker Tokens

Tag chip text uses the shared `DaisySize{Size}FontSize` tokens. Tag-specific tokens cover padding and close icon size:

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyTag{Size}Padding` | 4,2 | 6,3 | 8,4 | 10,5 | 12,6 |
| `DaisyTagClose{Size}Size` | 8 | 10 | 12 | 14 | 16 |

---

## Divider Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyDivider{Size}Thickness` | 1 | 1.5 | 2 | 3 | 4 |
| `DaisyDivider{Size}FontSize` | 9 | 10 | 12 | 14 | 16 |
| `DaisyDivider{Size}TextPadding` | 8 | 10 | 12 | 16 | 18 |
| `DaisyDivider{Size}Margin` | 2 | 3 | 4 | 6 | 8 |

---

## SlideToConfirm Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySlideToConfirm{Size}TrackWidth` (MinWidth) | 60 | 70 | 80 | 90 | 100 |
| `DaisySlideToConfirm{Size}TrackHeight` | 20 | 24 | 28 | 32 | 36 |
| `DaisySlideToConfirm{Size}HandleSize` | 12 | 14 | 16 | 18 | 22 |
| `DaisySlideToConfirm{Size}IconSize` | 6 | 7 | 8 | 9 | 11 |
| `DaisySlideToConfirm{Size}FontSize` | 6 | 7 | 8 | 9 | 10 |
| `DaisySlideToConfirm{Size}CornerRadius` | 10 | 12 | 14 | 16 | 18 |

> **Note:** Track width auto-sizes to fit text content. The token value is used as MinWidth.

---

## Dock Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyDock{Size}ItemSize` | 16 | 24 | 32 | 48 | 64 |
| `DaisyDock{Size}Spacing` | 2 | 2 | 4 | 4 | 6 |
| `DaisyDockIconSizeRatio` | 0.6 | 0.6 | 0.6 | 0.6 | 0.6 |

---

## Large Display Tokens

Used by `DaisyNumberFlow` and similar large-format controls.

| Token | Value |
| --- | --- |
| `LargeDisplayFontSize` | 36 |
| `LargeDisplayElementHeight` | 86 |
| `LargeDisplayElementWidth` | 58 |
| `LargeDisplayElementCornerRadius` | 12 |
| `LargeDisplayElementSpacing` | 4 |
| `LargeDisplayButtonSize` | 42 |
| `LargeDisplayButtonFontSize` | 20 |
| `LargeDisplayButtonCornerRadius` | 8 |
| `LargeDisplayButtonSpacing` | 2 |
| `LargeDisplayContainerPadding` | 16 |
| `LargeDisplayContainerCornerRadius` | 16 |

---

## Making Custom Controls Token-Aware

### Pattern 1: Use DaisyResourceLookup (Recommended)

For custom controls, use `DaisyResourceLookup` methods instead of hardcoded values:

```csharp
public class MyCustomControl : ContentControl
{
    public DaisySize Size { get; set; } = DaisySize.Medium;

    private void ApplySizing()
    {
        var resources = Application.Current?.Resources;
        if (resources != null)
            DaisyTokenDefaults.EnsureDefaults(resources);

        // Use centralized defaults - single source of truth
        Height = DaisyResourceLookup.GetDefaultHeight(Size);
        FontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
        Padding = DaisyResourceLookup.GetDefaultPadding(Size);
    }
}
```

### Pattern 2: Read from ResourceDictionary with Fallback

For maximum flexibility (allows token overrides):

```csharp
private void ApplySizing()
{
    var resources = Application.Current?.Resources;
    if (resources != null)
        DaisyTokenDefaults.EnsureDefaults(resources);

    string sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size); // "Medium", "Large", etc.

    // Try resource dictionary first, fallback to centralized default
    Height = DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}Height",
        DaisyResourceLookup.GetDefaultHeight(Size));
    FontSize = DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}FontSize",
        DaisyResourceLookup.GetDefaultFontSize(Size));
}
```

---

## Controls Using Tokens

Most Daisy controls use the token system for consistent sizing:

- **DaisyButton** - Heights, font sizes, padding, corner radius
- **DaisyInput** - Heights, font sizes, padding, corner radius
- **DaisySelect** - Heights, font sizes, corner radius
- **DaisyNumericUpDown** - Floating heights, font sizes, corner radius
- **DaisyTabs** - Font sizes, padding, corner radius
- **DaisyBadge** - Badge-specific heights, font sizes, padding
- **DaisyFileInput** - Floating heights, font sizes, corner radius
- **DaisyCheckBox** - Indicator sizes, checkmark sizes
- **DaisyRadio** - Indicator sizes, inner dot sizes
- **DaisyToggle** - Switch dimensions, knob sizes
- **DaisyProgress** - Bar heights, corner radius
- **DaisyAvatar** - Avatar dimensions, status indicators
- **DaisyMenu** - Item padding, font sizes
- **DaisyKbd** - Heights, padding, font sizes, corner radius
- **DaisyTextArea** - MinHeight, padding
- **DaisyRange** - Track height, thumb size
- **DaisyRating** - Star size, spacing
- **DaisyRadialProgress** - Circle dimensions
- **DaisyOtpInput** - Slot size, font size, spacing
- **DaisyTagPicker** - Tag padding, font size, icon sizes
- **DaisyDateTimeline** - Height, item width, font sizes, corner radius
- **DaisyDivider** - Thickness, font size, text padding, margin
- **DaisySlideToConfirm** - Track dimensions, handle size, icon size, font size
- **DaisyDock** - Item size, spacing, icon ratio
- And more...

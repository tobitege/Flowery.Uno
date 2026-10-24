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

1. Controls initialize defaults through the internal `DaisyTokenDefaults` implementation
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
using Flowery.Enums;
using Microsoft.UI.Xaml;

// Get default values based on DaisySize
double height = DaisyResourceLookup.GetDefaultHeight(DaisySize.Medium);        // 32 on desktop, 44 on mobile
double fontSize = DaisyResourceLookup.GetDefaultFontSize(DaisySize.Medium);    // 14
Thickness padding = DaisyResourceLookup.GetDefaultPadding(DaisySize.Medium);   // 12,0,12,0
CornerRadius radius = DaisyResourceLookup.GetDefaultCornerRadius(DaisySize.Medium); // 8

// Read from ResourceDictionary with fallback
var resources = Application.Current?.Resources;
double actualHeight = DaisyResourceLookup.GetDouble(resources, "DaisySizeMediumHeight", height);
```

### Available Helper Methods

| Method | Returns | Description |
| --- | --- | --- |
| `GetDefaultHeight(size)` | double | Standard height: desktop 24-40, mobile 36-56 |
| `GetDefaultFloatingInputHeight(size)` | double | Floating input height: desktop 36-50, mobile 44-64 |
| `GetDefaultFontSize(size)` | double | Primary font size (10-20) |
| `GetDefaultLineHeight(size)` | double | Primary line height (14-28) |
| `GetDefaultHeaderFontSize(size)` | double | Large display font size (14-28) |
| `GetDefaultPadding(size)` | Thickness | Horizontal button padding |
| `GetDefaultCornerRadius(size)` | CornerRadius | Rounded corners (4-12) |
| `GetDefaultIconSize(size)` | double | Icon dimensions (8-24) |
| `GetDefaultIconSpacing(size)` | double | Gap between icon and text (4-8) |
| `GetDefaultMenuPadding(size)` | Thickness | Menu/list item padding |
| `GetDefaultTagPadding(size)` | Thickness | Tag/chip padding |
| `GetDefaultRangeTrackHeight(size)` | double | Range slider track (4-16) |
| `GetDefaultRangeThumbSize(size)` | double | Range slider thumb (12-36) |
| `GetDefaultRatingStarSize(size)` | double | Rating star dimensions (16-40) |
| `GetDefaultRatingSpacing(size)` | double | Rating star gap (2-8) |
| `GetDefaultRadialProgressSize(size)` | double | Radial progress dimensions (24-128) |
| `GetDefaultOtpSlotSize(size)` | double | OTP input fallback size (32-64); token values can differ |
| `GetDefaultOtpSpacing(size)` | double | OTP slot spacing (4-10) |
| `GetDefaultSwatchSize(size)` | double | Color swatch/dot size for theme pickers (6-14) |
| `GetAvatarSize(size)` | double | Reads avatar tokens (20-80); fallback range 22-80 |
| `GetAvatarStatusSize(size)` | double | Status indicator size (5-16) |
| `GetAvatarFontSize(size)` | double | Avatar placeholder font (7-20) |
| `GetAvatarIconSize(size)` | double | Avatar icon size (10-34) |

## Token Reference

### Control Heights

Used for buttons, inputs, selects, and other fixed-height controls.

| Token | XS | S | M | L | XL | Used By |
| ----- | -- | -- | -- | -- | --- | ------- |
| `DaisySize{Size}Height` desktop | 24 | 28 | 32 | 36 | 40 | DaisyButton, DaisyInput, DaisySelect |
| `DaisySize{Size}Height` mobile | 36 | 40 | 44 | 48 | 56 | Larger touch targets |

`{Size}` denotes the full tier name: ExtraSmall, Small, Medium, Large or ExtraLarge.
For automatically sized controls, resolve `Size` before token lookup. See [Size ownership](FlowerySizeManager.md#size-ownership-and-precedence).

### Floating Input Heights

Specific heights for taller input controls like `DaisyNumericUpDown` and `DaisyFileInput`.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyInputFloating{Size}Height` | 36 | 40 | 44 | 48 | 50 |

The mobile profile uses 44/48/52/56/64 for this family.

### Font Sizes

The primary font ladder matches Flowery.NET. Other heading tiers can differ by platform.

#### Primary Font Sizes

Typography scale used across controls for main content.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}FontSize` | 10 | 12 | 14 | 18 | 20 |
| `DaisySize{Size}LineHeight` | 14 | 16 | 20 | 24 | 28 |

#### Secondary Font Sizes

For hint text, helper text, captions, labels (~0.8x of primary).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}SecondaryFontSize` | 9 | 10 | 12 | 14 | 16 |

#### Tertiary Font Sizes

For very small captions, counters (~0.7x of primary).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}TertiaryFontSize` | 8 | 9 | 11 | 12 | 14 |

#### Section Header Font Sizes

For section titles within pages.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}SectionHeaderFontSize` | 12 | 14 | 16 | 20 | 22 |

#### Header Font Sizes

For large display headings (~1.4x of primary).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}HeaderFontSize` | 14 | 16 | 20 | 24 | 28 |

### Corner Radius

Rounded corners for controls.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySize{Size}CornerRadius` | 4 | 4 | 8 | 8 | 12 |

### Button Padding

Horizontal-only padding (vertical is controlled by Height).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyButton{Size}Padding` | 8,0 | 12,0 | 12,0 | 16,0 | 16,0 |

### Input Padding

Full padding for text inputs.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyInput{Size}Padding` | 8,4 | 12,4 | 12,8 | 16,8 | 16,8 |

### Tab Padding

Both horizontal and vertical padding (tabs don't use fixed height).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyTab{Size}Padding` | 8,4 | 12,4 | 12,4 | 12,4 | 12,4 |

### Badge Tokens

Badge text uses the primary font ladder.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyBadge{Size}Height` | 16 | 20 | 24 | 28 | 32 |
| `DaisyBadge{Size}FontSize` | 10 | 12 | 14 | 18 | 20 |
| `DaisyBadge{Size}Padding` | 4,0 | 6,0 | 8,0 | 12,0 | 16,0 |

### Card Padding

| Token | Value |
| --- | --- |
| `DaisyCardLargePadding` | 24 |
| `DaisyCardMediumPadding` | 20 |
| `DaisyCardSmallPadding` | 12 |
| `DaisyCardCompactPadding` | 8 |

### Spacing

General spacing values for layouts.

| Token | Value |
| --- | --- |
| `DaisySpacingXL` | 20 |
| `DaisySpacingLarge` | 16 |
| `DaisySpacingMedium` | 12 |
| `DaisySpacingSmall` | 8 |
| `DaisySpacingXS` | 4 |

### Border Thickness

| Token | Value |
| --- | --- |
| `DaisyBorderThicknessNone` | 0 |
| `DaisyBorderThicknessThin` | 1 |
| `DaisyBorderThicknessMedium` | 2 |
| `DaisyBorderThicknessThick` | 3 |

---

## Checkbox/Radio Indicator Sizes

Each indicator is as tall as the line height of its label text in the Windows type ramp (Caption 12/16, Body 14/20, Body Large 18/24, Subtitle 20/28; 14 for 10 px text), so it never sticks out of the text line. Medium matches the WinUI defaults (`CheckBoxSize` 20 with a 12 px glyph, `RadioButton` 20 with a 12 px glyph); checkmarks and radio dots keep the WinUI ratio of about 0.6.

### Checkbox Sizes

Outer border size for checkboxes.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyCheckbox{Size}Size` | 14 | 16 | 20 | 24 | 28 |

### Checkmark Icon Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyCheckmark{Size}Size` | 8 | 10 | 12 | 14 | 17 |

### Radio Sizes

Radio buttons use their own outer size tokens (independent of the checkbox sizes).

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyRadio{Size}Size` | 14 | 16 | 20 | 24 | 28 |

### Radio Inner Dot Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyRadioDot{Size}Size` | 8 | 10 | 12 | 14 | 16 |

### Label Font Sizes

Checkbox, radio and toggle labels use the primary font ladder.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyCheckbox{Size}FontSize` | 10 | 12 | 14 | 18 | 20 |
| `DaisyRadio{Size}FontSize` | 10 | 12 | 14 | 18 | 20 |
| `DaisyToggle{Size}FontSize` | 10 | 12 | 14 | 18 | 20 |

---

## Toggle Switch Sizes

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyToggle{Size}Width` | 28 | 32 | 40 | 48 | 56 |
| `DaisyToggle{Size}Height` | 14 | 16 | 20 | 24 | 28 |

The track is as tall as the checkbox of its tier. Medium uses the WinUI `ToggleSwitch` track (40×20) and knob (12 px); all tiers keep its proportions (width = 2 × height, knob about 0.6 × height).

### Toggle Knob Sizes

With the automatic default `TogglePadding`, the knob is centered with the same gap on every side.

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyToggleKnob{Size}Size` | 8 | 10 | 12 | 14 | 16 |

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
| `DaisyMenu{Size}FontSize` | 10 | 12 | 14 | 18 | 20 |

---

## Kbd (Keyboard) Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyKbd{Size}Height` | 16 | 20 | 24 | 32 | 40 |
| `DaisyKbd{Size}Padding` | 2,0 | 3,0 | 4,0 | 6,0 | 8,0 |
| `DaisyKbd{Size}FontSize` | 10 | 12 | 14 | 18 | 20 |
| `DaisyKbd{Size}CornerRadius` | 2 | 3 | 4 | 5 | 6 |

---

## TextArea Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyTextArea{Size}MinHeight` | 48 | 60 | 80 | 120 | 160 |
| `DaisyTextArea{Size}Padding` | 6 | 10 | 12 | 14 | 18 |

---

## DateTimeline Tokens

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisyDateTimeline{Size}Height` | 56 | 68 | 80 | 96 | 130 |
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
| `DaisyDivider{Size}FontSize` | 10 | 12 | 14 | 18 | 20 |
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

Use the public `DaisyResourceLookup.EnsureTokens()` helper; `DaisyTokenDefaults` is internal.
For a `DaisyBaseContentControl` subclass, apply the concrete `ResolvedSize`:

```csharp
private void ApplySizing()
{
    Height = DaisyResourceLookup.GetDefaultHeight(ResolvedSize);
    FontSize = DaisyResourceLookup.GetDefaultFontSize(ResolvedSize);
    Padding = DaisyResourceLookup.GetDefaultPadding(ResolvedSize);
}
```

Register the authored Size with Auto metadata and refresh generated visuals through the existing lifecycle.
See the [custom-control contract](FlowerySizeManager.md#custom-controls); do not replace an authored Size from a global callback.

### Pattern 2: Read from ResourceDictionary with Fallback

For maximum flexibility (allows token overrides):

```csharp
private void ApplySizing()
{
    var resources = Application.Current?.Resources;
    DaisyResourceLookup.EnsureTokens(resources);

    var size = ResolvedSize;
    string sizeKey = DaisyResourceLookup.GetSizeKeyFull(size);

    // Try resource dictionary first, fallback to centralized default
    Height = DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}Height",
        DaisyResourceLookup.GetDefaultHeight(size));
    FontSize = DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}FontSize",
        DaisyResourceLookup.GetDefaultFontSize(size));
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

# Lightweight Styling Resources

> ✅ **IMPLEMENTED** for core and secondary controls
>
> This document describes the resource naming convention for lightweight styling in Flowery.Uno.
> See [STYLING_IMPLEMENTATION_PLAN.md](../!todos/STYLING_IMPLEMENTATION_PLAN.md) for full status.

---

## Overview

Lightweight styling allows users to customize control colors without subclassing or modifying C# code. By defining specific resource keys in your application's `ResourceDictionary`, you can override individual control colors while keeping the rest of the DaisyUI theming intact.

## Resource Naming Convention

All control-specific resources follow this pattern:

```xml
Daisy{Control}{Variant?}{Part?}{State?}{Property}
```

| Component | Description | Examples |
| --- | --- | --- |
| `Control` | The control name | `Button`, `Input`, `Modal`, `Alert` |
| `Variant` | Optional color variant | `Primary`, `Secondary`, `Success`, `Error` |
| `Part` | Optional visual part | `Border`, `Track`, `Knob`, `Icon` |
| `State` | Optional interaction state | `PointerOver`, `Pressed`, `Focused`, `Disabled` |
| `Property` | The visual property | `Background`, `Foreground`, `BorderBrush` |

## Examples

### DaisyButton Resources

```xml
DaisyButtonBackground                     ← Default background
DaisyButtonForeground                     ← Default foreground (text/icon color)
DaisyButtonBorderBrush                    ← Default border color

DaisyButtonBackgroundPointerOver          ← Hover state background
DaisyButtonBackgroundPressed              ← Pressed state background
DaisyButtonBackgroundDisabled             ← Disabled state background

DaisyButtonPrimaryBackground              ← Primary variant background
DaisyButtonPrimaryForeground              ← Primary variant foreground
DaisyButtonPrimaryBackgroundPointerOver   ← Primary variant hover background

DaisyButtonSecondaryBackground            ← Secondary variant background
DaisyButtonSuccessBackground              ← Success variant background
DaisyButtonWarningBackground              ← Warning variant background
DaisyButtonErrorBackground                ← Error variant background
```

### DaisyInput Resources

```xml
DaisyInputBackground                      ← Input background
DaisyInputBorderBrush                     ← Default border color
DaisyInputBorderBrushFocused              ← Border color when focused
DaisyInputForeground                      ← Text color
DaisyInputPlaceholderForeground           ← Placeholder text color

DaisyInputPrimaryBorderBrush              ← Primary variant border
DaisyInputErrorBorderBrush                ← Error variant border
```

### DaisyModal Resources

```xml
DaisyModalBackdropBrush                   ← Semi-transparent overlay
DaisyModalBackground                      ← Dialog background
DaisyModalCornerRadius                    ← Dialog corner radius (CornerRadius type)
```

### DaisyAlert Resources

```xml
DaisyAlertInfoBackground                  ← Info variant background
DaisyAlertInfoForeground                  ← Info variant text color
DaisyAlertInfoIconBrush                   ← Info variant icon color

DaisyAlertSuccessBackground               ← Success variant background
DaisyAlertSuccessForeground               ← Success variant text color
DaisyAlertSuccessIconBrush                ← Success variant icon color

DaisyAlertWarningBackground               ← Warning variant background
DaisyAlertWarningForeground               ← Warning variant text color
DaisyAlertWarningIconBrush                ← Warning variant icon color

DaisyAlertErrorBackground                 ← Error variant background
DaisyAlertErrorForeground                 ← Error variant text color
DaisyAlertErrorIconBrush                  ← Error variant icon color
```

### DaisySelect Resources

```xml
DaisySelectBackground                     ← Select background
DaisySelectForeground                     ← Select text color
DaisySelectBorderBrush                    ← Default border color

DaisySelectPrimaryBorderBrush             ← Primary variant border
DaisySelectSecondaryBorderBrush           ← Secondary variant border
DaisySelectAccentBorderBrush              ← Accent variant border
DaisySelectInfoBorderBrush                ← Info variant border
DaisySelectSuccessBorderBrush             ← Success variant border
DaisySelectWarningBorderBrush             ← Warning variant border
DaisySelectErrorBorderBrush               ← Error variant border
```

### DaisyToggle Resources

```xml
DaisyToggleTrackBackground                ← Toggle track background
DaisyToggleTrackBorderBrush               ← Toggle track border
DaisyToggleKnobBrush                      ← Generic checked knob color
DaisyToggleKnobBrushUnchecked             ← Knob color when unchecked

DaisyTogglePrimaryKnobBrush               ← Primary variant knob color
DaisyToggleSecondaryKnobBrush             ← Secondary variant knob color
DaisyToggleAccentKnobBrush                ← Accent variant knob color
DaisyToggleSuccessKnobBrush               ← Success variant knob color
DaisyToggleWarningKnobBrush               ← Warning variant knob color
DaisyToggleInfoKnobBrush                  ← Info variant knob color
DaisyToggleErrorKnobBrush                 ← Error variant knob color
```

### DaisyCheckBox Resources

```xml
DaisyCheckBoxBackground                   ← Checked background (generic)
DaisyCheckBoxCheckmark                    ← Checkmark stroke color (generic)
DaisyCheckBoxBorderBrush                  ← Unchecked border color

DaisyCheckBoxPrimaryBackground            ← Primary variant background
DaisyCheckBoxPrimaryCheckmark             ← Primary variant checkmark
DaisyCheckBoxSecondaryBackground          ← Secondary variant background
DaisyCheckBoxSuccessBackground            ← Success variant background
```

### DaisyRadio Resources

```xml
DaisyRadioAccentBrush                     ← Generic checked accent color
DaisyRadioBorderBrush                     ← Unchecked border color

DaisyRadioPrimaryAccentBrush              ← Primary variant accent
DaisyRadioSecondaryAccentBrush            ← Secondary variant accent
DaisyRadioSuccessAccentBrush              ← Success variant accent
```

### DaisyProgress Resources

```xml
DaisyProgressTrackBrush                   ← Progress bar track background
DaisyProgressFillBrush                    ← Generic fill color

DaisyProgressPrimaryFillBrush             ← Primary variant fill
DaisyProgressSecondaryFillBrush           ← Secondary variant fill
DaisyProgressSuccessFillBrush             ← Success variant fill
DaisyProgressWarningFillBrush             ← Warning variant fill
DaisyProgressErrorFillBrush               ← Error variant fill
```

### DaisyRadialProgress Resources

```xml
DaisyRadialProgressTrackBrush             ← Circular track stroke
DaisyRadialProgressArcBrush               ← Generic arc (progress) stroke
DaisyRadialProgressTextBrush              ← Center text color

DaisyRadialProgressPrimaryArcBrush        ← Primary variant arc
DaisyRadialProgressSuccessArcBrush        ← Success variant arc
```

### DaisyBadge Resources

```xml
DaisyBadgeBackground                      ← Generic badge background
DaisyBadgeForeground                      ← Generic badge text color
DaisyBadgeBorderBrush                     ← Badge border (solid style)

DaisyBadgePrimaryBackground               ← Primary variant background
DaisyBadgePrimaryForeground               ← Primary variant text
DaisyBadgeSuccessBackground               ← Success variant background
DaisyBadgeErrorBackground                 ← Error variant background

DaisyBadgeOutlineBackground               ← Outline style background
DaisyBadgeOutlineBorderBrush              ← Outline style border
DaisyBadgeOutlineForeground               ← Outline style text
DaisyBadgePrimaryOutlineBorderBrush       ← Primary outline border
DaisyBadgePrimaryOutlineForeground        ← Primary outline text
```

### DaisyCard Resources

```xml
DaisyCardBackground                       ← Card background
DaisyCardForeground                       ← Card text color
```

### DaisyRange Resources

```xml
DaisyRangeTrackBrush                      ← Slider track background
DaisyRangeFillBrush                       ← Generic fill color
DaisyRangeThumbBrush                      ← Generic thumb color

DaisyRangePrimaryFillBrush                ← Primary variant fill
DaisyRangePrimaryThumbBrush               ← Primary variant thumb
DaisyRangeSuccessFillBrush                ← Success variant fill
```

### DaisyAvatar Resources

```xml
DaisyAvatarBackground                     ← Avatar placeholder background
DaisyAvatarForeground                     ← Avatar placeholder text/icon color
```

### DaisyDivider Resources

```xml
DaisyDividerLineBrush                     ← Generic line color
DaisyDividerTextForeground                ← Divider text color
DaisyDividerTextBackground                ← Divider text background

DaisyDividerPrimaryLineBrush              ← Primary variant line
DaisyDividerSuccessLineBrush              ← Success variant line
```

### DaisyKbd Resources

```xml
DaisyKbdBackground                        ← Keyboard key background
DaisyKbdForeground                        ← Keyboard key text
DaisyKbdBorderBrush                       ← Keyboard key border
```

### DaisyTooltip Resources

```xml
DaisyTooltipBackground                    ← Generic tooltip background
DaisyTooltipForeground                    ← Generic tooltip text

DaisyTooltipPrimaryBackground             ← Primary variant background
DaisyTooltipPrimaryForeground             ← Primary variant text
```

### DaisyStat Resources

```xml
DaisyStatTitleForeground                  ← Stat title text color
DaisyStatValueForeground                  ← Stat value text color
DaisyStatDescriptionForeground            ← Stat description text color
```

### DaisyChatBubble Resources

```xml
DaisyChatBubbleBackground                 ← Generic bubble background
DaisyChatBubbleForeground                 ← Generic bubble text
DaisyChatBubbleHeaderForeground           ← Header text color
DaisyChatBubbleFooterForeground           ← Footer text color

DaisyChatBubblePrimaryBackground          ← Primary variant background
DaisyChatBubblePrimaryForeground          ← Primary variant text
```

### DaisyDrawer Resources

```xml
DaisyDrawerBackground                     ← Drawer pane background
```

### DaisyPopover Resources

```xml
DaisyPopoverBackground                    ← Popover background
DaisyPopoverBorderBrush                   ← Popover border
```

### DaisyAccordionItem Resources

```xml
DaisyAccordionItemHeaderBackground        ← Accordion header background
DaisyAccordionItemHeaderBorderBrush       ← Accordion header border
DaisyAccordionItemForeground              ← Accordion header/indicator text
```

### DaisyTimelineItem Resources

```xml
DaisyTimelineItemIndicatorBrush           ← Timeline dot (inactive)
DaisyTimelineItemActiveBrush              ← Active indicator/connector color
DaisyTimelineItemConnectorBrush           ← Connecting line color
DaisyTimelineItemBoxBackground            ← Boxed content background
DaisyTimelineItemBoxBorderBrush           ← Boxed content border
```

### DaisyHero Resources

```xml
DaisyHeroBackground                       ← Hero section background
```

### DaisyFileInput Resources

```xml
DaisyFileInputBackground                  ← File input container background
DaisyFileInputBorderBrush                 ← File input container border
DaisyFileInputForeground                  ← File name text color
```

### DaisyStepItem Resources

```xml
DaisyStepItemIndicatorBrush               ← Step indicator (inactive)
DaisyStepItemConnectorBrush               ← Connecting line color
DaisyStepItemLabelForeground              ← Step label text color
```

### DaisyBreadcrumbItem Resources

```xml
DaisyBreadcrumbItemForeground             ← Text color (non-clickable items)
DaisyBreadcrumbItemLinkForeground         ← Link text color (clickable items)
DaisyBreadcrumbItemSeparatorForeground    ← Separator text color
```

### DaisyCountdown Resources

```xml
DaisyCountdownForeground                  ← Countdown value text color
```

### DaisyList Resources

```xml
DaisyListBackground                       ← List container background
```

### DaisyListRow Resources

```xml
DaisyListRowBorderBrush                   ← Row separator border
DaisyListRowHoverBackground               ← Row hover background
```

### DaisySwap Resources

```xml
DaisySwapBackground                       ← Swap container background
DaisySwapBorderBrush                      ← Swap container border
```

### DaisyOtpInput Resources

```xml
DaisyOtpInputBackground                   ← Slot background
DaisyOtpInputBorderBrush                  ← Slot border
DaisyOtpInputForeground                   ← Digit text color
```

### DaisyMenuItem Resources

```xml
DaisyMenuItemForeground                   ← Default text color
DaisyMenuItemActiveForeground             ← Active item text color
DaisyMenuItemActiveBackground             ← Active item background
```

### DaisyCarousel Resources

```xml
DaisyCarouselNavigationForeground         ← Navigation button icon color
```

### DaisyAnimatedNumber Resources

```xml
DaisyAnimatedNumberForeground             ← Number text color
```

### DaisyDock Resources

```xml
DaisyDockBackground                       ← Dock container background
DaisyDockBorderBrush                      ← Dock container border
```

### DaisyTable Resources

```xml
DaisyTableBackground                      ← Table container background
DaisyTableBorderBrush                     ← Table container border
```

### DaisyTableRow Resources

```xml
DaisyTableRowBorderBrush                  ← Row separator border
DaisyTableRowForeground                   ← Row text color
DaisyTableRowActiveBackground             ← Active row background
DaisyTableRowActiveForeground             ← Active row text color
DaisyTableRowHeaderBackground             ← Header row background
DaisyTableRowHoverBackground              ← Row hover background
```

### DaisyNumericUpDown Resources

```xml
DaisyNumericUpDownBackground              ← Input background
DaisyNumericUpDownBorderBrush             ← Input border
DaisyNumericUpDownForeground              ← Text and icon color
```

### DaisyButtonGroup Resources

```xml
DaisyButtonGroupSegmentBackground         ← Segment background color
DaisyButtonGroupSegmentForeground         ← Segment text color
DaisyButtonGroupSegmentBorderBrush        ← Segment divider border
```

### DaisyTabs Resources

```xml
DaisyTabsBackground                       ← Tab strip background (Boxed variant)
DaisyTabsBorderBrush                      ← Tab strip border (Bordered variant)
DaisyTabsActiveBackground                 ← Active tab button background
DaisyTabsActiveForeground                 ← Active tab button text color
DaisyTabsForeground                       ← Inactive tab button text color
```

## Usage

You can override resources at multiple levels:

### Application-Level Override

Override in `App.xaml` to affect all instances of a control:

```xml
<Application.Resources>
    <!-- Make all Primary buttons use a custom purple -->
    <SolidColorBrush x:Key="DaisyButtonPrimaryBackground" Color="#8B5CF6" />
    <SolidColorBrush x:Key="DaisyButtonPrimaryBackgroundPointerOver" Color="#7C3AED" />
</Application.Resources>
```

### Control-Level Override (Lightweight Styling)

Override in the control's `Resources` to affect only that instance:

```xml
<daisy:DaisyButton Content="Special Button" Variant="Primary">
    <daisy:DaisyButton.Resources>
        <SolidColorBrush x:Key="DaisyButtonPrimaryBackground" Color="#EC4899" />
    </daisy:DaisyButton.Resources>
</daisy:DaisyButton>
```

## Lookup Priority

Resources are resolved in this order:

1. **Control.Resources** (lightweight styling on specific instance)
2. **Application.Current.Resources** (app-level overrides)
3. **Fallback to palette logic** (current behavior using `DaisyPrimaryBrush`, etc.)

This ensures backward compatibility - existing code continues to work, while new users can opt into resource-based customization.

---

## Related Documentation

- [THEMING.md](../THEMING.md) - Theme palette and switching
- [STYLING_ASSESSMENT.md](../!todos/STYLING_ASSESSMENT.md) - Analysis of current styling patterns
- [STYLING_IMPLEMENTATION_PLAN.md](../!todos/STYLING_IMPLEMENTATION_PLAN.md) - Implementation roadmap

# FlowerySizeManager

`FlowerySizeManager` selects the application-wide tier for controls whose `Size` is `Auto`.
It refreshes loaded controls without assigning their Size dependency property.

## Quick Start

```csharp
using Flowery.Controls;
using Flowery.Enums;
FlowerySizeManager.ApplySize(DaisySize.Large);
```

```xml
<controls:DaisyButton Content="Follows global size" />
<controls:DaisyButton Size="Medium" Content="Always Medium" />
<controls:DaisySizeDropdown />
```

## Size Ownership and Precedence

`DaisySize.Auto = -1` is the default value of control Size properties.
The concrete values remain unchanged: ExtraSmall = 0 through ExtraLarge = 4.
Auto requests resolution; it is not a sixth visual tier.

Read `ResolvedSize` to obtain the effective tier:

1. A concrete Size wins, whether supplied by XAML, code, a style or a binding.
2. An Auto child in a sizing composite uses the composite's explicit size or calculated child size.
3. Otherwise, Auto uses CurrentSize when global defaults are enabled and the control does not opt out.
4. Without global defaults, Auto uses the nearest composite fallback, if provided. An automatic Fab
   supplies Large to its automatic descendants. Otherwise, the control uses its fallback: normally Medium; Small for Divider, ModifierKeys,
   TabsHeaderButtons and TagPicker; Large for Fab.

Composite defaults apply to groups that size their children, such as ButtonGroup, AvatarGroup,
List, Table, Steps, Dock and Fab. WeatherForecast supplies its calculated size after auto-fit.
Explicit child sizes remain authoritative. Changing a composite's Size refreshes loaded automatic
descendants through intermediate layout containers without assigning their Size properties.

Normal dependency-property precedence applies. A local `Size="Auto"` overrides a style's concrete
size and requests automatic resolution. `ClearValue(SizeProperty)` restores the style or metadata
value. Global changes never create a local Size value or replace a binding.

**Migration:** Code reading an unset Size now receives Auto. Use ResolvedSize for layout and token
lookups. Set a concrete Size when the control must remain at a fixed tier.

## Configuration and Lifetime

| Member | Default | Behavior |
| --- | --- | --- |
| `CurrentSize` | Medium | Current concrete global tier; read-only. |
| `UseGlobalSizeByDefault` | true | Enables global fallback for Auto controls. A change immediately refreshes loaded controls. |
| `EnableGlobalAutoSize` | true | Enables automatic visual refresh after ApplySize. Re-enabling it catches up loaded controls. |
| `MainWindow` | null | Retained application window reference. Size propagation does not require it. |

With EnableGlobalAutoSize set to false, ApplySize still updates CurrentSize, raises SizeChanged,
and changes ResolvedSize. Existing visuals update on RefreshAllSizes, another normal visual refresh,
or reload. This switch does not freeze the resolved value. New controls initialize from current policy.

Controls register while loaded and unregister on unload. The manager does not retain detached
controls. Reattached controls catch up with the current tier. Registration works across window and
popup roots without a walk from MainWindow.

```csharp
FlowerySizeManager.EnableGlobalAutoSize = false;
FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
FlowerySizeManager.RefreshAllSizes();
FlowerySizeManager.EnableGlobalAutoSize = true;
FlowerySizeManager.Reset(); // Selects Small; initial CurrentSize is Medium.
```

ApplySize accepts concrete enum values only. Auto and invalid values throw ArgumentOutOfRangeException.
There is no string overload.

## IgnoreGlobalSize

The nearest explicitly assigned IgnoreGlobalSize on the control or an ancestor wins.
A local false lets a child opt back in. Changing the property refreshes loaded controls.

```xml
<StackPanel controls:FlowerySizeManager.IgnoreGlobalSize="True">
    <controls:DaisyButton Content="Uses Medium fallback" />
    <controls:DaisyButton Size="Large" Content="Always Large" />
    <controls:DaisyButton controls:FlowerySizeManager.IgnoreGlobalSize="False"
                          Content="Uses global tier" />
</StackPanel>
```

Concrete Size values do not require IgnoreGlobalSize. This property controls global fallback;
it does not cancel an explicit composite size.

`SetIgnoreGlobalSize(control, false)` also refreshes an already loaded control when the new local
false overrides an ignored ancestor. If you remove that local false with `ClearValue`, call
`RefreshAllSizes()` afterward: the native Boolean can remain false while its ownership changes.

## Responsive Text

```xml
<TextBlock Text="Body" controls:FlowerySizeManager.ResponsiveFont="Primary" />
<TextBlock Text="Hint" controls:FlowerySizeManager.ResponsiveFont="Secondary" />
<TextBlock Text="Heading" controls:FlowerySizeManager.ResponsiveFont="Header" />
```

Supported tiers are None, Primary, Secondary, Tertiary, SectionHeader and Header.
Values come from design tokens through GetFontSizeForTier. Responsive text is tracked while loaded
and catches up after reattachment. IgnoreGlobalSize suppresses subsequent global updates.
Setting ResponsiveFont explicitly applies its current tier immediately.

## Heights and Tokens

The primary font defaults are 10/12/14/18/20 for ExtraSmall through ExtraLarge.
The [Badge defaults](DaisyBadge.md) are 16/20/24/28/32 in height and use the same primary font ladder, including XL.

GetControlHeight returns fixed heights of 24, 28, 32, 34 and 36 for ExtraSmall through ExtraLarge.
Unknown values return 32. This helper does not read token overrides or mobile-specific heights.
It differs from `DaisyResourceLookup.GetDefaultHeight`: desktop 24/28/32/36/40, mobile 36/40/44/48/56.

For control rendering, pass ResolvedSize to DaisyResourceLookup:

```csharp
var height = DaisyResourceLookup.GetDefaultHeight(button.ResolvedSize);
var fontSize = DaisyResourceLookup.GetDefaultFontSize(button.ResolvedSize);
```

See [Design Tokens](DesignTokens.md) and [DaisyResourceLookup](DaisyResourceLookup.md)
for token overrides and platform-specific dimensions.

`GetSidebarWidth` returns 190/205/220/235/250 DIP for the five concrete tiers; other values return 220.
This helper does not include DPI calculations or safe-area Insets. A host can own its sidebar width and add its own horizontal Insets.

## Custom Controls

A control derived from [DaisyBaseContentControl](DaisyBaseContentControl.md) participates in
registration automatically. Register its Size dependency property with Auto metadata, return the
authored value from GetControlSize, and use ResolvedSize when applying layout:

```csharp
protected override DaisySize? GetControlSize() => Size;

protected override void OnResolvedSizeChanged()
{
    base.OnResolvedSizeChanged();
    ApplySizing();
}
```

The Size property-change callback must also call ApplySizing. Override DefaultSize for a fallback
other than Medium. Do not assign Size from a global callback.

Other custom controls can use ResolveSize(owner, requested, fallback) and manage their own
Loaded/Unloaded subscriptions. The resolver is a read operation; it does not register for refreshes.

## Notifications and the Size Picker

SizeChanged is raised when the concrete global tier changes. It is a notification, not a command
to overwrite control properties. Application subscribers must unsubscribe when no longer needed.

[DaisySizeDropdown](DaisySizeDropdown.md) selects one of the five concrete tiers. SelectedSize
tracks that selection independently of the dropdown's own visual Size, which may be fixed or Auto.

## Continuous Scaling

[FloweryScaleManager](FloweryScaleManager.md) handles separate, opt-in continuous scaling.
This global sizing contract does not change its platform limits or behavior.

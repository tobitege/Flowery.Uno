<!-- Supplementary documentation for DaisyStat -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyStat displays a metric with title, value, optional description, figure (icon/media), and action slot. `DaisyStats` groups multiple stats horizontally or vertically, adding separators between items. Variants color the value text; `DescriptionVariant` colors the description independently.

## DaisyStat Properties

| Property | Description |
| --- | --- |
| `Title` | Small label above the value. |
| `Value` | Main metric (large text). |
| `Description` | Secondary text below the value. |
| `Figure` | Optional leading visual (icon, image). |
| `Actions` | Optional trailing actions (e.g., buttons). |
| `IsCentered` | Centers text and actions within the stat. |
| `Variant` | Colors value text (Default/Primary/Secondary/Accent/Info/Success/Warning/Error). |
| `DescriptionVariant` | Colors description text using the same variant set. |
| `Size` | Size tier; `Auto` follows the containing size scope or global size. Read `ResolvedSize` for the concrete tier. |
| `ValueFontSize` (`double`) | Value text size. `NaN` (default) follows the resolved tier. Set a positive number to override it. |
| `LabelFontSize` (`double`) | Shared title and description text size. `NaN` (default) follows the resolved secondary font tier. |

## Text Brushes

The title and default description use `DaisyBaseContentMutedBrush` at element opacity 1.
An explicit `DaisyStatTitleForeground` or `DaisyStatDescriptionForeground` resource replaces that brush without additional dimming.
`DescriptionVariant` selects the corresponding semantic color. The value retains its existing variant color.

## Typography

Automatic typography uses these defaults:

| Resolved size | Value font size | Title and description font size |
| --- | --- | --- |
| ExtraSmall | 18 | 9 |
| Small | 20 | 10 |
| Medium | 24 | 12 |
| Large | 28 | 14 |
| ExtraLarge | 32 | 16 |

Value defaults read `DaisyStat{Tier}ValueFontSize` resources when supplied. Labels use `DaisySize{Tier}SecondaryFontSize` resources. See [size precedence](FlowerySizeManager.md#size-ownership-and-precedence) for automatic and explicit control sizes.

Local values, explicit or implicit style setters, and bindings on either font property take precedence over these defaults. The control never writes calculated defaults into the override properties. Their raw value remains `NaN` while automatic typography is active.

Set an override to `double.NaN` to resume automatic sizing. Use `ClearValue(DaisyStat.ValueFontSizeProperty)` or `ClearValue(DaisyStat.LabelFontSizeProperty)` to restore the applicable style value, or automatic sizing when no style value applies. Size changes, theme refreshes and reloads update the existing text elements.

```xml
<!-- Fixed typography while the control uses its Large size tier -->
<controls:DaisyStat Title="Users" Value="4,200" Description="Active accounts"
                    Size="Large" ValueFontSize="40" LabelFontSize="14" />
```

## DaisyStats Container

| Property | Description |
| --- | --- |
| `Orientation` | Horizontal (default) or Vertical; adds dividers between items. |

Supply multiple stats inside a panel such as `StackPanel`; the container moves those controls into its internal panel on first load.

## Quick Examples

```xml
<!-- Simple stats -->
<controls:DaisyStats>
    <StackPanel>
        <controls:DaisyStat Title="Total Page Views" Value="89,400" Description="21% more than last month" />
        <controls:DaisyStat Title="Downloads" Value="31K" Description="Jan 1st - Feb 1st" Variant="Primary" />
        <controls:DaisyStat Title="New Users" Value="4,200" Description="+400 (22%)" Variant="Secondary" />
    </StackPanel>
</controls:DaisyStats>

<!-- With figure and actions -->
<controls:DaisyStat Title="Account balance" Value="$89,400">
    <controls:DaisyStat.Figure>
        <PathIcon Data="{StaticResource DaisyIconStar}" Width="32" Height="32"
                  Foreground="{ThemeResource DaisyWarningBrush}" />
    </controls:DaisyStat.Figure>
    <controls:DaisyStat.Actions>
        <controls:DaisyButton Size="ExtraSmall" Variant="Success" Content="Add funds" />
    </controls:DaisyStat.Actions>
</controls:DaisyStat>

<!-- Vertical grouping and centered -->
<controls:DaisyStats Orientation="Vertical">
    <StackPanel>
        <controls:DaisyStat Title="Downloads" Value="31K" Description="Jan 1st - Feb 1st" IsCentered="True" />
        <controls:DaisyStat Title="Users" Value="4,200" Description="+40 (2%)" Variant="Secondary" DescriptionVariant="Secondary" IsCentered="True" />
    </StackPanel>
</controls:DaisyStats>
```

## Tips & Best Practices

- Use `DescriptionVariant` to align secondary text with status (e.g., Success for growth, Error for decline).
- Center stats (`IsCentered=True`) when presenting small sets or when values/figures need emphasis.
- For long lists, prefer `Orientation="Vertical"` and ensure consistent padding for readability.
- Keep titles short; use `Figure` to add meaning without overloading text.

<!-- Supplementary documentation for DaisyAccordion -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyAccordion displays stacked sections supplied as `DaisyAccordionItem` children of a content panel. It keeps one item expanded through `ExpandedIndex`. It is a content control, not an `ItemsControl`; it does not generate items from data.

## Variant Options

| Variant | Description |
| --- | --- |
| **Arrow (default)** | Shows a down chevron when collapsed and an up chevron when expanded. |
| **Plus** | Shows a plus when collapsed and a minus when expanded. |
| **None** | Hides the indicator. |

## Size and Ownership

Both controls expose `Size`, which defaults to `Auto`, and `ResolvedSize`, which reports the concrete rendering tier. An explicit accordion size supplies a default for its items. An item's own concrete size, including a style setter or binding, takes precedence. Automatic items otherwise follow the containing size scope or global size; they fall back to Medium when global sizing is disabled. See [size precedence](FlowerySizeManager.md#size-ownership-and-precedence).

| Tier | Header/body padding | Header minimum height | Default header font size |
| --- | --- | --- | --- |
| ExtraSmall | 8 | 32 | 10 |
| Small | 12 | 40 | 12 |
| Medium | 16 | 48 | 14 |
| Large | 20 | 56 | 18 |
| ExtraLarge | 24 | 64 | 20 |

The generated header uses the primary font token for its resolved tier. Padding and minimum height use `DaisyAccordion{Tier}Padding` and `DaisyAccordion{Tier}HeaderMinHeight` resources when supplied, with the table values as fallbacks. The indicator remains 14 by 14.

The accordion never writes an item's `Size`, `FontSize` or `Padding`. Set `Size` to `Auto` to follow the parent or global default. Clear its local value to restore an applicable style value or automatic sizing. Inherited `Padding` remains outer control spacing. Inherited `FontSize` remains available to body content; it does not override the generated header's tier-based typography.

## Expansion Behavior

| Property | Description |
| --- | --- |
| `ExpandedIndex` (int) | Index of the currently open item (`0`-based). Set to `-1` to start with everything collapsed. Updated automatically when a user opens an item; opening one item closes the others. |
| `DaisyAccordionItem.IsExpanded` | Header interaction toggles this property. Opening a loaded item updates `ExpandedIndex` and collapses its siblings. |
| `Variant` | Set on the accordion to apply the indicator style to its items. |

## Content Structure

DaisyAccordion expects a `Panel`, such as `StackPanel`, containing `DaisyAccordionItem` controls. It moves those supplied items into its internal panel on first load:

- Header comes from `Header` and is rendered as a clickable toggle.
- Body shows only when `IsExpanded=True` and uses the resolved tier's content spacing.

```xml
<controls:DaisyAccordion>
    <StackPanel>
        <controls:DaisyAccordionItem Header="Section title">
            <!-- Body content here -->
        </controls:DaisyAccordionItem>
    </StackPanel>
</controls:DaisyAccordion>
```

## Quick Examples

```xml
<!-- Basic accordion, first item open -->
<controls:DaisyAccordion ExpandedIndex="0">
    <StackPanel>
        <controls:DaisyAccordionItem Header="Overview">
            <TextBlock Text="High-level summary of the feature." TextWrapping="Wrap" />
        </controls:DaisyAccordionItem>
        <controls:DaisyAccordionItem Header="Details">
            <TextBlock Text="Deeper explanation with supporting info." TextWrapping="Wrap" />
        </controls:DaisyAccordionItem>
    </StackPanel>
</controls:DaisyAccordion>

<!-- Plus variant with FAQ-style content -->
<controls:DaisyAccordion Variant="Plus" ExpandedIndex="0">
    <StackPanel>
        <controls:DaisyAccordionItem Header="How do I reset my password?">
            <TextBlock Text="Use the Forgot Password link and follow the email instructions." TextWrapping="Wrap" />
        </controls:DaisyAccordionItem>
        <controls:DaisyAccordionItem Header="Can I change my plan later?">
            <TextBlock Text="Yes, upgrades are immediate; downgrades apply on the next cycle." TextWrapping="Wrap" />
        </controls:DaisyAccordionItem>
    </StackPanel>
</controls:DaisyAccordion>

<!-- Start fully collapsed -->
<controls:DaisyAccordion ExpandedIndex="-1">
    <StackPanel>
        <controls:DaisyAccordionItem Header="Shipping">
            <TextBlock Text="Ships within 3–5 business days." />
        </controls:DaisyAccordionItem>
        <controls:DaisyAccordionItem Header="Returns">
            <TextBlock Text="30-day returns with prepaid label." />
        </controls:DaisyAccordionItem>
    </StackPanel>
</controls:DaisyAccordion>

<!-- Rich body content -->
<controls:DaisyAccordion ExpandedIndex="1" Size="Large">
    <StackPanel>
        <controls:DaisyAccordionItem Header="Profile">
            <StackPanel Spacing="8">
                <TextBlock Text="Update your display name and bio." />
                <controls:DaisyButton Content="Edit profile" Variant="Primary" />
            </StackPanel>
        </controls:DaisyAccordionItem>
        <controls:DaisyAccordionItem Header="Notifications" Size="Small">
            <StackPanel Spacing="8">
                <TextBlock Text="Choose which alerts you receive." />
                <controls:DaisyToggle Content="Email alerts" IsChecked="True" />
                <controls:DaisyToggle Content="Push notifications" />
            </StackPanel>
        </controls:DaisyAccordionItem>
    </StackPanel>
</controls:DaisyAccordion>
```

## Tips & Best Practices

- Use `ExpandedIndex="-1"` when you want the accordion to load collapsed and let users expand what they need.
- Keep headers concise so they fit in the available width.
- Prefer the **Arrow** variant for subtle layouts and **Plus** for strong “open/close” affordance or FAQ sections.
- Size changes and reloads retain the supplied body content and expansion state.

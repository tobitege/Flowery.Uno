<!-- Supplementary documentation for DaisyBreadcrumbs -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyBreadcrumbs shows a horizontal trail of navigation steps. Provide a `Panel` (typically a horizontal `StackPanel`) as `Content` containing `DaisyBreadcrumbItem` children; the control re-parents those items into its internal layout, applies separators between items, and marks only the trailing item as non-clickable. Supports custom separators, icons, and command execution on intermediate items.

## Breadcrumb Properties

| Property | Description |
| --- | --- |
| `Separator` (string, default "/") | Text used between items. Accepts any string (e.g., `›`, `»`, `>`). |
| `SeparatorBrush` (Brush, default null) | Default separator brush for items without their own brush. Null retains the existing resource/theme fallback. |
| `SeparatorOpacity` (double, default 1) | Additional opacity multiplier for the separator glyph. |
| `Size` (DaisySize) | Controls text/icon sizing for the breadcrumb items. |
| `Content` (Panel) | Provide a `Panel` (e.g., `StackPanel` with `Orientation="Horizontal"`) containing `DaisyBreadcrumbItem` children; items are re-parented into the control layout. |

## Item Properties

| Property | Description |
| --- | --- |
| `Content` | The label text; supports templated content. |
| `Icon` (Geometry) | Optional leading icon; hidden when not set. |
| `Command` / `CommandParameter` | Invoked on click for non-last items when `IsClickable=True`. |
| `IsClickable` (bool) | Toggle interactivity; still respects `IsLast` (last item never executes commands). |
| `IsFirst` / `IsLast` / `Index` | Set by the parent for styling and interaction; not usually set manually. |
| `Separator` | Assigned by the parent from its `Separator` property. Separator opacity is also managed by the parent. |
| `SeparatorBrush` (Brush, default null) | Item override. Null uses the containing breadcrumbs' brush, then the existing resource/theme fallback. |

## Separator Brush Precedence

Both `DaisyBreadcrumbs.SeparatorBrush` and `DaisyBreadcrumbItem.SeparatorBrush` are nullable dependency properties.
The separator uses the first available brush: the item's brush, its nearest `DaisyBreadcrumbs` brush,
the `DaisyBreadcrumbItemSeparatorForeground` resource, then `DaisyBaseContentSubtleBrush`.
A transparent brush is an explicit override; null requests fallback.

Styles and bindings keep normal dependency-property precedence. Parent changes never assign the item's
`SeparatorBrush`. Set an override to null to resume fallback; `ClearValue(SeparatorBrushProperty)` restores
the applicable style or null default. Theme, size and reload updates retain authored brushes and bindings.
The separator brush carries its own alpha. `SeparatorOpacity` defaults to 1, so an explicit brush has no additional dimming.
An explicit multiplier still applies to both the default brush and custom brushes.
The last item's default text and icon use `DaisyBaseContentMutedBrush`.
Custom content keeps its own foreground and opacity values.

## When to Use (vs DaisyBreadcrumbBar)

- Use `DaisyBreadcrumbs` when you want manual control over per-item content, separators, and click behavior.
- Use `DaisyBreadcrumbBar` when you have an `ItemsSource` + `ItemTemplate` and want built-in overflow ellipsis.

## Quick Examples

```xml
<!-- Basic trail -->
<controls:DaisyBreadcrumbs>
    <StackPanel Orientation="Horizontal">
        <controls:DaisyBreadcrumbItem Content="Home" />
        <controls:DaisyBreadcrumbItem Content="Documents" />
        <controls:DaisyBreadcrumbItem Content="Add Document" />
    </StackPanel>
</controls:DaisyBreadcrumbs>

<!-- Custom separator brush and opacity, with one item override -->
<controls:DaisyBreadcrumbs Separator="›" SeparatorBrush="#FF64748B" SeparatorOpacity="0.7">
    <StackPanel Orientation="Horizontal">
        <controls:DaisyBreadcrumbItem Content="Root" />
        <controls:DaisyBreadcrumbItem Content="Folder" />
        <controls:DaisyBreadcrumbItem Content="File" SeparatorBrush="#FF2563EB" />
    </StackPanel>
</controls:DaisyBreadcrumbs>

<!-- With icons -->
<controls:DaisyBreadcrumbs>
    <StackPanel Orientation="Horizontal">
        <controls:DaisyBreadcrumbItem Content="Home" Icon="{StaticResource DaisyIconHome}" />
        <controls:DaisyBreadcrumbItem Content="Library" Icon="{StaticResource DaisyIconFolder}" />
        <controls:DaisyBreadcrumbItem Content="Report.pdf" Icon="{StaticResource DaisyIconDocument}" />
    </StackPanel>
</controls:DaisyBreadcrumbs>

<!-- Constrained width (wrap in ScrollViewer for horizontal scroll) -->
<ScrollViewer HorizontalScrollBarVisibility="Auto" HorizontalScrollMode="Auto">
    <controls:DaisyBreadcrumbs MaxWidth="220">
        <StackPanel Orientation="Horizontal">
            <controls:DaisyBreadcrumbItem Content="Long text 1" />
            <controls:DaisyBreadcrumbItem Content="Long text 2" />
            <controls:DaisyBreadcrumbItem Content="Long text 3" />
            <controls:DaisyBreadcrumbItem Content="Long text 4" />
            <controls:DaisyBreadcrumbItem Content="Long text 5" />
        </StackPanel>
    </controls:DaisyBreadcrumbs>
</ScrollViewer>

<!-- Command-driven navigation -->
<controls:DaisyBreadcrumbs Separator=">">
    <StackPanel Orientation="Horizontal">
        <controls:DaisyBreadcrumbItem Content="Home"
                                      Command="{Binding NavigateCommand}"
                                      CommandParameter="Home" />
        <controls:DaisyBreadcrumbItem Content="Projects"
                                      Command="{Binding NavigateCommand}"
                                      CommandParameter="Projects" />
        <controls:DaisyBreadcrumbItem Content="Current Project" />
    </StackPanel>
</controls:DaisyBreadcrumbs>
```

## Tips & Best Practices

- Keep labels short to avoid truncation; wrap in a `ScrollViewer` if you need horizontal scrolling for long paths.
- Use a distinctive separator (e.g., `›` or `»`) to improve scanability on dark backgrounds.
- Bind `Command` on intermediate items for navigation; the last item is intentionally non-interactive to indicate the current page.
- Provide icons for top-level nodes (e.g., Home, Folder) to aid recognition, especially on narrow layouts.
- Provide `DaisyBreadcrumbItem` children inside a `Panel`; the control re-parents them and assigns `IsFirst`/`IsLast` and separators automatically.
- For `ItemsSource` + `ItemTemplate` scenarios and built-in overflow behavior, use `DaisyBreadcrumbBar`.

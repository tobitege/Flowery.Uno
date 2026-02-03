# DaisyBreadcrumbBar

<!-- Supplementary documentation for DaisyBreadcrumbBar -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

## Overview

DaisyBreadcrumbBar is a themed wrapper around WinUI's `BreadcrumbBar`. It aligns typography
and colors with the Daisy token system while keeping standard breadcrumb behavior like
overflow ellipsis.

## When to Use (vs DaisyBreadcrumbs)

- Use `DaisyBreadcrumbBar` when you already have an `ItemsSource` and want standard WinUI `BreadcrumbBar` behavior (overflow ellipsis, built-in item handling, and keyboard/automation support).
- Use `DaisyBreadcrumbs` when you want fully custom per-item content, custom separators, or manual control over clickability and layout.
- `DaisyBreadcrumbBar` uses `ItemsSource` + `ItemTemplate`; `DaisyBreadcrumbs` expects `DaisyBreadcrumbItem` children.

## Size Options

| Size | Description |
| --- | --- |
| ExtraSmall | Compact breadcrumbs for dense layouts |
| Small | Tight navigation trails |
| Medium | Default sizing |
| Large | More prominent breadcrumb text |
| ExtraLarge | Large display breadcrumb trails |

## Quick Examples

```xml
<daisy:DaisyBreadcrumbBar ItemsSource="{x:Bind BreadcrumbItems}" />
<daisy:DaisyBreadcrumbBar ItemsSource="{x:Bind BreadcrumbItems}" Size="Small" />
```

## Tips & Best Practices

- Provide path segments in order using `ItemsSource`.
- Use `ItemTemplate` when you need icons or rich content.
- Apply lightweight styling via `DaisyBreadcrumbBar*` resource keys for color overrides.
- For manual breadcrumb items or custom separators, switch to `DaisyBreadcrumbs`.

---

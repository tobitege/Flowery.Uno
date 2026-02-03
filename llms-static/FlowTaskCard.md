<!-- Supplementary documentation for FlowTaskCard -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

`FlowTaskCard` is a specialized, lightweight card component designed for the Kanban board. It provides a compact container for task information with built-in support for semantic color palettes, a title header, and an integrated close button for removing or dismissing tasks. It inherits from `DaisyBaseContentControl`, ensuring full deep integration with the Flowery theming and lifecycle systems.

## Key Features

- **Semantic Palettes**: Instantly change the card's visual tone using the `Palette` property.
- **Built-in Header**: Integrated title and close button out of the box.
- **Compact Design**: Vertically stacked layout optimized for column-based board views.
- **Theme-Aware**: Automatically updates its brushes and typography when the system theme changes.

## Properties

| Property | Type | Description |
| --- | --- | --- |
| `Title` | `string` | The text displayed in the card header. |
| `Palette` | `DaisyColor` | The semantic color palette applied to the card (Primary, Success, Error, etc.). |
| `CloseCommand` | `ICommand` | Command executed when the close button is clicked. |
| `CloseCommandParameter` | `object` | Parameter passed to the `CloseCommand`. |
| `Content` | `object` | The main content of the card (inherited from `ContentControl`). |

## Palette Options

The `Palette` property allows you to categorize tasks visually:

| Palette | Visual Style | Use Case |
| --- | --- | --- |
| `Default` | Neutral base colors | Standard tasks |
| `Primary` | Bold primary accent | High priority |
| `Success` | Success green | Completed or verified |
| `Warning` | Warning orange | Blocked or urgent |
| `Error` | Error red | Critical or bug |
| `Info` | Info blue | Research or documentation |

## Usage Examples

### Basic Task Card

A simple card with just a title and text content.

```xml
<kanban:FlowTaskCard Title="Check logs" 
                     Margin="0,0,0,8">
    <TextBlock Text="Review production logs for 5xx errors." 
               TextWrapping="Wrap" Opacity="0.7" />
</kanban:FlowTaskCard>
```

### High Priority Task (Primary Palette)

Using the `Palette` property to highlight a task.

```xml
<kanban:FlowTaskCard Title="SECURITY PATCH" 
                     Palette="Primary">
    <TextBlock Text="Apply emergency security updates to API." />
</kanban:FlowTaskCard>
```

### Critical Bug (Error Palette)

Using the `Error` palette for immediate visibility.

```xml
<kanban:FlowTaskCard Title="Backend Crash" 
                     Palette="Error"
                     CloseCommand="{Binding RemoveTaskCommand}"
                     CloseCommandParameter="{Binding}">
    <TextBlock Text="The main service is failing on startup." />
</kanban:FlowTaskCard>
```

## Styling Notes

- **Header Padding**: The card header has intrinsic alignment for the title and the close button.
- **Background Transition**: When changing the `Palette`, the card updates both its `Background` and `Foreground` to ensure perfect legibility based on the active theme.
- **Corner Radius**: Follows the DaisyUI aesthetic with a default 8px corner radius.

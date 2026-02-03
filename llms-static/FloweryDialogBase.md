<!-- Supplementary documentation for FloweryDialogBase -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

FloweryDialogBase is the foundation for building **themed, responsive modal dialogs** in Flowery.Uno. It inherits from `DaisyModal` and provides **smart sizing** that adapts to any screen size--from narrow mobile portrait to large desktop--plus automatic **theme synchronization**, **standardized 3-row layouts** (header, scrollable content, footer), and factory methods for creating consistent button footers.

## Inheritance Chain

FloweryDialogBase -> DaisyModal -> ThemedControl -> ContentControl

FloweryDialogBase leverages `DaisyModal`'s overlay, draggable grab-bar, and backdrop features while adding the smart-sizing algorithm and dialog layout helpers.

## Size Constants

| Constant | Value | Description |
| --- | --- | --- |
| `AbsoluteMinWidth` | 280px | Minimum width (narrow mobile portrait). |
| `AbsoluteMinHeight` | 400px | Minimum height. |
| `PreferredWidth` | 420px | Default preferred width. |
| `PreferredHeight` | 520px | Default preferred height. |
| `MaxDialogWidth` | 840px | Maximum width (~2x preferred). |
| `MaxDialogHeight` | 800px | Maximum height. |
| `DialogMargin` | 40px | Margin reserved for window chrome/overlay. |
| `ContentDialogChrome` | 48px | Padding inside dialog for chrome, borders, scrollbar clearance. |

## Dependency Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `DialogWidth` | `double` | 420 | Current dialog width (set by `ApplySmartSizing`). |
| `DialogHeight` | `double` | 520 | Current dialog height (set by `ApplySmartSizing`). |
| `IsOutsideClickDismissEnabled` | `bool` | false | Whether tapping outside closes the dialog. |
| `IsCloseOnEnterEnabled` | `bool` | false | Whether pressing Enter closes the dialog. |
| `IsDialogResizingEnabled` | `bool` | false | Whether the dialog can be resized within its container. |

## Smart Sizing Algorithm

The `CalculateOptimalDialogSize(XamlRoot?)` method returns width/height tuned to the host window:

1. **Available Space** = Window size - `DialogMargin`
2. **Ratio Selection**:
   - Small screens (< 600px width): 90% of available width
   - Large screens: 60% of available width
   - Small screens (< 700px height): 85% of available height
   - Large screens: 65% of available height
3. **Clamp** to `[AbsoluteMin, Max]` bounds.
4. **Aspect Ratio Guard**: Keeps ratio between 1:1.2 and 1:1.8 (ideal for forms).

### Sizing Methods

| Method | Description |
| --- | --- |
| `ApplySmartSizing(XamlRoot?)` | Calculates and sets both DialogWidth/DialogHeight; applies fixed sizing. |
| `ApplySmartSizingWithAutoHeight(XamlRoot?)` | Same as above but sets height to `NaN` (auto), allowing content to determine height up to the calculated maximum. |

## Dialog Content Factory

Use these static helpers to build standardized dialog layouts:

### `CreateDialogContent(...)`

```csharp
public static Grid CreateDialogContent(
    XamlRoot? xamlRoot,
    FrameworkElement? headerContent,
    FrameworkElement mainContent,
    FrameworkElement? footerContent)
```

Creates a 3-row Grid:

- **Row 0 (Auto)**: Header with 12px bottom margin
- **Row 1 (Star)**: ScrollViewer containing main content (12px right margin for scrollbar clearance)
- **Row 2 (Auto)**: Footer with 12px top margin

### `CreateStandardButtonFooter(...)`

```csharp
public static StackPanel CreateStandardButtonFooter(
    out DaisyButton saveButton,
    out DaisyButton cancelButton,
    string? saveText = null,
    string? cancelText = null)
```

Creates a right-aligned horizontal button panel:

- **Save Button**: Primary variant, Medium size, 80px MinWidth
- **Cancel Button**: Ghost variant, Medium size, 80px MinWidth
- Button order: `[Save] [Cancel]` (left to right)
- Spacing: 12px between buttons

## Theming

FloweryDialogBase automatically responds to theme changes:

| Override | Description |
| --- | --- |
| `OnThemeChanged(string themeName)` | Called when active theme changes; triggers `ApplyTheming()`. |
| `OnLoaded()` | Applies theming on first load. |
| `ApplyTheming()` | Sets `Background` to `DaisyBase100Brush` and `Foreground` to `DaisyBaseContentBrush`. Override to customize. |

## Input Handling

FloweryDialogBase can close on Enter when `IsCloseOnEnterEnabled = true`. To customize Enter behavior, override `OnEnterKeyRequested()` in your dialog and return `true` to mark it handled.

## Resizable Dialogs

When `IsDialogResizingEnabled = true`, FloweryDialogBase displays a bottom-right resize grip and allows resizing within the modal container. Resizing respects the dialog's min/max bounds and the host's available size (with margin).

## Quick Example

```csharp
public sealed class MyCustomDialog : FloweryDialogBase
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    private readonly XamlRoot _xamlRoot;
    private Panel? _hostPanel;
    private DaisyButton _saveButton = null!;
    private DaisyButton _cancelButton = null!;

    private MyCustomDialog(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;

        // Build header
        var header = new StackPanel { Spacing = 4 };
        header.Children.Add(new TextBlock { Text = "Dialog Title", FontSize = 20, FontWeight = FontWeights.Bold });
        header.Children.Add(new TextBlock { Text = "Subtitle or description", FontSize = 12, Opacity = 0.7 });

        // Build main content
        var mainContent = new StackPanel { Spacing = 12 };
        mainContent.Children.Add(new DaisyInput { PlaceholderText = "Enter value..." });
        mainContent.Children.Add(new DaisyTextArea { PlaceholderText = "Enter description...", MinRows = 3 });

        // Build footer
        var footer = CreateStandardButtonFooter(out _saveButton, out _cancelButton);

        // Assemble using factory method
        Content = CreateDialogContent(xamlRoot, header, mainContent, footer);

        // Enable dragging and apply smart sizing
        IsDraggable = true;
        IsDialogResizingEnabled = true;
        ApplySmartSizingWithAutoHeight(xamlRoot);

        // Wire up button handlers
        _saveButton.Click += (s, e) => Close(true);
        _cancelButton.Click += (s, e) => Close(false);
    }

    public static Task<bool> ShowAsync(XamlRoot xamlRoot)
    {
        var dialog = new MyCustomDialog(xamlRoot);
        return dialog.ShowInternalAsync();
    }

    private Task<bool> ShowInternalAsync()
    {
        _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
        if (_hostPanel == null) return Task.FromResult(false);

        _hostPanel.Children.Add(this);
        IsOpen = true;
        return _tcs.Task;
    }

    private void Close(bool result)
    {
        IsOpen = false;
        _hostPanel?.Children.Remove(this);
        _tcs.TrySetResult(result);
    }
}

// Usage:
var saved = await MyCustomDialog.ShowAsync(myControl.XamlRoot);
```

## Tips & Best Practices

- **Use `ApplySmartSizingWithAutoHeight`** for dialogs with variable content--lets the dialog shrink to fit while respecting max bounds.
- **Use `ApplySmartSizing`** for fixed-layout dialogs or when you need guaranteed dimensions.
- **Override `ApplyTheming()`** if your dialog needs custom colors or additional themed elements.
- **Always use `FloweryDialogHost.EnsureHost()`** to obtain a host panel--it ensures proper z-index and reuses existing hosts.
- **Localize button text** by passing custom `saveText` / `cancelText` to `CreateStandardButtonFooter`, or rely on built-in localization (`Common_Save`, `Common_Cancel`).
- **Set `IsDraggable = true`** for movable dialogs--users appreciate being able to reposition dialogs on large screens.
- **Set `IsDialogResizingEnabled = true`** for editor/settings dialogs that benefit from extra space.
- **Set `IsCloseOnEnterEnabled = true`** for quick rename/input dialogs where Enter should confirm.
- **Leverage the constant values** (`AbsoluteMinWidth`, `ContentDialogChrome`, etc.) when calculating custom content dimensions.
- **Implement the `ShowAsync` pattern** with `TaskCompletionSource<bool>` for clean async dialog invocation.

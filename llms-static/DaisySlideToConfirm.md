<!-- Supplementary documentation for DaisySlideToConfirm -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisySlideToConfirm is a "slide to confirm" control that requires the user to drag a handle across a track to confirm an action. This pattern prevents accidental triggers for destructive or important operations like power-off, delete, or payment confirmation.

Key features:

- **Drag-to-Confirm**: User must deliberately slide the handle to the end to trigger the action.
- **Visual Feedback**: Track background color and label opacity animate proportionally as the user slides.
- **Auto-Reset**: After confirmation, the control shows a confirming state then resets automatically.
- **Variant Colors**: Use semantic variants (Primary, Success, Error, etc.) for themed colors.
- **Size Support**: Supports all DaisySize variants and responds to global sizing.
- **Design Tokens**: All dimensions are configurable through design tokens.

## Key Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| **Variant** | `DaisySlideToConfirmVariant` | `Default` | Color variant (Primary, Success, Error, etc.). |
| **Size** | `DaisySize` | `Medium` | Control size (ExtraSmall to ExtraLarge). |
| **Text** | `string` | `"SLIDE TO CONFIRM"` | Label text displayed on the track. |
| **ConfirmingText** | `string` | `"CONFIRMING..."` | Text displayed after the slide is completed. |
| **SlideColor** | `Color` | (from Variant) | Color the track transitions to as the user slides. |
| **IconData** | `string` | Power-off icon | Custom icon path data for the handle. |
| **IconForeground** | `Brush` | (from Variant) | Foreground color of the icon. |
| **HandleBackground** | `Brush` | `DaisyBase100Brush` | Background color of the slide handle. |
| **ResetDelay** | `TimeSpan` | `900ms` | Delay before auto-resetting after confirmation. |
| **AutoReset** | `bool` | `true` | Whether to auto-reset after confirmation. |
| **TrackWidth** | `double` | (from Size) | Override width of the slide track. |
| **TrackHeight** | `double` | (from Size) | Override height of the slide track. |

## Variants

Use the `Variant` property for semantic, theme-aware colors:

| Variant | Use Case |
| --- | --- |
| `Default` / `Primary` | General confirmations |
| `Success` | Positive confirmations (confirm, approve) |
| `Error` | Destructive actions (delete, power off) |
| `Warning` | Caution actions |
| `Info` | Informational actions (unlock, reveal) |
| `Secondary` / `Accent` | Alternative styling |

## Events

| Event | Description |
| --- | --- |
| **SlideCompleted** | Fires when the user successfully slides the handle all the way to the end. |

## Quick Examples

### Basic Power-Off Slider (Primary)

```xml
<daisy:DaisySlideToConfirm 
    Text="SLIDE TO POWER OFF"
    ConfirmingText="POWERING OFF..."
    Variant="Primary"
    SlideCompleted="OnSlideCompleted" />
```

### Confirm Slider (Success)

```xml
<daisy:DaisySlideToConfirm 
    Text="SLIDE TO CONFIRM"
    ConfirmingText="CONFIRMED!"
    Variant="Success"
    SlideCompleted="OnSlideCompleted" />
```

### Delete Slider (Error)

```xml
<daisy:DaisySlideToConfirm 
    Text="SLIDE TO DELETE"
    ConfirmingText="DELETING..."
    Variant="Error"
    SlideCompleted="OnDeleteConfirmed" />
```

### Unlock Slider (Info with Auto-Reset)

```xml
<daisy:DaisySlideToConfirm 
    Text="SLIDE TO UNLOCK"
    ConfirmingText="UNLOCKED!"
    Variant="Info"
    ResetDelay="0:0:2"
    SlideCompleted="OnUnlockCompleted" />
```

### Different Sizes

```xml
<!-- Responds to global size by default -->
<daisy:DaisySlideToConfirm Variant="Primary" Text="SLIDE" />

<!-- Explicit sizes (ignores global) -->
<daisy:DaisySlideToConfirm Size="Small" Variant="Primary" Text="SLIDE" />
<daisy:DaisySlideToConfirm Size="Large" Variant="Error" Text="SLIDE TO DELETE" />
```

### Custom Icon

```xml
<daisy:DaisySlideToConfirm 
    Text="SLIDE TO LOCK"
    IconData="{StaticResource DaisyIconLock}"
    Variant="Warning"
    SlideCompleted="OnLockConfirmed" />
```

### Code-Behind Event Handler

```csharp
private void OnSlideCompleted(object? sender, EventArgs e)
{
    // Perform the confirmed action
    System.Diagnostics.Debug.WriteLine("Action confirmed!");
}
```

## Design Tokens

DaisySlideToConfirm uses the following design tokens (customizable via `DaisyTokenDefaults`):

| Token | XS | S | M | L | XL |
| ----- | -- | -- | -- | -- | --- |
| `DaisySlideToConfirm{Size}TrackWidth` (MinWidth) | 60 | 70 | 80 | 90 | 100 |
| `DaisySlideToConfirm{Size}TrackHeight` | 20 | 24 | 28 | 32 | 36 |
| `DaisySlideToConfirm{Size}HandleSize` | 12 | 14 | 16 | 18 | 22 |
| `DaisySlideToConfirm{Size}IconSize` | 6 | 7 | 8 | 9 | 11 |
| `DaisySlideToConfirm{Size}FontSize` | 6 | 7 | 8 | 9 | 10 |
| `DaisySlideToConfirm{Size}CornerRadius` | 10 | 12 | 14 | 16 | 18 |

> **Note:** Track width auto-sizes to fit text content. The token value is used as MinWidth. Set `TrackWidth` explicitly for a fixed width.

Override tokens in your App.xaml or ResourceDictionary:

```xml
<x:Double x:Key="DaisySlideToConfirmMediumTrackWidth">350</x:Double>
```

## Tips & Best Practices

- **Use Variants for Colors**: Prefer `Variant="Error"` over hardcoded `SlideColor="#DC2626"` for theme-aware colors.
- **Semantic Variants**: Use `Error` for destructive actions, `Success` for confirmations, `Info` for unlocks.
- **Size Consistency**: The control responds to `FlowerySizeManager` global size by default.
- **Ignore Global Size in Demos**: Use `FlowerySizeManager.IgnoreGlobalSize="True"` on parent to show explicit sizes.
- **Reset Delay**: Adjust `ResetDelay` based on how long the confirmed action takes to complete.
- **Custom Icons**: Use `IconData` with StaticResource for custom icons (lock, trash, check, etc.).

## Common Pitfalls

| Issue | Solution |
| --- | --- |
| Text cut off by handle | Uses left margin automatically; ensure track is wide enough |
| Colors don't match theme | Use `Variant` property instead of hardcoded colors |
| Control too small | Check `Size` property or increase design token values |
| Icon not showing | Ensure `IconData` is a valid path geometry string |

## Related Controls

- [DaisyButton](DaisyButton.md) - Standard button for simple actions
- [DaisyModal](DaisyModal.md) - Dialog-based confirmation

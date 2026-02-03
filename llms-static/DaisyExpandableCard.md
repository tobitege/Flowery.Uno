<!-- Supplementary documentation for DaisyExpandableCard -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyExpandableCard is a versatile card component that can reveal a secondary content area when triggered. It is ideal for "Showcase" or "Detail" views where initial high-level information is presented, and additional details are revealed on demand (e.g., clicking a "Learn More" or "Play" button).

Key features:

- **Smooth Animation**: Uses a width-based easing animation to push neighboring content aside while revealing the expanded area.
- **Opacity Transition**: Content in the expanded area fades in/out simultaneously with the width change.
- **Toggle Command**: Includes a built-in `ToggleCommand` for easy binding from child buttons.
- **Batteries-Included Mode**: Set `UseBatteriesIncludedMode="True"` to generate visual content from simple properties.
- **Responsive**: Works well in horizontal scrolling containers like `ScrollViewer`.

## Key Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| **IsExpanded** | `bool` | `false` | Controls whether the card is currently showing its expanded content. |
| **ExpandedContent** | `object` | `null` | The UI content to display in the revealed area (manual mode). |
| **ToggleCommand** | `ICommand` | auto | A command that toggles `IsExpanded`. Typically bound to a button inside the card. |
| **AnimationDuration** | `TimeSpan` | `300ms` | Duration of the expand/collapse animation. |

### Batteries-Included Properties

When `UseBatteriesIncludedMode="True"`, these properties generate the visual content automatically:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| **UseBatteriesIncludedMode** | `bool` | `false` | When true, generates content from convenience properties. |
| **Title** | `string` | `null` | Main title displayed on the card. |
| **Subtitle** | `string` | `null` | Subtitle displayed below the title. |
| **GradientStart** | `Color` | `#0f172a` | Starting color of the background gradient. |
| **GradientEnd** | `Color` | `#334155` | Ending color of the background gradient. |
| **CardWidth** | `double` | `150` | Width of the main card content area. |
| **CardHeight** | `double` | `225` | Height of the card. |
| **ExpandedText** | `string` | `null` | Main text displayed in the expanded panel. |
| **ExpandedSubtitle** | `string` | `null` | Subtitle displayed in the expanded panel. |
| **ExpandedBackground** | `Color` | `#111827` | Background color of the expanded panel. |
| **ActionButtonText** | `string` | `"Play"` | Text displayed on the action button. |

## Quick Examples

### Batteries-Included Mode (Recommended)

With just 8 attributes instead of 40+ lines of XAML:

```xml
<daisy:DaisyExpandableCard 
    UseBatteriesIncludedMode="True"
    Padding="0"
    Title="Summer"
    Subtitle="Opening"
    GradientStart="#0f172a"
    GradientEnd="#334155"
    ExpandedText="Join us for the Summer Opening event."
    ExpandedSubtitle="Freddy K. · CEO" />
```

### Manual Mode (Full Customization)

```xml
<daisy:DaisyExpandableCard x:Name="MyCard">
    <!-- Main Content (Default) -->
    <Grid Width="150" Height="225">
        <daisy:DaisyButton Content="Expand" 
                           Command="{Binding #MyCard.ToggleCommand}" />
    </Grid>

    <!-- Expanded Content -->
    <daisy:DaisyExpandableCard.ExpandedContent>
        <Border Width="150" Background="#111827">
            <TextBlock Text="Detailed Info revealed!" />
        </Border>
    </daisy:DaisyExpandableCard.ExpandedContent>
</daisy:DaisyExpandableCard>
```

## Tips & Best Practices

- **Use Batteries-Included Mode**: For standard card layouts, use the convenience properties to reduce markup dramatically.
- **Fixed Dimensions**: For the most reliable expansion animation, define a fixed `Width` and `Height` on both the main content and the `ExpandedContent` container.
- **Horizontal Layout**: Expandable cards are best used in a horizontal `StackPanel` inside a `ScrollViewer` with `HorizontalScrollBarVisibility="Auto"`.
- **Glass Effect**: Supports the same `IsGlass="True"` mode as `DaisyCard` for a modern frosted look.

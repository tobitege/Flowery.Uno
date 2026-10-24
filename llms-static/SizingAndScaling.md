# Sizing and Scaling

Flowery.Uno provides **two distinct sizing systems**. Understanding when to use each is essential:

| System | Purpose | Scope |
| --- | --- | --- |
| **Global Size** (`FlowerySizeManager`) | User preference / accessibility | Auto controls throughout the app |
| **Auto Scaling** (`FloweryScaleManager`) | Responsive window-based sizing | Per-container opt-in only |

> [!NOTE]
> 💡 **Most apps should just use Global Size.** Auto Scaling is an advanced feature for specific responsive-layout scenarios like data forms.

## Uno Platform Implementation

Flowery.Uno uses a code-based token system instead of XAML resource dictionaries:

| Component | Purpose |
| --- | --- |
| `DaisyTokenDefaults.cs` | Single source of truth for all token values |
| `DaisyResourceLookup.cs` | Centralized helper methods for sizing calculations |
| `FlowerySizeManager` | Global size management and event broadcasting |

All controls read sizing from `DaisyResourceLookup.GetDefault*()` methods, ensuring consistency across the entire UI.

---

## Global Size (FlowerySizeManager)

The `FlowerySizeManager` is a static service that:

- Refreshes loaded controls whose Size resolves automatically
- Maintains the current global size setting
- Leaves authored Size values, styles and bindings unchanged

## Quick Start

Apply a global size to controls using Auto:

```csharp
using Flowery.Controls;
using Flowery.Enums;

// Apply a size by enum
FlowerySizeManager.ApplySize(DaisySize.Small);

```

## Size Ownership

Control `Size` defaults to `DaisySize.Auto`. Read `ResolvedSize` for the effective concrete tier.
An explicit Size from XAML, code, a style or a binding wins over the global setting.
Global changes never replace that value.

`UseGlobalSizeByDefault` (true by default) selects global fallback for Auto controls.
`EnableGlobalAutoSize` (also true) controls automatic refresh after ApplySize.
The current global tier starts at Medium; Reset selects Small.
See [FlowerySizeManager](FlowerySizeManager.md) for composite defaults, opt-out fallback values,
reload behavior and migration guidance.

## Size Options

| Size | Typical Use Case |
| --- | --- |
| `ExtraSmall` | High-density UIs, data tables, compact toolbars |
| `Small` | Good balance for desktop apps |
| `Medium` | Touch-friendly, accessibility |
| `Large` | Larger screens, presentations |
| `ExtraLarge` | Maximum readability, kiosk mode |

## API Reference

### Properties

```csharp
// Get the current global size
DaisySize currentSize = FlowerySizeManager.CurrentSize;

// Check/set if new controls should auto-use global size
FlowerySizeManager.UseGlobalSizeByDefault = true;
```

### Methods

```csharp
// Apply a size by enum
FlowerySizeManager.ApplySize(DaisySize.Medium);

// Reset to default (Small)
FlowerySizeManager.Reset();
```

### Events

```csharp
// Subscribe to size changes
FlowerySizeManager.SizeChanged += (sender, size) =>
{
    Console.WriteLine($"Size changed to: {size}");
    // Update your custom controls here
};
```

## Built-in UI Controls

Flowery.Uno provides ready-to-use controls for size selection:

### DaisySizeDropdown

A ComboBox-style dropdown that shows all available sizes with localized names:

```xml
<controls:DaisySizeDropdown />
```

The dropdown:

- Displays localized size names by default (for example, "Klein" in German)
- Shows abbreviations (XS, S, M, L, XL) when `ShowAbbreviations="True"`
- Refreshes Auto controls when selection changes and global refresh is enabled

**Advanced customization**: You can control which sizes appear and customize their display names. See [DaisySizeDropdown](DaisySizeDropdown.md) for details on the `SizeOptions` property.

### Which Controls Respond?

DaisyUI controls with `Size="Auto"` respond to global size changes unless opted out:

- `DaisyButton`
- `DaisyInput` / `DaisyTextArea`
- `DaisySelect`
- `DaisyCheckBox` / `DaisyRadio` / `DaisyToggle`
- `DaisyBadge`
- `DaisyProgress` / `DaisyRadialProgress`
- `DaisyTabs`
- `DaisyMenu`
- `DaisyKbd`
- `DaisyAvatar`
- `DaisyLoading`
- `DaisyFileInput`
- `DaisyNumericUpDown`
- `DaisyDateTimeline`
- `DaisyDivider`
- And more...

## Responsive Font for TextBlocks

For regular `TextBlock` elements that should scale with the global size, use the `ResponsiveFont` attached property:

```xml
<TextBlock Text="Description text" 
           controls:FlowerySizeManager.ResponsiveFont="Primary" />
```

### Available Tiers

| Tier | Description | XS/S/M/L/XL Font Sizes |
| --- | --- | --- |
| `Primary` | Body text, descriptions | 10/12/14/18/20 |
| `Secondary` | Hints, captions, labels | 9/10/12/14/16 |
| `Tertiary` | Very small text, counters | 8/9/11/12/14 |
| `SectionHeader` | Section titles, control groups | 12/14/16/20/22 |
| `Header` | Page titles, headings | 14/16/20/24/28 |

### Examples

```xml
<!-- Body text -->
<TextBlock Text="This is a description that scales with global size."
           controls:FlowerySizeManager.ResponsiveFont="Primary" />

<!-- Hint/caption text -->
<TextBlock Text="Optional field" Opacity="0.7"
           controls:FlowerySizeManager.ResponsiveFont="Secondary" />

<!-- Section header -->
<TextBlock Text="Settings" FontWeight="Bold"
           controls:FlowerySizeManager.ResponsiveFont="SectionHeader" />
```

### How It Works

The attached property:

1. Immediately applies the font size for the current global size
2. Tracks the TextBlock while it is loaded
3. Updates it when global refresh is enabled, unless IgnoreGlobalSize applies
4. Removes it on unload and catches up when it is loaded again

> **Note:** The `ResponsiveFont` attached property is the recommended approach for text that should scale with global size, as it handles subscription and cleanup automatically.

## Making Custom Controls Size-Aware

Use the Auto/ResolvedSize contract and lifecycle hooks described in
[FlowerySizeManager](FlowerySizeManager.md#custom-controls).
Apply resolved values to visual properties without assigning the authored Size.
Do not bulk-assign Size through reflection: that replaces local values and bindings.

## Opting Out of Global Sizing

Sometimes you have controls that should **not** respond to global size changes - for example, demonstration controls showing all five size variants. Use the `IgnoreGlobalSize` attached property:

### On Individual Controls

```xml
<controls:DaisyButton controls:FlowerySizeManager.IgnoreGlobalSize="True" 
                      Size="Large" Content="I stay Large!" />
```

### On Parent Containers (Recommended)

Mark a parent container to protect all descendant controls:

```xml
<StackPanel controls:FlowerySizeManager.IgnoreGlobalSize="True">
    <!-- All size example controls inside are protected -->
    <controls:DaisyAvatar Size="ExtraSmall" />
    <controls:DaisyAvatar Size="Small" />
    <controls:DaisyAvatar Size="Medium" />
    <controls:DaisyAvatar Size="Large" />
    <controls:DaisyAvatar Size="ExtraLarge" />
</StackPanel>
```

### How Inheritance Works

The `IgnoreGlobalSize` property uses visual tree traversal with "first explicit setting wins" semantics:

1. Resolving an Auto size checks `FlowerySizeManager.ShouldIgnoreGlobalSize(this)`
2. The method walks up the visual tree from the control to the root
3. It stops at the **first ancestor** with an explicitly set `IgnoreGlobalSize` value (either `True` or `False`)
4. If no explicit setting is found anywhere in the tree, the control responds to global size changes (returns `false`)

This design enables both **opting out** and **opting back in**:

```xml
<!-- Outer container opts OUT of global sizing -->
<StackPanel controls:FlowerySizeManager.IgnoreGlobalSize="True">
    <controls:DaisyButton Size="Small" Content="Stays Small" />
    <controls:DaisyButton Size="Large" Content="Stays Large" />
    
    <!-- Inner container opts BACK IN to global sizing -->
    <Border controls:FlowerySizeManager.IgnoreGlobalSize="False">
        <controls:DaisyButton Content="Responds to global size!" />
    </Border>
</StackPanel>
```

> **Note:** Only explicitly set values are considered. The default (unset) value does not stop the traversal.

## Localization

Size names are fully localized. Add translations to your language files:

```json
{
  "Size_ExtraSmall": "Extra Small",
  "Size_Small": "Small",
  "Size_Medium": "Medium",
  "Size_Large": "Large",
  "Size_ExtraLarge": "Extra Large"
}
```

Supported languages: English, German, French, Spanish, Italian, Japanese, Korean, Arabic, Turkish, Ukrainian, Chinese (Simplified).

## Design Tokens Integration

The sizing system works with [Design Tokens](DesignTokens.md). These are the desktop defaults:

| Size | Height Token | Font Size Token |
| --- | --- | --- |
| ExtraSmall | `DaisySizeExtraSmallHeight` (24) | `DaisySizeExtraSmallFontSize` (10) |
| Small | `DaisySizeSmallHeight` (28) | `DaisySizeSmallFontSize` (12) |
| Medium | `DaisySizeMediumHeight` (32) | `DaisySizeMediumFontSize` (14) |
| Large | `DaisySizeLargeHeight` (36) | `DaisySizeLargeFontSize` (18) |
| ExtraLarge | `DaisySizeExtraLargeHeight` (40) | `DaisySizeExtraLargeFontSize` (20) |

Android and iOS use height defaults of 36/40/44/48/56 for XS/S/M/L/XL. The font ladder stays the same.

`DaisyResourceLookup.GetDefaultHeight(size)` and `GetDefaultFontSize(size)` return code defaults, without resource overrides.
See [Design Tokens](DesignTokens.md) for token lookup and customization.

## Best Practices

1. **Choose an initial tier** - CurrentSize starts at Medium; call ApplySize to choose another tier.

2. **Provide a size picker** - Give users control over the size preference, especially for accessibility.

3. **Test all sizes** - Ensure your layouts don't break at `ExtraSmall` (compact) or `ExtraLarge` (spacious).

4. **Use tokens, not hardcoded values** - This ensures your custom controls scale properly.

5. **Unsubscribe from events** - Always clean up `SizeChanged` subscriptions in `OnUnloaded` to prevent memory leaks.

## Example: Settings Panel

A complete example of a settings panel with size selection:

```xml
<StackPanel Spacing="16">
    <TextBlock Text="Display Size" FontWeight="Bold" />
    
    <controls:DaisySizeDropdown Width="180" />
    
    <TextBlock Text="This text will resize when you change the size above."
               controls:FlowerySizeManager.ResponsiveFont="Primary" />
    
    <controls:DaisyButton Content="Sample Button" />
    <controls:DaisyInput PlaceholderText="Sample Input" />
</StackPanel>
```

All controls in the panel will automatically resize when the dropdown selection changes.

---

## Continuous Font Scaling (FloweryScaleManager)

While `FlowerySizeManager` provides **discrete size tiers** (Small, Medium, Large), Flowery.Uno also offers **continuous font scaling** based on window size through `FloweryScaleManager`.

> **⚠️ Important:** These are **two separate, independent systems**. Auto Scaling ONLY affects controls within a container marked with `EnableScaling="True"`. It does NOT affect the rest of your app (sidebar, navigation, etc.).

### Quick Comparison

| Feature | Global Size | Auto Scaling |
| --- | --- | --- |
| **Service** | `FlowerySizeManager` | `FloweryScaleManager` |
| **Scope** | Entire app (global) | Only containers with `EnableScaling="True"` |
| **Scaling Type** | Discrete tiers (xs, s, m, l, xl) | Continuous (0.5× to 1.0×) |
| **Trigger** | User selection via dropdown | Automatic based on window size |
| **What Changes** | Height, padding, font size | Font size and scaled properties only |
| **Best For** | User preference, accessibility | Responsive data forms, dashboards |

### Scoped Usage

Auto Scaling is **opt-in and scoped**. Only controls inside a container with `EnableScaling="True"` are affected:

```xml
xmlns:services="using:Flowery.Services"

<Window>
    <!-- Sidebar: NOT affected by Auto Scaling (uses Global Size) -->
    <controls:FloweryComponentSidebar />
    
    <!-- Content area: ONLY this panel uses Auto Scaling -->
    <UserControl services:FloweryScaleManager.EnableScaling="True">
        <!-- These controls scale with window size -->
        <controls:DaisyInput Label="Name" />
        <controls:DaisyButton Content="Submit" />
    </UserControl>
</Window>
```

### Scoped Usage: How It Works

1. Set `EnableScaling="True"` on a container (UserControl, Window, Panel, etc.)
2. The manager tracks the parent **TopLevel** size (desktop `Window` or browser root)
3. Calculates a scale factor:
   - Downscaling: `min(width/1920, height/1080)`
   - If `MaxScaleFactor > 1.0`, scale-up is allowed based on the available dimension
   - Clamped to `MinScaleFactor` … `MaxScaleFactor`
4. **ONLY** child controls inside that container adjust their font sizes
5. Controls outside the `EnableScaling` region are NOT affected

### Scaling Up (4K / High-DPI Desktops)

By default, Auto Scaling only scales *down* (max is `1.0`). On large 4K windows (especially at 100% Windows scaling) you may want the UI to scale *up* as well.

To allow scaling above 1.0, set `FloweryScaleManager.MaxScaleFactor` to your desired **physical** maximum (for example `1.5` to match 150% Windows scaling):

```csharp
FloweryScaleManager.MaxScaleFactor = 1.5;
```

When `MaxScaleFactor > 1.0`, Flowery compensates for OS DPI scaling using `TopLevel.RenderScaling`, so you **don't double-scale** on systems already set to 125% / 150% Windows scaling.

**Safety**: `MaxScaleFactor` (and internal scale factors) are sanity-capped at **5.0 (500%)** to prevent extreme values.

### Toggling at Runtime

You can enable/disable Auto Scaling programmatically:

```csharp
// Disable Auto Scaling globally (scale factor becomes 1.0)
FloweryScaleManager.IsEnabled = false;

// Re-enable
FloweryScaleManager.IsEnabled = true;
```

When `IsEnabled = false`, all scale factors reset to 1.0 (no scaling).

### Supported Controls

Controls implementing `IScalableControl` auto-scale when inside an enabled container:

- `DaisyInput` / `DaisyTextArea` (label + text font)
- `DaisyButton` (content font)
- `DaisySelect` (font)
- `DaisyBadge` (content font)
- And many more...

### Common Patterns

#### Pattern 1: Global Size Only (Most Apps)

Most desktop apps should just use **Global Size**:

```xml
<!-- In sidebar or settings -->
<controls:DaisySizeDropdown />

<!-- All controls across the app respond to the user's size preference -->
```

#### Pattern 2: Auto Scaling for a Specific Page

Use Auto Scaling for a data-dense page that benefits from window-responsive sizing:

**XAML:**

```xml
xmlns:services="using:Flowery.Services"

<UserControl services:FloweryScaleManager.EnableScaling="True">
    <!-- Customer details form that scales with window -->
    <controls:DaisyCard x:Name="CustomerCard">
        <TextBlock x:Name="TitleText" Text="Customer" />
        <controls:DaisyInput Label="Name" />
        <controls:DaisyInput Label="Email" />
    </controls:DaisyCard>
</UserControl>
```

**Code-Behind:**

```csharp
using Flowery.Services;

public MyPage()
{
    InitializeComponent();
    
    // Subscribe to scale factor changes
    FloweryScaleManager.ScaleFactorChanged += OnScaleFactorChanged;
}

private void OnScaleFactorChanged(object? sender, ScaleChangedEventArgs e)
{
    ApplyScaling(e.ScaleFactor);
}

private void ApplyScaling(double scaleFactor)
{
    // Use ScaleHelper for preset-based scaling
    TitleText.FontSize = ScaleHelper.FontTitle(scaleFactor);
    CustomerCard.Padding = ScaleHelper.GetScaledThickness(FloweryScalePreset.SpacingMedium, scaleFactor);
}
```

> **Note:** WinUI/Uno requires code-behind for dynamic property scaling. Flowery controls implementing `IScalableControl` will automatically scale their text when inside an `EnableScaling` container.

#### Pattern 3: Override Auto Scaling with Global Size

If Auto Scaling is on, but you want specific controls to use the fixed Global Size instead, mark them to opt out:

```xml
<UserControl services:FloweryScaleManager.EnableScaling="True">
    <!-- These scale with window -->
    <controls:DaisyInput Label="Name" />
    
    <!-- These buttons stay at Global Size -->
    <StackPanel controls:FlowerySizeManager.IgnoreGlobalSize="True">
        <controls:DaisyButton Size="Small" Content="Cancel" />
        <controls:DaisyButton Size="Small" Content="Save" />
    </StackPanel>
</UserControl>
```

### When to Use Which?

| Scenario | Recommended Approach |
| --- | --- |
| Standard desktop app | **Global Size only** – simple, user-controlled |
| Accessibility needs | **Global Size** – discrete tiers are clearer |
| Data-dense responsive form | **Auto Scaling** on that specific page |
| Dashboard with charts/data | **Auto Scaling** for continuous responsiveness |
| Navigation/sidebar/toolbar | **Global Size only** – never use Auto Scaling |

### Learn More

For comprehensive documentation on `FloweryScaleManager`, including configuration, custom control support, and helper methods, see [FloweryScaleManager](https://tobitege.github.io/Flowery.Uno/#FloweryScaleManager).

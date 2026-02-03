# FlowerySizeManager

Static service for global size management across all Daisy controls. Provides discrete size tiers (ExtraSmall to ExtraLarge) that all controls respond to simultaneously.

## Quick Start

```csharp
using Flowery.Controls;

// Apply a global size
FlowerySizeManager.ApplySize(DaisySize.Medium);

// Or use a built-in dropdown
```

```xml
<controls:DaisySizeDropdown />
```

## Size Tiers

| Size | Typical Use Case | Height | Font Size |
| --- | --- | --- | --- |
| `ExtraSmall` | High-density UIs, data tables | 20px | 9px |
| `Small` | **Default** - Desktop apps | 24px | 10px |
| `Medium` | Touch-friendly, accessibility | 28px | 12px |
| `Large` | Larger screens, presentations | 32px | 14px |
| `ExtraLarge` | Maximum readability, kiosk | 36px | 16px |

> **Uno Platform Note:** All size values are defined in `DaisyResourceLookup` and can be accessed via `GetDefaultHeight(size)`, `GetDefaultFontSize(size)`, etc. See [Design Tokens](DesignTokens.md) for the full reference.

## API Reference

### Properties

```csharp
// Get the current global size
DaisySize currentSize = FlowerySizeManager.CurrentSize;

// Auto-apply global size to new controls (default: false)
FlowerySizeManager.UseGlobalSizeByDefault = true;
```

### Methods

```csharp
// Apply by enum
FlowerySizeManager.ApplySize(DaisySize.Large);

// Apply by name (returns true if successful)
bool success = FlowerySizeManager.ApplySize("Large");

// Reset to default (Small)
FlowerySizeManager.Reset();
```

### Events

```csharp
FlowerySizeManager.SizeChanged += (sender, size) =>
{
    Console.WriteLine($"Size changed to: {size}");
};
```

## Attached Properties

### IgnoreGlobalSize

Prevents a control (and its descendants) from responding to global size changes:

```xml
<!-- Single control -->
<controls:DaisyButton controls:FlowerySizeManager.IgnoreGlobalSize="True" 
                      Size="Large" Content="Always Large" />

<!-- Container (protects all children) -->
<StackPanel controls:FlowerySizeManager.IgnoreGlobalSize="True">
    <controls:DaisyButton Size="ExtraSmall" Content="XS" />
    <controls:DaisyButton Size="Small" Content="S" />
    <controls:DaisyButton Size="Medium" Content="M" />
    <controls:DaisyButton Size="Large" Content="L" />
    <controls:DaisyButton Size="ExtraLarge" Content="XL" />
</StackPanel>
```

### ResponsiveFont

Makes TextBlock font sizes respond to global size changes:

```xml
<TextBlock Text="Body text" 
           controls:FlowerySizeManager.ResponsiveFont="Primary" />

<TextBlock Text="Hint text" Opacity="0.7"
           controls:FlowerySizeManager.ResponsiveFont="Secondary" />

<TextBlock Text="Section Title" FontWeight="Bold"
           controls:FlowerySizeManager.ResponsiveFont="Header" />
```

#### Font Tiers

| Tier | Description | XS/S/M/L/XL Sizes |
| --- | --- | --- |
| `None` | No responsive sizing (default) | - |
| `Primary` | Body text, descriptions | 9/10/12/14/16 |
| `Secondary` | Hints, captions, labels | 8/9/10/12/14 |
| `Tertiary` | Very small text, counters | 8/8/9/10/12 |
| `SectionHeader` | Section titles within pages | 10/12/13/15/17 |
| `Header` | Large display headings | 12/14/17/20/24 |

#### How It Works

1. When `ResponsiveFont` is set, the TextBlock subscribes to `SizeChanged`
2. Font size is immediately applied based on current global size
3. Updates automatically when global size changes
4. Subscription is cleaned up when control is unloaded

> **Why not ThemeResource?** Uno Platform's nested resource dictionary scoping prevents dynamically updated resources from propagating reliably. The attached property approach (event subscription) is the recommended pattern for text that should scale with global size.

## Supported Controls

All Daisy controls with a `Size` property respond to global size changes:

- `DaisyButton`, `DaisyInput`, `DaisyTextArea`
- `DaisySelect`, `DaisyCheckBox`, `DaisyRadio`, `DaisyToggle`
- `DaisyBadge`, `DaisyProgress`, `DaisyRadialProgress`
- `DaisyTabs`, `DaisyMenu`, `DaisyKbd`
- `DaisyAvatar`, `DaisyLoading`, `DaisyFileInput`
- `DaisyNumericUpDown`, `DaisyDateTimeline`
- And more...

## Making Custom Controls Size-Aware

### Option 1: Use DaisyResourceLookup (Recommended)

Use centralized helper methods for consistent sizing:

```csharp
using Flowery.Theming;
using Flowery.Controls;

public class MyControl : ContentControl
{
    public MyControl()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        FlowerySizeManager.SizeChanged += OnSizeChanged;
        ApplySize(FlowerySizeManager.CurrentSize);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        FlowerySizeManager.SizeChanged -= OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, DaisySize size) => ApplySize(size);

    private void ApplySize(DaisySize size)
    {
        var resources = Application.Current?.Resources;
        if (resources != null)
            DaisyTokenDefaults.EnsureDefaults(resources);

        // Use centralized defaults - single source of truth
        MyTextBlock.FontSize = DaisyResourceLookup.GetDefaultFontSize(size);
        Height = DaisyResourceLookup.GetDefaultHeight(size);
        Padding = DaisyResourceLookup.GetDefaultPadding(size);
    }
}
```

### Option 2: Add a Size DependencyProperty

For DaisyUI-style controls in Uno/WinUI:

```csharp
using Flowery.Theming;
using Flowery.Controls;

public class MyDaisyControl : ContentControl
{
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(DaisySize),
            typeof(MyDaisyControl),
            new PropertyMetadata(DaisySize.Medium, OnSizePropertyChanged));

    public DaisySize Size
    {
        get => (DaisySize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    private static void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MyDaisyControl control)
            control.ApplySizing();
    }

    private void ApplySizing()
    {
        var resources = Application.Current?.Resources;
        if (resources != null)
            DaisyTokenDefaults.EnsureDefaults(resources);

        // Use centralized defaults
        Height = DaisyResourceLookup.GetDefaultHeight(Size);
        FontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
        Padding = DaisyResourceLookup.GetDefaultPadding(Size);
    }
}
```

> **Note:** Uno/WinUI doesn't support XAML selector-based styling for DependencyProperties. Apply sizing programmatically in the property changed callback using `DaisyResourceLookup`.

## Built-in UI Control

### DaisySizeDropdown

Ready-to-use dropdown for size selection:

```xml
<controls:DaisySizeDropdown />
```

Features:

- Shows current size with abbreviation (XS, S, M, L, XL)
- Localized size names (11 languages)
- Automatically updates all controls when selection changes

**Advanced:** Customize visible sizes and display names via `SizeOptions` property. See [DaisySizeDropdown](DaisySizeDropdown.md).

## Localization

Size names are localized. Add translations to your language files:

```json
{
  "Size_ExtraSmall": "Extra Small",
  "Size_Small": "Small",
  "Size_Medium": "Medium",
  "Size_Large": "Large",
  "Size_ExtraLarge": "Extra Large"
}
```

Supported: English, German, French, Spanish, Italian, Japanese, Korean, Arabic, Turkish, Ukrainian, Chinese (Simplified).

## Design Tokens Integration

Each size tier maps to specific [Design Tokens](DesignTokens.md):

| Size | Height Token | Font Size Token |
| --- | --- | --- |
| ExtraSmall | `DaisySizeExtraSmallHeight` (20) | `DaisySizeExtraSmallFontSize` (9) |
| Small | `DaisySizeSmallHeight` (24) | `DaisySizeSmallFontSize` (10) |
| Medium | `DaisySizeMediumHeight` (28) | `DaisySizeMediumFontSize` (12) |
| Large | `DaisySizeLargeHeight` (32) | `DaisySizeLargeFontSize` (14) |
| ExtraLarge | `DaisySizeExtraLargeHeight` (36) | `DaisySizeExtraLargeFontSize` (16) |

### Accessing Tokens Programmatically

In Uno Platform, use `DaisyResourceLookup` to access token values:

```csharp
using Flowery.Theming;

// Direct access to centralized defaults
double height = DaisyResourceLookup.GetDefaultHeight(DaisySize.Medium);  // 28
double fontSize = DaisyResourceLookup.GetDefaultFontSize(DaisySize.Medium);  // 12
Thickness padding = DaisyResourceLookup.GetDefaultPadding(DaisySize.Medium);  // 10,0,10,0
CornerRadius radius = DaisyResourceLookup.GetDefaultCornerRadius(DaisySize.Medium);  // 6

// For control-specific sizing
double iconSize = DaisyResourceLookup.GetDefaultIconSize(DaisySize.Medium);  // 14
double headerFont = DaisyResourceLookup.GetDefaultHeaderFontSize(DaisySize.Medium);  // 20
```

See [Design Tokens](DesignTokens.md) for the complete list of helper methods.

## Comparison: FlowerySizeManager vs FloweryScaleManager

| Feature | FlowerySizeManager | FloweryScaleManager |
| --- | --- | --- |
| **Purpose** | User preference / accessibility | Responsive window sizing |
| **Scope** | Entire app (global) | Only `EnableScaling="True"` containers |
| **Scaling** | Discrete tiers (XS, S, M, L, XL) | Continuous (0.5× to 1.0×) |
| **Trigger** | User selection | Automatic (window resize) |
| **Best For** | Desktop apps, accessibility | Data forms, dashboards |

> **Most apps should use FlowerySizeManager only.** FloweryScaleManager is an advanced feature for specific responsive scenarios.

## Best Practices

1. **Start with Small** - Default size works well for most desktop apps
2. **Provide a size picker** - Use `DaisySizeDropdown` for user control
3. **Test all sizes** - Ensure layouts work at ExtraSmall and ExtraLarge
4. **Use design tokens** - Not hardcoded values
5. **Clean up subscriptions** - Always unsubscribe from `SizeChanged` in `OnUnloaded`
6. **Use ResponsiveFont for TextBlocks** - Not DynamicResource

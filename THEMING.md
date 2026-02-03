# Theming Guide

This guide covers theme configuration and advanced theming features in Flowery.Uno.

## Quick Start

### Switch Themes in Code

```csharp
using Flowery.Controls;

// One-liner to switch themes
DaisyThemeManager.ApplyTheme("Synthwave");
```

### Switch Themes in XAML

```xml
xmlns:daisy="using:Flowery.Controls"

<!-- Drop-in theme switcher - no code-behind needed -->
<daisy:DaisyThemeDropdown MinWidth="220" />

<!-- Or a simple toggle -->
<daisy:DaisyThemeController Mode="Swap" />
```

---

## Available Themes

Flowery.Uno includes all 36 official DaisyUI themes:

| Light Themes | Dark Themes |
| --- | --- |
| Light, Acid, Autumn, Bumblebee, Caramellatte, Cmyk, Corporate, Cupcake, Cyberpunk, Emerald, Fantasy, Garden, Lemonade, Lofi, Nord, Pastel, Retro, Silk, Valentine, Winter, Wireframe | Dark, Abyss, Aqua, Black, Business, Coffee, Dim, Dracula, Forest, Halloween, Luxury, Night, Smooth, Sunset, Synthwave |

---

## Theme Controls

### DaisyThemeDropdown

Dropdown listing all 36 themes with color previews. Automatically syncs with `DaisyThemeManager.CurrentThemeName`.

```xml
<daisy:DaisyThemeDropdown MinWidth="220" />
<daisy:DaisyThemeDropdown SelectedTheme="{x:Bind ViewModel.CurrentTheme, Mode=TwoWay}" />
```

### DaisyThemeController

Flexible toggle between two themes with multiple presentation modes:

```xml
<!-- Simple toggle switch -->
<daisy:DaisyThemeController Mode="Toggle" />

<!-- Checkbox style -->
<daisy:DaisyThemeController Mode="Checkbox" />

<!-- Animated sun/moon swap -->
<daisy:DaisyThemeController Mode="Swap" />

<!-- Toggle with text labels -->
<daisy:DaisyThemeController Mode="ToggleWithText"
    UncheckedTheme="Light" CheckedTheme="Synthwave"
    UncheckedLabel="Light" CheckedLabel="Synthwave" />

<!-- Toggle with sun/moon icons -->
<daisy:DaisyThemeController Mode="ToggleWithIcons" />
```

### DaisyThemeSwap

Toggle button with animated sun/moon icons for light/dark switching.

```xml
<daisy:DaisyThemeSwap />
```

### DaisyThemeRadio

Radio button for selecting a specific theme. Use `GroupName` across multiple radios for a theme picker.

```xml
<daisy:DaisyThemeRadio ThemeName="Light" GroupName="themes" />
<daisy:DaisyThemeRadio ThemeName="Dark" GroupName="themes" />
<daisy:DaisyThemeRadio ThemeName="Synthwave" GroupName="themes" />
```

---

## DaisyThemeManager API

The centralized static class for theme management.

```csharp
using Flowery.Controls;

// Apply a theme
DaisyThemeManager.ApplyTheme("Synthwave");

// Get current theme
var current = DaisyThemeManager.CurrentThemeName;

// List all themes
foreach (var theme in DaisyThemeManager.AvailableThemes)
    Console.WriteLine($"{theme.Name} (Dark: {theme.IsDark})");

// Listen for changes
DaisyThemeManager.ThemeChanged += (sender, themeName) =>
    Console.WriteLine($"Theme changed to: {themeName}");

// Check if dark
bool isDark = DaisyThemeManager.IsDarkTheme("Dracula");
```

### Key Members

| Member | Description |
| --- | --- |
| `ApplyTheme(string)` | Loads and applies a theme by name |
| `CurrentThemeName` | Currently applied theme name |
| `AvailableThemes` | Read-only list of all theme info |
| `ThemeChanged` | Event fired after theme changes |
| `IsDarkTheme(string)` | Returns whether a theme is dark |

---

## Persisting Theme Preferences

When your app persists theme preferences, apply the saved theme before UI construction:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // Apply saved theme FIRST - controls will sync to it
    var savedTheme = LoadThemeFromSettings() ?? "Dark";
    DaisyThemeManager.ApplyTheme(savedTheme);

    // Then create the main window
    MainWindow = new MainWindow();
    MainWindow.Activate();
}
```

---

## How Theming Works

Flowery.Uno uses a palette-based architecture:

1. `DaisyPaletteFactory` produces per-theme resources with `DaisyBase100Brush`, `DaisyPrimaryBrush`, etc.
2. Themes are applied by applying palette resources into `Application.Current.Resources`
3. Stable brush instances are mutated (Color property) to ensure UI updates correctly
4. Controls use `ThemeResource` bindings to automatically update when themes change

### Key Palette Resources

| Resource | Usage |
| --- | --- |
| `DaisyBase100Brush` | Page/window background |
| `DaisyBase200Brush` | Elevated surfaces (cards, sidebar) |
| `DaisyBase300Brush` | Borders |
| `DaisyBaseContentBrush` | Default text/icon color |
| `DaisyPrimaryBrush` | Primary accent color |
| `DaisySecondaryBrush` | Secondary accent |
| `DaisyAccentBrush` | Tertiary accent |
| `DaisyInfoBrush` | Info status color |
| `DaisySuccessBrush` | Success status color |
| `DaisyWarningBrush` | Warning status color |
| `DaisyErrorBrush` | Error status color |

### Using Resources in XAML

```xml
<!-- Prefer ThemeResource for runtime theme updates -->
<Border Background="{ThemeResource DaisyBase200Brush}"
        BorderBrush="{ThemeResource DaisyBase300Brush}">
    <TextBlock Foreground="{ThemeResource DaisyBaseContentBrush}" Text="Hello"/>
</Border>
```

> **Important:** Use `ThemeResource` instead of `StaticResource` for colors that should update when themes change.

---

## Uno Platform Theming Considerations

### StaticResource vs ThemeResource

**`StaticResource`** resolves once at load time. If you swap theme resources later, the UI will **not** update.

**Always use `ThemeResource`** for any Daisy palette brushes in XAML:

```xml
<!-- ✅ DO: Updates when theme changes -->
<Border Background="{ThemeResource DaisyBase100Brush}" />

<!-- ❌ DON'T: Won't update when theme changes -->
<Border Background="{StaticResource DaisyBase100Brush}" />
```

### ThemeResource Limitations

`ThemeResource` updates most reliably when the app theme flips Light↔Dark. Switching between two dark themes (e.g., Dracula → Forest) may not trigger updates if you only replace a `MergedDictionaries` entry.

**Solution:** Flowery.Uno keeps brush *instances* stable and mutates their `Color` property when themes change. This is handled internally by `DaisyThemeManager`.
**Critical detail:** Do NOT remove and replace the palette `ResourceDictionary` at runtime. Replacing the dictionary leaves existing `ThemeResource` bindings pointing at old brush instances, so text/background colors stay on the previous theme until a page reload. Instead, keep the palette dictionary stable and update its brush colors in-place.

### Resource Lookup Differences

On Uno, `Application.Current.Resources.TryGetValue("SomeKey", ...)` can miss resources in `MergedDictionaries`. Flowery.Uno provides `DaisyResourceLookup` which recursively searches merged dictionaries.

### RequestedTheme Exceptions

Setting `Application.Current.RequestedTheme` can throw `COMException` on some Uno backends. Flowery.Uno wraps this in try/catch to prevent theme switching from failing silently.

---

## ThemeShadow & Elevation Notes

These are based on the official Uno tests in `uno/src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Media/ThemeShadowTests`.

- **ThemeShadow needs receivers**: always add a receiver (`ThemeShadow.Receivers.Add(receiverElement)`), typically a background grid/canvas.
- **Translation.Z controls shadow depth**: use `Translation="0,0,Z"` on the casting element.
- **CornerRadius is respected** when `ThemeShadow` is applied to a `Border`/`Rectangle` with radius set.
- **WASM rounded shadows**: apply `SetElevation` to the template `Border` (e.g., `ButtonBorder`) rather than the `Button` control for correct rounding.

---

## Lightweight Styling (Per-Control Customization)

WinUI/Uno supports "lightweight styling" - overriding theme resources for individual controls without creating full custom styles.

### Override a Single Control's Colors

Place a `ResourceDictionary` with `ThemeDictionaries` inside the control's `Resources`:

```xml
<Button Content="Custom Purple Button">
    <Button.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                    <SolidColorBrush x:Key="ButtonBackground" Color="Transparent"/>
                    <SolidColorBrush x:Key="ButtonForeground" Color="MediumSlateBlue"/>
                    <SolidColorBrush x:Key="ButtonBorderBrush" Color="MediumSlateBlue"/>
                </ResourceDictionary>
                <ResourceDictionary x:Key="Dark">
                    <SolidColorBrush x:Key="ButtonBackground" Color="Transparent"/>
                    <SolidColorBrush x:Key="ButtonForeground" Color="Coral"/>
                    <SolidColorBrush x:Key="ButtonBorderBrush" Color="Coral"/>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </Button.Resources>
</Button>
```

### Handle Pointer States

Override state-specific resources using standard suffixes:

| State | Suffix Example |
| --- | --- |
| Hover | `ButtonBackgroundPointerOver` |
| Pressed | `ButtonForegroundPressed` |
| Disabled | `ButtonBorderBrushDisabled` |

```xml
<CheckBox Content="Purple CheckBox">
    <CheckBox.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                    <SolidColorBrush x:Key="CheckBoxForegroundUnchecked" Color="Purple"/>
                    <SolidColorBrush x:Key="CheckBoxForegroundChecked" Color="Purple"/>
                    <SolidColorBrush x:Key="CheckBoxCheckGlyphForegroundChecked" Color="White"/>
                    <SolidColorBrush x:Key="CheckBoxCheckBackgroundFillChecked" Color="Purple"/>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </CheckBox.Resources>
</CheckBox>
```

### Page-Wide Overrides

Apply lightweight styling to all controls of a type on a page:

```xml
<Page.Resources>
    <ResourceDictionary>
        <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
                <SolidColorBrush x:Key="ButtonBackground" Color="Transparent"/>
                <SolidColorBrush x:Key="ButtonForeground" Color="MediumSlateBlue"/>
            </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>
    </ResourceDictionary>
</Page.Resources>

<!-- All buttons on this page get the custom colors -->
<Button Content="Button 1"/>
<Button Content="Button 2"/>
```

### App-Wide Overrides

Place theme overrides in `App.xaml` to affect all controls globally:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Your other resources -->
        </ResourceDictionary.MergedDictionaries>
        <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
                <SolidColorBrush x:Key="ButtonBackground" Color="Lavender"/>
            </ResourceDictionary>
            <ResourceDictionary x:Key="Dark">
                <SolidColorBrush x:Key="ButtonBackground" Color="DarkSlateBlue"/>
            </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

---

## Daisy Control Styling Resources

Flowery.Uno controls support **lightweight styling** through control-specific resource keys. This allows you to customize individual controls without creating full custom styles.

### Why Daisy-Specific Resources?

Since Daisy controls are **code-driven** (they build their visual tree programmatically in C#), standard WinUI template-based lightweight styling doesn't apply. Instead, Daisy controls check for resource overrides using a consistent naming pattern.

### Resource Naming Convention

All Daisy styling resources follow this pattern:

```text
Daisy{ControlName}{Part?}{State?}{Property}
```

Examples:

- `DaisyButtonBackground` – Button background
- `DaisyToggleKnobBrush` – Toggle switch knob color  
- `DaisyTableRowHoverBackground` – Table row hover state
- `DaisyInputPrimaryBorderBrush` – Primary variant border

### Lookup Priority

Resources are resolved in this order:

1. **Control.Resources** – Instance-level override (highest priority)
2. **Application.Current.Resources** – App-wide override
3. **Fallback** – Control's internal palette logic

### Application-Level Styling

Override in `App.xaml` to affect all instances of a control:

```xml
<Application.Resources>
    <ResourceDictionary>
        <SolidColorBrush x:Key="DaisyButtonBackground" Color="MediumSlateBlue"/>
        <SolidColorBrush x:Key="DaisyCardBackground" Color="#2A2A3A"/>
    </ResourceDictionary>
</Application.Resources>
```

### Instance-Level Styling

Override on a single control instance:

```xml
<daisy:DaisyButton Content="Custom Button">
    <daisy:DaisyButton.Resources>
        <SolidColorBrush x:Key="DaisyButtonBackground" Color="Coral"/>
        <SolidColorBrush x:Key="DaisyButtonForeground" Color="White"/>
    </daisy:DaisyButton.Resources>
</daisy:DaisyButton>
```

### Available Resource Keys

For a complete list of all control-specific resource keys, see:

📖 **[`llms-static/StylingResources.md`](llms-static/StylingResources.md)** – Full reference of 40+ controls with their styling resources.

#### Common Controls (Quick Reference)

| Control | Resources |
| --- | --- |
| **DaisyButton** | `Background`, `Foreground`, `BorderBrush` + variant-specific |
| **DaisyInput** | `Background`, `Foreground`, `BorderBrush` + variant-specific |
| **DaisyCard** | `Background`, `Foreground` |
| **DaisyBadge** | `Background`, `Foreground` + variant-specific |
| **DaisyToggle** | `KnobBrush`, `TrackBackground` + variant-specific |
| **DaisyProgress** | `FillBrush`, `TrackBrush` + variant-specific |
| **DaisyModal** | `BackdropBrush`, `Background` |

#### Variant-Specific Resources

Many controls support variant-specific overrides. The pattern is:

```text
Daisy{Control}{Variant}{Property}
```

Example for DaisyButton with Primary variant:

- `DaisyButtonPrimaryBackground`
- `DaisyButtonPrimaryForeground`
- `DaisyButtonPrimaryBorderBrush`

---

## Style Inheritance with BasedOn

Create style hierarchies using `BasedOn`:

```xml
<Page.Resources>
    <Style x:Key="BaseButtonStyle" TargetType="Button">
        <Setter Property="Height" Value="40" />
        <Setter Property="CornerRadius" Value="8" />
    </Style>

    <Style x:Key="PrimaryButtonStyle" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
        <Setter Property="Background" Value="{ThemeResource DaisyPrimaryBrush}" />
    </Style>

    <Style x:Key="SecondaryButtonStyle" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
        <Setter Property="Background" Value="{ThemeResource DaisySecondaryBrush}" />
    </Style>
</Page.Resources>
```

For system controls, base on the default style:

```xml
<Style TargetType="TextBox" BasedOn="{StaticResource DefaultTextBoxStyle}">
    <Setter Property="BorderBrush" Value="{ThemeResource DaisyPrimaryBrush}"/>
</Style>
```

---

## Implicit vs Explicit Styles

**Implicit styles** (no `x:Key`) apply automatically to all matching controls:

```xml
<Page.Resources>
    <!-- Applies to ALL Buttons on this page -->
    <Style TargetType="Button">
        <Setter Property="CornerRadius" Value="12" />
    </Style>
</Page.Resources>

<Button Content="Gets rounded corners automatically" />
```

**Explicit styles** (with `x:Key`) must be referenced:

```xml
<Page.Resources>
    <Style x:Key="RoundedButton" TargetType="Button">
        <Setter Property="CornerRadius" Value="12" />
    </Style>
</Page.Resources>

<Button Content="Default corners" />
<Button Content="Rounded" Style="{StaticResource RoundedButton}" />
```

---

## Avoid Restyling Controls

Prefer lightweight styling over full control templates. Custom templates ("re-templating") can break when WinUI updates and require maintenance.

**Recommended approach:**

1. Use `BasedOn` to inherit from default styles
2. Use lightweight styling to override specific resources
3. Only create custom templates when absolutely necessary

```xml
<!-- ✅ DO: Inherit and customize -->
<Style TargetType="Button" BasedOn="{StaticResource DefaultButtonStyle}">
    <Setter Property="CornerRadius" Value="20" />
</Style>

<!-- ❌ AVOID: Full template replacement (unless required) -->
<Style TargetType="Button">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <!-- Custom template that may break on updates -->
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

---

## Control Development Guidelines

When creating custom controls that need to respect themes:

### Subscribe to ThemeChanged (with proper lifecycle)

Always subscribe in `Loaded` and unsubscribe in `Unloaded` to prevent memory leaks:

```csharp
public MyControl()
{
    InitializeComponent();
    Loaded += OnLoaded;
    Unloaded += OnUnloaded;
}

private void OnLoaded(object sender, RoutedEventArgs e)
{
    DaisyThemeManager.ThemeChanged += OnThemeChanged;
    ApplyTheme();
}

private void OnUnloaded(object sender, RoutedEventArgs e)
{
    DaisyThemeManager.ThemeChanged -= OnThemeChanged;
}

private void OnThemeChanged(object? sender, string themeName)
{
    ApplyTheme();
}

private void ApplyTheme()
{
    Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
}
```

> **Important:** The `DaisyThemeManager.ApplyTheme()` method mutates brush colors in-place, which works for code-behind lookups. However, XAML `{ThemeResource}` bindings do **not** automatically update when switching between themes of the same Light/Dark category (e.g., Dark → Dracula). Always use code-behind for controls that must update between same-category theme switches.

### Use the Right Tokens

| Surface Type | Token |
| --- | --- |
| Page/window background | `DaisyBase100Brush` |
| Card/elevated surface | `DaisyBase200Brush` |
| Borders | `DaisyBase300Brush` |
| Text/icons | `DaisyBaseContentBrush` |
| Primary actions | `DaisyPrimaryBrush` |
| Success states | `DaisySuccessBrush` |
| Error states | `DaisyErrorBrush` |

---

## Debugging Theme Issues

If theme switching appears "dead":

1. **Check if `ThemeChanged` fires** - persistence depends on this event
2. **Look for exceptions** - `RequestedTheme` can throw silently

### Common Issues

| Symptom | Cause | Fix |
| --- | --- | --- |
| UI doesn't update on theme change | Using `StaticResource` | Switch to `ThemeResource` |
| Text stays white on light themes | Missing `Foreground` binding | Explicitly bind to `DaisyBaseContentBrush` |
| Theme change works once then stops | Exception in theme apply | Check debug log for errors |
| Controls created in code-behind don't update | Brushes cached at creation | Re-apply brushes on `ThemeChanged` |

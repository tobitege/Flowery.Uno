<!-- Supplementary documentation for DaisyProductThemeDropdown -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyProductThemeDropdown is a specialized theme dropdown that provides **96 industry-specific color palettes** from the [UI UX Pro Max Skill](https://github.com/nextlevelbuilder/ui-ux-pro-max-skill) project. Unlike the standard `DaisyThemeDropdown` (which uses the 35+ DaisyUI themes), this control uses professionally crafted palettes designed for specific product types and industries.

## When to Use

| Scenario | Recommended Control |
| --- | --- |
| Standard DaisyUI theming (Light, Dark, Dracula, etc.) | `DaisyThemeDropdown` |
| Industry-specific product themes (SaaS, Fintech, Healthcare, etc.) | `DaisyProductThemeDropdown` ✓ |

## Available Industries & Themes

The 96 product themes span 12+ industry categories:

| Industry | Example Themes |
| --- | --- |
| **SaaS** | SaaS, SaaSLight, SaaSDark, Enterprise, StartupFresh |
| **E-commerce** | EcommModern, EcommLuxury, EcommMinimal, EcommBold |
| **Fintech** | FintechTrust, FintechModern, FintechBold, CryptoNeon |
| **Healthcare** | HealthCalm, HealthPro, HealthVitality, MedicalClean |
| **EdTech** | EduBright, EduPro, LearningFun, AcademicClassic |
| **Travel** | TravelAdventure, TravelLuxury, TravelBudget |
| **Food & Beverage** | FoodFresh, FoodOrganic, CafeCozy, RestaurantElegant |
| **Real Estate** | RealtyPremium, RealtyModern, RealtyTrust |
| **Fitness** | FitEnergy, FitCalm, GymPower, WellnessZen |
| **Gaming** | GameNeon, GameRetro, GameDark, ESportsVibes |
| **Social Media** | SocialVibrant, SocialClean, InfluencerPink |
| **Productivity** | ProdClean, ProdFocus, TaskMaster, OfficePro |

## Properties

| Property | Type | Description |
| --- | --- | --- |
| `SelectedTheme` | `string` | Name of the currently selected product theme. |
| `ApplyOnSelection` | `bool` | When true (default), selecting a theme immediately applies it globally. Set to false to only update SelectedTheme without applying. |
| `ProductThemeSelected` | `event` | Event raised when a product theme is selected, with the theme name. |

## How It Works

1. The dropdown populates from `ProductPalettes.GetAllNames()` (pre-compiled for performance)
2. Each theme shows a color preview with Primary, Secondary, Accent, and Neutral colors
3. When selected, the theme is:
   - Registered with `DaisyThemeManager` as a dynamic theme
   - Applied via `DaisyThemeManager.ApplyTheme()` for consistent resource updates
4. Product themes integrate seamlessly with the standard DaisyUI theme system

## Quick Examples

```xml
<!-- Add namespace -->
xmlns:daisy="using:Flowery.Controls"

<!-- Basic product theme dropdown -->
<daisy:DaisyProductThemeDropdown Width="200" />

<!-- With size variants -->
<daisy:DaisyProductThemeDropdown Size="Small" />
<daisy:DaisyProductThemeDropdown Size="Medium" />
<daisy:DaisyProductThemeDropdown Size="Large" />

<!-- Without immediate application -->
<daisy:DaisyProductThemeDropdown 
    x:Name="ThemeSelector"
    ApplyOnSelection="False"
    ProductThemeSelected="OnProductThemeSelected" />
```

### Code-Behind Usage

```csharp
using Flowery.Theming;

// Get all available product theme names
var allThemes = ProductPalettes.GetAllNames();

// Get a specific palette
var saasTheme = ProductPalettes.Get("SaaS");
if (saasTheme != null)
{
    // Access palette colors
    string primaryColor = saasTheme.Primary;    // e.g., "#6366F1"
    string accentColor = saasTheme.Accent;
    bool isDark = FloweryColorHelpers.IsDark(saasTheme.Base100);
}

// Apply a product theme programmatically
var palette = ProductPalettes.Get("FintechTrust");
if (palette != null)
{
    DaisyThemeManager.RegisterTheme(
        new DaisyThemeInfo("FintechTrust", FloweryColorHelpers.IsDark(palette.Base100)),
        () => DaisyPaletteFactory.Create(palette));
    DaisyThemeManager.ApplyTheme("FintechTrust");
}
```

## Differences from DaisyThemeDropdown

| Feature | DaisyThemeDropdown | DaisyProductThemeDropdown |
| --- | --- | --- |
| Theme source | 35+ DaisyUI themes | 96 product palettes |
| Default theme | "Dark" | "SaaS" |
| Theme sync | Syncs with `CurrentThemeName` | Independent (no sync) |
| Use case | General theming | Industry-specific apps |

## ProductPalette Structure

Each product palette provides:

```csharp
public class ProductPalette
{
    public string Primary { get; }      // Primary brand color
    public string Secondary { get; }    // Secondary brand color  
    public string Accent { get; }       // Accent/highlight color
    public string Neutral { get; }      // Neutral/text color
    public string Base100 { get; }      // Background color
    public string Base200 { get; }      // Card background
    public string Base300 { get; }      // Border/divider
    public string BaseContent { get; }  // Primary text
    public string Info { get; }         // Info state
    public string Success { get; }      // Success state
    public string Warning { get; }      // Warning state
    public string Error { get; }        // Error state
}
```

## Tips & Best Practices

- Use `DaisyProductThemeDropdown` when building industry-specific applications that need tailored color schemes
- Product themes are registered dynamically with `DaisyThemeManager`, so they work with all theme-aware controls
- For apps that need both DaisyUI and product themes, you can include both dropdowns
- The `ProductThemeSelected` event is useful for analytics or custom behavior on theme change
- Set `ApplyOnSelection="False"` if you want to preview the theme before applying

## Related Controls

- [DaisyThemeDropdown](DaisyThemeDropdown.md) - Standard DaisyUI theme selection
- [DaisyThemeManager](DaisyThemeManager.md) - Central theme management
- [DaisyThemeController](DaisyThemeController.md) - Light/dark toggle

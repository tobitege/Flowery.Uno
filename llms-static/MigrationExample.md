# Migration Guide: Integrating Flowery.Uno into Existing Apps

This guide explains when and how to use `CustomThemeApplicator` to integrate Flowery.Uno's theme controls into apps with existing theming architectures. For general theme management, see [DaisyThemeManager](controls/DaisyThemeManager.html).

## When You Need CustomThemeApplicator

The default `DaisyThemeManager.ApplyTheme()` works by adding/removing palette resources from `Application.Resources.MergedDictionaries`. This works great for simple apps, but you may need `CustomThemeApplicator` if:

| Scenario | Why Default Doesn't Work |
| --- | --- |
| **Custom MergedDictionaries** in `Application.Resources` | Resource resolution conflicts; bindings don't refresh |
| **Theme persistence** needed | Default doesn't save to settings |
| **Additional actions** on theme change | Logging, analytics, UI updates, etc. |
| **Complex resource hierarchies** | Multiple ResourceDictionaries that need coordinated updates |

If your app works fine with the default behavior, you don't need `CustomThemeApplicator`.

---

## Example: App with Custom Resources

### The Starting Point

You have an Uno/WinUI app with custom resources in `Application.Resources`:

```xml
<!-- App.xaml (before Flowery.Uno) -->
<Application ...>
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="ms-appx:///Themes/CustomResources.xaml" />
                <ResourceDictionary Source="ms-appx:///Themes/Icons.xaml" />
            </ResourceDictionary.MergedDictionaries>
            <SolidColorBrush x:Key="MyCustomBrush" Color="#808080"/>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### Step 1: Add Flowery.Uno

Add the Flowery.Uno library reference and include the theme resources:

> **Namespace Note:** Flowery.Uno uses the namespace `Flowery.Controls` for all controls.

```xml
<!-- App.xaml (with Flowery.Uno added) -->
<Application ...
             xmlns:controls="using:Flowery.Controls">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Your existing resources -->
                <ResourceDictionary Source="ms-appx:///Themes/CustomResources.xaml" />
                <ResourceDictionary Source="ms-appx:///Themes/Icons.xaml" />
                <!-- Add Flowery.Uno resources -->
                <ResourceDictionary Source="ms-appx:///Flowery.Uno/Themes/Generic.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Initialize the theme in your `App.xaml.cs`:

```csharp
// App.xaml.cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // Initialize default tokens and apply a theme
    DaisyTokenDefaults.EnsureDefaults();
    DaisyThemeManager.ApplyTheme("Dark");
    
    // ... rest of your startup code
}
```

Your views now use Daisy resources:

```xml
<Border Background="{ThemeResource DaisyBase200Brush}"
        BorderBrush="{ThemeResource DaisyBase300Brush}">
    <TextBlock Foreground="{ThemeResource DaisyBaseContentBrush}" 
               Text="Hello Flowery!"/>
</Border>
```

### Step 2: Add Theme Dropdown - The Problem Appears

You add `DaisyThemeDropdown` for runtime theme switching:

```xml
<controls:DaisyThemeDropdown Width="220"/>
```

**What happens:**

- The dropdown appears ✓
- You select "Synthwave" from the list ✓
- **Nothing changes** ✗

The colors don't update. If you restart the app, it's still using the old theme.

### Why It Doesn't Work

`DaisyThemeDropdown` internally calls `DaisyThemeManager.ApplyTheme()`, which modifies `Application.Resources.MergedDictionaries`.

**If your app has complex resource hierarchies**, the new palette may not refresh properly because:

1. WinUI/Uno resource resolution may not trigger a full refresh for MergedDictionaries changes
2. Controls may still see cached brush values from `ThemeResource` bindings
3. The theme change doesn't automatically persist across app restarts

**Note:** This issue depends on your specific resource setup. Many apps work fine with the default; others need custom handling.

---

## The Solution: CustomThemeApplicator

### Step 1: Create Your Custom Applicator

Write a method that applies themes the way your app needs:

```csharp
// App.xaml.cs
public static bool ApplyThemeCustom(string themeName)
{
    var themeInfo = DaisyThemeManager.GetThemeInfo(themeName);
    if (themeInfo == null) return false;

    var app = Application.Current;
    if (app?.Resources == null) return false;

    // Get the palette from DaisyPaletteFactory
    var palette = DaisyPaletteFactory.CreatePalette(themeInfo.Name);
    if (palette == null) return false;

    // Remove existing Daisy palette if present
    var existingPalette = app.Resources.MergedDictionaries
        .FirstOrDefault(d => d.ContainsKey("DaisyBase100Brush"));
    if (existingPalette != null)
    {
        app.Resources.MergedDictionaries.Remove(existingPalette);
    }

    // Add new palette
    app.Resources.MergedDictionaries.Add(palette);

    // Optional: Persist to settings
    // Windows.Storage.ApplicationData.Current.LocalSettings.Values["Theme"] = themeName;

    // Notify any listeners
    DaisyThemeManager.RaiseThemeChanged(themeName);

    return true;
}
```

### Step 2: Wire It Up at Startup

```csharp
// App.xaml.cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // Tell Flowery.Uno to use your custom applicator
    DaisyThemeManager.CustomThemeApplicator = ApplyThemeCustom;

    // Initialize tokens
    DaisyTokenDefaults.EnsureDefaults();

    // Load saved theme or use default
    var savedTheme = Windows.Storage.ApplicationData.Current.LocalSettings.Values["Theme"] as string;
    DaisyThemeManager.ApplyTheme(savedTheme ?? "Dark");

    // ... rest of your startup code
}
```

### Result

**Now all Flowery.Uno theme controls work with your custom logic:**

- `DaisyThemeDropdown` works ✓
- `DaisyThemeController` works ✓  
- `DaisyThemeRadio` works ✓
- Theme persists across restarts ✓
- All theme brushes refresh properly ✓

---

## Other Use Cases

### Persistence Only

If the default theme application works but you want to save the selected theme:

```csharp
DaisyThemeManager.CustomThemeApplicator = themeName =>
{
    // Apply theme using default logic
    var result = DaisyThemeManager.ApplyThemeInternal(themeName);
    if (result)
    {
        // Persist the theme
        Windows.Storage.ApplicationData.Current.LocalSettings.Values["Theme"] = themeName;
    }
    return result;
};
```

### Logging/Analytics

```csharp
DaisyThemeManager.CustomThemeApplicator = themeName =>
{
    // Use your logging framework
    Debug.WriteLine($"Theme changing to: {themeName}");
    
    // Optional: track with your analytics service
    // Analytics.Track("theme_changed", new { theme = themeName });
    
    // Do the actual theme application
    return ApplyThemeCustom(themeName);
};
```

### Chained Actions

```csharp
DaisyThemeManager.CustomThemeApplicator = themeName =>
{
    var result = ApplyThemeCustom(themeName);
    if (result)
    {
        // Update other UI elements
        MainViewModel.Instance?.RefreshColors();
        
        // Notify plugins or other components
        EventAggregator.Publish(new ThemeChangedEvent(themeName));
    }
    return result;
};
```

---

## Summary

| Before CustomThemeApplicator | After |
| --- | --- |
| Built-in theme controls may not work with custom resource setups | All controls work seamlessly |
| Had to build custom theme UI | Use `DaisyThemeDropdown` etc. directly |
| Theme persistence required separate handling | Persistence built into your applicator |
| Complex workarounds | Single line of setup |

The `CustomThemeApplicator` pattern gives you full control over how themes are applied while still benefiting from Flowery.Uno's theme UI controls.

---

## Platform-Specific Notes

### Windows (WinUI)

- Use `Windows.Storage.ApplicationData.Current.LocalSettings` for theme persistence
- `ThemeResource` bindings work but may need explicit refresh for dynamic changes

### Future Platforms (Browser, Android, iOS)

When Flowery.Uno adds support for additional platforms:

- Browser (WebAssembly): Use browser localStorage for persistence
- Mobile: Use platform-specific settings APIs
- Consider using `Preferences` from MAUI Essentials if available

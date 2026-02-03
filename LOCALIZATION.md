# Flowery.Uno Localization Guide

Flowery.Uno provides built-in localization support for theme names and control text, with easy extensibility for additional languages. The API lives in the Flowery.Localization namespace and centers on the FloweryLocalization class.

## Supported Languages

The library comes with built-in translations for the following languages:

- English (en, default)
- German (de)
- French (fr)
- Spanish (es)
- Italian (it)
- Mandarin Simplified (zh-CN)
- Korean (ko)
- Japanese (ja)
- Arabic (ar)
- Turkish (tr)
- Ukrainian (uk)
- Hebrew (he)

## Quick Start

### Switching Languages at Runtime

```csharp
using Flowery.Localization;

// Switch to German
FloweryLocalization.SetCulture("de");

// Switch to Mandarin
FloweryLocalization.SetCulture("zh-CN");
```

### Accessing Supported Languages

FloweryLocalization exposes a custom LanguageList for supported language codes and a LanguageDisplayNameMap for display names. These types are enumerable and provide indexers, but they do not implement `IReadOnlyList<string>` or `IReadOnlyDictionary<string, string>`.

```csharp
using Flowery.Localization;

var codes = FloweryLocalization.SupportedLanguages;
var current = FloweryLocalization.CurrentCultureName;

for (var i = 0; i < codes.Count; i++)
{
    var code = codes[i];
    if (FloweryLocalization.LanguageDisplayNames.TryGetValue(code, out var name))
    {
        // Use code and name in your UI
    }
}
```

### XAML Binding in Uno/WinUI Heads (Required)

All Uno heads (Windows, Desktop/Skia, Android, iOS, WASM) use the WinUI XAML compiler and share the same binding rules. Use standard Binding with the indexer and do NOT use x:Bind indexer syntax (it triggers WMC1110).

Note: Flowery.Uno does not ship a {loc:Localize} markup extension. Use the indexer binding patterns below.

Preferred pattern (set DataContext once):

```xml
<ScrollViewer DataContext="{x:Bind Localization, Mode=OneWay}">
    <TextBlock Text="{Binding [Gallery_DataInput_Variants]}" />
</ScrollViewer>
```

Alternative (explicit source per binding):

```xml
<TextBlock Text="{Binding [Gallery_DataInput_Variants], Source={x:Bind Localization}}" />
```

Never use:

```xml
<!-- Invalid in WinUI/Uno -->
<TextBlock Text="{x:Bind Localization['Gallery_DataInput_Variants']}" />
<TextBlock Text="{x:Bind Localization[Gallery_DataInput_Variants]}" />
```

### Desktop/Skia Runtime Language Switching

FloweryLocalization.SetCulture raises PropertyChanged on the UI thread via DispatcherQueue. Most Uno heads refresh indexer bindings automatically. If you still see stale values on Desktop/Skia, force a rebinding for the element where you set the Localization DataContext.

Fallback example:

```csharp
// In your page/control that uses {Binding [Key]} with a Localization DataContext.
Loaded += (_, _) => FloweryLocalization.CultureChanged += OnCultureChanged;
Unloaded += (_, _) => FloweryLocalization.CultureChanged -= OnCultureChanged;

private void OnCultureChanged(object? sender, string cultureName)
{
    RootElement.DataContext = null;
    RootElement.DataContext = Localization;
}
```

### Providing App-Specific Translations for Library Controls

FloweryLocalization.GetString uses CustomResolver when it is set. This is intended for app-owned keys (for example, FloweryComponentSidebar labels). Library controls that use built-in keys call GetStringInternal, so they are not affected by CustomResolver.

```csharp
// In your app's localization setup or App startup:
FloweryLocalization.CustomResolver = MyAppLocalization.GetString;
```

This allows:

- Library controls that call FloweryLocalization.GetString("Sidebar_Home") to resolve your app keys
- Your resolver to supply translations from your app's JSON files

If you set CustomResolver, make sure it returns the key when a value is not found so the UI degrades gracefully.

### Date Formatting

The DaisyDateTimeline control uses the Locale property for date formatting:

```xml
<daisy:DaisyDateTimeline Locale="de-DE" />
```

Or bind to the current UI culture name:

```csharp
using System.Globalization;

timeline.Locale = CultureInfo.GetCultureInfo(FloweryLocalization.CurrentCultureName);
```

---

## JSON-Based Localization

Flowery.Uno uses embedded JSON files for localization. This approach:

- Works across all Uno Platform targets (Desktop, WASM, Android, iOS)
- Avoids satellite assembly loading issues
- Supports CJK/Arabic characters
- Is AOT/trimming compatible with source generators

### Step-by-Step: App Localization Setup

#### 1. Create JSON Translation Files

Create JSON files in your Localization/ folder:

Localization/en.json (English - fallback):

```json
{
  "Effects_Reveal_Title": "Reveal Effect",
  "Effects_Reveal_Description": "Entrance animations when element enters the visual tree."
}
```

Localization/de.json (German):

```json
{
  "Effects_Reveal_Title": "Reveal Effect",
  "Effects_Reveal_Description": "Entrance animations when an element enters the visual tree."
}
```

#### 2. Embed JSON Files in .csproj

Add all JSON files as embedded resources:

```xml
<ItemGroup>
  <EmbeddedResource Include="Localization\\en.json" />
  <EmbeddedResource Include="Localization\\de.json" />
  <EmbeddedResource Include="Localization\\ja.json" />
  <!-- Add all other languages -->
</ItemGroup>
```

#### 3. Create JSON Localization Loader

Create a localization class that loads from embedded JSON:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MyAppLocalization : INotifyPropertyChanged
{
    private static CultureInfo _currentCulture = CultureInfo.CurrentUICulture;
    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new();
    private static readonly Lazy<MyAppLocalization> _instance = new(() => new MyAppLocalization());

    public static MyAppLocalization Instance => _instance.Value;
    public static event EventHandler<CultureInfo>? CultureChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    static MyAppLocalization()
    {
        // Load all translations at startup - use library's centralized list
        foreach (var lang in Flowery.Localization.FloweryLocalization.SupportedLanguages)
            LoadTranslation(lang);

        // Sync with Flowery library culture changes
        Flowery.Localization.FloweryLocalization.CultureChanged += (s, c) => SetCulture(c);
    }

    // Indexer for XAML binding
    public string this[string key] => GetString(key);

    public static void SetCulture(CultureInfo culture)
    {
        if (_currentCulture.Name == culture.Name) return;
        _currentCulture = culture;
        CultureChanged?.Invoke(null, culture);
        Instance.PropertyChanged?.Invoke(Instance, new PropertyChangedEventArgs("Item"));
        Instance.PropertyChanged?.Invoke(Instance, new PropertyChangedEventArgs("Item[]"));
    }

    public static string GetString(string key)
    {
        // Try exact culture (e.g., "de-DE")
        if (_translations.TryGetValue(_currentCulture.Name, out var exact) && exact.TryGetValue(key, out var v1))
            return v1;

        // Try language only (e.g., "de")
        var lang = _currentCulture.TwoLetterISOLanguageName;
        if (_translations.TryGetValue(lang, out var langDict) && langDict.TryGetValue(key, out var v2))
            return v2;

        // Fallback to English
        if (_translations.TryGetValue("en", out var en) && en.TryGetValue(key, out var v3))
            return v3;

        return key; // Return key if not found
    }

    private static void LoadTranslation(string langCode)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"YourApp.Localization.{langCode}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return;

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        // Use source generator for AOT compatibility
        var dict = JsonSerializer.Deserialize(json, LocalizationJsonContext.Default.DictionaryStringString);
        if (dict != null) _translations[langCode] = dict;
    }
}

// REQUIRED: JSON Source Generator for AOT/WASM compatibility
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class LocalizationJsonContext : JsonSerializerContext { }
```

Important: The JsonSerializerContext source generator is required for WASM. Without it, you will get JsonSerializerIsReflectionDisabled errors.

---

## CJK Font Support (Japanese, Korean, Chinese)

For CJK characters to display correctly, you must embed fonts and apply styles in your consuming application.

Important: Flowery.Uno does NOT ship with CJK fonts. Each consuming application that needs CJK support must embed its own fonts.

### 1. Embed CJK Fonts in Your App

Include Noto Sans CJK fonts in your application's .csproj:

```xml
<ItemGroup>
  <Content Include="Assets\\Fonts\\NotoSansJP-Regular.otf" />
  <Content Include="Assets\\Fonts\\NotoSansSC-Regular.otf" />
  <Content Include="Assets\\Fonts\\NotoSansKR-Regular.otf" />
</ItemGroup>
```

### 2. Define Font Family Resource

In App.xaml:

```xml
<Application.Resources>
    <FontFamily x:Key="NotoSansFamily">ms-appx:///Assets/Fonts/NotoSansJP-Regular.otf#Noto Sans JP</FontFamily>
</Application.Resources>
```

### 3. Apply Font to All Text Controls

```xml
<Application.Resources>
    <Style TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource NotoSansFamily}" />
    </Style>
    <Style TargetType="TextBox">
        <Setter Property="FontFamily" Value="{StaticResource NotoSansFamily}" />
    </Style>
    <!-- Add other text controls as needed -->
</Application.Resources>
```

---

## Available Resource Keys

The built-in keys live in Flowery.Uno/Localization/en.json. The list below is complete as of this version.

```text
# Size
Size_ExtraSmall
Size_Small
Size_Medium
Size_Large
Size_ExtraLarge

# Select
Select_Placeholder

# MaskInput watermarks
MaskInput_Watermark_AlphaNumericCode
MaskInput_Watermark_Timer
MaskInput_Watermark_ExpiryShort
MaskInput_Watermark_ExpiryLong
MaskInput_Watermark_CreditCardNumber
MaskInput_Watermark_Cvc

# Accessibility
Accessibility_Loading
Accessibility_Progress
Accessibility_Rating
Accessibility_LoadingPlaceholder
Accessibility_Status
Accessibility_StatusOnline
Accessibility_StatusError
Accessibility_StatusWarning
Accessibility_StatusInfo
Accessibility_StatusActive
Accessibility_StatusSecondary
Accessibility_StatusHighlighted
Accessibility_Countdown
Accessibility_NumberFlow

# Tabs and palettes
Tabs_CloseTab
Tabs_CloseOtherTabs
Tabs_CloseTabsToRight
Tabs_TabColor
Tabs_Palette_Default
Tabs_Palette_Purple
Tabs_Palette_Indigo
Tabs_Palette_Pink
Tabs_Palette_SkyBlue
Tabs_Palette_Blue
Tabs_Palette_Lime
Tabs_Palette_Green
Tabs_Palette_Yellow
Tabs_Palette_Orange
Tabs_Palette_Red
Tabs_Palette_Gray

# Themes
Theme_Abyss
Theme_Acid
Theme_Aqua
Theme_Autumn
Theme_Black
Theme_Bumblebee
Theme_Business
Theme_Caramellatte
Theme_Cmyk
Theme_Coffee
Theme_Corporate
Theme_Cupcake
Theme_Cyberpunk
Theme_Dark
Theme_Dim
Theme_Dracula
Theme_Emerald
Theme_Fantasy
Theme_Forest
Theme_Garden
Theme_Halloween
Theme_Lemonade
Theme_Light
Theme_Lofi
Theme_Luxury
Theme_Night
Theme_Nord
Theme_Pastel
Theme_Retro
Theme_Silk
Theme_Smooth
Theme_Sunset
Theme_Synthwave
Theme_Valentine
Theme_Winter
Theme_Wireframe

# Input and buttons
Input_Optional
CopyButton_Copy
CopyButton_Copied

# Tag picker
TagPicker_SelectedTags
TagPicker_AvailableTags

# File input
FileInput_NoFileChosen
FileInput_ChooseFile

# Contribution graph
ContributionGraph_Less
ContributionGraph_More
ContributionGraph_NoContributions
ContributionGraph_OneContribution
ContributionGraph_Contributions

# Slide to confirm
SlideToConfirm_Text
SlideToConfirm_Confirming

# Sidebar
Sidebar_SearchPlaceholder

# Clock
Clock_Hours_Singular
Clock_Hours_Plural
Clock_Hours_Short
Clock_Minutes_Singular
Clock_Minutes_Plural
Clock_Minutes_Short
Clock_Seconds_Singular
Clock_Seconds_Plural
Clock_Seconds_Short
Clock_AM
Clock_PM
Clock_Alarm_Next
Clock_Timer_Finished
Clock_Stopwatch
Clock_Lap

# Common
Common_Save
Common_Cancel
Common_Close
Common_Add
Common_Apply
Common_Clear
Common_Delete
Common_ConfirmDelete
Common_Disconnect
```

---

## Handling Culture Changes

When culture changes, bound strings need to update. For ViewModel properties:

```csharp
public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        FloweryLocalization.CultureChanged += (s, culture) =>
        {
            OnPropertyChanged(nameof(Greeting));
        };
    }

    public string Greeting => FloweryLocalization.GetString("Greeting_Text");
}
```

---

## Troubleshooting

### Japanese/Korean/Chinese shows as boxes (???)

Cause: CJK fonts not loaded or not applied.
Fix:

1. Embed Noto Sans CJK fonts as Content
2. Apply font to all text-displaying controls in App.xaml

### JsonSerializerIsReflectionDisabled error

Cause: WASM has Native AOT which disables reflection.
Fix: Add a JsonSerializerContext source generator (see Step 3 above).

---

## Contributing Translations

1. Fork the repository
2. Add your JSON file for your language
3. Submit a pull request

Contributions for any language are welcome!

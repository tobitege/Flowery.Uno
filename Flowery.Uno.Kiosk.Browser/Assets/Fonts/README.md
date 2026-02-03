# Noto Sans Fonts for Multilingual Support

This folder contains Noto Sans font files for proper CJK (Chinese, Japanese, Korean) and Arabic character support in the Uno Platform application.

## Included Fonts

| File | Purpose | Size |
| --- | --- | --- |
| `NotoSans-Regular.ttf` | Latin, Cyrillic, Greek | ~560KB |
| `NotoSansSC-Regular.otf` | Simplified Chinese (Standalone) | ~16MB |
| `NotoSansJP-Regular.otf` | Japanese (Standalone) | ~16MB |
| `NotoSansKR-Regular.otf` | Korean (Standalone) | ~16MB |
| `NotoSansArabic-Regular.ttf` | Arabic script | ~240KB |

**Total Size:** ~48MB (Note: Builds may be large, but this ensures full CJK support)

## License

These fonts are from the [Google Noto Fonts](https://fonts.google.com/noto) project and are licensed under the **SIL Open Font License, Version 1.1 (OFL-1.1)**.

### What This Means

✅ **Free to use** - in personal and commercial projects  
✅ **Free to distribute** - can be bundled with applications  
✅ **Free to modify** - derivative works are allowed  
✅ **No attribution required** - though appreciated  

The OFL is one of the most permissive font licenses available. The full license text is available at:
https://scripts.sil.org/OFL

For a practical guide on using OFL fonts, see:
https://openfontlicense.org/how-to-use-ofl-fonts/

> *"The OFL allows the licensed fonts to be used, studied, modified and redistributed freely as long as they are not sold by themselves."*

## Why These Are Committed

Unlike some platforms, Uno Platform applications may need embedded fonts to ensure consistent character rendering across all targets. These fonts ensure the language dropdown and all localized UI text display correctly across all supported languages.

## Configuration

Fonts are configured in `App.xaml` as a FontFamily resource with fallback chain:

```xml
<FontFamily x:Key="NotoSansFamily">
    ms-appx:///Assets/Fonts/NotoSans-Regular.ttf#Noto Sans,
    ms-appx:///Assets/Fonts/NotoSansSC-Regular.otf#Noto Sans CJK SC,
    ms-appx:///Assets/Fonts/NotoSansJP-Regular.otf#Noto Sans CJK JP,
    ms-appx:///Assets/Fonts/NotoSansKR-Regular.otf#Noto Sans CJK KR,
    ms-appx:///Assets/Fonts/NotoSansArabic-Regular.ttf#Noto Sans Arabic
</FontFamily>
```

This font family is then applied globally via styles for `TextBlock`, `TextBox`, `Button`, `ComboBox`, `ComboBoxItem`, and `ContentControl`.

> **Note:** If CJK characters don't render correctly, verify the font family names (after `#`) match the internal font names. You can check fonts by opening them in a font viewer.

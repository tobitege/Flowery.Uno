# Noto Sans Fonts for Multilingual Support

This folder contains the Noto Sans fonts for the multilingual Flowery.Uno Gallery.
`GalleryResources.xaml` defines `NotoSansFamily` once per platform group with XAML conditional prefixes.

## Variable Fonts (Windows)

The Windows (WinUI) head uses these variable fonts. Each file contains weights 100 through 900, including Regular (400), SemiBold (600), and Bold (700), and WinUI selects the weight through `FontWeight`.

| File | Font family | Script coverage |
| --- | --- | --- |
| `NotoSans-Variable.ttf` | Noto Sans | Latin, Cyrillic, Greek |
| `NotoSansSC-Variable.ttf` | Noto Sans SC | Simplified Chinese |
| `NotoSansJP-Variable.ttf` | Noto Sans JP | Japanese |
| `NotoSansKR-Variable.ttf` | Noto Sans KR | Korean |
| `NotoSansArabic-Variable.ttf` | Noto Sans Arabic | Arabic |
| `NotoSansHebrew-Variable.ttf` | Noto Sans Hebrew | Hebrew |

These files are unmodified downloads from [Google Fonts revision 45e3d5269e4a17d823f6d84470bc084dc5ae46f6](https://github.com/google/fonts/tree/45e3d5269e4a17d823f6d84470bc084dc5ae46f6/ofl).
The source folders are `notosans`, `notosanssc`, `notosansjp`, `notosanskr`, `notosansarabic`, and `notosanshebrew`.
Upstream filenames use `[wght]` or `[wdth,wght]` instead of `-Variable`.
Each family has an accompanying `*-OFL.txt` file with its copyright notice and license.

## Static Fonts (Skia: Desktop, WebAssembly, iOS; Android)

The other heads use these static Regular fonts.
Skia renders a variable font at its default instance and cannot select the weight, and the CJK and Hebrew variable fonts default to Thin (100).

| File | Purpose | Size |
| --- | --- | --- |
| `NotoSans-Regular.ttf` | Latin, Cyrillic, Greek | ~560KB |
| `NotoSansSC-Regular.otf` | Simplified Chinese (Standalone) | ~16MB |
| `NotoSansJP-Regular.otf` | Japanese (Standalone) | ~16MB |
| `NotoSansKR-Regular.otf` | Korean (Standalone) | ~16MB |
| `NotoSansArabic-Regular.ttf` | Arabic script | ~240KB |

These files are unmodified upstream files:

- `NotoSans-Regular.ttf` and `NotoSansArabic-Regular.ttf` come from `hinted/ttf` in [notofonts/noto-fonts](https://github.com/notofonts/noto-fonts).
- The CJK files are `NotoSansCJKsc-Regular.otf`, `NotoSansCJKjp-Regular.otf`, and `NotoSansCJKkr-Regular.otf` from `Sans/OTF` in [notofonts/noto-cjk](https://github.com/notofonts/noto-cjk), renamed.

`StaticFonts-OFL.txt` contains their copyright notices and the license.

Each head ships only the set it uses: the Windows head excludes the static fonts, and `Flowery.Uno.GalleryAssets.props` excludes the variable fonts from the other heads.

## License

These fonts are from the [Google Noto Fonts](https://fonts.google.com/noto) project and are licensed under the **SIL Open Font License, Version 1.1 (OFL-1.1)**.
The repository's MIT license does not apply to these fonts.
The copyright notice and license must accompany redistributed font files, so keep each `*-OFL.txt` file in the same `Assets/Fonts` folder as the fonts it covers.

The full license text is available at <https://openfontlicense.org/open-font-license-official-text/>.

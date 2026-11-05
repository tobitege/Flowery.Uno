<!-- Supplementary documentation for DaisyMnemonicText -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyMnemonicText shows WinForms-style mnemonic labels: `&` marks the access character, which is underlined, and `&&` shows a literal `&`. An `&` followed by whitespace also stays literal. It is display only. Your app handles Alt+key itself, for example with `KeyboardAccelerator` or a `KeyDown` handler.

`DaisyButton`, `DaisyTabs`, `DaisyMenuItem` and `DaisyIconText` use the same parsing for their string labels, so you rarely need DaisyMnemonicText directly. Ported from Flowery.NET 2.3.1.

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Text` | `string` | `null` | Label including `&` markers. |
| `ShowMnemonic` | `bool` | `true` | Whether the access character is underlined. When `false`, the display text is shown without underline. |
| `MnemonicChar` | `char?` | `null` | Read-only. The access character parsed from `Text`, or `null`. |

Font properties and `Foreground` are inherited, as for any text inside a control.

## Parsing Rules

| Label | Display | Access character |
| --- | --- | --- |
| `&Save` | Save (S underlined) | `S` |
| `E&xit` | Exit (x underlined) | `x` |
| `Save && Exit` | Save & Exit | none |
| `Save & Exit` | Save & Exit | none (an `&` before whitespace stays literal) |
| `A&B&C` | ABC (B underlined) | `B` (only the first marker counts) |
| `Trailing&` | Trailing | none (a trailing `&` is dropped) |

## Where Mnemonics Apply

| Control | Label | Notes |
| --- | --- | --- |
| `DaisyButton` | String `Content` | `Content` keeps the markers. While `Content` is a string with `&` and no `ContentTemplate` is set, the button uses `StringContentTemplate`. Strings without `&` keep the default text presentation. Works with icon buttons too. |
| `DaisyTabs` | Tab `Header` | The header button's automation name is the display text. |
| `DaisyMenuItem` | String `Content` | The automation name is the display text. |
| `DaisyIconText` | `Text` | Always parsed; there is no `ShowMnemonic` switch. |

A plain `&` followed by a space, as in "Crop & rotate", shows as it is. Only an `&` directly before a character needs `&&` to stay literal, for example `Content="R&amp;&amp;D"` in XAML or `"R&&D"` in C#.

## Helpers

`FloweryMnemonicHelpers` (namespace `Flowery.Helpers`) exposes the parser and the presentation pieces:

| Member | Description |
| --- | --- |
| `Parse(text)` | Returns `FloweryMnemonicInfo` with `DisplayText`, `MnemonicIndex` and `MnemonicChar`. |
| `GetDisplayText(text)` / `GetMnemonicChar(text)` | Shortcuts for `Parse`. |
| `ApplyMnemonicText(textBlock, text, showMnemonic)` | Shows a label in an existing `TextBlock`: plain `Text` when nothing is underlined, otherwise runs with the access character underlined. |
| `StringContentTemplate` | `DataTemplate` (resource `DaisyMnemonicStringTemplate`) that renders a string as DaisyMnemonicText. Assign it as `ContentTemplate`. |

Flowery.NET's `EnsureStringContentTemplate` has no counterpart: Avalonia adds the template to a control's `DataTemplates`, and WinUI did not apply a `ContentTemplateSelector` to string content in our tests. The built-in controls set `ContentTemplate` directly instead.

## Quick Examples

```xml
<!-- Standalone label -->
<daisy:DaisyMnemonicText Text="&amp;File" />

<!-- Hide the underline until the user presses Alt -->
<daisy:DaisyMnemonicText x:Name="FileLabel" Text="&amp;File" ShowMnemonic="False" />

<!-- Built-in controls -->
<daisy:DaisyButton Content="&amp;Save" Variant="Primary" />
<daisy:DaisyButton Content="Save &amp;&amp; Exit" />
```

```csharp
// Use mnemonics on your own ContentControl with string content
myContentControl.ContentTemplate = FloweryMnemonicHelpers.StringContentTemplate;

// Read the access character to wire up your own Alt+key handling
char? key = FloweryMnemonicHelpers.GetMnemonicChar("&Save"); // 'S'
```

## Tips & Best Practices

- Keep one access character per label; only the first `&` marker is used.
- Labels such as "Terms & Conditions" need no escaping. Check existing labels for an `&` directly before a character (for example "R&D") and write it as `&&`, otherwise that character is underlined and the `&` disappears.
- WinUI's `TextBlock` is sealed, so unlike the Flowery.NET version DaisyMnemonicText hosts a `TextBlock` instead of deriving from it. Set text layout options such as wrapping on a surrounding element, or use `ApplyMnemonicText` on your own `TextBlock`.
- Uno's Skia text renderer (Desktop, iOS) does not draw `TextDecorations`. On Skia, `ApplyMnemonicText` draws the underline as a composition child visual of the `TextBlock`, placed from the measured text and the current font and foreground. The line is shown only when the access character is on the first line of left-to-right text.

---

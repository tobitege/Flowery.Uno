<!-- markdownlint-disable MD033 -->
<!-- markdownlint-disable MD041 -->
<div align="center">

![Flowery.Uno Banner](banner.png)

# 🌼 Flowery.Uno

**An [Uno Platform](https://platform.uno/) / WinUI port of [Flowery.NET](https://github.com/tobitege/Flowery.NET) — fully themed C# controls.**

[![License](https://img.shields.io/github/license/tobitege/Flowery.Uno?style=flat-square)](LICENSE)
[![Uno Platform](https://img.shields.io/badge/Uno%20Platform-6.x-blue?style=flat-square)](https://platform.uno/)
[![X](https://img.shields.io/badge/X-@tobitege-000000?style=flat-square&logo=x)](https://x.com/tobitege)

</div>

<div align="center">

🌐 **Localized in 12 languages** including:

🇯🇵 日本語にローカライズ済み &nbsp;•&nbsp; 🇰🇷 한국어로 현지화됨 &nbsp;•&nbsp; 🇨🇳 已本地化为简体中文

🇺🇦 Локалізовано українською &nbsp;•&nbsp; 🇸🇦 مترجم للعربية &nbsp;•&nbsp; 🇮🇱 מתורגם לעברית

</div>

Flowery.Uno provides native WinUI controls that mimic the utility-first, semantic class naming of DaisyUI, making it easy to build beautiful, themed UIs.

> [!NOTE]
> This is still in alpha. Features may be added/removed or need fixing. I'm just a *guy with an AI*! Took all of my spare time over 5+ weeks for just the initial alpha. This Flowery version has even more controls and features then the original.
I hope this will be useful and fun to work with! -- tobitege

## Relationship to Flowery.NET

- **Flowery.NET (Avalonia)** was the starting point and the reference implementation.
- **Flowery.Uno (Uno Platform / WinUI)** reuses the same DaisyUI-inspired design language:
  - semantic variants (`Primary`, `Secondary`, `Accent`, etc.)
  - DaisyUI theme names and runtime switching
  - similar control names (`DaisyButton`, `DaisyInput`, `DaisyThemeDropdown`, …)

## Features

- **DaisyUI-inspired Controls**: C# classes inheriting from WinUI primitives (e.g., `DaisyButton : Button`).
- **35 DaisyUI Themes**: All official DaisyUI themes included (Light, Dark, Cupcake, Dracula, Nord, Synthwave, and more).
- **Runtime Theme Switching**: Use `DaisyThemeDropdown` to switch themes at runtime.
- **Lightweight Styling**: Customize individual controls with control-specific resource keys. [📖 Reference](llms-static/StylingResources.md)
- **Localization Support**: Built-in i18n with **12 languages** (🇺🇸 🇩🇪 🇫🇷 🇪🇸 🇮🇹 🇨🇳 🇰🇷 🇯🇵 🇸🇦 🇹🇷 🇺🇦 🇮🇱), localizable theme names, and runtime language switching. [📖 Guide](LOCALIZATION.md) WinUI/Uno heads must use `{Binding [Key]}` with a `Localization` DataContext; `x:Bind` indexers are invalid on all heads. Desktop/Skia must force a `DataContext` refresh on culture change to update indexer bindings.
- **FlowKanban**: Full Kanban board control with swimlanes, WIP limits, drag & drop, undo/redo history, multi-board home, and JSON persistence + migration. [📖 Guide](llms-static/FlowKanban.md)
- **Variants**: Supports `Primary`, `Secondary`, `Accent`, `Ghost`, etc.
- **Gallery App**: Demo application showcasing all controls and features.

## FlowKanban

`FlowKanban` is a full Kanban board system built for Flowery.Uno. It ships as a single control but includes a full ecosystem of models, dialogs, and a Home screen for managing multiple boards.

### FlowKanban Highlights

- Swimlanes with lane-aware drag and drop.
- WIP limits with visual enforcement and warnings.
- Undo/redo history for all board operations.
- Multi-board Home screen (search, sort, rename, duplicate, export).
- JSON persistence with atomic saves + migration support.
- Command surface for common actions (add/remove/rename/zoom).
- Search and filtering for tasks and boards.
- Industrial sidebar layout with board-level actions.

### Architecture

- `FlowKanban`: Control shell + state + persistence.
- `FlowKanbanManager`: Programmatic API for manipulating boards.
- `FlowKanbanHome`: Multi-board Home screen control.
- `FlowTaskCard` / `FlowKanbanColumn`: Visual task + column building blocks.

### Usage

```xml
xmlns:kanban="using:Flowery.Uno.Kanban.Controls"

<kanban:FlowKanban x:Name="ProjectBoard" />
```

📖 **[FlowKanban Guide](llms-static/FlowKanban.md)** - Full feature list, architecture, and usage examples.

## Quick Start

1. Reference the `Flowery.Uno` project (or later: a NuGet package).

2. Ensure `Themes/Generic.xaml` is loaded in your app. The library uses `GenerateLibraryLayout=true`.

3. (Recommended) Seed design tokens early if you calculate layout before any Daisy control loads:

```csharp
using Flowery.Theming;

DaisyResourceLookup.EnsureTokens();
```

4. Use controls in your views:

```xml
xmlns:daisy="using:Flowery.Controls"

<daisy:DaisyButton Content="Primary Button" Variant="Primary" />
<daisy:DaisyInput PlaceholderText="Type here" Variant="Bordered" />
<daisy:DaisyThemeDropdown MinWidth="220" />
```

📖 **[Full Theming Guide](THEMING.md)** - Detailed setup, theme switching, persistence, and custom theme loading.

---

## Components

### Actions

- **Button** (`DaisyButton`): Buttons with variants (Primary, Secondary, Accent, Ghost, Link) and sizes (Large, Normal, Small, Tiny). Supports Outline and Active states.
- **Fab** (`DaisyFab`): Floating Action Button (Speed Dial) with support for multiple actions.
- **Modal** (`DaisyModal`): Dialog box with backdrop overlay.
- **Swap** (`DaisySwap`): Toggle control that swaps between two content states with optional Rotate/Flip effects.

### Data Display

- **Accordion** (`DaisyAccordion`): Group of collapse items that ensures only one item is expanded at a time.
- **Alert** (`DaisyAlert`): Feedback messages (Info, Success, Warning, Error).
- **Avatar** (`DaisyAvatar`): User profile image container (Circle, Square) with online/offline status indicators.
- **Avatar Group** (`DaisyAvatarGroup`): Groups multiple avatars with automatic overflow into "+N" placeholder.
- **Badge** (`DaisyBadge`): Small status indicators (Primary, Secondary, Outline, etc.).
- **Card** (`DaisyCard`): Content container with padding and shadow.
- **Carousel** (`DaisyCarousel`): Scrollable container for items.
- **Chat Bubble** (`DaisyChatBubble`): Message bubbles with header, footer, and alignment (Start/End).
- **Collapse** (`DaisyCollapse`): Accordion/Expander with animated arrow or plus/minus icon.
- **Countdown** (`DaisyCountdown`): Monospace digit display.
- **Diff** (`DaisyDiff`): Image comparison slider (Before/After).
- **Kbd** (`DaisyKbd`): Keyboard key visual style.
- **Stat** (`DaisyStat`): Statistics block with Title, Value, and Description.
- **Timeline** (`DaisyTimeline`): Vertical list of events with connecting lines.

### Data Input

- **Checkbox** (`DaisyCheckBox`): Checkbox with themed colors.
- **File Input** (`DaisyFileInput`): Styled button/label for file selection.
- **Input** (`DaisyInput`): Text input field with variants (Bordered, Ghost, Primary, etc.).
- **Radio** (`DaisyRadio`): Radio button with themed colors.
- **Range** (`DaisyRange`): Slider control.
- **Rating** (`DaisyRating`): Star rating control with interactivity and partial fill support.
- **Select** (`DaisySelect`): ComboBox with themed styles.
- **Textarea** (`DaisyTextArea`): Multiline text input.
- **Toggle** (`DaisyToggle`): Switch toggle control.

### Layout

- **Divider** (`DaisyDivider`): Separation line with optional text.
- **Drawer** (`DaisyDrawer`): Sidebar navigation with overlay.
- **Hero** (`DaisyHero`): Large banner component.
- **Join** (`DaisyJoin`): Container that groups children (buttons/inputs) by merging their borders.
- **Stack** (`DaisyStack`): Container that stacks children visually with offsets.

### Navigation

- **Breadcrumbs** (`DaisyBreadcrumbs`): Navigation path with separators.
- **Dock** (`DaisyDock`): Bottom navigation bar (macOS-style dock) with item selection events.
- **Menu** (`DaisyMenu`): Vertical or horizontal list of links/actions.
- **Navbar** (`DaisyNavbar`): Top navigation bar container.
- **Pagination** (`DaisyPagination`): Group of buttons for page navigation.
- **Steps** (`DaisySteps`): Progress tracker steps.
- **Tabs** (`DaisyTabs`): Tabbed navigation (Bordered, Lifted, Boxed).

### Feedback & Utils

- **Indicator** (`DaisyIndicator`): Utility to place a badge on the corner of another element.
- **Loading** (`DaisyLoading`): Animated loading indicators with multiple variants.
- **Mask** (`DaisyMask`): Applies shapes (Squircle, Heart, Hexagon, etc.) to content.
- **Progress** (`DaisyProgress`): Linear progress bar.
- **Radial Progress** (`DaisyRadialProgress`): Circular progress indicator.
- **Skeleton** (`DaisySkeleton`): Animated placeholder for loading states.
- **Toast** (`DaisyToast`): Container for stacking alerts (fixed positioning).

### Theme Controls

- **Size Dropdown** (`DaisySizeDropdown`): Global size selector with localized size names.
- **Theme Controller** (`DaisyThemeController`): Flexible toggle with multiple modes.
- **Theme Dropdown** (`DaisyThemeDropdown`): Dropdown to select from all 35+ themes.
- **Theme Manager** (`DaisyThemeManager`): Static class for programmatic theme control.

---

## ✨ Flowery.Uno Exclusives

> **Beyond DaisyUI** - The following features and controls are **not part of the original DaisyUI CSS specification**. They are unique to Flowery, built natively for Uno Platform.

### Utility Controls

- **Animated Number** (`DaisyAnimatedNumber`): Animated numeric display with slide transitions.
- **Button Group** (`DaisyButtonGroup`): Segmented button-group container for joined buttons.
- **Component Sidebar** (`FloweryComponentSidebar`): Pre-built documentation/admin sidebar with categories and search.
- **Contribution Graph** (`DaisyContributionGraph`): GitHub-style contribution heatmap.
- **Copy Button** (`DaisyCopyButton`): Copy-to-clipboard button with temporary success state.
- **Dropdown** (`DaisyDropdown`): Menu-style dropdown for action menus.
- **Expandable Card** (`DaisyExpandableCard`): Card that expands to reveal secondary content.
- **OTP Input** (`DaisyOtpInput`): Multi-slot verification-code input with auto-advance.
- **Tag Picker** (`DaisyTagPicker`): Multi-select chip/tag picker with add/remove icons.

### Color Picker Suite

A complete color picker suite rebuilt natively for WinUI with DaisyUI styling:

- **Color Wheel** (`DaisyColorWheel`): Circular HSL color wheel.
- **Color Grid** (`DaisyColorGrid`): Grid-based palette selector.
- **Color Slider** (`DaisyColorSlider`): Channel-specific sliders (R/G/B/A/H/S/L).
- **Color Editor** (`DaisyColorEditor`): Comprehensive RGB/HSL editor.
- **Color Picker Dialog** (`DaisyColorPickerDialog`): Full-featured modal dialog.
- **Screen Color Picker** (`DaisyScreenColorPicker`): Screen sampling tool (Windows/Desktop).

### Date Timeline

A horizontal scrollable date picker:

- **Date Timeline** (`DaisyDateTimeline`): Scrollable date picker with selectable date items.
- **Date Timeline Item** (`DaisyDateTimelineItem`): Individual date cell with selection states.

---

## Build & Run

Build scripts (PowerShell + Git Bash) exist for these heads:

- Desktop (Skia): `build_desktop.ps1` / `build_desktop.sh`
- Windows (WinUI): `build_windows.ps1` / `build_windows.sh`
- Browser (WASM): `build_browser.ps1` / `build_browser.sh`
- Android: `build_android.ps1` / `build_android.sh`
- Kiosk Browser: `build_kiosk_browser.ps1` / `build_kiosk_browser.sh`
- All heads: `build_all.ps1` / `build_all.sh`

Run scripts exist for these heads:

- `run-desktop.ps1`
- `run-windows.ps1`
- `run-browser.ps1`
- `run-android.ps1`

Common build parameters (apply to all build scripts):

- `-Configuration <Debug|Release>`
- `-NoRun`
- `-Rebuild`
- `-VerboseOutput`

Additional parameters (script-specific):

- `build_android.ps1`: `-AndroidSdkDirectory <path>`
- `build_all.ps1`: `-RestoreWorkloads`

Examples:

```powershell
pwsh ./scripts/build_desktop.ps1
pwsh ./scripts/build_windows.ps1 -NoRun -VerboseOutput
pwsh ./scripts/build_browser.ps1 -Configuration Release
```

```bash
./scripts/build_desktop.sh
./scripts/build_windows.sh -NoRun -VerboseOutput
```

## Technical Requirements

**To use the library:**

- .NET 8.0+
- Uno Platform 6.x

**To build from source:**

- .NET 9.0 SDK or later (recommended: .NET 10.0 SDK)
- Visual Studio 2022, JetBrains Rider, or VS Code
- Windows, macOS, or Linux

**Required .NET Workloads:**

Different platform heads require specific workloads. Install them as needed:

```powershell
# Browser (WebAssembly) - Required for Flowery.Uno.Gallery.Browser
dotnet workload install wasm-tools

# Android - Required for Flowery.Uno.Gallery.Android
dotnet workload install android

# Or restore all workloads defined in the solution
dotnet workload restore
```

> **Tip:** Run `dotnet workload list` to see currently installed workloads.

---

## Project Structure

| Project | Description |
| --- | --- |
| `Flowery.Uno` | Core library containing all controls |
| `Flowery.Uno.Win2D` | Optional Win2D extension library for advanced geometry/effects |
| `Flowery.Uno.Gallery` | Shared Gallery UI library |
| `Flowery.Uno.Gallery.Windows` | WinUI (Windows) Gallery head |
| `Flowery.Uno.Gallery.Desktop` | Skia desktop Gallery head (Windows/Linux/macOS) |
| `Flowery.Uno.Gallery.Browser` | WebAssembly Gallery head |
| `Flowery.Uno.Gallery.Android` | Android Gallery head |

---

## Design Tokens & Styling

| Documentation | Purpose |
| --- | --- |
| [`Flowery.Uno/DesignTokens.md`](Flowery.Uno/DesignTokens.md) | Token keys (spacing, sizing, colors) and override guidance |
| [`llms-static/StylingResources.md`](llms-static/StylingResources.md) | Control-specific styling resources for lightweight customization |
| [`THEMING.md`](THEMING.md) | Full theming guide, theme controls, and per-control styling patterns |

---

## License

MIT

## Support

If you find this library useful, consider supporting its development:

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-support-yellow?style=flat-square&logo=buy-me-a-coffee)](https://buymeacoffee.com/tobitege23) [![Ko-Fi](https://img.shields.io/badge/Ko--Fi-support-ff5e5b?style=flat-square&logo=ko-fi)](https://ko-fi.com/tobitege)

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and release notes.

## Credits & References

- [**Uno Platform**](https://platform.uno/) - The cross-platform WinUI implementation.
- [**DaisyUI**](https://daisyui.com/) - The original Tailwind CSS component library.
- [**Cyotek ColorPicker**](https://github.com/cyotek/Cyotek.Windows.Forms.ColorPicker) - Inspiration for color picker controls.
- [**Easy Date Timeline**](https://github.com/FadyFayezYounan/easy_date_timeline) - Inspiration for date timeline controls.
- [**HappyHues.co**](https://www.happyhues.co/) - Inspirations for several extra themes

> **Disclaimer:** This project is not affiliated with, endorsed by, or sponsored by any of the above.

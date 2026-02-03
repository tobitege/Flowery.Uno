<!-- Supplementary documentation for FloweryComponentSidebar -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

FloweryComponentSidebar is a high-performance, navigational sidebar designed for the Gallery and complex app layouts. It features collapsible categories, clickable items with selection persistence, live search filtering, and deep integration with the project's global sizing and localization systems.

**Key Features:**

- **Collapsible Categories:** Sections can be expanded/collapsed and current state is persisted.
- **Dynamic Sizing:** Automatically scales all visual metrics (icons, fonts, margins) via `FlowerySizeManager`.
- **Uniform Selector Widths:** Theme/Language/Size dropdowns use a consistent fixed width inside the sidebar to keep alignment stable across themes.
- **Theme/Language Integration:** Special items for quick switching of UI settings.
- **Search Filtering:** Integrated search bar that filters categories and items in real-time.
- **Platform Optimized:** Uses `DaisyExpander` to bypass typical Uno/XAML performance issues on Skia/WASM.

## Key Properties & Events

| Member | Description |
| --- | --- |
| `Categories` (`ObservableCollection<SidebarCategory>`) | The source data. Add categories containing list of items. |
| `SelectedItem` (`SidebarItem?`) | Highlighted item. Selected state uses `DaisyPrimaryBrush` variant. |
| `SidebarWidth` (double) | The width of the sidebar. Default ~254. |
| `AutoSizeSidebarWidth` (bool) | If true, sidebar width scales automatically with the global `DaisySize`. |
| `SearchText` (string) | Live filtering query. Syncs automatically with the internal search input. |
| `Size` (`DaisySize`) | The manual sizing override. Typically synced with the global scale. |
| `ItemSelected` (event) | Fired when an item is clicked. Includes the item and its parent category. |
| `FavoriteToggled` (event) | Fired when an item's favorite/star icon is toggled. |

## Data Models

The sidebar uses a hierarchy of model classes that support localizable display names via `FloweryLocalization`:

- **`SidebarCategory`**: Contains `Name` (key), `IconKey`, `IsExpanded`, and `Items`.
- **`SidebarItem`**: Basic clickable item. Includes `Id`, `Name` (key), `TabHeader`, `Badge`, and `IsFavorite`.
- **Special Items**:
  - `SidebarLanguageSelectorItem`: Renders a localized language dropdown.
  - `SidebarThemeSelectorItem`: Renders a themed color/theme dropdown.
  - `SidebarSizeSelectorItem`: Renders the DaisyUI global size selector.

### Localizable Strings

`SidebarCategory.DisplayName` and `SidebarItem.DisplayName` automatically resolve via `FloweryLocalization.GetString(Name)`. Setting `Name="Actions"` will display "Actions" in English or "Aktionen" in German if the translation exists.

## Usage Example

### XAML Configuration

```xml
<controls:FloweryComponentSidebar 
    x:Name="Sidebar"
    AutoSizeSidebarWidth="True"
    ItemSelected="OnSidebarItemSelected" />
```

### Populating Data (C#)

```csharp
var general = new SidebarCategory { Name = "General", IconKey = "DaisyIconHome" };
general.Items.Add(new SidebarItem { Id = "welcome", Name = "Welcome", TabHeader = "Home" });
general.Items.Add(new SidebarItem { Id = "settings", Name = "Settings", TabHeader = "Home", ShowFavoriteIcon = true });

// Add a special selector item
general.Items.Add(new SidebarSizeSelectorItem { Id = "size_picker", Name = "Global Size" });

Sidebar.Categories = new ObservableCollection<SidebarCategory> { general };
```

## Internal Sizing Metrics

The sidebar uses a sophisticated internal metrics system (`SidebarMetrics`) to ensure a balanced look across all 5 standard sizes (`ExtraSmall` to `ExtraLarge`).

- **Header Font Size:** Targets specific section header tokens (`DaisySize*SectionHeaderFontSize`).
- **Item Font Size:** Matches the primary font tokens used by other DaisyUI controls.
- **Margins & Padding:** Every size has a custom-tuned set of paddings, icon sizes (e.g., 24px in XL vs 12px in XS), and spacing.
- **Vertical Spacing:** Section headers include a 4px left margin for the icon and a 2px top buffer for the sub-items for a premium, spacious look.

## Selection States

Selected items are visually distinct, using the project's **Primary** color palette:

- **Background:** `{DynamicResource DaisyPrimaryBrush}`
- **Foreground:** `{DynamicResource DaisyPrimaryContentBrush}` (white/contrast text)
- **Icons:** Favorite star outline syncs to the content brush color.

## State Management

The sidebar automatically persists its state via `StateStorageProvider`:

1. **Selection:** Re-selects the last item on app restart.
2. **Expansion:** Remembers which categories were collapsed or expanded.
3. **Scroll Position:** Attempts to scroll the selected item into view on load if it's deep in a long list.

## Tips for AI Agents

- Always use `SidebarCategory` for the root and `SidebarItem` (or derivatives) for the children.
- If an item doesn't appear, check the `SearchText` property or verify `IsExpanded` on the parent.
- To programmatically trigger a selection effect, set `sidebar.SelectedItem = myItem`.
- `FloweryComponentSidebar` manually manages its internal visual tree; avoid adding children directly via XAML `Children` property.

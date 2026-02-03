# Unified Icon API for DaisyButton

The **Unified Icon API** provides a batteries-included approach for adding icons to `DaisyButton` controls. It eliminates the need for complex XAML markup by handling icon creation, scaling, and color inheritance automatically.

## Features

- **Auto-scaling** - Icon size adapts to button Size (XS→12px, S→14px, M→16px, L→18px, XL→20px)
- **Auto-coloring** - Icon inherits button Foreground color (including hover/pressed states)
- **Icon-only mode** - If Content is null, shows just the icon
- **Icon + Content mode** - Shows icon and content together with proper spacing
- **Safe rendering** - Uses `Path` instead of `PathIcon` to avoid Uno runtime issues
- **Viewbox wrapping** - For proper scaling of 24x24 coordinate system paths

## Properties

| Property | Type | Description |
| --- | --- | --- |
| `IconSymbol` | `Symbol?` | Windows Symbol enum (Add, Edit, Delete, Favorite, etc.) |
| `IconData` | `string?` | Raw SVG path data string (24x24 coordinate system) |
| `IconPlacement` | `IconPlacement` | Where the icon appears relative to content (Left, Right, Top, Bottom). Default: Left |

## Usage

### IconSymbol (Windows Symbols)

The simplest approach using built-in Windows Symbol icons:

```xml
<!-- Icon + Text -->
<daisy:DaisyButton IconSymbol="Add" Content="Add" Variant="Primary" />
<daisy:DaisyButton IconSymbol="Edit" Content="Edit" Variant="Info" />
<daisy:DaisyButton IconSymbol="Delete" Content="Delete" Variant="Error" />
<daisy:DaisyButton IconSymbol="Accept" Content="Save" Variant="Success" />

<!-- Icon-only buttons -->
<daisy:DaisyButton IconSymbol="Add" Shape="Circle" Variant="Primary" />
<daisy:DaisyButton IconSymbol="Favorite" Shape="Circle" Variant="Error" />
<daisy:DaisyButton IconSymbol="Setting" Shape="Square" Variant="Ghost" />
```

### IconData (Custom SVG Paths)

For custom icons, provide raw SVG path data. The path should be designed for a 24x24 coordinate system:

```xml
<!-- Heart icon -->
<daisy:DaisyButton 
    IconData="M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"
    Content="Heart" 
    Variant="Error" />

<!-- Star icon -->
<daisy:DaisyButton 
    IconData="M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z"
    Content="Star" 
    Variant="Warning" />

<!-- Home icon -->
<daisy:DaisyButton 
    IconData="M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z"
    Content="Home" 
    Variant="Ghost" />
```

### Icon Placement

Control where the icon appears relative to the text:

```xml
<daisy:DaisyButton IconSymbol="Add" Content="Left" IconPlacement="Left" />
<daisy:DaisyButton IconSymbol="Add" Content="Right" IconPlacement="Right" />
<daisy:DaisyButton IconSymbol="Add" Content="Top" IconPlacement="Top" />
<daisy:DaisyButton IconSymbol="Add" Content="Bottom" IconPlacement="Bottom" />
```

### Size Scaling

Icons automatically scale with button size:

```xml
<daisy:DaisyButton IconSymbol="Home" Content="XS" Size="ExtraSmall" />  <!-- 12px icon -->
<daisy:DaisyButton IconSymbol="Home" Content="S" Size="Small" />        <!-- 14px icon -->
<daisy:DaisyButton IconSymbol="Home" Content="M" Size="Medium" />       <!-- 16px icon -->
<daisy:DaisyButton IconSymbol="Home" Content="L" Size="Large" />        <!-- 18px icon -->
<daisy:DaisyButton IconSymbol="Home" Content="XL" Size="ExtraLarge" />  <!-- 20px icon -->
```

## Comparison with Other Approaches

### Before (StackPanel layout)

```xml
<daisy:DaisyButton Variant="Primary">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <SymbolIcon Symbol="Add" />
        <TextBlock Text="Add Item" VerticalAlignment="Center" />
    </StackPanel>
</daisy:DaisyButton>
```

### After (StackPanel layout)

```xml
<daisy:DaisyButton IconSymbol="Add" Content="Add Item" Variant="Primary" />
```

### Before (Custom icon with Viewbox scaling)

```xml
<daisy:DaisyButton Shape="Circle" Variant="Error">
    <Viewbox Width="16" Height="16">
        <Path Data="M12,21.35L10.55,20.03C5.4..." Fill="{ThemeResource DaisyErrorContentBrush}" />
    </Viewbox>
</daisy:DaisyButton>
```

### After (Custom icon with Viewbox scaling)

```xml
<daisy:DaisyButton IconData="M12,21.35L10.55,20.03C5.4..." Shape="Circle" Variant="Error" />
```

## Technical Details

### Priority Order

When multiple icon properties are set, they are processed in this order:

1. `DaisyControlExtensions.Icon` (attached property - legacy)
2. `IconSymbol`
3. `IconData`

Only the first non-null icon source is used.

### Icon Sizing by Button Size

| Button Size | Icon Size | Icon Spacing |
| --- | --- | --- |
| ExtraSmall | 12px | 4px |
| Small | 14px | 6px |
| Medium | 16px | 8px |
| Large | 18px | 8px |
| ExtraLarge | 20px | 10px |

### Color Inheritance

Icons automatically inherit the button's `Foreground` color via data binding. This means:

- Normal state: Uses the button's normal foreground
- Hover state: Updates with the button's hover foreground  
- Pressed state: Updates with the button's pressed foreground
- Disabled state: Properly dimmed with the button

### Path Data Format

The `IconData` property expects path data in the standard SVG path format, designed for a 24x24 coordinate system. The icon is rendered inside a `Viewbox` that scales it to the appropriate size.

You can find icon path data from:

- [Material Design Icons](https://materialdesignicons.com/)
- [Heroicons](https://heroicons.com/)
- [Feather Icons](https://feathericons.com/)
- The project's `DaisyIcons.xaml` resource dictionary

## Available System Symbols

Common `Symbol` enum values you can use with `IconSymbol`:

| Category | Symbols |
| --- | --- |
| Actions | Add, Delete, Edit, Save, Cancel, Accept, Refresh, Sync |
| Navigation | Back, Forward, Home, Up, Down, Previous, Next |
| Media | Play, Pause, Stop, Volume, Mute |
| Communication | Mail, Message, Send, Share |
| Status | Help, Important, Warning, Flag |
| UI | More, Setting, Find, Filter, Sort, Zoom, ZoomIn, ZoomOut |
| Objects | Document, Folder, Camera, Photo, Video, Download, Upload |

See the [Microsoft Symbol enum documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.symbol) for the complete list.

## Migration from Attached Properties

If you were using `DaisyControlExtensions.Icon`:

### Before

```xml
<daisy:DaisyButton Content="Save">
    <daisy:DaisyControlExtensions.Icon>
        <SymbolIcon Symbol="Save" />
    </daisy:DaisyControlExtensions.Icon>
</daisy:DaisyButton>
```

### After

```xml
<daisy:DaisyButton IconSymbol="Save" Content="Save" />
```

The attached property pattern is still supported for complex icon scenarios, but the Unified Icon API is the recommended approach for most use cases.

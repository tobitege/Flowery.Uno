# Icon SVG Generator

A standalone CLI tool to export DaisyIcons from `Flowery.Uno/Themes/DaisyIcons.xaml` to individual SVG files with a customizable fill color.

## Purpose

This tool reads the icon path data definitions from `DaisyIcons.xaml` and generates individual SVG files for each icon. This is useful for:

- Creating icon assets for web applications
- Generating icons for design tools (Figma, Sketch, etc.)
- Exporting icons for documentation
- Creating icon packages with custom brand colors

## Usage

```bash
cd Tools/IconGenerator
dotnet run <output-folder> <hex-color>
```

### Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `output-folder` | Directory where SVG files will be created | Yes |
| `hex-color` | Fill color in hex format | Yes |

### Supported Color Formats

- `#RGB` - Short hex (e.g., `#F00` for red)
- `#RRGGBB` - Standard hex (e.g., `#FF0000` for red)
- `#AARRGGBB` - Hex with alpha (e.g., `#80FF0000` for semi-transparent red)
- Without `#` prefix - Also supported (e.g., `FF0000`)

## Examples

### Export with black color
```bash
dotnet run ./icons #000000
```

### Export with white color
```bash
dotnet run ./icons #FFFFFF
```

### Export with custom brand color
```bash
dotnet run ./icons ./brand-icons #FF5733
```

### Export to absolute path
```bash
dotnet run "C:/Users/[User]/Desktop/icons" #3498DB
```

## Output

The tool generates SVG files with the following naming convention:

| Original Key | Output Filename |
|--------------|-----------------|
| `DaisyIconHamburger` | `hamburger.svg` |
| `DaisyIconChevronLeft` | `chevron_left.svg` |
| `ModifierIconShift` | `shift.svg` |

Each SVG file uses a standard 24x24 viewBox:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24">
  <path d="..." fill="#FF5733"/>
</svg>
```

## Project Structure

```
Tools/IconGenerator/
├── Directory.Build.props   # Override solution settings
├── IconGenerator.csproj    # Project file
├── Program.cs              # Entry point
└── README.md               # This file
```

## Related

- `Flowery.Uno/Tools/IconSvgExporter.cs` - The core exporter class
- `Flowery.Uno/Themes/DaisyIcons.xaml` - Source icon definitions
- `Tools/PatternGenerator` - Similar tool for pattern tiles

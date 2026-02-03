# Pattern SVG Exporter Tool

A command-line tool for generating SVG pattern tiles used by `DaisyPatternedCard`.

## Purpose

This tool generates seamlessly-tiling SVG pattern files from `FloweryPatternTileGenerator`. While the runtime uses programmatic geometry generation (Tiled mode), these SVG exports serve as:

1. **Reference assets** - Visual documentation of all 13 pattern designs
2. **Potential optimization** - Pre-baked SVGs could be faster on some platforms (currently experimental)
3. **External use** - Patterns can be used in other tools, documentation, or design workflows

## Supported Patterns

| Pattern | Description |
|---------|-------------|
| CarbonFiber | Multi-layer woven carbon fiber texture |
| Dots | Evenly-spaced circular dots |
| Grid | Horizontal and vertical grid lines |
| Stripes | Diagonal stripe pattern at 45° |
| Noise | Random scattered dots (seeded) |
| Honeycomb | Hexagonal honeycomb mesh |
| Circuit | Random circuit board traces with nodes |
| Twill | Diagonal weave pattern at -30° |
| DiamondPlate | Industrial diamond plate texture |
| Mesh | Fine rectangular mesh grid |
| Perforated | Staggered circular perforations with shadows |
| Bumps | Regular circular bump pattern |
| Scales | Overlapping half-circle scale arcs |

## Usage

```bash
# Generate to default output folder
dotnet run --project Tools/PatternGenerator

# Generate to specific folder
dotnet run --project Tools/PatternGenerator -- "C:/path/to/output"
```

## Output Structure

The tool generates SVGs in two color variants:

```
output/
├── black/          # For light-themed backgrounds
│   ├── carbon_fiber.svg
│   ├── dots.svg
│   └── ...
└── white/          # For dark-themed backgrounds
    ├── carbon_fiber.svg
    ├── dots.svg
    └── ...
```

## Tile Specifications

- **Size**: 120×120 pixels (matches `FloweryPatternTileGenerator.TileSize`)
- **Colors**: Solid black or white with varying opacity per pattern
- **Format**: SVG 1.1 with basic shapes (rect, circle, polygon, path)

## Integration Status

**Current mode: Tiled (runtime geometry)**

The `DaisyPatternedCard` control uses `FloweryPatternMode.Tiled` by default, which generates pattern geometry at runtime using `FloweryPatternTileGenerator`. This provides:

- Theme-aware coloring (uses `Foreground` brush)
- Consistent rendering across all platforms
- No asset loading overhead

The SVG assets in `Flowery.Uno/Assets/Patterns/` are **not included in the build** - they are kept for reference only.

## API Reference

The tool uses `PatternSvgExporter.ExportAllPatterns(outputDirectory)` which internally calls:

```csharp
FloweryPatternTileGenerator.GeneratePatternSvg(pattern, fillColor)
```

The `fillColor` parameter accepts any SVG-valid color string:
- `"white"`, `"black"` (named colors)
- `"#FF0000"` (hex colors)
- `"rgba(255,0,0,0.5)"` (CSS colors)

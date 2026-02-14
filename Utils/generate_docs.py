#!/usr/bin/env python3
"""
Flowery.Uno Documentation Generator

Generates markdown documentation from curated llms-static/ files.

Usage:
    python Utils/generate_docs.py

================================================================================
CODE REQUIREMENTS FOR PARSING
================================================================================

For the documentation generator to correctly extract metadata, code must follow
these conventions:

C# CONTROL FILES (Flowery.Uno/Controls/Daisy*.cs):
--------------------------------------------------
1. Class XML documentation must immediately precede the class definition:

   /// <summary>
   /// A Button control styled after DaisyUI's Button component.
   /// </summary>
   public class DaisyButton : Button

2. StyledProperty definitions (or DependencyProperty for Uno) must follow consistent patterns:

   public static readonly DependencyProperty NAMEProperty =

3. Property XML documentation must immediately precede the StyledProperty:

   /// <summary>
   /// Gets or sets the button variant (Primary, Secondary, etc.).
   /// </summary>
   public static readonly StyledProperty<DaisyButtonVariant> VariantProperty = ...

4. Enums must be defined at namespace level with public access:

   public enum DaisyButtonVariant
   {
       Default,
       Primary,
       Secondary,
       ...
   }

OUTPUT:
-------
    llms/llms.txt            - Master index for LLMs
    llms/controls/*.md       - Per-control documentation
    llms/categories/*.md     - Category overviews

SUPPLEMENTARY DOCUMENTATION:
----------------------------
To add rich descriptions, usage guides, or variant explanations that won't be
overwritten by the generator, create markdown files in llms-static/:

    llms-static/DaisyLoading.md   - Extra docs for DaisyLoading control
    llms-static/DaisyButton.md    - Extra docs for DaisyButton control

The supplementary content is inserted AFTER the header/description and BEFORE the
auto-generated Properties section. HTML comments (<!-- -->) are stripped.

Example supplementary file:
    ## Overview
    DaisyLoading provides 11 animation variants...

    ## Animation Variants
    | Variant | Description |
    |---------|-------------|
    | Spinner | Classic rotating arc... |

================================================================================
"""

import re
from dataclasses import dataclass, field
from pathlib import Path


# =============================================================================
# Configuration Constants
# =============================================================================

# Parser limits
SUMMARY_PROXIMITY_CHARS = 300      # Max chars between summary comment and class definition
SUMMARY_LOOKBACK_LINES = 5        # Lines to search backward for summary comments

# Description truncation lengths
MAX_DESCRIPTION_LENGTH = 80       # Property description in tables
MAX_LLMS_DESC_LENGTH = 50         # Description in llms.txt overview
MAX_PROPS_IN_OVERVIEW = 3         # Number of properties listed in overview


@dataclass
class EnumInfo:
    """Represents a C# enum definition."""
    name: str
    values: list[str]
    description: str = ""


@dataclass
class PropertyInfo:
    """Represents a StyledProperty definition."""
    name: str
    prop_type: str
    default: str
    description: str = ""


@dataclass
class ControlInfo:
    """Represents a Daisy control class."""
    name: str
    base_class: str
    description: str
    properties: list[PropertyInfo] = field(default_factory=list)
    enums: list[EnumInfo] = field(default_factory=list)


# =============================================================================
# C# Parser
# =============================================================================

class CSharpParser:
    """Parses C# control files to extract metadata."""

    def parse_file(self, filepath: Path) -> ControlInfo | None:
        """Parse a C# control file and extract metadata."""
        content = filepath.read_text(encoding='utf-8')

        # Get target class name from filename
        target_name = filepath.stem

        # Extract class info
        class_info = self._extract_class(content, target_name)
        if not class_info:
            return None

        name, base_class, description = class_info

        # Extract enums and properties
        enums = self._extract_enums(content)
        properties = self._extract_properties(content)

        return ControlInfo(
            name=name,
            base_class=base_class,
            description=description,
            properties=properties,
            enums=enums
        )

    def _extract_enums(self, content: str) -> list[EnumInfo]:
        """Extract all enum definitions from the file."""
        enums = []
        enum_pattern = re.compile(
            r'public\s+enum\s+(\w+)\s*\{([^}]+)\}',
            re.DOTALL
        )

        for match in enum_pattern.finditer(content):
            name = match.group(1)
            values_block = match.group(2)
            # Extract enum values (handle both simple and attributed enums)
            values = []
            for line in values_block.split('\n'):
                line = line.strip().rstrip(',')
                if line and not line.startswith('//') and not line.startswith('['):
                    # Handle "Value = 0" pattern
                    value = line.split('=')[0].strip()
                    if value and value[0].isupper():
                        values.append(value)
            if values:
                enums.append(EnumInfo(name=name, values=values))

        return enums

    def _extract_class(self, content: str, target_name: str) -> tuple[str, str, str] | None:
        """
        Extract class info for the target class name.
        Returns (class_name, base_class, description).
        """
        # Pattern for class declaration with optional generic constraints
        class_pattern = re.compile(
            rf'public\s+(?:partial\s+)?class\s+({target_name})\s*:\s*(\w+)',
            re.MULTILINE
        )

        match = class_pattern.search(content)
        if not match:
            return None

        class_name = match.group(1)
        base_class = match.group(2)

        # Find summary comment before class
        class_start = match.start()
        before_class = content[:class_start]

        # Look for XML summary comment
        summary_pattern = re.compile(
            r'///\s*<summary>\s*(.*?)\s*///\s*</summary>',
            re.DOTALL
        )

        # Search in the last N characters before class declaration
        search_start = max(0, len(before_class) - SUMMARY_PROXIMITY_CHARS)
        search_text = before_class[search_start:]

        description = ""
        for summary_match in summary_pattern.finditer(search_text):
            # Get the last summary before the class
            description = self._clean_summary(summary_match.group(1))

        return class_name, base_class, description

    def _extract_properties(self, content: str) -> list[PropertyInfo]:
        """Extract all StyledProperty definitions."""
        properties = []

        # Pattern for DependencyProperty.Register (Uno/WinUI style)
        prop_pattern = re.compile(
            r'public\s+static\s+readonly\s+DependencyProperty\s+(\w+)Property\s*=\s*'
            r'DependencyProperty\.Register\s*\([^)]+\)',
            re.DOTALL
        )

        for match in prop_pattern.finditer(content):
            prop_name = match.group(1)
            block = match.group(0)

            # Get property type from the register call
            type_match = re.search(r'typeof\(([^)]+)\)', block)
            prop_type = type_match.group(1) if type_match else "object"

            # Get default value
            default = self._extract_default(block, prop_type)

            # Look for summary comment before property
            prop_start = match.start()
            before_prop = content[max(0, prop_start - 500):prop_start]

            description = ""
            summary_pattern = re.compile(
                r'///\s*<summary>\s*(.*?)\s*///\s*</summary>',
                re.DOTALL
            )

            # Find the last summary in the search area
            for summary_match in summary_pattern.finditer(before_prop):
                description = self._clean_summary(summary_match.group(1))

            properties.append(PropertyInfo(
                name=prop_name,
                prop_type=prop_type,
                default=default,
                description=description
            ))

        return properties

    def _extract_default(self, block: str, prop_type: str) -> str:
        """Extract default value from property registration block."""
        # Look for PropertyMetadata with default value
        default_patterns = [
            r'new\s+PropertyMetadata\s*\(\s*([^,)]+)',  # PropertyMetadata(default, ...)
            r'defaultValue:\s*([^,)]+)',  # Named parameter
        ]

        for pattern in default_patterns:
            match = re.search(pattern, block)
            if match:
                return self._clean_default(match.group(1).strip(), prop_type)

        return "-"

    def _clean_summary(self, text: str) -> str:
        """Clean XML documentation text."""
        # Remove /// prefixes and extra whitespace
        lines = []
        for line in text.split('\n'):
            line = re.sub(r'^///\s*', '', line.strip())
            if line:
                lines.append(line)
        return ' '.join(lines)

    def _clean_default(self, default: str, prop_type: str) -> str:
        """Clean and simplify default value representation."""
        if not default:
            return "null" if "?" in prop_type else "-"
        default = default.strip().rstrip(',')
        # Simplify common patterns
        if 'new Thickness(' in default:
            match = re.search(r'Thickness\((\d+)\)', default)
            if match:
                return f"Thickness({match.group(1)})"
            return "Thickness"
        if 'Color.FromArgb' in default:
            return "Color(semitransparent)"
        if 'Colors.' in default:
            return default.replace('Colors.', '')
        # Enum defaults
        if '.' in default:
            return default.split('.')[-1]
        return default


# =============================================================================
# Markdown Generator
# =============================================================================

class MarkdownGenerator:
    """Generates markdown documentation files."""

    def __init__(self, extras_dir: Path | None = None):
        """Initialize with optional supplementary docs directory."""
        self.extras_dir = extras_dir

    def _load_extra(self, control_name: str) -> str:
        """Load supplementary documentation for a control if it exists."""
        if not self.extras_dir:
            return ""
        extra_file = self.extras_dir / f"{control_name}.md"
        if extra_file.exists():
            content = extra_file.read_text(encoding='utf-8')
            # Remove HTML comments ONLY outside code blocks (metadata comments)
            content = self._strip_html_comments_outside_code(content)
            # Demote "# Overview" to "## Overview" since we add "# ControlName" header
            content = re.sub(r'^# Overview\b', '## Overview', content, flags=re.MULTILINE)
            return content.strip()
        return ""

    def _strip_html_comments_outside_code(self, content: str) -> str:
        """
        Remove HTML comments (<!-- ... -->) but preserve them inside code blocks.
        Code blocks are delimited by ``` markers.
        """
        result = []
        in_code_block = False
        lines = content.split('\n')

        i = 0
        while i < len(lines):
            line = lines[i]

            # Check for code block delimiter
            if line.strip().startswith('```'):
                in_code_block = not in_code_block
                result.append(line)
                i += 1
                continue

            if in_code_block:
                # Inside code block - preserve everything including comments
                result.append(line)
            else:
                # Outside code block - strip HTML comments
                # Handle single-line comments
                cleaned = re.sub(r'<!--.*?-->', '', line)
                # Only add non-empty lines (or preserve intentional blank lines)
                if cleaned.strip() or not line.strip():
                    result.append(cleaned)

            i += 1

        return '\n'.join(result)

    def _load_images(self, control_name: str) -> list[str]:
        """
        Find images for a control in llms-static/images/.
        Returns list of relative image paths (e.g., ['images/DaisyButton.png']).
        Handles multiple naming patterns:
        - DaisyMockup.png (exact match)
        - DaisyMockup_a.png, _b.png (chunked with letter suffix)
        - Mockup(Description).png (short name with parenthesized description)
        - DaisyGlass(Mode).png (full name with parenthesized description)
        """
        if not self.extras_dir:
            return []

        images_dir = self.extras_dir / "images"
        if not images_dir.exists():
            return []

        found_images = []

        # Check for single image (exact match)
        single_image = images_dir / f"{control_name}.png"
        if single_image.exists():
            found_images.append(f"images/{control_name}.png")

        # Check for chunked images (_a, _b, _c, etc.)
        for suffix in 'abcdefghij':
            chunk_image = images_dir / f"{control_name}_{suffix}.png"
            if chunk_image.exists():
                found_images.append(f"images/{control_name}_{suffix}.png")

        # Check for descriptive suffix images: ControlName(Description).png
        # e.g., DaisyGlass(BitmapCaptureMode).png or Mockup(Window).png
        short_name = control_name.replace('Daisy', '')  # "Mockup" from "DaisyMockup"
        for img_file in sorted(images_dir.glob("*.png")):
            fname = img_file.name
            # Match patterns like "Mockup(something).png" or "DaisyMockup(something).png"
            if (fname.startswith(f"{short_name}(") or fname.startswith(f"{control_name}(")) and fname.endswith(").png"):
                rel_path = f"images/{fname}"
                if rel_path not in found_images:
                    found_images.append(rel_path)

        return found_images

    def generate_control_doc(self, control: ControlInfo) -> str:
        """Generate markdown documentation for a control."""
        lines = []

        # Header
        lines.append(f"# {control.name}")
        lines.append("")

        # Description
        if control.description:
            lines.append(control.description)
        else:
            simple_name = control.name.replace('Daisy', '')
            lines.append(f"A {simple_name} control styled after DaisyUI.")
        lines.append("")

        # Base class
        lines.append(f"**Inherits from:** `{control.base_class}`")
        lines.append("")

        # Insert images if found in llms-static/images/
        images = self._load_images(control.name)
        if images:
            if len(images) == 1:
                # Single image
                lines.append(f"![{control.name}]({images[0]})")
            else:
                # Multiple chunks
                for i, img_path in enumerate(images):
                    part_num = i + 1
                    lines.append(f"![{control.name} - Part {part_num}]({img_path})")
            lines.append("")

        # Load and insert supplementary documentation from llms-static/
        extra_content = self._load_extra(control.name)
        if extra_content:
            lines.append(extra_content)
            lines.append("")

        return '\n'.join(lines)

    def generate_category_doc(self, category: str, controls: list[ControlInfo]) -> str:
        """Generate markdown documentation for a category."""
        lines = []

        lines.append(f"# {category}")
        lines.append("")
        lines.append(f"This category contains {len(controls)} controls:")
        lines.append("")

        for control in controls:
            desc = control.description if control.description else f"A {control.name.replace('Daisy', '')} control."
            # Don't truncate - show full description in category pages
            lines.append(f"- **[{control.name}](../controls/{control.name}.html)**: {desc}")

        lines.append("")
        lines.append("See individual control documentation for detailed usage.")
        lines.append("")

        return '\n'.join(lines)

    def generate_master_index(self, controls: list[ControlInfo]) -> str:
        """Generate the master llms.txt index file."""
        lines = []

        lines.append("# Flowery.Uno Component Library")
        lines.append("")
        lines.append("Flowery.Uno is an Uno Platform / WinUI component library inspired by DaisyUI.")
        lines.append("It provides styled controls for building modern cross-platform applications.")
        lines.append("")
        lines.append("## Documentation Structure")
        lines.append("")
        lines.append("- `docs/llms.txt` - This file (overview and quick reference)")
        lines.append("- `docs/controls/*.md` - Per-control documentation with properties, enums, and examples")
        lines.append("")
        lines.append("## Quick Start")
        lines.append("")
        lines.append("Add the namespace to your XAML:")
        lines.append("```xml")
        lines.append('xmlns:daisy="using:Flowery.Controls"')
        lines.append("```")
        lines.append("")
        lines.append("## Controls Overview")
        lines.append("")
        lines.append("| Control | Description | Key Properties |")
        lines.append("|---------|-------------|----------------|")

        for control in sorted(controls, key=lambda c: c.name):
            # Skip non-Daisy classes
            if not control.name.startswith('Daisy'):
                continue
            desc = control.description
            if not desc:
                desc = f"{control.name.replace('Daisy', '')} control"
            if len(desc) > MAX_LLMS_DESC_LENGTH:
                desc = desc[:MAX_LLMS_DESC_LENGTH - 3] + "..."
            props = ", ".join(p.name for p in control.properties[:MAX_PROPS_IN_OVERVIEW])
            if len(control.properties) > MAX_PROPS_IN_OVERVIEW:
                props += ", ..."
            lines.append(f"| [{control.name}](controls/{control.name}.html) | {desc} | {props} |")

        lines.append("")

        # Common patterns
        lines.append("## Common Patterns")
        lines.append("")
        lines.append("### Variants")
        lines.append("Most controls support a `Variant` property:")
        lines.append("- `Primary`, `Secondary`, `Accent` - Brand colors")
        lines.append("- `Info`, `Success`, `Warning`, `Error` - Status colors")
        lines.append("- `Neutral`, `Ghost`, `Link` - Subtle styles (on some controls)")
        lines.append("")
        lines.append("```xml")
        lines.append('<controls:DaisyButton Variant="Primary" Content="Primary"/>')
        lines.append('<controls:DaisyAlert Variant="Success">Operation completed!</controls:DaisyAlert>')
        lines.append("```")
        lines.append("")
        lines.append("### Sizes")
        lines.append("Controls support a `Size` property with values:")
        lines.append("`ExtraSmall`, `Small`, `Medium` (default), `Large`, `ExtraLarge`")
        lines.append("")
        lines.append("```xml")
        lines.append('<controls:DaisyButton Size="Large" Content="Large Button"/>')
        lines.append('<controls:DaisyInput Size="Small" Watermark="Small input"/>')
        lines.append("```")
        lines.append("")
        lines.append("### Theming")
        lines.append("Use `DaisyThemeManager` to switch themes programmatically:")
        lines.append("```csharp")
        lines.append('DaisyThemeManager.Instance.CurrentTheme = "dracula";')
        lines.append("```")
        lines.append("")
        lines.append("Or use theme controls:")
        lines.append("```xml")
        lines.append('<controls:DaisyThemeDropdown/>')
        lines.append('<controls:DaisyThemeSwap LightTheme="light" DarkTheme="dark"/>')
        lines.append("```")
        lines.append("")
        lines.append("Available themes: light, dark, cupcake, bumblebee, emerald, corporate,")
        lines.append("synthwave, retro, cyberpunk, valentine, halloween, garden, forest,")
        lines.append("aqua, lofi, pastel, fantasy, wireframe, black, luxury, dracula, cmyk,")
        lines.append("autumn, business, acid, lemonade, night, coffee, winter, dim, nord, sunset")
        lines.append("")

        return '\n'.join(lines)


# =============================================================================
# Main Generator
# =============================================================================

class DocumentationGenerator:
    """Main documentation generator that orchestrates parsing and output."""

    def __init__(self, root_dir: Path):
        self.root_dir = root_dir
        self.controls_dir = root_dir / "Flowery.Uno" / "Controls"
        self.output_dir = root_dir / "llms"
        self.supplementary_dir = root_dir / "llms-static"

        self.csharp_parser = CSharpParser()
        self.md_generator = MarkdownGenerator(extras_dir=self.supplementary_dir)

    def generate(self):
        """Generate all documentation."""
        print("Flowery.Uno Documentation Generator")
        print("=" * 40)
        print("Mode: CURATED (llms-static/)")

        # Create output directories
        self.output_dir.mkdir(exist_ok=True)
        (self.output_dir / "controls").mkdir(exist_ok=True)
        (self.output_dir / "categories").mkdir(exist_ok=True)

        # Parse all controls
        print("\n[1/3] Parsing C# control files...")
        controls = self._parse_all_controls()
        print(f"      Found {len(controls)} controls")

        # Generate per-control docs
        print("\n[2/3] Generating control documentation...")
        extras_count = 0
        for control in controls:
            doc = self.md_generator.generate_control_doc(control)
            output_path = self.output_dir / "controls" / f"{control.name}.md"
            output_path.write_text(doc, encoding='utf-8')
            # Check if supplementary docs were merged
            if self.supplementary_dir.exists():
                extra_file = self.supplementary_dir / f"{control.name}.md"
                if extra_file.exists():
                    extras_count += 1
        print(f"      Generated {len(controls)} control docs")
        print(f"      Used {extras_count} curated docs from llms-static/")

        # Generate master index
        print("\n[3/3] Generating index documentation...")
        master_doc = self.md_generator.generate_master_index(controls)
        (self.output_dir / "llms.txt").write_text(master_doc, encoding='utf-8')

        print("\n" + "=" * 40)
        print("Documentation generated successfully!")
        print(f"Output directory: {self.output_dir}")

    def _parse_all_controls(self) -> list[ControlInfo]:
        """Parse all C# control files, including those in subfolders."""
        controls = []
        # Search recursively in Controls folder and all subfolders
        for filepath in self.controls_dir.glob("**/Daisy*.cs"):
            if "Converter" in filepath.name:
                continue
            control = self.csharp_parser.parse_file(filepath)
            if control:
                controls.append(control)
        return controls


def main():
    """Main entry point."""
    print("Running Flowery.Uno Documentation Generator...")

    script_dir = Path(__file__).parent
    root_dir = script_dir.parent

    generator = DocumentationGenerator(root_dir)
    generator.generate()


if __name__ == "__main__":
    main()

# Flowery.Uno Documentation System

The pipeline is **curated-first**. Hand-written markdown in `llms-static/` is the primary and only source. No auto-parsing of source code is needed.

## At a Glance

```txt
+--------------------+     +--------------------+
|   llms-static/     | --> |   Static HTML Site |
|   (curated docs)   |     |   (docs/ folder)   |
+--------------------+     +--------------------+
          ^                          ^
          |                          |
   Hand-written             generate_site.py
```

---

## Folder Structure

```txt
Flowery.Uno/
  Flowery.Uno/Controls/           # C# control definitions
  Flowery.Uno.Gallery/Examples/   # XAML examples (for the gallery app)
  llms-static/                    # Curated docs (tracked)
    DaisyButton.md
    DaisyIconText.md
    images/                       # Control screenshots
    ...
  llms/                           # Generated docs (gitignored)
    llms.txt
    controls/
  docs/                           # Generated static site (gitignored)
    index.html
    controls/
    style.css
  Utils/                          # Tooling
    generate_docs.py
    generate_site.py
    DOCS.md (this file)
```

---

## Workflows

### Default (curated-only)

1) Write or edit `llms-static/<Control>.md`.
2) `python Utils/generate_site.py`
3) Open `docs/index.html`.

That's it! The site is built directly from curated markdown files.

---

## Scripts

### generate_site.py (main entry point)

- **Default mode:** reads curated docs directly from `llms-static/` and emits `docs/` plus `docs/llms.txt`.
- **Flag:** `--use-generated` switches the input to `llms/` (produced by `generate_docs.py`).
- **Outputs:** `docs/index.html`, `docs/controls/*.html`, `docs/style.css`, `docs/llms.txt`.

Run:

```bash
python Utils/generate_site.py
```

### generate_docs.py (optional)

- Parses C# control files to extract metadata (class info, properties, enums).
- Merges curated content from `llms-static/` into generated docs in `llms/`.
- Used to rebuild `llms/controls/*.md` files with structured metadata.

Run:

```bash
python Utils/generate_docs.py
```

---

## Quick Start

Build the site (recommended):

```bash
python Utils/generate_site.py
start docs/index.html  # or open/xdg-open on macOS/Linux
```

---

## Curated Documentation (llms-static/)

- One file per control: `llms-static/DaisyButton.md`, `llms-static/DaisyIconText.md`, etc.
- Files are tracked in git and are the **primary source** for the site and `docs/llms.txt`.
- HTML comments (`<!-- -->`) are stripped automatically.
- Images go in `llms-static/images/` (e.g., `DaisyButton.png`, `DaisyCard_a.png`, `DaisyCard_b.png`).

Suggested structure:

````markdown
<!-- Optional metadata comment -->

# Overview

Brief description of what this control does and when to use it.

## Variants

| Variant | Description |
| --- | --- |
| **Primary** | Main action styling |
| **Ghost** | Subtle/low-emphasis styling |

## Sizes

| Size | Description |
| --- | --- |
| Small | Compact UI |
| Medium | Default |

## Quick Examples

```xml
<daisy:DaisyButton Variant="Primary" Content="Primary" />
<daisy:DaisyButton Variant="Ghost" Content="Ghost" />
```

## Tips & Best Practices

- Use **Primary** for the main call-to-action.
- Prefer **Ghost** for non-blocking actions.
````

Best practices:

- Keep it concise; use tables for variants/sizes/colors when helpful.
- Add 2-3 quick XAML snippets for common use.
- Include a "Tips & Best Practices" section for guidance.

---

## Adding a New Control

1) Create `llms-static/DaisyNewControl.md` with overview, key variants, and examples.  
2) Run `python Utils/generate_site.py` to rebuild the site.  
3) Done! The control will appear in the sidebar and documentation.

---

## Troubleshooting

- **Missing control in site:** Ensure a matching `llms-static/<Control>.md` file exists.
- **Images not showing:** Place images in `llms-static/images/` with names matching the control (e.g., `DaisyButton.png`).
- **Broken links/404:** Re-run `generate_site.py` to rebuild the sidebar/index.
- **Sidebar order wrong:** Controls are sorted alphabetically by name.

---

## Related Files

| File | Purpose |
| --- | --- |
| `Utils/generate_site.py` | Builds the static site from curated docs |
| `Utils/generate_docs.py` | Generates `llms/` from C# + curated content |
| `llms-static/README.md` | How to write curated docs |
| `.github/workflows/generate-docs.yml` | CI entrypoint |
| `docs/llms.txt` | Machine-readable docs for AI assistants |

---

Last updated: December 2025

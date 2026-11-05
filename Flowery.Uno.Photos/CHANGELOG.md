<!-- markdownlint-disable MD022 MD024 -->
# Changelog

All notable changes to Flowery Photos are documented in this file. The app is versioned with the repository; the
library changes are in the [main changelog](../CHANGELOG.md).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

First version of the app, ported from Flowery.WinUI Photos 1.9.0 (`31798a9`) to the Uno Platform Skia Desktop head
(Windows, macOS, Linux). Issue #60; delivered in #108, #133 and #134.

### Added

- **App and library** (#108): the `Flowery.Uno.Photos` project with its 12 UI languages, runtime themes and global
  size; folders as library with a file system watcher, justified photo grid with grouping, timeline, search, filter and
  sort, multi-select with batch actions, context menu, import and drag and drop. Metadata comes from the file headers;
  thumbnails are cached in memory and on disk. The library reads files through `IPhotoSource`.
- **Shell and pages** (#108): home, favorites, albums, folders, settings with live preview, custom title bar on Windows.
- **Viewer** (#108): zoom and pan, navigator, rulers, grid, status bar, auto-hiding toolbar, slideshow, immersive full
  screen, file info panel with EXIF and GPS.
- **Platform layer** (#108): trash, show in file manager, set as wallpaper, open with, file clipboard, system accent and
  display information for Windows, macOS and Linux. An action without a working mechanism on the current OS is hidden.
- **Editing** (#134): crop, rotate, flip, straighten, adjustments, 14 filters, markup with pen and highlighter, color
  tools (palettes in JASC-PAL and GPL, harmonies, eyedropper), before/after compare, undo/redo, revert, save as copy
  or replace, copy to the clipboard. Rendering uses SkiaSharp with SkSL runtime effects fitted to the Win2D results.
- A JPEG keeps its EXIF data (camera, date, GPS) when it is saved; WebP photos can be replaced in place (#134).
- Unit tests for the library, metadata, thumbnails, platform layer, localization and the editing pipeline.

### Changed compared with Flowery.WinUI Photos

- Images are decoded and encoded with SkiaSharp. JPEG, PNG, WebP, GIF, BMP and ICO open; TIFF, HEIC, AVIF and JXR show
  a placeholder tile.
- *Replace* is disabled for formats that cannot be written back (GIF, BMP and others); an edit is then saved as a JPEG
  copy.
- Highlights, shadows and vignette were refitted to the Win2D output (#134).

### Fixed

- Follow-up fixes after the first review: full screen insets, language refresh, hero layout (#108, #133) and review
  findings on the editing code (#134).

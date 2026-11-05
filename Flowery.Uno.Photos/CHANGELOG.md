<!-- markdownlint-disable MD024 -->
# Changelog

All notable changes to Flowery Photos (`Flowery.Uno.Photos`) are documented in this file. The app shares its version
with the repository.

## [1.6.0] - 2026-11-05

- **New:** first version of Flowery Photos for the Uno Platform Skia desktop head on Windows, macOS and Linux, ported
  from Flowery.WinUI.Photos (Flowery.WinUI 1.8.0) (#108, #133 and #134): home page, library with folder
  watching and a disk thumbnail cache, gallery, favorites, albums and folders, settings, the viewer with slideshow and
  immersive full screen, and the editor (crop, adjust, filters, markup, colors, compare, save). See the
  [README](README.md) for the features and the differences from the WinUI app.
- Rendering uses SkiaSharp on the CPU instead of Win2D. Exposure, contrast, temperature, tint, highlights, shadows,
  clarity, sharpen and vignette were fitted to the output of Win2D 1.3.2.
- System features go through one platform layer per OS: trash, reveal in the file manager, desktop background, "open
  with", copying files to the clipboard and the system accent color. Items the system does not offer are hidden.
- A JPEG saved from a JPEG keeps its EXIF data (camera, date, GPS), with orientation 1, the new pixel size and without
  the old embedded thumbnail. WebP photos are saved as WebP.
- Closing the window with unsaved edits asks first, as closing the viewer does.
- The window reopens with the size it had, at most as large as the screen it opens on.
- `scripts/test_photos_start.ps1` starts the app with an isolated state folder and fails on `crash.log`; CI runs it on
  Windows.

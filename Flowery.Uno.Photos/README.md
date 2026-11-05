# Flowery Photos

A simple demo app for viewing photos and making basic edits, built with [Flowery.Uno](../README.md) Daisy controls,
SkiaSharp and the Uno Platform Skia desktop head. It runs on Windows, macOS and Linux.

Flowery Photos shows what a complete, everyday desktop app looks like with Flowery.Uno: Daisy controls throughout,
runtime themes, global sizing and 12 UI languages. It is a showcase, not a replacement for a full photo manager or
image editor. It is a port of Flowery.WinUI.Photos (Flowery.WinUI 1.8.0); the differences are listed below.

## Features

### Home

- **Start page:** greeting with a library summary and quick actions (slideshow, add folder, import), counts for
  photos, favorites, albums and folders, and rows of recently taken photos, favorites, folders and albums.
- **Welcome page:** an empty library shows the first steps (add a folder, import photos or use the sample photos) and
  what the app can do.

### Library and gallery

- **Folders as library:** starts with your `Pictures` folder. Add more folders, include or exclude subfolders, and
  remove folders without deleting files. Changes on disk are picked up automatically.
- **Fast scanning:** dimensions, capture date, orientation and camera model are read directly from file headers
  (JPEG/EXIF, PNG, GIF, BMP, WebP) and cached.
- **Justified photo grid:** virtualized rows that keep each photo's aspect ratio, grouped by day, month or year.
  Optional square thumbnails and file names. Thumbnails are cached on disk.
- **Date chip and year timeline:** the header shows the date of the photos in view; the timeline on the right jumps to
  any year or month.
- **Search, filter, sort:** search by file name, folder, camera, year or month name; filter by file type and
  orientation; sort by date taken, date modified, name or file size.
- **Thumbnail size:** slider, `Ctrl` + mouse wheel or `Ctrl` + `+`/`-`.
- **Multi-select:** check boxes, `Ctrl`/`Shift` click, select a whole date group or select all. Then favorite, add to an
  album, copy or delete in one step.
- **Context menu:** open, edit, favorite, add to album, copy, open with, show in the file manager, set as desktop
  background, delete (to the trash, with confirmation). Items the system does not offer are hidden (see
  [Platforms](#platforms)).
- **Import:** add a folder, import files (copied into `Pictures/Flowery Photos`), or drop photos and folders onto the
  window.

### Favorites, albums and folders

- **Favorites:** heart any photo (`F` in the viewer) and find it on the Favorites page.
- **Albums:** collect photos from anywhere without moving files; create, rename and delete albums and start a
  slideshow from an album.
- **Folders:** card overview of every folder that contains photos, with cover image and count.

### Viewer

- Zoom with the mouse wheel (or switch the wheel to next/previous) in round steps (10 % below 100 %, then 25 %),
  double-click for 100 %, drag to pan, fit to window, zoom presets and slider.
- Previous/next with arrow keys or the overlay buttons; opening a single file from outside the library browses its
  folder.
- File info panel: folder, size, dimensions, dates, camera, lens, exposure and GPS location (opens the map).
- Slideshow with cross-fade and configurable interval.
- Immersive full screen (`F11`): toolbar and status bar float above the photo and fade out until the pointer reaches
  the top or bottom edge.
- **View options:** rulers with pixel coordinates, gridlines, status bar on/off, auto-hide toolbar, and a navigator
  thumbnail for panning zoomed photos.

### Editing

All edits are non-destructive until you save. They are rendered with SkiaSharp on the CPU; the preview follows the
sliders while the full-size render runs in the background.

- **Crop:** rotate, flip, straighten, free or fixed aspect ratios (original, 1:1, 4:3, 3:2, 16:9 and portrait
  variants), crop handles with rule-of-thirds grid. The chosen ratio turns with the photo (16:9 becomes 9:16).
- **Adjust:** one-click auto enhance, then brightness, exposure, contrast, highlights, shadows, saturation, warmth,
  tint, sharpness, clarity, vignette and fade. The effects are matched to the Win2D effects of the WinUI app.
- **Filters:** 14 looks with live previews of the current photo and adjustable intensity.
- **Markup:** pen and highlighter with adjustable size; strokes are saved into the image.
- **Colors (markup):** primary/secondary color (left/right mouse button, swap with `X`), presets, eyedropper, nine
  persistent custom colors, recently used colors. The *Advanced* section offers color adjustments, color harmonies, a
  palette extracted from the photo, palette import/export (Paint Shop Pro JASC-PAL, GIMP `.gpl`) and a full color
  editor.
- **Compare:** split view with a draggable before/after divider.
- **Undo/redo** for every step, **Revert** to the original (at the top of the edit panel).
- **Save:** save a copy (default), replace the original after confirmation, or copy the edited image to the clipboard.
  A JPEG saved from a JPEG keeps its EXIF data (camera, date, GPS). Unsaved edits are never lost silently: changing
  the photo, closing the viewer and closing the window ask first.

### App

- Daisy themes, global size (scales controls and text) and 12 languages (English, German, French, Spanish, Italian,
  Chinese (Simplified), Japanese, Korean, Turkish, Arabic, Hebrew, Ukrainian), switchable at runtime.
- Navigation rail, toasts and dialogs. On Windows the app bar replaces the system title bar; on macOS and Linux it is
  a row below it.
- The hover, focus and selection frames use the system accent color where the system has one, otherwise the theme's
  primary color.
- Settings open as a modal dialog above the current view (also above the editor). Changes preview live; **OK** keeps
  them, **Cancel** (or `Esc`) restores the previous state and returns to where you were.
- Window size and all preferences are remembered.

## Platforms

| Feature | Windows | macOS | Linux |
| --- | --- | --- | --- |
| Move to trash | Recycle Bin | Trash | with `gio` |
| Show in file manager | File Explorer | Finder | with `dbus-send` or `xdg-open` |
| Set as desktop background | yes | yes | GNOME (`gsettings`) |
| Open with | system dialog | list of apps | list of apps, with `gio` |
| Copy files to the clipboard | yes | no (the edited image can be copied) | yes |
| System accent color | yes | yes | GNOME (`gsettings`) |

## Formats

The app opens the formats SkiaSharp decodes: JPEG, PNG, WebP, GIF, BMP and ICO. HEIC, AVIF, TIFF and JPEG XR show a
placeholder tile, on every system. Animated GIFs show their first frame.

JPEG, PNG and WebP are saved in their format; other formats are saved as JPEG copies, and "Replace" is disabled for
them.

## Keyboard shortcuts

| Keys | Action |
| --- | --- |
| `Ctrl` + `F` | Search |
| `Ctrl` + `,` | Settings |
| `Ctrl` + `A` | Select all photos |
| `Ctrl` + mouse wheel | Change thumbnail size |
| `←` / `→` | Previous / next photo |
| `Ctrl` + `E` | Open the editor |
| `Ctrl` + `Z` / `Ctrl` + `Y` | Undo / redo |
| `Ctrl` + `S` | Save a copy |
| `Ctrl` + `Shift` + `S` | Replace the original |
| `+` / `-`, `0`, `1` | Zoom in / out, fit, 100 % |
| `F` | Toggle favorite |
| `R` / `Shift` + `R` | Rotate right / left |
| `X` | Swap primary/secondary color (markup) |
| `Del` | Delete photo |
| `F5` | Slideshow |
| `F11` | Full screen |
| `Esc` | Close / go back |

## Screenshots

[`Screenies`](Screenies) contains feature screenshots (1400x1000, Windows). `Screenies/Capture-Screenshots.ps1`
recreates them: it starts the built app with an isolated state folder and the bundled sample photos, copies only theme,
size and view options from your settings, and drives the UI through UI Automation, mouse and keyboard input. Pass
`-Monitor` with a device name to use another monitor than the primary one.

```powershell
pwsh -File Flowery.Uno.Photos/Screenies/Capture-Screenshots.ps1
```

## Build and run

Requirements are the same as for the repository (.NET 10 SDK; on Linux the packages of the Skia desktop head).

```bash
dotnet build Flowery.Uno.Photos/Flowery.Uno.Photos.csproj -f net10.0-desktop -c Debug
dotnet run --project Flowery.Uno.Photos/Flowery.Uno.Photos.csproj -f net10.0-desktop
```

Pass an image path as the first argument to open it directly in the viewer.

Settings, the metadata cache and the thumbnail cache are stored in the local application data folder
(`%LOCALAPPDATA%\Flowery.Uno.Photos` on Windows, `~/.local/share/Flowery.Uno.Photos` on Linux and
`~/Library/Application Support/Flowery.Uno.Photos` on macOS). Set the environment variable `FLOWERY_STATE_ROOT` to an
absolute path to use a different location, for example for tests with sample data only. Unhandled errors are written
to `crash.log` in that folder.

`scripts/test_photos_start.ps1` starts the built app on Windows with an isolated state folder and checks that it opens
its window, runs and closes without writing `crash.log`; CI runs it.

## Differences from Flowery.WinUI.Photos

- Runs on Windows, macOS and Linux instead of Windows only; system features are offered where the system has them
  (see [Platforms](#platforms)).
- Rendering with SkiaSharp instead of Win2D. The effects were fitted to the output of Win2D 1.3.2, so an edit looks
  close to the same edit in the WinUI app, but not identical.
- A JPEG saved from a JPEG keeps its EXIF data, and WebP photos are saved as WebP. The WinUI app drops EXIF and saves
  WebP edits as JPEG copies.
- TIFF, HEIC, AVIF and JPEG XR cannot be opened. The WinUI app opens them through Windows Imaging.

## Limitations

- Saving re-encodes the image. PNG and WebP edits are saved without EXIF data.
- GIF and BMP edits are saved as JPEG copies.
- There is no repair/retouch tool, no RAW development and no cloud sync.

<#
.SYNOPSIS
    Captures the Flowery Photos feature screenshots into this folder (Windows).
.DESCRIPTION
    Starts the built app (Skia desktop head) with an isolated state folder, drives it through UI Automation and mouse
    input, and saves one PNG per feature (the window, default 1400 x 1000 physical pixels).

    Theme, size and view options are copied from your own Flowery Photos settings. Your folders, favorites, albums and
    metadata cache are never read: the library uses the bundled sample photos only, with a few favorites and two albums
    seeded for the screenshots. The app is driven by the names of its English UI, so it runs in English unless
    -Language says otherwise (other languages need names that match).

    The script places the window on the monitor given by -Monitor; mouse and keyboard input go there. Do not touch mouse
    or keyboard while the script runs. Only the app instance the script started is closed, and no file is deleted: every
    run uses a new state folder, and the PNGs are overwritten.
.PARAMETER Configuration
    Build configuration of the app to run (Debug or Release). Build it first.
.PARAMETER Width
    Window width in physical pixels.
.PARAMETER Height
    Window height in physical pixels.
.PARAMETER Monitor
    Device name of the monitor to use, as Windows lists it in System.Windows.Forms.Screen.DeviceName (the \\.\ prefix may
    be left out). Default: the primary monitor. Use a monitor with the same scaling as the primary one: after the window
    moved to a monitor with another scaling, Uno maps pointer input with the old scaling, so clicks and strokes land off.
.PARAMETER SettingsFrom
    State folder to copy theme, size and view options from.
.PARAMETER Language
    UI language of the screenshots (default en).
.EXAMPLE
    pwsh -File .\Capture-Screenshots.ps1
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [int]$Width = 1400,
    [int]$Height = 1000,
    [string]$Monitor,
    [string]$SettingsFrom = (Join-Path $env:LOCALAPPDATA 'Flowery.Uno.Photos'),
    [string]$Language = 'en'
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $PSVersionTable.PSVersion.Major -lt 7) {
    throw 'Run this script in PowerShell 7 on Windows.'
}

$outputDir = $PSScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$appDir = Join-Path $repoRoot "bin\$Configuration\Flowery.Uno.Photos\net10.0-desktop"
$exe = Join-Path $appDir 'Flowery.Uno.Photos.exe'
$samples = Join-Path $appDir 'Assets\Samples'
if (-not (Test-Path -LiteralPath $exe)) { throw "App not found, build it first: $exe" }
if (-not (Test-Path -LiteralPath $samples)) { throw "Sample photos not found: $samples" }

$stateDir = Join-Path ([IO.Path]::GetTempPath()) ('FloweryUnoPhotosScreenshots-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ShotNative
{
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, int data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out RECT rect, int size);
}
'@
# Per-monitor DPI aware: window bounds, UI Automation rectangles and mouse positions are all physical pixels.
[void][ShotNative]::SetProcessDpiAwarenessContext([IntPtr]::new(-4))

$screen = if ($Monitor) { [System.Windows.Forms.Screen]::AllScreens | Where-Object { $_.DeviceName -eq $Monitor -or $_.DeviceName -eq ('\\.\' + $Monitor) } } else { [System.Windows.Forms.Screen]::PrimaryScreen }
if (-not $screen) { throw "Monitor not found: $Monitor" }
$work = $screen.WorkingArea
if ($work.Width -lt $Width -or $work.Height -lt $Height) { throw ('{0} x {1} does not fit on {2} ({3} x {4}).' -f $Width, $Height, $screen.DeviceName, $work.Width, $work.Height) }

$script:process = $null
$script:hwnd = [IntPtr]::Zero
$A = [System.Windows.Automation.AutomationElement]
$TreeScope = [System.Windows.Automation.TreeScope]

#region Helpers

function Stop-App {
    if ($script:process -and -not $script:process.HasExited) {
        $null = $script:process.CloseMainWindow()
        if (-not $script:process.WaitForExit(10000)) { $script:process.Kill() }
    }
    $script:process = $null
    Start-Sleep -Milliseconds 800
}

function Get-VisibleBounds {
    $rect = New-Object ShotNative+RECT
    [void][ShotNative]::DwmGetWindowAttribute($script:hwnd, 9, [ref]$rect, 16) # DWMWA_EXTENDED_FRAME_BOUNDS
    return $rect
}

function Start-App {
    $env:FLOWERY_STATE_ROOT = $stateDir
    $script:process = Start-Process -FilePath $exe -PassThru
    for ($i = 0; $i -lt 80 -and $script:process.MainWindowHandle -eq [IntPtr]::Zero; $i++) {
        Start-Sleep -Milliseconds 250
        $script:process.Refresh()
    }
    if ($script:process.MainWindowHandle -eq [IntPtr]::Zero) { throw 'The app window did not appear.' }
    $script:hwnd = $script:process.MainWindowHandle
    Start-Sleep -Seconds 2

    # Placed from outside once it shows (the app opens where the system puts it). On a monitor with another scaling the
    # first call changes the scaling and Windows resizes the window, so the second one sets the size there.
    foreach ($i in 1..2) {
        [void][ShotNative]::SetWindowPos($script:hwnd, [IntPtr]::Zero, $work.X + 20, $work.Y + 20, $Width, $Height, 0x0014) # NOZORDER | NOACTIVATE
        Start-Sleep -Milliseconds 1500
    }
    Hide-Cursor
}

function Get-Window { $A::FromHandle($script:hwnd) }

# A real move out of the window (to the corner of the monitor), so the app sees the pointer leave and closes tooltips.
function Hide-Cursor { Move-Screen ($screen.Bounds.Right - 2) ($screen.Bounds.Bottom - 2) }

# A real move to a screen pixel (mouse_event with VIRTUALDESK, so monitors left of or above the primary work too).
# SetCursorPos alone produces no pointer move, so a drag would not draw.
function Move-Screen([int]$X, [int]$Y) {
    $vx = [ShotNative]::GetSystemMetrics(76); $vy = [ShotNative]::GetSystemMetrics(77)
    $vw = [ShotNative]::GetSystemMetrics(78); $vh = [ShotNative]::GetSystemMetrics(79)
    $ax = [int][Math]::Round(($X - $vx) * 65535.0 / ($vw - 1))
    $ay = [int][Math]::Round(($Y - $vy) * 65535.0 / ($vh - 1))
    [ShotNative]::mouse_event(0xC001, $ax, $ay, 0, [UIntPtr]::Zero) # MOVE | VIRTUALDESK | ABSOLUTE
}

# Window-relative position as a fraction of the window (0..1), so the steps work at any window size.
function Move-Relative([double]$FX, [double]$FY) {
    $b = Get-VisibleBounds
    Move-Screen ([int]($b.Left + ($b.Right - $b.Left) * $FX)) ([int]($b.Top + ($b.Bottom - $b.Top) * $FY))
}

function Send-Click {
    [ShotNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [ShotNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}

function Invoke-ClickRelative([double]$FX, [double]$FY, [double]$Wait = 1.0) {
    Move-Relative $FX $FY
    Start-Sleep -Milliseconds 150
    Send-Click
    Hide-Cursor
    Start-Sleep -Milliseconds ([int]($Wait * 1000))
}

function Invoke-Drag([double[][]]$Points) {
    Move-Relative $Points[0][0] $Points[0][1]
    Start-Sleep -Milliseconds 150
    [ShotNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    for ($i = 1; $i -lt $Points.Count; $i++) {
        for ($s = 1; $s -le 12; $s++) {
            Move-Relative ($Points[$i - 1][0] + ($Points[$i][0] - $Points[$i - 1][0]) * $s / 12) ($Points[$i - 1][1] + ($Points[$i][1] - $Points[$i - 1][1]) * $s / 12)
            Start-Sleep -Milliseconds 12
        }
    }
    [ShotNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Hide-Cursor
    Start-Sleep -Milliseconds 500
}

function Invoke-Wheel([double]$FX, [double]$FY, [int]$Notches) {
    Move-Relative $FX $FY
    for ($i = 0; $i -lt [Math]::Abs($Notches); $i++) {
        [ShotNative]::mouse_event(0x0800, 0, 0, [Math]::Sign($Notches) * 120, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 250
    }
    Hide-Cursor
}

# The first element with this accessible name that is on screen.
function Find-Name([string]$Name) {
    $elements = (Get-Window).FindAll($TreeScope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($A::NameProperty, $Name)))
    foreach ($element in $elements) {
        $r = $element.Current.BoundingRectangle
        if (-not $r.IsEmpty -and $r.Width -gt 0 -and $r.Height -gt 0) { return $element }
    }
    throw "No element named '$Name' on screen."
}

# Invokes, expands or toggles the element with this accessible name (or its nearest actionable parent); clicks its
# center when it offers none of these (photo tiles, filter tiles).
function Invoke-Name([string]$Name, [double]$Wait = 1.0) {
    $element = Find-Name $Name
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $current = $element
    for ($i = 0; $i -lt 4 -and $current; $i++) {
        $pattern = $null
        if ($current.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) { $pattern.Invoke(); Start-Sleep -Milliseconds ([int]($Wait * 1000)); return }
        if ($current.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$pattern)) { $pattern.Expand(); Start-Sleep -Milliseconds ([int]($Wait * 1000)); return }
        if ($current.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) { $pattern.Toggle(); Start-Sleep -Milliseconds ([int]($Wait * 1000)); return }
        $current = $walker.GetParent($current)
    }
    Invoke-ClickName $Name $Wait
}

# Clicks the center of the element with this accessible name.
function Invoke-ClickName([string]$Name, [double]$Wait = 1.0) {
    $r = (Find-Name $Name).Current.BoundingRectangle
    # Uno reports UI Automation rectangles in the scaling the window was created with: after the window moved to a monitor
    # with another scaling they can be off by that factor, also the window's own rectangle. They are mapped through the
    # caption's close button instead, which is 46 x 32 and sits in the top right corner of the frame DWM draws.
    $close = (Get-Window).FindFirst($TreeScope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($A::AutomationIdProperty, '_CloseButton'))).Current.BoundingRectangle
    $b = Get-VisibleBounds
    $scale = 46 * [ShotNative]::GetDpiForWindow($script:hwnd) / 96 / $close.Width
    Move-Screen ([int]($b.Right + ($r.X + $r.Width / 2 - $close.Right) * $scale)) ([int]($b.Top + ($r.Y + $r.Height / 2 - $close.Top) * $scale))
    Start-Sleep -Milliseconds 150
    Send-Click
    Hide-Cursor
    Start-Sleep -Milliseconds ([int]($Wait * 1000))
}

# Focuses the element with this accessible name through UI Automation and presses a key, for example Enter to open a
# photo tile or Space to select it. Needs no screen position.
function Invoke-KeyOnName([string]$Name, [byte]$Key, [double]$Wait = 1.0) {
    [void][ShotNative]::SetForegroundWindow($script:hwnd)
    (Find-Name $Name).SetFocus()
    Start-Sleep -Milliseconds 200
    [ShotNative]::keybd_event($Key, 0, 0, [UIntPtr]::Zero)
    [ShotNative]::keybd_event($Key, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds ([int]($Wait * 1000))
}

# Toggles the toggle switches with these indexes (in UI Automation order).
function Invoke-Toggles([int[]]$Indexes) {
    $toggles = (Get-Window).FindAll($TreeScope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($A::IsTogglePatternAvailableProperty, $true)))
    foreach ($index in $Indexes) {
        $toggles[$index].GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
        Start-Sleep -Milliseconds 400
    }
}

# Automation invokes move keyboard focus, which shows tooltips; a click on the empty title bar removes them.
function Clear-Tooltip { Invoke-ClickRelative 0.2 0.012 0.6 }

function Save-Shot([string]$Name) {
    [void][ShotNative]::SetForegroundWindow($script:hwnd)
    Start-Sleep -Milliseconds 400
    $b = Get-VisibleBounds
    $w = $b.Right - $b.Left
    $h = $b.Bottom - $b.Top
    $bitmap = New-Object System.Drawing.Bitmap $w, $h
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($b.Left, $b.Top, 0, 0, $bitmap.Size)
    $path = Join-Path $outputDir "$Name.png"
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Host ("{0}.png ({1}x{2})" -f $Name, $w, $h)
}

function Write-State([string]$Key, [string[]]$Lines) {
    Set-Content -LiteralPath (Join-Path $stateDir "$Key.state") -Value $Lines -Encoding utf8
}

#endregion

#region State

New-Item -ItemType Directory -Path $stateDir | Out-Null
foreach ($key in 'theme', 'size', 'photos.options') {
    $source = Join-Path $SettingsFrom "$key.state"
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $stateDir }
}
Write-State 'language' @($Language)
# An intentionally empty library (marker line), so the first screenshot shows the welcome page.
Write-State 'photos.folders' @(' ')

#endregion

try {
    # 01: home page without folders
    Start-App
    Save-Shot '01-home-no-folders'
    Stop-App

    # Sample library with favorites and albums
    $s = $samples
    Write-State 'photos.folders' @($s)
    Write-State 'photos.favorites' @("$s\wallpaper_yellow.jpg", "$s\tile_green.jpg", "$s\wallpaper_blue.jpg")
    Write-State 'photos.albums' @(
        (@('Favorite patterns', "$s\tile_blue.jpg", "$s\tile_green.jpg", "$s\tile_red.jpg", "$s\tile_yellow.jpg") -join "`t"),
        (@('Wallpapers', "$s\wallpaper_blue.jpg", "$s\wallpaper_green.jpg", "$s\wallpaper_red.jpg", "$s\wallpaper_yellow.jpg") -join "`t"))
    Start-App
    Start-Sleep -Seconds 2
    Save-Shot '02-home'

    # Gallery
    Invoke-Name 'Gallery' 2
    Save-Shot '03-gallery'

    Invoke-Name 'View options' 1.2
    Save-Shot '04-gallery-view-options'
    Invoke-ClickRelative 0.7 0.85 0.8

    # Space on a focused tile toggles its selection (the tiles offer no automation pattern for it).
    Invoke-KeyOnName 'wallpaper_blue.jpg' 0x20 0.3
    Invoke-KeyOnName 'tile_red.jpg' 0x20 0.3
    Invoke-KeyOnName 'tile_yellow.jpg' 0x20 0.8
    Save-Shot '05-gallery-multi-select'
    Invoke-Name 'Clear selection' 0.5

    # Collections
    Invoke-Name 'Favorites' 2
    Save-Shot '06-favorites'
    Invoke-Name 'Albums' 2
    Save-Shot '07-albums'
    Invoke-Name 'Folders' 2
    Save-Shot '08-folders'

    # Viewer
    Invoke-Name 'Gallery' 2
    Invoke-KeyOnName 'wallpaper_blue.jpg' 0x0D 2.5   # Enter opens the photo
    Save-Shot '09-viewer'

    Invoke-Name 'File info' 1.2
    Clear-Tooltip
    Save-Shot '10-file-info'

    # Editor
    Invoke-Name 'Edit' 1.2
    Clear-Tooltip
    Invoke-Name 'Crop' 1.2
    Invoke-Name '16:9' 1.2
    Save-Shot '11-edit-crop'

    Invoke-Name 'Revert' 1
    Invoke-Name 'Adjust' 1.2
    Invoke-Name 'Auto enhance' 1.5
    Save-Shot '12-edit-adjust'

    Invoke-Name 'Revert' 1
    Invoke-Name 'Filters' 1.5
    Invoke-ClickName 'Cool' 1.5
    Save-Shot '13-edit-filters'

    Invoke-Name 'Compare before and after' 1.5
    Clear-Tooltip
    Save-Shot '14-edit-before-after'
    Invoke-Name 'Compare before and after' 1

    Invoke-Name 'Revert' 1
    Invoke-Name 'Markup' 1.5
    # A loop with the pen, then a line with the highlighter, both on the photo left of the edit panel.
    Invoke-Drag @(@(0.33, 0.48), @(0.36, 0.39), @(0.43, 0.35), @(0.49, 0.40), @(0.52, 0.52), @(0.48, 0.64), @(0.42, 0.68), @(0.35, 0.63), @(0.33, 0.54), @(0.34, 0.46))
    Invoke-Name 'Highlighter' 0.6
    Invoke-Drag @(@(0.22, 0.81), @(0.31, 0.80), @(0.42, 0.82), @(0.52, 0.81), @(0.61, 0.81))
    Invoke-Name 'Advanced' 1
    Start-Sleep -Seconds 2             # navigation arrows fade out
    Save-Shot '15-edit-markup'

    # View options: rulers, grid and navigator
    Invoke-Name 'Revert' 1.2
    Invoke-Name 'Edit' 1.2
    Clear-Tooltip
    Invoke-Name 'View' 1.2
    Invoke-Toggles @(0, 1, 3)
    Invoke-ClickRelative 0.42 0.52 0.8 # closes the flyout
    Invoke-Wheel 0.42 0.52 3
    Start-Sleep -Seconds 3
    Save-Shot '16-viewer-rulers-grid-navigator'

    # Settings dialog
    Invoke-Name 'Settings' 1.5
    Save-Shot '17-settings'
    Invoke-Name 'Cancel' 0.8
}
finally {
    Stop-App
    Remove-Item Env:FLOWERY_STATE_ROOT -ErrorAction SilentlyContinue
}

Write-Host "Screenshots saved to $outputDir"

<#
.SYNOPSIS
    Builds and starts the Flowery.Uno.Gallery for Windows.

.DESCRIPTION
    Builds the Gallery for Windows and, if the resulting .exe exists, starts it in the background.

.PARAMETER Configuration
    Build configuration to use (default: Debug).
.PARAMETER Rebuild
    If specified, performs a clean rebuild before running.
.PARAMETER NoWait
    If specified, does not wait for the app process to exit.
.PARAMETER Cmd
    If specified, runs the app in an extra cmd shell that remains open on exit (useful for logs).

.EXAMPLE
    pwsh ./scripts/run-windows.ps1
    pwsh ./scripts/run-windows.ps1 -Configuration Release -Rebuild
    pwsh ./scripts/run-windows.ps1 -NoWait
    pwsh ./scripts/run-windows.ps1 -Cmd
#>
param(
    [string]$Configuration = "Debug",
    [switch]$Rebuild,
    [switch]$NoWait,
    [switch]$Cmd
)

$ErrorActionPreference = "Stop"

# --- LOAD CORE ---
. "$PSScriptRoot\build_core.ps1"

# --- SETUP ---
$repoRoot = Get-RepoRoot
$galleryProject = Join-Path $repoRoot "Flowery.Uno.Gallery\Flowery.Uno.Gallery.csproj"
$targetFramework = "net9.0-windows10.0.19041"

if (-not (Test-Path $galleryProject)) {
    Write-Host "ERROR: Gallery project not found at $galleryProject" -ForegroundColor Red
    exit 1
}

Clear-Host
Write-Host "Running Flowery.Uno Windows ($Configuration)..." -ForegroundColor Cyan
Write-Host ""

# --- BUILD (MSBuild) ---
Initialize-BuildEnvironment -HeadName "Windows" -LogFileName "windows_run_build.log" -Configuration $Configuration -Cli
if (-not (Invoke-ProjectBuild -Title "Flowery.Uno.Gallery (Windows)" -Project $galleryProject -Configuration $Configuration -Rebuild:$Rebuild -Framework $targetFramework)) {
    Write-BuildFailure "Flowery.Uno.Gallery (Windows)"
    exit 1
}

$outputDir = Join-Path $repoRoot "bin/$Configuration/Flowery.Uno.Gallery/$targetFramework"
$exePath = Get-ChildItem -Path $outputDir -Recurse -Filter "Flowery.Uno.Gallery.exe" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $exePath) {
    Write-Host "Windows executable not found under $outputDir (skipping run)" -ForegroundColor Yellow
    exit 0
}

$wait = $false  # Don't wait for app closure by default

if ($Cmd) {
    Write-Host "Starting Windows app in extra console shell (keeps open on exit)..." -ForegroundColor Cyan
    $cmdArgs = "/k `"$($exePath.FullName)`""
    if ($wait) {
        Start-Process -FilePath "cmd.exe" -ArgumentList $cmdArgs -WorkingDirectory $exePath.DirectoryName -Wait | Out-Null
    } else {
        Start-Process -FilePath "cmd.exe" -ArgumentList $cmdArgs -WorkingDirectory $exePath.DirectoryName | Out-Null
    }
} else {
    Write-Host "Starting Windows app..." -ForegroundColor Cyan
    if ($wait) {
        Start-Process -FilePath $exePath.FullName -WorkingDirectory $exePath.DirectoryName -Wait | Out-Null
    } else {
        Start-Process -FilePath $exePath.FullName -WorkingDirectory $exePath.DirectoryName | Out-Null
    }
}

Write-Host "Started: $($exePath.FullName)" -ForegroundColor Green

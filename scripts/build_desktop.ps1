<#
.SYNOPSIS
    Builds Flowery.Uno Desktop projects with clean summary output.

.DESCRIPTION
    Builds Flowery.Uno (library) and Flowery.Uno.Gallery for Desktop (Skia) using the single-project structure.
    Verbose build output is captured to desktop_build.log, showing only a clean summary.
    By default, runs the Gallery app after a successful build.

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)

.PARAMETER NoRun
    If specified, skips running the app after build.

.PARAMETER Rebuild
    If specified, performs a clean rebuild (recommended after code changes).

.PARAMETER VerboseOutput
    If specified, prints output to console instead of log file.

.EXAMPLE
    pwsh ./scripts/build_desktop.ps1
    pwsh ./scripts/build_desktop.ps1 -Rebuild
    pwsh ./scripts/build_desktop.ps1 -Configuration Release
#>
param(
    [string]$Configuration = "Debug",
    [switch]$NoRun,
    [switch]$Rebuild,
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"

# --- LOAD CORE ---
. "$PSScriptRoot\build_core.ps1"

# --- SETUP ---
$repoRoot = Get-RepoRoot
$floweryProject = Join-Path $repoRoot "Flowery.Uno\Flowery.Uno.csproj"
$galleryProject = Join-Path $repoRoot "Flowery.Uno.Gallery\Flowery.Uno.Gallery.csproj"
$targetFramework = "net9.0-desktop"

# --- INITIALIZE ---
Initialize-BuildEnvironment -HeadName "Desktop" -LogFileName "desktop_build.log" -Configuration $Configuration -VerboseOutput:$VerboseOutput

# --- BUILD ---

# 1. Build Library
if (-not (Invoke-ProjectBuild -Title "Flowery.Uno" -Project $floweryProject -Configuration $Configuration -Rebuild:$Rebuild)) {
    Write-BuildFailure "Flowery.Uno"
    exit 1
}

# 2. Build Gallery Desktop
if (-not (Invoke-ProjectBuild -Title "Flowery.Uno.Gallery (Desktop)" -Project $galleryProject -Configuration $Configuration -Rebuild:$Rebuild -Framework $targetFramework)) {
    Write-BuildFailure "Flowery.Uno.Gallery (Desktop)"
    exit 1
}

# --- SUMMARY ---
Write-BuildSummary

# --- RUN ---
if (-not $NoRun) {
    Start-GalleryApp -ExePattern "Flowery.Uno.Gallery.exe" -Configuration $Configuration -Framework $targetFramework
}

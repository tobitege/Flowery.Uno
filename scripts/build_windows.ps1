<#
.SYNOPSIS
    Builds the Flowery.Uno.Gallery for Windows.

.DESCRIPTION
    Builds the Windows target using the single-project structure. Output is written to windows_build.log unless -VerboseOutput is specified.

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)

.PARAMETER NoRun
    If specified, skips running the app after build.

.PARAMETER Rebuild
    If specified, performs a clean rebuild.

.PARAMETER VerboseOutput
    If specified, prints output to console instead of log file.

.EXAMPLE
    pwsh ./scripts/build_windows.ps1
    pwsh ./scripts/build_windows.ps1 -Rebuild
    pwsh ./scripts/build_windows.ps1 -Configuration Release
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
$galleryProject = Join-Path $repoRoot "Flowery.Uno.Gallery\Flowery.Uno.Gallery.csproj"
$targetFramework = "net9.0-windows10.0.19041"

if (-not (Test-Path $galleryProject)) {
    Write-Error "Gallery project not found at $galleryProject"
    exit 1
}

# --- INITIALIZE ---
Initialize-BuildEnvironment -HeadName "Windows" -LogFileName "windows_build.log" -Configuration $Configuration -VerboseOutput:$VerboseOutput

# --- BUILD ---
if (-not (Invoke-ProjectBuild -Title "Flowery.Uno.Gallery (Windows)" -Project $galleryProject -Configuration $Configuration -Rebuild:$Rebuild -Framework $targetFramework)) {
    Write-BuildFailure "Flowery.Uno.Gallery (Windows)"
    exit 1
}

# --- SUMMARY ---
Write-BuildSummary

# --- RUN ---
if (-not $NoRun) {
    Start-GalleryApp -ExePattern "Flowery.Uno.Gallery.exe" -Configuration $Configuration -Framework $targetFramework
}

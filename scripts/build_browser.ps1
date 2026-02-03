<#
.SYNOPSIS
    Builds Flowery.Uno Browser projects with clean summary output.

.DESCRIPTION
    Builds Flowery.Uno (library) and Flowery.Uno.Gallery for Browser (WASM) using the single-project structure.
    Verbose build output is captured to browser_build.log, showing only a clean summary.
    By default, runs the Gallery app after a successful build.

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)

.PARAMETER NoRun
    If specified, skips running the app after build.

.PARAMETER NoBrowser
    If specified, does not automatically open the browser.

.PARAMETER Rebuild
    If specified, performs a clean rebuild (recommended after code changes).

.PARAMETER VerboseOutput
    If specified, prints output to console instead of log file.
.PARAMETER Port
    HTTP port to use when running the app (default: 5236).
.PARAMETER Cli
    If specified, skips terminal clear for non-interactive shells.

.EXAMPLE
    pwsh ./scripts/build_browser.ps1
    pwsh ./scripts/build_browser.ps1 -Rebuild
    pwsh ./scripts/build_browser.ps1 -Configuration Release
#>
param(
    [string]$Configuration = "Debug",
    [switch]$NoRun,
    [Alias("no-browser")]
    [switch]$NoBrowser,
    [switch]$Rebuild,
    [switch]$VerboseOutput,
    [int]$Port = 5236,
    [switch]$Cli
)

$ErrorActionPreference = "Stop"

# Support double-dash flags passed as raw args (e.g., --no-browser, --cli).
$rawArgs = @()
$remainingArgs = @($args)
if ($Configuration -like "--*") {
    $rawArgs += $Configuration
    $Configuration = "Debug"

    if ($remainingArgs.Count -gt 0 -and $remainingArgs[0] -notlike "-*") {
        $Configuration = $remainingArgs[0]
        if ($remainingArgs.Count -gt 1) {
            $remainingArgs = $remainingArgs[1..($remainingArgs.Count - 1)]
        } else {
            $remainingArgs = @()
        }
    }
}

$rawArgs += $remainingArgs

if ($rawArgs -contains "--no-browser") { $NoBrowser = $true }
if ($rawArgs -contains "--cli") { $Cli = $true }

# --- LOAD CORE ---
. "$PSScriptRoot\build_core.ps1"

# --- SETUP ---
$repoRoot = Get-RepoRoot
$floweryProject = Join-Path $repoRoot "Flowery.Uno\Flowery.Uno.csproj"
$galleryProject = Join-Path $repoRoot "Flowery.Uno.Gallery\Flowery.Uno.Gallery.csproj"
$targetFramework = "net9.0-browserwasm"

# --- INITIALIZE ---
Initialize-BuildEnvironment -HeadName "Browser" -LogFileName "browser_build.log" -Configuration $Configuration -VerboseOutput:$VerboseOutput -Cli:$Cli

# --- BUILD ---

# 1. Build Library
if (-not (Invoke-ProjectBuild -Title "Flowery.Uno" -Project $floweryProject -Configuration $Configuration -Rebuild:$Rebuild)) {
    Write-BuildFailure "Flowery.Uno"
    exit 1
}

# 2. Build Gallery Browser
if (-not (Invoke-ProjectBuild -Title "Flowery.Uno.Gallery (Browser)" -Project $galleryProject -Configuration $Configuration -Rebuild:$Rebuild -Framework $targetFramework)) {
    Write-BuildFailure "Flowery.Uno.Gallery (Browser)"
    exit 1
}

# --- SUMMARY ---
Write-BuildSummary

# --- RUN ---
if (-not $NoRun) {
    # Browser uses run-browser.ps1 which starts a local server
    $runBrowserScript = Join-Path $PSScriptRoot "run-browser.ps1"
    Write-Host ""
    Write-Host "Starting browser server..." -ForegroundColor Cyan
    & $runBrowserScript -Configuration $Configuration -Port $Port -Cli:$Cli -NoBrowser:$NoBrowser
}

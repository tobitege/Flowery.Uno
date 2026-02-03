<#
.SYNOPSIS
    Builds Flowery.Uno Kiosk Browser project with clean summary output.
#>
param(
    [Parameter(Position = 0)]
    [string]$Configuration = "Debug",

    [Alias("no-run")]
    [switch]$NoRun,

    [switch]$Rebuild,
    [switch]$VerboseOutput,
    [Alias("no-browser")]
    [switch]$NoBrowser,
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
$kioskProject = Join-Path $repoRoot "Flowery.Uno.Kiosk.Browser\Flowery.Uno.Kiosk.Browser.csproj"
# Build Flowery.Uno only for the browser TFM to avoid duplicate pattern assets
# coming from multiple TFMs (net9.0 + net9.0-browserwasm) in the same build.
$floweryBrowserTfm = "net9.0-browserwasm"

# --- INITIALIZE ---
Initialize-BuildEnvironment -HeadName "Kiosk Browser" -LogFileName "kiosk_browser_build.log" -Configuration $Configuration -VerboseOutput:$VerboseOutput -Cli:$Cli

# --- BUILD ---

# 1. Build Library (for browser TFM only to avoid duplicate pattern assets)
if (-not (Invoke-ProjectBuild -Title "Flowery.Uno" -Project $floweryProject -Configuration $Configuration -Rebuild:$Rebuild -Framework $floweryBrowserTfm)) {
    Write-BuildFailure "Flowery.Uno"
    exit 1
}

# 2. Build Kiosk Browser
if (-not (Invoke-ProjectBuild -Title "Flowery.Uno.Kiosk.Browser" -Project $kioskProject -Configuration $Configuration -Rebuild:$Rebuild)) {
    Write-BuildFailure "Flowery.Uno.Kiosk.Browser"
    exit 1
}

# --- SUMMARY ---
Write-BuildSummary

# --- RUN ---
if (-not $NoRun) {
    # Browser uses run-browser.ps1 which starts a local server
    $runBrowserScript = Join-Path $PSScriptRoot "run-browser.ps1"
    Write-Host ""
    Write-Host "Starting kiosk browser server..." -ForegroundColor Cyan
    
    $runArgs = @{
        Configuration = $Configuration
        Project = "Flowery.Uno.Kiosk.Browser"
        NoBrowser = $NoBrowser
        Cli = $Cli
    }
    
    & $runBrowserScript @runArgs
}

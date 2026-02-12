<#
.SYNOPSIS
    Builds all Flowery.Uno projects with correct per-project settings.

.DESCRIPTION
    Builds all heads (Windows, Desktop, Browser, Android) using dotnet build.
    Output is captured to all_build.log, showing only a clean summary.

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)

.PARAMETER AndroidSdkDirectory
    Path to Android SDK (auto-detected from environment if not specified)

.PARAMETER RestoreWorkloads
    If specified, runs dotnet workload restore first

.PARAMETER NoRun
    If specified, skips running the app after build (not applicable for all heads)

.PARAMETER Rebuild
    If specified, performs a clean rebuild

.PARAMETER Verbose
    If specified, prints output to console instead of log file

.EXAMPLE
    pwsh ./scripts/build_all.ps1
    pwsh ./scripts/build_all.ps1 -AndroidSdkDirectory "C:\Users\YOURUSER\AppData\Local\Android\Sdk"
    pwsh ./scripts/build_all.ps1 -RestoreWorkloads -Rebuild
#>
param(
    [string]$Configuration = "Debug",
    [string]$AndroidSdkDirectory = "",
    [switch]$RestoreWorkloads,
    [switch]$NoRun,
    [switch]$Rebuild,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# --- SHARED STATE ---
$script:BuildResults = @()
$script:StartTime = Get-Date
$script:LogFile = $null
$script:VerboseOutput = $Verbose

# --- SETUP ---

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$logsDir = Join-Path $repoRoot "logs"
if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir | Out-Null }
$script:LogFile = Join-Path $logsDir "all_build.log"

# Clear terminal and log
Clear-Host
if (Test-Path $script:LogFile) { Remove-Item $script:LogFile }

Write-Host "Building Flowery.Uno All Heads projects ($Configuration)..." -ForegroundColor Cyan
Write-Host ""

# --- HELPER FUNCTION ---

function Invoke-DotnetBuild {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string]$Command
    )

    Write-Host "Building $Title..." -ForegroundColor Cyan
    $stepStart = Get-Date

    if ($script:VerboseOutput) {
        Invoke-Expression $Command
        $exitCode = $LASTEXITCODE
    } else {
        # Capture output to log
        $output = Invoke-Expression "$Command 2>&1" | Out-String
        $logContent = "=== BUILD: $Title ===`r`n$Command`r`n$output`r`n"
        $logContent | Out-File -FilePath $script:LogFile -Append -Encoding utf8
        $exitCode = $LASTEXITCODE
    }

    $stepDuration = (Get-Date) - $stepStart

    if ($exitCode -ne 0) {
        $script:BuildResults += [PSCustomObject]@{ Project = $Title; Status = "FAILED"; Duration = $stepDuration }
        Write-Host "FAILED: $Title" -ForegroundColor Red
        if (-not $script:VerboseOutput) {
            Write-Host ""
            Write-Host "=== Errors ===" -ForegroundColor Yellow
            Get-Content $script:LogFile | Select-String -Pattern "error [A-Z]+\d+:" | ForEach-Object {
                Write-Host $_.Line.Trim() -ForegroundColor Red
            }
            Write-Host ""
            Write-Host "Full log: $script:LogFile" -ForegroundColor Gray
        }
        exit 1
    }

    $script:BuildResults += [PSCustomObject]@{ Project = $Title; Status = "OK"; Duration = $stepDuration }
}

# --- WORKLOADS ---

if ($RestoreWorkloads) {
    Push-Location $repoRoot
    try {
        Invoke-DotnetBuild -Title "Workloads Restore" -Command "dotnet workload restore"
    } finally {
        Pop-Location
    }
}

# --- ANDROID SDK ---

if ([string]::IsNullOrWhiteSpace($AndroidSdkDirectory)) {
    if (-not [string]::IsNullOrWhiteSpace($env:ANDROID_SDK_ROOT)) {
        $AndroidSdkDirectory = $env:ANDROID_SDK_ROOT
    } elseif (-not [string]::IsNullOrWhiteSpace($env:ANDROID_HOME)) {
        $AndroidSdkDirectory = $env:ANDROID_HOME
    } elseif (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $AndroidSdkDirectory = Join-Path $env:LOCALAPPDATA "Android\Sdk"
    }
}

if ([string]::IsNullOrWhiteSpace($AndroidSdkDirectory)) {
    throw "AndroidSdkDirectory not set. Pass -AndroidSdkDirectory or set ANDROID_SDK_ROOT / ANDROID_HOME."
}

# --- PROJECT PATHS ---

$floweryProject = Join-Path $repoRoot "Flowery.Uno/Flowery.Uno.csproj"
$galleryProject = Join-Path $repoRoot "Flowery.Uno.Gallery/Flowery.Uno.Gallery.csproj"

$buildTarget = if ($Rebuild) { "-t:Rebuild" } else { "" }
$eolSuppression = "-p:CheckEolWorkloads=false"

# --- BUILD ALL ---

# Build library first
Invoke-DotnetBuild -Title "Flowery.Uno" -Command "dotnet build `"$floweryProject`" -c $Configuration $buildTarget $eolSuppression"

# Build Gallery for each platform using single-project structure
Invoke-DotnetBuild -Title "Flowery.Uno.Gallery (Windows)" -Command "dotnet build `"$galleryProject`" -c $Configuration -f net9.0-windows10.0.19041 $buildTarget $eolSuppression"
Invoke-DotnetBuild -Title "Flowery.Uno.Gallery (Desktop)" -Command "dotnet build `"$galleryProject`" -c $Configuration -f net9.0-desktop $buildTarget $eolSuppression"
Invoke-DotnetBuild -Title "Flowery.Uno.Gallery (Browser)" -Command "dotnet build `"$galleryProject`" -c $Configuration -f net9.0-browserwasm $buildTarget $eolSuppression"

Invoke-DotnetBuild -Title "Android Dependencies" -Command "dotnet build `"$galleryProject`" -c $Configuration -f net10.0-android36.0 -t:InstallAndroidDependencies -p:AndroidSdkDirectory=`"$AndroidSdkDirectory`" $eolSuppression"
Invoke-DotnetBuild -Title "Flowery.Uno.Gallery (Android)" -Command "dotnet build `"$galleryProject`" -c $Configuration -f net10.0-android36.0 -p:AndroidSdkDirectory=`"$AndroidSdkDirectory`" $buildTarget $eolSuppression"

# --- SUMMARY ---

$totalDuration = (Get-Date) - $script:StartTime

$summary = [System.Text.StringBuilder]::new()
[void]$summary.AppendLine()
[void]$summary.AppendLine("═══════════════════════════════════════════════════════════")
[void]$summary.AppendLine(" BUILD SUMMARY")
[void]$summary.AppendLine("═══════════════════════════════════════════════════════════")
[void]$summary.AppendLine()

foreach ($result in $script:BuildResults) {
    if ($result.Status -eq "OK") {
        [void]$summary.AppendLine(("  [OK] {0,-40} {1}" -f $result.Project, $result.Duration.ToString("mm\:ss\.ff")))
    } else {
        [void]$summary.AppendLine(("  [FAILED] {0,-40} {1}" -f $result.Project, $result.Duration.ToString("mm\:ss\.ff")))
    }
}

[void]$summary.AppendLine()
[void]$summary.AppendLine(("  Total time: {0:mm\:ss\.ff}" -f $totalDuration))
[void]$summary.AppendLine(("  Projects built: {0}" -f $script:BuildResults.Count))
[void]$summary.AppendLine("  Log: $script:LogFile")
[void]$summary.AppendLine()

$failedCount = ($script:BuildResults | Where-Object { $_.Status -eq "FAILED" }).Count
if ($failedCount -eq 0) {
    [void]$summary.AppendLine("All builds completed successfully.")
    Write-Host $summary.ToString() -ForegroundColor Green
} else {
    [void]$summary.AppendLine("$failedCount build(s) failed.")
    Write-Host $summary.ToString() -ForegroundColor Red
}

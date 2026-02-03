<#
.SYNOPSIS
    Core build functions for Flowery.Uno projects.

.DESCRIPTION
    This script provides reusable build functions and must be dot-sourced by build scripts.
    It does nothing when run directly - callers must define projects and invoke functions.

.NOTES
    Usage (from another script):
        . "$PSScriptRoot\build_core.ps1"
        Initialize-BuildEnvironment -HeadName "Desktop" -LogFileName "desktop_build.log"
        Invoke-ProjectBuild -Title "Flowery.Uno" -Project $floweryProject
        Write-BuildSummary
#>

# --- GUARD: Do nothing if run directly without setup ---
if (-not $script:BuildCoreInitialized) {
    $script:BuildCoreInitialized = $false
}

# --- SHARED STATE ---
$script:BuildResults = @()
$script:BuildStartTime = $null
$script:LogFile = $null
$script:HeadName = $null
$script:VerboseOutput = $false
$script:MSBuildPath = $null

# --- INITIALIZATION ---

function Initialize-BuildEnvironment {
    <#
    .SYNOPSIS
        Initializes the build environment. Must be called before any build operations.
    .PARAMETER HeadName
        Name of the head being built (e.g., "Desktop", "Windows", "Android", "iOS")
    .PARAMETER LogFileName
        Name of the log file (placed in logs/ folder)
    .PARAMETER Configuration
        Build configuration (Debug or Release)
    .PARAMETER VerboseOutput
        If true, prints output to console instead of log file
    .PARAMETER Cli
        If true, skips terminal clear for non-interactive shells
    #>
    param(
        [Parameter(Mandatory = $true)][string]$HeadName,
        [Parameter(Mandatory = $true)][string]$LogFileName,
        [string]$Configuration = "Debug",
        [switch]$VerboseOutput,
        [switch]$Cli
    )

    $script:HeadName = $HeadName
    $script:VerboseOutput = $VerboseOutput
    $script:BuildStartTime = Get-Date
    $script:BuildResults = @()

    # Clear terminal (skip in non-interactive shells)
    if (-not $Cli) {
        try {
            Clear-Host
        } catch {
        }
    }

    # Locate MSBuild via vswhere
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) { 
        Write-Error "vswhere.exe not found."
        exit 1 
    }

    $vsPath = & $vswhere -latest -property installationPath
    if (-not $vsPath) { 
        Write-Error "No Visual Studio found."
        exit 1 
    }

    $script:MSBuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
    if (-not (Test-Path $script:MSBuildPath)) { 
        Write-Error "MSBuild not found."
        exit 1 
    }

    # Setup logging
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
    $logsDir = Join-Path $repoRoot "logs"
    if (-not (Test-Path $logsDir)) { 
        New-Item -ItemType Directory -Path $logsDir | Out-Null 
    }
    $script:LogFile = Join-Path $logsDir $LogFileName

    # Clear previous log
    if (Test-Path $script:LogFile) { 
        Remove-Item $script:LogFile 
    }

    $script:BuildCoreInitialized = $true

    # Print header
    Write-Host "Building Flowery.Uno $HeadName projects ($Configuration)..." -ForegroundColor Cyan
    Write-Host ""
}

# --- BUILD FUNCTION ---

function Invoke-ProjectBuild {
    <#
    .SYNOPSIS
        Builds a single project using MSBuild.
    .PARAMETER Title
        Display name for the project
    .PARAMETER Project
        Full path to the .csproj file
    .PARAMETER Configuration
        Build configuration (default: Debug)
    .PARAMETER Rebuild
        If true, performs a clean rebuild
    .PARAMETER Framework
        Target framework (e.g., net9.0-windows10.0.19041, net9.0-desktop)
    .RETURNS
        $true if build succeeded, $false otherwise
    #>
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string]$Project,
        [string]$Configuration = "Debug",
        [switch]$Rebuild,
        [string]$Framework
    )

    if (-not $script:BuildCoreInitialized) {
        Write-Error "Build environment not initialized. Call Initialize-BuildEnvironment first."
        exit 1
    }

    $rebuildSuffix = if ($Rebuild) { " (rebuild)" } else { "" }
    Write-Host "Building $Title...$rebuildSuffix" -ForegroundColor Cyan
    $stepStart = Get-Date

    # Build arguments
    $target = if ($Rebuild) { "Rebuild" } else { "Build" }
    $buildArgs = @("$Project", "-t:$target", "-verbosity:normal", "-nologo", "-restore", "-p:Configuration=$Configuration")
    if ($Framework) {
        $buildArgs += "-p:TargetFramework=$Framework"
    }

    # Run MSBuild
    $stdout = ""
    $stderr = ""
    
    if ($script:VerboseOutput) {
        # In verbose mode, we still need to capture output to count warnings
        $pInfo = New-Object System.Diagnostics.ProcessStartInfo
        $pInfo.FileName = $script:MSBuildPath
        $pInfo.Arguments = $buildArgs -join " "
        $pInfo.RedirectStandardOutput = $true
        $pInfo.RedirectStandardError = $true
        $pInfo.UseShellExecute = $false
        $pInfo.CreateNoWindow = $true
        # Use UTF-8 encoding to prevent codepage mangling of special characters (German umlauts, etc.)
        $pInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
        $pInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8

        $p = [System.Diagnostics.Process]::Start($pInfo)
        $stdout = $p.StandardOutput.ReadToEnd()
        $stderr = $p.StandardError.ReadToEnd()
        $p.WaitForExit()
        $exitCode = $p.ExitCode
        
        # Print to console in verbose mode
        Write-Host $stdout
        if ($stderr) { Write-Host $stderr -ForegroundColor Yellow }
    } else {
        $pInfo = New-Object System.Diagnostics.ProcessStartInfo
        $pInfo.FileName = $script:MSBuildPath
        $pInfo.Arguments = $buildArgs -join " "
        $pInfo.RedirectStandardOutput = $true
        $pInfo.RedirectStandardError = $true
        $pInfo.UseShellExecute = $false
        $pInfo.CreateNoWindow = $true
        # Use UTF-8 encoding to prevent codepage mangling of special characters (German umlauts, etc.)
        $pInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
        $pInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8

        $p = [System.Diagnostics.Process]::Start($pInfo)
        $stdout = $p.StandardOutput.ReadToEnd()
        $stderr = $p.StandardError.ReadToEnd()
        $p.WaitForExit()
        $exitCode = $p.ExitCode

        # Append to log file
        $logContent = "=== BUILD: $Title ===`r`n" + $stdout + "`r`n" + $stderr + "`r`n"
        $logContent | Out-File -FilePath $script:LogFile -Append -Encoding utf8
    }

    $stepDuration = (Get-Date) - $stepStart
    
    # Count warnings from output (pattern: "warning CS1234:" or "warning NU1234:" etc.)
    $warningCount = ($stdout | Select-String -Pattern "warning [A-Z]+\d+:" -AllMatches).Matches.Count

    if ($exitCode -ne 0) {
        $script:BuildResults += [PSCustomObject]@{ Project = $Title; Status = "FAILED"; Duration = $stepDuration; Warnings = $warningCount }
        return $false
    }

    $script:BuildResults += [PSCustomObject]@{ Project = $Title; Status = "OK"; Duration = $stepDuration; Warnings = $warningCount }
    return $true
}

# --- FAILURE OUTPUT ---

function Write-BuildFailure {
    <#
    .SYNOPSIS
        Displays build failure information with errors/warnings from log.
    .PARAMETER ProjectName
        Name of the failed project
    #>
    param([string]$ProjectName)

    Write-Host ""
    Write-Host "BUILD FAILED: $ProjectName" -ForegroundColor Red

    if (-not $script:VerboseOutput -and (Test-Path $script:LogFile)) {
        Write-Host ""
        Write-Host "=== Errors and Warnings ===" -ForegroundColor Yellow
        Get-Content $script:LogFile | Select-String -Pattern "(error|warning) [A-Z]+\d+:" | ForEach-Object {
            Write-Host $_.Line.Trim() -ForegroundColor ($_.Line -match "error" ? "Red" : "Yellow")
        }
        Write-Host ""
        Write-Host "Full log: $script:LogFile" -ForegroundColor Gray
    }
}

# --- SUMMARY ---

function Write-BuildSummary {
    <#
    .SYNOPSIS
        Writes the build summary to console.
    #>
    if (-not $script:BuildCoreInitialized) {
        Write-Error "Build environment not initialized."
        exit 1
    }

    $totalDuration = (Get-Date) - $script:BuildStartTime

    $summary = [System.Text.StringBuilder]::new()
    [void]$summary.AppendLine()
    [void]$summary.AppendLine("═══════════════════════════════════════════════════════════")
    [void]$summary.AppendLine(" BUILD SUMMARY")
    [void]$summary.AppendLine("═══════════════════════════════════════════════════════════")
    [void]$summary.AppendLine()

    $totalWarnings = 0
    foreach ($result in $script:BuildResults) {
        $totalWarnings += $result.Warnings
        $warnText = if ($result.Warnings -gt 0) { " ($($result.Warnings) warnings)" } else { "" }
        if ($result.Status -eq "OK") {
            [void]$summary.AppendLine(("  [OK] {0,-40} {1}{2}" -f $result.Project, $result.Duration.ToString("mm\:ss\.ff"), $warnText))
        } else {
            [void]$summary.AppendLine(("  [FAILED] {0,-40} {1}{2}" -f $result.Project, $result.Duration.ToString("mm\:ss\.ff"), $warnText))
        }
    }

    [void]$summary.AppendLine()
    [void]$summary.AppendLine(("  Total time: {0:mm\:ss\.ff}" -f $totalDuration))
    [void]$summary.AppendLine(("  Projects built: {0}" -f $script:BuildResults.Count))
    if ($totalWarnings -gt 0) {
        [void]$summary.AppendLine(("  Total warnings: {0}" -f $totalWarnings))
    }
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
}

# --- APP RUNNER ---

function Start-GalleryApp {
    <#
    .SYNOPSIS
        Starts the Gallery application after a successful build.
    .PARAMETER ExePattern
        Pattern to search for the executable (e.g., "Flowery.Uno.Gallery.exe")
    .PARAMETER Configuration
        Build configuration to find the correct output folder
    .PARAMETER Framework
        Target framework to narrow down the search path
    #>
    param(
        [Parameter(Mandatory = $true)][string]$ExePattern,
        [string]$Configuration = "Debug",
        [string]$Framework
    )

    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
    
    # With single-project structure, output is in bin/$Configuration/Flowery.Uno.Gallery/$Framework
    if ($Framework) {
        $outputDir = Join-Path $repoRoot "bin\$Configuration\Flowery.Uno.Gallery\$Framework"
    } else {
        $outputDir = Join-Path $repoRoot "bin\$Configuration"
    }
    
    $exePath = Get-ChildItem -Path $outputDir -Recurse -Filter $ExePattern -ErrorAction SilentlyContinue |
               Sort-Object LastWriteTime -Descending |
               Select-Object -First 1

    if ($exePath) {
        Write-Host "Starting Gallery app..." -ForegroundColor Cyan
        Start-Process -FilePath $exePath.FullName -WorkingDirectory $exePath.DirectoryName | Out-Null
        Write-Host "Started: $($exePath.Name)" -ForegroundColor Green
    } else {
        Write-Host "Warning: Could not find $ExePattern in $outputDir" -ForegroundColor Yellow
    }
}

# --- UTILITY ---

function Get-RepoRoot {
    return Resolve-Path (Join-Path $PSScriptRoot "..")
}

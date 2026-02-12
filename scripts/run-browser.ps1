<#
.SYNOPSIS
    Starts the Flowery.Uno.Gallery.Browser WASM application.

.DESCRIPTION
    Builds the Browser WASM project and serves it using Python's HTTP server.

.PARAMETER NoBrowser
    If specified, does not automatically open the browser.

.PARAMETER Port
    HTTP port to use (default: 5235).
.PARAMETER Cli
    If specified, skips terminal clear for non-interactive shells.
.PARAMETER Rebuild
    If specified, performs a clean rebuild before running.

.EXAMPLE
    pwsh ./scripts/run-browser.ps1
    pwsh ./scripts/run-browser.ps1 -NoBrowser
    pwsh ./scripts/run-browser.ps1 -Port 8080
#>
param(
    [Alias("no-browser")]
    [switch]$NoBrowser,
    [int]$Port = 5235,
    [switch]$Cli,
    [string]$Configuration = "Debug",
    [switch]$Rebuild,
    [string]$Project = "Flowery.Uno.Gallery"
)

$ErrorActionPreference = "Stop"

# Support double-dash flags passed as raw args (e.g., --no-browser, --cli).
if ($args -contains "--no-browser") { $NoBrowser = $true }
if ($args -contains "--cli") { $Cli = $true }

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot $Project
$targetFramework = "net9.0-browserwasm"

if (-not (Test-Path $projectPath)) {
    Write-Host "ERROR: Project not found at $projectPath" -ForegroundColor Red
    exit 1
}

$url = "http://localhost:$Port/"

# Clear terminal before printing status messages (skip in non-interactive shells)
if (-not $Cli) {
    Clear-Host
}

# Check if port is already in use
$portInUse = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($portInUse) {
    $owner = $null
    if ($portInUse.OwningProcess) {
        $owner = Get-Process -Id $portInUse.OwningProcess -ErrorAction SilentlyContinue
    }

    $ownerInfo = if ($owner) { " (PID $($owner.Id): $($owner.ProcessName))" } else { "" }
    Write-Host "ERROR: Port $Port is already in use$ownerInfo." -ForegroundColor Red
    Write-Host "Stop the other server or run with -Port <free port>." -ForegroundColor Yellow
    exit 1
}

Write-Host "Running $Project Browser ($Configuration)..." -ForegroundColor Cyan
Write-Host ""
Write-Host "  URL: $url" -ForegroundColor Gray
Write-Host "  Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

Push-Location $projectPath
try {
    $restoreArgs = @(
        "-p:TargetFramework=$targetFramework",
        "-p:TargetFrameworks=$targetFramework",
        "-p:CheckEolWorkloads=false",
        "-p:CheckEolTargetFramework=false"
    )
    $buildArgs = @(
        "-c", $Configuration,
        "-f", $targetFramework,
        "-p:TargetFrameworks=$targetFramework",
        "-p:CheckEolWorkloads=false",
        "-p:CheckEolTargetFramework=false"
    )

    if ($Rebuild) {
        Write-Host "Restoring packages..." -ForegroundColor Yellow
        dotnet restore @restoreArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Restore failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }

        Write-Host "Building with -t:Rebuild..." -ForegroundColor Yellow
        dotnet build @buildArgs -t:Rebuild
    } else {
        Write-Host "Building..." -ForegroundColor Yellow
        dotnet build @buildArgs
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    # Find the wwwroot folder in the build output
    # The structure can be either:
    # - bin/Debug/Flowery.Uno.Gallery.Browser/net9.0-browserwasm/wwwroot (older)
    # - bin/Debug/Flowery.Uno.Gallery.Browser/net9.0-browserwasm/browser-wasm/wwwroot (newer)
    $baseBuildPath = Join-Path $repoRoot "bin\$Configuration\$Project\$targetFramework"
    $wwwrootPath = Join-Path $baseBuildPath "wwwroot"

    if (-not (Test-Path $wwwrootPath)) {
        # Check browser-wasm subfolder
        $wwwrootPath = Join-Path $baseBuildPath "browser-wasm\wwwroot"
    }

    if (-not (Test-Path $wwwrootPath)) {
        Write-Host "ERROR: wwwroot folder not found" -ForegroundColor Red
        Write-Host "Looking for wwwroot in build output..." -ForegroundColor Yellow
        $found = Get-ChildItem -Recurse -Directory -Filter "wwwroot" $baseBuildPath 2>$null | Select-Object -First 1
        if ($found) {
            Write-Host "Found at: $($found.FullName)" -ForegroundColor Green
            $wwwrootPath = $found.FullName
        } else {
            Write-Host "Not found in $baseBuildPath" -ForegroundColor Red
            exit 1
        }
    }

    Write-Host ""
    Write-Host "Build successful!" -ForegroundColor Green

    # Create _framework symlink if it doesn't exist
    # uno-bootstrap.js expects runtime files at /_framework/dotnet.js
    # but they're in the parent folder (net9.0-browserwasm)
    $frameworkLink = Join-Path $wwwrootPath "_framework"
    $parentPath = Split-Path $wwwrootPath -Parent

    if (-not (Test-Path $frameworkLink)) {
        Write-Host "Creating _framework symlink..." -ForegroundColor Yellow
        # Use junction on Windows (works without admin rights)
        cmd /c mklink /J "$frameworkLink" "$parentPath" 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "WARNING: Could not create _framework symlink. Copying files instead..." -ForegroundColor Yellow
            New-Item -ItemType Directory -Path $frameworkLink -Force | Out-Null
            Copy-Item "$parentPath\dotnet.*" $frameworkLink -Force
        }
    }

    # Generate blazor.boot.json if it doesn't exist
    # This file is required by uno-bootstrap.js but only generated during publish
    $bootJsonPath = Join-Path $frameworkLink "blazor.boot.json"
    if (-not (Test-Path $bootJsonPath)) {
        Write-Host "Generating blazor.boot.json..." -ForegroundColor Yellow

        # Find all DLL files in the build output
        $assemblies = @{}
        Get-ChildItem -Path $parentPath -Filter "*.dll" | ForEach-Object {
            $assemblies[$_.Name] = ""
        }

        $bootJson = @{
            mainAssemblyName = "$Project.dll"
            globalizationMode = "icu"
            icuDataMode = "sharded"
            cacheBootResources = $false
            debugBuild = $true
            linkerEnabled = $false
            resources = @{
                jsModuleNative = @{
                    "dotnet.native.js" = ""
                }
                jsModuleRuntime = @{
                    "dotnet.runtime.js" = ""
                }
                wasmNative = @{
                    "dotnet.native.wasm" = ""
                }
                assembly = $assemblies
                icu = @{
                    "icudt_hybrid.dat" = ""
                }
            }
            config = @()
        }

        $bootJson | ConvertTo-Json -Depth 10 | Set-Content -Path $bootJsonPath -Encoding UTF8
    }

    Write-Host "Serving from: $wwwrootPath" -ForegroundColor Cyan
    $fullPath = (Resolve-Path $wwwrootPath).Path
    Write-Host "Full local path: $fullPath" -ForegroundColor Gray
    Write-Host ""

    # Open browser once the server is listening
    if (-not $NoBrowser) {
        Start-Job -ScriptBlock {
            param($url, $port)
            $timeoutSeconds = 30
            $start = Get-Date
            while ((Get-Date) - $start -lt [TimeSpan]::FromSeconds($timeoutSeconds)) {
                try {
                    $probe = Test-NetConnection -ComputerName "127.0.0.1" -Port $port -WarningAction SilentlyContinue
                    if ($probe -and $probe.TcpTestSucceeded) {
                        Start-Process $url
                        return
                    }
                } catch {
                }
                Start-Sleep -Milliseconds 250
            }

            Start-Process $url
        } -ArgumentList $url, $Port | Out-Null
    }

    # Serve from wwwroot folder
    Push-Location $wwwrootPath
    try {
        python -m http.server $Port --bind 127.0.0.1
    } finally {
        Pop-Location
    }

} finally {
    Pop-Location
}

<#
.SYNOPSIS
    Publishes the Flowery.Uno.Gallery for Windows.

.DESCRIPTION
    Uses dotnet publish to produce a self-contained Windows build. Output is logged to
    logs/windows_publish.log unless -VerboseOutput is specified.

.PARAMETER Configuration
    Publish configuration: Debug or Release (default: Release)

.PARAMETER RuntimeIdentifier
    Runtime identifier (default: win-x64)

.PARAMETER SelfContained
    Whether to publish self-contained output (default: true)

.PARAMETER OutputDir
    Output directory for published files (default: publish)

.PARAMETER VerboseOutput
    If specified, prints output to console and still writes the log.

.EXAMPLE
    pwsh ./scripts/publish_windows.ps1
    pwsh ./scripts/publish_windows.ps1 -Configuration Release
    pwsh ./scripts/publish_windows.ps1 -SelfContained:$false -OutputDir ./publish-win
#>
param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [bool]$SelfContained = $true,
    [string]$OutputDir = "publish",
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$floweryProject = Join-Path $repoRoot "Flowery.Uno\Flowery.Uno.csproj"
$galleryProject = Join-Path $repoRoot "Flowery.Uno.Gallery\Flowery.Uno.Gallery.csproj"
$win2dProject = Join-Path $repoRoot "Flowery.Uno.Win2D\Flowery.Uno.Win2D.csproj"
$windowsTfm = "net9.0-windows10.0.19041"

$missingProjects = @()
if (-not (Test-Path $floweryProject)) { $missingProjects += $floweryProject }
if (-not (Test-Path $galleryProject)) { $missingProjects += $galleryProject }
if (-not (Test-Path $win2dProject)) { $missingProjects += $win2dProject }

if ($missingProjects.Count -gt 0) {
    Write-Error ("Project not found: " + ($missingProjects -join ", "))
    exit 1
}

$logsDir = Join-Path $repoRoot "logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
}

$logFile = Join-Path $logsDir "windows_publish.log"
"" | Out-File -FilePath $logFile -Encoding utf8

if (-not [System.IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir = Join-Path $repoRoot $OutputDir
}

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

function Invoke-DotnetStep {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string[]]$Args
    )

    Write-Host "$Title..." -ForegroundColor Cyan

    $commandLine = "dotnet " + ($Args -join " ")
    $header = "=== $Title ===`r`n$commandLine`r`n"
    $header | Out-File -FilePath $logFile -Append -Encoding utf8

    if ($VerboseOutput) {
        & dotnet @Args 2>&1 | Tee-Object -FilePath $logFile -Append -Encoding utf8
    } else {
        & dotnet @Args 2>&1 | Out-File -FilePath $logFile -Append -Encoding utf8
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $Title" -ForegroundColor Red
        Write-Host "Log: $logFile" -ForegroundColor Gray

        if (-not $VerboseOutput -and (Test-Path $logFile)) {
            Write-Host ""
            Write-Host "=== Errors and Warnings ===" -ForegroundColor Yellow
            Get-Content $logFile | Select-String -Pattern "(error|warning) [A-Z]+\d+:" | ForEach-Object {
                Write-Host $_.Line.Trim() -ForegroundColor ($_.Line -match "error" ? "Red" : "Yellow")
            }
        }
        exit 1
    }
}

try {
    Clear-Host
} catch {
}

Write-Host "Publishing Flowery.Uno.Gallery for Windows ($Configuration)..." -ForegroundColor Cyan
Write-Host "Output: $OutputDir" -ForegroundColor Gray
Write-Host "RID: $RuntimeIdentifier, SelfContained: $SelfContained" -ForegroundColor Gray
Write-Host ""

$commonMsbuildArgs = @(
    "-m:1",
    "-p:BuildInParallel=false",
    "-p:UseSharedCompilation=false",
    "-p:UnoDisableMonoRuntimeCheck=true",
    "-p:UnoDisableValidateWinAppSDK3548=true",
    "-p:UseMonoRuntime=false"
)

$galleryBuildOutputBase = Join-Path $repoRoot "bin\\$Configuration\\Flowery.Uno.Gallery\\$windowsTfm"
$galleryBuildOutput = Join-Path $galleryBuildOutputBase $RuntimeIdentifier
if (-not (Test-Path $galleryBuildOutput)) {
    # Try with Platform folder (common in WinUI 3)
    $galleryBuildOutput = Join-Path $repoRoot "bin\\x64\\$Configuration\\Flowery.Uno.Gallery\\$windowsTfm\\$RuntimeIdentifier"
}

$floweryBuildOutputBase = Join-Path $repoRoot "bin\\$Configuration\\Flowery.Uno\\$windowsTfm"
$floweryBuildOutput = Join-Path $floweryBuildOutputBase $RuntimeIdentifier
if (-not (Test-Path $floweryBuildOutput)) {
    # Try with Platform folder
    $floweryBuildOutput = Join-Path $repoRoot "bin\\x64\\$Configuration\\Flowery.Uno\\$windowsTfm\\$RuntimeIdentifier"
}

Invoke-DotnetStep -Title "Build Flowery.Uno (windows only)" -Args (@(
    "build", $floweryProject,
    "-c", $Configuration,
    "-f", $windowsTfm,
    "-p:TargetFramework=$windowsTfm",
    "-p:TargetFrameworks=$windowsTfm"
) + $commonMsbuildArgs)

Invoke-DotnetStep -Title "Build Flowery.Uno.Win2D (windows only)" -Args (@(
    "build", $win2dProject,
    "-c", $Configuration,
    "-f", $windowsTfm,
    "-p:TargetFramework=$windowsTfm",
    "-p:TargetFrameworks=$windowsTfm",
    "-p:BuildProjectReferences=false"
) + $commonMsbuildArgs)

Invoke-DotnetStep -Title "Restore Flowery.Uno.Gallery (Windows only)" -Args (@(
    "restore", $galleryProject,
    "-p:TargetFramework=$windowsTfm",
    "-p:TargetFrameworks=$windowsTfm",
    "-p:RuntimeIdentifier=$RuntimeIdentifier",
    "-p:SelfContained=$($SelfContained.ToString().ToLowerInvariant())"
) + $commonMsbuildArgs)

Invoke-DotnetStep -Title "Build Flowery.Uno.Gallery (Windows)" -Args (@(
    "build", $galleryProject,
    "-c", $Configuration,
    "-f", $windowsTfm,
    "-r", $RuntimeIdentifier,
    "-p:SelfContained=$($SelfContained.ToString().ToLowerInvariant())",
    "--no-restore"
) + $commonMsbuildArgs)

Invoke-DotnetStep -Title "Publish Flowery.Uno.Gallery (Windows)" -Args (@(
    "publish", $galleryProject,
    "-c", $Configuration,
    "-f", $windowsTfm,
    "-r", $RuntimeIdentifier,
    "--self-contained", $SelfContained.ToString().ToLowerInvariant(),
    "-p:GenerateMergedPriFile=true",
    "-p:AppxPackage=false",
    "-p:AppxBundle=Never",
    "--no-restore",
    "-o", $OutputDir
) + $commonMsbuildArgs)

# 1. Rename the freshly produced merged PRI file.
# Unpackaged apps MUST have [ExecutableName].pri at the root to resolve resources correctly.
$publishPri = Join-Path $OutputDir "resources.pri"
if (Test-Path $publishPri) {
    $destPri = Join-Path $OutputDir "Flowery.Uno.Gallery.pri"
    Write-Host "Renaming fresh merged PRI file to: $destPri" -ForegroundColor Gray
    Copy-Item -Path $publishPri -Destination $destPri -Force
} else {
    Write-Host "Warning: resources.pri not found in $OutputDir! Resource resolution will likely fail." -ForegroundColor Yellow
}

# 2. Sync project library resources (Assets and Themes)
# We ONLY copy from the FRESH build output of the projects to avoid ancient artifacts.

function Copy-IfExists {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (Test-Path $Source) {
        $parent = Split-Path $Destination -Parent
        if (-not (Test-Path $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        # Copy the directory itself to the parent, creating the destination folder correctly.
        Copy-Item -Path $Source -Destination $parent -Recurse -Force
    }
}

if (Test-Path $floweryBuildOutput) {
    $floweryPackageDir = Join-Path $OutputDir "Flowery.Uno"
    if (-not (Test-Path $floweryPackageDir)) {
        New-Item -ItemType Directory -Path $floweryPackageDir -Force | Out-Null
    }

    Copy-IfExists -Source (Join-Path $floweryBuildOutput "Assets") -Destination (Join-Path $floweryPackageDir "Assets")
    Copy-IfExists -Source (Join-Path $floweryBuildOutput "Themes") -Destination (Join-Path $floweryPackageDir "Themes")
}

# 3. Collect ANY remaining PRIs from the dependencies' FRESH build output.
# Some libraries might need their own PRI if not fully merged.
Write-Host "Syncing PRI files from dependencies..." -ForegroundColor Gray
$projectDirs = "Flowery.Uno", "Flowery.Uno.Gallery.Core", "Flowery.Uno.Kanban", "Flowery.Uno.Win2D"
foreach ($proj in $projectDirs) {
    $projOutput = Join-Path $repoRoot "bin\\$Configuration\\$proj\\$windowsTfm\\$RuntimeIdentifier"
    if (-not (Test-Path $projOutput)) {
        $projOutput = Join-Path $repoRoot "bin\\x64\\$Configuration\\$proj\\$windowsTfm\\$RuntimeIdentifier"
    }

    if (Test-Path $projOutput) {
        # Copy PRIs that are fresh (not from 2025!)
        $pris = Get-ChildItem -Path $projOutput -Filter "*.pri" | Where-Object { $_.LastWriteTime -gt (Get-Date).AddDays(-1) }
        foreach ($pri in $pris) {
            if ($pri.Name -ne "resources.pri") {
                Copy-Item -Path $pri.FullName -Destination $OutputDir -Force
                Write-Host "  Sync'd $($pri.Name) from $proj" -ForegroundColor Gray
            }
        }
    }
}

Write-Host "Publish succeeded." -ForegroundColor Green
Write-Host "Output: $OutputDir" -ForegroundColor Green
Write-Host "Log: $logFile" -ForegroundColor Gray

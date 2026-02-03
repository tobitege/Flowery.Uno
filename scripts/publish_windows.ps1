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
if (Test-Path $logFile) {
    Remove-Item $logFile
}

if (-not [System.IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir = Join-Path $repoRoot $OutputDir
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
    "-p:UseSharedCompilation=false"
)

$galleryBuildOutput = Join-Path $repoRoot "bin\\$Configuration\\Flowery.Uno.Gallery\\$windowsTfm\\$RuntimeIdentifier"
$floweryBuildOutput = Join-Path $repoRoot "bin\\$Configuration\\Flowery.Uno\\$windowsTfm\\$RuntimeIdentifier"

Invoke-DotnetStep -Title "Build Flowery.Uno (windows only)" -Args (@(
    "build", $floweryProject,
    "-c", $Configuration,
    "-f", $windowsTfm
) + $commonMsbuildArgs)

Invoke-DotnetStep -Title "Build Flowery.Uno.Win2D (windows only)" -Args (@(
    "build", $win2dProject,
    "-c", $Configuration,
    "-f", $windowsTfm,
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
    "--no-restore",
    "--no-build",
    "-o", $OutputDir
) + $commonMsbuildArgs)

if (Test-Path $galleryBuildOutput) {
    $resourcePri = Join-Path $galleryBuildOutput "resources.pri"
    if (Test-Path $resourcePri) {
        Copy-Item -Path $resourcePri -Destination $OutputDir -Force
    }

    $rootXamlFiles = Get-ChildItem -File $galleryBuildOutput | Where-Object {
        $_.Extension -in @(".xaml", ".xbf")
    }
    foreach ($file in $rootXamlFiles) {
        Copy-Item -Path $file.FullName -Destination (Join-Path $OutputDir $file.Name) -Force
    }

    $contentDirs = Get-ChildItem -Directory $galleryBuildOutput
    foreach ($dir in $contentDirs) {
        $destDir = Join-Path $OutputDir $dir.Name
        Copy-Item -Path $dir.FullName -Destination $destDir -Recurse -Force
    }
} else {
    Write-Host "Warning: Build output not found at $galleryBuildOutput (skipping resource merge)." -ForegroundColor Yellow
}

function Copy-IfExists {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (Test-Path $Source) {
        Copy-Item -Path $Source -Destination $Destination -Recurse -Force
    }
}

if (Test-Path $floweryBuildOutput) {
    $floweryPackageDir = Join-Path $OutputDir "Flowery.Uno"
    New-Item -ItemType Directory -Path $floweryPackageDir -Force | Out-Null
    Copy-IfExists -Source (Join-Path $floweryBuildOutput "Assets") -Destination (Join-Path $floweryPackageDir "Assets")
    Copy-IfExists -Source (Join-Path $floweryBuildOutput "Themes") -Destination (Join-Path $floweryPackageDir "Themes")
} else {
    Write-Host "Warning: Flowery.Uno build output not found at $floweryBuildOutput (skipping library resources)." -ForegroundColor Yellow
}

Write-Host "Publish succeeded." -ForegroundColor Green
Write-Host "Output: $OutputDir" -ForegroundColor Green
Write-Host "Log: $logFile" -ForegroundColor Gray

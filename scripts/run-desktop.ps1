<#
.SYNOPSIS
    Builds and starts the Flowery.Uno.Gallery for Desktop.

.DESCRIPTION
    Builds the Gallery for Desktop (Skia) and, if the resulting .exe exists, starts it in the background.

.PARAMETER Configuration
    Build configuration to use (default: Debug).
.PARAMETER Rebuild
    If specified, performs a clean rebuild before running.

.EXAMPLE
    pwsh ./scripts/run-desktop.ps1
    pwsh ./scripts/run-desktop.ps1 -Configuration Release
#>
param(
    [string]$Configuration = "Debug",
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$galleryProject = Join-Path $repoRoot "Flowery.Uno.Gallery/Flowery.Uno.Gallery.csproj"
$targetFramework = "net9.0-desktop"

if (-not (Test-Path $galleryProject)) {
    Write-Host "ERROR: Gallery project not found at $galleryProject" -ForegroundColor Red
    exit 1
}

Clear-Host
Write-Host "Running Flowery.Uno Desktop ($Configuration)..." -ForegroundColor Cyan
Write-Host ""

$target = if ($Rebuild) { "Rebuild" } else { "Build" }
dotnet build "$galleryProject" -c $Configuration -f $targetFramework -t:$target

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$outputDir = Join-Path $repoRoot "bin/$Configuration/Flowery.Uno.Gallery/$targetFramework"
$exePath = Get-ChildItem -Path $outputDir -Recurse -Filter "Flowery.Uno.Gallery.exe" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $exePath) {
    Write-Host "Desktop executable not found under $outputDir (skipping run)" -ForegroundColor Yellow
    exit 0
}

Write-Host "Starting Desktop app..." -ForegroundColor Cyan
Start-Process -FilePath $exePath.FullName -WorkingDirectory $exePath.DirectoryName | Out-Null

Write-Host "Started: $($exePath.FullName)" -ForegroundColor Green

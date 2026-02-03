# Run Flowery.Uno Gallery on Android Emulator
# Usage: .\run-android.ps1 [-DeviceName "emulator-5554"] [-Configuration "Debug"] [-Rebuild]

param(
    [string]$DeviceName = "emulator-5554",
    [string]$Configuration = "Debug",
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot "Flowery.Uno.Gallery"
$targetFramework = "net9.0-android"

Clear-Host
Write-Host "Running Flowery.Uno Android ($Configuration)..." -ForegroundColor Cyan
Write-Host ""
Write-Host "  Device: $DeviceName" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $projectPath)) {
    Write-Host "ERROR: Gallery project not found at $projectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Checking for connected devices..." -ForegroundColor Yellow
$devices = & adb devices 2>&1 | Out-String
$deviceFound = $devices -like "*$DeviceName*"
if (-not $deviceFound) {
    Write-Host "WARNING: Device '$DeviceName' not found in ADB devices list." -ForegroundColor Yellow
    Write-Host "Available devices:" -ForegroundColor Yellow
    Write-Host $devices -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit 0
    }
} else {
    Write-Host "Found device: $DeviceName" -ForegroundColor Green
}

Write-Host ""
Write-Host "Building and deploying to $DeviceName..." -ForegroundColor Green
Write-Host ""

Push-Location $projectPath
try {
    if ($Rebuild) {
        dotnet build -t:Rebuild -f $targetFramework -c $Configuration
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    dotnet build -t:Run -f $targetFramework -c $Configuration -p:DeviceName=$DeviceName

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "SUCCESS: App deployed and running!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "ERROR: Build/deploy failed with exit code $LASTEXITCODE" -ForegroundColor Red
    }
}
finally {
    Pop-Location
}

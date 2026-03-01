# InScope Windows Readiness Script
# Verifies prerequisites, builds, and runs the app per docs/VALIDATION_CHECKLIST.md
# Run from project root: .\scripts\run-readiness.ps1

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

# 1. Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Cyan
$dotnetVersion = ""
try {
    $dotnetVersion = (dotnet --version 2>&1) | Out-String
    $dotnetVersion = $dotnetVersion.Trim()
} catch { }
if (-not $dotnetVersion -or $dotnetVersion -notmatch "^8\.") {
    Write-Host "ERROR: .NET SDK 8.0 or later is required but not found." -ForegroundColor Red
    Write-Host "Install from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    Write-Host "Then run this script again."
    exit 1
}
Write-Host "  .NET SDK $dotnetVersion found" -ForegroundColor Green

# 2. Restore and build
Push-Location $projectRoot
Write-Host ""
Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) { Pop-Location; exit 1 }

Write-Host "Building Release..." -ForegroundColor Cyan
dotnet build -c Release --no-restore
if ($LASTEXITCODE -ne 0) { Pop-Location; exit 1 }

Write-Host ""
Write-Host "Build succeeded. Launching InScope..." -ForegroundColor Green
Pop-Location

# 3. Run the app
dotnet run --project $projectRoot --no-build -c Release

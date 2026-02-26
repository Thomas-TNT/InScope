# InScope Publish Script (Windows PowerShell)
# Run from project root: .\scripts\publish.ps1

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$publishDir = Join-Path $projectRoot "bin\Release\net8.0-windows\win-x64\publish"

Push-Location $projectRoot

Write-Host "Restoring..."
dotnet restore

Write-Host "Publishing..."
dotnet publish -c Release -r win-x64 --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true

Pop-Location

Write-Host ""
Write-Host "Publish complete. Output: $publishDir"
Write-Host "Contents: InScope.exe + Content\ folder"
Write-Host "Copy entire publish folder to target machine. No .NET runtime required."

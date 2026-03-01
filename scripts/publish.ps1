# InScope Publish Script (Windows PowerShell)
# Run from project root: .\scripts\publish.ps1

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot "bin\Release\net8.0-windows\win-x64\publish"
$distDir = Join-Path $projectRoot "dist"
$issPath = Join-Path $projectRoot "installer\InScope.iss"

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

# Build installer if Inno Setup is available
$isccPaths = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $null
foreach ($p in $isccPaths) {
    if (Test-Path $p) { $iscc = $p; break }
}

if ($iscc) {
    $version = "1.0.0"
    $csprojPath = Join-Path $projectRoot "InScope.csproj"
    if (Test-Path $csprojPath) {
        $content = Get-Content $csprojPath -Raw
        if ($content -match '<Version>(.*?)</Version>') { $version = $matches[1].Trim() }
    }
    Write-Host "Building installer (version $version)..."
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
    & $iscc "/DAppVersion=$version" $issPath
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Installer built: $distDir\InScope-Setup.exe"
    } else {
        Write-Host "Installer build failed. Publish folder is ready to copy."
    }
} else {
    Write-Host "To build installer, install Inno Setup and run: ISCC /DAppVersion=1.0.0 installer\InScope.iss"
}

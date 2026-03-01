# InScope Build Installer Script (Windows PowerShell)
# Run from project root: .\scripts\build-installer.ps1
# Publishes the app, then builds the Inno Setup installer.

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

Push-Location $projectRoot

& (Join-Path $scriptDir "publish.ps1")

Pop-Location

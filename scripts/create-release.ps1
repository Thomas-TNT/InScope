# InScope Create Release Script
# Triggers a GitHub Release by creating and pushing a version tag.
# Double-click or run: .\scripts\create-release.ps1
# Or with version: .\scripts\create-release.ps1 -Version 1.0.5

param(
    [string]$Version
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

Push-Location $projectRoot

try {
    Write-Host "InScope - Create Release" -ForegroundColor Cyan
    Write-Host ""

    # Get version if not provided
    if (-not $Version) {
        $Version = Read-Host "Enter version number (e.g. 1.0.5)"
    }
    $Version = $Version.Trim()
    if (-not $Version) {
        Write-Host "No version entered. Exiting." -ForegroundColor Red
        exit 1
    }

    # Normalize version: strip leading 'v' and any stray dots
    $Version = $Version.Trim().TrimStart("v").TrimStart(".")
    if (-not $Version) {
        Write-Host "Invalid version. Use e.g. 1.0.4 or v1.0.4" -ForegroundColor Red
        exit 1
    }
    $tag = "v$Version"

    # Check for uncommitted changes
    $status = git status --porcelain 2>$null
    if ($status) {
        Write-Host "You have uncommitted changes:" -ForegroundColor Yellow
        git status --short
        $reply = Read-Host "Continue anyway? (y/N)"
        if ($reply -notmatch '^[yY]') {
            Write-Host "Exiting. Commit and push your changes first, then run again." -ForegroundColor Yellow
            exit 1
        }
    }

    # Ensure on main and up to date
    Write-Host "Switching to main and pulling latest..."
    $currentBranch = (git branch --show-current 2>$null)
    if ($currentBranch -ne "main") {
        git checkout main
    }
    git pull origin main

    # Check if tag already exists
    $existing = git tag -l $tag 2>$null
    if ($existing) {
        Write-Host "Tag $tag already exists." -ForegroundColor Red
        $reply = Read-Host "Delete and recreate? (y/N)"
        if ($reply -match '^[yY]') {
            git tag -d $tag 2>$null
            git push origin ":refs/tags/$tag" 2>$null
        } else {
            Write-Host "Exiting. Use a different version number." -ForegroundColor Yellow
            exit 1
        }
    }

    # Create and push tag
    Write-Host ""
    Write-Host "Creating tag $tag and pushing to GitHub..."
    git tag $tag
    git push origin $tag

    Write-Host ""
    Write-Host "Done! Release workflow has been triggered." -ForegroundColor Green
    Write-Host "  - Actions: https://github.com/Thomas-TNT/InScope/actions"
    Write-Host "  - Releases: https://github.com/Thomas-TNT/InScope/releases"
    Write-Host ""
    Write-Host "Wait 3-5 minutes for the build. InScope-Setup.exe will appear under the new release."
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}

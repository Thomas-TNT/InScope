# How to Create a Release (with InScope-Setup.exe)

This guide explains how to create a GitHub release that includes the **InScope-Setup.exe** installer. The installer is built and uploaded automatically when you push a version tag — **do not** create the release through the GitHub web UI, or the .exe will not be attached.

## Prerequisites

- Git installed
- Push access to the InScope repository
- Changes committed and pushed to `main` before creating the release

## Step-by-Step

### 1. Open a terminal

In VS Code/Cursor: **Terminal → New Terminal**, or open PowerShell/Command Prompt.

### 2. Navigate to the project

```powershell
cd C:\Users\tteru\OneDrive\Documents\GitHub\InScope
```

(Or the path where your InScope repo lives.)

### 3. Ensure you're on main and up to date

```powershell
git checkout main
git pull origin main
```

### 4. Commit and push any local changes (if needed)

If you have uncommitted work you want in the release:

```powershell
git add .
git commit -m "Your commit message"
git push origin main
```

### 5. Create the tag

Use the next version number (e.g. v1.0.4, v1.0.5, v1.1.0). The tag **must** start with `v`:

```powershell
git tag v1.0.4
```

### 6. Push the tag to GitHub

```powershell
git push origin v1.0.4
```

This triggers the Release workflow, which builds the app and installer and attaches InScope-Setup.exe to the release.

### 7. Verify

1. Go to **GitHub → Actions** — you should see the "Release" workflow running.
2. Wait for it to complete (typically 3–5 minutes).
3. Go to **Releases** — the new release should have **InScope-Setup.exe** under Assets.

## Quick Reference

**Easiest:** Double-click `scripts\create-release.bat` or run from terminal:

```powershell
.\scripts\create-release.ps1
```

It will prompt for the version, create the tag, and push. Or pass the version directly:

```powershell
.\scripts\create-release.ps1 -Version 1.0.4
```

**Manual:** When you're already on `main` with everything committed:

```powershell
git tag v1.0.4
git push origin v1.0.4
```

## Why This Works

The `.github/workflows/release.yml` workflow runs **only** when you push a tag that matches `v*`. Creating a release in the GitHub web UI does not trigger this workflow, so the .exe is never built or uploaded. Pushing the tag via git does.

The workflow injects the tag version (e.g. 1.0.4) into the assembly, so "Check for Updates" correctly recognizes when you already have the latest version installed.

## Troubleshooting

**"Tag already exists"**

- Pick a new version (e.g. v1.0.5), or
- Delete the tag locally: `git tag -d v1.0.4`
- Delete the tag on GitHub: `git push origin :refs/tags/v1.0.4`
- Then create and push again

**Release workflow fails**

- Check the **Actions** tab for the failed run.
- Common issues: merge conflicts in code, Inno Setup install path, or publish path.
- See `docs/BUILD_TROUBLESHOOTING.md` for build errors.

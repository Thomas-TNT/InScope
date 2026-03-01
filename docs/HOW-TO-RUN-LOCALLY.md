# How to Run InScope Locally

This guide explains how to run the InScope application on your machine for development or testing.

## Prerequisites

- **Windows 10/11** (x64)
- **.NET SDK 8.0** or later

Check that .NET is installed:

```powershell
dotnet --version
```

You should see `8.0.x` or higher. If not, install from [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0).

## Option 1: Quick Start (Recommended)

From the project root, run:

```powershell
.\scripts\run-readiness.ps1
```

This script checks prerequisites, restores packages, builds, and launches the app in one step.

## Option 2: Manual Steps

### 1. Open a terminal

In VS Code/Cursor: **Terminal → New Terminal**, or open PowerShell/Command Prompt.

### 2. Navigate to the project

```powershell
cd C:\Users\tteru\OneDrive\Documents\GitHub\InScope
```

(Use the actual path to your InScope folder.)

### 3. Restore and build

```powershell
dotnet restore
dotnet build -c Release
```

For faster iteration during development, use Debug instead:

```powershell
dotnet build -c Debug
```

### 4. Run the application

**Using dotnet run (builds and runs):**

```powershell
dotnet run -c Release
```

Or for Debug:

```powershell
dotnet run -c Debug
```

**Or run the executable directly:**

```powershell
.\bin\Release\net8.0-windows\InScope.exe
```

(Debug output: `.\bin\Debug\net8.0-windows\InScope.exe`)

## Development vs. Installed

When running locally, the status bar shows **v1.0.x (dev)** in the bottom right to indicate you are running from the development build, not an installed release.

## Content

The app loads content from the `Content\` folder next to the executable. During development, the project copies `Content\` to the output directory automatically. If you see "Content setup not found", ensure `Content\config.json` exists in the project.

## Summary

| Goal                     | Command                               |
|--------------------------|---------------------------------------|
| One-step run             | `.\scripts\run-readiness.ps1`         |
| Build and run (Release)  | `dotnet run -c Release`               |
| Build and run (Debug)    | `dotnet run -c Debug`                 |
| Run exe directly         | `.\bin\Release\net8.0-windows\InScope.exe` |

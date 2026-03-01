# InScope

A Windows WPF desktop application that builds procedure documents from standardized RTF instruction blocks. Guided Yes/No questions determine which blocks are appended; the assembled document can be edited in place and exported to PDF.

**For AI agents:** Read `docs/AGENT_CONTEXT.md` first (Section 0 = quick context). For CI build failures, see `docs/BUILD_TROUBLESHOOTING.md`.

## Prerequisites

- **Windows 10/11** (x64)
- **.NET SDK 8.0** or later

## Build

```bash
dotnet restore
dotnet build -c Release
```

## Run

```bash
dotnet run
```

Or run `bin\Release\net8.0-windows\InScope.exe` (or Debug) directly. The app looks for `Content\` next to the executable.

**Quick start:** Run `.\scripts\run-readiness.ps1` to verify prerequisites, build, and launch in one step.

## Publish (single self-contained .exe)

```bash
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\InScope.exe`

**Deployment:** Copy the entire `publish` folder (including `Content\`) to the target machine, or ensure `C:\ProgramData\InScope\` contains Blocks, BlockMetadata, and config.json.

See `docs/VALIDATION_CHECKLIST.md` for post-build verification steps.

## Content Setup

**Development:** The `Content\` folder in the project is copied to the output. It contains:

```
Content/
├── Blocks/          (.rtf files)
├── BlockMetadata/   (.json files)
└── config.json
```

**Production:** Place content at `C:\ProgramData\InScope\` or in a `Content\` folder next to the .exe. See `docs/config-schema.md` for config.json format.

## Project Structure

```
InScope/
├── Models/       ProcedureSession, BlockMetadata, AppConfig
├── Services/     BlockLoader, RuleEngine, DocumentAssembler, PdfExporter, ConfigLoader, FlowDocumentToPdfConverter
├── Content/      Sample blocks and config (copied to output)
├── docs/         ADRs, config schema, error handling, content lifecycle
├── App.xaml
├── MainWindow.xaml
└── InScope.csproj
```

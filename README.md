# InScope

A Windows WPF desktop application that builds procedure documents from standardized RTF instruction blocks. Guided Yes/No questions determine which blocks are appended; the assembled document can be edited in place and exported to PDF.

## Prerequisites

- **Windows 10/11** (x64)
- **.NET SDK 8.0** or later

## Build

```bash
dotnet restore
dotnet build -c Release
```

## Publish (single self-contained .exe)

```bash
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\InScope.exe`

## Content Setup

Place blocks and metadata at `C:\ProgramData\InScope\` (or configure `basePath` in `config.json`):

```
C:\ProgramData\InScope\
├── Blocks\          (.rtf files)
├── BlockMetadata\   (.json files)
└── config.json
```

For development, use a local `Content\` folder and set `basePath` accordingly.

## Project Structure

```
InScope/
├── Models/       ProcedureSession, BlockMetadata
├── Services/     BlockLoader, RuleEngine, DocumentAssembler, PdfExporter
├── App.xaml
├── MainWindow.xaml
└── InScope.csproj
```

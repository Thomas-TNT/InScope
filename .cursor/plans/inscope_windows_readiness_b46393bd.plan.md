---
name: InScope Windows Readiness
overview: Assessment of whether the InScope project is ready to build, run, and deploy on a Windows machine. The project is structurally complete with all known fixes applied; a few verification steps remain.
todos: []
isProject: false
---

# InScope Windows Readiness Assessment

## Summary

**Verdict: The project is ready to load and run on your Windows machine.** All prerequisites, content, and known build fixes are in place. You need only install .NET 8 (if not already installed) and run the build/run commands.

---

## Prerequisites Check


| Requirement       | Status                         |
| ----------------- | ------------------------------ |
| Windows 10/11 x64 | Assumed (your system)          |
| .NET SDK 8.0+     | Verify with `dotnet --version` |


**Quick check:** Open PowerShell and run `dotnet --version`. You should see `8.0.x` or higher. If not, install from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0).

---

## Project Readiness

### 1. Build Configuration

[InScope.csproj](InScope.csproj) targets `net8.0-windows` with WPF and QuestPDF. Content is copied to output:

```xml
<None Include="Content\**\*" CopyToOutputDirectory="PreserveNewest" />
```

### 2. Content Setup

Content is present and correctly structured:

```
Content/
├── config.json          # Procedure types, questions, basePath
├── Blocks/
│   ├── elec-000.rtf
│   ├── elec-001.rtf
│   └── elec-002.rtf
└── BlockMetadata/
    ├── elec-000.json
    ├── elec-001.json
    └── elec-002.json
```

[config.json](Content/config.json) has `basePath: "."`; [ConfigLoader](Services/ConfigLoader.cs) resolves it to the Content folder. [BlockLoader](Services/BlockLoader.cs) and [BlockMetadata](Content/BlockMetadata/) align with the Electrical sample.

### 3. Known Build Fixes (All Applied)


| Fix                                 | File                                                                    | Status  |
| ----------------------------------- | ----------------------------------------------------------------------- | ------- |
| Document alias (CS0103)             | [PdfExporter.cs](Services/PdfExporter.cs)                               | Applied |
| ValueKind + GetBoolean (CS1061)     | [RuleEngine.cs](Services/RuleEngine.cs)                                 | Applied |
| Func pattern (CS1977)               | [FlowDocumentToPdfConverter.cs](Services/FlowDocumentToPdfConverter.cs) | Applied |
| yield outside try/catch             | [BlockLoader.cs](Services/BlockLoader.cs)                               | Applied |
| XAML serialization (no Block.Clone) | [DocumentAssembler.cs](Services/DocumentAssembler.cs)                   | Applied |


### 4. Runtime Path Resolution

[ConfigLoader.GetContentBasePath](Services/ConfigLoader.cs) checks, in order:

1. `./Content` next to the executable (used in development)
2. `C:\ProgramData\InScope` (used in production)

With `dotnet run` or running the exe from the build output, `Content\` is placed next to the exe, so no extra setup is needed.

---

## Steps to Load and Run

### Development (build and run)

```powershell
cd c:\Users\tteru\OneDrive\Documents\GitHub\InScope
dotnet restore
dotnet build -c Release
dotnet run
```

Or run the exe directly:

```
bin\Release\net8.0-windows\InScope.exe
```

### Self-contained publish (for deployment to another machine)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\InScope.exe`

**Deployment:** Copy the entire `publish` folder (including `Content\`) to the target machine. No .NET runtime or Office required.

---

## Post-Run Validation

Use [docs/VALIDATION_CHECKLIST.md](docs/VALIDATION_CHECKLIST.md):

1. App launches without error
2. File → Start New → Electrical initializes session
3. Answer Yes to "Does this procedure involve high voltage?" — blocks appear
4. Edit document; edits persist
5. File → Export to PDF — PDF generates successfully

---

## Potential Gotchas

1. **First run:** If `Content\` is missing next to the exe, the app shows a setup warning. Ensure you run from the build output directory or use `dotnet run` (which uses the project output).
2. **Hydraulic/Mechanical:** Only Electrical sample content exists. Selecting Hydraulic or Mechanical will show questions but no blocks will be added until BlockMetadata and Blocks are added for those sections.
3. **Antivirus:** Self-contained single-file exe may be slow on first launch due to extraction; some antivirus software may scan it.

---

## Recommendation

Run the development build first:

```powershell
dotnet build -c Release && dotnet run
```

If that succeeds, the project is ready. Use the validation checklist to confirm end-to-end behavior.
# InScope — Agent Context Reference

This document provides context for AI agents (e.g. Cursor) working on the InScope codebase. It summarizes the project, implementation status, design decisions, known gotchas, and **required fix patterns** for CI builds. Read this before making changes.

**Critical:** See `docs/BUILD_TROUBLESHOOTING.md` for GitHub Actions build errors and exact fixes.

**Role-based agents:** For code work, adopt the Developer role from `Personal_notes/Personal_notes-main/role_based_agents/`. The project uses `.cursor/rules/developer-role.mdc` to enforce Developer responsibilities (implementation, design, review, handoff awareness).

---

## 0. Current State Summary (Quick Context)

| Area | Status |
|------|--------|
| **Content** | Electrical procedure. Hydraulic/Mechanical may have placeholder blocks (elec-000..004, hyd-*, mech-*). |
| **Questions** | Filtered by `sections` when configured; otherwise all shown. |
| **OnAnswer** | RebuildDocument() clears and re-inserts blocks when answers change (avoids duplication). |
| **Logging** | `AppLogger` to %LocalAppData%\InScope\Logs\inscope.log. Help → Open Log Folder. |
| **Version** | Shown in bottom-right status bar. `(dev)` suffix when running from bin\Debug or bin\Release. |
| **Production UI** | Blue accent bar at top + blue title bar (Win11) when not in dev. |
| **CI** | `build.yml` on push/PR to main. `release.yml` on push of v* tag → builds InScope-Setup.exe, creates release. |
| **Updates** | UpdateService checks GitHub Releases on startup (production) and via Help → Check for Updates. Download and run installer. |
| **Release script** | `scripts\create-release.ps1` / `.bat` — auto-suggests next version from tags, creates and pushes tag. |
| **QuestPDF** | Community license set in App.OnStartup. |

---

## 1. Project Overview

**InScope** is a Windows WPF desktop application that builds procedure documents from pre-authored RTF blocks. The user answers guided Yes/No questions; the RuleEngine decides which blocks to insert; blocks are appended to a live document; the user can edit in place; the final document is exported to PDF.

**Target users:** Operators or technicians assembling procedure documents in controlled or offline environments.

**Stack:** WPF, C#, .NET 8, QuestPDF. Distributed as a single self-contained `.exe` (no .NET runtime, Word, or cloud required on target machine).

---

## 2. Source Documents

The project was built from two spec documents (outside this repo):

- `Personal/Alexs_ideas/standalone software creation.md` — Build instructions and requirements
- `Personal/Alexs_ideas/InScope_Review.md` — Product Manager, Developer, and Architect review with gap analysis

A detailed build plan lives at `Personal_notes/.cursor/plans/inscope_build_plan_0043f7da.plan.md`.

---

## 3. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  WPF UI                                                      │
│  MainWindow: Menu, Questions (left), RichTextBox (right)     │
└───────────────────────┬─────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────┐
│  Services                                                    │
│  ConfigLoader, ContentPathResolver, BlockLoader, BlockChangeLog │
│  RuleEngine, DocumentAssembler, PdfExporter, FlowDocToPdf, UpdateService, AppLogger │
└───────────────────────┬─────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────┐
│  Content (effective path: primary or %LocalAppData%\InScope\Content)       │
│  Blocks/*.rtf, BlockMetadata/*.json, config.json — all editable in-app    │
└─────────────────────────────────────────────────────────────┘
```

**Flow:** User selects procedure type → questions appear → user answers Yes/No → RuleEngine evaluates BlockMetadata conditions → DocumentAssembler appends matching RTF blocks to FlowDocument → user edits → PdfExporter converts FlowDocument to PDF via QuestPDF.

---

## 4. Implementation Status (Phases Completed)

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 0 | ADRs, config schema, error-handling docs | Done |
| Phase 1 | RTF spike, FlowDocument→QuestPDF converter, ADR 002 | Done |
| Phase 1.5 | GitHub repo, .gitignore, GitHub Actions build | Done |
| Phase 2 | Project scaffold, Models, Services | Done |
| Phase 3 | Main window UI, menu, questions, RichTextBox | Done |
| Phase 4 | Sample content (Electrical procedure) | Done |
| Phase 5 | Publish script, validation checklist | Done |
| Phase 5.5 | AppLogger, Help → Open Log Folder, startup/PDF error logging | Done |
| Phase 6 | QuestPDF license (Community), Hydraulic/Mechanical placeholder content, run-readiness.ps1 | Done |
| Phase 7 | Block Library Editor (File → Edit Block Library); in-app RTF block editing with Save/Revert | Done |
| Phase 8 | Release workflow (v* tag → InScope-Setup.exe), UpdateService, Check for Updates, version display, dev indicator, production blue header, create-release script | Done |
| Phase 9 | Block Editor enhancements: Add/Delete blocks, ContentPathResolver (edit without admin), BlockChangeLog (14-day rolling backups) | Done |
| Phase 10 | Question Editor (File → Edit Questions); ConfigLoader.SaveConfig; add/edit/delete questions in-app | Done |

---

## 5. Key Files and Roles

### Models
- `Models/ProcedureSession.cs` — Current session: ProcedureType, Answers, InsertedBlockIds, InsertedBlocks (block refs for removal), Document (FlowDocument)
- `Models/BlockMetadata.cs` — Block metadata from JSON: BlockId, Section, Order, Conditions
- `Models/AppConfig.cs` — Config from config.json: procedureTypes, questions, basePath

### Services
- `Services/ContentPathResolver.cs` — Effective content path; when primary Blocks folder is read-only, uses %LocalAppData%\InScope\Content (copy from primary on first use)
- `Services/ConfigLoader.cs` — Loads config.json from given path; SaveConfig for persisting changes
- `Services/AppLogger.cs` — File-based logging to %LocalAppData%\InScope\Logs\inscope.log; OpenLogFolder()
- `Services/UpdateService.cs` — Checks GitHub Releases (Thomas-TNT/InScope); DownloadInstallerAsync; GetCurrentVersion
- `Services/BlockLoader.cs` — Loads RTF, SaveRtf, CreateBlock, DeleteBlock, EnumerateBlockIds, LoadMetadata, ReadRtfBytes
- `Services/BlockChangeLog.cs` — Logs block changes; backs up previous content; 14-day rolling retention for recovery
- `Services/RuleEngine.cs` — Evaluates Conditions (AND/OR) against Answers; returns ordered BlockIds
- `Services/DocumentAssembler.cs` — Appends blocks via XAML serialization (TextRange save/load); tracks InsertedBlockIds
- `Services/PdfExporter.cs` — Delegates to FlowDocumentToPdfConverter, generates PDF
- `Services/FlowDocumentToPdfConverter.cs` — Traverses FlowDocument blocks, rebuilds content in QuestPDF fluent API

### UI
- `MainWindow.xaml` — Menu (File → Start New, Export to PDF, Edit Questions, Edit Block Library, Exit; Help → Check for Updates, Open Log Folder), two-pane layout, status bar, production blue header bar
- `MainWindow.xaml.cs` — Wiring: Start New, answer handlers, RebuildDocument, RuleEngine/DocumentAssembler, PDF export, UpdateService, AppLogger; IsRunningFromDev, production title bar (DWM)
- `BlockEditorWindow.xaml` — Block Library Editor: Add/Delete blocks, list (grouped by section), RichTextBox, Save/Revert/Close. Change log on save.
- `AddBlockDialog.xaml` — Dialog for new block: BlockId, Section. Used by Block Editor Add button.
- `QuestionEditorWindow.xaml` — Question Editor: Add/Edit/Delete questions, list, Save/Close. File → Edit Questions.
- `QuestionDialog.xaml` — Dialog for add/edit question: Id, Text, Sections (procedure types).

### Scripts
- `scripts/create-release.ps1` — Creates and pushes v* tag; auto-suggests next version from remote tags
- `scripts/create-release.bat` — Double-click launcher for create-release.ps1
- `scripts/run-readiness.ps1` — Verify .NET, restore, build, launch
- `scripts/publish.ps1` — Publish and optionally build installer locally

### Content
- `Content/config.json` — Procedure types, questions, basePath
- `Content/Blocks/*.rtf` — RTF blocks (Electrical: elec-000..004 + elec-loto; Hydraulic: hyd-000..004; Mechanical: mech-000..004)
- `Content/BlockMetadata/*.json` — BlockId, Section, Order, Conditions

### Docs
- `docs/AGENT_CONTEXT.md` — This file; start here for new agents
- `docs/BUILD_TROUBLESHOOTING.md` — CI build errors and exact fixes
- `docs/HOW-TO-RUN-LOCALLY.md` — Run app locally (dotnet run, scripts)
- `docs/HOW-TO-CREATE-RELEASE.md` — Create release with InScope-Setup.exe (push v* tag)
- `docs/HOW-TO-CODE-SIGN.md` — Code sign to fix SmartScreen warning
- `docs/adr/001-rule-engine-conditions.md` — Conditions format (AND, OR, JSON schema)
- `docs/adr/002-pdf-export-strategy.md` — FlowDocument→QuestPDF approach
- `docs/config-schema.md` — config.json schema
- `docs/error-handling.md` — Error-handling strategy
- `docs/content-lifecycle.md` — Technical writer workflow
- `docs/spikes/rtf-flowdocument.md` — RTF loading notes
- `docs/VALIDATION_CHECKLIST.md` — Post-build verification
- `docs/web-app-feasibility.md` — Web application port assessment

---

## 6. Design Decisions

### RuleEngine Conditions (ADR 001)
- **AND:** All top-level conditions must be true
- **OR:** Use nested array, e.g. `[["Q1","Q2"], true]` — any question matches
- **Format:** `[QuestionId, expectedBool]` or `[[QuestionId1, QuestionId2], expectedBool]`
- **Empty conditions:** Block is always included

### PDF Export (ADR 002)
- Option A chosen: Parse FlowDocument, rebuild in QuestPDF
- Handles: Paragraph, List, Section, BlockUIContainer (images), Table (simplified)
- Paragraph inline formatting (bold/italic) is flattened to plain text
- Images: BitmapSource → PNG bytes via PngBitmapEncoder

### Document Copy
- DocumentAssembler uses XAML serialization (TextRange.Save/Load with DataFormats.Xaml) to copy blocks between FlowDocuments — avoids block ownership issues

### Content Path
- ContentPathResolver.GetEffectiveContentPath(): uses `./Content` or `C:\ProgramData\InScope` when writable; when primary Blocks folder is read-only (e.g. non-admin), falls back to `%LocalAppData%\InScope\Content` (copy on first use) — enables edit without admin
- config.json’s `basePath` can override; if relative, resolved against config directory

### Dev vs Production
- **Dev:** Running from `bin\Debug` or `bin\Release` — version shows "(dev)", no blue header
- **Production:** Installed or from publish folder — blue accent bar at top, blue title bar on Win11 (DWM)

---

## 7. Build and Deploy

- **Build:** `dotnet build -c Release`
- **Run:** `dotnet run` or run exe from output directory; or `.\scripts\run-readiness.ps1` (checks .NET, restores, builds, launches)
- **Publish:** `scripts/publish.ps1` or `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true ...`
- **GitHub Actions:** `build.yml` on push/PR to main. `release.yml` on push of v* tag (e.g. v1.0.6) — builds app, Inno Setup installer, creates release with InScope-Setup.exe. Version injected from tag.
- **Create release:** Run `scripts\create-release.ps1` or double-click `scripts\create-release.bat`; auto-suggests next version from remote tags.

Content is copied to output via `InScope.csproj`:
```xml
<None Include="Content\**\*" CopyToOutputDirectory="PreserveNewest" />
```

---

## 8. Known Gotchas and Build Fixes

### QuestPDF — License Required
QuestPDF 2024.x requires explicit license configuration. Set in `App.xaml.cs` OnStartup:
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```
Community license applies to individuals, nonprofits, FOSS, and orgs under $1M revenue. See https://www.questpdf.com/license/.

### PdfExporter — Document Resolves (CS0103)
`Document` can conflict with `System.Windows.Documents`. Use an alias:
```csharp
using QuestPDF.Fluent;
using Document = QuestPDF.Fluent.Document;
```
Then call `Document.Create(container => ...)`.

### QuestPDF — Unit and Namespaces
- `Unit.Centimetre` requires `using QuestPDF.Infrastructure;` (in FlowDocumentToPdfConverter)
- FlowDocumentToPdfConverter must not use `dynamic` for the column parameter — lambdas fail with CS1977. **Use `Func<IContainer> getItem`** and pass `() => column.Item()` from the Column callback. Pass `getItem` to helper methods; call `getItem().Row(row => ...)` etc.

### FlowDocumentToPdfConverter — Correct Pattern
```csharp
page.Content().Column(column =>
{
    foreach (Block block in document.Blocks)
        AddBlock(block, () => column.Item(), 0);
});

private static void AddList(List list, Func<IContainer> getItem, int indentLevel)
{
    getItem().Row(row => { ... });  // getItem returns IContainer; no dynamic
}
```

### RuleEngine — JsonElement Boolean (CS1061)
**Do not use** `TryGetBoolean` — not available in all System.Text.Json versions. Use:
```csharp
var last = arr[^1];
if (last.ValueKind != JsonValueKind.True && last.ValueKind != JsonValueKind.False)
    return false;
var expected = last.GetBoolean();
```

### BlockLoader — yield in try/catch
Cannot `yield return` inside a try block with catch. Deserialize in try, yield outside:
```csharp
BlockMetadata? meta = null;
try { meta = JsonSerializer.Deserialize<BlockMetadata>(...); }
catch { }
if (meta != null && ...) yield return meta;
```

### DocumentAssembler
- WPF `Block` has no `Clone()` method. Use XAML serialization (TextRange.Save/Load with DataFormats.Xaml) to copy FlowDocument content.
- Block removal: When answers change from Yes to No, blocks whose conditions are no longer met are removed. ProcedureSession.InsertedBlocks tracks block references for removal.

---

## 9. Known Limitations (Not Yet Implemented)

- **Hydraulic/Mechanical content:** Placeholder blocks may exist; full content set may need expansion.
- **Code signing:** App is unsigned; SmartScreen shows "unrecognized app" warning. See `docs/HOW-TO-CODE-SIGN.md`.

---

## 10. Out of Scope (MVP)

- Automatic numbering
- Word/Office dependency
- Cloud services or database
- Versioning/approvals, PDF branding, installer (.msi)

---

## 11. Suggested Next Steps

1. Run validation checklist on Windows (`docs/VALIDATION_CHECKLIST.md`)
2. Extend FlowDocumentToPdfConverter to preserve bold/italic (traverse Paragraph Inlines)
4. Implement error handling per `docs/error-handling.md` (status bar messages, dialogs)
5. Consider ProcedureSession persistence for crash recovery
6. Optional: Code sign for SmartScreen (see `docs/HOW-TO-CODE-SIGN.md`)

---

## 12. Do Not (Common Mistakes)

- **Do not** omit QuestPDF license — set `QuestPDF.Settings.License = LicenseType.Community` in App.OnStartup or PDF export will throw
- **Do not** use `JsonElement.TryGetBoolean` — use ValueKind check + GetBoolean
- **Do not** use `dynamic` for QuestPDF column/row callbacks — use `Func<IContainer> getItem`
- **Do not** use `Block.Clone()` — use XAML serialization
- **Do not** put `yield return` inside try-with-catch — move yield outside
- **Do not** assume `Document` resolves with only `using QuestPDF.Fluent` — add alias if CS0103 occurs
- **Do not** use `Assembly.GetEntryAssembly()?.Location` for single-file apps — it returns empty; use `Environment.ProcessPath` or `AppContext.BaseDirectory` instead

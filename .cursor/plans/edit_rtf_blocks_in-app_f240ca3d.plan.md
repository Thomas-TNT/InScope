---
name: Edit RTF Blocks In-App
overview: Add a Block Library Editor to InScope so end users can browse, open, and edit the source RTF block files (Content/Blocks/*.rtf) from within the application, with changes saved back to disk.
todos:
  - id: blockloader-savertf
    content: Add SaveRtf and EnumerateBlockIds to BlockLoader
    status: completed
  - id: block-editor-window
    content: Create BlockEditorWindow.xaml with list, RichTextBox, Save/Revert/Close
    status: completed
  - id: mainwindow-integration
    content: Add Edit Block Library menu item and wire to BlockEditorWindow
    status: completed
  - id: write-access-handling
    content: Handle read-only content folder with clear status/error messages
    status: completed
  - id: update-docs
    content: Update content-lifecycle.md and AGENT_CONTEXT.md
    status: completed
isProject: false
---

# Edit RTF Blocks From Application

## Context

InScope currently treats content as **read-only** at runtime. RTF blocks live in `Content/Blocks/*.rtf` and are authored externally (Word, WordPad). The assembled procedure document is editable in the main RichTextBox, but the **source block templates** cannot be modified from the app.

**Goal:** Let end users edit the source RTF block files from within InScope.

---

## Technical Approach

### RTF Load/Save

- **Load:** Already implemented in [BlockLoader.cs](InScope/Services/BlockLoader.cs) via `TextRange.Load(stream, DataFormats.Rtf)`
- **Save:** WPF supports `TextRange.Save(stream, DataFormats.Rtf)` — standard procedure blocks (paragraphs, bullets, bold/italic) round-trip correctly. Some advanced RTF features may not preserve perfectly; acceptable for MVP.

### Content Path and Write Permissions

- Content base path: `./Content` (relative to exe) or `C:\ProgramData\InScope` (from [ConfigLoader.cs](InScope/Services/ConfigLoader.cs))
- `./Content` (portable install) is typically user-writable
- `C:\ProgramData\InScope` may require admin to write — handle read-only gracefully with a clear status message

---

## Implementation Plan

### 1. BlockLoader: Add SaveRtf

Add a `SaveRtf(string blockId, FlowDocument document)` method that:

- Opens the RTF file with `FileMode.Create`, `FileAccess.Write`, `FileShare.None`
- Uses `TextRange(document.ContentStart, document.ContentEnd).Save(stream, DataFormats.Rtf)`
- Returns success/failure; on `IOException` (e.g. read-only or file in use), return false and let UI show an error

Optionally add `GetBlocksPath()` and `EnumerateBlockIds()` to list available blocks without loading metadata.

### 2. Block Editor Window

Create a new `BlockEditorWindow.xaml` (and `.xaml.cs`):

- **List/selector:** Left pane with blocks grouped by section (Electrical, Hydraulic, Mechanical), or a flat list with section as secondary label
  - Source: Enumerate `Blocks/*.rtf` by filename, optionally enrich with `BlockMetadata` for section labels
- **Editor:** Right pane with a `RichTextBox` bound to the selected block's `FlowDocument`
- **Toolbar/buttons:** Save, Revert (reload from file), Close
- **Status:** Show current block id, path, and any write-permission or save errors

Block selection flow:

1. User picks a block from the list
2. Load RTF via `BlockLoader.LoadRtf(blockId)` into a `FlowDocument`
3. Assign to `RichTextBox.Document`
4. On Save: call `BlockLoader.SaveRtf(blockId, DocumentEditor.Document)`, show success/error

### 3. Main Window Integration

Add a menu item under **File** (or a new **Content** menu):

- **Edit Block Library…** — opens `BlockEditorWindow` as a modeless or modal dialog
- Requires `_config` and `_blockLoader` to be initialized (same as Start New)

### 4. Write-Access Handling

- Before opening the editor, optionally check if Blocks folder is writable (e.g. attempt a test write)
- If Blocks folder is read-only: disable Save, show a status message (e.g. "Content folder is read-only. Run as administrator or use a writable content path.")
- On Save failure: catch `IOException` / `UnauthorizedAccessException`, show MessageBox with actionable message

### 5. Documentation

- Update [content-lifecycle.md](InScope/docs/content-lifecycle.md): add "Edit blocks from app" as an option alongside Word/WordPad
- Update [AGENT_CONTEXT.md](InScope/docs/AGENT_CONTEXT.md): remove "Block editing inside app" from "Out of Scope (MVP)" and add a brief note about the Block Library Editor

---

## Key Files to Modify/Create


| File                                                           | Action                                                |
| -------------------------------------------------------------- | ----------------------------------------------------- |
| [Services/BlockLoader.cs](InScope/Services/BlockLoader.cs)     | Add `SaveRtf`, optional `EnumerateBlockIds`           |
| `BlockEditorWindow.xaml`                                       | New: list + RichTextBox + Save/Revert/Close           |
| `BlockEditorWindow.xaml.cs`                                    | New: load/save logic, block selection, error handling |
| [MainWindow.xaml](InScope/MainWindow.xaml)                     | Add menu item "Edit Block Library…"                   |
| [MainWindow.xaml.cs](InScope/MainWindow.xaml.cs)               | Wire menu click to open `BlockEditorWindow`           |
| [InScope.csproj](InScope/InScope.csproj)                       | Ensure `BlockEditorWindow` XAML is included           |
| [docs/content-lifecycle.md](InScope/docs/content-lifecycle.md) | Document in-app block editing                         |
| [docs/AGENT_CONTEXT.md](InScope/docs/AGENT_CONTEXT.md)         | Update "Out of Scope" and implementation status       |


---

## UI Sketch

```
+----------------------------------------------------------+
| Block Library Editor                               [X]   |
+------------------+---------------------------------------+
| Blocks           |  [elec-001.rtf]                       |
|                  |                                       |
| Electrical       |  +----------------------------------+ |
|  elec-000        |  | [Rich text - paragraphs, bullets]| |
|  elec-001 *      |  |                                  | |
|  elec-002        |  |                                  | |
|  ...             |  |                                  | |
| Hydraulic        |  +----------------------------------+ |
|  hyd-000         |                                       |
|  ...             |  [Save]  [Revert]                     |
| Mechanical       |                                       |
|  mech-000        |  Status: Ready / Saved / Read-only     |
+------------------+---------------------------------------+
```

---

## Alternative: Open in External Editor

If a full in-app editor is too heavy, a lighter option is **"Open Block in Editor"** — open the `.rtf` file with the system default (e.g. WordPad) via `Process.Start`. Pros: no custom editor, full RTF support. Cons: external app, user must remember to save; no integrated workflow. This plan assumes the **in-app editor** path for a seamless experience.

---

## Risks and Mitigations

- **WPF RTF save limitations:** Some advanced formatting may change on round-trip. Mitigation: test with real procedure blocks; document that complex formatting (e.g. from Word) may simplify.
- **File locking:** If Word/WordPad has the file open, save may fail. Mitigation: clear error message and optional "retry" or "save as" to a different path.
- **Concurrent edits:** If user edits a block and then does "Start New" in the main window, the new session will use the updated block on next RebuildDocument. No extra handling needed.


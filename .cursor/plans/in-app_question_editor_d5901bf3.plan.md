---
name: In-App Question Editor
overview: Add an in-app Question Editor so end users can add, edit, and delete questions from config.json without manually editing the file. Mirrors the Block Library Editor pattern.
todos: []
isProject: false
---

# In-App Question Editor

## Goal

Let end users add, edit, and delete questions from within the application. Questions drive block selection via BlockMetadata Conditions and are filtered by section (Electrical, Hydraulic, Mechanical).

---

## Current State

- Questions live in `Content/config.json` under the `questions` array
- Each question: `id`, `text`, `type` (boolean), optional `sections` (procedure types)
- [MainWindow.xaml.cs](InScope/MainWindow.xaml.cs) filters questions by `_session.ProcedureType` in `RenderQuestions()`
- Config is loaded at startup; there is no save path today

---

## Implementation

### 1. ConfigLoader: Add SaveConfig

Add to [ConfigLoader.cs](InScope/Services/ConfigLoader.cs):

```csharp
public static bool SaveConfig(string basePath, AppConfig config)
```

- Serialize `config` to JSON (pretty-printed for readability)
- Write to `Path.Combine(basePath, "config.json")`
- Return true on success, false on IOException/UnauthorizedAccessException
- Preserve `config.BasePath` as stored (or re-resolve when loading; current Load normalizes it)

### 2. Config Writable Check

Reuse `ContentPathResolver.GetEffectiveContentPath()` — the content path is already chosen for writability. Add `ConfigLoader.IsConfigWritable(basePath)` that attempts a test write to config.json path, or simply try SaveConfig and handle failure. Simpler: just attempt save and show error on failure.

### 3. Question Editor Window

Create `QuestionEditorWindow.xaml` and `QuestionEditorWindow.xaml.cs`:

- **Layout:** List of questions (left), edit form (right) — or list with Add/Edit/Delete buttons and a dialog for add/edit
- **List:** ListBox/DataGrid with columns: Id, Text (truncated), Sections. Items from `AppConfig.Questions`
- **Buttons:** Add, Edit, Delete, Close
- **Add:** Opens dialog → Id, Text, Sections (multi-select: Electrical, Hydraulic, Mechanical)
- **Edit:** Same dialog pre-filled; Id editable but warn if changing (BlockMetadata may reference it)
- **Delete:** Confirm; optionally warn if any BlockMetadata references this question Id (can check `BlockLoader.LoadAllMetadata()` for Conditions containing the Id — non-trivial; MVP: simple confirm)
- **Save:** Call `ConfigLoader.SaveConfig(basePath, config)`, close or stay open with success message
- **Sections:** CheckBoxList or multi-select ComboBox for procedure types from `config.ProcedureTypes`; empty = "all sections"

### 4. Add/Edit Question Dialog

Create `QuestionDialog.xaml` (or `AddEditQuestionDialog.xaml`):

- **Id:** TextBox, alphanumeric + underscore/hyphen. Required. On Edit, pre-filled.
- **Text:** TextBox multiline. Required. Question text shown to user.
- **Sections:** ListBox with CheckBoxes or multi-select for Electrical, Hydraulic, Mechanical. If none selected, treat as "all sections" (null/empty).
- OK / Cancel
- Validation: Id unique on Add; Id not empty; Text not empty

### 5. Main Window Integration

- Add menu item **File → Edit Questions…** (or **Content → Edit Questions…**)
- Opens `QuestionEditorWindow` with `_config` and `_basePath` (effective content path)
- On close, if config was saved: reload `_config = ConfigLoader.Load(_basePath)`, call `RenderQuestions()` if `_session != null` to refresh the questions panel

### 6. QuestionEditorWindow Data Flow

- Receives `AppConfig config` and `string basePath` (or `contentPath`)
- Holds a working copy of config; on Save, writes to disk
- Add: append new `QuestionConfig` to working copy
- Edit: update existing in working copy
- Delete: remove from working copy
- Save: `ConfigLoader.SaveConfig(basePath, config)`

---

## Key Files


| File                                                         | Action                                        |
| ------------------------------------------------------------ | --------------------------------------------- |
| [Services/ConfigLoader.cs](InScope/Services/ConfigLoader.cs) | Add `SaveConfig(basePath, config)`            |
| `QuestionEditorWindow.xaml`                                  | New: question list, Add/Edit/Delete/Close     |
| `QuestionEditorWindow.xaml.cs`                               | New: list binding, add/edit/delete/save logic |
| `QuestionDialog.xaml`                                        | New: Id, Text, Sections (multi-select)        |
| `QuestionDialog.xaml.cs`                                     | New: validation, OK/Cancel                    |
| [MainWindow.xaml](InScope/MainWindow.xaml)                   | Add "Edit Questions…" menu item               |
| [MainWindow.xaml.cs](InScope/MainWindow.xaml.cs)             | Wire menu, reopen config + refresh on save    |


---

## Question Schema (Reminder)

```json
{
  "id": "HasHighVoltage",
  "text": "Does this procedure involve high voltage?",
  "type": "boolean",
  "sections": ["Electrical"]
}
```

- `sections`: optional; if null/empty, question shown for all procedure types

---

## Edge Cases

- **Id conflict:** On Add, ensure Id is unique. On Edit, allow Id change but warn about BlockMetadata.
- **Procedure types:** Sections dropdown uses `config.ProcedureTypes`. Do not allow editing procedure types in this MVP (that would require more extensive config changes).
- **Read-only config:** If save fails, show MessageBox with error; same pattern as Block Editor.


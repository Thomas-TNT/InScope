# Error Handling Strategy

## Principles

- User edits and assembled content are never lost due to errors.
- Failures are surfaced with clear, actionable messages.
- Non-fatal errors allow the user to continue; fatal errors exit gracefully.

## Error Scenarios

### Missing config.json

**Behavior:** Show message box "Content setup not found. Ensure config.json exists in the content path." and exit the application.

**Recovery:** User must create or restore config.json and relaunch.

---

### Block storage path missing (basePath invalid or empty)

**Behavior:** Show message box "Block storage path is invalid or not found." and exit (or warn on startup if config exists but path is wrong).

**Recovery:** User corrects basePath in config.json.

---

### Missing block (.rtf file not found)

**Behavior:**
- Log the missing BlockId.
- Show status bar message: "Block [BlockId] not found."
- Skip the block; continue assembling other blocks.
- Do not overwrite or remove existing document content.

**Recovery:** User or technical writer adds the missing .rtf file and retries (Start New or answers a question again if blocks are re-evaluated).

---

### Invalid metadata (BlockMetadata JSON malformed or missing)

**Behavior:**
- Log the file path and error.
- Skip the block; do not add it to the document.
- Notify in status bar: "Skipped invalid metadata: [filename]."
- Continue with other metadata files.

**Recovery:** Fix the JSON file and restart the procedure (Start New).

---

### PDF export failure

**Behavior:**
- Show dialog: "PDF export failed: [error message]"
- Document remains editable; user can retry or save differently if future features allow.
- Do not close the application.

**Recovery:** User checks disk space, path permissions, and retries. If QuestPDF throws, log full exception for debugging.

---

### RTF load failure (corrupt or unsupported format)

**Behavior:**
- Log the BlockId and exception.
- Status bar: "Could not load block [BlockId]."
- Skip the block; continue.

**Recovery:** Replace the .rtf file with valid content.

---

## Logging

- Log errors to a file in %LocalAppData%\InScope\Logs\ (or equivalent) when implemented.
- Include timestamp, error type, and context (BlockId, file path, exception message).
- For MVP, console or debug output is acceptable; file logging can be added later.

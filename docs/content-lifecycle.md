# Content Lifecycle

## Overview

InScope content (RTF blocks and BlockMetadata) can be edited from within the app. When the primary content path (e.g. `C:\ProgramData\InScope`) is read-only for the current user, the app automatically uses `%LocalAppData%\InScope\Content` so edits work without administrator privileges. Updates to procedure content do not require an app update.

**Change log:** Block edits are logged with 14-day rolling retention. Previous versions are backed up for recovery (see Recovery below).

## Ownership

**Technical Writer** owns:
- RTF blocks in `Blocks/`
- BlockMetadata JSON files in `BlockMetadata/`
- `config.json` (questions and procedure types)

## Authoring Workflow

1. **Create or edit RTF blocks** — use one of:
   - **In-app:** File → Edit Block Library. Use **Add** to create new blocks (BlockId + Section), **Delete** to remove blocks, or select a block to edit. Changes are logged and previous versions backed up for 14 days.
   - **External:** Word, WordPad, or another RTF editor. Include title paragraph, bullet lists, images as needed. Save as `.rtf` in `Blocks/` with a descriptive BlockId (e.g. `elec-001.rtf`).

2. **Create or edit BlockMetadata** — when adding blocks in-app, metadata is created automatically. For external edits:
   - Ensure `BlockId` matches the RTF filename (without extension).
   - Set `Section` to match a procedure type from `config.json`.
   - Set `Order` for insertion sequence.
   - Define `Conditions` per ADR 001.

3. **Update config.json** when adding questions.
   - Add new question with `id`, `text`, and `type`.
   - Use the question `id` in BlockMetadata Conditions.

## Review Steps

- [ ] BlockMetadata Conditions reference only existing question IDs from config.json.
- [ ] BlockId in metadata matches the RTF filename.
- [ ] RTF files render correctly (bullets, images) when loaded in the app.
- [ ] Section values match procedure types in config.json.

## Versioning

- No formal versioning for MVP. Replace files in place.
- For future: consider a version field in BlockMetadata or filename suffix (e.g. `elec-001-v2.rtf`).

## Edit Without Admin

When content is installed to `C:\ProgramData\InScope` (read-only for non-admin users), the app uses `%LocalAppData%\InScope\Content` for block editing. Content is copied from the primary path on first use. Both reading and writing use this user-specific path, so no administrator privileges are required.

## Recovery

Block changes are logged to `%LocalAppData%\InScope\BlockChangeLog.json`. Previous file versions are stored in `BlockBackups\` with names like `{blockId}_{yyyyMMdd_HHmmss}.rtf`. Entries and backups older than 14 days are automatically pruned. To recover a previous version, copy the desired backup file from `BlockBackups\` back to the content `Blocks\` folder, renaming it to match the block (e.g. `elec-001.rtf`).

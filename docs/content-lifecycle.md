# Content Lifecycle

## Overview

InScope content (RTF blocks and BlockMetadata) can be edited from within the app when the content folder is writable. Updates to procedure content do not require an app update.

## Ownership

**Technical Writer** owns:
- RTF blocks in `Blocks/`
- BlockMetadata JSON files in `BlockMetadata/`
- `config.json` (questions and procedure types)

## Authoring Workflow

1. **Create or edit RTF blocks** — use one of:
   - **In-app:** File → Edit Block Library to browse, edit, and save blocks directly.
   - **External:** Word, WordPad, or another RTF editor. Include title paragraph, bullet lists, images as needed. Save as `.rtf` in `Blocks/` with a descriptive BlockId (e.g. `elec-001.rtf`).

2. **Create or edit BlockMetadata** JSON for each block.
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

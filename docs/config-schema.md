# config.json Schema

## Overview

`config.json` defines procedure types, guided questions, and the content base path. It is loaded from the content directory at startup.

## Schema

```json
{
  "procedureTypes": ["Electrical", "Hydraulic", "Mechanical"],
  "questions": [
    {
      "id": "HasHighVoltage",
      "text": "Does this procedure involve high voltage?",
      "type": "boolean"
    }
  ],
  "basePath": "C:\\ProgramData\\InScope"
}
```

## Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `procedureTypes` | string[] | Yes | Procedure type names shown in File → Start New menu. Must match `Section` in BlockMetadata. |
| `questions` | object[] | Yes | Guided questions for the procedure flow. Each question has `id`, `text`, and `type`. |
| `basePath` | string | Yes | Root path for Blocks/ and BlockMetadata/ folders. Use `./Content` for dev; `C:\ProgramData\InScope` for production. |

## Question Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | Yes | Unique identifier. Used in BlockMetadata Conditions. |
| `text` | string | Yes | Question text displayed to the user. |
| `type` | string | Yes | Currently only `"boolean"` (Yes/No). |

## Example

```json
{
  "procedureTypes": ["Electrical", "Hydraulic", "Mechanical"],
  "questions": [
    { "id": "HasHighVoltage", "text": "Does this procedure involve high voltage?", "type": "boolean" },
    { "id": "UsesTransformer", "text": "Does it use a transformer?", "type": "boolean" },
    { "id": "UsesInverter", "text": "Does it use an inverter?", "type": "boolean" }
  ],
  "basePath": "C:\\ProgramData\\InScope"
}
```

## Defaults

- If `config.json` is missing, the app should fail with a clear setup error.
- If `basePath` is relative, resolve against the executable directory or current working directory (implementation-defined).

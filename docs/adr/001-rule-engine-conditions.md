# ADR 001: RuleEngine Conditions Format and Evaluation Logic

## Status
Accepted

## Context
BlockMetadata JSON files define which RTF blocks to insert based on user answers to guided questions. The Conditions format must support AND logic (all must match) and OR logic (any one matches).

## Decision

### Conditions Schema

```json
{
  "BlockId": "elec-001",
  "Section": "Electrical",
  "Order": 1,
  "Conditions": [
    ["HasHighVoltage", true],
    [["UsesTransformer", "UsesInverter"], true]
  ]
}
```

### Evaluation Rules

- **AND logic:** All items in the top-level `Conditions` array must evaluate to true for the block to be included.
- **OR logic:** Within a nested array (e.g. `[["Q1", "Q2"], true]`), any one question matching the expected value satisfies that condition.
- **Condition formats:**
  - `[QuestionId, expectedBool]` — Single question must equal expectedBool
  - `[[QuestionId1, QuestionId2, ...], expectedBool]` — Any of the questions must equal expectedBool
- **QuestionId:** Maps to the `id` field in `config.json` questions. Unknown questions are treated as false.
- **Empty conditions:** If `Conditions` is empty or missing, the block is always included (no filtering).

### Examples

| Conditions | Answers | Result |
|------------|---------|--------|
| `[["A", true]]` | A=true | Include |
| `[["A", true]]` | A=false | Exclude |
| `[["A", true], ["B", true]]` | A=true, B=true | Include |
| `[["A", true], ["B", true]]` | A=true, B=false | Exclude |
| `[["A", "B"], true]` | A=true, B=false | Include (A matches) |
| `[["A", "B"], true]` | A=false, B=false | Exclude |

## Consequences

- BlockMetadata JSON must use the exact format above.
- RuleEngine implementation must deserialize Conditions as flexible JSON (e.g. JsonElement) to handle nested arrays.
- Adding new question types (e.g. multi-choice) would require schema evolution.

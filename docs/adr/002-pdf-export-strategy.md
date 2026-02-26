# ADR 002: PDF Export Strategy

## Status
Accepted

## Context
QuestPDF does not consume WPF FlowDocument directly; it uses a fluent API. We need a viable path from FlowDocument (the live document in the RichTextBox) to PDF.

## Decision: Option A — Parse FlowDocument, Rebuild in QuestPDF

We implement a converter that traverses the FlowDocument block tree and rebuilds content using QuestPDF's fluent API.

### Implementation

- **FlowDocumentToPdfConverter:** Static helper that walks `document.Blocks` and adds corresponding QuestPDF elements.
- **Supported block types:**
  - **Paragraph:** Extract text via `TextRange(block.ContentStart, block.ContentEnd).Text`; add with `column.Item().Text(text)`.
  - **List:** Iterate `ListItems`, each containing `Blocks`; for `Paragraph` blocks, add bullet + text via `column.Item().Row(row => { row.ConstantItem(15).Text("\u2022"); row.RelativeItem().Text(text); })`.
  - **Section:** Recurse into `section.Blocks`.
  - **BlockUIContainer:** If `Child` is `System.Windows.Controls.Image`, encode `BitmapSource` to PNG via `PngBitmapEncoder`, add with `column.Item().Image(bytes)`.
  - **Table:** Extract text via TextRange; add as plain text (simplified for MVP).
- **Pagination:** QuestPDF's Column layout automatically flows content across pages when it overflows.
- **Styling:** Bullets, indentation (via `PaddingLeft`), and images are preserved. Bold/italic in Paragraph text are flattened to plain text for MVP.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| B. FlowDocument → XPS → PDF (PdfSharp.Xps) | Reuses WPF rendering | Package availability uncertain in 2024 |
| C. Dual format: RTF for display + JSON for PDF | QuestPDF renders from JSON | Duplicate content; authoring burden |
| D. Render FlowDocument to image per block | Simple | Quality, size, layout limitations |

## Consequences

- FlowDocument structure must remain traversable (Paragraph, List, Section, BlockUIContainer, Table).
- Nested lists beyond one level are supported via recursion.
- Inline formatting (bold, italic) in Paragraph is lost in PDF; only plain text is exported. Future: extend to handle `Run` and `Bold`/`Italic` inlines.
- Images must be `BitmapSource`; other `UIElement` types in BlockUIContainer are skipped.

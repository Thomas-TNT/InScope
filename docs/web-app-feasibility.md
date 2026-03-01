# InScope Web Application Feasibility

Assessment of whether InScope would function well as a web application. Based on analysis of the current WPF architecture.

---

## Summary

**Verdict:** The core workflow and business logic would port well to the web. The document pipeline (RTF → editable document → PDF) is heavily coupled to WPF and would require significant reimplementation. Overall: feasible with moderate effort, but not a straightforward port.

---

## What Would Work Well on the Web

| Component | Assessment |
|-----------|------------|
| **RuleEngine** | Pure C# logic; trivially portable to an ASP.NET Core API |
| **Models** (ProcedureSession, BlockMetadata, AppConfig) | No WPF dependency; fully portable |
| **Block selection logic** | Identical to desktop |
| **QuestPDF** | .NET-based; runs server-side and can stream generated PDFs as downloads |
| **Content structure** | JSON config and block metadata work as-is; block content would need a different format (see below) |
| **Session state** | Can live in server sessions, JWT, or client state |

---

## Major Challenges

### 1. WPF Document Stack

The app is built around WPF-specific types:

- `FlowDocument`, `RichTextBox`, `TextRange`
- `DataFormats.Rtf`, `DataFormats.Xaml`
- `Block`, `Paragraph`, `List`, `BlockUIContainer`, `Table`

None of these exist in the browser. A web app would need a different document representation (e.g., HTML/DOM or a library like Slate.js, TipTap, or Quill).

### 2. RTF as Source Format

`BlockLoader` uses WPF's built-in RTF loading:

```csharp
range.Load(stream, DataFormats.Rtf);
```

There's no direct web equivalent. Options:

- Convert RTF to HTML server-side (various C# RTF parsers exist)
- Author blocks as HTML or Markdown instead of RTF for the web version

### 3. XAML Serialization for Document Assembly

`DocumentAssembler` copies blocks via XAML serialization:

```csharp
range.Save(stream, DataFormats.Xaml);
insertPoint.Load(stream, DataFormats.Xaml);
```

That's WPF-only. Web would need a different assembly strategy (e.g., HTML concatenation, DOM manipulation, or a rich-text editor API).

### 4. PDF Generation Input

`FlowDocumentToPdfConverter` traverses WPF `FlowDocument` blocks and builds QuestPDF content. QuestPDF itself is fine on the server, but the *input* to the converter is WPF-specific. A web backend would need:

- A document model (e.g., HTML or a custom DTO) that represents the assembled procedure
- A converter from that model into QuestPDF's fluent API

### 5. Offline / Controlled Environments

The docs describe target users as operators in "controlled or offline environments." A web app assumes connectivity unless you add PWA/offline capabilities.

---

## Verdict

The **business logic** (questions, rules, block selection, session management) ports cleanly. The **document pipeline** (RTF → editable doc → PDF) is tightly coupled to WPF and would need to be reimplemented.

**Rough effort:**

- Backend API (config, questions, block metadata, PDF generation): moderate
- New document format and assembly: moderate
- Rich text editor and document model: significant
- RTF migration or conversion: moderate

**Architecture options:**

1. **Blazor WebAssembly** — Reuse C# and models; still need a non-WPF document format and rich-text editor.
2. **React/Vue + ASP.NET Core** — HTML-based rich editor (TipTap, Quill, etc.), backend services, QuestPDF for PDF.
3. **Store blocks as HTML** — Easiest for web; could add RTF→HTML conversion path if desktop continues to use RTF.

---

## References

- Current architecture: `docs/AGENT_CONTEXT.md`
- Document assembly: `Services/DocumentAssembler.cs`
- PDF conversion: `Services/FlowDocumentToPdfConverter.cs`
- Block loading: `Services/BlockLoader.cs`

# RTF + FlowDocument Spike

## Goal
Confirm RTF loading with images and nested bullets works in WPF.

## Approach

InScope uses `TextRange.Load(stream, DataFormats.Rtf)` to load .rtf files into a `FlowDocument`:

```csharp
var doc = new FlowDocument();
using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
var range = new TextRange(doc.ContentStart, doc.ContentEnd);
range.Load(stream, DataFormats.Rtf);
```

## Findings

### Works
- **Full WPF context:** Loading must occur within a running WPF application. RTF with embedded images can fail in console/headless contexts due to "Unrecognized structure in data format" when WPF rendering context is missing.
- **Standard RTF:** Title paragraphs, basic bullets, and embedded images load correctly in normal WPF app context.
- **File sharing:** Using `FileShare.Read` allows blocks to be read while other processes may have the file open.

### Potential Quirks
- **Nested bullets:** When pasting RTF with multi-level lists, block-level vs inline handling can affect layout. DocumentAssembler uses XAML serialization for copying between FlowDocuments, which may alter list structure. If issues arise, consider testing with real procedure content.
- **Images:** Embedded images in RTF require proper WPF context. InScope runs as a WPF app, so this should be satisfied.
- **Encoding:** RTF is typically ASCII with escapes; UTF-8 RTF should work. If extended characters fail, verify RTF encoding.

## Verification

To validate:
1. Create a sample .rtf with: title paragraph, multilevel bullet list, one embedded image.
2. Load via BlockLoader.LoadRtf() in the running app.
3. Append to the main document via DocumentAssembler.
4. Confirm bullets, indentation, and image render correctly in the RichTextBox.
5. Export to PDF and verify (Phase 1 PDF spike).

## Conclusion

The current approach is viable. DocumentAssembler uses XAML format for copying (not RTF) to avoid block ownership issues. If XAML copy loses inline images, consider preserving RTF round-trip for block copy or iterating blocks with XamlWriter/XamlReader per block.

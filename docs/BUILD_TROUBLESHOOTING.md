# Build Troubleshooting

This document records GitHub Actions CI build errors and their exact fixes. Use when CI fails or when refactoring services.

---

## Error 1: Document does not exist (CS0103)

**Error:**
```
Services/PdfExporter.cs(16,22): error CS0103: The name 'Document' does not exist in the current context
```

**Cause:** `Document` from QuestPDF may conflict with or not resolve when `System.Windows.Documents` is in scope.

**Fix in `Services/PdfExporter.cs`:**
```csharp
using System.Windows.Documents;
using QuestPDF.Fluent;
using Document = QuestPDF.Fluent.Document;

namespace InScope.Services;

public class PdfExporter
{
    public void Export(FlowDocument document, string outputPath)
    {
        var pdfDoc = Document.Create(container =>
        {
            FlowDocumentToPdfConverter.AddToDocument(document, container);
        });
        pdfDoc.GeneratePdf(outputPath);
    }
}
```

---

## Error 2: TryGetBoolean not found (CS1061)

**Error:**
```
Services/RuleEngine.cs(49,22): error CS1061: 'JsonElement' does not contain a definition for 'TryGetBoolean'
```

**Cause:** `TryGetBoolean` is not available in all System.Text.Json / .NET versions used by the build.

**Fix in `Services/RuleEngine.cs` — replace:**
```csharp
if (!arr[^1].TryGetBoolean(out var expected))
    return false;
```

**With:**
```csharp
var last = arr[^1];
if (last.ValueKind != JsonValueKind.True && last.ValueKind != JsonValueKind.False)
    return false;
var expected = last.GetBoolean();
```

---

## Error 3: Lambda with dynamic (CS1977)

**Error:**
```
Services/FlowDocumentToPdfConverter.cs(89,39): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
```

**Cause:** Using `dynamic column` and passing a lambda to `column.Item().Row(row => ...)` — the compiler cannot resolve the lambda when the receiver is dynamic.

**Fix in `Services/FlowDocumentToPdfConverter.cs`:**

1. Do **not** pass `column` or any `dynamic` to helper methods.
2. Pass `Func<IContainer> getItem` instead, where `getItem = () => column.Item()`.
3. In helpers, call `getItem().Row(row => ...)` — `getItem()` returns `IContainer`, so the lambda is statically typed.

**Correct pattern:**
```csharp
page.Content().Column(column =>
{
    column.Spacing(4);
    foreach (Block block in document.Blocks)
    {
        AddBlock(block, () => column.Item(), 0);
    }
});

private static void AddBlock(Block block, Func<IContainer> getItem, int indentLevel) { ... }
private static void AddList(List list, Func<IContainer> getItem, int indentLevel)
{
    getItem().Row(row =>
    {
        if (indent > 0) row.ConstantItem(indent * BulletIndent);
        row.ConstantItem(15).Text("\u2022");
        row.ConstantItem(5);
        row.RelativeItem().Text(text);
    });
}
```

---

## Error 4: yield in try/catch

**Error:**
```
Services/BlockLoader.cs: Cannot yield a value in the body of a try block with a catch clause
```

**Fix:** Deserialize in try, yield outside:
```csharp
foreach (var file in Directory.EnumerateFiles(metaPath, "*.json"))
{
    BlockMetadata? meta = null;
    try
    {
        var json = File.ReadAllText(file);
        meta = JsonSerializer.Deserialize<BlockMetadata>(json, options);
    }
    catch { }
    if (meta != null && string.Equals(meta.Section, section, ...))
        yield return meta;
}
```

---

## Error 5: Block.Clone does not exist

**Error:**
```
'Block' does not contain a definition for 'Clone'
```

**Fix:** Use XAML serialization in DocumentAssembler:
```csharp
private static void CopyFlowDocumentContent(FlowDocument source, FlowDocument target)
{
    var range = new TextRange(source.ContentStart, source.ContentEnd);
    using var stream = new MemoryStream();
    range.Save(stream, DataFormats.Xaml);
    var insertPoint = new TextRange(target.ContentEnd, target.ContentEnd);
    stream.Position = 0;
    insertPoint.Load(stream, DataFormats.Xaml);
}
```

---

## Error 6: MC3000 — Name cannot begin with '<' (Merge conflict markers)

**Error:**
```
MainWindow.xaml(28,2): error MC3000: 'Name cannot begin with the '<' character, hexadecimal value 0x3C. Line 28, position 2.' XML is not valid.
```

**Cause:** Unresolved Git merge conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`) left in XAML or other XML files. The `<` in `<<<<<<<` is invalid XML.

**Fix:** Remove all conflict markers from the file. Search for `<<<<<<`, `======`, `>>>>>>` and resolve conflicts. Keep only the desired content. For `MainWindow.xaml` Help menu, the correct result is:
```xml
<MenuItem Header="Help">
    <MenuItem Header="Check for Updates" Click="CheckForUpdates_Click"/>
    <MenuItem Header="Open Log Folder" Click="OpenLogFolder_Click"/>
</MenuItem>
```

**Prevention:** After merging branches, run `git diff` or search for conflict markers before committing. Ensure the branch that triggers CI (e.g. main) has clean files.

---

## Verifying Fixes

After applying fixes, ensure:
1. All changes are committed and pushed to the branch that triggers the PR
2. GitHub Actions runs on Windows with .NET 8
3. `dotnet restore` and `dotnet build -c Release --no-restore` both succeed

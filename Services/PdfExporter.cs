using System.Windows.Documents;
using QuestPDF.Fluent;
using Document = QuestPDF.Fluent.Document;

namespace InScope.Services;

/// <summary>
/// Exports the assembled FlowDocument to PDF.
/// </summary>
public class PdfExporter
{
    /// <summary>
    /// Export the FlowDocument to a PDF file at the given path.
    /// </summary>
    public void Export(FlowDocument document, string outputPath)
    {
        var pdfDoc = Document.Create(container =>
        {
            FlowDocumentToPdfConverter.AddToDocument(document, container);
        });

        pdfDoc.GeneratePdf(outputPath);
    }
}

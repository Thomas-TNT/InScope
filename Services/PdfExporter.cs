using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Windows.Documents;

namespace InScope.Services;

/// <summary>
/// Exports the assembled FlowDocument to PDF.
/// TODO: Implement FlowDocument → QuestPDF conversion (Phase 1 spike).
/// </summary>
public class PdfExporter
{
    /// <summary>
    /// Export the FlowDocument to a PDF file at the given path.
    /// </summary>
    public void Export(FlowDocument document, string outputPath)
    {
        // Placeholder: QuestPDF requires building from its fluent API.
        // Phase 1 spike will define the FlowDocument → QuestPDF mapping.
        var pdfDoc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                // TODO: Traverse FlowDocument blocks and add content (Phase 1 spike)
                page.Content().Column(column =>
                {
                    column.Item().Text("InScope Procedure Document")
                        .FontSize(16)
                        .Bold();
                    column.Item().PaddingTop(10).Text("(PDF export - FlowDocument conversion pending)");
                });
            });
        });

        pdfDoc.GeneratePdf(outputPath);
    }
}

using System.IO;
using System.Windows.Documents;
using InScope.Services;
using QuestPDF.Infrastructure;
using Xunit;

namespace InScope.Tests;

public class PdfExporterTests
{
    static PdfExporterTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Export_WhenValidDocument_CreatesPdfFile()
    {
        var doc = new FlowDocument();
        doc.Blocks.Add(new Paragraph(new Run("Test content for PDF export")));

        var tempPath = Path.Combine(Path.GetTempPath(), "inscope_pdf_test_" + Guid.NewGuid().ToString("N")[..8] + ".pdf");
        try
        {
            var exporter = new PdfExporter();
            exporter.Export(doc, tempPath);

            Assert.True(File.Exists(tempPath));
            var bytes = File.ReadAllBytes(tempPath);
            Assert.True(bytes.Length > 0);
            // PDF files start with %PDF
            var header = System.Text.Encoding.ASCII.GetString(bytes.AsSpan(0, 5));
            Assert.Equal("%PDF-", header);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void Export_WhenEmptyDocument_CreatesValidPdf()
    {
        var doc = new FlowDocument();

        var tempPath = Path.Combine(Path.GetTempPath(), "inscope_pdf_empty_" + Guid.NewGuid().ToString("N")[..8] + ".pdf");
        try
        {
            var exporter = new PdfExporter();
            exporter.Export(doc, tempPath);

            Assert.True(File.Exists(tempPath));
            var bytes = File.ReadAllBytes(tempPath);
            Assert.True(bytes.Length > 0);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}

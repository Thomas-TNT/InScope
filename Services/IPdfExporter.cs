using System.Windows.Documents;

namespace InScope.Services;

public interface IPdfExporter
{
    void Export(FlowDocument document, string outputPath);
}

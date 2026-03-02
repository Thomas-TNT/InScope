using System.IO;
using InScope;

namespace InScope.Tests;

public sealed class TempContentDir : IDisposable
{
    public string BasePath { get; }
    private readonly string _blocksPath;
    private readonly string _metaPath;

    public TempContentDir()
    {
        BasePath = Path.Combine(Path.GetTempPath(), "inscope_tests_" + Guid.NewGuid().ToString("N")[..8]);
        _blocksPath = Path.Combine(BasePath, Constants.BlocksFolder);
        _metaPath = Path.Combine(BasePath, Constants.BlockMetadataFolder);
        Directory.CreateDirectory(_blocksPath);
        Directory.CreateDirectory(_metaPath);
    }

    public void CreateRtf(string blockId)
    {
        var path = Path.Combine(_blocksPath, blockId + Constants.RtfExtension);
        // Use RTF with paragraph content so FlowDocument has Blocks when loaded
        File.WriteAllText(path, "{\\rtf1\\ansi Hello world.}");
    }

    public void CreateMetadata(string blockId, string section, int order)
    {
        var json = $$"""
            {"BlockId":"{{blockId}}","Section":"{{section}}","Order":{{order}},"Conditions":[]}
            """;
        var path = Path.Combine(_metaPath, blockId + ".json");
        File.WriteAllText(path, json);
    }

    public void Dispose() => Directory.Delete(BasePath, recursive: true);
}

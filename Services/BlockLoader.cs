using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
using InScope.Models;

namespace InScope.Services;

/// <summary>
/// Loads RTF blocks and BlockMetadata from the content directory.
/// </summary>
public class BlockLoader
{
    private readonly string _basePath;

    public BlockLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// Load RTF content as FlowDocument for the given BlockId.
    /// </summary>
    public FlowDocument? LoadRtf(string blockId)
    {
        var path = Path.Combine(_basePath, "Blocks", $"{blockId}.rtf");
        if (!File.Exists(path))
            return null;

        var doc = new FlowDocument();
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var range = new TextRange(doc.ContentStart, doc.ContentEnd);
        range.Load(stream, DataFormats.Rtf);
        return doc;
    }

    /// <summary>
    /// Load all BlockMetadata JSON files for the given section.
    /// </summary>
    public IEnumerable<BlockMetadata> LoadMetadata(string section)
    {
        var metaPath = Path.Combine(_basePath, "BlockMetadata");
        if (!Directory.Exists(metaPath))
            yield break;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        foreach (var file in Directory.EnumerateFiles(metaPath, "*.json"))
        {
            BlockMetadata? meta = null;
            try
            {
                var json = File.ReadAllText(file);
                meta = JsonSerializer.Deserialize<BlockMetadata>(json, options);
            }
            catch
            {
                // Skip invalid metadata files
            }

            if (meta != null && string.Equals(meta.Section, section, System.StringComparison.OrdinalIgnoreCase))
                yield return meta;
        }
    }
}

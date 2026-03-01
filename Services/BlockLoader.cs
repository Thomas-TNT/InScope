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
    /// Get the full path to the Blocks directory.
    /// </summary>
    public string GetBlocksPath() => Path.Combine(_basePath, "Blocks");

    /// <summary>
    /// Returns true if the Blocks directory exists and is writable.
    /// </summary>
    public bool IsBlocksWritable()
    {
        var blocksPath = GetBlocksPath();
        if (!Directory.Exists(blocksPath))
            return false;
        try
        {
            var testPath = Path.Combine(blocksPath, ".inscope_write_test");
            File.WriteAllText(testPath, "");
            File.Delete(testPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enumerate all BlockIds from .rtf files in the Blocks directory.
    /// </summary>
    public IEnumerable<string> EnumerateBlockIds()
    {
        var blocksPath = GetBlocksPath();
        if (!Directory.Exists(blocksPath))
            yield break;

        foreach (var file in Directory.EnumerateFiles(blocksPath, "*.rtf"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (!string.IsNullOrEmpty(name))
                yield return name;
        }
    }

    /// <summary>
    /// Save FlowDocument content to the RTF file for the given BlockId.
    /// Returns true on success, false on failure (e.g. read-only, file in use).
    /// </summary>
    public bool SaveRtf(string blockId, FlowDocument document)
    {
        var path = Path.Combine(_basePath, "Blocks", $"{blockId}.rtf");
        try
        {
            var range = new TextRange(document.ContentStart, document.ContentEnd);
            using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            range.Save(stream, DataFormats.Rtf);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Load all BlockMetadata JSON files (all sections). Used for block editor grouping.
    /// </summary>
    public IEnumerable<BlockMetadata> LoadAllMetadata()
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

            if (meta != null && !string.IsNullOrEmpty(meta.BlockId))
                yield return meta;
        }
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

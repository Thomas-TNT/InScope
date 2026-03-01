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
    /// Create a new block with empty RTF and metadata. Returns true on success.
    /// </summary>
    public bool CreateBlock(string blockId, string section)
    {
        if (string.IsNullOrWhiteSpace(blockId))
            return false;

        var blocksPath = GetBlocksPath();
        var metaPath = Path.Combine(_basePath, "BlockMetadata");
        Directory.CreateDirectory(blocksPath);
        Directory.CreateDirectory(metaPath);

        var rtfPath = Path.Combine(blocksPath, $"{blockId}.rtf");
        var metaFilePath = Path.Combine(metaPath, $"{blockId}.json");
        if (File.Exists(rtfPath) || File.Exists(metaFilePath))
            return false;

        try
        {
            var minimalRtf = "{\\rtf1\\ansi }";
            File.WriteAllText(rtfPath, minimalRtf);

            var maxOrder = LoadAllMetadata()
                .Where(m => string.Equals(m.Section, section, System.StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Order)
                .DefaultIfEmpty(-1)
                .Max();
            var order = maxOrder + 1;

            var meta = new BlockMetadata
            {
                BlockId = blockId,
                Section = section,
                Order = order,
                Conditions = new List<object>()
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(meta, options);
            File.WriteAllText(metaFilePath, json);
            return true;
        }
        catch
        {
            try { if (File.Exists(rtfPath)) File.Delete(rtfPath); } catch { }
            try { if (File.Exists(metaFilePath)) File.Delete(metaFilePath); } catch { }
            return false;
        }
    }

    /// <summary>
    /// Delete a block (RTF and metadata). Returns true on success.
    /// </summary>
    public bool DeleteBlock(string blockId)
    {
        if (string.IsNullOrWhiteSpace(blockId))
            return false;

        var rtfPath = Path.Combine(GetBlocksPath(), $"{blockId}.rtf");
        var metaPath = Path.Combine(_basePath, "BlockMetadata", $"{blockId}.json");
        try
        {
            if (File.Exists(rtfPath))
                File.Delete(rtfPath);
            if (File.Exists(metaPath))
                File.Delete(metaPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true if the block exists (has RTF file).
    /// </summary>
    public bool BlockExists(string blockId)
    {
        var path = Path.Combine(GetBlocksPath(), $"{blockId}.rtf");
        return File.Exists(path);
    }

    /// <summary>
    /// Read current RTF file content as bytes. Returns null if file does not exist.
    /// </summary>
    public byte[]? ReadRtfBytes(string blockId)
    {
        var path = Path.Combine(GetBlocksPath(), $"{blockId}.rtf");
        if (!File.Exists(path))
            return null;
        try
        {
            return File.ReadAllBytes(path);
        }
        catch
        {
            return null;
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

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
using InScope;
using InScope.Models;

namespace InScope.Services;

/// <summary>
/// Loads RTF blocks and BlockMetadata from the content directory.
/// </summary>
public class BlockLoader : IBlockLoader
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
        var path = Path.Combine(_basePath, Constants.BlocksFolder, $"{blockId}{Constants.RtfExtension}");
        if (!File.Exists(path))
            return null;

        try
        {
            var doc = new FlowDocument();
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var range = new TextRange(doc.ContentStart, doc.ContentEnd);
            range.Load(stream, DataFormats.Rtf);
            return doc;
        }
        catch (Exception ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "BlockLoader", "RTF load failed", new { blockId, path, message = ex.Message });
            return null;
        }
    }

    /// <summary>
    /// Get the full path to the Blocks directory.
    /// </summary>
    public string GetBlocksPath() => Path.Combine(_basePath, Constants.BlocksFolder);

    /// <summary>
    /// Returns true if the Blocks directory exists and is writable.
    /// </summary>
    public bool IsBlocksWritable() => ContentPathResolver.IsBlocksWritable(_basePath);

    /// <summary>
    /// Enumerate all BlockIds from .rtf files in the Blocks directory.
    /// </summary>
    public IEnumerable<string> EnumerateBlockIds()
    {
        var blocksPath = GetBlocksPath();
        if (!Directory.Exists(blocksPath))
            yield break;

        foreach (var file in Directory.EnumerateFiles(blocksPath, "*" + Constants.RtfExtension))
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
        var path = Path.Combine(_basePath, Constants.BlocksFolder, $"{blockId}{Constants.RtfExtension}");
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
    public bool CreateBlock(string blockId, string section) => CreateBlock(blockId, section, null);

    /// <summary>
    /// Create a new block with empty RTF and metadata. Returns true on success.
    /// When conditions is provided, the block is shown only when those conditions match user answers.
    /// </summary>
    public bool CreateBlock(string blockId, string section, IReadOnlyList<object>? conditions)
    {
        if (string.IsNullOrWhiteSpace(blockId))
            return false;

        var blocksPath = GetBlocksPath();
        var metaPath = Path.Combine(_basePath, Constants.BlockMetadataFolder);
        Directory.CreateDirectory(blocksPath);
        Directory.CreateDirectory(metaPath);

        var rtfPath = Path.Combine(blocksPath, $"{blockId}{Constants.RtfExtension}");
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
                Conditions = conditions != null ? new List<object>(conditions) : new List<object>()
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

        var rtfPath = Path.Combine(GetBlocksPath(), $"{blockId}{Constants.RtfExtension}");
        var metaPath = Path.Combine(_basePath, Constants.BlockMetadataFolder, $"{blockId}.json");
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
        var path = Path.Combine(GetBlocksPath(), $"{blockId}{Constants.RtfExtension}");
        return File.Exists(path);
    }

    /// <summary>
    /// Read current RTF file content as bytes. Returns null if file does not exist.
    /// </summary>
    public byte[]? ReadRtfBytes(string blockId)
    {
        var path = Path.Combine(GetBlocksPath(), $"{blockId}{Constants.RtfExtension}");
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
    public IEnumerable<BlockMetadata> LoadAllMetadata() => LoadMetadata(null);

    /// <summary>
    /// Load BlockMetadata JSON files, optionally filtered by section. Pass null for all sections.
    /// </summary>
    public IEnumerable<BlockMetadata> LoadMetadata(string? section)
    {
        var metaPath = Path.Combine(_basePath, Constants.BlockMetadataFolder);
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
            catch (Exception ex)
            {
                AppLogger.Log(AppLogger.LogLevel.Warning, "BlockLoader", "Invalid metadata file skipped", new { file, message = ex.Message });
            }

            if (meta == null || string.IsNullOrEmpty(meta.BlockId))
                continue;

            if (section == null || string.Equals(meta.Section, section, System.StringComparison.OrdinalIgnoreCase))
                yield return meta;
        }
    }
}

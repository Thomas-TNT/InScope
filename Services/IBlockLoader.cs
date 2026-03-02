using System.Collections.Generic;
using System.Windows.Documents;
using InScope.Models;

namespace InScope.Services;

public interface IBlockLoader
{
    FlowDocument? LoadRtf(string blockId);
    string GetBlocksPath();
    bool IsBlocksWritable();
    IEnumerable<string> EnumerateBlockIds();
    bool SaveRtf(string blockId, FlowDocument document);
    bool CreateBlock(string blockId, string section);
    bool CreateBlock(string blockId, string section, IReadOnlyList<object>? conditions);
    bool DeleteBlock(string blockId);
    bool BlockExists(string blockId);
    byte[]? ReadRtfBytes(string blockId);
    IEnumerable<BlockMetadata> LoadAllMetadata();
    IEnumerable<BlockMetadata> LoadMetadata(string? section);
}

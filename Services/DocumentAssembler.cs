using System.Collections.Generic;
using System.Windows.Documents;
using InScope.Models;

namespace InScope.Services;

/// <summary>
/// Appends blocks to the procedure document. Append-only; never modifies existing content.
/// Tracks InsertedBlockIds to prevent duplicates.
/// </summary>
public class DocumentAssembler
{
    private readonly BlockLoader _blockLoader;

    public DocumentAssembler(BlockLoader blockLoader)
    {
        _blockLoader = blockLoader;
    }

    /// <summary>
    /// Append blocks for the given BlockIds to the document. Skips already-inserted BlockIds.
    /// Deep-clones block content before insertion.
    /// </summary>
    public void AppendBlocks(
        FlowDocument targetDocument,
        HashSet<string> insertedBlockIds,
        IEnumerable<string> blockIdsToInsert)
    {
        foreach (var blockId in blockIdsToInsert)
        {
            if (insertedBlockIds.Contains(blockId))
                continue;

            var sourceDoc = _blockLoader.LoadRtf(blockId);
            if (sourceDoc == null)
                continue;

            var blocks = sourceDoc.Blocks;
            foreach (Block block in blocks)
            {
                var clone = CloneBlock(block);
                if (clone != null)
                    targetDocument.Blocks.Add(clone);
            }

            insertedBlockIds.Add(blockId);
        }
    }

    private static Block? CloneBlock(Block block)
    {
        return block.Clone() as Block;
    }
}

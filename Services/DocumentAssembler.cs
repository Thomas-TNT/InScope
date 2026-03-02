using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using InScope.Models;

namespace InScope.Services;

/// <summary>
/// Appends blocks to the procedure document. Tracks InsertedBlockIds to prevent duplicates.
/// When insertedBlocksMap is provided, records block references for later removal.
/// </summary>
public class DocumentAssembler : IDocumentAssembler
{
    private readonly IBlockLoader _blockLoader;

    public DocumentAssembler(IBlockLoader blockLoader)
    {
        _blockLoader = blockLoader;
    }

    /// <summary>
    /// Append blocks for the given BlockIds to the document. Skips already-inserted BlockIds.
    /// Deep-copies block content via XAML serialization before insertion.
    /// When insertedBlocksMap is non-null, records the Blocks added for each blockId (for removal support).
    /// </summary>
    public void AppendBlocks(
        FlowDocument targetDocument,
        HashSet<string> insertedBlockIds,
        IEnumerable<string> blockIdsToInsert,
        Dictionary<string, List<Block>>? insertedBlocksMap = null)
    {
        foreach (var blockId in blockIdsToInsert)
        {
            if (insertedBlockIds.Contains(blockId))
                continue;

            var sourceDoc = _blockLoader.LoadRtf(blockId);
            if (sourceDoc == null)
                continue;

            var countBefore = targetDocument.Blocks.Count;
            CopyFlowDocumentContent(sourceDoc, targetDocument);
            insertedBlockIds.Add(blockId);

            if (insertedBlocksMap != null)
            {
                var added = targetDocument.Blocks.Skip(countBefore).ToList();
                insertedBlocksMap[blockId] = added;
            }
        }
    }

    /// <summary>
    /// Copy content from source to target using XAML serialization (breaks block ownership).
    /// </summary>
    private static void CopyFlowDocumentContent(FlowDocument source, FlowDocument target)
    {
        var range = new TextRange(source.ContentStart, source.ContentEnd);
        using var stream = new MemoryStream();
        range.Save(stream, DataFormats.Xaml);

        var insertPoint = new TextRange(target.ContentEnd, target.ContentEnd);
        stream.Position = 0;
        insertPoint.Load(stream, DataFormats.Xaml);
    }
}

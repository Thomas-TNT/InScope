using System.Collections.Generic;
using System.IO;
using System.Windows;
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
    /// Deep-copies block content via XAML serialization before insertion.
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

            CopyFlowDocumentContent(sourceDoc, targetDocument);
            insertedBlockIds.Add(blockId);
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

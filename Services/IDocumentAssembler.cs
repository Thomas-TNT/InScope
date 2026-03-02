using System.Collections.Generic;
using System.Windows.Documents;

namespace InScope.Services;

public interface IDocumentAssembler
{
    void AppendBlocks(
        FlowDocument targetDocument,
        HashSet<string> insertedBlockIds,
        IEnumerable<string> blockIdsToInsert,
        Dictionary<string, List<Block>>? insertedBlocksMap = null);
}

using System.Collections.Generic;
using InScope.Models;

namespace InScope.Services;

public interface IRuleEngine
{
    IEnumerable<string> GetBlocksToInsert(
        IEnumerable<BlockMetadata> metadata,
        Dictionary<string, bool> answers);
}

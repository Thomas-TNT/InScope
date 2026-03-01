using System.Collections.Generic;
using System.Windows.Documents;

namespace InScope.Models;

/// <summary>
/// Represents the current procedure assembly session.
/// </summary>
public class ProcedureSession
{
    /// <summary>
    /// The procedure type: Electrical, Hydraulic, or Mechanical.
    /// </summary>
    public string ProcedureType { get; set; } = string.Empty;

    /// <summary>
    /// User answers to guided questions (QuestionId -> Yes/No).
    /// </summary>
    public Dictionary<string, bool> Answers { get; set; } = new();

    /// <summary>
    /// BlockIds that have already been inserted. Prevents duplicate insertion.
    /// </summary>
    public HashSet<string> InsertedBlockIds { get; set; } = new();

    /// <summary>
    /// BlockId to the document Blocks that were inserted for that block. Used for removal when answers change.
    /// </summary>
    public Dictionary<string, List<Block>> InsertedBlocks { get; set; } = new();

    /// <summary>
    /// The live document being assembled. User edits are preserved.
    /// </summary>
    public FlowDocument Document { get; set; } = new FlowDocument();
}

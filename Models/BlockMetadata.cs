using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InScope.Models;

/// <summary>
/// Metadata for an RTF block. Loaded from BlockMetadata/*.json.
/// </summary>
public class BlockMetadata
{
    /// <summary>
    /// Unique identifier; matches the .rtf filename (without extension).
    /// </summary>
    [JsonPropertyName("BlockId")]
    public string BlockId { get; set; } = string.Empty;

    /// <summary>
    /// Section/procedure type (e.g., Electrical, Hydraulic, Mechanical).
    /// </summary>
    [JsonPropertyName("Section")]
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Order for insertion when multiple blocks match.
    /// </summary>
    [JsonPropertyName("Order")]
    public int Order { get; set; }

    /// <summary>
    /// AND logic: all conditions must evaluate to true.
    /// OR logic: use nested arrays, e.g. [["Q1", "Q2"], true] means Q1 OR Q2.
    /// Format: [QuestionId, expectedBool] or [[QuestionId1, QuestionId2], expectedBool]
    /// </summary>
    [JsonPropertyName("Conditions")]
    public List<object> Conditions { get; set; } = new();
}

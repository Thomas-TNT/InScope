using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InScope.Models;

/// <summary>
/// Application configuration loaded from config.json.
/// </summary>
public class AppConfig
{
    [JsonPropertyName("procedureTypes")]
    public List<string> ProcedureTypes { get; set; } = new();

    [JsonPropertyName("questions")]
    public List<QuestionConfig> Questions { get; set; } = new();

    [JsonPropertyName("basePath")]
    public string BasePath { get; set; } = string.Empty;
}

/// <summary>
/// A guided question from config.json.
/// </summary>
public class QuestionConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "boolean";

    /// <summary>
    /// Optional. Procedure types this question applies to. If null/empty, shown for all.
    /// </summary>
    [JsonPropertyName("sections")]
    public List<string>? Sections { get; set; }
}

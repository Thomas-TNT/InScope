using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace InScope;

/// <summary>
/// Derives a valid Question ID from question text (e.g., "Does it use a pump?" -> "UsesPump").
/// </summary>
public static class QuestionIdHelper
{
    private static readonly HashSet<string> Stopwords = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "it", "does", "do", "is", "are", "this", "that",
        "to", "of", "in", "for", "with", "on", "at", "by", "from"
    };

    private static readonly Regex ValidIdRegex = new(@"^[a-zA-Z0-9_\-]+$");
    private static int _fallbackCounter;

    /// <summary>
    /// Derives a Question ID from question text. Result matches ^[a-zA-Z0-9_\-]+$.
    /// </summary>
    public static string DeriveFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return GetFallbackId();

        var words = Regex.Split(text.Trim(), @"\W+")
            .Where(w => !string.IsNullOrEmpty(w))
            .Where(w => !Stopwords.Contains(w))
            .Select(PascalCase)
            .ToList();

        if (words.Count == 0)
            return GetFallbackId();

        var result = string.Concat(words);
        if (string.IsNullOrEmpty(result) || !ValidIdRegex.IsMatch(result))
            return GetFallbackId();

        return result;
    }

    private static string PascalCase(string word)
    {
        if (string.IsNullOrEmpty(word)) return "";
        if (word.Length == 1) return char.ToUpperInvariant(word[0]).ToString();
        return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
    }

    private static string GetFallbackId()
    {
        _fallbackCounter++;
        return $"Question{_fallbackCounter}";
    }
}

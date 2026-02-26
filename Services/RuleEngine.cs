using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InScope.Models;

namespace InScope.Services;

/// <summary>
/// Evaluates block conditions against user answers. Returns ordered BlockIds to insert.
/// AND: all top-level conditions must be true. OR: nested arrays, any one satisfies.
/// </summary>
public class RuleEngine
{
    /// <summary>
    /// Given metadata and answers, return BlockIds whose conditions are satisfied, ordered by Order.
    /// </summary>
    public IEnumerable<string> GetBlocksToInsert(
        IEnumerable<BlockMetadata> metadata,
        Dictionary<string, bool> answers)
    {
        return metadata
            .Where(m => EvaluateConditions(m.Conditions, answers))
            .OrderBy(m => m.Order)
            .Select(m => m.BlockId);
    }

    private static bool EvaluateConditions(IEnumerable<object>? conditions, Dictionary<string, bool> answers)
    {
        if (conditions == null)
            return true;
        foreach (var cond in conditions)
        {
            if (cond is JsonElement je && !EvaluateCondition(je, answers))
                return false;
        }
        return true;
    }

    private static bool EvaluateCondition(JsonElement cond, Dictionary<string, bool> answers)
    {
        // Expect [QuestionId, expectedBool] or [[Q1, Q2], expectedBool]
        if (cond.ValueKind != JsonValueKind.Array)
            return false;

        var arr = cond.EnumerateArray().ToList();
        if (arr.Count < 2)
            return false;

        var last = arr[^1];
        if (last.ValueKind != JsonValueKind.True && last.ValueKind != JsonValueKind.False)
            return false;
        var expected = last.GetBoolean();
        var first = arr[0];

        if (first.ValueKind == JsonValueKind.String)
        {
            var qId = first.GetString();
            return answers.TryGetValue(qId ?? "", out var ans) && ans == expected;
        }

        if (first.ValueKind == JsonValueKind.Array)
        {
            // OR: any question matches
            foreach (var item in first.EnumerateArray())
            {
                var qId = item.GetString();
                if (answers.TryGetValue(qId ?? "", out var ans) && ans == expected)
                    return true;
            }
            return false;
        }

        return false;
    }
}

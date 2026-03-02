using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InScope.Models;
using InScope.Services;
using Xunit;

namespace InScope.Tests;

public class RuleEngineTests
{
    private static BlockMetadata Meta(string blockId, int order, params object[] conditionJsons)
    {
        var conditions = conditionJsons
            .Select(json => JsonSerializer.Deserialize<JsonElement>((string)json))
            .Cast<object>()
            .ToList();
        return new BlockMetadata { BlockId = blockId, Section = "Electrical", Order = order, Conditions = conditions };
    }

    [Fact]
    public void EmptyConditions_ReturnsBlock()
    {
        var engine = new RuleEngine();
        var metadata = new[] { new BlockMetadata { BlockId = "elec-000", Section = "Electrical", Order = 0, Conditions = new List<object>() } };
        var answers = new Dictionary<string, bool>();

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Single(result);
        Assert.Equal("elec-000", result[0]);
    }

    [Fact]
    public void NullConditions_ReturnsBlock()
    {
        var engine = new RuleEngine();
        var metadata = new[] { new BlockMetadata { BlockId = "elec-000", Section = "Electrical", Order = 0, Conditions = null! } };
        var answers = new Dictionary<string, bool>();

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Single(result);
        Assert.Equal("elec-000", result[0]);
    }

    [Fact]
    public void And_AllConditionsMatch_ReturnsBlock()
    {
        var engine = new RuleEngine();
        var metadata = new[]
        {
            Meta("elec-001", 0, "[\"Q1\", true]", "[\"Q2\", true]")
        };
        var answers = new Dictionary<string, bool> { ["Q1"] = true, ["Q2"] = true };

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Single(result);
        Assert.Equal("elec-001", result[0]);
    }

    [Fact]
    public void And_OneConditionFails_ExcludesBlock()
    {
        var engine = new RuleEngine();
        var metadata = new[]
        {
            Meta("elec-001", 0, "[\"Q1\", true]", "[\"Q2\", true]")
        };
        var answers = new Dictionary<string, bool> { ["Q1"] = true, ["Q2"] = false };

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void And_UnknownQuestionId_ExcludesBlock()
    {
        var engine = new RuleEngine();
        var metadata = new[]
        {
            Meta("elec-001", 0, "[\"UnknownQ\", true]")
        };
        var answers = new Dictionary<string, bool> { ["Q1"] = true };

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Or_AnyQuestionMatches_ReturnsBlock()
    {
        var engine = new RuleEngine();
        var metadata = new[]
        {
            Meta("elec-002", 0, "[[\"Q1\", \"Q2\"], true]")
        };
        var answers = new Dictionary<string, bool> { ["Q1"] = false, ["Q2"] = true };

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Single(result);
        Assert.Equal("elec-002", result[0]);
    }

    [Fact]
    public void Or_NoneMatch_ExcludesBlock()
    {
        var engine = new RuleEngine();
        var metadata = new[]
        {
            Meta("elec-002", 0, "[[\"Q1\", \"Q2\"], true]")
        };
        var answers = new Dictionary<string, bool> { ["Q1"] = false, ["Q2"] = false };

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Order_RespectsMetadataOrder()
    {
        var engine = new RuleEngine();
        var metadata = new[]
        {
            Meta("elec-003", 10, "[\"Q1\", true]"),
            Meta("elec-001", 0, "[\"Q1\", true]"),
            Meta("elec-002", 5, "[\"Q1\", true]")
        };
        var answers = new Dictionary<string, bool> { ["Q1"] = true };

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("elec-001", result[0]);
        Assert.Equal("elec-002", result[1]);
        Assert.Equal("elec-003", result[2]);
    }

    [Fact]
    public void ExpectedFalse_MatchesWhenAnswerIsFalse()
    {
        var engine = new RuleEngine();
        var metadata = new[]
        {
            Meta("elec-004", 0, "[\"Q1\", false]")
        };
        var answers = new Dictionary<string, bool> { ["Q1"] = false };

        var result = engine.GetBlocksToInsert(metadata, answers).ToList();

        Assert.Single(result);
        Assert.Equal("elec-004", result[0]);
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using InScope.Services;
using Xunit;

namespace InScope.Tests;

public class DocumentAssemblerTests
{
    [Fact]
    public void AppendBlocks_WhenBlockExists_AppendsContentToDocument()
    {
        using var temp = new TempContentDir();
        temp.CreateRtf("block-a");
        var loader = new BlockLoader(temp.BasePath);
        var assembler = new DocumentAssembler(loader);

        var doc = new FlowDocument();
        var insertedIds = new HashSet<string>();
        var insertedBlocks = new Dictionary<string, List<Block>>();

        assembler.AppendBlocks(doc, insertedIds, new[] { "block-a" }, insertedBlocks);

        Assert.Single(insertedIds);
        Assert.Contains("block-a", insertedIds);
        Assert.NotEmpty(doc.Blocks);
        Assert.Single(insertedBlocks);
        Assert.Contains("block-a", insertedBlocks.Keys);
    }

    [Fact]
    public void AppendBlocks_WhenBlockMissing_SkipsAndContinues()
    {
        using var temp = new TempContentDir();
        temp.CreateRtf("exists");
        var loader = new BlockLoader(temp.BasePath);
        var assembler = new DocumentAssembler(loader);

        var doc = new FlowDocument();
        var insertedIds = new HashSet<string>();

        assembler.AppendBlocks(doc, insertedIds, new[] { "exists", "missing" }, null);

        Assert.Single(insertedIds);
        Assert.Contains("exists", insertedIds);
    }

    [Fact]
    public void AppendBlocks_WhenBlockAlreadyInserted_SkipsDuplicate()
    {
        using var temp = new TempContentDir();
        temp.CreateRtf("block-a");
        var loader = new BlockLoader(temp.BasePath);
        var assembler = new DocumentAssembler(loader);

        var doc = new FlowDocument();
        var insertedIds = new HashSet<string> { "block-a" };

        assembler.AppendBlocks(doc, insertedIds, new[] { "block-a" }, null);

        Assert.Single(insertedIds);
        Assert.Empty(doc.Blocks);
    }
}

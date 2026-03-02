using System.IO;
using System.Linq;
using InScope;
using InScope.Services;
using Xunit;

namespace InScope.Tests;

public class BlockLoaderTests
{
    [Fact]
    public void EnumerateBlockIds_WhenEmpty_ReturnsEmpty()
    {
        using var temp = new TempContentDir();
        var loader = new BlockLoader(temp.BasePath);

        var ids = loader.EnumerateBlockIds().ToList();

        Assert.Empty(ids);
    }

    [Fact]
    public void EnumerateBlockIds_WhenRtfFilesExist_ReturnsIds()
    {
        using var temp = new TempContentDir();
        temp.CreateRtf("elec-001");
        temp.CreateRtf("elec-002");
        var loader = new BlockLoader(temp.BasePath);

        var ids = loader.EnumerateBlockIds().OrderBy(x => x).ToList();

        Assert.Equal(2, ids.Count);
        Assert.Equal("elec-001", ids[0]);
        Assert.Equal("elec-002", ids[1]);
    }

    [Fact]
    public void LoadMetadata_WhenEmpty_ReturnsEmpty()
    {
        using var temp = new TempContentDir();
        var loader = new BlockLoader(temp.BasePath);

        var meta = loader.LoadMetadata("Electrical").ToList();

        Assert.Empty(meta);
    }

    [Fact]
    public void LoadMetadata_WhenMatchingSection_ReturnsMetadata()
    {
        using var temp = new TempContentDir();
        temp.CreateRtf("elec-001");
        temp.CreateMetadata("elec-001", "Electrical", 0);
        var loader = new BlockLoader(temp.BasePath);

        var meta = loader.LoadMetadata("Electrical").ToList();

        Assert.Single(meta);
        Assert.Equal("elec-001", meta[0].BlockId);
        Assert.Equal("Electrical", meta[0].Section);
        Assert.Equal(0, meta[0].Order);
    }

    [Fact]
    public void LoadMetadata_WhenDifferentSection_ReturnsEmpty()
    {
        using var temp = new TempContentDir();
        temp.CreateRtf("elec-001");
        temp.CreateMetadata("elec-001", "Electrical", 0);
        var loader = new BlockLoader(temp.BasePath);

        var meta = loader.LoadMetadata("Hydraulic").ToList();

        Assert.Empty(meta);
    }

    [Fact]
    public void CreateBlock_WhenValid_CreatesRtfAndMetadata()
    {
        using var temp = new TempContentDir();
        var loader = new BlockLoader(temp.BasePath);

        var success = loader.CreateBlock("test-001", "Electrical");

        Assert.True(success);
        Assert.True(loader.BlockExists("test-001"));
        var meta = loader.LoadMetadata("Electrical").ToList();
        Assert.Single(meta);
        Assert.Equal("test-001", meta[0].BlockId);
    }

    [Fact]
    public void CreateBlock_WhenBlockExists_ReturnsFalse()
    {
        using var temp = new TempContentDir();
        temp.CreateRtf("existing");
        var loader = new BlockLoader(temp.BasePath);

        var success = loader.CreateBlock("existing", "Electrical");

        Assert.False(success);
    }

    [Fact]
    public void DeleteBlock_WhenExists_RemovesFiles()
    {
        using var temp = new TempContentDir();
        temp.CreateRtf("to-delete");
        temp.CreateMetadata("to-delete", "Electrical", 0);
        var loader = new BlockLoader(temp.BasePath);

        var success = loader.DeleteBlock("to-delete");

        Assert.True(success);
        Assert.False(loader.BlockExists("to-delete"));
    }
}

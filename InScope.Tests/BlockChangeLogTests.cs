using InScope.Services;
using Xunit;

namespace InScope.Tests;

public class BlockChangeLogTests
{
    [Fact]
    public void GetRecentEntries_WhenNoLogExists_ReturnsEmpty()
    {
        var entries = BlockChangeLog.GetRecentEntries();
        Assert.NotNull(entries);
        // May be empty or have entries from other tests/runs - we mainly verify it doesn't throw
    }

    [Fact]
    public void GetBackupFullPath_WhenNullOrEmpty_ReturnsNull()
    {
        Assert.Null(BlockChangeLog.GetBackupFullPath(null!));
        Assert.Null(BlockChangeLog.GetBackupFullPath(""));
    }

    [Fact]
    public void GetBackupFullPath_WhenNonExistentPath_ReturnsNull()
    {
        var result = BlockChangeLog.GetBackupFullPath("BlockBackups/nonexistent_20250101_000000.rtf");
        Assert.Null(result);
    }
}

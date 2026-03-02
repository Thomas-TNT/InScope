namespace InScope.Services;

/// <summary>
/// Adapter that implements IBlockChangeLog by delegating to BlockChangeLog static methods.
/// </summary>
public sealed class BlockChangeLogAdapter : IBlockChangeLog
{
    public void LogChange(string blockId, string action, byte[]? previousContent = null) =>
        BlockChangeLog.LogChange(blockId, action, previousContent);

    public IReadOnlyList<BlockChangeLog.ChangeEntry> GetRecentEntries() =>
        BlockChangeLog.GetRecentEntries();

    public string? GetBackupFullPath(string relativeBackupPath) =>
        BlockChangeLog.GetBackupFullPath(relativeBackupPath);
}

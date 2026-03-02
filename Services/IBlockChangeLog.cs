namespace InScope.Services;

public interface IBlockChangeLog
{
    void LogChange(string blockId, string action, byte[]? previousContent = null);
    IReadOnlyList<BlockChangeLog.ChangeEntry> GetRecentEntries();
    string? GetBackupFullPath(string relativeBackupPath);
}

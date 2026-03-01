using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace InScope.Services;

/// <summary>
/// Logs block changes and keeps backup copies for recovery.
/// Prunes entries and backups older than 14 days.
/// </summary>
public static class BlockChangeLog
{
    private static readonly object Lock = new();
    private static readonly TimeSpan RetentionDays = TimeSpan.FromDays(14);

    private static string BaseDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "InScope");
    private static string LogPath => Path.Combine(BaseDir, "BlockChangeLog.json");
    private static string BackupsDir => Path.Combine(BaseDir, "BlockBackups");

    public sealed record ChangeEntry(
        string Timestamp,
        string BlockId,
        string Action,
        string? BackupPath);

    /// <summary>
    /// Log a block change. For Modified, pass previous file content to create backup.
    /// Prunes entries older than 14 days after adding.
    /// </summary>
    public static void LogChange(string blockId, string action, byte[]? previousContent = null)
    {
        lock (Lock)
        {
            EnsureDirectories();

            var timestamp = DateTime.UtcNow;
            string? backupPath = null;

            if (action == "Modified" && previousContent != null && previousContent.Length > 0)
            {
                var fileName = $"{blockId}_{timestamp:yyyyMMdd_HHmmss}.rtf";
                backupPath = Path.Combine("BlockBackups", fileName);
                var fullBackupPath = Path.Combine(BaseDir, backupPath);
                File.WriteAllBytes(fullBackupPath, previousContent);
            }

            var entry = new ChangeEntry(
                timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                blockId,
                action,
                backupPath);

            var entries = LoadEntries();
            entries.Add(entry);
            PruneAndSave(entries);
        }
    }

    /// <summary>
    /// Get recent log entries (after pruning). For UI or recovery.
    /// </summary>
    public static IReadOnlyList<ChangeEntry> GetRecentEntries()
    {
        lock (Lock)
        {
            if (!File.Exists(LogPath))
                return Array.Empty<ChangeEntry>();
            var entries = LoadEntries();
            var cutoff = DateTime.UtcNow - RetentionDays;
            return entries.Where(e => DateTime.TryParse(e.Timestamp, out var dt) && dt >= cutoff).ToList();
        }
    }

    /// <summary>
    /// Get the full path to a backup file for recovery.
    /// </summary>
    public static string? GetBackupFullPath(string relativeBackupPath)
    {
        if (string.IsNullOrEmpty(relativeBackupPath))
            return null;
        var fullPath = Path.Combine(BaseDir, relativeBackupPath);
        return File.Exists(fullPath) ? fullPath : null;
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(BaseDir))
            Directory.CreateDirectory(BaseDir);
        if (!Directory.Exists(BackupsDir))
            Directory.CreateDirectory(BackupsDir);
    }

    private static List<ChangeEntry> LoadEntries()
    {
        if (!File.Exists(LogPath))
            return new List<ChangeEntry>();
        try
        {
            var json = File.ReadAllText(LogPath);
            var list = JsonSerializer.Deserialize<List<ChangeEntry>>(json);
            return list ?? new List<ChangeEntry>();
        }
        catch
        {
            return new List<ChangeEntry>();
        }
    }

    private static void PruneAndSave(List<ChangeEntry> entries)
    {
        var cutoff = DateTime.UtcNow - RetentionDays;

        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var e = entries[i];
            if (DateTime.TryParse(e.Timestamp, out var dt) && dt < cutoff)
            {
                if (!string.IsNullOrEmpty(e.BackupPath))
                {
                    var fullPath = Path.Combine(BaseDir, e.BackupPath);
                    try
                    {
                        if (File.Exists(fullPath))
                            File.Delete(fullPath);
                    }
                    catch { /* ignore */ }
                }
                entries.RemoveAt(i);
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = false };
        File.WriteAllText(LogPath, JsonSerializer.Serialize(entries, options));
    }
}

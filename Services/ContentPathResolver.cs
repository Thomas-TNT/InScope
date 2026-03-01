using System;
using System.IO;

namespace InScope.Services;

/// <summary>
/// Resolves the effective content path. When the primary path's Blocks folder is read-only
/// (e.g. ProgramData for non-admin users), falls back to %LocalAppData%\InScope\Content
/// and copies content from primary on first use.
/// </summary>
public static class ContentPathResolver
{
    private static string? _cachedEffectivePath;

    /// <summary>
    /// Get the primary content path (./Content or ProgramData). Does not apply writability fallback.
    /// </summary>
    public static string GetPrimaryContentPath()
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var localContent = Path.Combine(exeDir, "Content");
        var programData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "InScope");

        if (Directory.Exists(localContent) && File.Exists(Path.Combine(localContent, "config.json")))
            return localContent;
        if (Directory.Exists(programData) && File.Exists(Path.Combine(programData, "config.json")))
            return programData;

        return localContent;
    }

    /// <summary>
    /// Returns true if the Blocks folder at the given content path is writable.
    /// </summary>
    public static bool IsBlocksWritable(string contentPath)
    {
        var blocksPath = Path.Combine(contentPath, "Blocks");
        if (!Directory.Exists(blocksPath))
            return false;
        try
        {
            var testPath = Path.Combine(blocksPath, ".inscope_write_test");
            File.WriteAllText(testPath, "");
            File.Delete(testPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the path to the user's editable content folder (%LocalAppData%\InScope\Content).
    /// </summary>
    public static string GetUserContentPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InScope", "Content");
    }

    /// <summary>
    /// Get the effective content path. When primary is read-only, uses LocalAppData
    /// and copies from primary if needed. Result is cached for the session.
    /// </summary>
    public static string GetEffectiveContentPath()
    {
        if (_cachedEffectivePath != null)
            return _cachedEffectivePath;

        var primary = GetPrimaryContentPath();
        if (IsBlocksWritable(primary))
        {
            _cachedEffectivePath = primary;
            return primary;
        }

        var userPath = GetUserContentPath();
        EnsureUserContentFromPrimary(primary, userPath);
        _cachedEffectivePath = userPath;
        return userPath;
    }

    /// <summary>
    /// Reset cached path. Use when content path may have changed.
    /// </summary>
    public static void ResetCache()
    {
        _cachedEffectivePath = null;
    }

    private static void EnsureUserContentFromPrimary(string primaryPath, string userPath)
    {
        if (!Directory.Exists(userPath))
        {
            Directory.CreateDirectory(userPath);
        }

        var configPath = Path.Combine(userPath, "config.json");
        if (!File.Exists(configPath))
        {
            var srcConfig = Path.Combine(primaryPath, "config.json");
            if (File.Exists(srcConfig))
                File.Copy(srcConfig, configPath);
        }

        var blocksPath = Path.Combine(userPath, "Blocks");
        if (!Directory.Exists(blocksPath))
            Directory.CreateDirectory(blocksPath);

        var blocksSrc = Path.Combine(primaryPath, "Blocks");
        if (Directory.Exists(blocksSrc))
        {
            foreach (var file in Directory.EnumerateFiles(blocksSrc, "*.rtf"))
            {
                var destFile = Path.Combine(blocksPath, Path.GetFileName(file));
                if (!File.Exists(destFile))
                    File.Copy(file, destFile);
            }
        }

        var metaPath = Path.Combine(userPath, "BlockMetadata");
        if (!Directory.Exists(metaPath))
            Directory.CreateDirectory(metaPath);

        var metaSrc = Path.Combine(primaryPath, "BlockMetadata");
        if (Directory.Exists(metaSrc))
        {
            foreach (var file in Directory.EnumerateFiles(metaSrc, "*.json"))
            {
                var destFile = Path.Combine(metaPath, Path.GetFileName(file));
                if (!File.Exists(destFile))
                    File.Copy(file, destFile);
            }
        }
    }
}

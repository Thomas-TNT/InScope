using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InScope.Services;

/// <summary>
/// Checks GitHub Releases for updates. Opt-in only (Help → Check for Updates).
/// </summary>
public class UpdateService
{
    private const string RepoOwner = "tteru";
    private const string RepoName = "InScope";
    private const string ApiBase = "https://api.github.com";
    private const string InstallerAssetName = "InScope-Setup.exe";

    private static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "InScope-Update-Check" } }
    };

    /// <summary>
    /// Get the current app version from the entry assembly.
    /// </summary>
    public static string GetCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version == null) return "0.0.0";
        var build = version.Build >= 0 ? version.Build : 0;
        return $"{version.Major}.{version.Minor}.{build}";
    }

    /// <summary>
    /// Result of an update check.
    /// </summary>
    public sealed class UpdateCheckResult
    {
        public bool Success { get; init; }
        public UpdateInfo? Update { get; init; }
    }

    /// <summary>
    /// Fetch the latest release from GitHub and return info if a newer version exists.
    /// </summary>
    public static async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{ApiBase}/repos/{RepoOwner}/{RepoName}/releases/latest";
            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            var version = tagName.TrimStart('v');
            var current = GetCurrentVersion();

            if (!IsNewerVersion(version, current))
                return new UpdateCheckResult { Success = true, Update = null };

            var downloadUrl = GetDownloadUrl(root);
            if (string.IsNullOrEmpty(downloadUrl))
                return new UpdateCheckResult { Success = true, Update = null };

            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : null;
            var htmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;

            return new UpdateCheckResult { Success = true, Update = new UpdateInfo(version, downloadUrl, htmlUrl ?? "", body ?? "") };
        }
        catch
        {
            return new UpdateCheckResult { Success = false, Update = null };
        }
    }

    /// <summary>
    /// Compare semver. Returns true if latest is newer than current.
    /// </summary>
    public static bool IsNewerVersion(string latest, string current)
    {
        var l = ParseVersion(latest);
        var c = ParseVersion(current);
        if (l == null || c == null) return false;
        var (lMajor, lMinor, lPatch) = l.Value;
        var (cMajor, cMinor, cPatch) = c.Value;
        if (lMajor != cMajor) return lMajor > cMajor;
        if (lMinor != cMinor) return lMinor > cMinor;
        return lPatch > cPatch;
    }

    private static (int Major, int Minor, int Patch)? ParseVersion(string v)
    {
        var parts = v.Split('.');
        if (parts.Length < 3) return null;
        if (!int.TryParse(parts[0], out var major)) return null;
        if (!int.TryParse(parts[1], out var minor)) return null;
        if (!int.TryParse(parts[2], out var patch)) return null;
        return (major, minor, patch);
    }

    private static string? GetDownloadUrl(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name == InstallerAssetName && asset.TryGetProperty("browser_download_url", out var urlProp))
                return urlProp.GetString();
        }

        return null;
    }
}

/// <summary>
/// Info about an available update.
/// </summary>
public record UpdateInfo(string Version, string DownloadUrl, string ReleaseUrl, string ReleaseNotes);

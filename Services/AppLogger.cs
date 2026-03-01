using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace InScope.Services;

/// <summary>
/// File-based logger for diagnosing errors. Logs to %LocalAppData%\InScope\Logs\inscope.log
/// </summary>
public static class AppLogger
{
    private static readonly object Lock = new();
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "InScope", "Logs");
    private static readonly string LogPath = Path.Combine(LogDir, "inscope.log");

    public enum LogLevel { Info, Warning, Error }

    /// <summary>
    /// Get the full path to the log file.
    /// </summary>
    public static string GetLogPath() => LogPath;

    /// <summary>
    /// Open the Logs folder in Explorer.
    /// </summary>
    public static void OpenLogFolder()
    {
        try
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = LogDir,
                UseShellExecute = true
            });
        }
        catch (Exception) { /* ignore */ }
    }

    /// <summary>
    /// Log a message with optional structured data.
    /// </summary>
    public static void Log(LogLevel level, string category, string message, object? data = null)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var levelStr = level switch
            {
                LogLevel.Info => "INFO ",
                LogLevel.Warning => "WARN ",
                LogLevel.Error => "ERROR",
                _ => "INFO "
            };
            var dataJson = data != null ? " " + JsonSerializer.Serialize(data) : "";
            var line = $"[{timestamp}] [{levelStr}] [{category}] {message}{dataJson}\n";

            lock (Lock)
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);
                File.AppendAllText(LogPath, line);
            }
        }
        catch { /* never crash the app */ }
    }
}

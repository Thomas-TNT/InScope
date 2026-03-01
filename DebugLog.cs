using System.IO;
using System.Text.Json;

namespace InScope;

static class DebugLog
{
    private static readonly string Path = System.IO.Path.Combine(System.Environment.CurrentDirectory, "debug-d38425.log");

    public static void Log(string location, string message, object? data = null)
    {
        try
        {
            var entry = new
            {
                sessionId = "d38425",
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location,
                message,
                data
            };
            var line = JsonSerializer.Serialize(entry) + "\n";
            File.AppendAllText(Path, line);
        }
        catch { /* ignore */ }
    }
}

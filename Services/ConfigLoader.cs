using System;
using System.IO;
using System.Text.Json;
using InScope.Models;

namespace InScope.Services;

/// <summary>
/// Loads config.json from the content directory.
/// </summary>
public class ConfigLoader
{
    /// <summary>
    /// Get the content base path. Tries ./Content (relative to exe) then C:\ProgramData\InScope.
    /// </summary>
    public static string GetContentBasePath()
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
    /// Load config.json from the given base path.
    /// </summary>
    public static AppConfig? Load(string basePath)
    {
        var configPath = Path.Combine(basePath, "config.json");
        if (!File.Exists(configPath))
            return null;

        try
        {
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<AppConfig>(json, options);
            if (config != null && !string.IsNullOrEmpty(config.BasePath))
            {
                config.BasePath = Path.IsPathRooted(config.BasePath)
                    ? config.BasePath
                    : Path.Combine(basePath, config.BasePath);
            }
            else if (config != null)
            {
                config.BasePath = basePath;
            }
            return config;
        }
        catch
        {
            return null;
        }
    }
}

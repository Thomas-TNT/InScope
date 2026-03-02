using System;
using System.IO;
using System.Text.Json;
using InScope;
using InScope.Models;

namespace InScope.Services;

/// <summary>
/// Loads config.json from the content directory.
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// Load config.json from the given base path.
    /// </summary>
    public static AppConfig? Load(string basePath)
    {
        var configPath = Path.Combine(basePath, Constants.ConfigFileName);
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
        catch (JsonException ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "ConfigLoader", "Invalid config JSON", new { configPath, message = ex.Message });
            return null;
        }
        catch (IOException ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "ConfigLoader", "Config file read failed", new { configPath, message = ex.Message });
            return null;
        }
        catch (Exception ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "ConfigLoader", "Config load failed", new { configPath, message = ex.Message, type = ex.GetType().Name });
            return null;
        }
    }

    /// <summary>
    /// Save config to config.json. Returns true on success.
    /// </summary>
    public static bool SaveConfig(string basePath, AppConfig config)
    {
        var configPath = Path.Combine(basePath, Constants.ConfigFileName);
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, json);
            return true;
        }
        catch (IOException ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "ConfigLoader", "Config save failed (IO)", new { configPath, message = ex.Message });
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "ConfigLoader", "Config save failed (access denied)", new { configPath, message = ex.Message });
            return false;
        }
    }
}

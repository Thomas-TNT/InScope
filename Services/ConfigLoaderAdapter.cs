using InScope.Models;

namespace InScope.Services;

/// <summary>
/// Adapter that implements IConfigLoader by delegating to ConfigLoader static methods.
/// </summary>
public sealed class ConfigLoaderAdapter : IConfigLoader
{
    public AppConfig? Load(string basePath) => ConfigLoader.Load(basePath);
    public bool SaveConfig(string basePath, AppConfig config) => ConfigLoader.SaveConfig(basePath, config);
}

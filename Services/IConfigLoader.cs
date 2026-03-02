using InScope.Models;

namespace InScope.Services;

public interface IConfigLoader
{
    AppConfig? Load(string basePath);
    bool SaveConfig(string basePath, AppConfig config);
}

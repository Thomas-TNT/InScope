using System.IO;
using System.Text.Json;
using InScope.Models;
using InScope.Services;
using Xunit;

namespace InScope.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsNull()
    {
        using var temp = new TempDirectory();
        var result = ConfigLoader.Load(temp.Path);
        Assert.Null(result);
    }

    [Fact]
    public void Load_WhenValidConfig_ReturnsConfig()
    {
        using var temp = new TempDirectory();
        var configJson = """{"procedureTypes":["Electrical"],"questions":[],"basePath":""}""";
        File.WriteAllText(Path.Combine(temp.Path, "config.json"), configJson);

        var result = ConfigLoader.Load(temp.Path);

        Assert.NotNull(result);
        Assert.Single(result.ProcedureTypes);
        Assert.Equal("Electrical", result.ProcedureTypes[0]);
        Assert.Equal(Path.GetFullPath(temp.Path), Path.GetFullPath(result.BasePath));
    }

    [Fact]
    public void Load_WhenBasePathIsRelative_ResolvesAgainstConfigDir()
    {
        using var temp = new TempDirectory();
        var subDir = Path.Combine(temp.Path, "Content");
        Directory.CreateDirectory(subDir);
        var configJson = $$"""{"procedureTypes":[],"questions":[],"basePath":"Content"}""";
        File.WriteAllText(Path.Combine(temp.Path, "config.json"), configJson);

        var result = ConfigLoader.Load(temp.Path);

        Assert.NotNull(result);
        Assert.Equal(subDir, result.BasePath);
    }

    [Fact]
    public void Load_WhenInvalidJson_ReturnsNull()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "config.json"), "{ invalid json }");

        var result = ConfigLoader.Load(temp.Path);

        Assert.Null(result);
    }

    [Fact]
    public void SaveConfig_WhenValidConfig_WritesFile()
    {
        using var temp = new TempDirectory();
        var config = new AppConfig
        {
            ProcedureTypes = new() { "Electrical" },
            Questions = new(),
            BasePath = temp.Path
        };

        var success = ConfigLoader.SaveConfig(temp.Path, config);

        Assert.True(success);
        var path = Path.Combine(temp.Path, "config.json");
        Assert.True(File.Exists(path));
        var loaded = ConfigLoader.Load(temp.Path);
        Assert.NotNull(loaded);
        Assert.Single(loaded.ProcedureTypes);
    }
}

file sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "inscope_tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Path);
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

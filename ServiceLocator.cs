using Microsoft.Extensions.DependencyInjection;

namespace InScope;

/// <summary>
/// Simple service locator for resolving dependencies. Populated at application startup.
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider => _provider ?? throw new InvalidOperationException("ServiceLocator not initialized. Call Configure() first.");

    public static void Configure(IServiceProvider provider)
    {
        _provider = provider;
    }

    public static T GetRequiredService<T>() where T : notnull => Provider.GetRequiredService<T>();
}

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using InScope.Services;
using QuestPDF.Infrastructure;

namespace InScope;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        QuestPDF.Settings.License = LicenseType.Community;

        var services = new ServiceCollection();
        services.AddSingleton<IConfigLoader, ConfigLoaderAdapter>();
        services.AddSingleton<IUpdateService, UpdateServiceAdapter>();
        services.AddSingleton<IBlockChangeLog, BlockChangeLogAdapter>();
        services.AddTransient<IRuleEngine, RuleEngine>();
        services.AddTransient<IPdfExporter, PdfExporter>();
        // IBlockLoader and IDocumentAssembler are created per-session with basePath; registered when config is loaded
        ServiceLocator.Configure(services.BuildServiceProvider());
    }
}

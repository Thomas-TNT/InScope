using System.Windows;
using QuestPDF.Infrastructure;

namespace InScope;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        QuestPDF.Settings.License = LicenseType.Community;
    }
}

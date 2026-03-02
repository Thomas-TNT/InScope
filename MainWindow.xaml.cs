using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using Microsoft.Win32;
using InScope.Models;
using InScope.Services;
using InScope.ViewModels;

namespace InScope;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private IBlockLoader? _blockLoader;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        LoadConfiguration();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (IsRunningFromDev()) return;
        await PerformStartupUpdateCheckAsync();
    }

    private async Task PerformStartupUpdateCheckAsync()
    {
        try
        {
            var result = await ServiceLocator.GetRequiredService<IUpdateService>().CheckForUpdateAsync();
            if (!result.Success || result.Update == null) return;

            await TryDownloadAndInstallUpdateAsync(result.Update, s =>
            {
                if (_viewModel != null)
                    _viewModel.StatusText = s;
            });
        }
        catch (Exception ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "UpdateCheck", "Startup update check failed", new { message = ex.Message });
        }
    }

    private async Task TryDownloadAndInstallUpdateAsync(UpdateInfo update, Action<string> setStatus)
    {
        var message = $"Update v{update.Version} available.\n\nDownload and install now? The app will close when the installer starts.";
        var dialogResult = MessageBox.Show(message, "InScope - Update Available",
            MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (dialogResult != MessageBoxResult.Yes || string.IsNullOrEmpty(update.DownloadUrl))
            return;

        setStatus("Downloading update...");
        try
        {
            var installerPath = await ServiceLocator.GetRequiredService<IUpdateService>().DownloadInstallerAsync(update.DownloadUrl);
            setStatus("Launching installer...");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(installerPath) { UseShellExecute = true });
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Error, "UpdateCheck", "Download failed", new { message = ex.Message });
            MessageBox.Show($"Download failed: {ex.Message}\n\nYou can try Help → Check for Updates or the website instead.", "InScope - Update", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (!string.IsNullOrEmpty(update.ReleaseUrl))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true });
        }
        finally
        {
            setStatus("Ready.");
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (IsRunningFromDev()) return;
        ApplyProductionTitleBarColor();
    }

    private static void ApplyProductionTitleBarColor()
    {
        try
        {
            var w = Application.Current.MainWindow;
            if (w == null) return;
            var helper = new WindowInteropHelper(w);
            var hwnd = helper.EnsureHandle();
            if (hwnd == IntPtr.Zero) return;
            const int DWMWA_CAPTION_COLOR = 35;
            var color = 0xD47800; // BGR for #0078D4 blue
            var attr = new[] { (int)(color & 0xFFFFFF) };
            _ = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, attr, sizeof(int));
        }
        catch { /* Windows 10 or older - caption color not supported */ }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

    private static bool IsRunningFromDev()
    {
        try
        {
            var path = Environment.ProcessPath ?? AppContext.BaseDirectory ?? "";
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory ?? "";
            var sep = Path.DirectorySeparatorChar.ToString();
            return dir.Contains(sep + "bin" + sep + "Debug" + sep, StringComparison.OrdinalIgnoreCase)
                || dir.Contains(sep + "bin" + sep + "Release" + sep, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private void LoadConfiguration()
    {
        var basePath = ContentPathResolver.GetEffectiveContentPath();
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{(version.Build >= 0 ? version.Build : 0)}" : "?";
        AppLogger.Log(AppLogger.LogLevel.Info, "Startup", "InScope starting", new { version = versionStr, contentPath = basePath });

        var config = ServiceLocator.GetRequiredService<IConfigLoader>().Load(basePath);
        if (config == null)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "Startup", "Content setup not found");
            CreateViewModelWithoutConfig(basePath);
            return;
        }

        _blockLoader = new BlockLoader(config.BasePath);
        var ruleEngine = new RuleEngine();
        var documentAssembler = new DocumentAssembler(_blockLoader);
        var pdfExporter = new PdfExporter();

        _viewModel = new MainViewModel(
            ServiceLocator.GetRequiredService<IConfigLoader>(),
            ServiceLocator.GetRequiredService<IUpdateService>(),
            _blockLoader,
            ruleEngine,
            documentAssembler,
            pdfExporter,
            IsRunningFromDev,
            () => Application.Current.Shutdown());

        _viewModel.SetDocument = doc =>
        {
            DocumentEditor.Document = doc;
            DocumentEditor.IsDocumentEnabled = true;
        };
        _viewModel.GetDocument = () => DocumentEditor.Document;
        _viewModel.RequestOpenRecoveryWindow = () =>
        {
            var recoveryWindow = new RecoveryWindow { Owner = this };
            recoveryWindow.ShowDialog();
        };
        _viewModel.RequestEditQuestions = () =>
        {
            if (_viewModel == null) return;
            var configForEdit = ServiceLocator.GetRequiredService<IConfigLoader>().Load(basePath);
            if (configForEdit == null) return;
            var editor = new QuestionEditorWindow(configForEdit, basePath) { Owner = this };
            editor.ShowDialog();
            if (editor.WasSaved)
            {
                var reloaded = ServiceLocator.GetRequiredService<IConfigLoader>().Load(basePath);
                if (reloaded != null)
                    _viewModel.OnQuestionsConfigReloaded(reloaded);
            }
        };
        _viewModel.RequestEditBlockLibrary = () =>
        {
            if (_blockLoader == null) return;
            var configForEdit = ServiceLocator.GetRequiredService<IConfigLoader>().Load(basePath);
            var procedureTypes = configForEdit?.ProcedureTypes ?? new List<string>();
            var editor = new BlockEditorWindow(_blockLoader, procedureTypes) { Owner = this };
            editor.ShowDialog();
        };

        DataContext = _viewModel;
        _viewModel.LoadConfiguration(basePath);
    }

    private void CreateViewModelWithoutConfig(string basePath)
    {
        _blockLoader = new BlockLoader(basePath);
        var ruleEngine = new RuleEngine();
        var documentAssembler = new DocumentAssembler(_blockLoader);
        var pdfExporter = new PdfExporter();

        _viewModel = new MainViewModel(
            ServiceLocator.GetRequiredService<IConfigLoader>(),
            ServiceLocator.GetRequiredService<IUpdateService>(),
            _blockLoader,
            ruleEngine,
            documentAssembler,
            pdfExporter,
            IsRunningFromDev,
            () => Application.Current.Shutdown());

        _viewModel.SetDocument = doc =>
        {
            DocumentEditor.Document = doc;
            DocumentEditor.IsDocumentEnabled = true;
        };
        _viewModel.GetDocument = () => DocumentEditor.Document;
        _viewModel.RequestOpenRecoveryWindow = () =>
        {
            var recoveryWindow = new RecoveryWindow { Owner = this };
            recoveryWindow.ShowDialog();
        };
        _viewModel.RequestEditQuestions = () =>
        {
            MessageBox.Show("Content setup not found. Add config.json to the Content folder first.", "InScope", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        _viewModel.RequestEditBlockLibrary = () =>
        {
            if (_blockLoader == null) return;
            var editor = new BlockEditorWindow(_blockLoader, Constants.DefaultProcedureTypes) { Owner = this };
            editor.ShowDialog();
        };

        DataContext = _viewModel;
        _viewModel.LoadConfiguration(basePath);
            MessageBox.Show(
            "Content setup not found. Ensure config.json exists in the Content folder or C:\\ProgramData\\InScope.",
            "InScope - Setup Required",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}

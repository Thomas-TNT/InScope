using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using InScope.Models;
using InScope.Services;

namespace InScope.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IConfigLoader _configLoader;
    private readonly IUpdateService _updateService;
    private readonly IBlockLoader _blockLoader;
    private readonly IRuleEngine _ruleEngine;
    private readonly IDocumentAssembler _documentAssembler;
    private readonly IPdfExporter _pdfExporter;
    private readonly Func<bool> _isRunningFromDev;
    private readonly Action _exitApp;

    private ProcedureSession? _session;
    private AppConfig? _config;
    private string _basePath = string.Empty;

    public ObservableCollection<QuestionItemViewModel> Questions { get; } = new();

    [ObservableProperty]
    private string _questionsHeader = "Select a procedure type (File → Start New)";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _procedureTypeText = "—";

    [ObservableProperty]
    private string _blockCountText = "0 blocks";

    [ObservableProperty]
    private string _versionText = "v0.0.0";

    [ObservableProperty]
    private Visibility _productionHeaderVisibility = Visibility.Collapsed;

    /// <summary>
    /// Invoked when the document is replaced (e.g. on Start New).
    /// The callback receives the new FlowDocument to display.
    /// </summary>
    public Action<FlowDocument>? SetDocument { get; set; }

    /// <summary>
    /// Invoked to get the current document for export or rebuild.
    /// </summary>
    public Func<FlowDocument>? GetDocument { get; set; }

    public MainViewModel(
        IConfigLoader configLoader,
        IUpdateService updateService,
        IBlockLoader blockLoader,
        IRuleEngine ruleEngine,
        IDocumentAssembler documentAssembler,
        IPdfExporter pdfExporter,
        Func<bool> isRunningFromDev,
        Action exitApp)
    {
        _configLoader = configLoader;
        _updateService = updateService;
        _blockLoader = blockLoader;
        _ruleEngine = ruleEngine;
        _documentAssembler = documentAssembler;
        _pdfExporter = pdfExporter;
        _isRunningFromDev = isRunningFromDev;
        _exitApp = exitApp;
    }

    public void LoadConfiguration(string basePath)
    {
        _basePath = basePath;
        InitializeVersionDisplay();

        _config = _configLoader.Load(_basePath);
        if (_config == null)
        {
            QuestionsHeader = "Content setup not found. Add config.json to Content folder.";
            return;
        }

        ApplyInitialUiState();
    }

    private void InitializeVersionDisplay()
    {
        VersionText = $"v{_updateService.GetCurrentVersion()}";
        var isDev = _isRunningFromDev();
        if (isDev)
            VersionText += " (dev)";
        ProductionHeaderVisibility = isDev ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyInitialUiState()
    {
        QuestionsHeader = "Select a procedure type (File → Start New)";
        StatusText = "Ready. Select File → Start New to begin.";
        UpdateBlockCount(0);
    }

    [RelayCommand]
    private void StartNew(string procedureType)
    {
        if (string.IsNullOrEmpty(procedureType) || _config == null)
            return;

        _session = new ProcedureSession
        {
            ProcedureType = procedureType,
            Answers = new Dictionary<string, bool>(),
            InsertedBlockIds = new HashSet<string>(),
            InsertedBlocks = new Dictionary<string, List<Block>>(),
            Document = new FlowDocument()
        };

        SetDocument?.Invoke(_session.Document);

        ProcedureTypeText = procedureType;
        UpdateBlockCount(0);
        StatusText = $"Started {procedureType} procedure.";

        RenderQuestions();
        RebuildDocument();
    }

    private void RenderQuestions()
    {
        Questions.Clear();
        if (_session == null || _config == null)
            return;

        var questions = _config.Questions
            .Where(q => q.Type == Constants.QuestionTypeBoolean)
            .Where(q => q.Sections == null || q.Sections.Count == 0 ||
                        q.Sections.Contains(_session.ProcedureType, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (questions.Count == 0)
        {
            QuestionsHeader = "No questions configured in config.json.";
            return;
        }

        foreach (var q in questions)
        {
            var hasAnswer = _session.Answers.TryGetValue(q.Id, out var current);
            var item = new QuestionItemViewModel(q.Id, q.Text, hasAnswer ? current : null, OnAnswer);
            Questions.Add(item);
        }
    }

    private void OnAnswer(string questionId, bool value)
    {
        if (_session == null)
            return;

        _session.Answers[questionId] = value;
        RebuildDocument();
        StatusText = _session.InsertedBlockIds.Count > 0
            ? $"Document updated: {_session.InsertedBlockIds.Count} block(s)."
            : "Document updated.";
    }

    private void RebuildDocument()
    {
        if (_session == null || GetDocument == null)
            return;

        var doc = GetDocument();
        var metadata = _blockLoader.LoadMetadata(_session.ProcedureType).ToList();
        var blockIds = _ruleEngine.GetBlocksToInsert(metadata, _session.Answers).ToList();

        _session.Document.Blocks.Clear();
        _session.InsertedBlockIds.Clear();
        _session.InsertedBlocks.Clear();

        if (blockIds.Count > 0)
        {
            _documentAssembler.AppendBlocks(_session.Document, _session.InsertedBlockIds, blockIds, _session.InsertedBlocks);
        }

        UpdateBlockCount(_session.InsertedBlockIds.Count);
    }

    private void UpdateBlockCount(int count)
    {
        BlockCountText = $"{count} block(s)";
    }

    [RelayCommand]
    private void ExportPdf()
    {
        if (_session == null || GetDocument == null)
        {
            MessageBox.Show("Start a procedure first (File → Start New).", "InScope", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            DefaultExt = "pdf",
            FileName = $"InScope_{_session.ProcedureType}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _pdfExporter.Export(GetDocument(), dialog.FileName);
                StatusText = $"Exported to {Path.GetFileName(dialog.FileName)}";
                MessageBox.Show($"PDF saved to {dialog.FileName}", "InScope", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Log(AppLogger.LogLevel.Error, "PdfExport", "PDF export failed", new { message = ex.Message, exceptionType = ex.GetType().FullName });
                MessageBox.Show($"PDF export failed: {ex.Message}", "InScope", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Export failed.";
            }
        }
    }

    [RelayCommand]
    private void Exit() => _exitApp();

    [RelayCommand]
    private void OpenLogFolder()
    {
        AppLogger.OpenLogFolder();
    }

    [RelayCommand]
    private void RecoverBlockBackup()
    {
        RequestOpenRecoveryWindow?.Invoke();
    }

    /// <summary>
    /// Invoked to open the Recovery window (View responsibility).
    /// </summary>
    public Action? RequestOpenRecoveryWindow { get; set; }

    [RelayCommand]
    private void EditQuestions()
    {
        if (_config == null)
        {
            MessageBox.Show("Content setup not found. Add config.json to the Content folder first.", "InScope", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        RequestEditQuestions?.Invoke();
    }

    /// <summary>
    /// Invoked to open Question Editor. View shows dialog and reloads config if saved.
    /// </summary>
    public Action? RequestEditQuestions { get; set; }

    [RelayCommand]
    private void EditBlockLibrary()
    {
        RequestEditBlockLibrary?.Invoke();
    }

    /// <summary>
    /// Invoked to open Block Editor. View shows dialog.
    /// </summary>
    public Action? RequestEditBlockLibrary { get; set; }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var result = await _updateService.CheckForUpdateAsync();
            if (!result.Success)
            {
                var msg = result.ErrorMessage ?? "Could not check for updates. Please try again later.";
                MessageBox.Show(msg, "InScope - Check for Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (result.Update != null)
            {
                await TryDownloadAndInstallUpdateAsync(result.Update);
            }
            else
            {
                MessageBox.Show($"You have the latest version (v{_updateService.GetCurrentVersion()}).",
                    "InScope - Check for Updates", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Error, "UpdateCheck", "Check for updates failed", new { message = ex.Message });
            MessageBox.Show("Could not check for updates. Please try again later.",
                "InScope - Check for Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task TryDownloadAndInstallUpdateAsync(UpdateInfo update)
    {
        var message = $"Update v{update.Version} available.\n\nDownload and install now? The app will close when the installer starts.";
        var dialogResult = MessageBox.Show(message, "InScope - Update Available",
            MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (dialogResult != MessageBoxResult.Yes || string.IsNullOrEmpty(update.DownloadUrl))
            return;

        StatusText = "Downloading update...";
        try
        {
            var installerPath = await _updateService.DownloadInstallerAsync(update.DownloadUrl);
            StatusText = "Launching installer...";
            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Error, "UpdateCheck", "Download failed", new { message = ex.Message });
            MessageBox.Show($"Download failed: {ex.Message}\n\nYou can try Help → Check for Updates or the website instead.", "InScope - Update", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (!string.IsNullOrEmpty(update.ReleaseUrl))
                Process.Start(new ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true });
        }
        finally
        {
            StatusText = "Ready.";
        }
    }

    /// <summary>
    /// Call when questions were edited and config was reloaded. Refreshes questions panel if session active.
    /// </summary>
    public void OnQuestionsConfigReloaded(AppConfig reloaded)
    {
        _config = reloaded;
        if (_session != null)
            RenderQuestions();
    }
}

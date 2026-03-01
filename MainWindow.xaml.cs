using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using Microsoft.Win32;
using InScope.Models;
using InScope.Services;

namespace InScope;

public partial class MainWindow : Window
{
    private ProcedureSession? _session;
    private AppConfig? _config;
    private string _basePath = string.Empty;
    private BlockLoader? _blockLoader;
    private RuleEngine? _ruleEngine;
    private DocumentAssembler? _documentAssembler;
    private PdfExporter? _pdfExporter;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        LoadConfiguration();
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
        var versionDisplay = $"v{UpdateService.GetCurrentVersion()}";
        var isDev = IsRunningFromDev();
        if (isDev)
            versionDisplay += " (dev)";
        VersionText.Text = versionDisplay;
        ProductionHeaderBar.Visibility = isDev ? Visibility.Collapsed : Visibility.Visible;
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var build = version?.Build ?? 0;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{(build >= 0 ? build : 0)}" : "?";
        _basePath = ContentPathResolver.GetEffectiveContentPath();
        AppLogger.Log(AppLogger.LogLevel.Info, "Startup", "InScope starting", new { version = versionStr, contentPath = _basePath });

        _config = ConfigLoader.Load(_basePath);

        if (_config == null)
        {
            AppLogger.Log(AppLogger.LogLevel.Warning, "Startup", "Content setup not found");
            MessageBox.Show(
                "Content setup not found. Ensure config.json exists in the Content folder or C:\\ProgramData\\InScope.",
                "InScope - Setup Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            QuestionsHeader.Text = "Content setup not found. Add config.json to Content folder.";
            return;
        }

        _blockLoader = new BlockLoader(_config.BasePath);
        _ruleEngine = new RuleEngine();
        _documentAssembler = new DocumentAssembler(_blockLoader);
        _pdfExporter = new PdfExporter();

        QuestionsHeader.Text = "Select a procedure type (File → Start New)";
        StatusText.Text = "Ready. Select File → Start New to begin.";
        UpdateBlockCount(0);
    }

    private void StartNew_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || menuItem.Tag is not string procedureType || _config == null)
            return;

        _session = new ProcedureSession
        {
            ProcedureType = procedureType,
            Answers = new Dictionary<string, bool>(),
            InsertedBlockIds = new HashSet<string>(),
            InsertedBlocks = new Dictionary<string, List<Block>>(),
            Document = new FlowDocument()
        };

        DocumentEditor.Document = _session.Document;
        DocumentEditor.IsDocumentEnabled = true;

        ProcedureTypeText.Text = procedureType;
        UpdateBlockCount(0);
        StatusText.Text = $"Started {procedureType} procedure.";
        RenderQuestions();
        RebuildDocument();
    }

    private void RenderQuestions()
    {
        QuestionsPanel.Children.Clear();

        if (_session == null || _config == null)
            return;

        var questions = _config.Questions
            .Where(q => q.Type == "boolean")
            .Where(q => q.Sections == null || q.Sections.Count == 0 || q.Sections.Contains(_session.ProcedureType, StringComparer.OrdinalIgnoreCase))
            .ToList();
        // #region agent log
        DebugLog.Log("MainWindow.RenderQuestions", "Questions filtered", new { procedureType = _session.ProcedureType, questionIds = questions.Select(q => q.Id).ToList() });
        // #endregion
        if (questions.Count == 0)
        {
            QuestionsPanel.Children.Add(new TextBlock
            {
                Text = "No questions configured in config.json.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Gray
            });
            return;
        }

        foreach (var q in questions)
        {
            var questionId = q.Id;
            var textBlock = new TextBlock
            {
                Text = q.Text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 4)
            };
            QuestionsPanel.Children.Add(textBlock);

            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            var yesBtn = new Button { Content = "Yes", Width = 60, Margin = new Thickness(0, 0, 8, 0), Tag = (questionId, true) };
            var noBtn = new Button { Content = "No", Width = 60, Tag = (questionId, false) };

            var hasAnswer = _session.Answers.TryGetValue(questionId, out var current);
            if (hasAnswer)
            {
                yesBtn.IsEnabled = !current;
                noBtn.IsEnabled = current;
            }

            yesBtn.Click += (_, _) => OnAnswer(questionId, true, yesBtn, noBtn);
            noBtn.Click += (_, _) => OnAnswer(questionId, false, yesBtn, noBtn);

            stack.Children.Add(yesBtn);
            stack.Children.Add(noBtn);
            QuestionsPanel.Children.Add(stack);
        }
    }

    private void OnAnswer(string questionId, bool value, Button yesBtn, Button noBtn)
    {
        if (_session == null || _blockLoader == null || _ruleEngine == null || _documentAssembler == null)
            return;

        _session.Answers[questionId] = value;
        yesBtn.IsEnabled = !value;
        noBtn.IsEnabled = value;

        RebuildDocument();
        StatusText.Text = _session.InsertedBlockIds.Count > 0 ? $"Document updated: {_session.InsertedBlockIds.Count} block(s)." : "Document updated.";
    }

    private void RebuildDocument()
    {
        if (_session == null || _blockLoader == null || _ruleEngine == null || _documentAssembler == null)
            return;

        var metadata = _blockLoader.LoadMetadata(_session.ProcedureType).ToList();
        var blockIds = _ruleEngine.GetBlocksToInsert(metadata, _session.Answers).ToList();
        // #region agent log
        var answersSnap = new Dictionary<string, bool>(_session.Answers);
        DebugLog.Log("MainWindow.RebuildDocument", "Blocks resolved", new { procedureType = _session.ProcedureType, answers = answersSnap, blockIds = blockIds.ToList() });
        // #endregion

        // Rebuild document from scratch to avoid duplication (block-by-block removal was unreliable)
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
        BlockCountText.Text = $"{count} block(s)";
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_session == null || _pdfExporter == null)
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
                _pdfExporter.Export(_session.Document, dialog.FileName);
                StatusText.Text = $"Exported to {Path.GetFileName(dialog.FileName)}";
                MessageBox.Show($"PDF saved to {dialog.FileName}", "InScope", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Log(AppLogger.LogLevel.Error, "PdfExport", "PDF export failed", new { message = ex.Message, exceptionType = ex.GetType().FullName });
                MessageBox.Show($"PDF export failed: {ex.Message}", "InScope", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Export failed.";
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
            item.IsEnabled = false;

        try
        {
            var result = await UpdateService.CheckForUpdateAsync();
            if (!result.Success)
            {
                var msg = result.ErrorMessage ?? "Could not check for updates. Please try again later.";
                MessageBox.Show(msg, "InScope - Check for Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (result.Update != null)
            {
                var update = result.Update;
                var message = $"Update v{update.Version} available.\n\nDownload and install now? The app will close when the installer starts.";
                var dialogResult = MessageBox.Show(message, "InScope - Update Available",
                    MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (dialogResult == MessageBoxResult.Yes && !string.IsNullOrEmpty(update.DownloadUrl))
                {
                    StatusText.Text = "Downloading update...";
                    try
                    {
                        var installerPath = await UpdateService.DownloadInstallerAsync(update.DownloadUrl);
                        StatusText.Text = "Launching installer...";
                        Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
                        Application.Current.Shutdown();
                    }
                    catch (Exception downloadEx)
                    {
                        AppLogger.Log(AppLogger.LogLevel.Error, "UpdateCheck", "Download failed", new { message = downloadEx.Message });
                        MessageBox.Show($"Download failed: {downloadEx.Message}\n\nYou can try the website instead.", "InScope - Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                        if (!string.IsNullOrEmpty(update.ReleaseUrl))
                            Process.Start(new ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true });
                    }
                    finally
                    {
                        StatusText.Text = "Ready.";
                    }
                }
            }
            else
            {
                MessageBox.Show($"You have the latest version (v{UpdateService.GetCurrentVersion()}).",
                    "InScope - Check for Updates", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Error, "UpdateCheck", "Check for updates failed", new { message = ex.Message });
            MessageBox.Show("Could not check for updates. Please try again later.",
                "InScope - Check for Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            if (sender is MenuItem menuItem)
                menuItem.IsEnabled = true;
        }
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        AppLogger.OpenLogFolder();
    }

    private void EditBlockLibrary_Click(object sender, RoutedEventArgs e)
    {
        if (_blockLoader == null)
        {
            MessageBox.Show("Content setup not found. Add config.json to the Content folder first.", "InScope", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var procedureTypes = _config?.ProcedureTypes ?? new List<string>();
        var editor = new BlockEditorWindow(_blockLoader, procedureTypes)
        {
            Owner = this
        };
        editor.ShowDialog();
    }
}

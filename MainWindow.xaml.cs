using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var build = version?.Build ?? 0;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{(build >= 0 ? build : 0)}" : "?";
        _basePath = ConfigLoader.GetContentBasePath();
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
            Document = new FlowDocument()
        };

        DocumentEditor.Document = _session.Document;
        DocumentEditor.IsDocumentEnabled = true;

        ProcedureTypeText.Text = procedureType;
        UpdateBlockCount(0);
        StatusText.Text = $"Started {procedureType} procedure.";
        RenderQuestions();
    }

    private void RenderQuestions()
    {
        QuestionsPanel.Children.Clear();

        if (_session == null || _config == null)
            return;

        var questions = _config.Questions.Where(q => q.Type == "boolean").ToList();
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

        var metadata = _blockLoader.LoadMetadata(_session.ProcedureType).ToList();
        var blockIds = _ruleEngine.GetBlocksToInsert(metadata, _session.Answers);
        var toInsert = blockIds.Except(_session.InsertedBlockIds).ToList();

        if (toInsert.Count > 0)
        {
            _documentAssembler.AppendBlocks(_session.Document, _session.InsertedBlockIds, toInsert);
            UpdateBlockCount(_session.InsertedBlockIds.Count);
            StatusText.Text = $"Added {toInsert.Count} block(s).";
        }
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

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        AppLogger.OpenLogFolder();
    }
}

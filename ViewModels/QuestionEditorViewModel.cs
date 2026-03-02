using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InScope.Models;
using InScope.Services;

namespace InScope.ViewModels;

public partial class QuestionEditorViewModel : ObservableObject
{
    private readonly IConfigLoader _configLoader;
    private readonly AppConfig _config;
    private readonly string _basePath;
    private readonly Action _closeCallback;
    private readonly Func<IEnumerable<string>, QuestionConfig?, IEnumerable<string>, QuestionConfig?> _showQuestionDialog;

    public ObservableCollection<QuestionConfig> Questions { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private QuestionConfig? _selectedQuestion;

    [ObservableProperty]
    private string _statusText = "Add, edit, or delete questions. Click Save to persist changes.";

    public bool WasSaved { get; private set; }

    public QuestionEditorViewModel(
        IConfigLoader configLoader,
        AppConfig config,
        string basePath,
        Action closeCallback,
        Func<IEnumerable<string>, QuestionConfig?, IEnumerable<string>, QuestionConfig?> showQuestionDialog)
    {
        _configLoader = configLoader;
        _config = config;
        _basePath = basePath;
        _closeCallback = closeCallback;
        _showQuestionDialog = showQuestionDialog;

        foreach (var q in config.Questions)
            Questions.Add(q);
    }

    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void Edit()
    {
        if (SelectedQuestion == null) return;

        var selected = SelectedQuestion;
        var existingIds = Questions.Where(q => q != selected).Select(q => q.Id).ToList();
        var result = _showQuestionDialog(_config.ProcedureTypes, selected, existingIds);
        if (result == null) return;

        var index = Questions.IndexOf(selected);
        if (index >= 0)
            Questions[index] = result;
        StatusText = "Updated question. Click Save to persist.";
    }

    private bool CanEditOrDelete() => SelectedQuestion != null;

    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void Delete()
    {
        if (SelectedQuestion == null) return;

        var selected = SelectedQuestion;
        var result = MessageBox.Show(
            $"Delete question '{selected.Id}'? Block conditions that reference this question will no longer match.",
            "Delete Question",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        Questions.Remove(selected);
        StatusText = $"Deleted '{selected.Id}'. Click Save to persist.";
    }

    [RelayCommand]
    private void Add()
    {
        var existingIds = Questions.Select(q => q.Id).ToList();
        var result = _showQuestionDialog(_config.ProcedureTypes, null, existingIds);
        if (result == null) return;

        Questions.Add(result);
        StatusText = $"Added question '{result.Id}'. Click Save to persist.";
    }

    [RelayCommand]
    private void Save()
    {
        _config.Questions.Clear();
        foreach (var q in Questions)
            _config.Questions.Add(q);

        if (!_configLoader.SaveConfig(_basePath, _config))
        {
            MessageBox.Show(
                "Could not save config.json. You may not have write permission.",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            StatusText = "Save failed.";
            return;
        }

        WasSaved = true;
        StatusText = "Saved successfully.";
    }

    [RelayCommand]
    private void CloseRequested() => _closeCallback();
}

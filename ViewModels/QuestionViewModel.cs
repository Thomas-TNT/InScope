using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InScope.Models;
using InScope;

namespace InScope.ViewModels;

public partial class QuestionViewModel : ObservableObject
{
    private readonly QuestionConfig? _editQuestion;
    private readonly HashSet<string> _existingIds;
    private readonly Action<bool> _closeCallback;
    private string _lastDerivedId = string.Empty;

    [ObservableProperty]
    private string _questionId = string.Empty;

    [ObservableProperty]
    private string _questionText = string.Empty;

    partial void OnQuestionTextChanged(string value)
    {
        if (_editQuestion != null) return;
        var derived = QuestionIdHelper.DeriveFromText(value);
        if (string.IsNullOrEmpty(QuestionId) || string.Equals(QuestionId, _lastDerivedId, System.StringComparison.Ordinal))
        {
            QuestionId = derived;
        }
        _lastDerivedId = derived;
    }

    public ObservableCollection<SectionItemViewModel> Sections { get; } = new();

    public QuestionViewModel(
        IEnumerable<string> procedureTypes,
        QuestionConfig? editQuestion,
        IEnumerable<string>? existingIds,
        Action<bool> closeCallback)
    {
        _editQuestion = editQuestion;
        _existingIds = existingIds?.ToHashSet(System.StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        _closeCallback = closeCallback;

        var editSections = editQuestion?.Sections;
        foreach (var name in procedureTypes)
        {
            var isSelected = editSections != null
                && editSections.Contains(name, System.StringComparer.OrdinalIgnoreCase);
            Sections.Add(new SectionItemViewModel(name, isSelected));
        }

        if (editQuestion != null)
        {
            QuestionId = editQuestion.Id;
            QuestionText = editQuestion.Text;
        }
    }

    [RelayCommand]
    private void Ok()
    {
        var id = QuestionId.Trim();
        if (string.IsNullOrEmpty(id))
        {
            MessageBox.Show("Please enter a Question ID.", "Question", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Regex.IsMatch(id, @"^[a-zA-Z0-9_\-]+$"))
        {
            MessageBox.Show("Question ID may only contain letters, numbers, underscores, and hyphens.", "Question", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var isEdit = _editQuestion != null;
        var idChanged = isEdit && !string.Equals(id, _editQuestion!.Id, System.StringComparison.OrdinalIgnoreCase);
        if (!isEdit || idChanged)
        {
            if (_existingIds.Contains(id))
            {
                MessageBox.Show($"A question with ID '{id}' already exists.", "Question", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var text = QuestionText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("Please enter the question text.", "Question", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sections = Sections.Where(s => s.IsSelected).Select(s => s.Name).ToList();
        if (sections.Count == 0)
            sections = null;

        Result = new QuestionConfig
        {
            Id = id,
            Text = text,
            Type = Constants.QuestionTypeBoolean,
            Sections = sections
        };
        _closeCallback(true);
    }

    [RelayCommand]
    private void Cancel() => _closeCallback(false);

    public QuestionConfig? Result { get; private set; }
}

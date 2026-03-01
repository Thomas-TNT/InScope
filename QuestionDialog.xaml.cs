using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using InScope.Models;

namespace InScope;

public partial class QuestionDialog : Window
{
    private readonly List<string> _procedureTypes;
    private readonly HashSet<string> _existingIds;
    private readonly List<CheckBox> _sectionCheckBoxes = new();
    private readonly QuestionConfig? _editQuestion;

    public QuestionConfig? Result { get; private set; }

    public QuestionDialog(IEnumerable<string> procedureTypes, QuestionConfig? editQuestion = null, IEnumerable<string>? existingIds = null)
    {
        _procedureTypes = procedureTypes.ToList();
        _editQuestion = editQuestion;
        _existingIds = existingIds?.ToHashSet(System.StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        InitializeComponent();

        foreach (var section in _procedureTypes)
        {
            var cb = new CheckBox
            {
                Content = section,
                Margin = new Thickness(0, 4, 0, 0)
            };
            if (editQuestion?.Sections != null && editQuestion.Sections.Contains(section, System.StringComparer.OrdinalIgnoreCase))
                cb.IsChecked = true;
            _sectionCheckBoxes.Add(cb);
            SectionsPanel.Children.Add(cb);
        }

        if (editQuestion != null)
        {
            Title = "Edit Question";
            IdBox.Text = editQuestion.Id;
            TextBox.Text = editQuestion.Text;
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var id = IdBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(id))
        {
            MessageBox.Show("Please enter a Question ID.", "Question", MessageBoxButton.OK, MessageBoxImage.Warning);
            IdBox.Focus();
            return;
        }

        if (!Regex.IsMatch(id, @"^[a-zA-Z0-9_\-]+$"))
        {
            MessageBox.Show("Question ID may only contain letters, numbers, underscores, and hyphens.", "Question", MessageBoxButton.OK, MessageBoxImage.Warning);
            IdBox.Focus();
            return;
        }

        var isEdit = _editQuestion != null;
        var idChanged = isEdit && !string.Equals(id, _editQuestion!.Id, System.StringComparison.OrdinalIgnoreCase);
        if (!isEdit || idChanged)
        {
            if (_existingIds.Contains(id) && (!isEdit || idChanged))
            {
                MessageBox.Show($"A question with ID '{id}' already exists.", "Question", MessageBoxButton.OK, MessageBoxImage.Warning);
                IdBox.Focus();
                return;
            }
        }

        var text = TextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("Please enter the question text.", "Question", MessageBoxButton.OK, MessageBoxImage.Warning);
            TextBox.Focus();
            return;
        }

        var sections = _sectionCheckBoxes
            .Where(cb => cb.IsChecked == true && cb.Content is string s)
            .Select(cb => cb.Content.ToString()!)
            .ToList();
        if (sections.Count == 0)
            sections = null;

        Result = new QuestionConfig
        {
            Id = id,
            Text = text,
            Type = Constants.QuestionTypeBoolean,
            Sections = sections
        };
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InScope.Models;
using InScope.Services;

namespace InScope;

public partial class QuestionEditorWindow : Window
{
    private readonly AppConfig _config;
    private readonly string _basePath;

    public bool WasSaved { get; private set; }

    public QuestionEditorWindow(AppConfig config, string basePath)
    {
        _config = config;
        _basePath = basePath;
        InitializeComponent();
        RefreshList();
    }

    private void RefreshList()
    {
        QuestionList.ItemsSource = null;
        QuestionList.ItemsSource = _config.Questions.ToList();
        EditButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
    }

    private void QuestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = QuestionList.SelectedItem != null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var existingIds = _config.Questions.Select(q => q.Id).ToList();
        var dialog = new QuestionDialog(_config.ProcedureTypes, editQuestion: null, existingIds)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.Result == null)
            return;

        _config.Questions.Add(dialog.Result);
        RefreshList();
        StatusText.Text = $"Added question '{dialog.Result.Id}'. Click Save to persist.";
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (QuestionList.SelectedItem is not QuestionConfig selected)
            return;

        var index = _config.Questions.IndexOf(selected);
        if (index < 0) return;

        var existingIds = _config.Questions.Where(q => q != selected).Select(q => q.Id).ToList();
        var dialog = new QuestionDialog(_config.ProcedureTypes, selected, existingIds)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.Result == null)
            return;

        _config.Questions[index] = dialog.Result;
        RefreshList();
        StatusText.Text = $"Updated question. Click Save to persist.";
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (QuestionList.SelectedItem is not QuestionConfig selected)
            return;

        var result = MessageBox.Show(
            $"Delete question '{selected.Id}'? Block conditions that reference this question will no longer match.",
            "Delete Question",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        _config.Questions.Remove(selected);
        RefreshList();
        StatusText.Text = $"Deleted '{selected.Id}'. Click Save to persist.";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfigLoader.SaveConfig(_basePath, _config))
        {
            MessageBox.Show(
                "Could not save config.json. You may not have write permission.",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            StatusText.Text = "Save failed.";
            return;
        }

        WasSaved = true;
        StatusText.Text = "Saved successfully.";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

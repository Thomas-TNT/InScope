using System.Collections.Generic;
using System.Windows;
using InScope.Models;
using InScope.ViewModels;

namespace InScope;

public partial class QuestionDialog : Window
{
    public QuestionConfig? Result => (DataContext as QuestionViewModel)?.Result;

    public QuestionDialog(IEnumerable<string> procedureTypes, QuestionConfig? editQuestion = null, IEnumerable<string>? existingIds = null)
    {
        InitializeComponent();
        Title = editQuestion != null ? "Edit Question" : "Question";
        DataContext = new QuestionViewModel(procedureTypes, editQuestion, existingIds, ok =>
        {
            DialogResult = ok;
            Close();
        });
    }
}

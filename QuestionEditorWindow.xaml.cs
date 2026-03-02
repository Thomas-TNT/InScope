using System.Collections.Generic;
using System.Linq;
using System.Windows;
using InScope.Models;
using InScope.Services;
using InScope.ViewModels;

namespace InScope;

public partial class QuestionEditorWindow : Window
{
    public bool WasSaved => (DataContext as QuestionEditorViewModel)?.WasSaved ?? false;

    public QuestionEditorWindow(AppConfig config, string basePath, IBlockLoader blockLoader)
    {
        InitializeComponent();
        var configLoader = ServiceLocator.GetRequiredService<IConfigLoader>();
        DataContext = new QuestionEditorViewModel(
            configLoader,
            blockLoader,
            config,
            basePath,
            () => Close(),
            (procedureTypes, editQuestion, existingIds) =>
            {
                var dialog = new QuestionDialog(procedureTypes, editQuestion, existingIds) { Owner = this };
                return dialog.ShowDialog() == true ? dialog.Result : null;
            });
    }
}

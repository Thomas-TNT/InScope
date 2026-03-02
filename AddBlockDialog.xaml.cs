using System.Collections.Generic;
using System.Windows;
using InScope.Services;
using InScope.ViewModels;

namespace InScope;

public partial class AddBlockDialog : Window
{
    public string? BlockId => (DataContext as AddBlockViewModel)?.ResultBlockId;
    public string? Section => (DataContext as AddBlockViewModel)?.ResultSection;

    public AddBlockDialog(IBlockLoader blockLoader, IEnumerable<string> sections)
    {
        InitializeComponent();
        DataContext = new AddBlockViewModel(blockLoader, sections, ok =>
        {
            DialogResult = ok;
            Close();
        });
    }
}

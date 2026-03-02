using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InScope.Services;

namespace InScope.ViewModels;

public partial class AddBlockViewModel : ObservableObject
{
    private readonly IBlockLoader _blockLoader;
    private readonly Action<bool> _closeCallback;

    [ObservableProperty]
    private string _blockId = string.Empty;

    [ObservableProperty]
    private string _selectedSection = string.Empty;

    public List<string> Sections { get; }

    public AddBlockViewModel(IBlockLoader blockLoader, IEnumerable<string> sections, Action<bool> closeCallback)
    {
        _blockLoader = blockLoader;
        _closeCallback = closeCallback;
        Sections = sections.ToList();
        if (Sections.Count > 0)
            SelectedSection = Sections[0];
    }

    [RelayCommand]
    private void Ok()
    {
        var id = BlockId.Trim();
        if (string.IsNullOrEmpty(id))
        {
            MessageBox.Show("Please enter a Block ID.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Regex.IsMatch(id, @"^[a-zA-Z0-9\-]+$"))
        {
            MessageBox.Show("Block ID may only contain letters, numbers, and hyphens.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_blockLoader.BlockExists(id))
        {
            MessageBox.Show($"A block with ID '{id}' already exists.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var section = SelectedSection?.Trim() ?? "";
        if (string.IsNullOrEmpty(section))
        {
            MessageBox.Show("Please select a section.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultBlockId = id;
        ResultSection = section;
        _closeCallback(true);
    }

    [RelayCommand]
    private void Cancel() => _closeCallback(false);

    public string? ResultBlockId { get; private set; }
    public string? ResultSection { get; private set; }
}

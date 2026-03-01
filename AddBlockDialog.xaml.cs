using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using InScope.Services;

namespace InScope;

public partial class AddBlockDialog : Window
{
    private readonly BlockLoader _blockLoader;

    public string? BlockId { get; private set; }
    public string? Section { get; private set; }

    public AddBlockDialog(BlockLoader blockLoader, System.Collections.Generic.IEnumerable<string> sections)
    {
        _blockLoader = blockLoader;
        InitializeComponent();
        SectionCombo.ItemsSource = sections.ToList();
        if (SectionCombo.Items.Count > 0)
            SectionCombo.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var blockId = BlockIdBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(blockId))
        {
            MessageBox.Show("Please enter a Block ID.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            BlockIdBox.Focus();
            return;
        }

        if (!Regex.IsMatch(blockId, @"^[a-zA-Z0-9\-]+$"))
        {
            MessageBox.Show("Block ID may only contain letters, numbers, and hyphens.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            BlockIdBox.Focus();
            return;
        }

        if (_blockLoader.BlockExists(blockId))
        {
            MessageBox.Show($"A block with ID '{blockId}' already exists.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            BlockIdBox.Focus();
            return;
        }

        var section = SectionCombo.SelectedItem as string ?? SectionCombo.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(section))
        {
            MessageBox.Show("Please select a section.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BlockId = blockId;
        Section = section;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

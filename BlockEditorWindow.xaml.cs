using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using InScope.Services;

namespace InScope;

public partial class BlockEditorWindow : Window
{
    private readonly BlockLoader _blockLoader;
    private readonly IEnumerable<string> _procedureTypes;
    private string? _selectedBlockId;
    private bool _isWritable;

    private sealed class BlockListEntry
    {
        public string BlockId { get; init; } = string.Empty;
        public string Section { get; init; } = string.Empty;
    }

    public BlockEditorWindow(BlockLoader blockLoader, IEnumerable<string> procedureTypes)
    {
        _blockLoader = blockLoader;
        _procedureTypes = procedureTypes?.ToList() ?? new List<string>();
        _isWritable = blockLoader.IsBlocksWritable();
        InitializeComponent();
        AddBlockButton.IsEnabled = _isWritable;
        DeleteBlockButton.IsEnabled = false;
        LoadBlockList();
        UpdateStatusReadOnly();
    }

    private void LoadBlockList()
    {
        var metadataByBlock = _blockLoader.LoadAllMetadata().ToDictionary(m => m.BlockId, m => m.Section, System.StringComparer.OrdinalIgnoreCase);
        var blockIds = _blockLoader.EnumerateBlockIds().OrderBy(id => id).ToList();
        var entries = new List<BlockListEntry>();
        foreach (var blockId in blockIds)
        {
            var section = metadataByBlock.TryGetValue(blockId, out var s) ? s : Constants.ProcedureTypeOther;
            entries.Add(new BlockListEntry { BlockId = blockId, Section = section });
        }

        BlockList.ItemsSource = entries;
        var view = (CollectionView)CollectionViewSource.GetDefaultView(BlockList.ItemsSource);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(BlockListEntry.Section)));
        view.SortDescriptions.Add(new SortDescription(nameof(BlockListEntry.Section), ListSortDirection.Ascending));
        view.SortDescriptions.Add(new SortDescription(nameof(BlockListEntry.BlockId), ListSortDirection.Ascending));
    }

    private void BlockList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BlockList.SelectedItem is not BlockListEntry entry)
        {
            _selectedBlockId = null;
            DocumentEditor.Document = new FlowDocument();
            SaveButton.IsEnabled = false;
            RevertButton.IsEnabled = false;
            DeleteBlockButton.IsEnabled = false;
            BlockFileNameText.Text = "Select a block";
            StatusText.Text = "Ready. Select a block to edit.";
            return;
        }

        _selectedBlockId = entry.BlockId;
        var doc = _blockLoader.LoadRtf(entry.BlockId);
        if (doc == null)
        {
            DocumentEditor.Document = new FlowDocument();
            StatusText.Text = $"Could not load {entry.BlockId}.rtf";
            SaveButton.IsEnabled = false;
            RevertButton.IsEnabled = false;
            DeleteBlockButton.IsEnabled = false;
        }
        else
        {
            DocumentEditor.Document = doc;
            SaveButton.IsEnabled = _isWritable;
            RevertButton.IsEnabled = true;
            DeleteBlockButton.IsEnabled = _isWritable;
            StatusText.Text = _isWritable ? "Ready. Edit and click Save to persist changes." : "Content folder is read-only. Run as administrator or use a writable content path.";
        }

        BlockFileNameText.Text = $"{entry.BlockId}.rtf";
    }

    private void UpdateStatusReadOnly()
    {
        if (!_isWritable)
            StatusText.Text = "Content folder is read-only. Run as administrator or use a writable content path.";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedBlockId) || !_isWritable)
            return;

        var blockId = _selectedBlockId;
        var previousContent = _blockLoader.ReadRtfBytes(blockId);
        if (previousContent != null && previousContent.Length > 0)
        {
            BlockChangeLog.LogChange(blockId, Constants.BlockChangeActionModified, previousContent);
        }

        var success = _blockLoader.SaveRtf(blockId, DocumentEditor.Document);
        if (success)
        {
            StatusText.Text = $"Saved {blockId}.rtf";
        }
        else
        {
            MessageBox.Show(
                "Could not save the file. The file may be in use by another program, or you may not have write permission to the content folder.",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            StatusText.Text = "Save failed. Check that the file is not open elsewhere and you have write permission.";
        }
    }

    private void Revert_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedBlockId))
            return;

        var doc = _blockLoader.LoadRtf(_selectedBlockId);
        if (doc != null)
        {
            DocumentEditor.Document = doc;
            StatusText.Text = $"Reverted {_selectedBlockId}.rtf";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AddBlock_Click(object sender, RoutedEventArgs e)
    {
        var sections = _procedureTypes.Any() ? _procedureTypes : Constants.DefaultProcedureTypes;
        var dialog = new AddBlockDialog(_blockLoader, sections)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.BlockId == null || dialog.Section == null)
            return;

        if (!_blockLoader.CreateBlock(dialog.BlockId, dialog.Section))
        {
            MessageBox.Show("Could not create the block.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BlockChangeLog.LogChange(dialog.BlockId, Constants.BlockChangeActionCreated, null);
        LoadBlockList();
        SelectBlockById(dialog.BlockId);
        StatusText.Text = $"Created {dialog.BlockId}.rtf. Edit and save to add content.";
    }

    private void SelectBlockById(string blockId)
    {
        if (BlockList.ItemsSource is not System.Collections.IEnumerable items)
            return;
        foreach (BlockListEntry entry in items)
        {
            if (string.Equals(entry.BlockId, blockId, System.StringComparison.OrdinalIgnoreCase))
            {
                BlockList.SelectedItem = entry;
                return;
            }
        }
    }

    private void DeleteBlock_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedBlockId))
            return;
        var blockId = _selectedBlockId;
        var result = MessageBox.Show(
            $"Delete block '{blockId}'? This cannot be undone.",
            "Delete Block",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        if (!_blockLoader.DeleteBlock(blockId))
        {
            MessageBox.Show("Could not delete the block.", "Delete Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selectedBlockId = null;
        DocumentEditor.Document = new FlowDocument();
        LoadBlockList();
        BlockFileNameText.Text = "Select a block";
        StatusText.Text = $"Deleted {blockId}.";
    }
}

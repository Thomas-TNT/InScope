using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using InScope.Models;
using InScope.Services;

namespace InScope;

public partial class BlockEditorWindow : Window
{
    private readonly BlockLoader _blockLoader;
    private string? _selectedBlockId;
    private bool _isWritable;

    private sealed class BlockListEntry
    {
        public string BlockId { get; init; } = string.Empty;
        public string Section { get; init; } = string.Empty;
    }

    public BlockEditorWindow(BlockLoader blockLoader)
    {
        _blockLoader = blockLoader;
        _isWritable = blockLoader.IsBlocksWritable();
        InitializeComponent();
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
            var section = metadataByBlock.TryGetValue(blockId, out var s) ? s : "Other";
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
        }
        else
        {
            DocumentEditor.Document = doc;
            SaveButton.IsEnabled = _isWritable;
            RevertButton.IsEnabled = true;
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

        var success = _blockLoader.SaveRtf(_selectedBlockId, DocumentEditor.Document);
        if (success)
        {
            StatusText.Text = $"Saved {_selectedBlockId}.rtf";
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
}

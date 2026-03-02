using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InScope.Services;
using InScope;

namespace InScope.ViewModels;

public partial class BlockEditorViewModel : ObservableObject
{
    private readonly IBlockLoader _blockLoader;
    private readonly IBlockChangeLog _blockChangeLog;
    private readonly IEnumerable<string> _procedureTypes;
    private readonly Action _closeCallback;
    private readonly Func<(string? blockId, string? section)?> _showAddBlockDialog;
    private readonly Func<FlowDocument?> _getCurrentDocument;
    private readonly Action<FlowDocument?> _setCurrentDocument;

    public ObservableCollection<BlockListEntry> Blocks { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteBlockCommand))]
    private BlockListEntry? _selectedBlock;

    private bool CanSaveOrDelete => SelectedBlock != null && IsWritable;
    private bool CanRevert => SelectedBlock != null;

    [ObservableProperty]
    private string _blockFileNameText = "Select a block";

    [ObservableProperty]
    private string _statusText = "Ready. Select a block to edit.";

    [ObservableProperty]
    private bool _isWritable;

    [ObservableProperty]
    private bool _canAddBlock;

    public BlockEditorViewModel(
        IBlockLoader blockLoader,
        IBlockChangeLog blockChangeLog,
        IEnumerable<string> procedureTypes,
        Action closeCallback,
        Func<(string? blockId, string? section)?> showAddBlockDialog,
        Func<FlowDocument?> getCurrentDocument,
        Action<FlowDocument?> setCurrentDocument)
    {
        _blockLoader = blockLoader;
        _blockChangeLog = blockChangeLog;
        _procedureTypes = procedureTypes?.ToList() ?? new List<string>();
        _closeCallback = closeCallback;
        _showAddBlockDialog = showAddBlockDialog;
        _getCurrentDocument = getCurrentDocument;
        _setCurrentDocument = setCurrentDocument;

        IsWritable = blockLoader.IsBlocksWritable();
        CanAddBlock = _isWritable;

        LoadBlockList();
        _setCurrentDocument(new FlowDocument());
        if (!_isWritable)
            StatusText = "Content folder is read-only. Run as administrator or use a writable content path.";
    }

    partial void OnSelectedBlockChanged(BlockListEntry? value)
    {
        _setCurrentDocument(new FlowDocument());
        if (value == null)
        {
            BlockFileNameText = "Select a block";
            StatusText = IsWritable ? "Ready. Select a block to edit." : StatusText;
            return;
        }

        BlockFileNameText = $"{value.BlockId}.rtf";
        var doc = _blockLoader.LoadRtf(value.BlockId);
        if (doc == null)
        {
            StatusText = $"Could not load {value.BlockId}.rtf";
            return;
        }

        _setCurrentDocument(doc);
        StatusText = IsWritable
            ? "Ready. Edit and click Save to persist changes."
            : "Content folder is read-only. Run as administrator or use a writable content path.";
    }

    private void LoadBlockList()
    {
        Blocks.Clear();
        var metadataByBlock = _blockLoader.LoadAllMetadata()
            .ToDictionary(m => m.BlockId, m => m.Section, System.StringComparer.OrdinalIgnoreCase);
        foreach (var blockId in _blockLoader.EnumerateBlockIds().OrderBy(id => id))
        {
            var section = metadataByBlock.TryGetValue(blockId, out var s) ? s : Constants.ProcedureTypeOther;
            Blocks.Add(new BlockListEntry { BlockId = blockId, Section = section });
        }
    }

    [RelayCommand]
    private void AddBlock()
    {
        var sections = _procedureTypes.Any() ? _procedureTypes : Constants.DefaultProcedureTypes;
        var result = _showAddBlockDialog();
        if (result == null) return;

        var (blockId, section) = result.Value;
        if (string.IsNullOrEmpty(blockId) || string.IsNullOrEmpty(section))
            return;

        if (!_blockLoader.CreateBlock(blockId, section))
        {
            MessageBox.Show("Could not create the block.", "Add Block", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _blockChangeLog.LogChange(blockId, Constants.BlockChangeActionCreated, null);
        LoadBlockList();
        StatusText = $"Created {blockId}.rtf. Edit and save to add content.";
        RequestSelectBlock?.Invoke(blockId);
    }

    /// <summary>
    /// Invoked when a new block is added so the view can select it.
    /// </summary>
    public Action<string>? RequestSelectBlock { get; set; }

    [RelayCommand(CanExecute = nameof(CanSaveOrDelete))]
    private void DeleteBlock()
    {
        if (SelectedBlock == null) return;

        var blockId = SelectedBlock.BlockId;
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

        SelectedBlock = null;
        _setCurrentDocument(new FlowDocument());
        LoadBlockList();
        BlockFileNameText = "Select a block";
        StatusText = $"Deleted {blockId}.";
    }

    [RelayCommand(CanExecute = nameof(CanSaveOrDelete))]
    private void Save()
    {
        if (SelectedBlock == null || !IsWritable) return;

        var blockId = SelectedBlock.BlockId;
        var previousContent = _blockLoader.ReadRtfBytes(blockId);
        if (previousContent != null && previousContent.Length > 0)
            _blockChangeLog.LogChange(blockId, Constants.BlockChangeActionModified, previousContent);

        var doc = _getCurrentDocument();
        if (doc == null) return;

        if (!_blockLoader.SaveRtf(blockId, doc))
        {
            MessageBox.Show(
                "Could not save the file. The file may be in use by another program, or you may not have write permission to the content folder.",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            StatusText = "Save failed. Check that the file is not open elsewhere and you have write permission.";
            return;
        }

        StatusText = $"Saved {blockId}.rtf";
    }

    [RelayCommand(CanExecute = nameof(CanRevert))]
    private void Revert()
    {
        if (SelectedBlock == null) return;

        var doc = _blockLoader.LoadRtf(SelectedBlock.BlockId);
        if (doc != null)
        {
            _setCurrentDocument(doc);
            StatusText = $"Reverted {SelectedBlock.BlockId}.rtf";
        }
    }

    [RelayCommand]
    private void CloseRequested() => _closeCallback();
}

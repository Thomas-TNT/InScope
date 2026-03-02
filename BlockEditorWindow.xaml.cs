using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using InScope.Services;
using InScope.ViewModels;

namespace InScope;

public partial class BlockEditorWindow : Window
{
    public BlockEditorWindow(IBlockLoader blockLoader, IEnumerable<string> procedureTypes)
    {
        InitializeComponent();

        var blockChangeLog = ServiceLocator.GetRequiredService<IBlockChangeLog>();
        var procedureTypesList = procedureTypes?.ToList() ?? new List<string>();

        var vm = new BlockEditorViewModel(
            blockLoader,
            blockChangeLog,
            procedureTypesList,
            () => Close(),
            () =>
            {
                var sections = procedureTypesList.Any() ? procedureTypesList : Constants.DefaultProcedureTypes.ToList();
                var dialog = new AddBlockDialog(blockLoader, sections) { Owner = this };
                if (dialog.ShowDialog() != true || dialog.BlockId == null || dialog.Section == null)
                    return ((string?, string?)?)null;
                return (dialog.BlockId, dialog.Section);
            },
            () => DocumentEditor.Document,
            doc => DocumentEditor.Document = doc ?? new FlowDocument());

        vm.RequestSelectBlock = blockId =>
        {
            var entry = vm.Blocks.FirstOrDefault(b =>
                string.Equals(b.BlockId, blockId, System.StringComparison.OrdinalIgnoreCase));
            if (entry != null)
                vm.SelectedBlock = entry;
        };

        DataContext = vm;
    }
}

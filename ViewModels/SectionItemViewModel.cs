using CommunityToolkit.Mvvm.ComponentModel;

namespace InScope.ViewModels;

public partial class SectionItemViewModel : ObservableObject
{
    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;

    public SectionItemViewModel(string name, bool isSelected = false)
    {
        Name = name;
        _isSelected = isSelected;
    }
}

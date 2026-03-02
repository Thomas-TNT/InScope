using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InScope.ViewModels;

public partial class QuestionItemViewModel : ObservableObject
{
    public string Id { get; }
    public string Text { get; }

    /// <summary>
    /// true = Yes, false = No, null = not answered.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(YesCommand))]
    [NotifyCanExecuteChangedFor(nameof(NoCommand))]
    private bool? _selectedAnswer;

    private readonly Action<string, bool> _onAnswer;

    public QuestionItemViewModel(string id, string text, bool? initialAnswer, Action<string, bool> onAnswer)
    {
        Id = id;
        Text = text;
        _selectedAnswer = initialAnswer;
        _onAnswer = onAnswer;
    }

    [RelayCommand(CanExecute = nameof(CanSelectYes))]
    private void Yes()
    {
        SelectedAnswer = true;
        _onAnswer(Id, true);
    }

    [RelayCommand(CanExecute = nameof(CanSelectNo))]
    private void No()
    {
        SelectedAnswer = false;
        _onAnswer(Id, false);
    }

    private bool CanSelectYes() => SelectedAnswer != true;
    private bool CanSelectNo() => SelectedAnswer != false;
}

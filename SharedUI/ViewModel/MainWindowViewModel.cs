namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Service;

using System.Threading.Tasks;

public partial class MainWindowViewModel(IIoService ioService) : ObservableObject
{
    private readonly IIoService _ioService = ioService;

    [ObservableProperty]
    private string title = "MOBAflow";

    [ObservableProperty]
    private string? currentSolutionPath;

    [ObservableProperty]
    private bool hasSolution;

    [ObservableProperty]
    private Solution? solution;

    partial void OnSolutionChanged(Solution? value)
    {
        HasSolution = value is { Projects.Count: > 0 };
        SaveSolutionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        (Solution? _solution, string? path, string? error) = await _ioService.LoadAsync();
        if (!string.IsNullOrEmpty(error))
        {
            // TODO: expose error to UI (add property or messaging)
            return;
        }
        if (_solution != null)
        {
            Solution = _solution;
            CurrentSolutionPath = path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        if (Solution == null) return;
        var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath);
        if (success && path != null)
        {
            CurrentSolutionPath = path;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            // TODO: expose error to UI
        }
    }

    private bool CanSaveSolution() => Solution != null;
}
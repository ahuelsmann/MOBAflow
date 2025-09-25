namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Backend.Model;
using Service;

using System.Threading.Tasks;

public partial class MainWindowViewModel(IIoService ioService) : ObservableObject
{
    private readonly IIoService _ioService = ioService;

    [ObservableProperty]
    public partial string Title { get; set; } = "MOBAflow"; // Generates Title property

    [ObservableProperty]
    public partial string? CurrentSolutionPath { get; set; } // Path of loaded solution

    [ObservableProperty]
    public partial bool HasSolution { get; set; }

    [ObservableProperty]
    public partial Solution? Solution { get; set; } // The loaded solution model

    partial void OnSolutionChanged(Solution? value)
    {
        HasSolution = value is { Projects.Count: > 0 };
        SaveSolutionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        var (solution, path, error) = await _ioService.LoadAsync();
        if (!string.IsNullOrEmpty(error))
        {
            // TODO: expose error to UI (add property or messaging)
            return;
        }
        if (solution != null)
        {
            Solution = solution;
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
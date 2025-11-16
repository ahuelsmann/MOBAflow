namespace Moba.WinUI.ViewModels;

using Backend.Model;
using Moba.SharedUI.Service;

/// <summary>
/// WinUI-specific MainWindowViewModel that uses TreeViewBuilder
/// to create WinUI-specific JourneyViewModel instances.
/// This ensures PropertyChanged events are dispatched to the UI thread (DispatcherQueue).
/// </summary>
public class MainWindowViewModel : SharedUI.ViewModel.MainWindowViewModel
{
    private Solution? _solution;

    public MainWindowViewModel(IIoService ioService) : base(ioService)
    {
    }

    /// <summary>
    /// Overrides Solution property to use WinUI-specific TreeView builder.
    /// </summary>
    public new Solution? Solution
    {
        get => _solution;
        set
        {
            if (SetProperty(ref _solution, value))
            {
                // Update base property
                base.Solution = value;
                
                // Update HasSolution and commands
                HasSolution = value is { Projects.Count: > 0 };
                SaveSolutionCommand.NotifyCanExecuteChanged();
                ConnectToZ21Command.NotifyCanExecuteChanged();
                
                // Use WinUI-specific TreeView builder
                TreeNodes = Services.TreeViewBuilder.BuildTreeView(value);
            }
        }
    }
}

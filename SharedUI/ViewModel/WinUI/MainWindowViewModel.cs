namespace Moba.SharedUI.ViewModel.WinUI;

using Backend.Model;
using Moba.Backend.Interface;
using Moba.SharedUI.Service;

/// <summary>
/// WinUI-specific MainWindowViewModel adapter in SharedUI. Does not reference WinUI-specific services.
/// Platform project may derive from this to add DispatcherQueue-specific behavior.
/// </summary>
public class MainWindowViewModel : ViewModel.MainWindowViewModel
{
    private Solution? _solution;

    public MainWindowViewModel(IIoService ioService, IZ21 z21, IJourneyManagerFactory journeyManagerFactory, TreeViewBuilder treeViewBuilder, IUiDispatcher uiDispatcher)
        : base(ioService, z21, journeyManagerFactory, treeViewBuilder, uiDispatcher)
    {
    }

    public new Solution? Solution
    {
        get => _solution;
        set
        {
            if (SetProperty(ref _solution, value))
            {
                base.Solution = value;

                HasSolution = value is { Projects.Count: > 0 };
                SaveSolutionCommand.NotifyCanExecuteChanged();
                ConnectToZ21Command.NotifyCanExecuteChanged();
            }
        }
    }
}

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
    public MainWindowViewModel(IIoService ioService, IZ21 z21, IJourneyManagerFactory journeyManagerFactory, TreeViewBuilder treeViewBuilder)
        : base(ioService, z21, journeyManagerFactory, treeViewBuilder)
    {
    }
}

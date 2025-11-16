namespace Moba.WinUI.Service;

using Moba.Backend.Model;
using Moba.SharedUI.Service;

public class WinUIJourneyViewModelFactory : IJourneyViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public WinUIJourneyViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public SharedUI.ViewModel.JourneyViewModel Create(Journey model)
        => new SharedUI.ViewModel.WinUI.JourneyViewModel(model, _dispatcher);
}

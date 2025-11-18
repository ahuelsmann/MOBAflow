namespace Moba.WinUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface;

/// <summary>
/// WinUI-specific factory for creating JourneyViewModel instances with UI dispatcher
/// </summary>
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

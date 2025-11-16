namespace Moba.MAUI.Service;

using Moba.Backend.Model;
using Moba.SharedUI.Service;

public class MauiJourneyViewModelFactory : IJourneyViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public MauiJourneyViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public SharedUI.ViewModel.JourneyViewModel Create(Journey model)
        => new SharedUI.ViewModel.MAUI.JourneyViewModel(model, _dispatcher);
}

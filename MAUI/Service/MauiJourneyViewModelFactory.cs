namespace Moba.MAUI.Service;

using Moba.Backend.Model;
using Moba.SharedUI.Service;

public class MauiJourneyViewModelFactory : IJourneyViewModelFactory
{
    private readonly UiDispatcher _dispatcher = new UiDispatcher();

    public SharedUI.ViewModel.JourneyViewModel Create(Journey model)
        => new SharedUI.ViewModel.MAUI.JourneyViewModel(model, _dispatcher);
}

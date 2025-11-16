namespace Moba.WebApp.Service;

using Moba.Backend.Model;
using Moba.SharedUI.Service;

public class WebJourneyViewModelFactory : IJourneyViewModelFactory
{
    public SharedUI.ViewModel.JourneyViewModel Create(Journey model)
        => new SharedUI.ViewModel.JourneyViewModel(model);
}

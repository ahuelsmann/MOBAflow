namespace Moba.WebApp.Service;

using Moba.Backend.Model;
using Moba.SharedUI.Service;

public class WebJourneyViewModelFactory : IJourneyViewModelFactory
{
    public Moba.SharedUI.ViewModel.JourneyViewModel Create(Journey model)
        => new Moba.SharedUI.ViewModel.JourneyViewModel(model);
}

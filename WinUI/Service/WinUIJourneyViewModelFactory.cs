namespace Moba.WinUI.Service;

using Moba.Backend.Model;
using Moba.SharedUI.Service;

public class WinUIJourneyViewModelFactory : IJourneyViewModelFactory
{
    public Moba.SharedUI.ViewModel.JourneyViewModel Create(Journey model)
        => new Moba.SharedUI.ViewModel.WinUI.JourneyViewModel(model);
}

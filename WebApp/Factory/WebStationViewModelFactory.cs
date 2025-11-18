namespace Moba.WebApp.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Blazor-specific factory for creating StationViewModel instances
/// </summary>
public class WebStationViewModelFactory : IStationViewModelFactory
{
    public StationViewModel Create(Station model)
        => new StationViewModel(model, dispatcher: null);
}

namespace Moba.WebApp.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Blazor-specific factory for creating WagonViewModel instances
/// </summary>
public class WebWagonViewModelFactory : IWagonViewModelFactory
{
    public WagonViewModel Create(Wagon model)
        => new WagonViewModel(model, dispatcher: null);
}

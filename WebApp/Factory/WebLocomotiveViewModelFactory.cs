namespace Moba.WebApp.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Blazor-specific factory for creating LocomotiveViewModel instances
/// </summary>
public class WebLocomotiveViewModelFactory : ILocomotiveViewModelFactory
{
    public LocomotiveViewModel Create(Locomotive model)
        => new LocomotiveViewModel(model, dispatcher: null);
}

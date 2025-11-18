namespace Moba.MAUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Service.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// MAUI-specific factory for creating LocomotiveViewModel instances with UI dispatcher
/// </summary>
public class MauiLocomotiveViewModelFactory : ILocomotiveViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public MauiLocomotiveViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public LocomotiveViewModel Create(Locomotive model)
        => new LocomotiveViewModel(model, _dispatcher);
}

namespace Moba.MAUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Service.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// MAUI-specific factory for creating WagonViewModel instances with UI dispatcher
/// </summary>
public class MauiWagonViewModelFactory : IWagonViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public MauiWagonViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public WagonViewModel Create(Wagon model)
        => new WagonViewModel(model, _dispatcher);
}

namespace Moba.MAUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// MAUI-specific factory for creating StationViewModel instances with UI dispatcher
/// </summary>
public class MauiStationViewModelFactory : IStationViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public MauiStationViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public StationViewModel Create(Station model)
        => new StationViewModel(model, _dispatcher);
}

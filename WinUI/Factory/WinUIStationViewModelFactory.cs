namespace Moba.WinUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Service.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// WinUI-specific factory for creating StationViewModel instances with UI dispatcher
/// </summary>
public class WinUIStationViewModelFactory : IStationViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public WinUIStationViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public StationViewModel Create(Station model)
        => new StationViewModel(model, _dispatcher);
}

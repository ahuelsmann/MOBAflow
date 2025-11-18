namespace Moba.WinUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Service.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// WinUI-specific factory for creating TrainViewModel instances with UI dispatcher
/// </summary>
public class WinUITrainViewModelFactory : ITrainViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public WinUITrainViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public TrainViewModel Create(Train model)
        => new TrainViewModel(model, _dispatcher);
}

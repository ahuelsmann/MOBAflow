namespace Moba.MAUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// MAUI-specific factory for creating TrainViewModel instances with UI dispatcher
/// </summary>
public class MauiTrainViewModelFactory : ITrainViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public MauiTrainViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public TrainViewModel Create(Train model)
        => new TrainViewModel(model, _dispatcher);
}

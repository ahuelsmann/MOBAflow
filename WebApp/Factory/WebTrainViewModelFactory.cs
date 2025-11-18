namespace Moba.WebApp.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Blazor-specific factory for creating TrainViewModel instances
/// </summary>
public class WebTrainViewModelFactory : ITrainViewModelFactory
{
    public TrainViewModel Create(Train model)
        => new TrainViewModel(model, dispatcher: null);
}

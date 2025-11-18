namespace Moba.SharedUI.Service.Interface;

using Moba.Backend.Model;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Factory for creating platform-specific TrainViewModel instances
/// </summary>
public interface ITrainViewModelFactory
{
    /// <summary>
    /// Creates a new TrainViewModel for the given Train model
    /// </summary>
    /// <param name="model">The Train model to wrap</param>
    /// <returns>A platform-specific TrainViewModel</returns>
    TrainViewModel Create(Train model);
}

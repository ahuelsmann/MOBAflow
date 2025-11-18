namespace Moba.SharedUI.Interface;

using Moba.Backend.Model;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Factory for creating platform-specific LocomotiveViewModel instances
/// </summary>
public interface ILocomotiveViewModelFactory
{
    /// <summary>
    /// Creates a new LocomotiveViewModel for the given Locomotive model
    /// </summary>
    /// <param name="model">The Locomotive model to wrap</param>
    /// <returns>A platform-specific LocomotiveViewModel</returns>
    LocomotiveViewModel Create(Locomotive model);
}

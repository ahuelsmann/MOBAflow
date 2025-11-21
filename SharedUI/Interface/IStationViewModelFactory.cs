// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Moba.Backend.Model;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Factory for creating platform-specific StationViewModel instances
/// </summary>
public interface IStationViewModelFactory
{
    /// <summary>
    /// Creates a new StationViewModel for the given Station model
    /// </summary>
    /// <param name="model">The Station model to wrap</param>
    /// <returns>A platform-specific StationViewModel</returns>
    StationViewModel Create(Station model);
}

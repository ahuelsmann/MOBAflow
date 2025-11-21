// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Moba.Backend.Model;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Factory for creating platform-specific WagonViewModel instances
/// </summary>
public interface IWagonViewModelFactory
{
    /// <summary>
    /// Creates a new WagonViewModel for the given Wagon model
    /// </summary>
    /// <param name="model">The Wagon model to wrap</param>
    /// <returns>A platform-specific WagonViewModel</returns>
    WagonViewModel Create(Wagon model);
}

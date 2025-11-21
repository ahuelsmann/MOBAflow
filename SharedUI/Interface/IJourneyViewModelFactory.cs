// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Moba.Backend.Model;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Factory for creating platform-specific JourneyViewModel instances
/// </summary>
public interface IJourneyViewModelFactory
{
    /// <summary>
    /// Creates a new JourneyViewModel for the given Journey model
    /// </summary>
    /// <param name="model">The Journey model to wrap</param>
    /// <returns>A platform-specific JourneyViewModel</returns>
    JourneyViewModel Create(Journey model);
}

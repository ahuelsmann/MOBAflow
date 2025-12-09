// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Interface for application preferences service.
/// Provides access to user preferences like auto-load behavior.
/// </summary>
public interface IPreferencesService
{
    /// <summary>
    /// Gets or sets the path to the last loaded solution file.
    /// </summary>
    string? LastSolutionPath { get; set; }

    /// <summary>
    /// Gets or sets whether the last solution should be automatically loaded on startup.
    /// </summary>
    bool AutoLoadLastSolution { get; set; }
}
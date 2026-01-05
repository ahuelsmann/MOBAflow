// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Common.Configuration;

/// <summary>
/// Service interface for reading and writing application settings.
/// Combines application configuration (AppSettings) with user preferences.
/// </summary>
public interface ISettingsService
{
    #region Application Settings
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings GetSettings();

    /// <summary>
    /// Loads settings from appsettings.json.
    /// </summary>
    Task LoadSettingsAsync();

    /// <summary>
    /// Saves settings to appsettings.json.
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Resets settings to default values.
    /// </summary>
    Task ResetToDefaultsAsync();
    #endregion

    #region User Preferences
    /// <summary>
    /// Gets or sets the path to the last loaded solution file.
    /// </summary>
    string? LastSolutionPath { get; set; }

    /// <summary>
    /// Gets or sets whether the last solution should be automatically loaded on startup.
    /// </summary>
    bool AutoLoadLastSolution { get; set; }
    #endregion
}
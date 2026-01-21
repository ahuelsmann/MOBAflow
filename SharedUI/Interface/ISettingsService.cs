// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Common.Configuration;

/// <summary>
/// Service interface for reading and writing application settings.
/// Combines application configuration (AppSettings from appsettings.json) with user preferences.
/// Implementations handle platform-specific storage (Windows Registry, MAUI Preferences, etc.).
/// </summary>
public interface ISettingsService
{
    #region Application Settings
    /// <summary>
    /// Gets the current in-memory application settings.
    /// Call <see cref="LoadSettingsAsync"/> first to ensure settings are loaded from storage.
    /// </summary>
    /// <returns>Current AppSettings instance.</returns>
    AppSettings GetSettings();

    /// <summary>
    /// Loads settings from appsettings.json into memory.
    /// Creates default settings file if it doesn't exist.
    /// Should be called during application startup.
    /// </summary>
    Task LoadSettingsAsync();

    /// <summary>
    /// Saves the provided settings to appsettings.json.
    /// Uses atomic write to prevent data corruption during save.
    /// </summary>
    /// <param name="settings">AppSettings instance to persist.</param>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Resets all settings to their default values and persists them.
    /// User preferences (LastSolutionPath, AutoLoadLastSolution) are also reset.
    /// </summary>
    Task ResetToDefaultsAsync();
    #endregion

    #region User Preferences
    /// <summary>
    /// Gets or sets the path to the last loaded solution file.
    /// Used for auto-loading the last solution on startup.
    /// Stored in platform-specific user preferences storage.
    /// </summary>
    string? LastSolutionPath { get; set; }

    /// <summary>
    /// Gets or sets whether the last solution should be automatically loaded on startup.
    /// When true and <see cref="LastSolutionPath"/> is set, the solution is loaded automatically.
    /// </summary>
    bool AutoLoadLastSolution { get; set; }
    #endregion
}
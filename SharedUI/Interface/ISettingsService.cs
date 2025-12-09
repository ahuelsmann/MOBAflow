// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Moba.Common.Configuration;

/// <summary>
/// Service interface for reading and writing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings GetSettings();

    /// <summary>
    /// Saves settings to appsettings.json.
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Resets settings to default values.
    /// </summary>
    Task ResetToDefaultsAsync();
}
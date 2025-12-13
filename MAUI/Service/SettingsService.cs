// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;

using System.Text.Json;

/// <summary>
/// MAUI-specific settings service for reading and writing application settings.
/// Uses Preferences for simple storage and file system for full settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly AppSettings _settings;
    private readonly string _settingsFilePath;

    public SettingsService(AppSettings settings)
    {
        _settings = settings;
        _settingsFilePath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.json");
    }

    #region Application Settings
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    public AppSettings GetSettings() => _settings;

    /// <summary>
    /// Saves settings to appsettings.json file in app data directory.
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFilePath, json);
            System.Diagnostics.Debug.WriteLine($"✅ MAUI Settings saved to {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Resets settings to default values and saves to file.
    /// </summary>
    public async Task ResetToDefaultsAsync()
    {
        var defaultSettings = new AppSettings();
        await SaveSettingsAsync(defaultSettings);
    }
    #endregion

    #region User Preferences
    /// <summary>
    /// Gets or sets the path to the last loaded solution file.
    /// </summary>
    public string? LastSolutionPath
    {
        get => _settings.Application.LastSolutionPath;
        set
        {
            if (_settings.Application.LastSolutionPath != value)
            {
                _settings.Application.LastSolutionPath = value;
                _ = SaveSettingsAsync(_settings);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the last solution should be automatically loaded on startup.
    /// </summary>
    public bool AutoLoadLastSolution
    {
        get => _settings.Application.AutoLoadLastSolution;
        set
        {
            if (_settings.Application.AutoLoadLastSolution != value)
            {
                _settings.Application.AutoLoadLastSolution = value;
                _ = SaveSettingsAsync(_settings);
            }
        }
    }
    #endregion
}

// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Newtonsoft.Json;
using SharedUI.Interface;
using System.Diagnostics;

/// <summary>
/// Service for reading and writing application settings to appsettings.json.
/// Also handles user preferences (LastSolutionPath, AutoLoadLastSolution).
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly AppSettings _settings;
    private readonly string _settingsFilePath;

    public SettingsService(AppSettings settings)
    {
        _settings = settings;
        _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    #region Application Settings
    /// <summary>
    /// Gets the current application settings from IOptions.
    /// </summary>
    public AppSettings GetSettings()
    {
        return _settings;
    }

    /// <summary>
    /// Saves settings to appsettings.json file.
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            
            Debug.WriteLine($"✅ Settings saved to {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
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
    /// Stored in AppSettings.Application.LastSolutionPath.
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
    /// Stored in AppSettings.Application.AutoLoadLastSolution.
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

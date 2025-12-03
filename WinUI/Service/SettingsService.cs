// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Moba.SharedUI.Interface;
using Moba.Common.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Service for reading and writing application settings to appsettings.json.
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
            
            System.Diagnostics.Debug.WriteLine($"✅ Settings saved to {_settingsFilePath}");
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
}

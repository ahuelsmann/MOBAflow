// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Configuration;
using Common.Discovery;

using SharedUI.Interface;

using System.Text.Json;

/// <summary>
/// MAUI-specific settings service for reading and writing application settings.
/// Uses Preferences for simple storage and file system for full settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly AppSettings _settings;
    private readonly string _settingsFilePath;
    private bool _isLoaded;

    public SettingsService(AppSettings settings)
    {
        _settings = settings;
        _settingsFilePath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.json");

        // ✅ DON'T block constructor - settings will be loaded in App.xaml.cs
        _isLoaded = false;
    }

    #region Application Settings

    /// <summary>
    /// Loads settings from appsettings.json file in app data directory.
    /// If file doesn't exist, uses current default settings.
    /// IMPORTANT: Must be called before using ViewModel!
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _isLoaded = true;
                    return;
                }

                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);

                if (loadedSettings != null)
                {
                    var loadedRestApi = loadedSettings.RestApi;
                    var loadedFeedbackPointCount = Math.Max(loadedSettings.Counter.CountOfFeedbackPoints, 1);

                    // Auto-migrate legacy default port 5000 to 5001 to avoid conflicts
                    if (loadedRestApi.Port == 5000 || loadedRestApi.Port == 0)
                    {
                        loadedRestApi.Port = 5001;
                    }

                    if (string.Equals(
                            loadedRestApi.CurrentIpAddress?.Trim(),
                            RestApiDiscoveryCandidateBuilder.LegacyFactoryDefaultIp,
                            StringComparison.Ordinal))
                    {
                        loadedRestApi.CurrentIpAddress = string.Empty;
                    }

                    loadedRestApi.RecentIpAddresses = (loadedRestApi.RecentIpAddresses ?? [])
                        .Where(ip => !string.Equals(
                            ip?.Trim(),
                            RestApiDiscoveryCandidateBuilder.LegacyFactoryDefaultIp,
                            StringComparison.Ordinal))
                        .ToList();

                    if (loadedSettings.Counter.CountOfFeedbackPoints != loadedFeedbackPointCount)
                    {
                        loadedSettings.Counter.CountOfFeedbackPoints = loadedFeedbackPointCount;
                    }

                    // Copy all loaded values to the DI-registered singleton
                    _settings.Application.LastSolutionPath = loadedSettings.Application.LastSolutionPath;
                    _settings.Application.AutoLoadLastSolution = loadedSettings.Application.AutoLoadLastSolution;
                    _settings.Application.IsDarkMode = loadedSettings.Application.IsDarkMode;
                    _settings.Application.UseSystemTheme = loadedSettings.Application.UseSystemTheme;
                    _settings.Z21.CurrentIpAddress = loadedSettings.Z21.CurrentIpAddress;
                    _settings.Z21.DefaultPort = loadedSettings.Z21.DefaultPort;
                    _settings.Counter.CountOfFeedbackPoints = loadedFeedbackPointCount;
                    _settings.Counter.TargetLapCount = loadedSettings.Counter.TargetLapCount;
                    _settings.Counter.UseTimerFilter = loadedSettings.Counter.UseTimerFilter;
                    _settings.Counter.TimerIntervalSeconds = loadedSettings.Counter.TimerIntervalSeconds;
                    _settings.TrainControl.SelectedPresetIndex = loadedSettings.TrainControl.SelectedPresetIndex;
                    _settings.TrainControl.SpeedRampStepSize = loadedSettings.TrainControl.SpeedRampStepSize;
                    _settings.TrainControl.SpeedRampIntervalMs = loadedSettings.TrainControl.SpeedRampIntervalMs;
                    _settings.TrainControl.SpeedSteps = loadedSettings.TrainControl.SpeedSteps;
                    _settings.TrainControl.SelectedLocoSeries = loadedSettings.TrainControl.SelectedLocoSeries;
                    _settings.TrainControl.SelectedVmax = loadedSettings.TrainControl.SelectedVmax;
                    _settings.TrainControl.SelectedLocomotiveFromProjectId = loadedSettings.TrainControl.SelectedLocomotiveFromProjectId;
                    if (loadedSettings.TrainControl.Presets.Count >= 3)
                    {
                        _settings.TrainControl.Presets = loadedSettings.TrainControl.Presets;
                    }
                    _settings.RestApi.CurrentIpAddress = loadedRestApi.CurrentIpAddress ?? string.Empty;
                    _settings.RestApi.Port = loadedRestApi.Port;
                    _settings.RestApi.RecentIpAddresses = loadedRestApi.RecentIpAddresses;
                    _settings.RestApi.IsConnectionEnabled = loadedRestApi.IsConnectionEnabled;

                    _isLoaded = true;
                }
                else
                {
                    _isLoaded = true;
                }
            }
            else
            {
                // ✅ Create initial settings file with defaults
                await SaveSettingsAsync(_settings).ConfigureAwait(false);
                _isLoaded = true;
            }
        }
        catch (Exception)
        {
            _isLoaded = true; // Don't retry on error
        }
    }

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

            await File.WriteAllTextAsync(_settingsFilePath, json).ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Resets settings to default values and saves to file.
    /// </summary>
    public async Task ResetToDefaultsAsync()
    {
        await SaveSettingsAsync(new AppSettings()).ConfigureAwait(false);
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
            var newValue = value ?? string.Empty;
            if (_settings.Application.LastSolutionPath != newValue)
            {
                _settings.Application.LastSolutionPath = newValue;
                QueueSaveSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the last solution should be automatically loaded.
    /// </summary>
    public bool AutoLoadLastSolution
    {
        get => _settings.Application.AutoLoadLastSolution;
        set
        {
            if (_settings.Application.AutoLoadLastSolution != value)
            {
                _settings.Application.AutoLoadLastSolution = value;
                QueueSaveSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the app is in dark mode.
    /// </summary>
    public bool IsDarkMode
    {
        get => _settings.Application.IsDarkMode;
        set
        {
            if (_settings.Application.IsDarkMode != value)
            {
                _settings.Application.IsDarkMode = value;
                QueueSaveSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the app should use the system theme.
    /// </summary>
    public bool UseSystemTheme
    {
        get => _settings.Application.UseSystemTheme;
        set
        {
            if (_settings.Application.UseSystemTheme != value)
            {
                _settings.Application.UseSystemTheme = value;
                QueueSaveSettings();
            }
        }
    }
    #endregion

    private void QueueSaveSettings()
    {
        SaveSettingsAsync(_settings).ContinueWith(
            _ => { },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
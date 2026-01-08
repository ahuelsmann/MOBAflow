// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Domain;
using Microsoft.Extensions.Logging;
using SharedUI.Interface;
using System.Text.Json;

/// <summary>
/// Service for reading and writing application settings to appsettings.json.
/// Also handles user preferences (LastSolutionPath, AutoLoadLastSolution).
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly AppSettings _settings;
    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly Timer _debounceTimer;
    private readonly ILogger<SettingsService> _logger;
    private bool _hasPendingSave;

    public SettingsService(AppSettings settings, ILogger<SettingsService> logger)
    {
        _settings = settings;
        _logger = logger;
        _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
        
        // ✅ Load settings synchronously to avoid deadlock
        // WinUI 3 Desktop apps cannot use async in DI constructors
        LoadSettingsSync();
    }

    /// <summary>
    /// Loads settings synchronously from appsettings.json (constructor-safe).
    /// </summary>
    private void LoadSettingsSync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions.Default);

                if (loadedSettings != null)
                {
                    // Copy all loaded values to the DI-registered singleton
                    _settings.Application.LastSolutionPath = loadedSettings.Application.LastSolutionPath;
                    _settings.Application.AutoLoadLastSolution = loadedSettings.Application.AutoLoadLastSolution;
                    _settings.Application.AutoStartWebApp = loadedSettings.Application.AutoStartWebApp;
                    _settings.Z21.CurrentIpAddress = loadedSettings.Z21.CurrentIpAddress;
                    _settings.Z21.DefaultPort = loadedSettings.Z21.DefaultPort;
                    _settings.Counter.CountOfFeedbackPoints = loadedSettings.Counter.CountOfFeedbackPoints;
                    _settings.Counter.TargetLapCount = loadedSettings.Counter.TargetLapCount;
                    _settings.Counter.UseTimerFilter = loadedSettings.Counter.UseTimerFilter;
                    _settings.Counter.TimerIntervalSeconds = loadedSettings.Counter.TimerIntervalSeconds;

                    // TrainControl settings (locomotive presets)
                    _settings.TrainControl.SelectedPresetIndex = loadedSettings.TrainControl.SelectedPresetIndex;
                    _settings.TrainControl.SpeedRampStepSize = loadedSettings.TrainControl.SpeedRampStepSize;
                    _settings.TrainControl.SpeedRampIntervalMs = loadedSettings.TrainControl.SpeedRampIntervalMs;
                    if (loadedSettings.TrainControl.Presets.Count >= 3)
                    {
                        _settings.TrainControl.Presets = loadedSettings.TrainControl.Presets;
                    }

                    _logger.LogInformation("WinUI settings loaded from {SettingsFilePath}", _settingsFilePath);
                }
            }
            else
            {
                _logger.LogWarning("Settings file not found: {SettingsFilePath} - using defaults", _settingsFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings synchronously");
            // Continue with default settings
        }
    }

    #region Application Settings
    /// <summary>
    /// Loads settings from appsettings.json file.
    /// If file doesn't exist, uses current default settings.
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions.Default);
                
                if (loadedSettings != null)
                {
                    // Copy all loaded values to the DI-registered singleton
                    _settings.Application.LastSolutionPath = loadedSettings.Application.LastSolutionPath;
                    _settings.Application.AutoLoadLastSolution = loadedSettings.Application.AutoLoadLastSolution;
                    _settings.Application.AutoStartWebApp = loadedSettings.Application.AutoStartWebApp;
                    _settings.Z21.CurrentIpAddress = loadedSettings.Z21.CurrentIpAddress;
                    _settings.Z21.DefaultPort = loadedSettings.Z21.DefaultPort;
                            _settings.Counter.CountOfFeedbackPoints = loadedSettings.Counter.CountOfFeedbackPoints;
                            _settings.Counter.TargetLapCount = loadedSettings.Counter.TargetLapCount;
                            _settings.Counter.UseTimerFilter = loadedSettings.Counter.UseTimerFilter;
                            _settings.Counter.TimerIntervalSeconds = loadedSettings.Counter.TimerIntervalSeconds;

                            // TrainControl settings (locomotive presets)
                            _settings.TrainControl.SelectedPresetIndex = loadedSettings.TrainControl.SelectedPresetIndex;
                            _settings.TrainControl.SpeedRampStepSize = loadedSettings.TrainControl.SpeedRampStepSize;
                            _settings.TrainControl.SpeedRampIntervalMs = loadedSettings.TrainControl.SpeedRampIntervalMs;
                            if (loadedSettings.TrainControl.Presets.Count >= 3)
                            {
                                _settings.TrainControl.Presets = loadedSettings.TrainControl.Presets;
                            }

                            _logger.LogInformation("WinUI settings loaded from {SettingsFilePath}", _settingsFilePath);
                        }

                    }
                    else
                    {
                        _logger.LogInformation("No settings file found at {SettingsFilePath}, using defaults", _settingsFilePath);
                    }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
        }
    }
    
    /// <summary>
    /// Gets the current application settings from IOptions.
    /// </summary>
    public AppSettings GetSettings()
    {
        return _settings;
    }

    /// <summary>
    /// Saves settings to appsettings.json file with debouncing (500ms delay).
    /// Multiple rapid calls are batched into a single write operation.
    /// </summary>
    public Task SaveSettingsAsync(AppSettings settings)
    {
        _hasPendingSave = true;
        _debounceTimer.Change(500, Timeout.Infinite); // 500ms debounce
        return Task.CompletedTask;
    }

    private async void OnDebounceElapsed(object? state)
    {
        if (!_hasPendingSave)
            return;

        _hasPendingSave = false;

        await _saveLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_settings, JsonOptions.Default);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _logger.LogInformation("Settings saved to {SettingsFilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
        finally
        {
            _saveLock.Release();
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
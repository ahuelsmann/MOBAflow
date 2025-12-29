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
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly Timer _debounceTimer;
    private bool _hasPendingSave;

    public SettingsService(AppSettings settings)
    {
        _settings = settings;
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
                var loadedSettings = JsonConvert.DeserializeObject<AppSettings>(json);
                
                if (loadedSettings != null)
                {
                    // Copy all loaded values to the DI-registered singleton
                    _settings.Application.LastSolutionPath = loadedSettings.Application.LastSolutionPath;
                    _settings.Application.AutoLoadLastSolution = loadedSettings.Application.AutoLoadLastSolution;
                    _settings.Z21.CurrentIpAddress = loadedSettings.Z21.CurrentIpAddress;
                    _settings.Z21.DefaultPort = loadedSettings.Z21.DefaultPort;
                    _settings.Counter.CountOfFeedbackPoints = loadedSettings.Counter.CountOfFeedbackPoints;
                    _settings.Counter.TargetLapCount = loadedSettings.Counter.TargetLapCount;
                    _settings.Counter.UseTimerFilter = loadedSettings.Counter.UseTimerFilter;
                    _settings.Counter.TimerIntervalSeconds = loadedSettings.Counter.TimerIntervalSeconds;
                    
                    Debug.WriteLine($"✅ WinUI Settings loaded from {_settingsFilePath}");
                }
            }
            else
            {
                Debug.WriteLine($"⚠️ Settings file not found: {_settingsFilePath} - using defaults");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to load settings: {ex.Message}");
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
                var loadedSettings = JsonConvert.DeserializeObject<AppSettings>(json);
                
                if (loadedSettings != null)
                {
                    // Copy all loaded values to the DI-registered singleton
                    _settings.Application.LastSolutionPath = loadedSettings.Application.LastSolutionPath;
                    _settings.Application.AutoLoadLastSolution = loadedSettings.Application.AutoLoadLastSolution;
                    _settings.Z21.CurrentIpAddress = loadedSettings.Z21.CurrentIpAddress;
                    _settings.Z21.DefaultPort = loadedSettings.Z21.DefaultPort;
                    _settings.Counter.CountOfFeedbackPoints = loadedSettings.Counter.CountOfFeedbackPoints;
                    _settings.Counter.TargetLapCount = loadedSettings.Counter.TargetLapCount;
                    _settings.Counter.UseTimerFilter = loadedSettings.Counter.UseTimerFilter;
                    _settings.Counter.TimerIntervalSeconds = loadedSettings.Counter.TimerIntervalSeconds;
                    
                    Debug.WriteLine($"✅ WinUI Settings loaded from {_settingsFilePath}");
                }
            }
            else
            {
                Debug.WriteLine($"ℹ️ No settings file found at {_settingsFilePath}, using defaults");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Failed to load settings: {ex.Message}, using defaults");
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
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            Debug.WriteLine($"✅ Settings saved to {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
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
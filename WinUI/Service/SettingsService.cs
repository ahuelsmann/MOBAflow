// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Common.Extension;

using Domain;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// Service for reading and writing application settings to appsettings.json.
/// Also handles user preferences (LastSolutionPath, AutoLoadLastSolution).
/// </summary>
internal class SettingsService : ISettingsService
{
    private readonly AppSettings _settings;
    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(AppSettings settings, ILogger<SettingsService> logger)
    {
        _settings = settings;
        _logger = logger;
        _settingsFilePath = ResolveSettingsFilePath();

        // Load settings synchronously to avoid deadlock
        LoadSettingsSync();
    }

    private static string ResolveSettingsFilePath()
    {
#if DEBUG
        if (Debugger.IsAttached)
        {
            var projectPath = FindProjectSettingsPath("appsettings.Development.json");
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                return projectPath;
            }
        }
#endif
        return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    private static string? FindProjectSettingsPath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, fileName);
            if (File.Exists(candidate) && File.Exists(Path.Combine(current.FullName, "WinUI.csproj")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Copies all property values from a deserialized AppSettings to the DI-registered singleton.
    /// Single source of truth for property mapping - used by both sync and async load paths.
    /// </summary>
    private void ApplyLoadedSettings(AppSettings source)
    {
        _settings.Application.LastSolutionPath = source.Application.LastSolutionPath;
        _settings.Application.AutoLoadLastSolution = source.Application.AutoLoadLastSolution;
        _settings.Application.AutoStartWebApp = source.Application.AutoStartWebApp;
        _settings.Application.SelectedSkin = source.Application.SelectedSkin;
        _settings.Application.IsDarkMode = source.Application.IsDarkMode;
        _settings.Application.UseSystemTheme = source.Application.UseSystemTheme;

        _settings.Z21.CurrentIpAddress = source.Z21.CurrentIpAddress;
        _settings.Z21.DefaultPort = source.Z21.DefaultPort;

        _settings.Counter.CountOfFeedbackPoints = source.Counter.CountOfFeedbackPoints;
        _settings.Counter.TargetLapCount = source.Counter.TargetLapCount;
        _settings.Counter.UseTimerFilter = source.Counter.UseTimerFilter;
        _settings.Counter.TimerIntervalSeconds = source.Counter.TimerIntervalSeconds;

        _settings.TrainControl.SelectedPresetIndex = source.TrainControl.SelectedPresetIndex;
        _settings.TrainControl.SpeedRampStepSize = source.TrainControl.SpeedRampStepSize;
        _settings.TrainControl.SpeedRampIntervalMs = source.TrainControl.SpeedRampIntervalMs;
        _settings.TrainControl.SpeedSteps = source.TrainControl.SpeedSteps;
        _settings.TrainControl.SelectedLocoSeries = source.TrainControl.SelectedLocoSeries;
        _settings.TrainControl.SelectedVmax = source.TrainControl.SelectedVmax;
        _settings.TrainControl.SelectedLocomotiveFromProjectId = source.TrainControl.SelectedLocomotiveFromProjectId;
        if (source.TrainControl.Presets.Count >= 3)
        {
            _settings.TrainControl.Presets = source.TrainControl.Presets;
        }

        _settings.FeatureToggles = source.FeatureToggles;
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
                    ApplyLoadedSettings(loadedSettings);
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
                    ApplyLoadedSettings(loadedSettings);
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
    /// Saves settings to appsettings.json file immediately.
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
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
            var nonNullValue = value ?? string.Empty;
            if (_settings.Application.LastSolutionPath != nonNullValue)
            {
                _settings.Application.LastSolutionPath = nonNullValue;
                SaveSettingsAsync(_settings)
                    .SafeFireAndForget(ex => _logger.LogError(ex, "Failed to save LastSolutionPath"));
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
                SaveSettingsAsync(_settings)
                    .SafeFireAndForget(ex => _logger.LogError(ex, "Failed to save AutoLoadLastSolution"));
            }
        }
    }
    #endregion
}
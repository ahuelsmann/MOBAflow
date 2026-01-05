// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SettingsService> _logger;
    private bool _isLoaded;

    public SettingsService(AppSettings settings, ILogger<SettingsService> logger)
    {
        _settings = settings;
        _logger = logger;
        _settingsFilePath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.json");

        _logger.LogInformation("SettingsService initialized. FilePath: {SettingsFilePath}; AppDataDirectory: {AppDataDirectory}", _settingsFilePath, FileSystem.AppDataDirectory);

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
            _logger.LogDebug("Settings already loaded, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("LoadSettingsAsync: Checking file existence at {SettingsFilePath}; Exists: {Exists}", _settingsFilePath, File.Exists(_settingsFilePath));

            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
                _logger.LogDebug("Settings file length: {Length} chars", json.Length);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("Settings file is empty, using defaults");
                    _isLoaded = true;
                    return;
                }

                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);

                if (loadedSettings != null)
                {
                    var loadedRestApi = loadedSettings.RestApi;

                    // Auto-migrate legacy default port 5000 to 5001 to avoid conflicts
                    if (loadedRestApi.Port == 5000 || loadedRestApi.Port == 0)
                    {
                        _logger.LogWarning("REST API port {LegacyPort} detected (legacy default) - migrating to {NewPort}", loadedRestApi.Port, 5001);
                        loadedRestApi.Port = 5001;
                    }

                    _logger.LogInformation("Deserializing settings: Tracks {Tracks}; Target {Target}; Timer {Timer}s; Z21 IP {Z21Ip}; REST IP {RestIp}; REST Port {RestPort}",
                        loadedSettings.Counter.CountOfFeedbackPoints,
                        loadedSettings.Counter.TargetLapCount,
                        loadedSettings.Counter.TimerIntervalSeconds,
                        loadedSettings.Z21.CurrentIpAddress,
                        loadedRestApi.CurrentIpAddress,
                        loadedRestApi.Port);

                    // Copy all loaded values to the DI-registered singleton
                    _settings.Application.LastSolutionPath = loadedSettings.Application.LastSolutionPath;
                    _settings.Application.AutoLoadLastSolution = loadedSettings.Application.AutoLoadLastSolution;
                    _settings.Z21.CurrentIpAddress = loadedSettings.Z21.CurrentIpAddress;
                    _settings.Z21.DefaultPort = loadedSettings.Z21.DefaultPort;
                    _settings.Counter.CountOfFeedbackPoints = loadedSettings.Counter.CountOfFeedbackPoints;
                    _settings.Counter.TargetLapCount = loadedSettings.Counter.TargetLapCount;
                    _settings.Counter.UseTimerFilter = loadedSettings.Counter.UseTimerFilter;
                    _settings.Counter.TimerIntervalSeconds = loadedSettings.Counter.TimerIntervalSeconds;
                    _settings.RestApi.CurrentIpAddress = loadedRestApi.CurrentIpAddress;
                    _settings.RestApi.Port = loadedRestApi.Port;
                    _settings.RestApi.RecentIpAddresses = loadedRestApi.RecentIpAddresses;

                    _logger.LogInformation("Settings applied to singleton");
                    _isLoaded = true;
                }
                else
                {
                    _logger.LogWarning("Deserialization returned null");
                    _isLoaded = true;
                }
            }
            else
            {
                _logger.LogInformation("No settings file found, using defaults. Tracks {Tracks}; Target {Target}; Timer {Timer}s; REST IP {RestIp}",
                    _settings.Counter.CountOfFeedbackPoints,
                    _settings.Counter.TargetLapCount,
                    _settings.Counter.TimerIntervalSeconds,
                    _settings.RestApi.CurrentIpAddress);
                
                // ✅ Create initial settings file with defaults
                _logger.LogInformation("Creating initial settings file...");
                await SaveSettingsAsync(_settings).ConfigureAwait(false);
                _isLoaded = true;
            }

            _logger.LogInformation("SettingsService initialized. Tracks {Tracks}; Target {Target}; Timer Filter {TimerFilter}; Timer Interval {TimerInterval}s; Z21 IP {Z21Ip}; REST IP {RestIp}",
                _settings.Counter.CountOfFeedbackPoints,
                _settings.Counter.TargetLapCount,
                _settings.Counter.UseTimerFilter,
                _settings.Counter.TimerIntervalSeconds,
                _settings.Z21.CurrentIpAddress,
                _settings.RestApi.CurrentIpAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
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

            _logger.LogInformation("SaveSettingsAsync called. Path: {SettingsFilePath}; Tracks {Tracks}; Target {Target}; Timer {Timer}s; Z21 IP {Z21Ip}; REST IP {RestIp}; REST Port {RestPort}",
                _settingsFilePath,
                settings.Counter.CountOfFeedbackPoints,
                settings.Counter.TargetLapCount,
                settings.Counter.TimerIntervalSeconds,
                settings.Z21.CurrentIpAddress,
                settings.RestApi.CurrentIpAddress,
                settings.RestApi.Port);

            await File.WriteAllTextAsync(_settingsFilePath, json).ConfigureAwait(false);

            var fileInfo = new FileInfo(_settingsFilePath);
            _logger.LogInformation("Settings saved successfully. File size: {FileSize} bytes; Last modified: {LastModified}", fileInfo.Length, fileInfo.LastWriteTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
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
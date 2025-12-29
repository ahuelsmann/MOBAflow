// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Configuration;
using SharedUI.Interface;
using System.Diagnostics;
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

        Debug.WriteLine("═══════════════════════════════════════════════════════");
        Debug.WriteLine("🔧 SettingsService Constructor");
        Debug.WriteLine($"   File path: {_settingsFilePath}");
        Debug.WriteLine($"   AppDataDirectory: {FileSystem.AppDataDirectory}");
        Debug.WriteLine("═══════════════════════════════════════════════════════");

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
            Debug.WriteLine("⚠️ Settings already loaded, skipping");
            return;
        }

        try
        {
            Debug.WriteLine("📂 LoadSettingsAsync: Checking file existence...");
            Debug.WriteLine($"   Path: {_settingsFilePath}");
            Debug.WriteLine($"   Exists: {File.Exists(_settingsFilePath)}");

            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
                Debug.WriteLine($"📄 File content length: {json.Length} chars");
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.WriteLine("⚠️ File is empty, using defaults");
                    _isLoaded = true;
                    return;
                }

                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);

                if (loadedSettings != null)
                {
                    Debug.WriteLine("📦 Deserializing settings...");
                    Debug.WriteLine($"   Loaded Tracks: {loadedSettings.Counter.CountOfFeedbackPoints}");
                    Debug.WriteLine($"   Loaded Target: {loadedSettings.Counter.TargetLapCount}");
                    Debug.WriteLine($"   Loaded Timer: {loadedSettings.Counter.TimerIntervalSeconds}s");
                    Debug.WriteLine($"   Loaded IP: {loadedSettings.Z21.CurrentIpAddress}");

                    // Copy all loaded values to the DI-registered singleton
                    _settings.Application.LastSolutionPath = loadedSettings.Application.LastSolutionPath;
                    _settings.Application.AutoLoadLastSolution = loadedSettings.Application.AutoLoadLastSolution;
                    _settings.Z21.CurrentIpAddress = loadedSettings.Z21.CurrentIpAddress;
                    _settings.Z21.DefaultPort = loadedSettings.Z21.DefaultPort;
                    _settings.Counter.CountOfFeedbackPoints = loadedSettings.Counter.CountOfFeedbackPoints;
                    _settings.Counter.TargetLapCount = loadedSettings.Counter.TargetLapCount;
                    _settings.Counter.UseTimerFilter = loadedSettings.Counter.UseTimerFilter;
                    _settings.Counter.TimerIntervalSeconds = loadedSettings.Counter.TimerIntervalSeconds;

                    Debug.WriteLine("✅ Settings applied to singleton");
                    _isLoaded = true;
                }
                else
                {
                    Debug.WriteLine("⚠️ Deserialization returned null!");
                    _isLoaded = true;
                }
            }
            else
            {
                Debug.WriteLine("ℹ️ No settings file found, using defaults");
                Debug.WriteLine($"   Default Tracks: {_settings.Counter.CountOfFeedbackPoints}");
                Debug.WriteLine($"   Default Target: {_settings.Counter.TargetLapCount}");
                Debug.WriteLine($"   Default Timer: {_settings.Counter.TimerIntervalSeconds}s");
                
                // ✅ Create initial settings file with defaults
                Debug.WriteLine("💾 Creating initial settings file...");
                await SaveSettingsAsync(_settings).ConfigureAwait(false);
                _isLoaded = true;
            }

            Debug.WriteLine("═══════════════════════════════════════════════════════");
            Debug.WriteLine("✅ SettingsService Initialized");
            Debug.WriteLine($"   Tracks: {_settings.Counter.CountOfFeedbackPoints}");
            Debug.WriteLine($"   Target: {_settings.Counter.TargetLapCount}");
            Debug.WriteLine($"   Timer Filter: {_settings.Counter.UseTimerFilter}");
            Debug.WriteLine($"   Timer Interval: {_settings.Counter.TimerIntervalSeconds}s");
            Debug.WriteLine($"   IP Address: {_settings.Z21.CurrentIpAddress}");
            Debug.WriteLine("═══════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to load settings: {ex.Message}");
            Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
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

            Debug.WriteLine("💾 SaveSettingsAsync called");
            Debug.WriteLine($"   Path: {_settingsFilePath}");
            Debug.WriteLine($"   Tracks: {settings.Counter.CountOfFeedbackPoints}");
            Debug.WriteLine($"   Target: {settings.Counter.TargetLapCount}");
            Debug.WriteLine($"   Timer: {settings.Counter.TimerIntervalSeconds}s");
            Debug.WriteLine($"   IP: {settings.Z21.CurrentIpAddress}");

            await File.WriteAllTextAsync(_settingsFilePath, json).ConfigureAwait(false);

            var fileInfo = new FileInfo(_settingsFilePath);
            Debug.WriteLine("✅ Settings saved successfully");
            Debug.WriteLine($"   File size: {fileInfo.Length} bytes");
            Debug.WriteLine($"   Last modified: {fileInfo.LastWriteTime}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
            Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
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
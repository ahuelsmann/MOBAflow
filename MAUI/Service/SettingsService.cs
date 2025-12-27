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

    public SettingsService(AppSettings settings)
    {
        _settings = settings;
        _settingsFilePath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.json");

        // ✅ Load settings from file on startup
        _ = LoadSettingsAsync();
    }

    #region Application Settings

    /// <summary>
    /// Loads settings from appsettings.json file in app data directory.
    /// If file doesn't exist, uses current default settings.
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        try
        {
            // ✅ DIAGNOSTIC: Log file path and existence
            Debug.WriteLine($"📂 Settings path: {_settingsFilePath}");
            Debug.WriteLine($"📂 File exists: {File.Exists(_settingsFilePath)}");

            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                Debug.WriteLine($"📄 File content length: {json.Length} chars");
                Debug.WriteLine($"📄 JSON Content (first 500 chars):");
                Debug.WriteLine(json.Length > 500 ? json.Substring(0, 500) + "..." : json);

                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);

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

                    Debug.WriteLine($"✅ MAUI Settings loaded from {_settingsFilePath}");
                    Debug.WriteLine($"   IP: {loadedSettings.Z21.CurrentIpAddress}");
                    Debug.WriteLine($"   Tracks: {loadedSettings.Counter.CountOfFeedbackPoints}");
                    Debug.WriteLine($"   Target: {loadedSettings.Counter.TargetLapCount}");
                    Debug.WriteLine($"   Timer: {loadedSettings.Counter.TimerIntervalSeconds}s");
                }
                else
                {
                    Debug.WriteLine($"⚠️ Deserialization returned null!");
                }
            }
            else
            {
                Debug.WriteLine($"ℹ️ No settings file found at {_settingsFilePath}, using defaults");

                // ✅ DIAGNOSTIC: Try to list directory contents
                try
                {
                    var dir = Path.GetDirectoryName(_settingsFilePath);
                    if (Directory.Exists(dir))
                    {
                        var files = Directory.GetFiles(dir!);
                        Debug.WriteLine($"📂 Files in {dir}: {files.Length}");
                        foreach (var file in files)
                        {
                            Debug.WriteLine($"   - {Path.GetFileName(file)}");
                        }
                    }
                }
                catch (Exception dirEx)
                {
                    Debug.WriteLine($"⚠️ Could not list directory: {dirEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Failed to load settings: {ex.Message}, using defaults");
            Debug.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
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

            // ✅ DIAGNOSTIC: Log save operation
            Debug.WriteLine($"💾 Saving settings to {_settingsFilePath}");
            Debug.WriteLine($"💾 JSON length: {json.Length} chars");

            await File.WriteAllTextAsync(_settingsFilePath, json);

            // ✅ DIAGNOSTIC: Verify file was written
            var fileInfo = new FileInfo(_settingsFilePath);
            Debug.WriteLine($"✅ MAUI Settings saved to {_settingsFilePath}");
            Debug.WriteLine($"   File size: {fileInfo.Length} bytes");
            Debug.WriteLine($"   Last modified: {fileInfo.LastWriteTime}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
            Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
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
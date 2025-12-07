// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Moba.SharedUI.Interface;

using Newtonsoft.Json;

using System;
using System.IO;

/// <summary>
/// Service for persisting application preferences to disk.
/// Stores settings like last solution path and auto-load behavior.
/// </summary>
public class PreferencesService : IPreferencesService
{
    private readonly string _preferencesFilePath;
    private Preferences? _preferences;

    public PreferencesService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MOBAflow");
        
        Directory.CreateDirectory(appDataPath);
        _preferencesFilePath = Path.Combine(appDataPath, "preferences.json");
    }

    /// <summary>
    /// Gets the path to the last loaded solution file.
    /// Returns null if no solution was previously loaded.
    /// </summary>
    public string? LastSolutionPath
    {
        get => LoadPreferences().LastSolutionPath;
        set
        {
            var prefs = LoadPreferences();
            prefs.LastSolutionPath = value;
            SavePreferences(prefs);
        }
    }

    /// <summary>
    /// Gets or sets whether the last solution should be automatically loaded on startup.
    /// Default is true.
    /// </summary>
    public bool AutoLoadLastSolution
    {
        get => LoadPreferences().AutoLoadLastSolution;
        set
        {
            var prefs = LoadPreferences();
            prefs.AutoLoadLastSolution = value;
            SavePreferences(prefs);
        }
    }

    /// <summary>
    /// Loads preferences from disk. Returns default preferences if file doesn't exist.
    /// </summary>
    private Preferences LoadPreferences()
    {
        if (_preferences != null)
            return _preferences;

        try
        {
            if (File.Exists(_preferencesFilePath))
            {
                var json = File.ReadAllText(_preferencesFilePath);
                _preferences = JsonConvert.DeserializeObject<Preferences>(json) ?? new Preferences();
            }
            else
            {
                _preferences = new Preferences();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Failed to load preferences: {ex.Message}");
            _preferences = new Preferences();
        }

        return _preferences;
    }

    /// <summary>
    /// Saves preferences to disk.
    /// </summary>
    private void SavePreferences(Preferences preferences)
    {
        try
        {
            var json = JsonConvert.SerializeObject(preferences, Formatting.Indented);
            File.WriteAllText(_preferencesFilePath, json);
            _preferences = preferences;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Failed to save preferences: {ex.Message}");
        }
    }
}

/// <summary>
/// Model class for application preferences.
/// </summary>
internal class Preferences
{
    /// <summary>
    /// Path to the last loaded solution file.
    /// </summary>
    public string? LastSolutionPath { get; set; }

    /// <summary>
    /// Whether to automatically load the last solution on startup.
    /// </summary>
    public bool AutoLoadLastSolution { get; set; } = true;
}
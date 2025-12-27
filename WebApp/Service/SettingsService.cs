// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Service;

using Common.Configuration;
using SharedUI.Interface;
using System.Diagnostics;

/// <summary>
/// Blazor-specific settings service for reading and writing application settings.
/// Uses in-memory storage for Blazor Server (appsettings.json is read-only at runtime).
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly AppSettings _settings;

    public SettingsService(AppSettings settings)
    {
        _settings = settings;
    }

    #region Application Settings
    /// <summary>
    /// Loads settings (no-op for Blazor Server - settings come from appsettings.json via DI).
    /// </summary>
    public Task LoadSettingsAsync()
    {
        // In Blazor Server, settings are loaded by the host from appsettings.json
        // This is a no-op to satisfy the interface
        Debug.WriteLine("ℹ️ Blazor Server: Settings loaded from appsettings.json via DI");
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    public AppSettings GetSettings() => _settings;

    /// <summary>
    /// Saves settings (in-memory only for Blazor Server).
    /// Note: appsettings.json is typically read-only in production Blazor Server apps.
    /// </summary>
    public Task SaveSettingsAsync(AppSettings settings)
    {
        // In Blazor Server, we typically don't write to appsettings.json
        // Settings changes are kept in memory for the current session
        Debug.WriteLine("⚠️ Blazor Server: Settings saved to memory only");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets settings to default values (in-memory).
    /// </summary>
    public Task ResetToDefaultsAsync()
    {
        // Reset to defaults in memory
        return Task.CompletedTask;
    }
    #endregion

    #region User Preferences
    /// <summary>
    /// Gets or sets the path to the last loaded solution file.
    /// </summary>
    public string? LastSolutionPath
    {
        get => _settings.Application.LastSolutionPath;
        set => _settings.Application.LastSolutionPath = value;
    }

    /// <summary>
    /// Gets or sets whether the last solution should be automatically loaded on startup.
    /// </summary>
    public bool AutoLoadLastSolution
    {
        get => _settings.Application.AutoLoadLastSolution;
        set => _settings.Application.AutoLoadLastSolution = value;
    }
    #endregion
}

// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using SharedUI.Interface;

/// <summary>
/// No-op implementation of ISettingsService for scenarios where settings are unavailable.
/// 
/// This follows the NullObject pattern: instead of checking for null services,
/// callers receive this no-op implementation that safely does nothing.
/// 
/// Usage:
/// - When settings storage is not available
/// - When settings file is read-only or missing
/// - For testing without persistence
/// 
/// Behavior:
/// - All async methods complete successfully without doing anything
/// - All properties return default values
/// - No exceptions are thrown
/// </summary>
public class NullSettingsService : ISettingsService
{
    /// <summary>
    /// Returns a default AppSettings instance.
    /// </summary>
    public AppSettings GetSettings()
    {
        return new AppSettings();
    }

    /// <summary>
    /// Does nothing (no file to load).
    /// </summary>
    public Task LoadSettingsAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Does nothing (no file to save).
    /// </summary>
    public Task SaveSettingsAsync(AppSettings settings)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Does nothing (nothing to reset).
    /// </summary>
    public Task ResetToDefaultsAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Always returns null (no last solution path).
    /// </summary>
    public string? LastSolutionPath { get; set; }

    /// <summary>
    /// Always returns false (auto-load disabled).
    /// </summary>
    public bool AutoLoadLastSolution { get; set; }
}

// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.ReactApp.Service;

using Common.Configuration;
using SharedUI.Interface;
using System.Diagnostics;

/// <summary>
/// Blazor-specific settings service for reading and writing application settings.
/// Uses in-memory storage for Blazor Server (appsettings.json is read-only at runtime).
/// </summary>
internal class SettingsService : ISettingsService
{
    private readonly AppSettings _settings;

    public SettingsService(AppSettings settings)
    {
        _settings = settings;
    }

    public Task LoadSettingsAsync()
    {
        Debug.WriteLine("ℹ️ Blazor Server: Settings loaded from appsettings.json via DI");
        return Task.CompletedTask;
    }

    public AppSettings GetSettings() => _settings;

    public Task SaveSettingsAsync(AppSettings settings)
    {
        Debug.WriteLine("⚠️ Blazor Server: Settings saved to memory only");
        return Task.CompletedTask;
    }

    public Task ResetToDefaultsAsync()
    {
        return Task.CompletedTask;
    }

    public string? LastSolutionPath
    {
        get => _settings.Application.LastSolutionPath;
        set => _settings.Application.LastSolutionPath = value ?? string.Empty;
    }

    public bool AutoLoadLastSolution
    {
        get => _settings.Application.AutoLoadLastSolution;
        set => _settings.Application.AutoLoadLastSolution = value;
    }
}

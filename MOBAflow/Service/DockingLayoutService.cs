// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Controls.Docking.Workspace;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;

/// <summary>
/// Service for persisting and restoring DockingManager layouts.
/// Saves layout state to JSON file in LocalAppData.
/// </summary>
public class DockingLayoutService
{
    #region Constants

    private const string LayoutFileName = "docking-layout.json";
    private const int CurrentLayoutVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    #endregion

    #region Fields

    private readonly string _layoutFilePath;
    private readonly ILogger<DockingLayoutService> _logger;

    #endregion

    public DockingLayoutService(ILogger<DockingLayoutService> logger)
    {
        _logger = logger;
        _layoutFilePath = Path.Combine(GetLocalDataDirectory(), LayoutFileName);
    }

    #region Public Methods

    /// <summary>
    /// Loads the last saved layout.
    /// </summary>
    public async Task<DockingWorkspaceState?> LoadLastLayoutAsync()
    {
        try
        {
            if (!File.Exists(_layoutFilePath))
                return null;

            var json = await File.ReadAllTextAsync(_layoutFilePath);
            var state = JsonSerializer.Deserialize<DockingWorkspaceState>(json, JsonOptions);

            // Check version compatibility
            if (state?.Version != CurrentLayoutVersion)
                return null;

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading layout");
            return null;
        }
    }

    /// <summary>
    /// Saves the current layout.
    /// </summary>
    public async Task SaveLayoutAsync(DockingWorkspaceState state)
    {
        try
        {
            state.Version = CurrentLayoutVersion;

            var json = JsonSerializer.Serialize(state, JsonOptions);
            var directoryPath = Path.GetDirectoryName(_layoutFilePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await File.WriteAllTextAsync(_layoutFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving layout");
        }
    }

    /// <summary>
    /// Deletes the saved layout.
    /// </summary>
    public async Task DeleteLayoutAsync()
    {
        try
        {
            if (File.Exists(_layoutFilePath))
            {
                File.Delete(_layoutFilePath);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deleting layout");
        }
    }

    private string GetLocalDataDirectory()
    {
        try
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ApplicationData.Current.LocalFolder unavailable, falling back to LocalApplicationData.");
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MOBAflow");
        }
    }

    #endregion
}

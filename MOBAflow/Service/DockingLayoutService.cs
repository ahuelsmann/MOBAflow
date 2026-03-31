// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Controls.Docking.Workspace;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;

/// <summary>
/// Service for persisting and restoring DockingManager layouts.
/// Saves layout state to JSON file in LocalAppData.
/// </summary>
internal class DockingLayoutService
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

    private readonly StorageFolder _localAppDataFolder;
    private readonly ILogger<DockingLayoutService> _logger;

    #endregion

    public DockingLayoutService(ILogger<DockingLayoutService> logger)
    {
        _localAppDataFolder = ApplicationData.Current.LocalFolder;
        _logger = logger;
    }

    #region Public Methods

    /// <summary>
    /// Loads the last saved layout.
    /// </summary>
    public async Task<DockingWorkspaceState?> LoadLastLayoutAsync()
    {
        try
        {
            var layoutFile = await _localAppDataFolder.TryGetItemAsync(LayoutFileName) as StorageFile;
            if (layoutFile == null)
                return null;

            var json = await FileIO.ReadTextAsync(layoutFile);
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

            var layoutFile = await _localAppDataFolder.CreateFileAsync(
                LayoutFileName,
                CreationCollisionOption.ReplaceExisting);

            var json = JsonSerializer.Serialize(state, JsonOptions);
            await FileIO.WriteTextAsync(layoutFile, json);
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
            var layoutFile = await _localAppDataFolder.TryGetItemAsync(LayoutFileName) as StorageFile;
            if (layoutFile != null)
            {
                await layoutFile.DeleteAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deleting layout");
        }
    }

    #endregion
}

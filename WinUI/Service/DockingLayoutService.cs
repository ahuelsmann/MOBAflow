// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Windows.Storage;

using ViewModel;

/// <summary>
/// Service für Persistierung und Restore von DockingManager Layouts.
/// Speichert Layout-State in JSON-Datei im LocalAppData.
/// </summary>
internal class DockingLayoutService
{
    #region Constants

    private const string LayoutFileName = "docking-layout.json";
    private const int CurrentLayoutVersion = 1;

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
    /// Lädt das zuletzt gespeicherte Layout.
    /// </summary>
    public async Task<DockingLayoutState?> LoadLastLayoutAsync()
    {
        try
        {
            var layoutFile = await _localAppDataFolder.TryGetItemAsync(LayoutFileName) as StorageFile;
            if (layoutFile == null)
                return null;

            var json = await FileIO.ReadTextAsync(layoutFile);
            var state = JsonSerializer.Deserialize<DockingLayoutState>(json, JsonOptions);

            // Version-Kompatibilität prüfen
            if (state?.Version != CurrentLayoutVersion)
                return null;

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler beim Laden des Layouts");
            return null;
        }
    }

    /// <summary>
    /// Speichert das aktuelles Layout.
    /// </summary>
    public async Task SaveLayoutAsync(DockingPanelViewModel viewModel)
    {
        try
        {
            var state = new DockingLayoutState
            {
                Version = CurrentLayoutVersion,
                Timestamp = DateTime.UtcNow,
                IsLeftPanelVisible = viewModel.IsLeftPanelVisible,
                IsRightPanelVisible = viewModel.IsRightPanelVisible,
                IsTopPanelVisible = viewModel.IsTopPanelVisible,
                IsBottomPanelVisible = viewModel.IsBottomPanelVisible,
                LeftPanelWidth = viewModel.LeftPanelWidth,
                RightPanelWidth = viewModel.RightPanelWidth,
                TopPanelHeight = viewModel.TopPanelHeight,
                BottomPanelHeight = viewModel.BottomPanelHeight,
                IsLeftPanelPinned = viewModel.IsLeftPanelPinned,
                IsRightPanelPinned = viewModel.IsRightPanelPinned,
                IsTopPanelPinned = viewModel.IsTopPanelPinned,
                IsBottomPanelPinned = viewModel.IsBottomPanelPinned
            };

            var layoutFile = await _localAppDataFolder.CreateFileAsync(
                LayoutFileName,
                CreationCollisionOption.ReplaceExisting);

            var json = JsonSerializer.Serialize(state, JsonOptions);
            await FileIO.WriteTextAsync(layoutFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler beim Speichern des Layouts");
        }
    }

    /// <summary>
    /// Wendet ein gespeichertes Layout auf ein ViewModel an.
    /// </summary>
    public void ApplyLayoutState(DockingLayoutState state, DockingPanelViewModel viewModel)
    {
        viewModel.IsLeftPanelVisible = state.IsLeftPanelVisible;
        viewModel.IsRightPanelVisible = state.IsRightPanelVisible;
        viewModel.IsTopPanelVisible = state.IsTopPanelVisible;
        viewModel.IsBottomPanelVisible = state.IsBottomPanelVisible;

        viewModel.LeftPanelWidth = state.LeftPanelWidth;
        viewModel.RightPanelWidth = state.RightPanelWidth;
        viewModel.TopPanelHeight = state.TopPanelHeight;
        viewModel.BottomPanelHeight = state.BottomPanelHeight;

        viewModel.IsLeftPanelPinned = state.IsLeftPanelPinned;
        viewModel.IsRightPanelPinned = state.IsRightPanelPinned;
        viewModel.IsTopPanelPinned = state.IsTopPanelPinned;
        viewModel.IsBottomPanelPinned = state.IsBottomPanelPinned;
    }

    /// <summary>
    /// Löscht das gespeicherte Layout.
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
            _logger.LogWarning(ex, "Fehler beim Löschen des Layouts");
        }
    }

    #endregion
}

/// <summary>
/// Serialisierbare Repräsentation des DockingManager Layout-States.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
internal class DockingLayoutState
{
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }

    public bool IsLeftPanelVisible { get; set; }
    public bool IsRightPanelVisible { get; set; }
    public bool IsTopPanelVisible { get; set; }
    public bool IsBottomPanelVisible { get; set; }

    public double LeftPanelWidth { get; set; }
    public double RightPanelWidth { get; set; }
    public double TopPanelHeight { get; set; }
    public double BottomPanelHeight { get; set; }

    public bool IsLeftPanelPinned { get; set; }
    public bool IsRightPanelPinned { get; set; }
    public bool IsTopPanelPinned { get; set; }
    public bool IsBottomPanelPinned { get; set; }
}

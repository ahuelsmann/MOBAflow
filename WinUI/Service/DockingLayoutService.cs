// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Windows.Storage;

using ViewModel;

/// <summary>
/// Service für Persistierung und Restore von DockingManager Layouts.
/// Speichert Layout-State in JSON-Datei im LocalAppData.
/// </summary>
public class DockingLayoutService
{
    #region Constants

    private const string LayoutFileName = "docking-layout.json";
    private const int CurrentLayoutVersion = 1;

    #endregion

    #region Fields

    private readonly StorageFolder _localAppDataFolder;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion

    public DockingLayoutService()
    {
        _localAppDataFolder = ApplicationData.Current.LocalFolder;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
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
            var state = JsonSerializer.Deserialize<DockingLayoutState>(json, _jsonOptions);

            // Version-Kompatibilität prüfen
            if (state?.Version != CurrentLayoutVersion)
                return null;

            return state;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Laden des Layouts: {ex.Message}");
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

            var json = JsonSerializer.Serialize(state, _jsonOptions);
            await FileIO.WriteTextAsync(layoutFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern des Layouts: {ex.Message}");
        }
    }

    /// <summary>
    /// Wendet ein gespeichertes Layout auf ein ViewModel an.
    /// </summary>
    public void ApplyLayoutState(DockingLayoutState state, DockingPanelViewModel viewModel)
    {
        if (state == null)
            return;

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
            System.Diagnostics.Debug.WriteLine($"Fehler beim Löschen des Layouts: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Serialisierbare Repräsentation des DockingManager Layout-States.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
public class DockingLayoutState
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

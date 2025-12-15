// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for TrackPlanPage - displays physical track layout from AnyRail SVG.
/// Shows sensor positions (InPort markers) and current train location.
/// </summary>
public partial class TrackPlanViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;

    public TrackPlanViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;

        // Subscribe to journey state changes
        _mainViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedJourney))
            {
                OnPropertyChanged(nameof(CurrentJourneyName));
                OnPropertyChanged(nameof(CurrentStationName));
                OnPropertyChanged(nameof(CurrentLapDisplay));
            }
        };
    }

    #region Track Plan Properties
    /// <summary>
    /// Indicates whether a track plan SVG has been loaded.
    /// </summary>
    [ObservableProperty]
    private bool hasTrackPlan;

    /// <summary>
    /// Path to the loaded SVG file.
    /// </summary>
    [ObservableProperty]
    private string? trackPlanPath;

    /// <summary>
    /// Raw SVG content for rendering.
    /// </summary>
    [ObservableProperty]
    private string? svgContent;
    #endregion

    #region Sensor Markers
    /// <summary>
    /// Collection of sensor markers to display on the track plan.
    /// Positions will be set manually or from SVG metadata.
    /// </summary>
    public ObservableCollection<SensorMarker> SensorMarkers { get; } = new();

    /// <summary>
    /// Status text showing active sensors.
    /// </summary>
    public string SensorStatusText => $"{SensorMarkers.Count} configured";
    #endregion

    #region Train Position
    /// <summary>
    /// Indicates whether the train indicator should be visible.
    /// </summary>
    public bool IsTrainVisible => _mainViewModel.SelectedJourney != null;

    /// <summary>
    /// Current journey name for status bar.
    /// </summary>
    public string CurrentJourneyName => _mainViewModel.SelectedJourney?.Name ?? "(No journey active)";

    /// <summary>
    /// Current station name for status bar.
    /// </summary>
    public string CurrentStationName => _mainViewModel.SelectedJourney?.CurrentStation ?? "(No station)";

    /// <summary>
    /// Current lap display (e.g., "2/6").
    /// </summary>
    public string CurrentLapDisplay
    {
        get
        {
            var journey = _mainViewModel.SelectedJourney;
            if (journey == null) return "-";
            return $"{journey.CurrentCounter}/{journey.Stations.Count}";
        }
    }
    #endregion

    #region Commands
    /// <summary>
    /// Import a track plan SVG file from AnyRail.
    /// </summary>
    [RelayCommand]
    private async Task ImportTrackPlanAsync()
    {
        // TODO: Implement file picker and SVG loading
        // This will be completed when AnyRail SVG structure is analyzed
        await Task.CompletedTask;
    }

    /// <summary>
    /// Add a sensor marker at a specific position.
    /// </summary>
    [RelayCommand]
    private void AddSensorMarker(int inPort)
    {
        // TODO: Allow user to place sensor markers on the track plan
        SensorMarkers.Add(new SensorMarker
        {
            InPort = inPort,
            X = 100,  // Default position - will be set interactively
            Y = 100,
            IsActive = false
        });
        OnPropertyChanged(nameof(SensorStatusText));
    }
    #endregion
}

/// <summary>
/// Represents a sensor marker on the track plan.
/// </summary>
public partial class SensorMarker : ObservableObject
{
    /// <summary>
    /// The InPort number (feedback sensor address).
    /// </summary>
    [ObservableProperty]
    private int inPort;

    /// <summary>
    /// X position on the track plan canvas.
    /// </summary>
    [ObservableProperty]
    private double x;

    /// <summary>
    /// Y position on the track plan canvas.
    /// </summary>
    [ObservableProperty]
    private double y;

    /// <summary>
    /// Indicates whether this sensor is currently triggered.
    /// </summary>
    [ObservableProperty]
    private bool isActive;
}

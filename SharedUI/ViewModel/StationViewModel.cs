// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Domain;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Interface;

/// <summary>
/// ViewModel wrapper for JourneyStation (Junction Entity).
/// Combines Station data (from City Library) with Journey-specific properties (JourneyStation).
/// </summary>
public partial class StationViewModel : ObservableObject, IViewModelWrapper<Station>
{
    [ObservableProperty]
    private Station model; // Station from City Library (Name, InPort)

    private readonly JourneyStation _journeyStation; // Journey-specific data
    private readonly Project _project; // For resolving Platform IDs
    private readonly IUiDispatcher? _dispatcher;

    public StationViewModel(Station station, JourneyStation journeyStation, Project project, IUiDispatcher? dispatcher = null)
    {
        Model = station;
        _journeyStation = journeyStation;
        _project = project;
        _dispatcher = dispatcher;
        
        // Track property changes for unsaved changes detection
        PropertyChanged += (s, e) =>
        {
            // Ignore Model property changes (those are internal)
            if (e.PropertyName != nameof(Model))
            {
                // Property changed - mark as modified
                // MainWindowViewModel will listen to this
            }
        };
    }

    public MobaType EntityType => MobaType.Station;

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public string? Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }

    public int FeedbackInPort
    {
        get => (int)Model.InPort;
        set => SetProperty(Model.InPort, (uint)value, Model, (m, v) => m.InPort = v);
    }

    // Journey-specific properties (from JourneyStation)
    public int NumberOfLapsToStop
    {
        get => (int)_journeyStation.NumberOfLapsToStop;
        set => SetProperty(_journeyStation.NumberOfLapsToStop, (uint)value, _journeyStation, (m, v) => m.NumberOfLapsToStop = v);
    }

    public Guid? WorkflowId
    {
        get => _journeyStation.WorkflowId;
        set => SetProperty(_journeyStation.WorkflowId, value, _journeyStation, (m, v) => m.WorkflowId = v);
    }

    public bool IsExitOnLeft
    {
        get => _journeyStation.IsExitOnLeft;
        set => SetProperty(_journeyStation.IsExitOnLeft, value, _journeyStation, (m, v) => m.IsExitOnLeft = v);
    }

    // Platform references (resolved from Project.Platforms via Station.PlatformIds)
    public List<Platform> Platforms
    {
        get
        {
            return Model.PlatformIds
                .Select(id => _project.Platforms.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList()!;
        }
    }

    // --- Phase 1 Properties (from JourneyStation - will move to Platform in Phase 2) ---

    public int Track
    {
        get => (int)(_journeyStation.Track ?? 1);
        set => SetProperty(_journeyStation.Track, (uint?)value, _journeyStation, (m, v) => m.Track = v);
    }

    public DateTime? Arrival
    {
        get => _journeyStation.Arrival;
        set => SetProperty(_journeyStation.Arrival, value, _journeyStation, (m, v) => m.Arrival = v);
    }

    public DateTime? Departure
    {
        get => _journeyStation.Departure;
        set => SetProperty(_journeyStation.Departure, value, _journeyStation, (m, v) => m.Departure = v);
    }

    // --- Adaptive Properties (Hybrid Mode Support) ---

    /// <summary>
    /// Indicates if station uses platform-based configuration.
    /// True when Platforms list has entries, false for simple mode.
    /// </summary>
    public bool UsesPlatforms => Platforms != null && Platforms.Count > 0;

    /// <summary>
    /// Gets the effective track number as string for display.
    /// Returns JourneyStation.Track if no platforms, otherwise first Platform.Track.
    /// </summary>
    public string EffectiveTrack
    {
        get
        {
            if (!UsesPlatforms)
                return Track.ToString();

            return Platforms[0].Track.ToString();
        }
    }

    /// <summary>
    /// Gets the effective arrival designation as string for display.
    /// Returns JourneyStation.Arrival formatted if no platforms, otherwise Platform info.
    /// </summary>
    public string? EffectiveArrival
    {
        get
        {
            if (!UsesPlatforms)
                return Arrival?.ToString("HH:mm");

            // In Phase 2: Use Platform.ArrivalTime when implemented
            var platform = Platforms[0];
            return platform.Name ?? "-";
        }
    }

    /// <summary>
    /// Gets the effective departure designation as string for display.
    /// Returns JourneyStation.Departure formatted if no platforms, otherwise Platform info.
    /// </summary>
    public string? EffectiveDeparture
    {
        get
        {
            if (!UsesPlatforms)
                return Departure?.ToString("HH:mm");

            // In Phase 2: Use Platform.DepartureTime when implemented
            var platform = Platforms[0];
            return platform.Description ?? "-";
        }
    }

    /// <summary>
    /// Gets the effective exit orientation.
    /// Returns JourneyStation.IsExitOnLeft if no platforms, otherwise false (Phase 2 pending).
    /// </summary>
    public bool EffectiveIsExitOnLeft
    {
        get
        {
            if (!UsesPlatforms)
                return IsExitOnLeft;

            // In Phase 2: Use Platform.IsExitOnLeft when implemented
            return false;
        }
    }

    /// <summary>
    /// Gets display text indicating configuration mode.
    /// </summary>
    public string ConfigurationMode => UsesPlatforms
        ? $"Platform Mode ({Platforms.Count} platforms)"
        : "Simple Mode";

    // --- Runtime State Properties ---

    /// <summary>
    /// Position of this station in the journey (1-based index).
    /// Used for displaying station order in UI.
    /// </summary>
    public int Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }
    private int _position;

    /// <summary>
    /// Indicates if this station is currently active in journey execution.
    /// Used for visual highlighting in UI.
    /// </summary>
    [ObservableProperty]
    private bool isCurrentStation;

    /// <summary>
    /// Gets the background color for this station based on its current state.
    /// Returns green (#60A060) for active station, transparent otherwise.
    /// </summary>
    public string BackgroundColor => IsCurrentStation ? "#60A060" : "Transparent";

    partial void OnIsCurrentStationChanged(bool value)
    {
        // Notify UI that background color has changed
        OnPropertyChanged(nameof(BackgroundColor));
    }
}
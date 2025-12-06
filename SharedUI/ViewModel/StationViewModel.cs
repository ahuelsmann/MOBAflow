// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Domain;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Interface;

/// <summary>
/// ViewModel wrapper for Station model.
/// Note: Platforms are not yet supported in MVP (City = Station for now).
/// </summary>
public partial class StationViewModel : ObservableObject, IViewModelWrapper<Station>
{
    [ObservableProperty]
    private Station model;

    private readonly IUiDispatcher? _dispatcher;

    public StationViewModel(Station model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
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

    public int NumberOfLapsToStop
    {
        get => (int)Model.NumberOfLapsToStop;
        set => SetProperty(Model.NumberOfLapsToStop, (uint)value, Model, (m, v) => m.NumberOfLapsToStop = v);
    }

    public List<Platform> Platforms
    {
        get => Model.Platforms;
        set => SetProperty(Model.Platforms, value, Model, (m, v) => m.Platforms = v);
    }

    public Workflow? Flow
    {
        get => Model.Flow;
        set => SetProperty(Model.Flow, value, Model, (m, v) => m.Flow = v);
    }

    public Guid? WorkflowId
    {
        get => Model.WorkflowId;
        set => SetProperty(Model.WorkflowId, value, Model, (m, v) => m.WorkflowId = v);
    }

    // --- Phase 1 Properties (Simplified Platform representation) ---
    // TODO: Remove when Phase 2 (Platform support) is implemented

    public int Track
    {
        get => (int)(Model.Track ?? 1);
        set => SetProperty(Model.Track, (uint?)value, Model, (m, v) => m.Track = v);
    }

    public DateTime? Arrival
    {
        get => Model.Arrival;
        set => SetProperty(Model.Arrival, value, Model, (m, v) => m.Arrival = v);
    }

    public DateTime? Departure
    {
        get => Model.Departure;
        set => SetProperty(Model.Departure, value, Model, (m, v) => m.Departure = v);
    }

    public bool IsExitOnLeft
    {
        get => Model.IsExitOnLeft;
        set => SetProperty(Model.IsExitOnLeft, value, Model, (m, v) => m.IsExitOnLeft = v);
    }

    // --- Adaptive Properties (Hybrid Mode Support) ---

    /// <summary>
    /// Indicates if station uses platform-based configuration.
    /// True when Platforms list has entries, false for simple mode.
    /// </summary>
    public bool UsesPlatforms => Model.Platforms != null && Model.Platforms.Count > 0;

    /// <summary>
    /// Gets the effective track number as string for display.
    /// Returns Station.Track if no platforms, otherwise first Platform.Track.
    /// </summary>
    public string EffectiveTrack
    {
        get
        {
            if (!UsesPlatforms)
                return Model.Track.ToString();

            return Model.Platforms[0].Track.ToString();
        }
    }

    /// <summary>
    /// Gets the effective arrival designation as string for display.
    /// Returns Station.Arrival formatted if no platforms, otherwise Platform info.
    /// </summary>
    public string? EffectiveArrival
    {
        get
        {
            if (!UsesPlatforms)
                return Model.Arrival?.ToString("HH:mm");

            // In Phase 2: Use Platform.ArrivalTime when implemented
            var platform = Model.Platforms[0];
            return platform.Name ?? "-";
        }
    }

    /// <summary>
    /// Gets the effective departure designation as string for display.
    /// Returns Station.Departure formatted if no platforms, otherwise Platform info.
    /// </summary>
    public string? EffectiveDeparture
    {
        get
        {
            if (!UsesPlatforms)
                return Model.Departure?.ToString("HH:mm");

            // In Phase 2: Use Platform.DepartureTime when implemented
            var platform = Model.Platforms[0];
            return platform.Description ?? "-";
        }
    }

    /// <summary>
    /// Gets the effective exit orientation.
    /// Returns Station.IsExitOnLeft if no platforms, otherwise false (Phase 2 pending).
    /// </summary>
    public bool EffectiveIsExitOnLeft
    {
        get
        {
            if (!UsesPlatforms)
                return Model.IsExitOnLeft;

            // In Phase 2: Use Platform.IsExitOnLeft when implemented
            return false;
        }
    }

    /// <summary>
    /// Gets display text indicating configuration mode.
    /// </summary>
    public string ConfigurationMode => UsesPlatforms
        ? $"Platform Mode ({Model.Platforms.Count} platforms)"
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


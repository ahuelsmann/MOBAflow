// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Station model with workflow assignment operations.
/// Uses Project for resolving workflow GUID references.
/// </summary>
public sealed partial class StationViewModel : ObservableObject, IViewModelWrapper<Station>
{
    #region Fields
    // Model
    private readonly Station _station;

    // Context
    private readonly Project _project;
    private ObservableCollection<PlatformViewModel>? _platforms;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="StationViewModel"/> class.
    /// </summary>
    /// <param name="station">The station domain model.</param>
    /// <param name="project">The parent project that owns this station's workflow references.</param>
    public StationViewModel(Station station, Project project)
    {
        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(project);
        _station = station;
        _project = project;
        RefreshPlatforms();
    }

    /// <summary>
    /// Gets the underlying domain model (for IViewModelWrapper interface).
    /// </summary>
    public Station Model => _station;

    /// <summary>
    /// Gets or sets the display name of the station.
    /// </summary>
    public string Name
    {
        get => _station.Name;
        set => SetProperty(_station.Name, value, _station, (m, v) => m.Name = v);
    }

    public bool IsVirtual
    {
        get => _station.IsVirtual;
        set
        {
            if (SetProperty(_station.IsVirtual, value, _station, (m, v) => m.IsVirtual = v))
            {
                OnPropertyChanged(nameof(IsRealStation));
                OnPropertyChanged(nameof(StationIconGlyph));
                OnPropertyChanged(nameof(StationKindText));
                OnPropertyChanged(nameof(StationForegroundResourceKey));
            }
        }
    }

    public bool IsRealStation => !IsVirtual;

    public string StationIconGlyph => IsVirtual ? "\uE945" : "\uEC06";

    public string StationKindText => IsVirtual ? "Event" : "Station";

    public string StationForegroundResourceKey => IsVirtual ? "SystemFillColorCautionBrush" : "TextFillColorPrimaryBrush";

    /// <summary>
    /// Gets or sets an optional description shown in the UI for this station.
    /// </summary>
    public string? Description
    {
        get => _station.Description;
        set => SetProperty(_station.Description, value, _station, (m, v) => m.Description = v);
    }

    /// <summary>
    /// Gets or sets the feedback input port used to detect this station.
    /// </summary>
    public int InPort
    {
        get => (int)_station.InPort;
        set => SetProperty(_station.InPort, (uint)value, _station, (m, v) => m.InPort = v);
    }

    // Journey-specific properties (now directly from Station)
    /// <summary>
    /// Gets or sets the number of laps to run before stopping at this station.
    /// </summary>
    public int NumberOfLapsToStop
    {
        get => (int)_station.NumberOfLapsToStop;
        set => SetProperty(_station.NumberOfLapsToStop, (uint)value, _station, (m, v) => m.NumberOfLapsToStop = v);
    }

    /// <summary>
    /// Gets or sets the identifier of the workflow that should run when this station is reached.
    /// </summary>
    public Guid? WorkflowId
    {
        get => _station.WorkflowId;
        set
        {
            if (SetProperty(_station.WorkflowId, value, _station, (m, v) => m.WorkflowId = v))
            {
                OnPropertyChanged(nameof(WorkflowName));
            }
        }
    }

    /// <summary>
    /// Gets the name of the assigned workflow, or a placeholder if none is assigned.
    /// </summary>
    public string WorkflowName
    {
        get
        {
            if (_station.WorkflowId == null) return "(Drop workflow here)";
            var workflow = _project.Workflows.FirstOrDefault(w => w.Id == _station.WorkflowId);
            return workflow?.Name ?? "(Unknown workflow)";
        }
    }

    /// <summary>
    /// Command to assign a workflow to this station via drag and drop.
    /// </summary>
    [RelayCommand]
    private void AssignWorkflow(WorkflowViewModel? workflow)
    {
        if (workflow == null) return;
        WorkflowId = workflow.Model.Id;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the train exits this station on the left side.
    /// </summary>
    public bool IsExitOnLeft
    {
        get => _station.IsExitOnLeft;
        set
        {
            if (SetProperty(_station.IsExitOnLeft, value, _station, (m, v) => m.IsExitOnLeft = v))
            {
                OnPropertyChanged(nameof(ExitSideText));
            }
        }
    }

    public string ExitSideText => IsExitOnLeft ? "Left" : "Right";

    /// <summary>
    /// Gets the platforms belonging to this station.
    /// </summary>
    public ObservableCollection<PlatformViewModel> Platforms
    {
        get
        {
            if (_platforms == null)
            {
                RefreshPlatforms();
            }

            return _platforms!;
        }
    }

    /// <summary>
    /// Gets or sets the platform selected for this journey stop.
    /// </summary>
    public Guid? PlatformId
    {
        get => _station.PlatformId;
        set
        {
            if (SetProperty(_station.PlatformId, value, _station, (m, v) => m.PlatformId = v))
            {
            }
        }
    }

    /// <summary>
    /// Refreshes the platform ViewModel collection from the model.
    /// </summary>
    public void RefreshPlatforms()
    {
        if (_platforms == null)
        {
            _platforms = [];
        }
        else
        {
            _platforms.Clear();
        }

        foreach (var platform in _station.Platforms)
        {
            _platforms.Add(new PlatformViewModel(platform, _project));
        }

        OnPropertyChanged(nameof(Platforms));
    }

    /// <summary>
    /// Adds a new platform to this station.
    /// </summary>
    [RelayCommand]
    private void AddPlatform()
    {
        var platform = new Platform
        {
            Number = (uint)(_station.Platforms.Count + 1),
            Name = $"Platform {_station.Platforms.Count + 1}"
        };

        _station.Platforms.Add(platform);
        RefreshPlatforms();
    }

    /// <summary>
    /// Removes a platform from this station.
    /// </summary>
    [RelayCommand]
    private void DeletePlatform(PlatformViewModel? platform)
    {
        if (platform == null) return;

        _station.Platforms.Remove(platform.Model);
        if (_station.PlatformId == platform.Model.Id)
        {
            PlatformId = null;
        }

        RefreshPlatforms();
    }

    /// <summary>
    /// Gets or sets the planned arrival time at this station.
    /// </summary>
    public DateTime? Arrival
    {
        get => _station.Arrival;
        set
        {
            if (SetProperty(_station.Arrival, value, _station, (m, v) => m.Arrival = v))
            {
                OnPropertyChanged(nameof(ArrivalTimeText));
            }
        }
    }

    public string ArrivalTimeText => Arrival?.ToString("HH:mm") ?? "--:--";

    /// <summary>
    /// Gets or sets the planned departure time from this station.
    /// </summary>
    public DateTime? Departure
    {
        get => _station.Departure;
        set
        {
            if (SetProperty(_station.Departure, value, _station, (m, v) => m.Departure = v))
            {
                OnPropertyChanged(nameof(DepartureTimeText));
            }
        }
    }

    public string DepartureTimeText => Departure?.ToString("HH:mm") ?? "--:--";

    /// <summary>
    /// Gets or sets the 1-based position of this station within the journey.
    /// </summary>
    public int Position
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Indicates if this station is currently active in journey execution.
    /// Used for visual highlighting in UI.
    /// </summary>
    [ObservableProperty]
    private bool _isCurrentStation;

    /// <summary>
    /// Gets the background color for this station based on its current state.
    /// Returns white (#FFFFFF) for active station, empty string (theme default) otherwise.
    /// </summary>
    public string BackgroundColor => IsCurrentStation ? "#FFFFFF" : "";

    /// <summary>
    /// Gets the foreground (text) color for this station based on its current state.
    /// Returns black (#000000) for active station (on white background), empty string (theme default) otherwise.
    /// </summary>
    public string ForegroundColor => IsCurrentStation ? "#000000" : "";

    partial void OnIsCurrentStationChanged(bool value)
    {
        _ = value; // Suppress unused parameter warning
        // Notify UI that colors have changed
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(ForegroundColor));
    }
}

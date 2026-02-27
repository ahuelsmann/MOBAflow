// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Interface;

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
    #endregion

    public StationViewModel(Station station, Project project)
    {
        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(project);
        _station = station;
        _project = project;
    }

    /// <summary>
    /// Gets the underlying domain model (for IViewModelWrapper interface).
    /// </summary>
    public Station Model => _station;

    public string Name
    {
        get => _station.Name;
        set => SetProperty(_station.Name, value, _station, (m, v) => m.Name = v);
    }

    public string? Description
    {
        get => _station.Description;
        set => SetProperty(_station.Description, value, _station, (m, v) => m.Description = v);
    }

    public int InPort
    {
        get => (int)_station.InPort;
        set => SetProperty(_station.InPort, (uint)value, _station, (m, v) => m.InPort = v);
    }

    // Journey-specific properties (now directly from Station)
    public int NumberOfLapsToStop
    {
        get => (int)_station.NumberOfLapsToStop;
        set => SetProperty(_station.NumberOfLapsToStop, (uint)value, _station, (m, v) => m.NumberOfLapsToStop = v);
    }

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

    public bool IsExitOnLeft
    {
        get => _station.IsExitOnLeft;
        set => SetProperty(_station.IsExitOnLeft, value, _station, (m, v) => m.IsExitOnLeft = v);
    }

    public int Track
    {
        get => (int)(_station.Track ?? 1);
        set => SetProperty(_station.Track, (uint?)value, _station, (m, v) => m.Track = v);
    }

    public DateTime? Arrival
    {
        get => _station.Arrival;
        set => SetProperty(_station.Arrival, value, _station, (m, v) => m.Arrival = v);
    }

    public DateTime? Departure
    {
        get => _station.Departure;
        set => SetProperty(_station.Departure, value, _station, (m, v) => m.Departure = v);
    }

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

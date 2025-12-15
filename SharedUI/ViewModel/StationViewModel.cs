// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

/// <summary>
/// ViewModel wrapper for Station.
/// Station contains both hardware properties (InPort) and journey-specific properties.
/// </summary>
public partial class StationViewModel : ObservableObject, IViewModelWrapper<Station>
{
    private readonly Station _model;
    private readonly Project _project;

    public StationViewModel(Station station, Project project)
    {
        _model = station;
        _project = project;
    }

    /// <summary>
    /// Gets the underlying domain model (for IViewModelWrapper interface).
    /// </summary>
    public Station Model => _model;

    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    public string? Description
    {
        get => _model.Description;
        set => SetProperty(_model.Description, value, _model, (m, v) => m.Description = v);
    }

    public int InPort
    {
        get => (int)_model.InPort;
        set => SetProperty(_model.InPort, (uint)value, _model, (m, v) => m.InPort = v);
    }

    // Journey-specific properties (now directly from Station)
    public int NumberOfLapsToStop
    {
        get => (int)_model.NumberOfLapsToStop;
        set => SetProperty(_model.NumberOfLapsToStop, (uint)value, _model, (m, v) => m.NumberOfLapsToStop = v);
    }

    public Guid? WorkflowId
    {
        get => _model.WorkflowId;
        set
        {
            if (SetProperty(_model.WorkflowId, value, _model, (m, v) => m.WorkflowId = v))
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
            if (_model.WorkflowId == null) return "(Drop workflow here)";
            var workflow = _project.Workflows.FirstOrDefault(w => w.Id == _model.WorkflowId);
            return workflow?.Name ?? "(Unknown workflow)";
        }
    }

    /// <summary>
    /// Command to assign a workflow to this station via drag & drop.
    /// </summary>
    [RelayCommand]
    private void AssignWorkflow(WorkflowViewModel? workflow)
    {
        if (workflow == null) return;
        WorkflowId = workflow.Model.Id;
    }

    public bool IsExitOnLeft
    {
        get => _model.IsExitOnLeft;
        set => SetProperty(_model.IsExitOnLeft, value, _model, (m, v) => m.IsExitOnLeft = v);
    }

    public int Track
    {
        get => (int)(_model.Track ?? 1);
        set => SetProperty(_model.Track, (uint?)value, _model, (m, v) => m.Track = v);
    }

    public DateTime? Arrival
    {
        get => _model.Arrival;
        set => SetProperty(_model.Arrival, value, _model, (m, v) => m.Arrival = v);
    }

    public DateTime? Departure
    {
        get => _model.Departure;
        set => SetProperty(_model.Departure, value, _model, (m, v) => m.Departure = v);
    }

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
        // Notify UI that colors have changed
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(ForegroundColor));
    }
}

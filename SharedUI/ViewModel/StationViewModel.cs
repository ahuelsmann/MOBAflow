// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Domain;

using Interface;

/// <summary>
/// ViewModel wrapper for Station.
/// Station contains both hardware properties (InPort) and journey-specific properties.
/// </summary>
public partial class StationViewModel : ObservableObject, IViewModelWrapper<Station>
{
    [ObservableProperty]
    private Station model; // Station with all properties

    private readonly Project _project; // For resolving Platform IDs

    public StationViewModel(Station station, Project project)
    {
        Model = station;
        _project = project;

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

    public int InPort
    {
        get => (int)Model.InPort;
        set => SetProperty(Model.InPort, (uint)value, Model, (m, v) => m.InPort = v);
    }

    // Journey-specific properties (now directly from Station)
    public int NumberOfLapsToStop
    {
        get => (int)Model.NumberOfLapsToStop;
        set => SetProperty(Model.NumberOfLapsToStop, (uint)value, Model, (m, v) => m.NumberOfLapsToStop = v);
    }

    public Guid? WorkflowId
    {
        get => Model.WorkflowId;
        set => SetProperty(Model.WorkflowId, value, Model, (m, v) => m.WorkflowId = v);
    }

    public bool IsExitOnLeft
    {
        get => Model.IsExitOnLeft;
        set => SetProperty(Model.IsExitOnLeft, value, Model, (m, v) => m.IsExitOnLeft = v);
    }

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
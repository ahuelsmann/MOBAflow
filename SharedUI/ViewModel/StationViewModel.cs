// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Domain;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.SharedUI.Service;

/// <summary>
/// ViewModel wrapper for Station model.
/// Note: Platforms are not yet supported in MVP (City = Station for now).
/// </summary>
public partial class StationViewModel : ObservableObject
{
    [ObservableProperty]
    private Station model;

    private readonly IUiDispatcher? _dispatcher;

    public StationViewModel(Station model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public uint Number
    {
        get => Model.Number;
        set => SetProperty(Model.Number, value, Model, (m, v) => m.Number = v);
    }

    public uint NumberOfLapsToStop
    {
        get => Model.NumberOfLapsToStop;
        set => SetProperty(Model.NumberOfLapsToStop, value, Model, (m, v) => m.NumberOfLapsToStop = v);
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

    public uint Track
    {
        get => Model.Track;
        set => SetProperty(Model.Track, value, Model, (m, v) => m.Track = v);
    }

    public bool IsExitOnLeft
    {
        get => Model.IsExitOnLeft;
        set => SetProperty(Model.IsExitOnLeft, value, Model, (m, v) => m.IsExitOnLeft = v);
    }

    public string TransferConnections
    {
        get => Model.TransferConnections;
        set => SetProperty(Model.TransferConnections, value, Model, (m, v) => m.TransferConnections = v);
    }

    public Workflow? Flow
    {
        get => Model.Flow;
        set => SetProperty(Model.Flow, value, Model, (m, v) => m.Flow = v);
    }
}

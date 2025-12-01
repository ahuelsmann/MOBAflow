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

    public string? Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }

    public int? FeedbackInPort
    {
        get => Model.FeedbackInPort;
        set => SetProperty(Model.FeedbackInPort, value, Model, (m, v) => m.FeedbackInPort = v);
    }

    public int NumberOfLapsToStop
    {
        get => Model.NumberOfLapsToStop;
        set => SetProperty(Model.NumberOfLapsToStop, value, Model, (m, v) => m.NumberOfLapsToStop = v);
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
}

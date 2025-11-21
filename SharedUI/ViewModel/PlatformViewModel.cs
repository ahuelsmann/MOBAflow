// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.SharedUI.Service;

/// <summary>
/// ViewModel wrapper for Platform model.
/// </summary>
public partial class PlatformViewModel : ObservableObject
{
    [ObservableProperty]
    private Platform model;

    private readonly IUiDispatcher? _dispatcher;

    public PlatformViewModel(Platform model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public uint Track
    {
        get => Model.Track;
        set => SetProperty(Model.Track, value, Model, (m, v) => m.Track = v);
    }

    public uint InPort
    {
        get => Model.InPort;
        set => SetProperty(Model.InPort, value, Model, (m, v) => m.InPort = v);
    }

    public Workflow? Flow
    {
        get => Model.Flow;
        set => SetProperty(Model.Flow, value, Model, (m, v) => m.Flow = v);
    }

    public bool IsUsingTimerToIgnoreFeedbacks
    {
        get => Model.IsUsingTimerToIgnoreFeedbacks;
        set => SetProperty(Model.IsUsingTimerToIgnoreFeedbacks, value, Model, (m, v) => m.IsUsingTimerToIgnoreFeedbacks = v);
    }

    public double IntervalForTimerToIgnoreFeedbacks
    {
        get => Model.IntervalForTimerToIgnoreFeedbacks;
        set => SetProperty(Model.IntervalForTimerToIgnoreFeedbacks, value, Model, (m, v) => m.IntervalForTimerToIgnoreFeedbacks = v);
    }
}

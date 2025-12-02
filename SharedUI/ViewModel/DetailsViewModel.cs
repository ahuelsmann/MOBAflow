// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Service;

/// <summary>
/// ViewModel wrapper for Details model.
/// Provides additional locomotive/wagon information.
/// </summary>
public partial class DetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private Details model;

    private readonly IUiDispatcher? _dispatcher;

    public DetailsViewModel(Details model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
    }

    public uint Axles
    {
        get => Model.Axles;
        set => SetProperty(Model.Axles, value, Model, (m, v) => m.Axles = v);
    }

    public Epoch? Epoch
    {
        get => Model.Epoch;
        set => SetProperty(Model.Epoch, value, Model, (m, v) => m.Epoch = v);
    }

    public string? RailroadCompany
    {
        get => Model.RailroadCompany;
        set => SetProperty(Model.RailroadCompany, value, Model, (m, v) => m.RailroadCompany = v);
    }

    public PowerSystem? Power
    {
        get => Model.Power;
        set => SetProperty(Model.Power, value, Model, (m, v) => m.Power = v);
    }

    public DigitalSystem? Digital
    {
        get => Model.Digital;
        set => SetProperty(Model.Digital, value, Model, (m, v) => m.Digital = v);
    }

    public string? Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }
}
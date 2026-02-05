// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Domain.Enum;
using Interface;

/// <summary>
/// ViewModel wrapper for Details model.
/// </summary>
public sealed class DetailsViewModel : ObservableObject, IViewModelWrapper<Details>
{
    public DetailsViewModel(Details model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public Details Model { get; }

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

    public string Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }
}

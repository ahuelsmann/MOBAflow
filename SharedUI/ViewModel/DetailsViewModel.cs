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
    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying details model to wrap.</param>
    public DetailsViewModel(Details model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    /// <summary>
    /// Gets the underlying details domain model.
    /// </summary>
    public Details Model { get; }

    /// <summary>
    /// Gets or sets the number of axles.
    /// </summary>
    public uint Axles
    {
        get => Model.Axles;
        set => SetProperty(Model.Axles, value, Model, (m, v) => m.Axles = v);
    }

    /// <summary>
    /// Gets or sets the epoch (era) of the vehicle.
    /// </summary>
    public Epoch? Epoch
    {
        get => Model.Epoch;
        set => SetProperty(Model.Epoch, value, Model, (m, v) => m.Epoch = v);
    }

    /// <summary>
    /// Gets or sets the railroad company name or code.
    /// </summary>
    public string? RailroadCompany
    {
        get => Model.RailroadCompany;
        set => SetProperty(Model.RailroadCompany, value, Model, (m, v) => m.RailroadCompany = v);
    }

    /// <summary>
    /// Gets or sets the power system of the vehicle.
    /// </summary>
    public PowerSystem? Power
    {
        get => Model.Power;
        set => SetProperty(Model.Power, value, Model, (m, v) => m.Power = v);
    }

    /// <summary>
    /// Gets or sets the digital control system of the vehicle.
    /// </summary>
    public DigitalSystem? Digital
    {
        get => Model.Digital;
        set => SetProperty(Model.Digital, value, Model, (m, v) => m.Digital = v);
    }

    /// <summary>
    /// Gets or sets a free-form description of the model.
    /// </summary>
    public string Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }
}

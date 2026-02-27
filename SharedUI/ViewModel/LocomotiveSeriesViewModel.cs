// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;

/// <summary>
/// ViewModel wrapper for LocomotiveSeries model.
/// </summary>
public sealed class LocomotiveSeriesViewModel : ObservableObject, IViewModelWrapper<LocomotiveSeries>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveSeriesViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying locomotive series model to wrap.</param>
    public LocomotiveSeriesViewModel(LocomotiveSeries model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    /// <summary>
    /// Gets the underlying locomotive series domain model.
    /// </summary>
    public LocomotiveSeries Model { get; }

    /// <summary>
    /// Gets or sets the name of the locomotive series.
    /// </summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the maximum speed in km/h.
    /// </summary>
    public int Vmax
    {
        get => Model.Vmax;
        set => SetProperty(Model.Vmax, value, Model, (m, v) => m.Vmax = v);
    }

    /// <summary>
    /// Gets or sets the series type (for example, passenger, freight).
    /// </summary>
    public string Type
    {
        get => Model.Type;
        set => SetProperty(Model.Type, value, Model, (m, v) => m.Type = v);
    }

    /// <summary>
    /// Gets or sets the epoch (era) of this series.
    /// </summary>
    public string Epoch
    {
        get => Model.Epoch;
        set => SetProperty(Model.Epoch, value, Model, (m, v) => m.Epoch = v);
    }

    /// <summary>
    /// Gets or sets a free-form description of this locomotive series.
    /// </summary>
    public string Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }
}

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
    public LocomotiveSeriesViewModel(LocomotiveSeries model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public LocomotiveSeries Model { get; }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public int Vmax
    {
        get => Model.Vmax;
        set => SetProperty(Model.Vmax, value, Model, (m, v) => m.Vmax = v);
    }

    public string Type
    {
        get => Model.Type;
        set => SetProperty(Model.Type, value, Model, (m, v) => m.Type = v);
    }

    public string Epoch
    {
        get => Model.Epoch;
        set => SetProperty(Model.Epoch, value, Model, (m, v) => m.Epoch = v);
    }

    public string Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }
}

// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;

/// <summary>
/// ViewModel wrapper for City model.
/// </summary>
public sealed class CityViewModel : ObservableObject, IViewModelWrapper<City>
{
    public CityViewModel(City model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public City Model { get; }

    public Guid Id => Model.Id;

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

    public List<Station> Stations => Model.Stations;
}

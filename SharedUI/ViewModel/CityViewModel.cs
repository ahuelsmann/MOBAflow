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
    /// <summary>
    /// Initializes a new instance of the <see cref="CityViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying city model to wrap.</param>
    public CityViewModel(City model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    /// <summary>
    /// Gets the underlying city domain model.
    /// </summary>
    public City Model { get; }

    /// <summary>
    /// Gets the unique identifier of the city.
    /// </summary>
    public Guid Id => Model.Id;

    /// <summary>
    /// Gets or sets the name of the city.
    /// </summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the optional description of the city.
    /// </summary>
    public string? Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }

    /// <summary>
    /// Gets the list of stations that belong to this city.
    /// </summary>
    public List<Station> Stations => Model.Stations;
}

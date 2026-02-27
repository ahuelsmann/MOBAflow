// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;

/// <summary>
/// ViewModel wrapper for LocomotiveCategory model.
/// </summary>
public sealed class LocomotiveCategoryViewModel : ObservableObject, IViewModelWrapper<LocomotiveCategory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveCategoryViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying locomotive category model to wrap.</param>
    public LocomotiveCategoryViewModel(LocomotiveCategory model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    /// <summary>
    /// Gets the underlying locomotive category domain model.
    /// </summary>
    public LocomotiveCategory Model { get; }

    /// <summary>
    /// Gets or sets the category name (for example, diesel, electric).
    /// </summary>
    public string Category
    {
        get => Model.Category;
        set => SetProperty(Model.Category, value, Model, (m, v) => m.Category = v);
    }

    /// <summary>
    /// Gets the list of locomotive series that belong to this category.
    /// </summary>
    public List<LocomotiveSeries> Series => Model.Series;
}

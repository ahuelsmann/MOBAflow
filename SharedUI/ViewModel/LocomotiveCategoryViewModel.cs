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
    public LocomotiveCategoryViewModel(LocomotiveCategory model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public LocomotiveCategory Model { get; }

    public string Category
    {
        get => Model.Category;
        set => SetProperty(Model.Category, value, Model, (m, v) => m.Category = v);
    }

    public List<LocomotiveSeries> Series => Model.Series;
}

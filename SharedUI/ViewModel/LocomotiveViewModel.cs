// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Domain.Enum;
using Interface;

/// <summary>
/// ViewModel wrapper for Locomotive model with throttle control operations.
/// </summary>
public partial class LocomotiveViewModel : ObservableObject, IViewModelWrapper<Locomotive>
{
    #region Fields
    // Model
    [ObservableProperty]
    private Locomotive model;
    #endregion

    public LocomotiveViewModel(Locomotive model)
    {
        Model = model;
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public uint Pos
    {
        get => Model.Pos;
        set => SetProperty(Model.Pos, value, Model, (m, v) => m.Pos = v);
    }

    public uint? DigitalAddress
    {
        get => Model.DigitalAddress;
        set => SetProperty(Model.DigitalAddress, value, Model, (m, v) => m.DigitalAddress = v);
    }

    public string? Manufacturer
    {
        get => Model.Manufacturer;
        set => SetProperty(Model.Manufacturer, value, Model, (m, v) => m.Manufacturer = v);
    }

    public string? ArticleNumber
    {
        get => Model.ArticleNumber;
        set => SetProperty(Model.ArticleNumber, value, Model, (m, v) => m.ArticleNumber = v);
    }

    public string? Series
    {
        get => Model.Series;
        set => SetProperty(Model.Series, value, Model, (m, v) => m.Series = v);
    }

    public ColorScheme? ColorPrimary
    {
        get => Model.ColorPrimary;
        set => SetProperty(Model.ColorPrimary, value, Model, (m, v) => m.ColorPrimary = v);
    }

    public ColorScheme? ColorSecondary
    {
        get => Model.ColorSecondary;
        set => SetProperty(Model.ColorSecondary, value, Model, (m, v) => m.ColorSecondary = v);
    }

    public bool IsPushing
    {
        get => Model.IsPushing;
        set => SetProperty(Model.IsPushing, value, Model, (m, v) => m.IsPushing = v);
    }

    public Details? Details
    {
        get => Model.Details;
        set => SetProperty(Model.Details, value, Model, (m, v) => m.Details = v);
    }
}

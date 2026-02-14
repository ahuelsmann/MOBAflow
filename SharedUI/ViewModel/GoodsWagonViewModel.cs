// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Domain;
using Domain.Enum;

/// <summary>
/// ViewModel wrapper for GoodsWagon model.
/// Extends WagonViewModel with freight-specific properties.
/// </summary>
public sealed class GoodsWagonViewModel : WagonViewModel
{
    #region Fields
    // Model (specialized type)
    private GoodsWagon GoodsWagonModel { get; }
    #endregion

    public GoodsWagonViewModel(GoodsWagon model) : base(model)
    {
        ArgumentNullException.ThrowIfNull(model);
        GoodsWagonModel = model;
    }

    public CargoType Cargo
    {
        get => GoodsWagonModel.Cargo;
        set => SetProperty(GoodsWagonModel.Cargo, value, GoodsWagonModel, (m, v) => m.Cargo = v);
    }
}

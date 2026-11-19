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

    /// <summary>
    /// Initializes a new instance of the <see cref="GoodsWagonViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying goods wagon model to wrap.</param>
    public GoodsWagonViewModel(GoodsWagon model) : base(model)
    {
        ArgumentNullException.ThrowIfNull(model);
        GoodsWagonModel = model;
    }

    /// <summary>
    /// Gets or sets the cargo type transported by this wagon.
    /// </summary>
    public CargoType Cargo
    {
        get => GoodsWagonModel.Cargo;
        set => SetProperty(GoodsWagonModel.Cargo, value, GoodsWagonModel, (m, v) => m.Cargo = v);
    }
}

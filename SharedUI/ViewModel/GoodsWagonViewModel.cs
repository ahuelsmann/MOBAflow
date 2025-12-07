// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;

/// <summary>
/// ViewModel wrapper for GoodsWagon model.
/// Extends WagonViewModel with freight-specific properties.
/// </summary>
public partial class GoodsWagonViewModel : WagonViewModel
{
    public GoodsWagonViewModel(GoodsWagon model, IUiDispatcher? dispatcher = null)
        : base(model, dispatcher)
    {
        GoodsWagonModel = model;
    }

    private GoodsWagon GoodsWagonModel { get; }

    public CargoType Cargo
    {
        get => GoodsWagonModel.Cargo;
        set => SetProperty(GoodsWagonModel.Cargo, value, GoodsWagonModel, (m, v) => m.Cargo = v);
    }
}
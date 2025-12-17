// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Domain;

using Moba.Domain.Enum;

/// <summary>
/// ViewModel wrapper for PassengerWagon model.
/// Extends WagonViewModel with passenger-specific properties.
/// </summary>
public partial class PassengerWagonViewModel : WagonViewModel
{
    #region Fields
    // Model (specialized type)
    private PassengerWagon PassengerWagonModel { get; }
    #endregion

    public PassengerWagonViewModel(PassengerWagon model) : base(model)
    {
        PassengerWagonModel = model;
    }

    public PassengerClass WagonClass
    {
        get => PassengerWagonModel.WagonClass;
        set => SetProperty(PassengerWagonModel.WagonClass, value, PassengerWagonModel, (m, v) => m.WagonClass = v);
    }
}

// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;

/// <summary>
/// ViewModel wrapper for PassengerWagon model.
/// Extends WagonViewModel with passenger-specific properties.
/// </summary>
public partial class PassengerWagonViewModel : WagonViewModel
{
    public PassengerWagonViewModel(PassengerWagon model, IUiDispatcher? dispatcher = null)
        : base(model, dispatcher)
    {
        PassengerWagonModel = model;
    }

    private PassengerWagon PassengerWagonModel { get; }

    public PassengerClass WagonClass
    {
        get => PassengerWagonModel.WagonClass;
        set => SetProperty(PassengerWagonModel.WagonClass, value, PassengerWagonModel, (m, v) => m.WagonClass = v);
    }
}
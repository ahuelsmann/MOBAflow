// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

public sealed partial class VehicleItemViewModel : ObservableObject
{
    public VehicleItemViewModel(Vehicle model, string displayName)
    {
        Model = model;
        _displayName = displayName;
    }

    public Vehicle Model { get; }

    [ObservableProperty]
    private string _displayName;

    public Guid VehicleId => Model.VehicleId;

    public TrainVehicleKind VehicleKind => Model.VehicleKind;

    public bool IsLocomotive => VehicleKind == TrainVehicleKind.Locomotive;

    public bool IsPassengerWagon => VehicleKind == TrainVehicleKind.PassengerWagon;

    public bool IsGoodsWagon => VehicleKind == TrainVehicleKind.GoodsWagon;

    public bool IsWagon => !IsLocomotive;

    public bool IsReversed
    {
        get => Model.IsReversed;
        set
        {
            if (Model.IsReversed != value)
            {
                Model.IsReversed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScaleX));
            }
        }
    }

    public double ScaleX => IsReversed ? -1.0 : 1.0;

    [ObservableProperty]
    private IRelayCommand? _removeCommand;

    [ObservableProperty]
    private IRelayCommand? _toggleDirectionCommand;
}
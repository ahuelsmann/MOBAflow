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

    [ObservableProperty]
    private IRelayCommand? _removeCommand;
}
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using System.Collections.ObjectModel;

public sealed class TrainViewModel : ObservableObject, IViewModelWrapper<Train>
{
    private readonly ProjectViewModel _project;

    public TrainViewModel(Train model, ProjectViewModel project)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(project);

        Model = model;
        _project = project;

        EnsureVehiclesInitialized();
        RefreshVehicleItems();
    }

    public Train Model { get; }

    public IReadOnlyList<TrainType> TrainTypeValues { get; } = Enum.GetValues<TrainType>();

    public IReadOnlyList<ServiceType> ServiceTypeValues { get; } = Enum.GetValues<ServiceType>();

    public ObservableCollection<VehicleItemViewModel> VehicleItems { get; } = [];

    public event EventHandler? VehiclesModified;

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, static (m, v) => m.Name = v);
    }

    public string Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, static (m, v) => m.Description = v);
    }

    public bool IsDoubleTraction
    {
        get => Model.IsDoubleTraction;
        set => SetProperty(Model.IsDoubleTraction, value, Model, static (m, v) => m.IsDoubleTraction = v);
    }

    public TrainType TrainType
    {
        get => Model.TrainType;
        set => SetProperty(Model.TrainType, value, Model, static (m, v) => m.TrainType = v);
    }

    public ServiceType ServiceType
    {
        get => Model.ServiceType;
        set => SetProperty(Model.ServiceType, value, Model, static (m, v) => m.ServiceType = v);
    }

    public void InsertLocomotive(LocomotiveViewModel locomotive, int index = -1)
    {
        InsertVehicle(new Vehicle
        {
            VehicleId = locomotive.Model.Id,
            VehicleKind = TrainVehicleKind.Locomotive
        }, index);
    }

    public void InsertPassengerWagon(PassengerWagonViewModel wagon, int index = -1)
    {
        InsertVehicle(new Vehicle
        {
            VehicleId = wagon.Model.Id,
            VehicleKind = TrainVehicleKind.PassengerWagon
        }, index);
    }

    public void InsertGoodsWagon(GoodsWagonViewModel wagon, int index = -1)
    {
        InsertVehicle(new Vehicle
        {
            VehicleId = wagon.Model.Id,
            VehicleKind = TrainVehicleKind.GoodsWagon
        }, index);
    }

    public void SynchronizeVehiclesFromItems()
    {
        Model.Vehicles = [.. VehicleItems.Select(item => new Vehicle
        {
            VehicleId = item.VehicleId,
            VehicleKind = item.VehicleKind,
            IsReversed = item.IsReversed
        })];

        RefreshVehicleItems();
        VehiclesModified?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshVehicleItems()
    {
        EnsureVehiclesInitialized();

        VehicleItems.Clear();
        foreach (var vehicle in Model.Vehicles)
        {
            var item = new VehicleItemViewModel(vehicle, ResolveDisplayName(vehicle))
            {
                RemoveCommand = new RelayCommand(() => RemoveVehicle(vehicle)),
                ToggleDirectionCommand = new RelayCommand(() => ToggleDirection(vehicle))
            };
            VehicleItems.Add(item);
        }

        OnPropertyChanged(nameof(VehicleItems));
    }

    private void ToggleDirection(Vehicle vehicle)
    {
        var item = VehicleItems.FirstOrDefault(i => i.VehicleId == vehicle.VehicleId && i.VehicleKind == vehicle.VehicleKind);
        if (item != null)
        {
            item.IsReversed = !item.IsReversed;
            VehiclesModified?.Invoke(this, EventArgs.Empty);
        }
    }

    private void InsertVehicle(Vehicle vehicle, int index)
    {
        EnsureVehiclesInitialized();

        if (Model.Vehicles.Any(existing => existing.VehicleId == vehicle.VehicleId && existing.VehicleKind == vehicle.VehicleKind))
        {
            return;
        }

        if (index < 0 || index > Model.Vehicles.Count)
        {
            Model.Vehicles.Add(vehicle);
        }
        else
        {
            Model.Vehicles.Insert(index, vehicle);
        }

        RefreshVehicleItems();
    }

    private void RemoveVehicle(Vehicle vehicle)
    {
        EnsureVehiclesInitialized();

        var index = Model.Vehicles.FindIndex(existing => existing.VehicleId == vehicle.VehicleId && existing.VehicleKind == vehicle.VehicleKind);
        if (index < 0)
        {
            return;
        }

        Model.Vehicles.RemoveAt(index);
        RefreshVehicleItems();
        VehiclesModified?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureVehiclesInitialized()
    {
        Model.Vehicles ??= [];
    }

    private string ResolveDisplayName(Vehicle vehicle)
    {
        return vehicle.VehicleKind switch
        {
            TrainVehicleKind.Locomotive => _project.Model.Locomotives.FirstOrDefault(locomotive => locomotive.Id == vehicle.VehicleId)?.Name ?? "Unknown Locomotive",
            TrainVehicleKind.PassengerWagon => _project.Model.PassengerWagons.FirstOrDefault(wagon => wagon.Id == vehicle.VehicleId)?.Name ?? "Unknown Passenger Wagon",
            TrainVehicleKind.GoodsWagon => _project.Model.GoodsWagons.FirstOrDefault(wagon => wagon.Id == vehicle.VehicleId)?.Name ?? "Unknown Goods Wagon",
            _ => "Unknown Vehicle"
        };
    }
}
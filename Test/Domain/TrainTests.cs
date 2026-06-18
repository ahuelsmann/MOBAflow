// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;
using Moba.Domain.Enum;

[TestFixture]
internal class TrainTests
{
    [Test]
    public void Constructor_InitializesDefaults()
    {
        var train = new Train();

        Assert.That(train.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(train.Name, Is.EqualTo("New Train"));
        Assert.That(train.Description, Is.EqualTo(string.Empty));
        Assert.That(train.IsDoubleTraction, Is.False);
        Assert.That(train.TrainType, Is.EqualTo(TrainType.None));
        Assert.That(train.ServiceType, Is.EqualTo(ServiceType.None));
        Assert.That(train.Vehicles, Is.Not.Null);
        Assert.That(train.Vehicles, Is.Empty);
    }

    [Test]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var locoId1 = Guid.NewGuid();
        var locoId2 = Guid.NewGuid();
        var wagonId = Guid.NewGuid();
        var vehicles = new List<Vehicle>
        {
            new() { VehicleId = locoId1, VehicleKind = TrainVehicleKind.Locomotive },
            new() { VehicleId = wagonId, VehicleKind = TrainVehicleKind.PassengerWagon },
            new() { VehicleId = locoId2, VehicleKind = TrainVehicleKind.Locomotive }
        };

        var train = new Train
        {
            Id = id,
            Name = "ICE 123",
            Description = "München - Hamburg",
            IsDoubleTraction = true,
            TrainType = TrainType.Passenger,
            ServiceType = ServiceType.InterCityExpress,
            Vehicles = vehicles
        };

        Assert.That(train.Id, Is.EqualTo(id));
        Assert.That(train.Name, Is.EqualTo("ICE 123"));
        Assert.That(train.Description, Is.EqualTo("München - Hamburg"));
        Assert.That(train.IsDoubleTraction, Is.True);
        Assert.That(train.TrainType, Is.EqualTo(TrainType.Passenger));
        Assert.That(train.ServiceType, Is.EqualTo(ServiceType.InterCityExpress));
        Assert.That(train.Vehicles, Is.SameAs(vehicles));
        Assert.That(train.Vehicles.Select(item => item.VehicleKind), Is.EqualTo(new[]
        {
            TrainVehicleKind.Locomotive,
            TrainVehicleKind.PassengerWagon,
            TrainVehicleKind.Locomotive
        }));
    }

    [Test]
    public void Vehicles_CanAddAndRemove()
    {
        var train = new Train();
        var locoId = Guid.NewGuid();
        var wagonId = Guid.NewGuid();

        train.Vehicles.Add(new Vehicle { VehicleId = locoId, VehicleKind = TrainVehicleKind.Locomotive });
        train.Vehicles.Add(new Vehicle { VehicleId = wagonId, VehicleKind = TrainVehicleKind.GoodsWagon });
        Assert.That(train.Vehicles, Has.Count.EqualTo(2));

        train.Vehicles.RemoveAt(0);
        Assert.That(train.Vehicles, Has.Count.EqualTo(1));
        Assert.That(train.Vehicles[0].VehicleId, Is.EqualTo(wagonId));
    }

    [Test]
    public void Vehicles_CanStoreMixedOrderedVehicles()
    {
        var locomotiveId = Guid.NewGuid();
        var passengerWagonId = Guid.NewGuid();
        var goodsWagonId = Guid.NewGuid();
        var train = new Train();

        train.Vehicles.Add(new Vehicle { VehicleId = locomotiveId, VehicleKind = TrainVehicleKind.Locomotive });
        train.Vehicles.Add(new Vehicle { VehicleId = passengerWagonId, VehicleKind = TrainVehicleKind.PassengerWagon });
        train.Vehicles.Add(new Vehicle { VehicleId = goodsWagonId, VehicleKind = TrainVehicleKind.GoodsWagon });

        Assert.That(train.Vehicles, Has.Count.EqualTo(3));
        Assert.That(train.Vehicles[0].VehicleId, Is.EqualTo(locomotiveId));
        Assert.That(train.Vehicles[1].VehicleKind, Is.EqualTo(TrainVehicleKind.PassengerWagon));
        Assert.That(train.Vehicles[2].VehicleKind, Is.EqualTo(TrainVehicleKind.GoodsWagon));
    }

    [TestCase(TrainType.None)]
    [TestCase(TrainType.Passenger)]
    [TestCase(TrainType.Freight)]
    [TestCase(TrainType.Maintenance)]
    [TestCase(TrainType.Special)]
    public void TrainType_AllValuesSupported(TrainType trainType)
    {
        var train = new Train { TrainType = trainType };
        Assert.That(train.TrainType, Is.EqualTo(trainType));
    }

    [TestCase(ServiceType.None)]
    [TestCase(ServiceType.RegionalExpress)]
    [TestCase(ServiceType.InterCity)]
    [TestCase(ServiceType.InterCityExpress)]
    [TestCase(ServiceType.InterRegio)]
    [TestCase(ServiceType.Freight)]
    [TestCase(ServiceType.Special)]
    public void ServiceType_AllValuesSupported(ServiceType serviceType)
    {
        var train = new Train { ServiceType = serviceType };
        Assert.That(train.ServiceType, Is.EqualTo(serviceType));
    }
}
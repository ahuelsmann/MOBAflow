// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain.Enum;

[TestFixture]
public class TrainTests
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
        Assert.That(train.LocomotiveIds, Is.Not.Null);
        Assert.That(train.LocomotiveIds, Is.Empty);
        Assert.That(train.WagonIds, Is.Not.Null);
        Assert.That(train.WagonIds, Is.Empty);
    }

    [Test]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var locoId1 = Guid.NewGuid();
        var locoId2 = Guid.NewGuid();
        var wagonId = Guid.NewGuid();

        var train = new Train
        {
            Id = id,
            Name = "ICE 123",
            Description = "München - Hamburg",
            IsDoubleTraction = true,
            TrainType = TrainType.Passenger,
            ServiceType = ServiceType.InterCityExpress,
            LocomotiveIds = [locoId1, locoId2],
            WagonIds = [wagonId]
        };

        Assert.That(train.Id, Is.EqualTo(id));
        Assert.That(train.Name, Is.EqualTo("ICE 123"));
        Assert.That(train.Description, Is.EqualTo("München - Hamburg"));
        Assert.That(train.IsDoubleTraction, Is.True);
        Assert.That(train.TrainType, Is.EqualTo(TrainType.Passenger));
        Assert.That(train.ServiceType, Is.EqualTo(ServiceType.InterCityExpress));
        Assert.That(train.LocomotiveIds, Has.Count.EqualTo(2));
        Assert.That(train.LocomotiveIds, Does.Contain(locoId1));
        Assert.That(train.LocomotiveIds, Does.Contain(locoId2));
        Assert.That(train.WagonIds, Has.Count.EqualTo(1));
        Assert.That(train.WagonIds, Does.Contain(wagonId));
    }

    [Test]
    public void LocomotiveIds_CanAddAndRemove()
    {
        var train = new Train();
        var locoId = Guid.NewGuid();

        train.LocomotiveIds.Add(locoId);
        Assert.That(train.LocomotiveIds, Has.Count.EqualTo(1));

        train.LocomotiveIds.Remove(locoId);
        Assert.That(train.LocomotiveIds, Is.Empty);
    }

    [Test]
    public void WagonIds_CanAddAndRemove()
    {
        var train = new Train();
        var wagonId = Guid.NewGuid();

        train.WagonIds.Add(wagonId);
        Assert.That(train.WagonIds, Has.Count.EqualTo(1));

        train.WagonIds.Remove(wagonId);
        Assert.That(train.WagonIds, Is.Empty);
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

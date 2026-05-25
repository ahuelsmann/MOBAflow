// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

[TestFixture]
internal class StationTests
{
    [Test]
    public void Constructor_InitializesDefaults()
    {
        var station = new Station();

        Assert.That(station.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(station.Name, Is.EqualTo("New Station"));
        Assert.That(station.Description, Is.Null);
        Assert.That(station.InPort, Is.EqualTo(0u));
        Assert.That(station.NumberOfLapsToStop, Is.EqualTo(1u));
        Assert.That(station.WorkflowId, Is.Null);
        Assert.That(station.IsVirtual, Is.False);
        Assert.That(station.IsExitOnLeft, Is.False);
        Assert.That(station.Platforms, Is.Not.Null);
        Assert.That(station.Platforms, Is.Empty);
        Assert.That(station.Arrival, Is.Null);
        Assert.That(station.Departure, Is.Null);
        Assert.That(station.Connections, Is.Not.Null);
        Assert.That(station.Connections, Is.Empty);
    }

    [Test]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var arrival = new DateTime(2026, 1, 24, 10, 30, 0);
        var departure = new DateTime(2026, 1, 24, 10, 35, 0);
        var connections = new List<ConnectingService> { new() };

        var station = new Station
        {
            Id = id,
            Name = "München Hbf",
            Description = "Hauptbahnhof München",
            InPort = 15,
            NumberOfLapsToStop = 3,
            WorkflowId = workflowId,
            IsVirtual = true,
            IsExitOnLeft = true,
            Platforms = [new Platform { Number = 5, InPort = 20 }],
            Arrival = arrival,
            Departure = departure,
            Connections = connections
        };

        Assert.That(station.Id, Is.EqualTo(id));
        Assert.That(station.Name, Is.EqualTo("München Hbf"));
        Assert.That(station.Description, Is.EqualTo("Hauptbahnhof München"));
        Assert.That(station.InPort, Is.EqualTo(15u));
        Assert.That(station.NumberOfLapsToStop, Is.EqualTo(3u));
        Assert.That(station.WorkflowId, Is.EqualTo(workflowId));
        Assert.That(station.IsVirtual, Is.True);
        Assert.That(station.IsExitOnLeft, Is.True);
        Assert.That(station.Platforms, Has.Count.EqualTo(1));
        Assert.That(station.Platforms[0].Number, Is.EqualTo(5u));
        Assert.That(station.Arrival, Is.EqualTo(arrival));
        Assert.That(station.Departure, Is.EqualTo(departure));
        Assert.That(station.Connections, Is.SameAs(connections));
    }

    [Test]
    public void Connections_CanAddAndRemove()
    {
        var station = new Station();
        var connection = new ConnectingService { Name = "ICE 123" };

        station.Connections.Add(connection);
        Assert.That(station.Connections, Has.Count.EqualTo(1));
        Assert.That(station.Connections[0].Name, Is.EqualTo("ICE 123"));

        station.Connections.Remove(connection);
        Assert.That(station.Connections, Is.Empty);
    }

    [Test]
    public void Platforms_CanAddAndRemove()
    {
        var station = new Station();
        var platform = new Platform { Number = 12 };

        station.Platforms.Add(platform);
        Assert.That(station.Platforms, Has.Count.EqualTo(1));
        Assert.That(station.Platforms[0].Number, Is.EqualTo(12u));

        station.Platforms.Remove(platform);
        Assert.That(station.Platforms, Is.Empty);
    }
}

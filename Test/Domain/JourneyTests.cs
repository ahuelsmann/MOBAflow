// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain.Enum;

[TestFixture]
internal class JourneyTests
{
    [Test]
    public void EventPlanRoundTripPreservesSparseCountsAndActiveFlag()
    {
        var workflowId = Guid.NewGuid();
        var journey = new Journey { IsActive = true, EventPlan = new JourneyEventPlan { Events =
            [new JourneyEvent { InPort = 2, Count = ulong.MaxValue, WorkflowId = workflowId }] } };
        var copy = System.Text.Json.JsonSerializer.Deserialize<Journey>(System.Text.Json.JsonSerializer.Serialize(journey))!;
        Assert.Multiple(() =>
        {
            Assert.That(copy.IsActive, Is.True);
            Assert.That(copy.EventPlan.Events.Single().Count, Is.EqualTo(ulong.MaxValue));
            Assert.That(copy.EventPlan.Events.Single().WorkflowId, Is.EqualTo(workflowId));
            Assert.That(copy.EventPlan.Events.Single().Id, Is.EqualTo(journey.EventPlan.Events.Single().Id));
        });
    }

    [Test]
    public void Constructor_InitializesDefaults()
    {
        var journey = new Journey();

        Assert.That(journey.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(journey.Name, Is.EqualTo("New Journey"));
        Assert.That(journey.Description, Is.EqualTo(string.Empty));
        Assert.That(journey.Text, Is.EqualTo(string.Empty));
        Assert.That(journey.Stations, Is.Not.Null);
        Assert.That(journey.Stations, Is.Empty);
        Assert.That(journey.EventPlan.Events, Is.Empty);
        Assert.That(journey.IsActive, Is.False);
        Assert.That(journey.BehaviorOnLastStop, Is.EqualTo(BehaviorOnLastStop.None));
        Assert.That(journey.FirstPos, Is.EqualTo(0u));
    }

    [Test]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var stations = new List<Station> { new() };

        var journey = new Journey
        {
            Id = id,
            Name = "Test Journey",
            Description = "Test Description",
            Text = "Some text",
            Stations = stations,
            IsActive = true,
            EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { InPort = 42 }] },
            BehaviorOnLastStop = BehaviorOnLastStop.BeginAgainFromFistStop,
            FirstPos = 2
        };

        Assert.That(journey.Id, Is.EqualTo(id));
        Assert.That(journey.Name, Is.EqualTo("Test Journey"));
        Assert.That(journey.Description, Is.EqualTo("Test Description"));
        Assert.That(journey.Text, Is.EqualTo("Some text"));
        Assert.That(journey.Stations, Is.SameAs(stations));
        Assert.That(journey.IsActive, Is.True);
        Assert.That(journey.EventPlan.Events.Single().InPort, Is.EqualTo(42u));
        Assert.That(journey.EventPlan.Events.Single().Count, Is.EqualTo(1UL));
        Assert.That(journey.BehaviorOnLastStop, Is.EqualTo(BehaviorOnLastStop.BeginAgainFromFistStop));
        Assert.That(journey.FirstPos, Is.EqualTo(2u));
    }

    [Test]
    public void Stations_CanAddAndRemove()
    {
        var journey = new Journey();
        var station = new Station { Name = "Berlin Hbf" };

        journey.Stations.Add(station);
        Assert.That(journey.Stations, Has.Count.EqualTo(1));
        Assert.That(journey.Stations[0].Name, Is.EqualTo("Berlin Hbf"));

        journey.Stations.Remove(station);
        Assert.That(journey.Stations, Is.Empty);
    }

    [Test]
    public void BehaviorOnLastStop_AllValuesSupported()
    {
        var journey = new Journey();

        journey.BehaviorOnLastStop = BehaviorOnLastStop.None;
        Assert.That(journey.BehaviorOnLastStop, Is.EqualTo(BehaviorOnLastStop.None));

        journey.BehaviorOnLastStop = BehaviorOnLastStop.BeginAgainFromFistStop;
        Assert.That(journey.BehaviorOnLastStop, Is.EqualTo(BehaviorOnLastStop.BeginAgainFromFistStop));
    }
}

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
        };

        Assert.That(journey.Id, Is.EqualTo(id));
        Assert.That(journey.Name, Is.EqualTo("Test Journey"));
        Assert.That(journey.Description, Is.EqualTo("Test Description"));
        Assert.That(journey.Text, Is.EqualTo("Some text"));
        Assert.That(journey.Stations, Is.SameAs(stations));
        Assert.That(journey.IsActive, Is.True);
        Assert.That(journey.EventPlan.Events.Single().InPort, Is.EqualTo(42u));
        Assert.That(journey.EventPlan.Events.Single().Count, Is.EqualTo(1UL));
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

    private static Journey ThreeStops() =>
        new() { Stations = [new Station { Name = "Porta" }, new Station { Name = "Minden" }, new Station { Name = "Bielefeld" }] };

    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(-1, 0)]
    public void ResolveStopTransition_NextMovesOneStopAndStartsAtTheFirstStopWhenTheCurrentOneIsUnknown(int current, int expected)
    {
        var target = ThreeStops().ResolveStopTransition(current, new JourneyStopTransition { Mode = JourneyStopTransitionMode.Next });

        Assert.That(target, Is.EqualTo(expected));
    }

    [Test]
    public void ResolveStopTransition_NextAtTheLastStopAndNoneKeepTheCurrentStop()
    {
        var journey = ThreeStops();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(journey.ResolveStopTransition(2, new JourneyStopTransition { Mode = JourneyStopTransitionMode.Next }), Is.Null);
            Assert.That(journey.ResolveStopTransition(1, new JourneyStopTransition { Mode = JourneyStopTransitionMode.None }), Is.Null);
        }
    }

    [Test]
    public void ResolveStopTransition_SpecificStationFindsItsStopOrRejectsAForeignStation()
    {
        var journey = ThreeStops();
        var target = journey.Stations[2].Id;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(journey.ResolveStopTransition(0,
                new JourneyStopTransition { Mode = JourneyStopTransitionMode.SpecificStation, StationId = target }), Is.EqualTo(2));
            Assert.Throws<InvalidOperationException>(() => journey.ResolveStopTransition(0,
                new JourneyStopTransition { Mode = JourneyStopTransitionMode.SpecificStation, StationId = Guid.NewGuid() }));
            Assert.Throws<ArgumentNullException>(() => journey.ResolveStopTransition(0, null!));
        }
    }

    [Test]
    public void IndexOfStop_PrefersTheStationIdOverThePosition()
    {
        var journey = ThreeStops();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(journey.IndexOfStop(journey.Stations[1].Id, fallbackPosition: 0), Is.EqualTo(1));
            Assert.That(journey.IndexOfStop(null, fallbackPosition: 2), Is.EqualTo(2));
            Assert.That(journey.IndexOfStop(Guid.NewGuid(), fallbackPosition: 2), Is.EqualTo(-1));
        }
    }
}

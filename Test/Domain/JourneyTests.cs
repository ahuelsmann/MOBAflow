// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.Domain;

using Moba.Domain.Enum;

[TestFixture]
public class JourneyTests
{
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
        Assert.That(journey.InPort, Is.EqualTo(0u));
        Assert.That(journey.IsUsingTimerToIgnoreFeedbacks, Is.False);
        Assert.That(journey.IntervalForTimerToIgnoreFeedbacks, Is.EqualTo(0.0));
        Assert.That(journey.BehaviorOnLastStop, Is.EqualTo(BehaviorOnLastStop.None));
        Assert.That(journey.NextJourneyId, Is.Null);
        Assert.That(journey.FirstPos, Is.EqualTo(0u));
    }

    [Test]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var nextJourneyId = Guid.NewGuid();
        var stations = new List<Station> { new() };

        var journey = new Journey
        {
            Id = id,
            Name = "Test Journey",
            Description = "Test Description",
            Text = "Some text",
            Stations = stations,
            InPort = 42,
            IsUsingTimerToIgnoreFeedbacks = true,
            IntervalForTimerToIgnoreFeedbacks = 1500.0,
            BehaviorOnLastStop = BehaviorOnLastStop.GotoJourney,
            NextJourneyId = nextJourneyId,
            FirstPos = 2
        };

        Assert.That(journey.Id, Is.EqualTo(id));
        Assert.That(journey.Name, Is.EqualTo("Test Journey"));
        Assert.That(journey.Description, Is.EqualTo("Test Description"));
        Assert.That(journey.Text, Is.EqualTo("Some text"));
        Assert.That(journey.Stations, Is.SameAs(stations));
        Assert.That(journey.InPort, Is.EqualTo(42u));
        Assert.That(journey.IsUsingTimerToIgnoreFeedbacks, Is.True);
        Assert.That(journey.IntervalForTimerToIgnoreFeedbacks, Is.EqualTo(1500.0));
        Assert.That(journey.BehaviorOnLastStop, Is.EqualTo(BehaviorOnLastStop.GotoJourney));
        Assert.That(journey.NextJourneyId, Is.EqualTo(nextJourneyId));
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

        journey.BehaviorOnLastStop = BehaviorOnLastStop.GotoJourney;
        Assert.That(journey.BehaviorOnLastStop, Is.EqualTo(BehaviorOnLastStop.GotoJourney));
    }
}

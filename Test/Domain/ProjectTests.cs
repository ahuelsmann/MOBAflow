// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.Domain;

using Moba.Domain;
using Moba.TrackPlan.Graph;

[TestFixture]
public class ProjectTests
{
    [Test]
    public void Constructor_InitializesDefaults()
    {
        var project = new Project();

        Assert.That(project.Name, Is.EqualTo(string.Empty));
        Assert.That(project.SpeakerEngines, Is.Not.Null);
        Assert.That(project.SpeakerEngines, Is.Empty);
        Assert.That(project.Voices, Is.Not.Null);
        Assert.That(project.Voices, Is.Empty);
        Assert.That(project.Locomotives, Is.Not.Null);
        Assert.That(project.Locomotives, Is.Empty);
        Assert.That(project.PassengerWagons, Is.Not.Null);
        Assert.That(project.PassengerWagons, Is.Empty);
        Assert.That(project.GoodsWagons, Is.Not.Null);
        Assert.That(project.GoodsWagons, Is.Empty);
        Assert.That(project.Trains, Is.Not.Null);
        Assert.That(project.Trains, Is.Empty);
        Assert.That(project.Workflows, Is.Not.Null);
        Assert.That(project.Workflows, Is.Empty);
        Assert.That(project.Journeys, Is.Not.Null);
        Assert.That(project.Journeys, Is.Empty);
        Assert.That(project.TrackPlan, Is.Null);
        Assert.That(project.SignalBoxPlan, Is.Null);
    }

    [Test]
    public void Properties_CanBeSet()
    {
        var speakerEngines = new List<SpeakerEngineConfiguration> { new() };
        var voices = new List<Voice> { new() };
        var locomotives = new List<Locomotive> { new() };
        var passengerWagons = new List<PassengerWagon> { new() };
        var goodsWagons = new List<GoodsWagon> { new() };
        var trains = new List<Train> { new() };
        var workflows = new List<Workflow> { new() };
        var journeys = new List<Journey> { new() };
        var trackPlan = new TopologyGraph();
        var signalBoxPlan = new SignalBoxPlan();

        var project = new Project
        {
            Name = "Meine Anlage",
            SpeakerEngines = speakerEngines,
            Voices = voices,
            Locomotives = locomotives,
            PassengerWagons = passengerWagons,
            GoodsWagons = goodsWagons,
            Trains = trains,
            Workflows = workflows,
            Journeys = journeys,
            TrackPlan = trackPlan,
            SignalBoxPlan = signalBoxPlan
        };

        Assert.That(project.Name, Is.EqualTo("Meine Anlage"));
        Assert.That(project.SpeakerEngines, Is.SameAs(speakerEngines));
        Assert.That(project.Voices, Is.SameAs(voices));
        Assert.That(project.Locomotives, Is.SameAs(locomotives));
        Assert.That(project.PassengerWagons, Is.SameAs(passengerWagons));
        Assert.That(project.GoodsWagons, Is.SameAs(goodsWagons));
        Assert.That(project.Trains, Is.SameAs(trains));
        Assert.That(project.Workflows, Is.SameAs(workflows));
        Assert.That(project.Journeys, Is.SameAs(journeys));
        Assert.That(project.TrackPlan, Is.SameAs(trackPlan));
        Assert.That(project.SignalBoxPlan, Is.SameAs(signalBoxPlan));
    }

    [Test]
    public void Journeys_CanAddAndRemove()
    {
        var project = new Project();
        var journey = new Journey { Name = "Rundfahrt" };

        project.Journeys.Add(journey);
        Assert.That(project.Journeys, Has.Count.EqualTo(1));
        Assert.That(project.Journeys[0].Name, Is.EqualTo("Rundfahrt"));

        project.Journeys.Remove(journey);
        Assert.That(project.Journeys, Is.Empty);
    }

    [Test]
    public void Workflows_CanAddAndRemove()
    {
        var project = new Project();
        var workflow = new Workflow { Name = "Ankunft" };

        project.Workflows.Add(workflow);
        Assert.That(project.Workflows, Has.Count.EqualTo(1));
        Assert.That(project.Workflows[0].Name, Is.EqualTo("Ankunft"));

        project.Workflows.Remove(workflow);
        Assert.That(project.Workflows, Is.Empty);
    }

    [Test]
    public void Trains_CanAddAndRemove()
    {
        var project = new Project();
        var train = new Train { Name = "ICE 1234" };

        project.Trains.Add(train);
        Assert.That(project.Trains, Has.Count.EqualTo(1));
        Assert.That(project.Trains[0].Name, Is.EqualTo("ICE 1234"));

        project.Trains.Remove(train);
        Assert.That(project.Trains, Is.Empty);
    }

    [Test]
    public void Locomotives_CanAddAndRemove()
    {
        var project = new Project();
        var loco = new Locomotive { Name = "BR 101" };

        project.Locomotives.Add(loco);
        Assert.That(project.Locomotives, Has.Count.EqualTo(1));
        Assert.That(project.Locomotives[0].Name, Is.EqualTo("BR 101"));

        project.Locomotives.Remove(loco);
        Assert.That(project.Locomotives, Is.Empty);
    }
}

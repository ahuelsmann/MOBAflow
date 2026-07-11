// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Moba.Domain.Enum;

[TestFixture]
internal class JourneyDefaultsTests
{
    [Test]
    public void Constructor_Should_Initialize_DefaultValues()
    {
        // Act
        var journey = new Journey();

        // Assert
        Assert.That(journey.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(journey.Name, Is.EqualTo("New Journey"));
        Assert.That(journey.Description, Is.EqualTo(string.Empty));
        Assert.That(journey.Text, Is.EqualTo(string.Empty));
        Assert.That(journey.Stations, Is.Not.Null);
        Assert.That(journey.Stations, Is.Empty);
        Assert.That(journey.BehaviorOnLastStop, Is.EqualTo(BehaviorOnLastStop.None));
        Assert.That(journey.NextJourneyId, Is.Null);
        Assert.That(journey.FirstPos, Is.EqualTo(0u));
        Assert.That(journey.FeedbackSequence, Is.Not.Null);
        Assert.That(journey.FeedbackSequence, Is.Empty);
    }
}

[TestFixture]
internal class StationDefaultsTests
{
    [Test]
    public void Constructor_Should_Initialize_DefaultValues()
    {
        // Act
        var station = new Station();

        // Assert
        Assert.That(station.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(station.Name, Is.EqualTo("New Station"));
        Assert.That(station.Connections, Is.Not.Null);
        Assert.That(station.Connections, Is.Empty);
        Assert.That(station.Platforms, Is.Not.Null);
        Assert.That(station.Platforms, Is.Empty);
        Assert.That(station.Arrival, Is.Null);
        Assert.That(station.Departure, Is.Null);
        Assert.That(station.IsExitOnLeft, Is.False);
    }
}

[TestFixture]
internal class WorkflowDefaultsTests
{
    [Test]
    public void Constructor_Should_Initialize_DefaultValues()
    {
        // Act
        var workflow = new Workflow();

        // Assert
        Assert.That(workflow.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(workflow.Name, Is.EqualTo("New Flow"));
        Assert.That(workflow.Description, Is.EqualTo(string.Empty));
        Assert.That(workflow.Actions, Is.Not.Null);
        Assert.That(workflow.Actions, Is.Empty);
        Assert.That(workflow.ExecutionMode, Is.EqualTo(WorkflowExecutionMode.Sequential));
        Assert.That(workflow.InPort, Is.EqualTo(0u));
        Assert.That(workflow.IsUsingTimerToIgnoreFeedbacks, Is.False);
        Assert.That(workflow.IntervalForTimerToIgnoreFeedbacks, Is.EqualTo(0d));
    }
}

[TestFixture]
internal class TrainDefaultsTests
{
    [Test]
    public void Constructor_Should_Initialize_DefaultValues()
    {
        // Act
        var train = new Train();

        // Assert
        Assert.That(train.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(train.Name, Is.EqualTo("New Train"));
        Assert.That(train.Description, Is.EqualTo(string.Empty));
        Assert.That(train.Vehicles, Is.Not.Null);
        Assert.That(train.Vehicles, Is.Empty);
        Assert.That(train.TrainType, Is.EqualTo(TrainType.None));
        Assert.That(train.ServiceType, Is.EqualTo(ServiceType.None));
        Assert.That(train.IsDoubleTraction, Is.False);
    }
}

[TestFixture]
internal class ProjectDefaultsTests
{
    [Test]
    public void Constructor_Should_Initialize_DefaultValues()
    {
        // Act
        var project = new Project();

        // Assert
        Assert.That(project.Name, Is.EqualTo(string.Empty));
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
        Assert.That(project.Stations, Is.Not.Null);
        Assert.That(project.Stations, Is.Empty);
        Assert.That(project.SignalBoxPlan, Is.Null);
    }
}

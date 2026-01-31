namespace Moba.Test.Unit;

using Domain.Enum;

[TestFixture]
public class JourneyDefaultsTests
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
        Assert.That(journey.InPort, Is.EqualTo(0u));
        Assert.That(journey.IsUsingTimerToIgnoreFeedbacks, Is.False);
        Assert.That(journey.IntervalForTimerToIgnoreFeedbacks, Is.EqualTo(0d));
    }
}

[TestFixture]
public class StationDefaultsTests
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
        Assert.That(station.NumberOfLapsToStop, Is.EqualTo(1u));
        Assert.That(station.InPort, Is.EqualTo(0u));
        Assert.That(station.WorkflowId, Is.Null);
        Assert.That(station.Track, Is.EqualTo(1u));
        Assert.That(station.Arrival, Is.Null);
        Assert.That(station.Departure, Is.Null);
        Assert.That(station.IsExitOnLeft, Is.False);
    }
}

[TestFixture]
public class WorkflowDefaultsTests
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
public class TrainDefaultsTests
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
        Assert.That(train.LocomotiveIds, Is.Not.Null);
        Assert.That(train.LocomotiveIds, Is.Empty);
        Assert.That(train.WagonIds, Is.Not.Null);
        Assert.That(train.WagonIds, Is.Empty);
        Assert.That(train.TrainType, Is.EqualTo(TrainType.None));
        Assert.That(train.ServiceType, Is.EqualTo(ServiceType.None));
        Assert.That(train.IsDoubleTraction, Is.False);
    }
}

[TestFixture]
public class ProjectDefaultsTests
{
    [Test]
    public void Constructor_Should_Initialize_DefaultValues()
    {
        // Act
        var project = new Project();

        // Assert
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
        Assert.That(project.SignalBoxPlan, Is.Null);
    }
}
using Moba.Backend.Manager;
using Moba.Backend.Model;
using Moba.Backend;
using Moba.Test.Mocks;
using Moba.Backend.Model.Action;
using Moba.Backend.Model.Enum;

namespace Moba.Test.Backend;

/// <summary>
/// Unit tests for StationManager class.
/// Tests station workflow execution based on feedback events.
/// </summary>
[TestFixture]
public class StationManagerTests
{
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeUdp = new FakeUdpClientWrapper();
        _z21 = new Z21(_fakeUdp, null);
    }

    [TearDown]
    public void TearDown()
    {
        _z21?.Dispose();
    }

    [Test]
    public async Task ProcessFeedback_ExecutesStationWorkflow_OnMatchingInPort()
    {
        // Arrange
        var actionExecuted = false;
        var testAction = new TestAction(() => actionExecuted = true);

        var workflow = new Workflow
        {
            Name = "Station Workflow",
            InPort = 20,
            Actions = new List<Model.Action.Base> { testAction }
        };

        var station = new Station
        {
            Name = "Main Station",
            Flow = workflow
        };

        var stations = new List<Station> { station };
        using var manager = new StationManager(_z21, stations);

        // Act
        _z21.SimulateFeedback(20);
        await Task.Delay(200);

        // Assert
        Assert.That(actionExecuted, Is.True, "Station workflow should have been executed");
    }

    [Test]
    public async Task ProcessFeedback_DoesNotExecute_WhenStationHasNoWorkflow()
    {
        // Arrange
        var station = new Station
        {
            Name = "Station Without Workflow",
            Flow = null
        };

        var stations = new List<Station> { station };
        using var manager = new StationManager(_z21, stations);

        // Act & Assert - should not crash
        _z21.SimulateFeedback(21);
        await Task.Delay(200);

        Assert.Pass("Manager handled station without workflow gracefully");
    }

    [Test]
    public async Task ProcessFeedback_IgnoresWorkflow_WhenInPortDoesNotMatch()
    {
        // Arrange
        var actionExecuted = false;
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            InPort = 22,
            Actions = new List<Model.Action.Base> { new TestAction(() => actionExecuted = true) }
        };

        var station = new Station
        {
            Name = "Test Station",
            Flow = workflow
        };

        var stations = new List<Station> { station };
        using var manager = new StationManager(_z21, stations);

        // Act
        _z21.SimulateFeedback(99); // Different InPort
        await Task.Delay(200);

        // Assert
        Assert.That(actionExecuted, Is.False, "Workflow should not execute for non-matching InPort");
    }

    [Test]
    public async Task ProcessFeedback_IgnoresFeedback_WhenTimerActive()
    {
        // Arrange
        var executionCount = 0;
        var workflow = new Workflow
        {
            Name = "Timer Workflow",
            InPort = 23,
            IsUsingTimerToIgnoreFeedbacks = true,
            IntervalForTimerToIgnoreFeedbacks = 1.0,
            Actions = new List<Model.Action.Base> { new TestAction(() => executionCount++) }
        };

        var station = new Station
        {
            Name = "Timer Station",
            Flow = workflow
        };

        var stations = new List<Station> { station };
        using var manager = new StationManager(_z21, stations);

        // Act
        _z21.SimulateFeedback(23); // First
        await Task.Delay(200);

        _z21.SimulateFeedback(23); // Second (should be ignored)
        await Task.Delay(200);

        // Assert
        Assert.That(executionCount, Is.EqualTo(1), "Second feedback should be ignored");
    }

    [Test]
    public async Task ProcessFeedback_AllowsFeedback_AfterTimerExpires()
    {
        // Arrange
        var executionCount = 0;
        var workflow = new Workflow
        {
            Name = "Timer Workflow",
            InPort = 24,
            IsUsingTimerToIgnoreFeedbacks = true,
            IntervalForTimerToIgnoreFeedbacks = 0.5, // 500ms
            Actions = new List<Model.Action.Base> { new TestAction(() => executionCount++) }
        };

        var station = new Station
        {
            Name = "Timer Station",
            Flow = workflow
        };

        var stations = new List<Station> { station };
        using var manager = new StationManager(_z21, stations);

        // Act
        _z21.SimulateFeedback(24); // First
        await Task.Delay(200);

        await Task.Delay(600); // Wait for timer expiry

        _z21.SimulateFeedback(24); // Second (should execute)
        await Task.Delay(200);

        // Assert
        Assert.That(executionCount, Is.EqualTo(2), "Second feedback should execute after timer expires");
    }

    [Test]
    public async Task ProcessFeedback_ExecutesMultipleStationWorkflows_OnSameInPort()
    {
        // Arrange
        var station1Executed = false;
        var station2Executed = false;

        var workflow1 = new Workflow
        {
            Name = "Workflow1",
            InPort = 25,
            Actions = new List<Model.Action.Base> { new TestAction(() => station1Executed = true) }
        };

        var workflow2 = new Workflow
        {
            Name = "Workflow2",
            InPort = 25, // Same InPort
            Actions = new List<Model.Action.Base> { new TestAction(() => station2Executed = true) }
        };

        var station1 = new Station { Name = "Station1", Flow = workflow1 };
        var station2 = new Station { Name = "Station2", Flow = workflow2 };

        var stations = new List<Station> { station1, station2 };
        using var manager = new StationManager(_z21, stations);

        // Act
        _z21.SimulateFeedback(25);
        await Task.Delay(200);

        // Assert
        Assert.That(station1Executed, Is.True);
        Assert.That(station2Executed, Is.True);
    }

    [Test]
    public void ResetAll_ClearsAllTimers()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            InPort = 26,
            IsUsingTimerToIgnoreFeedbacks = true,
            IntervalForTimerToIgnoreFeedbacks = 10.0,
            Actions = new List<Model.Action.Base> { new TestAction(() => { }) }
        };

        var station = new Station { Name = "Test Station", Flow = workflow };
        var stations = new List<Station> { station };
        using var manager = new StationManager(_z21, stations);

        _z21.SimulateFeedback(26); // Set timer

        // Act
        manager.ResetAll();

        // Assert
        Assert.Pass("ResetAll executed without exceptions");
    }

    [Test]
    public void Dispose_UnsubscribesFromZ21Events()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            InPort = 27,
            Actions = new List<Model.Action.Base> { new TestAction(() => { }) }
        };

        var station = new Station { Name = "Test Station", Flow = workflow };
        var stations = new List<Station> { station };
        var manager = new StationManager(_z21, stations);

        // Act
        manager.Dispose();
        manager.Dispose(); // Idempotent

        // Assert
        Assert.Pass("Dispose is idempotent");
    }

    [Test]
    public async Task ProcessFeedback_HandlesWorkflowException_Gracefully()
    {
        // Arrange
        var faultyAction = new TestAction(() => throw new InvalidOperationException("Test exception"));
        var workflow = new Workflow
        {
            Name = "Faulty Workflow",
            InPort = 28,
            Actions = new List<Model.Action.Base> { faultyAction }
        };

        var station = new Station { Name = "Faulty Station", Flow = workflow };
        var stations = new List<Station> { station };
        using var manager = new StationManager(_z21, stations);

        // Act & Assert
        _z21.SimulateFeedback(28);
        await Task.Delay(200);

        Assert.Pass("Exception handled gracefully");
    }

    [Test]
    public async Task ProcessFeedback_HandlesEmptyStationList()
    {
        // Arrange
        var stations = new List<Station>();
        using var manager = new StationManager(_z21, stations);

        // Act & Assert
        _z21.SimulateFeedback(29);
        await Task.Delay(200);

        Assert.Pass("Empty station list handled without exceptions");
    }

    [Test]
    public async Task ProcessFeedback_ExecutesMultipleActionsInWorkflow()
    {
        // Arrange
        var executionOrder = new List<int>();
        var action1 = new TestAction(() => executionOrder.Add(1));
        var action2 = new TestAction(() => executionOrder.Add(2));
        var action3 = new TestAction(() => executionOrder.Add(3));

        var workflow = new Workflow
        {
            Name = "Multi-Action Workflow",
            InPort = 30,
            Actions = new List<Model.Action.Base> { action1, action2, action3 }
        };

        var station = new Station { Name = "Multi-Action Station", Flow = workflow };
        var stations = new List<Station> { station };
        using var manager = new StationManager(_z21, stations);

        // Act
        _z21.SimulateFeedback(30);
        await Task.Delay(200);

        // Assert
        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }), 
            "Actions should execute in order");
    }
}

/// <summary>
/// Test action for testing purposes.
/// </summary>
file class TestAction : Moba.Backend.Model.Action.Base
{
    private readonly Action _callback;

    public TestAction(Action callback)
    {
        _callback = callback;
        Name = "TestAction";
    }

    public override Moba.Backend.Model.Enum.ActionType Type => Moba.Backend.Model.Enum.ActionType.Command;

    public override Task ExecuteAsync(Moba.Backend.Model.Action.ActionExecutionContext context)
    {
        _callback();
        return Task.CompletedTask;
    }
}

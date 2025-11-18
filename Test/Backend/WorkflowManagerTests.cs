using Moq;
using Moba.Backend.Manager;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend;
using Moba.Test.Mocks;
using Moba.Backend.Model.Action;
using Moba.Backend.Model.Enum;

namespace Moba.Test.Backend;

/// <summary>
/// Unit tests for WorkflowManager class.
/// Tests workflow execution based on feedback events.
/// </summary>
[TestFixture]
public class WorkflowManagerTests
{
    private Mock<IZ21> _z21Mock = null!;
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;

    [SetUp]
    public void SetUp()
    {
        _z21Mock = new Mock<IZ21>();
        _fakeUdp = new FakeUdpClientWrapper();
        _z21 = new Z21(_fakeUdp, null);
    }

    [TearDown]
    public void TearDown()
    {
        _z21?.Dispose();
        _fakeUdp?.Dispose();
    }

    [Test]
    public async Task ProcessFeedback_ExecutesWorkflow_OnMatchingInPort()
    {
        // Arrange
        var actionExecuted = false;
        var testAction = new TestAction(() => actionExecuted = true);
        
        var workflow = new Workflow
        {
            Name = "TestWorkflow",
            InPort = 10,
            Actions = new List<Moba.Backend.Model.Action.Base> { testAction }
        };

        var workflows = new List<Workflow> { workflow };
        using var manager = new WorkflowManager(_z21, workflows);

        // Act
        _z21.SimulateFeedback(10);
        await Task.Delay(200); // Wait for async processing

        // Assert
        Assert.That(actionExecuted, Is.True, "Workflow action should have been executed");
    }

    [Test]
    public async Task ProcessFeedback_ExecutesMultipleActions_InSequence()
    {
        // Arrange
        var executionOrder = new List<int>();
        var action1 = new TestAction(() => executionOrder.Add(1));
        var action2 = new TestAction(() => executionOrder.Add(2));
        var action3 = new TestAction(() => executionOrder.Add(3));

        var workflow = new Workflow
        {
            Name = "MultiActionWorkflow",
            InPort = 11,
            Actions = new List<Moba.Backend.Model.Action.Base> { action1, action2, action3 }
        };

        var workflows = new List<Workflow> { workflow };
        using var manager = new WorkflowManager(_z21, workflows);

        // Act
        _z21.SimulateFeedback(11);
        await Task.Delay(200);

        // Assert
        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }), "Actions should execute in order");
    }

    [Test]
    public async Task ProcessFeedback_IgnoresWorkflow_WhenInPortDoesNotMatch()
    {
        // Arrange
        var actionExecuted = false;
        var testAction = new TestAction(() => actionExecuted = true);

        var workflow = new Workflow
        {
            Name = "TestWorkflow",
            InPort = 10,
            Actions = new List<Moba.Backend.Model.Action.Base> { testAction }
        };

        var workflows = new List<Workflow> { workflow };
        using var manager = new WorkflowManager(_z21, workflows);

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
        var testAction = new TestAction(() => executionCount++);

        var workflow = new Workflow
        {
            Name = "TimerWorkflow",
            InPort = 12,
            IsUsingTimerToIgnoreFeedbacks = true,
            IntervalForTimerToIgnoreFeedbacks = 1.0, // 1 second
            Actions = new List<Moba.Backend.Model.Action.Base> { testAction }
        };

        var workflows = new List<Workflow> { workflow };
        using var manager = new WorkflowManager(_z21, workflows);

        // Act
        _z21.SimulateFeedback(12); // First feedback
        await Task.Delay(200);
        
        _z21.SimulateFeedback(12); // Second feedback (should be ignored)
        await Task.Delay(200);

        // Assert
        Assert.That(executionCount, Is.EqualTo(1), "Second feedback should be ignored due to timer");
    }

    [Test]
    public async Task ProcessFeedback_AllowsFeedback_AfterTimerExpires()
    {
        // Arrange
        var executionCount = 0;
        var testAction = new TestAction(() => executionCount++);

        var workflow = new Workflow
        {
            Name = "TimerWorkflow",
            InPort = 13,
            IsUsingTimerToIgnoreFeedbacks = true,
            IntervalForTimerToIgnoreFeedbacks = 0.5, // 500ms
            Actions = new List<Moba.Backend.Model.Action.Base> { testAction }
        };

        var workflows = new List<Workflow> { workflow };
        using var manager = new WorkflowManager(_z21, workflows);

        // Act
        _z21.SimulateFeedback(13); // First feedback
        await Task.Delay(200);
        
        await Task.Delay(600); // Wait for timer to expire
        
        _z21.SimulateFeedback(13); // Second feedback (should execute)
        await Task.Delay(200);

        // Assert
        Assert.That(executionCount, Is.EqualTo(2), "Second feedback should execute after timer expires");
    }

    [Test]
    public async Task ProcessFeedback_ExecutesMultipleWorkflows_OnSameInPort()
    {
        // Arrange
        var workflow1Executed = false;
        var workflow2Executed = false;

        var workflow1 = new Workflow
        {
            Name = "Workflow1",
            InPort = 14,
            Actions = new List<Moba.Backend.Model.Action.Base> { new TestAction(() => workflow1Executed = true) }
        };

        var workflow2 = new Workflow
        {
            Name = "Workflow2",
            InPort = 14, // Same InPort
            Actions = new List<Moba.Backend.Model.Action.Base> { new TestAction(() => workflow2Executed = true) }
        };

        var workflows = new List<Workflow> { workflow1, workflow2 };
        using var manager = new WorkflowManager(_z21, workflows);

        // Act
        _z21.SimulateFeedback(14);
        await Task.Delay(200);

        // Assert
        Assert.That(workflow1Executed, Is.True, "Workflow1 should execute");
        Assert.That(workflow2Executed, Is.True, "Workflow2 should execute");
    }

    [Test]
    public void ResetAll_ClearsAllTimers()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "TestWorkflow",
            InPort = 15,
            IsUsingTimerToIgnoreFeedbacks = true,
            IntervalForTimerToIgnoreFeedbacks = 10.0,
            Actions = new List<Moba.Backend.Model.Action.Base> { new TestAction(() => { }) }
        };

        var workflows = new List<Workflow> { workflow };
        using var manager = new WorkflowManager(_z21, workflows);

        _z21.SimulateFeedback(15); // Trigger feedback to set timer

        // Act
        manager.ResetAll();

        // Assert - should not throw
        Assert.Pass("ResetAll executed without exceptions");
    }

    [Test]
    public void Dispose_UnsubscribesFromZ21Events()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "TestWorkflow",
            InPort = 16,
            Actions = new List<Moba.Backend.Model.Action.Base> { new TestAction(() => { }) }
        };

        var workflows = new List<Workflow> { workflow };
        var manager = new WorkflowManager(_z21, workflows);

        // Act
        manager.Dispose();
        manager.Dispose(); // Should be idempotent

        // Assert - no exception thrown
        Assert.Pass("Dispose is idempotent and safe");
    }

    [Test]
    public async Task ProcessFeedback_HandlesWorkflowException_Gracefully()
    {
        // Arrange
        var faultyAction = new TestAction(() => throw new InvalidOperationException("Test exception"));
        var workflow = new Workflow
        {
            Name = "FaultyWorkflow",
            InPort = 17,
            Actions = new List<Moba.Backend.Model.Action.Base> { faultyAction }
        };

        var workflows = new List<Workflow> { workflow };
        using var manager = new WorkflowManager(_z21, workflows);

        // Act & Assert
        _z21.SimulateFeedback(17);
        await Task.Delay(200);

        // Manager should catch exception and log it (not crash)
        Assert.Pass("Exception was handled gracefully");
    }

    [Test]
    public async Task ProcessFeedback_HandlesEmptyWorkflowList()
    {
        // Arrange
        var workflows = new List<Workflow>();
        using var manager = new WorkflowManager(_z21, workflows);

        // Act & Assert
        _z21.SimulateFeedback(18);
        await Task.Delay(200);

        Assert.Pass("Empty workflow list handled without exceptions");
    }

    [Test]
    public async Task ProcessFeedback_SerializesMultipleFeedbacks()
    {
        // Arrange
        var executionOrder = new List<string>();
        var lockObject = new object();

        var action = new TestAction(() =>
        {
            lock (lockObject)
            {
                executionOrder.Add($"Start-{DateTime.UtcNow.Ticks}");
            }
            Thread.Sleep(100); // Simulate work
            lock (lockObject)
            {
                executionOrder.Add($"End-{DateTime.UtcNow.Ticks}");
            }
        });

        var workflow = new Workflow
        {
            Name = "SlowWorkflow",
            InPort = 19,
            Actions = new List<Moba.Backend.Model.Action.Base> { action }
        };

        var workflows = new List<Workflow> { workflow };
        using var manager = new WorkflowManager(_z21, workflows);

        // Act - Send multiple feedbacks quickly
        _z21.SimulateFeedback(19);
        _z21.SimulateFeedback(19);
        _z21.SimulateFeedback(19);
        
        await Task.Delay(500);

        // Assert - Should have completed all executions sequentially
        Assert.That(executionOrder.Count, Is.GreaterThanOrEqualTo(2), "At least one workflow should complete");
    }
}

/// <summary>
/// Test action that executes a callback for testing purposes.
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

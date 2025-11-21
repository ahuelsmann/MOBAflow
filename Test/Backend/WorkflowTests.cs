namespace Moba.Test.Backend;

/// <summary>
/// Unit tests for Workflow class.
/// Tests workflow action execution and validation.
/// </summary>
[TestFixture]
public class WorkflowTests
{
    [Test]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var workflow = new Workflow();

        // Assert
        Assert.That(workflow.Id, Is.Not.EqualTo(Guid.Empty), "Id should be generated");
        Assert.That(workflow.Name, Is.EqualTo("New Flow"));
        Assert.That(workflow.Actions, Is.Not.Null);
        Assert.That(workflow.Actions, Is.Empty);
        Assert.That(workflow.InPort, Is.EqualTo(0));
        Assert.That(workflow.IsUsingTimerToIgnoreFeedbacks, Is.False);
        Assert.That(workflow.IntervalForTimerToIgnoreFeedbacks, Is.EqualTo(0.0));
    }

    [Test]
    public void Constructor_GeneratesUniqueIds()
    {
        // Act
        var workflow1 = new Workflow();
        var workflow2 = new Workflow();

        // Assert
        Assert.That(workflow1.Id, Is.Not.EqualTo(workflow2.Id), "Each workflow should have unique ID");
    }

    [Test]
    public void Properties_AreSettable()
    {
        // Arrange
        var workflow = new Workflow();
        var testId = Guid.NewGuid();

        // Act
        workflow.Id = testId;
        workflow.Name = "Test Workflow";
        workflow.InPort = 42;
        workflow.IsUsingTimerToIgnoreFeedbacks = true;
        workflow.IntervalForTimerToIgnoreFeedbacks = 5.0;

        // Assert
        Assert.That(workflow.Id, Is.EqualTo(testId));
        Assert.That(workflow.Name, Is.EqualTo("Test Workflow"));
        Assert.That(workflow.InPort, Is.EqualTo(42));
        Assert.That(workflow.IsUsingTimerToIgnoreFeedbacks, Is.True);
        Assert.That(workflow.IntervalForTimerToIgnoreFeedbacks, Is.EqualTo(5.0));
    }

    [Test]
    public async Task StartAsync_ThrowsException_WhenNoActions()
    {
        // Arrange
        var workflow = new Workflow { Name = "Empty Workflow" };

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await workflow.StartAsync());
        
        Assert.That(ex?.Message, Does.Contain("must have at least one action"));
        Assert.That(ex?.Message, Does.Contain("Empty Workflow"));
    }

    [Test]
    public async Task StartAsync_ExecutesSingleAction()
    {
        // Arrange
        var actionExecuted = false;
        var testAction = new TestAction(() => actionExecuted = true);
        
        var workflow = new Workflow
        {
            Name = "Single Action Workflow",
            Actions = new List<Moba.Backend.Model.Action.Base> { testAction }
        };

        // Act
        await workflow.StartAsync();

        // Assert
        Assert.That(actionExecuted, Is.True, "Action should have been executed");
    }

    [Test]
    public async Task StartAsync_ExecutesMultipleActions_InSequence()
    {
        // Arrange
        var executionOrder = new List<int>();
        var action1 = new TestAction(() => executionOrder.Add(1));
        var action2 = new TestAction(() => executionOrder.Add(2));
        var action3 = new TestAction(() => executionOrder.Add(3));

        var workflow = new Workflow
        {
            Name = "Multi Action Workflow",
            Actions = new List<Moba.Backend.Model.Action.Base> { action1, action2, action3 }
        };

        // Act
        await workflow.StartAsync();

        // Assert
        Assert.That(executionOrder, Is.EqualTo([1, 2, 3]), 
            "Actions should execute in the order they were added");
    }

    [Test]
    public async Task StartAsync_PassesExecutionContext_ToActions()
    {
        // Arrange
        Moba.Backend.Model.Action.ActionExecutionContext? receivedContext = null;
        var testAction = new TestActionWithContext(ctx => receivedContext = ctx);

        var workflow = new Workflow
        {
            Actions = new List<Moba.Backend.Model.Action.Base> { testAction }
        };

        var context = new Moba.Backend.Model.Action.ActionExecutionContext();

        // Act
        await workflow.StartAsync(context);

        // Assert
        Assert.That(receivedContext, Is.Not.Null, "Context should be passed to action");
        Assert.That(receivedContext, Is.SameAs(context), "Context should be the same instance");
    }

    [Test]
    public async Task StartAsync_CreatesDefaultContext_WhenNoneProvided()
    {
        // Arrange
        Moba.Backend.Model.Action.ActionExecutionContext? receivedContext = null;
        var testAction = new TestActionWithContext(ctx => receivedContext = ctx);

        var workflow = new Workflow
        {
            Actions = new List<Moba.Backend.Model.Action.Base> { testAction }
        };

        // Act
        await workflow.StartAsync(); // No context provided

        // Assert
        Assert.That(receivedContext, Is.Not.Null, "Default context should be created");
    }

    [Test]
    public async Task StartAsync_ContinuesExecution_AfterActionException()
    {
        // Arrange
        var action1Executed = false;
        var action3Executed = false;

        var action1 = new TestAction(() => action1Executed = true);
        var action2 = new TestAction(() => throw new InvalidOperationException("Test exception"));
        var action3 = new TestAction(() => action3Executed = true);

        var workflow = new Workflow
        {
            Actions = new List<Moba.Backend.Model.Action.Base> { action1, action2, action3 }
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await workflow.StartAsync());
        
        Assert.That(action1Executed, Is.True, "Action 1 should execute");
        Assert.That(action3Executed, Is.False, "Action 3 should not execute after exception");
    }

    [Test]
    public async Task StartAsync_HandlesAsyncActions()
    {
        // Arrange
        var action1Complete = false;
        var action2Complete = false;

        var action1 = new AsyncTestAction(async () =>
        {
            await Task.Delay(50);
            action1Complete = true;
        });

        var action2 = new AsyncTestAction(async () =>
        {
            await Task.Delay(50);
            action2Complete = true;
        });

        var workflow = new Workflow
        {
            Actions = new List<Moba.Backend.Model.Action.Base> { action1, action2 }
        };

        // Act
        await workflow.StartAsync();

        // Assert
        Assert.That(action1Complete, Is.True);
        Assert.That(action2Complete, Is.True);
    }

    [Test]
    public async Task StartAsync_ExecutesActionsSequentially_NotParallel()
    {
        // Arrange
        var executionLog = new List<string>();
        var lockObject = new object();

        var action1 = new AsyncTestAction(async () =>
        {
            lock (lockObject) executionLog.Add("Action1-Start");
            await Task.Delay(100);
            lock (lockObject) executionLog.Add("Action1-End");
        });

        var action2 = new AsyncTestAction(async () =>
        {
            lock (lockObject) executionLog.Add("Action2-Start");
            await Task.Delay(100);
            lock (lockObject) executionLog.Add("Action2-End");
        });

        var workflow = new Workflow
        {
            Actions = new List<Moba.Backend.Model.Action.Base> { action1, action2 }
        };

        // Act
        await workflow.StartAsync();

        // Assert
        Assert.That(executionLog, Is.EqualTo(
        [
            "Action1-Start", 
            "Action1-End", 
            "Action2-Start", 
            "Action2-End" 
        ]), "Actions should execute sequentially");
    }

    [Test]
    public void Actions_CanBeModified()
    {
        // Arrange
        var workflow = new Workflow();
        var action = new TestAction(() => { });

        // Act
        workflow.Actions.Add(action);

        // Assert
        Assert.That(workflow.Actions.Count, Is.EqualTo(1));
        Assert.That(workflow.Actions[0], Is.SameAs(action));
    }

    [Test]
    public async Task StartAsync_WorksWithEmptyContext()
    {
        // Arrange
        var actionExecuted = false;
        var workflow = new Workflow
        {
            Actions = new List<Moba.Backend.Model.Action.Base> 
            { 
                new TestAction(() => actionExecuted = true) 
            }
        };

        var emptyContext = new Moba.Backend.Model.Action.ActionExecutionContext();

        // Act
        await workflow.StartAsync(emptyContext);

        // Assert
        Assert.That(actionExecuted, Is.True);
    }
}

/// <summary>
/// Simple test action for synchronous testing.
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

/// <summary>
/// Test action that receives execution context.
/// </summary>
file class TestActionWithContext : Moba.Backend.Model.Action.Base
{
    private readonly Action<Moba.Backend.Model.Action.ActionExecutionContext> _callback;

    public TestActionWithContext(Action<Moba.Backend.Model.Action.ActionExecutionContext> callback)
    {
        _callback = callback;
        Name = "TestActionWithContext";
    }

    public override Moba.Backend.Model.Enum.ActionType Type => Moba.Backend.Model.Enum.ActionType.Command;

    public override Task ExecuteAsync(Moba.Backend.Model.Action.ActionExecutionContext context)
    {
        _callback(context);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test action for async testing.
/// </summary>
file class AsyncTestAction : Moba.Backend.Model.Action.Base
{
    private readonly Func<Task> _callback;

    public AsyncTestAction(Func<Task> callback)
    {
        _callback = callback;
        Name = "AsyncTestAction";
    }

    public override Moba.Backend.Model.Enum.ActionType Type => Moba.Backend.Model.Enum.ActionType.Command;

    public override async Task ExecuteAsync(Moba.Backend.Model.Action.ActionExecutionContext context)
    {
        await _callback();
    }
}

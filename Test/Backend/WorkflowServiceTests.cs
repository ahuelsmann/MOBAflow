// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;

using Mocks;

using Moq;

/// <summary>
/// Integration tests for WorkflowService with IActionExecutor.
/// Tests end-to-end workflow execution.
/// </summary>
[TestFixture]
internal class WorkflowServiceTests
{
    private WorkflowService _workflowService = null!;
    private IActionExecutor _actionExecutor = null!;
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;
    private IEventBus _eventBus = null!;
    private ActionExecutionContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeUdp = new FakeUdpClientWrapper();
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
        _z21 = new Z21(_fakeUdp, _eventBus);
        _actionExecutor = ActionExecutor.CreateWithDefaultHandlers();
        _workflowService = new WorkflowService(_actionExecutor);

        _context = new ActionExecutionContext
        {
            Z21 = _z21
        };
    }

    [TearDown]
    public void TearDown()
    {
        _z21.Dispose();
        _fakeUdp.Dispose();
    }

    [Test]
    public Task ExecuteAsync_WithEmptyWorkflow_ShouldNotThrow()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Empty Workflow",
            Actions = []
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _workflowService.ExecuteAsync(workflow, _context));
        return Task.CompletedTask;
    }

    [Test]
    public async Task ExecuteAsync_WithMultipleActions_ShouldExecuteAll()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Multi-Action Workflow",
            Actions =
            [
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 1,
                    Name = "Command 1",
                    Type = ActionType.Command,
                    Command = new CommandActionPayload
                    {
                        BytesBase64 = Convert.ToBase64String(new byte[] { 0x01, 0x00, 0x00, 0x00 })
                    }
                },
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 2,
                    Name = "Command 2",
                    Type = ActionType.Command,
                    Command = new CommandActionPayload
                    {
                        BytesBase64 = Convert.ToBase64String(new byte[] { 0x02, 0x00, 0x00, 0x00 })
                    }
                }
            ]
        };

        // Act
        await _workflowService.ExecuteAsync(workflow, _context);

        // Assert
        Assert.That(_fakeUdp.SentPayloads, Has.Count.GreaterThanOrEqualTo(2),
            "At least 2 command packets should have been sent");
    }

    [Test]
    public void ExecuteAsync_WithNullWorkflow_ShouldThrow()
    {
        // Arrange
        Workflow? workflow = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
#pragma warning disable CS8604 // Possible null reference argument
            await _workflowService.ExecuteAsync(workflow, _context);
#pragma warning restore CS8604
        });
    }

    [Test]
    public async Task ExecuteAsync_Sequential_StopOnFirstActionFailure_StopsAfterFirstException()
    {
        var executorMock = new Mock<IActionExecutor>();
        executorMock
            .Setup(e => e.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("first action failed"));

        var service = new WorkflowService(executorMock.Object);
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Two-step",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Actions =
            [
                new WorkflowAction { Id = Guid.NewGuid(), Number = 1, Name = "A", Type = ActionType.Command },
                new WorkflowAction { Id = Guid.NewGuid(), Number = 2, Name = "B", Type = ActionType.Command }
            ]
        };

        try
        {
            await service.ExecuteAsync(workflow, _context, new WorkflowExecutionOptions { StopOnFirstActionFailure = true });
            Assert.Fail("Expected exception");
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Is.EqualTo("first action failed"));
        }

        executorMock.Verify(
            e => e.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void ExecuteAsync_WithNullContext_ShouldThrow()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Test Workflow",
            Actions = []
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            await _workflowService.ExecuteAsync(workflow, null);
#pragma warning restore CS8625
        });
    }

    [Test]
    public async Task ExecuteAsync_Sequential_ExecutesByNumberAndContinuesAfterFailureByDefault()
    {
        // Arrange
        var executedNumbers = new List<uint>();
        var executorMock = new Mock<IActionExecutor>();
        executorMock
            .Setup(executor => executor.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<WorkflowAction, ActionExecutionContext, CancellationToken>((action, _, _) =>
            {
                executedNumbers.Add(action.Number);
                return action.Number == 1
                    ? Task.FromException(new InvalidOperationException("Expected failure"))
                    : Task.CompletedTask;
            });
        var service = new WorkflowService(executorMock.Object);
        var errors = new List<ActionExecutionErrorEventArgs>();
        service.ActionExecutionError += (_, args) => errors.Add(args);
        var workflow = new Workflow
        {
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Actions =
            [
                new WorkflowAction { Number = 2, Name = "Second", Type = ActionType.Command },
                new WorkflowAction { Number = 1, Name = "First", Type = ActionType.Command }
            ]
        };

        // Act
        await service.ExecuteAsync(workflow, _context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(executedNumbers, Is.EqualTo(new uint[] { 1, 2 }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Action.Number, Is.EqualTo(1));
            Assert.That(errors[0].Exception.Message, Is.EqualTo("Expected failure"));
        });
    }

    [Test]
    public async Task ExecuteAsync_Parallel_IgnoresStopOnFirstFailureAndAttemptsEveryAction()
    {
        // Arrange
        var executedNumbers = new System.Collections.Concurrent.ConcurrentBag<uint>();
        var executorMock = new Mock<IActionExecutor>();
        executorMock
            .Setup(executor => executor.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<WorkflowAction, ActionExecutionContext, CancellationToken>((action, _, _) =>
            {
                executedNumbers.Add(action.Number);
                return action.Number == 1
                    ? Task.FromException(new InvalidOperationException("Expected failure"))
                    : Task.CompletedTask;
            });
        var service = new WorkflowService(executorMock.Object);
        var errors = new List<ActionExecutionErrorEventArgs>();
        service.ActionExecutionError += (_, args) => errors.Add(args);
        var workflow = new Workflow
        {
            ExecutionMode = WorkflowExecutionMode.Parallel,
            Actions =
            [
                new WorkflowAction { Number = 1, Name = "First", Type = ActionType.Command },
                new WorkflowAction { Number = 2, Name = "Second", Type = ActionType.Command }
            ]
        };

        // Act
        await service.ExecuteAsync(
            workflow,
            _context,
            new WorkflowExecutionOptions { StopOnFirstActionFailure = true });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(executedNumbers, Is.EquivalentTo(new uint[] { 1, 2 }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Action.Number, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExecuteAsync_CancelledDuringDelay_UsesInjectedTimeProviderAndStopsBeforeNextAction()
    {
        // Arrange
        var timeProvider = new NeverCompletingTimeProvider();
        var executorMock = new Mock<IActionExecutor>();
        executorMock
            .Setup(executor => executor.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new WorkflowService(executorMock.Object, timeProvider);
        var workflow = new Workflow
        {
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Actions =
            [
                new WorkflowAction { Number = 1, DelayAfterMs = 1000, Type = ActionType.Command },
                new WorkflowAction { Number = 2, Type = ActionType.Command }
            ]
        };
        using var cancellation = new CancellationTokenSource();

        // Act
        var execution = service.ExecuteAsync(workflow, _context, default, cancellation.Token);
        cancellation.Cancel();

        // Assert
        Assert.CatchAsync<OperationCanceledException>(async () => await execution);
        Assert.Multiple(() =>
        {
            Assert.That(timeProvider.CreatedTimers, Is.EqualTo(1));
            executorMock.Verify(executor => executor.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                cancellation.Token), Times.Once);
        });
    }

    private sealed class NeverCompletingTimeProvider : TimeProvider
    {
        public int CreatedTimers { get; private set; }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            _ = callback;
            _ = state;
            _ = dueTime;
            _ = period;
            CreatedTimers++;
            return new NeverCompletingTimer();
        }
    }

    private sealed class NeverCompletingTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

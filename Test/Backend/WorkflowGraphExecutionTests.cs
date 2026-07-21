// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;

using Moq;

[TestFixture]
internal sealed class WorkflowGraphExecutionTests
{
    [Test]
    public async Task ExecuteAsync_DryRun_PlansEffectsWithoutCallingLiveHandlerOrWaiting()
    {
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);
        var traceStore = new WorkflowTraceStore();
        var service = CreateService(executor.Object, traceStore);
        var actionId = Guid.NewGuid();
        var delayId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var workflow = new Workflow
        {
            EntryStepId = actionId,
            Steps =
            [
                CreateCommandStep(actionId, delayId, "Command"),
                new WorkflowDelayStep { Id = delayId, DelayMs = 60_000, NextStepId = terminalId },
                new WorkflowTerminateStep { Id = terminalId }
            ]
        };
        var project = new Project { Workflows = [workflow] };

        var result = await service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = CreateContext(project),
            Mode = WorkflowRunMode.DryRun
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(result.PlannedEffects, Has.Count.EqualTo(1));
            Assert.That(result.PlannedEffects[0].Category, Is.EqualTo(WorkflowEffectCategory.CommandStation));
            Assert.That(traceStore.GetEntries().Count(entry => entry.Kind == WorkflowLifecycleKind.PlannedEffect), Is.EqualTo(1));
            Assert.That(traceStore.GetEntries().Single(entry =>
                entry.Kind == WorkflowLifecycleKind.StepCompleted && entry.StepId == delayId).Detail,
                Is.EqualTo("Planned delay: 60000 ms."));
        });
        executor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ExecuteAsync_Condition_ExecutesOnlySelectedBranch()
    {
        var executedNames = new List<string>();
        var executor = new Mock<IActionExecutor>();
        executor
            .Setup(value => value.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<WorkflowAction, ActionExecutionContext, CancellationToken>((action, _, _) =>
            {
                executedNames.Add(action.Name);
                return Task.CompletedTask;
            });
        var service = CreateService(executor.Object);
        var conditionId = Guid.NewGuid();
        var trueId = Guid.NewGuid();
        var falseId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var station = new Station { Name = "Minden" };
        var workflow = new Workflow
        {
            EntryStepId = conditionId,
            Steps =
            [
                new WorkflowConditionStep
                {
                    Id = conditionId,
                    Condition = new CurrentStationWorkflowCondition { StationId = station.Id },
                    TrueStepId = trueId,
                    FalseStepId = falseId
                },
                CreateCommandStep(trueId, terminalId, "True"),
                CreateCommandStep(falseId, terminalId, "False"),
                new WorkflowTerminateStep { Id = terminalId }
            ]
        };
        var project = new Project { Workflows = [workflow] };
        var context = CreateContext(project);
        context.CurrentStation = station;

        var result = await service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = context,
            Mode = WorkflowRunMode.Live
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(executedNames, Is.EqualTo(new[] { "True" }));
        });
    }

    [Test]
    public async Task ExecuteAsync_RetryPolicy_RetriesBoundedAttemptsThenSucceeds()
    {
        var attempts = 0;
        var executor = new Mock<IActionExecutor>();
        executor
            .Setup(value => value.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => ++attempts < 3
                ? Task.FromException(new InvalidOperationException("Expected"))
                : Task.CompletedTask);
        var traceStore = new WorkflowTraceStore();
        var service = CreateService(executor.Object, traceStore);
        var workflow = CreateLinearWorkflow();
        workflow.DefaultErrorPolicy = new WorkflowErrorPolicy
        {
            Retry = new WorkflowRetryPolicy { AdditionalAttempts = 2, DelayMs = 0 }
        };
        var project = new Project { Workflows = [workflow] };

        var result = await service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = CreateContext(project),
            Mode = WorkflowRunMode.Live
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(attempts, Is.EqualTo(3));
            Assert.That(traceStore.GetEntries().Count(entry => entry.Kind == WorkflowLifecycleKind.RetryScheduled), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ExecuteAsync_StepFailurePolicy_UsesDeclaredFailureBranch()
    {
        var executedNames = new List<string>();
        var executor = new Mock<IActionExecutor>();
        executor
            .Setup(value => value.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<WorkflowAction, ActionExecutionContext, CancellationToken>((action, _, _) =>
            {
                executedNames.Add(action.Name);
                return action.Name == "Failing"
                    ? Task.FromException(new InvalidOperationException("Expected"))
                    : Task.CompletedTask;
            });
        var service = CreateService(executor.Object);
        var failingId = Guid.NewGuid();
        var recoveryId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var failing = CreateCommandStep(failingId, terminalId, "Failing");
        failing.ErrorPolicy = new WorkflowErrorPolicy
        {
            Behavior = WorkflowFailureBehavior.FailureBranch,
            FailureStepId = recoveryId
        };
        var workflow = new Workflow
        {
            EntryStepId = failingId,
            Steps =
            [
                failing,
                CreateCommandStep(recoveryId, terminalId, "Recovery"),
                new WorkflowTerminateStep { Id = terminalId }
            ]
        };
        var project = new Project { Workflows = [workflow] };

        var result = await service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = CreateContext(project),
            Mode = WorkflowRunMode.Live
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(executedNames, Is.EqualTo(new[] { "Failing", "Recovery" }));
        });
    }

    [Test]
    public async Task ExecuteAsync_ParallelDryRun_ReducesPlannedEffectsInPersistedBranchOrder()
    {
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);
        var service = CreateService(executor.Object);
        var parallelId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var joinId = Guid.NewGuid();
        var firstDisplay = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var secondDisplay = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var workflow = new Workflow
        {
            EntryStepId = parallelId,
            Steps =
            [
                new WorkflowParallelStep
                {
                    Id = parallelId,
                    JoinStepId = joinId,
                    Branches =
                    [
                        new WorkflowParallelBranch { Name = "First", EntryStepId = firstId },
                        new WorkflowParallelBranch { Name = "Second", EntryStepId = secondId }
                    ]
                },
                CreateDisplayStep(firstId, joinId, firstDisplay),
                CreateDisplayStep(secondId, joinId, secondDisplay),
                new WorkflowTerminateStep { Id = joinId }
            ]
        };
        var project = new Project { Workflows = [workflow] };

        var result = await service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = CreateContext(project),
            Mode = WorkflowRunMode.DryRun
        });

        Assert.That(
            result.PlannedEffects.Select(effect => effect.Resources[0].Key),
            Is.EqualTo(new[] { $"display:{firstDisplay:D}", $"display:{secondDisplay:D}" }));
        executor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ExecuteAsync_NestedWorkflow_PublishesParentChildCorrelation()
    {
        var executor = new Mock<IActionExecutor>();
        executor
            .Setup(value => value.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var traceStore = new WorkflowTraceStore();
        var service = CreateService(executor.Object, traceStore);
        var child = CreateLinearWorkflow();
        var nestedId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var parent = new Workflow
        {
            EntryStepId = nestedId,
            Steps =
            [
                new WorkflowNestedStep { Id = nestedId, WorkflowId = child.Id, NextStepId = terminalId },
                new WorkflowTerminateStep { Id = terminalId }
            ]
        };
        var project = new Project { Workflows = [parent, child] };

        var result = await service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = parent,
            Context = CreateContext(project),
            Mode = WorkflowRunMode.Live
        });

        var entries = traceStore.GetEntries();
        var childStarted = entries.Single(entry =>
            entry.Kind == WorkflowLifecycleKind.WorkflowStarted && entry.WorkflowId == child.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(childStarted.ParentExecutionId, Is.EqualTo(result.ExecutionId));
            Assert.That(childStarted.ExecutionId, Is.Not.EqualTo(result.ExecutionId));
            Assert.That(entries.Select(entry => entry.Sequence), Is.EqualTo(Enumerable.Range(1, entries.Count).Select(value => (long)value)));
        });
    }

    [Test]
    public async Task ExecuteAsync_Cancellation_ReturnsCancelledAndPublishesOneRootTerminalEvent()
    {
        var executor = new Mock<IActionExecutor>();
        executor
            .Setup(value => value.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<WorkflowAction, ActionExecutionContext, CancellationToken>((_, _, token) =>
                Task.Delay(Timeout.InfiniteTimeSpan, token));
        var traceStore = new WorkflowTraceStore();
        var service = CreateService(executor.Object, traceStore);
        var workflow = CreateLinearWorkflow();
        var project = new Project { Workflows = [workflow] };
        using var cancellation = new CancellationTokenSource();

        var execution = service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = CreateContext(project),
            Mode = WorkflowRunMode.Live
        }, cancellation.Token);
        cancellation.Cancel();
        var result = await execution;

        var rootTerminalEvents = traceStore.GetEntries().Where(entry =>
            entry.ExecutionId == result.ExecutionId &&
            entry.Kind is WorkflowLifecycleKind.WorkflowCompleted or
                WorkflowLifecycleKind.WorkflowCancelled or
                WorkflowLifecycleKind.WorkflowFailed);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Cancelled));
            Assert.That(rootTerminalEvents.Count(), Is.EqualTo(1));
            Assert.That(rootTerminalEvents.Single().Kind, Is.EqualTo(WorkflowLifecycleKind.WorkflowCancelled));
        });
    }

    [Test]
    public async Task ExecuteAsync_InvalidWorkflow_ReturnsNotStartedWithoutCallingHandler()
    {
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);
        var service = CreateService(executor.Object);
        var workflow = new Workflow { EntryStepId = Guid.NewGuid(), Steps = [] };
        var project = new Project { Workflows = [workflow] };

        var result = await service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = CreateContext(project),
            Mode = WorkflowRunMode.Live
        });

        Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.NotStarted));
        Assert.That(result.ValidationIssues, Is.Not.Empty);
        executor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ExecuteAsync_WorkflowOutsideProjectSnapshot_ReturnsNotStarted()
    {
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);
        var service = CreateService(executor.Object);
        var detachedWorkflow = CreateLinearWorkflow();

        var result = await service.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = new Project(),
            Workflow = detachedWorkflow,
            Context = CreateContext(new Project()),
            Mode = WorkflowRunMode.Live
        });

        Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.NotStarted));
        Assert.That(result.ValidationIssues.Any(issue => issue.Code == WorkflowValidationCodes.MissingWorkflow), Is.True);
        executor.VerifyNoOtherCalls();
    }

    private static WorkflowService CreateService(
        IActionExecutor executor,
        IWorkflowTraceStore? traceStore = null) =>
        new(
            executor,
            new WorkflowValidator(),
            new WorkflowEffectPlanner(),
            new WorkflowConditionEvaluator(),
            new EventBus(NullLogger<EventBus>.Instance),
            traceStore ?? new WorkflowTraceStore(),
            TimeProvider.System);

    private static ActionExecutionContext CreateContext(Project project) =>
        new()
        {
            Z21 = Mock.Of<IZ21>(),
            CurrentProject = project
        };

    private static Workflow CreateLinearWorkflow()
    {
        var actionId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        return new Workflow
        {
            EntryStepId = actionId,
            Steps =
            [
                CreateCommandStep(actionId, terminalId, "Command"),
                new WorkflowTerminateStep { Id = terminalId }
            ]
        };
    }

    private static WorkflowActionStep CreateCommandStep(Guid id, Guid nextStepId, string name) =>
        new()
        {
            Id = id,
            NextStepId = nextStepId,
            Action = new WorkflowAction
            {
                Name = name,
                Type = ActionType.Command,
                Command = new CommandActionPayload { BytesBase64 = "AQID" }
            }
        };

    private static WorkflowActionStep CreateDisplayStep(Guid id, Guid nextStepId, Guid displayId) =>
        new()
        {
            Id = id,
            NextStepId = nextStepId,
            Action = new WorkflowAction
            {
                Type = ActionType.TrainDestinationDisplay,
                TrainDestinationDisplay = new TrainDestinationDisplayActionPayload { DisplayDeviceId = displayId }
            }
        };
}

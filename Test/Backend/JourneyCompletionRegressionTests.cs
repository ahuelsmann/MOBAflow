// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.Domain.Enum;
using Moq;

[TestFixture]
internal sealed class JourneyCompletionRegressionTests
{
    [Test]
    public async Task AutomaticStartRejectsChangedTargetWhileAnotherJourneyRuns()
    {
        using var fixture = new CompletionFixture();
        var target = fixture.AddTarget();
        var other = new Journey { EventPlan = new JourneyEventPlan() };
        fixture.Project.Journeys.Add(other);
        fixture.Execute = (request, _) =>
        {
            CompleteStop(request);
            return Task.FromResult(Success(request));
        };
        await fixture.StartAsync().ConfigureAwait(false);
        await fixture.Runtime.StartJourneyAsync(other.Id).ConfigureAwait(false);
        var otherRun = fixture.Runtime.Current.JourneyStates[other.Id].JourneyRunId;
        target.EventPlan!.Events[0].Count = 5;
        await fixture.Runtime.ActivateProjectAsync(fixture.Project).ConfigureAwait(false);

        Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Runtime.StartJourneyAsync(target.Id));
        fixture.EmitFeedback();
        Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Queue.LastExecution.WaitAsync(TimeSpan.FromSeconds(5)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Runtime.Current.JourneyStates[target.Id].IsActive, Is.False);
            Assert.That(fixture.Runtime.Current.JourneyStates[fixture.Journey.Id].IsActive, Is.False);
            Assert.That(fixture.Runtime.Current.JourneyStates[other.Id].JourneyRunId, Is.EqualTo(otherRun));
            Assert.That(fixture.Runtime.Current.JourneyStates[other.Id].IsActive, Is.True);
        }
    }

    [Test]
    public async Task AutomaticStartUsesPendingDefinitionAfterLastJourneyFinishes()
    {
        using var fixture = new CompletionFixture();
        var target = fixture.AddTarget();
        var targetExecutions = 0;
        fixture.Execute = (request, _) =>
        {
            if (request.Context.CurrentJourney?.Id == fixture.Journey.Id)
                CompleteStop(request);
            else
                targetExecutions++;
            return Task.FromResult(Success(request));
        };
        await fixture.StartAsync().ConfigureAwait(false);
        target.EventPlan!.Events[0].Count = 5;
        await fixture.Runtime.ActivateProjectAsync(fixture.Project).ConfigureAwait(false);
        await fixture.RaiseAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Runtime.Current.JourneyStates[target.Id].IsActive, Is.True);
            Assert.That(fixture.Runtime.Current.JourneyStates[target.Id].StartCounterValues[1], Is.EqualTo(1UL));
        }
        for (var count = 1; count <= 4; count++)
            await fixture.RaiseAsync().ConfigureAwait(false);
        Assert.That(targetExecutions, Is.Zero, "The old threshold of two must not execute.");
        await fixture.RaiseAsync().ConfigureAwait(false);
        Assert.That(targetExecutions, Is.EqualTo(1));
    }

    [TestCase(BehaviorOnLastStop.None)]
    [TestCase(BehaviorOnLastStop.BeginAgainFromFistStop)]
    public async Task NaturalCompletionLetsRemainingActionsFinishBeforeStoppingOrRestarting(BehaviorOnLastStop behavior)
    {
        using var fixture = new CompletionFixture();
        fixture.Journey.BehaviorOnLastStop = behavior;
        var remainingActionRan = false;
        fixture.Execute = async (request, cancellationToken) =>
        {
            CompleteStop(request);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            Assert.That(fixture.Runtime.Current.CanResetInPortCounters, Is.False,
                "The current execution still owns its journey and counters.");
            remainingActionRan = true;
            return Success(request);
        };
        await fixture.StartAsync().ConfigureAwait(false);
        var originalRun = fixture.Runtime.Current.JourneyStates[fixture.Journey.Id].JourneyRunId;
        await fixture.RaiseAsync().ConfigureAwait(false);

        var state = fixture.Runtime.Current.JourneyStates[fixture.Journey.Id];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(remainingActionRan, Is.True);
            Assert.That((await fixture.Queue.LastExecution.ConfigureAwait(false)).Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(state.IsActive, Is.EqualTo(behavior == BehaviorOnLastStop.BeginAgainFromFistStop));
            Assert.That(state.JourneyRunId == originalRun, Is.EqualTo(behavior == BehaviorOnLastStop.None));
            Assert.That(fixture.Runtime.Current.InPortCounters.Single(counter => counter.InPort == 1).Count, Is.EqualTo(1UL));
        }
    }

    [Test]
    public async Task NaturalCompletionCancelsQueuedEventsAfterCurrentWorkflowFinishes()
    {
        using var fixture = new CompletionFixture();
        fixture.Journey.EventPlan!.Events.Add(new JourneyEvent { InPort = 1, Count = 2, WorkflowId = fixture.Workflow.Id });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;
        fixture.Execute = async (request, cancellationToken) =>
        {
            executions++;
            started.TrySetResult();
            await release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            CompleteStop(request);
            cancellationToken.ThrowIfCancellationRequested();
            return Success(request);
        };
        await fixture.StartAsync().ConfigureAwait(false);
        fixture.EmitFeedback();
        var first = fixture.Queue.LastExecution;
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        fixture.EmitFeedback();
        var queued = fixture.Queue.LastExecution;
        release.TrySetResult();
        await Task.WhenAll(first, queued).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That((await first.ConfigureAwait(false)).Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That((await queued.ConfigureAwait(false)).Status, Is.EqualTo(WorkflowExecutionStatus.Cancelled));
            Assert.That(executions, Is.EqualTo(1));
            Assert.That(fixture.Runtime.Current.CanResetInPortCounters, Is.True);
        }
    }

    [Test]
    public async Task ExplicitStopStillCancelsAWorkflowAfterItsFinalStopAction()
    {
        using var fixture = new CompletionFixture();
        var reachedEnd = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Execute = async (request, cancellationToken) =>
        {
            CompleteStop(request);
            reachedEnd.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return Success(request);
        };
        await fixture.StartAsync().ConfigureAwait(false);
        fixture.EmitFeedback();
        await reachedEnd.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await fixture.Runtime.StopJourneyAsync(fixture.Journey.Id).ConfigureAwait(false);
        var result = await fixture.Queue.LastExecution.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Cancelled));
            Assert.That(fixture.Runtime.Current.JourneyStates[fixture.Journey.Id].IsActive, Is.False);
        }
    }

    private static void CompleteStop(WorkflowExecutionRequest request) =>
        request.Context.ApplyJourneyStopTransition!(new JourneyStopTransition { Mode = JourneyStopTransitionMode.Next });

    private static WorkflowExecutionResult Success(WorkflowExecutionRequest request) => new()
    {
        ExecutionId = Guid.NewGuid(), WorkflowId = request.Workflow.Id,
        SourceCorrelationId = request.SourceCorrelationId, Status = WorkflowExecutionStatus.Succeeded
    };

    private sealed class CompletionFixture : IDisposable
    {
        public Mock<IZ21> Z21 { get; } = new();
        public Workflow Workflow { get; } = new();
        public Journey Journey { get; }
        public Project Project { get; }
        public TrackingCoordinator Queue { get; }
        public MobaRuntimeService Runtime { get; }
        public Func<WorkflowExecutionRequest, CancellationToken, Task<WorkflowExecutionResult>> Execute { get; set; } =
            (request, _) => Task.FromResult(Success(request));

        public CompletionFixture()
        {
            var workflows = new Mock<IWorkflowService>();
            workflows.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns((WorkflowExecutionRequest request, CancellationToken cancellationToken) => Execute(request, cancellationToken));
            Queue = new TrackingCoordinator(workflows.Object);
            Journey = new Journey
            {
                Stations = [new Station { Name = "Final stop" }],
                BehaviorOnLastStop = BehaviorOnLastStop.None,
                EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { InPort = 1, Count = 1, WorkflowId = Workflow.Id }] }
            };
            Project = new Project { Journeys = [Journey], Workflows = [Workflow] };
            Runtime = new MobaRuntimeService(Z21.Object, workflows.Object,
                new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = Z21.Object }),
                new AppSettings { Counter = new CounterSettings { CountOfFeedbackPoints = 3, UseTimerFilter = false } },
                NullLogger<MobaRuntimeService>.Instance,
                journeyManagerFactory: new JourneyManagerFactory(Z21.Object, workflows.Object,
                    new JourneyManagerDependencies { ExecutionCoordinator = Queue }, logger: null));
        }

        public Journey AddTarget()
        {
            var target = new Journey
            {
                EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { InPort = 1, Count = 2, WorkflowId = Workflow.Id }] }
            };
            Project.Journeys.Add(target);
            Journey.BehaviorOnLastStop = BehaviorOnLastStop.GotoJourney;
            Journey.NextJourneyId = target.Id;
            return target;
        }

        public async Task StartAsync()
        {
            await Runtime.ActivateProjectAsync(Project).ConfigureAwait(false);
            await Runtime.StartJourneyAsync(Journey.Id).ConfigureAwait(false);
        }

        public void EmitFeedback() => InPortCounterServiceTests.Raise(Z21, 1);

        public async Task RaiseAsync()
        {
            EmitFeedback();
            await Queue.LastExecution.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Runtime.Dispose();
            Queue.Dispose();
        }
    }

    private sealed class TrackingCoordinator(IWorkflowService workflows) : IWorkflowExecutionCoordinator
    {
        private readonly WorkflowExecutionCoordinator _inner = new(workflows, TimeProvider.System);
        public Task<WorkflowExecutionResult> LastExecution { get; private set; } = Task.FromResult(new WorkflowExecutionResult
        {
            ExecutionId = Guid.Empty, WorkflowId = Guid.Empty,
            SourceCorrelationId = Guid.Empty, Status = WorkflowExecutionStatus.Succeeded
        });

        public Task<WorkflowExecutionResult> EnqueueAsync(QueuedWorkflowExecution execution, CancellationToken cancellationToken = default)
        {
            LastExecution = _inner.EnqueueAsync(execution, cancellationToken);
            return LastExecution;
        }

        public void CancelOwner(Guid ownerId) => _inner.CancelOwner(ownerId);
        public void CancelPending() => _inner.CancelPending();
        public void Dispose() => _inner.Dispose();
    }
}

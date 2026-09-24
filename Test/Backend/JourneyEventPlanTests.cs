// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;
using Moq;
using System.Collections.Concurrent;

[TestFixture]
public sealed class JourneyEventPlanTests
{
    private static readonly string[] ExpectedTransitionOrder = ["transition:FeedbackAccepted:1", "callback"];

    [Test]
    public async Task InactiveJourneyIgnoresMatchingCounts()
    {
        using var fixture = new EventPlanFixture(isActive: false);
        await fixture.RaiseAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.IsActive, Is.False);
            Assert.That(fixture.Requests, Is.Empty);
            Assert.That(fixture.Counters.GetSnapshot().Single().Count, Is.EqualTo(1UL));
        });
    }

    [Test]
    public async Task ActiveJourneyRunsEachEventOnceAtItsSessionCount()
    {
        using var fixture = new EventPlanFixture();
        fixture.Journey.EventPlan.Events.Single().Count = 2;

        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests, Is.Empty);
        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Requests, Has.Count.EqualTo(1));
            Assert.That(fixture.Requests.Single().Context.FeedbackInPort, Is.EqualTo(1u));
        });
    }

    [Test]
    public async Task CounterResetStartsANewSessionSoEventsRunAgain()
    {
        using var fixture = new EventPlanFixture();
        await fixture.RaiseAsync(1);
        fixture.Counters.ResetAll();
        await fixture.RaiseAsync(1);

        Assert.That(fixture.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ResetDuringFeedbackNotificationDoesNotQueueTheOldActivation()
    {
        using var fixture = new EventPlanFixture();
        fixture.Manager.FeedbackReceived += (_, _) => fixture.Counters.ResetAll();

        await fixture.RaiseAsync(1).ConfigureAwait(false);

        Assert.That(fixture.Requests, Is.Empty);
    }

    [Test]
    public async Task ResetOnAnotherThreadDuringFeedbackNotificationDoesNotQueueTheOldActivation()
    {
        using var fixture = new EventPlanFixture();
        using var releaseNotification = new ManualResetEventSlim();
        var notificationEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Manager.FeedbackReceived += (_, _) =>
        {
            notificationEntered.TrySetResult();
            if (!releaseNotification.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("Feedback notification was not released.");
        };
        var processing = Task.Run(() => fixture.RaiseAsync(1));
        try
        {
            await notificationEntered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            fixture.Counters.ResetAll();
        }
        finally
        {
            releaseNotification.Set();
            await processing.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        Assert.That(fixture.Requests, Is.Empty);
    }

    [Test]
    public async Task EventsAreIndependentOfDisplayOrderAndOtherInPorts()
    {
        using var fixture = new EventPlanFixture();
        var later = new JourneyEvent { InPort = 1, Count = 3, WorkflowId = fixture.Workflow.Id };
        var otherPort = new JourneyEvent { InPort = 2, Count = 1, WorkflowId = fixture.Workflow.Id };
        var earlier = new JourneyEvent { InPort = 1, Count = 1, WorkflowId = fixture.Workflow.Id };
        fixture.Journey.EventPlan.Events = [later, otherPort, earlier];
        await fixture.RaiseAsync(2);
        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests.Select(request => request.Context.FeedbackInPort), Is.EqualTo(new uint?[] { 2, 1 }));
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests.Select(request => request.Context.FeedbackInPort), Is.EqualTo(new uint?[] { 2, 1, 1 }));
    }

    [Test]
    public async Task AllActiveJourneysAreEvaluatedForTheSameCount()
    {
        using var fixture = new EventPlanFixture();
        var secondJourney = new Journey
        {
            IsActive = true,
            EventPlan = new JourneyEventPlan
            {
                Events = [new JourneyEvent { InPort = 1, Count = 1, WorkflowId = fixture.Workflow.Id }]
            }
        };
        var inactiveJourney = new Journey
        {
            EventPlan = new JourneyEventPlan
            {
                Events = [new JourneyEvent { InPort = 1, Count = 1, WorkflowId = fixture.Workflow.Id }]
            }
        };
        fixture.Project.Journeys.AddRange([secondJourney, inactiveJourney]);
        using var manager = fixture.CreateManager();

        await fixture.RaiseAsync(1, manager);

        Assert.That(fixture.Requests.Select(request => request.Context.CurrentJourney!.Id),
            Is.EquivalentTo(new[] { fixture.Journey.Id, secondJourney.Id }));
    }

    [Test]
    public async Task NextStopActionsStayAtLastStopWithoutDeactivating()
    {
        using var fixture = new EventPlanFixture();
        var first = new Station { Name = "A" };
        var second = new Station { Name = "B" };
        var third = new Station { Name = "C" };
        fixture.Journey.Stations = [first, second, third];
        using var manager = fixture.CreateManager();
        var stations = new List<Guid>();
        manager.StationChanged += (_, args) => stations.Add(args.Station.Id);
        fixture.SetupNextStopActions(times: 3);

        await fixture.RaiseAsync(1, manager);

        var state = manager.GetState(fixture.Journey.Id)!;
        Assert.Multiple(() =>
        {
            Assert.That(stations, Is.EqualTo(new[] { second.Id, third.Id }));
            Assert.That(state.CurrentStationId, Is.EqualTo(third.Id));
            Assert.That(state.IsActive, Is.True);
        });
    }

    [Test]
    public async Task LaterRulesStillRunAtTheLastStopAndUnmatchedCountsDoNothing()
    {
        using var fixture = new EventPlanFixture();
        var terminal = new Station { Name = "Terminal" };
        fixture.Journey.Stations = [new Station { Name = "First" }, terminal];
        fixture.Journey.EventPlan.Events.Add(new JourneyEvent { Count = 3, WorkflowId = fixture.Workflow.Id });
        fixture.SetupNextStopActions(times: 2);

        for (var count = 1; count <= 5; count++)
        {
            await fixture.RaiseAsync(1).ConfigureAwait(false);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Requests, Has.Count.EqualTo(2));
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.CurrentStationId, Is.EqualTo(terminal.Id));
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.IsActive, Is.True);
            Assert.That(fixture.Counters.GetSnapshot().Single().Count, Is.EqualTo(5UL));
        }
    }

    [Test]
    public async Task ExplicitStopSelectionReturnsToFirstStopWithoutResettingCounts()
    {
        using var fixture = new EventPlanFixture();
        var first = new Station { Name = "First" };
        fixture.Journey.Stations = [first, new Station { Name = "Terminal" }];
        fixture.SetupNextStopActions(times: 2);
        await fixture.RaiseAsync(1).ConfigureAwait(false);
        fixture.Journey.EventPlan.Events.Add(new JourneyEvent { Count = 2, WorkflowId = fixture.Workflow.Id });
        fixture.WorkflowService.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowExecutionRequest request, CancellationToken _) =>
            {
                request.Context.ApplyJourneyStopTransition!(new JourneyStopTransition
                {
                    Mode = JourneyStopTransitionMode.SpecificStation, StationId = first.Id
                });
                return Success(request);
            });

        await fixture.RaiseAsync(1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.CurrentStationId, Is.EqualTo(first.Id));
            Assert.That(fixture.Counters.GetSnapshot().Single().Count, Is.EqualTo(2UL));
        }
    }

    [Test]
    public async Task NextStopWithoutStationsLeavesTheJourneyActive()
    {
        using var fixture = new EventPlanFixture();
        fixture.SetupNextStopActions(times: 1);

        await fixture.RaiseAsync(1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.CurrentStationId, Is.Null);
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.IsActive, Is.True);
            Assert.That(fixture.Requests, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task QueuedWorkflowSeesTheStopSelectedByThePreviousWorkflow()
    {
        using var fixture = new EventPlanFixture();
        var firstStation = new Station { Name = "A" };
        var nextStation = new Station { Name = "B" };
        fixture.Journey.Stations = [firstStation, nextStation];
        fixture.Journey.EventPlan.Events.Add(new JourneyEvent { InPort = 2, Count = 1, WorkflowId = fixture.Workflow.Id });
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.WorkflowService.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
            {
                fixture.Requests.Enqueue(request);
                if (request.Context.FeedbackInPort == 1)
                {
                    firstStarted.TrySetResult();
                    await releaseFirst.Task.WaitAsync(cancellationToken);
                    await new ChangeJourneyStopWorkflowActionHandler().ExecuteAsync(NextStopAction(), request.Context, cancellationToken);
                }

                return Success(request);
            });
        var manager = fixture.Manager;
        InPortCounterServiceTests.Raise(fixture.Z21, 1);
        var firstProcessing = manager.LastProcessing;
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        InPortCounterServiceTests.Raise(fixture.Z21, 2);
        var secondProcessing = manager.LastProcessing;
        releaseFirst.TrySetResult();
        await Task.WhenAll(firstProcessing, secondProcessing).WaitAsync(TimeSpan.FromSeconds(5));

        var secondContext = fixture.Requests.Last().Context;
        Assert.Multiple(() =>
        {
            Assert.That(secondContext.CurrentStation!.Id, Is.EqualTo(nextStation.Id));
            Assert.That(secondContext.CurrentStationIndex, Is.EqualTo(2));
            Assert.That(secondContext.CurrentJourneySessionState!.CurrentStationId, Is.EqualTo(nextStation.Id));
        });
    }

    [Test]
    public async Task ResetCancelsRunningWorkflowAndReturnsToTheFirstStop()
    {
        using var fixture = new EventPlanFixture();
        var first = new Station { Name = "A" };
        fixture.Journey.Stations = [first, new Station { Name = "B" }];
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.WorkflowService.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
            {
                await new ChangeJourneyStopWorkflowActionHandler().ExecuteAsync(NextStopAction(), request.Context, cancellationToken);
                started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult();
                    throw;
                }

                return Success(request);
            });
        var manager = fixture.Manager;
        InPortCounterServiceTests.Raise(fixture.Z21, 1);
        var processing = manager.LastProcessing;
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        manager.Reset(fixture.Journey);

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await processing.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(manager.GetState(fixture.Journey.Id)!.CurrentStationId, Is.EqualTo(first.Id));
    }

    [Test]
    public async Task NewManagerRestoresTheCheckpointedStop()
    {
        using var fixture = new EventPlanFixture();
        var second = new Station { Name = "B" };
        fixture.Journey.Stations = [new Station { Name = "A" }, second];
        var store = new InMemoryJourneyRuntimeStateStore();
        using (var manager = fixture.CreateManager(store))
        {
            fixture.SetupNextStopActions(times: 1);
            await fixture.RaiseAsync(1, manager);
        }

        using var restored = fixture.CreateManager(store);

        Assert.That(restored.GetState(fixture.Journey.Id)!.CurrentStationId, Is.EqualTo(second.Id));
    }

    [Test]
    public async Task ReorderedStopsRestoreTheSameStopIdentity()
    {
        using var fixture = new EventPlanFixture();
        var second = new Station { Name = "B" };
        fixture.Journey.Stations = [new Station { Name = "A" }, second];
        var store = new InMemoryJourneyRuntimeStateStore();
        using (var manager = fixture.CreateManager(store))
        {
            fixture.SetupNextStopActions(times: 1);
            await fixture.RaiseAsync(1, manager).ConfigureAwait(false);
        }
        fixture.Journey.Stations.Reverse();

        using var restored = fixture.CreateManager(store);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored.GetState(fixture.Journey.Id)!.CurrentStationId, Is.EqualTo(second.Id));
            Assert.That(restored.GetState(fixture.Journey.Id)!.CurrentPos, Is.Zero);
        }
    }

    [Test]
    public async Task RemovedCheckpointedStopFallsBackToFirstPositionWithANewIdentity()
    {
        using var fixture = new EventPlanFixture();
        fixture.Journey.Stations = [new Station { Name = "Old terminal" }];
        var store = new InMemoryJourneyRuntimeStateStore();
        Guid oldRunId;
        using (var manager = fixture.CreateManager(store))
        {
            fixture.SetupNextStopActions(times: 1);
            await fixture.RaiseAsync(1, manager).ConfigureAwait(false);
            oldRunId = manager.GetState(fixture.Journey.Id)!.RunId;
        }
        var replacement = new Station { Name = "New terminal" };
        fixture.Journey.Stations = [replacement];
        using var restored = fixture.CreateManager(store);
        var state = restored.GetState(fixture.Journey.Id)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.CurrentStationId, Is.EqualTo(replacement.Id));
            Assert.That(state.RunId, Is.Not.EqualTo(oldRunId));
        }
    }

    [Test]
    public async Task DisabledAndUnassignedEventsDoNotStartWorkflows()
    {
        using var fixture = new EventPlanFixture();
        fixture.Journey.EventPlan.Events.Single().Enabled = false;
        fixture.Journey.EventPlan.Events.Add(new JourneyEvent { InPort = 1, Count = 1 });
        fixture.Journey.EventPlan.Events.Add(new JourneyEvent { InPort = 1, Count = 1, WorkflowId = Guid.NewGuid() });

        await fixture.RaiseAsync(1);

        Assert.That(fixture.Requests, Is.Empty);
    }

    [Test]
    public async Task MatchingEventPropagatesTheActivationCorrelationToTheWorkflow()
    {
        using var fixture = new EventPlanFixture();
        var correlations = new List<Guid>();
        fixture.Counters.Counted += (_, args) => correlations.Add(args.CorrelationId);

        await fixture.RaiseAsync(1);

        Assert.That(fixture.Requests.Single().SourceCorrelationId, Is.EqualTo(correlations.Single()));
    }

    [Test]
    public async Task MatchingEventPublishesTheStructuredTransitionBeforeTheFeedbackCallback()
    {
        using var fixture = new EventPlanFixture();
        var order = new List<string>();
        var eventBus = new Mock<IEventBus>();
        eventBus.Setup(bus => bus.Publish(It.IsAny<JourneyRuntimeTransitionEvent>()))
            .Callback<JourneyRuntimeTransitionEvent>(transition => order.Add($"transition:{transition.Kind}:{transition.InPort}"));
        using var manager = fixture.CreateManager(eventBus: eventBus.Object);
        manager.FeedbackReceived += (_, _) => order.Add("callback");

        await fixture.RaiseAsync(1, manager);

        Assert.That(order, Is.EqualTo(ExpectedTransitionOrder));
    }

    [Test]
    public async Task RunningWorkflowDoesNotBlockAnotherJourney()
    {
        using var fixture = new EventPlanFixture();
        var otherJourney = new Journey
        {
            IsActive = true,
            EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { InPort = 2, Count = 1, WorkflowId = fixture.Workflow.Id }] }
        };
        fixture.Project.Journeys.Add(otherJourney);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var otherStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.WorkflowService.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
            {
                if (request.Context.CurrentJourney!.Id == otherJourney.Id)
                {
                    otherStarted.TrySetResult();
                }
                else
                {
                    await releaseFirst.Task.WaitAsync(cancellationToken);
                }

                return Success(request);
            });
        var manager = fixture.Manager;
        InPortCounterServiceTests.Raise(fixture.Z21, 1);
        var blockedProcessing = manager.LastProcessing;

        InPortCounterServiceTests.Raise(fixture.Z21, 2);
        var otherProcessing = manager.LastProcessing;

        try
        {
            await otherStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(blockedProcessing.IsCompleted, Is.False,
                "The other journey must start while the first journey's workflow is still blocked.");
        }
        finally
        {
            releaseFirst.TrySetResult();
        }

        await Task.WhenAll(blockedProcessing, otherProcessing).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task FeedbackSubscriberCanReadStateOnAnotherThread()
    {
        using var fixture = new EventPlanFixture();
        var readable = false;
        fixture.Manager.FeedbackReceived += (_, _) =>
        {
            using var observed = new ManualResetEventSlim();
            _ = Task.Run(() =>
            {
                fixture.Manager.GetState(fixture.Journey.Id);
                observed.Set();
            });
            readable = observed.Wait(TimeSpan.FromSeconds(5));
        };
        await fixture.RaiseAsync(1).ConfigureAwait(false);
        Assert.That(readable, Is.True, "External subscribers must run outside the manager lock.");
    }

    private static WorkflowAction NextStopAction() => new()
    {
        Type = ActionType.ChangeJourneyStop,
        ChangeJourneyStop = new ChangeJourneyStopActionPayload { MoveToNextStop = true }
    };

    private static WorkflowExecutionResult Success(WorkflowExecutionRequest request) => new()
    {
        ExecutionId = Guid.NewGuid(),
        WorkflowId = request.Workflow.Id,
        SourceCorrelationId = request.SourceCorrelationId,
        Status = WorkflowExecutionStatus.Succeeded
    };

    private sealed class EventPlanFixture : IDisposable
    {
        private readonly List<IDisposable> _managers = [];
        private TestableJourneyManager? _manager;

        public Mock<IZ21> Z21 { get; } = new();
        public Mock<IWorkflowService> WorkflowService { get; } = new();
        public Workflow Workflow { get; } = new() { Name = "Event workflow" };
        public Journey Journey { get; }
        public Project Project { get; }
        public InPortCounterService Counters { get; }
        public TestableJourneyManager Manager => _manager ??= CreateManager();
        public ConcurrentQueue<WorkflowExecutionRequest> Requests { get; } = new();

        public EventPlanFixture(bool isActive = true)
        {
            Journey = new Journey
            {
                IsActive = isActive,
                EventPlan = new JourneyEventPlan
                {
                    Events = [new JourneyEvent { InPort = 1, Count = 1, WorkflowId = Workflow.Id }]
                }
            };
            Project = new Project { Journeys = [Journey], Workflows = [Workflow] };
            Counters = new InPortCounterService(Z21.Object, new AppSettings { Counter = { UseTimerFilter = false } });
            WorkflowService.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns((WorkflowExecutionRequest request, CancellationToken _) =>
                {
                    Requests.Enqueue(request);
                    return Task.FromResult(Success(request));
                });
        }

        public TestableJourneyManager CreateManager(IJourneyRuntimeStateStore? store = null, IEventBus? eventBus = null)
        {
            var manager = new TestableJourneyManager(Z21.Object, Project, WorkflowService.Object, Counters, store, eventBus);
            _managers.Add(manager);
            return manager;
        }

        public void SetupNextStopActions(int times)
        {
            var handler = new ChangeJourneyStopWorkflowActionHandler();
            WorkflowService.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
                {
                    Requests.Enqueue(request);
                    for (var index = 0; index < times; index++)
                    {
                        await handler.ExecuteAsync(NextStopAction(), request.Context, cancellationToken);
                    }

                    return Success(request);
                });
        }

        public Task RaiseAsync(int inPort) => RaiseAsync(inPort, Manager);

        public async Task RaiseAsync(int inPort, TestableJourneyManager manager)
        {
            InPortCounterServiceTests.Raise(Z21, inPort);
            await manager.LastProcessing.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
            foreach (var manager in _managers)
            {
                manager.Dispose();
            }

            Counters.Dispose();
        }
    }

    private sealed class InMemoryJourneyRuntimeStateStore : IJourneyRuntimeStateStore
    {
        private readonly Dictionary<Guid, JourneyRuntimeCheckpoint> _checkpoints = [];

        public JourneyRuntimeCheckpoint? Load(Guid projectId, Guid journeyId) => _checkpoints.GetValueOrDefault(journeyId);

        public void Save(Guid projectId, JourneySessionState state) =>
            _checkpoints[state.JourneyId] = new JourneyRuntimeCheckpoint(state.CurrentStationId, state.RunId);

        public void Reset(Guid projectId, Guid journeyId) => _checkpoints.Remove(journeyId);
    }

    private sealed class TestableJourneyManager(
        IZ21 z21,
        Project project,
        IWorkflowService workflowService,
        InPortCounterService counters,
        IJourneyRuntimeStateStore? store,
        IEventBus? eventBus)
        : JourneyManager(z21, project, workflowService,
            dependencies: new JourneyManagerDependencies { InPortCounterService = counters, RuntimeStateStore = store, EventBus = eventBus })
    {
        public Task LastProcessing { get; private set; } = Task.CompletedTask;

        protected override Task ProcessCountedFeedbackAsync(InPortCountedEventArgs args)
        {
            LastProcessing = base.ProcessCountedFeedbackAsync(args);
            return LastProcessing;
        }
    }
}

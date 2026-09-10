// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.Domain.Enum;
using Moq;
using System.Collections.Concurrent;

[TestFixture]
public sealed class JourneyEventPlanTests
{
    [Test]
    public async Task NewPlan_IsInactiveUntilStartedAndUsesCountsAfterThatStart()
    {
        using var fixture = new EventPlanFixture();
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.IsActive, Is.False);
        Assert.That(fixture.Requests, Is.Empty);

        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.CurrentEventBases[1], Is.EqualTo(1UL));
        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(1);
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Requests, Has.Count.EqualTo(1));
            Assert.That(fixture.Counters.GetSnapshot().Single().Count, Is.EqualTo(3UL));
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.CompletedEventIds,
                Is.EqualTo(new[] { fixture.Journey.EventPlan!.Events.Single().Id }));
        });
    }

    [Test]
    public async Task Events_AreIndependentOfDisplayOrderAndOtherInPorts()
    {
        using var fixture = new EventPlanFixture();
        var later = new JourneyEvent { InPort = 1, Count = 3, WorkflowId = fixture.Workflow.Id };
        var otherPort = new JourneyEvent { InPort = 2, Count = 1, WorkflowId = fixture.Workflow.Id };
        var earlier = new JourneyEvent { InPort = 1, Count = 1, WorkflowId = fixture.Workflow.Id };
        fixture.Journey.EventPlan!.Events = [later, otherPort, earlier];
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        await fixture.RaiseAsync(2);
        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests.Select(request => request.Context.FeedbackInPort), Is.EqualTo(new uint?[] { 2, 1 }));
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests.Select(request => request.Context.FeedbackInPort), Is.EqualTo(new uint?[] { 2, 1, 1 }));
    }

    [Test]
    public async Task Start_CapturesEventsWorkflowsAndStopsUntilTheNextRun()
    {
        using var fixture = new EventPlanFixture();
        fixture.Journey.Stations = [new Station { Name = "Original stop" }];
        var originalName = fixture.Workflow.Name;
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        fixture.Journey.EventPlan!.Events.Single().InPort = 2;
        fixture.Journey.EventPlan.Events.Single().Count = 8;
        fixture.Journey.Stations[0].Name = "Edited stop";
        fixture.Workflow.Name = "Edited workflow";
        await fixture.RaiseAsync(1);

        var request = fixture.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(request.Context.CurrentJourney!.EventPlan!.Events.Single().Count, Is.EqualTo(1UL));
            Assert.That(request.Context.CurrentStation!.Name, Is.EqualTo("Original stop"));
            Assert.That(request.Workflow.Name, Is.EqualTo(originalName));
        });
    }

    [Test]
    public async Task StopAndRestart_CaptureNewBasesWithoutResettingGlobalCounts()
    {
        using var fixture = new EventPlanFixture();
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        var firstRunId = fixture.Manager.GetState(fixture.Journey.Id)!.RunId;
        await fixture.RaiseAsync(1);
        await fixture.Manager.StopJourneyAsync(fixture.Journey);
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests, Has.Count.EqualTo(1));
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        var state = fixture.Manager.GetState(fixture.Journey.Id)!;
        Assert.Multiple(() =>
        {
            Assert.That(state.RunId, Is.Not.EqualTo(firstRunId));
            Assert.That(state.CurrentEventBases[1], Is.EqualTo(2UL));
            Assert.That(state.CompletedEventIds, Is.Empty);
        });
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests, Has.Count.EqualTo(2));
        Assert.That(fixture.Counters.GetSnapshot().Single().Count, Is.EqualTo(3UL));
    }

    [Test]
    public async Task Feedback_DoesNotMoveStopsAndStopActionsPublishEveryTransition()
    {
        using var fixture = new EventPlanFixture();
        var first = new Station { Name = "A" };
        var second = new Station { Name = "B" };
        var third = new Station { Name = "C" };
        fixture.Journey.Stations = [first, second, third];
        fixture.Journey.BehaviorOnLastStop = BehaviorOnLastStop.None;
        var stations = new List<Guid>();
        var completed = new List<Guid>();
        fixture.Manager.StationChanged += (_, args) => stations.Add(args.Station.Id);
        fixture.Manager.JourneyCompleted += (_, args) => completed.Add(args.JourneyRunId);
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.CurrentStationId, Is.EqualTo(first.Id));

        var request = fixture.Requests.Single();
        var action = new WorkflowAction
        {
            Type = ActionType.ChangeJourneyStop,
            ChangeJourneyStop = new ChangeJourneyStopActionPayload { MoveToNextStop = true }
        };
        var handler = new ChangeJourneyStopWorkflowActionHandler();
        await handler.ExecuteAsync(action, request.Context);
        await handler.ExecuteAsync(action, request.Context);
        await handler.ExecuteAsync(action, request.Context);
        Assert.Multiple(() =>
        {
            Assert.That(stations, Is.EqualTo(new[] { second.Id, third.Id }));
            Assert.That(completed, Is.EqualTo(new[] { request.Context.CurrentJourneySessionState!.RunId }));
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.IsActive, Is.False);
            Assert.That(fixture.Counters.HasActiveJourneys, Is.False);
        });
    }

    [Test]
    public async Task OldRunStopAction_CannotChangeRestartedJourney()
    {
        using var fixture = new EventPlanFixture();
        var first = new Station { Name = "A" };
        fixture.Journey.Stations = [first, new Station { Name = "B" }];
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        await fixture.RaiseAsync(1);
        var staleContext = fixture.Requests.Single().Context;
        await fixture.Manager.StopJourneyAsync(fixture.Journey);
        await fixture.Manager.StartJourneyAsync(fixture.Journey);

        Assert.Throws<OperationCanceledException>(() => staleContext.ApplyJourneyStopTransition!(
            new JourneyStopTransition { Mode = JourneyStopTransitionMode.Next }));
        Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.CurrentStationId, Is.EqualTo(first.Id));
    }

    [Test]
    public async Task Stop_CancelsPendingWorkAndRejectsFutureFeedback()
    {
        using var fixture = new EventPlanFixture();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.WorkflowService.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
            {
                fixture.Requests.Enqueue(request);
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
        fixture.Journey.EventPlan!.Events.Add(new JourneyEvent { InPort = 1, Count = 2, WorkflowId = fixture.Workflow.Id });
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        InPortCounterServiceTests.Raise(fixture.Z21, 1);
        var firstProcessing = fixture.Manager.LastProcessing;
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        InPortCounterServiceTests.Raise(fixture.Z21, 1);
        var queuedProcessing = fixture.Manager.LastProcessing;
        await fixture.Manager.StopJourneyAsync(fixture.Journey);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.WhenAll(firstProcessing, queuedProcessing).WaitAsync(TimeSpan.FromSeconds(5));
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueuedWorkflow_SeesTheStopSelectedByThePreviousWorkflow()
    {
        using var fixture = new EventPlanFixture();
        var firstStation = new Station { Name = "A" };
        var nextStation = new Station { Name = "B" };
        fixture.Journey.Stations = [firstStation, nextStation];
        fixture.Journey.EventPlan!.Events.Add(new JourneyEvent { InPort = 2, Count = 1, WorkflowId = fixture.Workflow.Id });
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
                    await new ChangeJourneyStopWorkflowActionHandler().ExecuteAsync(new WorkflowAction
                    {
                        Type = ActionType.ChangeJourneyStop,
                        ChangeJourneyStop = new ChangeJourneyStopActionPayload { MoveToNextStop = true }
                    }, request.Context, cancellationToken);
                }

                return Success(request);
            });
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        InPortCounterServiceTests.Raise(fixture.Z21, 1);
        var firstProcessing = fixture.Manager.LastProcessing;
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        InPortCounterServiceTests.Raise(fixture.Z21, 2);
        var secondProcessing = fixture.Manager.LastProcessing;
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
    public async Task TwoJourneys_StartAtDifferentGlobalCountsAndKeepIndependentBases()
    {
        using var fixture = new EventPlanFixture();
        var secondJourney = new Journey
        {
            EventPlan = new JourneyEventPlan
            {
                Events = [new JourneyEvent { InPort = 1, Count = 1, WorkflowId = fixture.Workflow.Id }]
            }
        };
        fixture.Project.Journeys.Add(secondJourney);
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        await fixture.RaiseAsync(1);
        await fixture.Manager.StartJourneyAsync(secondJourney);
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests.Select(request => request.Context.CurrentJourney!.Id),
            Is.EqualTo(new[] { fixture.Journey.Id, secondJourney.Id }));
        Assert.That(fixture.Counters.GetSnapshot().Single().Count, Is.EqualTo(2UL));
    }

    [Test]
    public async Task GotoJourney_AlreadyActiveEventPlanKeepsItsRunAndCountBases()
    {
        using var fixture = new EventPlanFixture();
        var targetJourney = new Journey
        {
            EventPlan = new JourneyEventPlan
            {
                Events = [new JourneyEvent { InPort = 1, Count = 2, WorkflowId = fixture.Workflow.Id }]
            }
        };
        var thirdJourney = new Journey { EventPlan = new JourneyEventPlan() };
        fixture.Project.Journeys.AddRange([targetJourney, thirdJourney]);
        fixture.Journey.EventPlan!.Events.Single().InPort = 2;
        fixture.Journey.Stations = [new Station { Name = "Final stop" }];
        fixture.Journey.BehaviorOnLastStop = BehaviorOnLastStop.GotoJourney;
        fixture.Journey.NextJourneyId = targetJourney.Id;

        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(1);
        await fixture.Manager.StartJourneyAsync(targetJourney);
        await fixture.Manager.StartJourneyAsync(thirdJourney);
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        var targetRunId = fixture.Manager.GetState(targetJourney.Id)!.RunId;
        var thirdRunId = fixture.Manager.GetState(thirdJourney.Id)!.RunId;
        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(2);

        await new ChangeJourneyStopWorkflowActionHandler().ExecuteAsync(new WorkflowAction
        {
            Type = ActionType.ChangeJourneyStop,
            ChangeJourneyStop = new ChangeJourneyStopActionPayload { MoveToNextStop = true }
        }, fixture.Requests.Single().Context);

        var targetState = fixture.Manager.GetState(targetJourney.Id)!;
        Assert.Multiple(() =>
        {
            Assert.That(targetState.IsActive, Is.True);
            Assert.That(targetState.RunId, Is.EqualTo(targetRunId));
            Assert.That(targetState.CurrentEventBases[1], Is.EqualTo(2UL));
            Assert.That(fixture.Manager.GetState(thirdJourney.Id)!.RunId, Is.EqualTo(thirdRunId));
            Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.IsActive, Is.False);
        });

        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests.Last().Context.CurrentJourney!.Id, Is.EqualTo(targetJourney.Id));
    }

    [Test]
    public async Task DisabledAndUnassignedEvents_AreSafeAndEmptyPlansCanBeStopped()
    {
        using var fixture = new EventPlanFixture();
        fixture.Journey.EventPlan!.Events.Single().Enabled = false;
        fixture.Journey.EventPlan.Events.Add(new JourneyEvent { InPort = 1, Count = 1 });
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests, Is.Empty);
        Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.CompletedEventIds, Has.Count.EqualTo(1));
        await fixture.Manager.StopJourneyAsync(fixture.Journey);
        fixture.Journey.EventPlan.Events.Clear();
        await fixture.Manager.StartJourneyAsync(fixture.Journey);
        Assert.That(fixture.Manager.GetState(fixture.Journey.Id)!.IsActive, Is.True);
        await fixture.Manager.StopJourneyAsync(fixture.Journey);
        Assert.That(fixture.Counters.TryResetAll(), Is.True);
    }

    private static WorkflowExecutionResult Success(WorkflowExecutionRequest request) => new()
    {
        ExecutionId = Guid.NewGuid(),
        WorkflowId = request.Workflow.Id,
        SourceCorrelationId = request.SourceCorrelationId,
        Status = WorkflowExecutionStatus.Succeeded
    };

    private sealed class EventPlanFixture : IDisposable
    {
        public Mock<IZ21> Z21 { get; } = new();
        public Mock<IWorkflowService> WorkflowService { get; } = new();
        public Workflow Workflow { get; } = new() { Name = "Event workflow" };
        public Journey Journey { get; }
        public Project Project { get; }
        public InPortCounterService Counters { get; }
        public TestableJourneyManager Manager { get; }
        public ConcurrentQueue<WorkflowExecutionRequest> Requests { get; } = new();

        public EventPlanFixture()
        {
            Journey = new Journey
            {
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
            Manager = new TestableJourneyManager(Z21.Object, Project, WorkflowService.Object, Counters);
        }

        public async Task RaiseAsync(int inPort)
        {
            InPortCounterServiceTests.Raise(Z21, inPort);
            await Manager.LastProcessing.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
            Manager.Dispose();
            Counters.Dispose();
        }
    }

    private sealed class TestableJourneyManager(IZ21 z21, Project project, IWorkflowService workflowService, InPortCounterService counters)
        : JourneyManager(z21, project, workflowService,
            dependencies: new JourneyManagerDependencies { InPortCounterService = counters })
    {
        public Task LastProcessing { get; private set; } = Task.CompletedTask;

        protected override Task ProcessCountedFeedbackAsync(InPortCountedEventArgs args)
        {
            LastProcessing = base.ProcessCountedFeedbackAsync(args);
            return LastProcessing;
        }
    }
}

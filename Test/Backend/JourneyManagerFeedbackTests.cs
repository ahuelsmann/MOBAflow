// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moq;

/// <summary>
/// Regression tests for <see cref="JourneyManager"/> feedback handling edge cases.
/// </summary>
[TestFixture]
public sealed class JourneyManagerFeedbackTests
{
    /// <summary>
    /// Exposes protected feedback processing for tests.
    /// </summary>
    private sealed class TestableJourneyManager : JourneyManager
    {
        public TestableJourneyManager(
            IZ21 z21,
            Project project,
            IWorkflowService workflowService,
            ActionExecutionContext? executionContext = null,
            IEventBus? eventBus = null)
            : base(
                z21,
                project,
                workflowService,
                executionContext,
                dependencies: new JourneyManagerDependencies { EventBus = eventBus })
        {
        }

        public Task RunProcessFeedbackAsync(FeedbackResult feedback) => ProcessFeedbackAsync(feedback);
    }

    [Test]
    public async Task ProcessFeedbackAsync_JourneyWithNoStations_DoesNotThrowAndAdvancesSequence()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey { FeedbackSequence = [new JourneyFeedbackStep { InPort = 1 }], Stations = [] };
        var project = new Project();
        project.Journeys.Add(journey);

        var context = new ActionExecutionContext { Z21 = z21Mock.Object };

        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, context);

        var feedback = new FeedbackResult(BuildFeedbackPacketForInPort(1));
        await manager.RunProcessFeedbackAsync(feedback).ConfigureAwait(false);

        var state = manager.GetState(journey.Id);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.CurrentFeedbackIndex, Is.EqualTo(1));
        Assert.That(state.CurrentStepOccurrence, Is.Zero);
    }

    [Test]
    public async Task ProcessFeedbackAsync_CurrentPosOutOfRange_DoesNotThrowAndAdvancesSequence()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey
        {
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 1 }],
            Stations = [new Station { Name = "A" }],
            FirstPos = 0
        };
        var project = new Project();
        project.Journeys.Add(journey);

        var context = new ActionExecutionContext { Z21 = z21Mock.Object };

        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, context);

        var state = manager.GetState(journey.Id);
        Assert.That(state, Is.Not.Null);
        state!.CurrentPos = 99;

        var feedback = new FeedbackResult(BuildFeedbackPacketForInPort(1));
        await manager.RunProcessFeedbackAsync(feedback).ConfigureAwait(false);

        Assert.That(state.CurrentFeedbackIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task ProcessFeedbackAsync_UnexpectedInPort_DoesNotAdvanceSequence()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey { FeedbackSequence = [new JourneyFeedbackStep { InPort = 1 }], Stations = [] };
        var project = new Project();
        project.Journeys.Add(journey);

        var context = new ActionExecutionContext { Z21 = z21Mock.Object };

        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, context);

        var feedback = new FeedbackResult(BuildFeedbackPacketWithoutFeedback());
        await manager.RunProcessFeedbackAsync(feedback).ConfigureAwait(false);

        var state = manager.GetState(journey.Id);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.CurrentFeedbackIndex, Is.Zero);
        Assert.That(state.CurrentStepOccurrence, Is.Zero);
    }

    [Test]
    public async Task ProcessFeedbackAsync_FeedbackStep_ExecutesWorkflowForCurrentStop()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var workflowId = Guid.NewGuid();
        var currentStop = new Station { Name = "Bielefeld" };
        var journey = new Journey
        {
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 1, WorkflowId = workflowId }],
            Stations = [currentStop]
        };
        var project = new Project();
        project.Workflows.Add(new Workflow { Id = workflowId, Name = "Event workflow" });
        project.Journeys.Add(journey);
        var context = new ActionExecutionContext { Z21 = z21Mock.Object };
        Station? capturedStation = null;
        WorkflowExecutionRequest? capturedRequest = null;
        workflowMock
            .Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecutionRequest, CancellationToken>((request, _) =>
            {
                capturedRequest = request;
                capturedStation = request.Context.CurrentStation;
            })
            .Returns<WorkflowExecutionRequest, CancellationToken>((request, _) => Task.FromResult(Succeeded(request)));

        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, context);

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(1))).ConfigureAwait(false);

        workflowMock.Verify(service => service.ExecuteAsync(
            It.Is<WorkflowExecutionRequest>(request => request.Workflow.Id == workflowId),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(capturedStation, Is.SameAs(currentStop));
        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Context.FeedbackInPort, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ProcessFeedbackAsync_StationReached_RaisesFeedbackAfterPositionAdvance()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey
        {
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 1 }],
            Stations =
            [
                new Station { Name = "Bielefeld" }
            ]
        };
        var project = new Project();
        project.Journeys.Add(journey);
        var context = new ActionExecutionContext { Z21 = z21Mock.Object };
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, context);
        var feedbackPositions = new List<int>();
        var feedbackOccurrences = new List<uint>();
        manager.FeedbackReceived += (_, args) =>
        {
            feedbackPositions.Add(args.SessionState.CurrentPos);
            feedbackOccurrences.Add(args.SessionState.CurrentStepOccurrence);
        };

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(1))).ConfigureAwait(false);

        Assert.That(feedbackPositions, Is.EqualTo(new[] { 0, 0 }));
        Assert.That(feedbackOccurrences, Is.EqualTo(new uint[] { 1, 0 }));
    }

    [Test]
    public async Task ProcessFeedbackAsync_RepeatStep_ExecutesWorkflowOnlyOnFinalOccurrence()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var workflowId = Guid.NewGuid();
        var journey = new Journey
        {
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 2, RepeatCount = 3, WorkflowId = workflowId }]
        };
        var project = new Project { Journeys = [journey], Workflows = [new Workflow { Id = workflowId }] };
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object);

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(2)));
        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(2)));

        Assert.That(manager.GetState(journey.Id)!.CurrentStepOccurrence, Is.EqualTo(2));
        workflowMock.Verify(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(2)));

        Assert.That(manager.GetState(journey.Id)!.CurrentFeedbackIndex, Is.EqualTo(1));
        workflowMock.Verify(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProcessFeedbackAsync_StopTransition_IsAppliedBeforeWorkflow()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var first = new Station { Name = "Bielefeld" };
        var target = new Station { Name = "Herford" };
        var workflowId = Guid.NewGuid();
        var step = new JourneyFeedbackStep { InPort = 2, WorkflowId = workflowId };
        step.StopTransition = new JourneyStopTransition { Mode = Moba.Domain.Enum.JourneyStopTransitionMode.SpecificStation, StationId = target.Id };
        var journey = new Journey { Stations = [first, target], FeedbackSequence = [step] };
        var project = new Project { Journeys = [journey], Workflows = [new Workflow { Id = workflowId }] };
        Station? workflowStation = null;
        workflowMock.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecutionRequest, CancellationToken>((request, _) => workflowStation = request.Context.CurrentStation)
            .Returns<WorkflowExecutionRequest, CancellationToken>((request, _) => Task.FromResult(Succeeded(request)));
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var transitions = new List<JourneyRuntimeTransitionKind>();
        eventBus.Subscribe<JourneyRuntimeTransitionEvent>(transition => transitions.Add(transition.Kind));
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, eventBus: eventBus);

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(2)));

        Assert.That(workflowStation, Is.SameAs(target));
        Assert.That(manager.GetState(journey.Id)!.CurrentStationId, Is.EqualTo(target.Id));
        Assert.That(
            transitions,
            Is.EqualTo(new[] { JourneyRuntimeTransitionKind.FeedbackAccepted, JourneyRuntimeTransitionKind.StopChanged }));
    }

    [Test]
    public async Task ProcessFeedbackAsync_DoesNotBlockIndependentSourceWhileWorkflowRuns()
    {
        // Arrange
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var blockingWorkflowId = Guid.NewGuid();
        var workflowStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseWorkflow = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        workflowMock
            .Setup(service => service.ExecuteAsync(
                It.Is<WorkflowExecutionRequest>(request => request.Workflow.Id == blockingWorkflowId),
                It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken _) =>
            {
                workflowStarted.TrySetResult();
                await releaseWorkflow.Task.ConfigureAwait(false);
                return Succeeded(request);
            });
        var blockingJourney = new Journey
        {
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 1, WorkflowId = blockingWorkflowId }]
        };
        var independentJourney = new Journey
        {
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 2 }]
        };
        var project = new Project
        {
            Journeys = [blockingJourney, independentJourney],
            Workflows = [new Workflow { Id = blockingWorkflowId }]
        };
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object);

        // Act
        var blockingFeedback = manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(1)));
        await workflowStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var independentFeedback = manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(2)));
        await Task.Delay(25);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(independentFeedback.IsCompleted, Is.True);
            Assert.That(manager.GetState(independentJourney.Id)!.CurrentFeedbackIndex, Is.EqualTo(1));
        });

        releaseWorkflow.TrySetResult();
        await Task.WhenAll(blockingFeedback, independentFeedback).WaitAsync(TimeSpan.FromSeconds(1));
        Assert.That(manager.GetState(independentJourney.Id)!.CurrentFeedbackIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task ProcessFeedbackAsync_SerializesWorkflowsFromSameSource()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var firstWorkflowId = Guid.NewGuid();
        var secondWorkflowId = Guid.NewGuid();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executionOrder = new List<Guid>();
        workflowMock
            .Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
            {
                executionOrder.Add(request.Workflow.Id);
                if (request.Workflow.Id == firstWorkflowId)
                {
                    firstStarted.TrySetResult();
                    await releaseFirst.Task.WaitAsync(cancellationToken);
                }

                return Succeeded(request);
            });
        var journey = new Journey
        {
            FeedbackSequence =
            [
                new JourneyFeedbackStep { InPort = 1, WorkflowId = firstWorkflowId },
                new JourneyFeedbackStep { InPort = 1, WorkflowId = secondWorkflowId }
            ]
        };
        var project = new Project
        {
            Journeys = [journey],
            Workflows = [new Workflow { Id = firstWorkflowId }, new Workflow { Id = secondWorkflowId }]
        };
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object);

        var firstFeedback = manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(1)));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var secondFeedback = manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(1)));
        await Task.Delay(25);

        Assert.That(executionOrder, Is.EqualTo(new[] { firstWorkflowId }));

        releaseFirst.TrySetResult();
        await Task.WhenAll(firstFeedback, secondFeedback).WaitAsync(TimeSpan.FromSeconds(1));
        Assert.That(executionOrder, Is.EqualTo(new[] { firstWorkflowId, secondWorkflowId }));
    }

    [Test]
    public async Task ProcessFeedbackAsync_PropagatesSourceCorrelationToWorkflowRequest()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var workflowId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        WorkflowExecutionRequest? capturedRequest = null;
        workflowMock
            .Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecutionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns<WorkflowExecutionRequest, CancellationToken>((request, _) => Task.FromResult(Succeeded(request)));
        var journey = new Journey
        {
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 3, WorkflowId = workflowId }]
        };
        var project = new Project { Journeys = [journey], Workflows = [new Workflow { Id = workflowId }] };
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object);

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(3), correlationId));

        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.SourceCorrelationId, Is.EqualTo(correlationId));
            Assert.That(capturedRequest.Context.FeedbackInPort, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Reset_CancelsRunningWorkflowOwnedByJourney()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var workflowId = Guid.NewGuid();
        var workflowStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        workflowMock
            .Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
            {
                workflowStarted.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return Succeeded(request);
                }
                catch (OperationCanceledException)
                {
                    cancellationObserved.TrySetResult();
                    throw;
                }
            });
        var journey = new Journey
        {
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 4, WorkflowId = workflowId }]
        };
        var project = new Project { Journeys = [journey], Workflows = [new Workflow { Id = workflowId }] };
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object);

        var feedbackTask = manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(4)));
        await workflowStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        manager.Reset(journey);

        await cancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await feedbackTask.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.That(manager.GetState(journey.Id)!.CurrentFeedbackIndex, Is.Zero);
    }

    private static WorkflowExecutionResult Succeeded(WorkflowExecutionRequest request) => new()
    {
        ExecutionId = Guid.NewGuid(),
        WorkflowId = request.Workflow.Id,
        SourceCorrelationId = request.SourceCorrelationId,
        Status = WorkflowExecutionStatus.Succeeded
    };

    [Test]
    public async Task ProcessFeedbackAsync_TerminalTransition_RaisesOneStableRunCompletion()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey
        {
            Stations = [new Station { Name = "Bielefeld" }],
            BehaviorOnLastStop = Moba.Domain.Enum.BehaviorOnLastStop.None,
            FeedbackSequence =
            [
                new JourneyFeedbackStep
                {
                    InPort = 3,
                    StopTransition = new JourneyStopTransition
                    {
                        Mode = Moba.Domain.Enum.JourneyStopTransitionMode.Next
                    }
                }
            ]
        };
        var project = new Project { Journeys = [journey] };
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var transitions = new List<JourneyRuntimeTransitionKind>();
        eventBus.Subscribe<JourneyRuntimeTransitionEvent>(transition => transitions.Add(transition.Kind));
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, eventBus: eventBus);
        var completions = new List<Guid>();
        manager.JourneyCompleted += (_, args) => completions.Add(args.JourneyRunId);
        var initialRunId = manager.GetState(journey.Id)!.RunId;

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(3)));
        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(3)));

        Assert.That(completions, Is.EqualTo(new[] { initialRunId }));

        manager.Reset(journey);
        var resetRunId = manager.GetState(journey.Id)!.RunId;
        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(3)));

        Assert.Multiple(() =>
        {
            Assert.That(resetRunId, Is.Not.EqualTo(initialRunId));
            Assert.That(completions, Is.EqualTo(new[] { initialRunId, resetRunId }));
            Assert.That(
                transitions,
                Is.EqualTo(new[]
                {
                    JourneyRuntimeTransitionKind.FeedbackAccepted,
                    JourneyRuntimeTransitionKind.Completed,
                    JourneyRuntimeTransitionKind.Stopped,
                    JourneyRuntimeTransitionKind.Reset,
                    JourneyRuntimeTransitionKind.FeedbackAccepted,
                    JourneyRuntimeTransitionKind.Completed,
                    JourneyRuntimeTransitionKind.Stopped
                }));
        });
    }

    [Test]
    public async Task ProcessFeedbackAsync_Should_PublishStructuredTransitionBeforeLegacyCallback()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var station = new Station { Name = "Bielefeld" };
        var step = new JourneyFeedbackStep
        {
            InPort = 5,
            RepeatCount = 2,
            StopTransition = new JourneyStopTransition
            {
                Mode = Moba.Domain.Enum.JourneyStopTransitionMode.SpecificStation,
                StationId = station.Id
            }
        };
        var journey = new Journey { Stations = [station], FeedbackSequence = [step] };
        var project = new Project { Journeys = [journey] };
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var deliveryOrder = new List<string>();
        var transitions = new List<JourneyRuntimeTransitionEvent>();
        eventBus.Subscribe<JourneyRuntimeTransitionEvent>(transition =>
        {
            deliveryOrder.Add("structured");
            transitions.Add(transition);
        });
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, eventBus: eventBus);
        manager.FeedbackReceived += (_, _) => deliveryOrder.Add("legacy");

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(5)));

        Assert.Multiple(() =>
        {
            Assert.That(deliveryOrder.Take(2), Is.EqualTo(new[] { "structured", "legacy" }));
            Assert.That(transitions, Has.Count.EqualTo(1));
            Assert.That(transitions[0].Kind, Is.EqualTo(JourneyRuntimeTransitionKind.FeedbackAccepted));
            Assert.That(transitions[0].JourneyRunId, Is.EqualTo(manager.GetState(journey.Id)!.RunId));
            Assert.That(transitions[0].CurrentOccurrence, Is.EqualTo(1));
            Assert.That(transitions[0].RequiredOccurrences, Is.EqualTo(2));
            Assert.That(transitions[0].InPort, Is.EqualTo(5));
        });
    }

    /// <summary>
    /// Builds a minimal LAN_RMBUS_DATACHANGED packet with a single active bit for the given 1-based InPort.
    /// </summary>
    private static byte[] BuildFeedbackPacketForInPort(int inPort)
    {
        var portIndex = Math.Max(inPort - 1, 0);
        var groupNumber = portIndex / 64;
        var byteIndex = portIndex % 64 / 8;
        var bitPosition = portIndex % 8;

        var content = new byte[15];
        content[0] = 0x0F;
        content[1] = 0x00;
        content[2] = 0x80;
        content[3] = 0x00;
        content[4] = (byte)groupNumber;
        content[5 + byteIndex] = (byte)(1 << bitPosition);
        return content;
    }

    private static byte[] BuildFeedbackPacketWithoutFeedback()
    {
        return
        [
            0x0F,
            0x00,
            0x80,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00
        ];
    }
}
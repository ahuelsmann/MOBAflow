// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
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
            ActionExecutionContext? executionContext = null)
            : base(z21, project, workflowService, executionContext)
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
        workflowMock
            .Setup(service => service.ExecuteAsync(It.IsAny<Workflow>(), It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()))
            .Callback<Workflow, ActionExecutionContext, WorkflowExecutionOptions>((_, executionContext, _) => capturedStation = executionContext.CurrentStation)
            .Returns(Task.CompletedTask);

        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, context);

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(1))).ConfigureAwait(false);

        workflowMock.Verify(
            service => service.ExecuteAsync(
                It.Is<Workflow>(workflow => workflow.Id == workflowId),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<WorkflowExecutionOptions>()),
            Times.Once);
        Assert.That(capturedStation, Is.SameAs(currentStop));
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
        workflowMock.Verify(service => service.ExecuteAsync(It.IsAny<Workflow>(), It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()), Times.Never);

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(2)));

        Assert.That(manager.GetState(journey.Id)!.CurrentFeedbackIndex, Is.EqualTo(1));
        workflowMock.Verify(service => service.ExecuteAsync(It.IsAny<Workflow>(), It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()), Times.Once);
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
        workflowMock.Setup(service => service.ExecuteAsync(It.IsAny<Workflow>(), It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()))
            .Callback<Workflow, ActionExecutionContext, WorkflowExecutionOptions>((_, context, _) => workflowStation = context.CurrentStation)
            .Returns(Task.CompletedTask);
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object);

        await manager.RunProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(2)));

        Assert.That(workflowStation, Is.SameAs(target));
        Assert.That(manager.GetState(journey.Id)!.CurrentStationId, Is.EqualTo(target.Id));
    }

    [Test]
    public async Task ProcessFeedbackAsync_TerminalTransition_RaisesOneStableRunCompletion()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey
        {
            Stations = [new Station { Name = "Bielefeld" }],
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
        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object);
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

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
    public async Task ProcessFeedbackAsync_JourneyWithNoStations_DoesNotThrowAndIncrementsCounter()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey { InPort = 1, Stations = [] };
        var project = new Project();
        project.Journeys.Add(journey);

        var context = new ActionExecutionContext { Z21 = z21Mock.Object };

        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, context);

        var feedback = new FeedbackResult(BuildFeedbackPacketForInPort(1));
        await manager.RunProcessFeedbackAsync(feedback).ConfigureAwait(false);

        var state = manager.GetState(journey.Id);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Counter, Is.EqualTo(1));
    }

    [Test]
    public async Task ProcessFeedbackAsync_CurrentPosOutOfRange_DoesNotThrowAndIncrementsCounter()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey
        {
            InPort = 1,
            Stations = [new Station { Name = "A", NumberOfLapsToStop = 1 }],
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

        Assert.That(state.Counter, Is.EqualTo(1));
    }

    [Test]
    public async Task ProcessFeedbackAsync_JourneyInPortZero_DoesNotIncrementCounter()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var journey = new Journey { InPort = 0, Stations = [] };
        var project = new Project();
        project.Journeys.Add(journey);

        var context = new ActionExecutionContext { Z21 = z21Mock.Object };

        using var manager = new TestableJourneyManager(z21Mock.Object, project, workflowMock.Object, context);

        var feedback = new FeedbackResult(BuildFeedbackPacketWithoutFeedback());
        await manager.RunProcessFeedbackAsync(feedback).ConfigureAwait(false);

        var state = manager.GetState(journey.Id);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Counter, Is.EqualTo(0));
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

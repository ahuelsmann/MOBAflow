// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;

using Moq;

/// <summary>
/// Verifies that the runtime executes against an isolated deep copy of the editor project,
/// so edits made after activation never leak into the running session and vice versa.
/// </summary>
[TestFixture]
internal sealed class MobaRuntimeServiceProjectIsolationTests
{
    [Test]
    public async Task ActivateProjectAsync_ClonesProject_PreservingJourneyIds()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);

        var journeyId = Guid.NewGuid();
        var project = new Project { Name = "Editor" };
        project.Journeys.Add(new Journey { Id = journeyId, Name = "J1" });

        await runtime.ActivateProjectAsync(project);

        Assert.That(runtime.Current.JourneyStates, Has.Count.EqualTo(1));
        Assert.That(runtime.Current.JourneyStates.ContainsKey(journeyId), Is.True,
            "Runtime copy must preserve the editor journey Ids so snapshots and reset resolve correctly.");
    }

    [Test]
    public async Task EditingProjectAfterActivation_DoesNotAffectRuntime()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);

        var journeyId = Guid.NewGuid();
        var project = new Project { Name = "Editor" };
        project.Journeys.Add(new Journey { Id = journeyId, Name = "J1" });

        await runtime.ActivateProjectAsync(project);

        // Editor keeps mutating its live model after the runtime started.
        project.Journeys.Add(new Journey { Id = Guid.NewGuid(), Name = "J2" });
        project.Name = "Edited";

        // Force a fresh runtime snapshot.
        await runtime.SimulateFeedbackAsync(1);

        Assert.That(runtime.Current.JourneyStates, Has.Count.EqualTo(1),
            "Adding a journey to the editor project must not appear in the isolated runtime copy.");
        Assert.That(runtime.Current.JourneyStates.ContainsKey(journeyId), Is.True);
    }

    [Test]
    public async Task StationTransition_Should_PublishReachedEventOnlyAfterFeedbackAppliesTransition()
    {
        // Arrange
        var z21Mock = CreateZ21Mock();
        var eventBus = new EventBus(Mock.Of<ILogger<EventBus>>());
        using var runtime = CreateRuntime(z21Mock.Object, eventBus);
        var first = new Station { Id = Guid.NewGuid(), Name = "First" };
        var reached = new Station { Id = Guid.NewGuid(), Name = "Reached" };
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Stations = [first, reached],
            FeedbackSequence =
            [
                new JourneyFeedbackStep
                {
                    InPort = 1,
                    StopTransition = new JourneyStopTransition
                    {
                        Mode = JourneyStopTransitionMode.SpecificStation,
                        StationId = reached.Id
                    }
                }
            ]
        };
        var project = new Project { Id = Guid.NewGuid(), Name = "Runtime event", Journeys = [journey] };
        var reachedEvent = new TaskCompletionSource<JourneyStationReachedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        eventBus.Subscribe<JourneyStationReachedEvent>(@event => reachedEvent.TrySetResult(@event));
        await runtime.ActivateProjectAsync(project);

        // Act
        Assert.That(reachedEvent.Task.IsCompleted, Is.False);
        z21Mock.Raise(z21 => z21.Received += null, new FeedbackResult(BuildFeedbackPacketForInPort(1)));
        var published = await reachedEvent.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(published.ProjectId, Is.EqualTo(project.Id));
            Assert.That(published.JourneyId, Is.EqualTo(journey.Id));
            Assert.That(published.JourneyRunId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(published.StationId, Is.EqualTo(reached.Id));
            Assert.That(published.OccurredAt, Is.Not.EqualTo(default(DateTimeOffset)));
            Assert.That(runtime.Current.JourneyStates[journey.Id].CurrentStationId, Is.EqualTo(reached.Id));
        });
    }

    private static Mock<IZ21> CreateZ21Mock()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z => z.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.SetupGet(z => z.IsConnected).Returns(false);
        return z21Mock;
    }

    private static MobaRuntimeService CreateRuntime(IZ21 z21, IEventBus? eventBus = null)
    {
        var workflowServiceMock = new Mock<IWorkflowService>();
        var loggerMock = new Mock<ILogger<MobaRuntimeService>>();

        return new MobaRuntimeService(
            z21,
            workflowServiceMock.Object,
            new ActionExecutionContext { Z21 = z21 },
            new AppSettings
            {
                // Disable auto-connect during tests to keep behavior deterministic.
                Z21 = new Z21Settings { CurrentIpAddress = string.Empty }
            },
            loggerMock.Object,
            eventBus);
    }

    private static byte[] BuildFeedbackPacketForInPort(int inPort)
    {
        var portIndex = Math.Max(inPort - 1, 0);
        var content = new byte[15];
        content[0] = 0x0F;
        content[2] = 0x80;
        content[4] = (byte)(portIndex / 64);
        content[5 + portIndex % 64 / 8] = (byte)(1 << portIndex % 8);
        return content;
    }
}

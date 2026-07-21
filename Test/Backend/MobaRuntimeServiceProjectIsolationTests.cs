// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;

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
    public async Task ActivateProjectAsync_ActivatesClonedInterlockingDefinition()
    {
        var z21Mock = CreateZ21Mock();
        var interlockingRuntime = new Mock<IInterlockingRuntime>();
        interlockingRuntime
            .Setup(runtime => runtime.ActivateAsync(It.IsAny<InterlockingDefinition>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        using var runtime = CreateRuntime(z21Mock.Object, interlockingRuntime.Object);
        var project = new Project { Name = "Editor" };
        project.Interlocking.Routes.Add(new RouteDefinition { Name = "R1" });

        await runtime.ActivateProjectAsync(project);

        interlockingRuntime.Verify(item => item.ActivateAsync(
            It.Is<InterlockingDefinition>(definition =>
                !ReferenceEquals(definition, project.Interlocking)
                && definition.Routes.Count == 1
                && definition.Routes[0].Name == "R1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IZ21> CreateZ21Mock()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z => z.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.SetupGet(z => z.IsConnected).Returns(false);
        return z21Mock;
    }

    private static MobaRuntimeService CreateRuntime(
        IZ21 z21,
        IInterlockingRuntime? interlockingRuntime = null)
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
            interlockingRuntime: interlockingRuntime);
    }
}

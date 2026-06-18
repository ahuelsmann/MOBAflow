// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Domain;

using Moq;

/// <summary>
/// Tests for <see cref="StationManager"/> session reset and post-dispose guard rails.
/// </summary>
[TestFixture]
internal sealed class StationManagerResetTests
{
    [Test]
    public void ResetAll_ClearsStationAndPlatformSessionState()
    {
        var z21 = new Mock<IZ21>();
        var workflow = new Mock<IWorkflowService>();
        var station = new Station { Name = "Hbf", InPort = 3 };
        var platform = new Platform { Name = "Gleis 1", InPort = 9 };
        station.Platforms.Add(platform);
        var project = new Project();
        project.Stations.Add(station);

        using var manager = new StationManager(z21.Object, project, workflow.Object, new ActionExecutionContext { Z21 = z21.Object });
        manager.GetState(station.Id)!.Counter = 4;
        manager.GetState(station.Id)!.IsOccupied = true;
        manager.PlatformManagers[0].GetState(platform.Id)!.Counter = 2;

        manager.ResetAll();

        Assert.Multiple(() =>
        {
            var state = manager.GetState(station.Id);
            Assert.That(state!.Counter, Is.EqualTo(0));
            Assert.That(state.IsOccupied, Is.False);
            Assert.That(manager.PlatformManagers[0].GetState(platform.Id)!.Counter, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task ProcessFeedbackAsync_AfterDispose_IsIgnored()
    {
        var z21 = new Mock<IZ21>();
        var workflow = new Mock<IWorkflowService>();
        var station = new Station { Name = "Hbf", InPort = 3, WorkflowId = Guid.NewGuid() };
        var project = new Project();
        project.Stations.Add(station);
        project.Workflows.Add(new Workflow { Id = station.WorkflowId.Value });

        var manager = new StationManager(z21.Object, project, workflow.Object, new ActionExecutionContext { Z21 = z21.Object });
        manager.Dispose();

        await manager.ProcessFeedbackAsync(new FeedbackResult([0x0F, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]));

        workflow.Verify(
            service => service.ExecuteAsync(It.IsAny<Workflow>(), It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()),
            Times.Never);
    }
}
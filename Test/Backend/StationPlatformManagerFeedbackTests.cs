// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Domain;

using Moq;

[TestFixture]
public sealed class StationPlatformManagerFeedbackTests
{
    [Test]
    public async Task ProcessFeedbackAsync_WhenStationInPortMatches_UpdatesStateAndExecutesWorkflow()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var workflow = new Workflow { Name = "Station Workflow" };
        var station = new Station { Name = "Station A", InPort = 5, WorkflowId = workflow.Id };
        var project = new Project();
        project.Stations.Add(station);
        project.Workflows.Add(workflow);
        var context = new ActionExecutionContext { Z21 = z21Mock.Object };
        Station? capturedStation = null;
        workflowMock
            .Setup(service => service.ExecuteAsync(workflow, It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()))
            .Callback<Workflow, ActionExecutionContext, WorkflowExecutionOptions>((_, executionContext, _) => capturedStation = executionContext.CurrentStation)
            .Returns(Task.CompletedTask);

        using var manager = new StationManager(z21Mock.Object, project, workflowMock.Object, context);
        StationFeedbackEventArgs? eventArgs = null;
        manager.StationChanged += (_, args) => eventArgs = args;

        await manager.ProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(5))).ConfigureAwait(false);

        var state = manager.GetState(station.Id);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Counter, Is.EqualTo(1));
        Assert.That(state.IsOccupied, Is.True);
        Assert.That(eventArgs, Is.Not.Null);
        Assert.That(eventArgs!.Station, Is.SameAs(station));
        workflowMock.Verify(service => service.ExecuteAsync(workflow, It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()), Times.Once);
        Assert.That(capturedStation, Is.SameAs(station));
    }

    [Test]
    public async Task ProcessFeedbackAsync_WhenStationInPortDoesNotMatch_DoesNotExecuteWorkflow()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var workflow = new Workflow { Name = "Station Workflow" };
        var station = new Station { Name = "Station A", InPort = 5, WorkflowId = workflow.Id };
        var project = new Project();
        project.Stations.Add(station);
        project.Workflows.Add(workflow);

        using var manager = new StationManager(z21Mock.Object, project, workflowMock.Object, new ActionExecutionContext { Z21 = z21Mock.Object });

        await manager.ProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(6))).ConfigureAwait(false);

        var state = manager.GetState(station.Id);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Counter, Is.EqualTo(0));
        workflowMock.Verify(service => service.ExecuteAsync(It.IsAny<Workflow>(), It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()), Times.Never);
    }

    [Test]
    public async Task ProcessFeedbackAsync_WhenPlatformInPortMatches_UpdatesStateAndExecutesWorkflow()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var workflow = new Workflow { Name = "Platform Workflow" };
        var platform = new Platform { Name = "Gleis 1", Number = 1, InPort = 9, WorkflowId = workflow.Id };
        var station = new Station { Name = "Station A" };
        station.Platforms.Add(platform);
        var project = new Project();
        project.Stations.Add(station);
        project.Workflows.Add(workflow);
        var context = new ActionExecutionContext { Z21 = z21Mock.Object };
        Station? capturedStation = null;
        Platform? capturedPlatform = null;
        workflowMock
            .Setup(service => service.ExecuteAsync(workflow, It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()))
            .Callback<Workflow, ActionExecutionContext, WorkflowExecutionOptions>((_, executionContext, _) =>
            {
                capturedStation = executionContext.CurrentStation;
                capturedPlatform = executionContext.CurrentPlatform;
            })
            .Returns(Task.CompletedTask);

        using var manager = new PlatformManager(z21Mock.Object, project, station, workflowMock.Object, context);
        PlatformChangedEventArgs? eventArgs = null;
        manager.PlatformChanged += (_, args) => eventArgs = args;

        await manager.ProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(9))).ConfigureAwait(false);

        var state = manager.GetState(platform.Id);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Counter, Is.EqualTo(1));
        Assert.That(state.IsOccupied, Is.True);
        Assert.That(eventArgs, Is.Not.Null);
        Assert.That(eventArgs!.Platform, Is.SameAs(platform));
        workflowMock.Verify(service => service.ExecuteAsync(workflow, It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()), Times.Once);
        Assert.That(capturedStation, Is.SameAs(station));
        Assert.That(capturedPlatform, Is.SameAs(platform));
    }

    [Test]
    public async Task ProcessFeedbackAsync_WhenPlatformWorkflowsOverlap_UsesIsolatedExecutionContexts()
    {
        var z21Mock = new Mock<IZ21>();
        var workflowMock = new Mock<IWorkflowService>();
        var workflowA = new Workflow { Name = "Platform Workflow A" };
        var workflowB = new Workflow { Name = "Platform Workflow B" };
        var platformA = new Platform { Name = "Gleis 1", Number = 1, InPort = 9, WorkflowId = workflowA.Id };
        var platformB = new Platform { Name = "Gleis 2", Number = 2, InPort = 10, WorkflowId = workflowB.Id };
        var stationA = new Station { Name = "Station A" };
        var stationB = new Station { Name = "Station B" };
        stationA.Platforms.Add(platformA);
        stationB.Platforms.Add(platformB);
        var project = new Project();
        project.Stations.Add(stationA);
        project.Stations.Add(stationB);
        project.Workflows.Add(workflowA);
        project.Workflows.Add(workflowB);
        var sharedContext = new ActionExecutionContext { Z21 = z21Mock.Object };
        var capturedContexts = new List<ActionExecutionContext>();
        var enteredWorkflows = new CountdownEvent(2);
        var releaseWorkflows = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        workflowMock
            .Setup(service => service.ExecuteAsync(It.IsAny<Workflow>(), It.IsAny<ActionExecutionContext>(), It.IsAny<WorkflowExecutionOptions>()))
            .Callback<Workflow, ActionExecutionContext, WorkflowExecutionOptions>((_, executionContext, _) =>
            {
                lock (capturedContexts)
                {
                    capturedContexts.Add(executionContext);
                }

                enteredWorkflows.Signal();
            })
            .Returns(async () => await releaseWorkflows.Task.ConfigureAwait(false));

        using var managerA = new PlatformManager(z21Mock.Object, project, stationA, workflowMock.Object, sharedContext);
        using var managerB = new PlatformManager(z21Mock.Object, project, stationB, workflowMock.Object, sharedContext);

        var processingA = managerA.ProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(9)));
        var processingB = managerB.ProcessFeedbackAsync(new FeedbackResult(BuildFeedbackPacketForInPort(10)));

        Assert.That(enteredWorkflows.Wait(TimeSpan.FromSeconds(1)), Is.True);
        releaseWorkflows.SetResult();
        await Task.WhenAll(processingA, processingB).ConfigureAwait(false);

        Assert.That(capturedContexts, Has.Count.EqualTo(2));
        Assert.That(capturedContexts[0], Is.Not.SameAs(capturedContexts[1]));
        Assert.That(capturedContexts.Select(context => context.CurrentPlatform), Is.EquivalentTo(new[] { platformA, platformB }));
        Assert.That(capturedContexts.Select(context => context.CurrentStation), Is.EquivalentTo(new[] { stationA, stationB }));
        Assert.That(sharedContext.CurrentPlatform, Is.Null);
        Assert.That(sharedContext.CurrentStation, Is.Null);
    }

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
}

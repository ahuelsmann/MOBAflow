// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Test.Helpers;
using Microsoft.Extensions.Logging;

using Moba.Backend;
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
    [TestCase(21105, 21105, true)]
    [TestCase(21105, 21106, true)]
    [TestCase(21106, 21105, true)]
    [TestCase(31000, 31000, true)]
    [TestCase(31000, 31001, false)]
    public async Task MobileEndpoint_UsesSameSolutionOrderConflictRuleAsDesktop(int ownerPort, int selectedPort, bool conflict)
    {
        var owner = new Project { Name = "Owner", Z21 = { IpAddress = "192.168.0.111", Port = ownerPort } };
        var selected = new Project { Name = "Selected", Z21 = { IpAddress = "192.168.0.111", Port = selectedPort } };
        var solution = new Solution { Projects = [owner, selected] };
        var endpoint = new ProjectZ21EndpointSource(() => selected, solution: () => solution);

        var resolution = await endpoint.ResolveAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution.Address is null, Is.EqualTo(conflict));
            if (conflict) Assert.That(resolution.Error, Does.Contain("Owner"));
        }
        var first = await new ProjectZ21EndpointSource(() => owner, solution: () => solution).ResolveAsync().ConfigureAwait(false);
        Assert.That(first.Address, Is.Not.Null);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task LateConnectionAttempt_CannotUseAnotherProjectsEndpoint(bool sameProjectId, bool automatic)
    {
        var active = new Project { Z21 = { IpAddress = "192.168.0.111" } };
        var selected = new Project { Z21 = { IpAddress = "192.168.0.112" } };
        if (sameProjectId) selected.Id = active.Id;
        var z21 = CreateZ21Mock();
        using var runtime = new MobaRuntimeService(z21.Object, Mock.Of<IWorkflowService>(),
            new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = z21.Object }), new AppSettings(), Mock.Of<ILogger<MobaRuntimeService>>(),
            endpointSource: new ProjectZ21EndpointSource(() => selected));
        await runtime.ActivateProjectAsync(active).ConfigureAwait(false);

        if (automatic) await runtime.StartAsync().ConfigureAwait(false);
        else await runtime.ConnectAsync().ConfigureAwait(false);

        z21.Verify(value => value.ConnectAsync(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(runtime.Current.StatusText, Does.Contain("selected project's runtime"));
    }

    [Test]
    public async Task UsageRead_CannotUseAnOwnersUnrelatedActiveContext()
    {
        var fixture = new Moba.Test.Helpers.TestProjectRuntimeHost();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = new Project { Name = "Original context" };
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        var owner = fixture.Host.Get(project.Id)!;
        var otherContext = new Project { Name = "Another active project" };
        await owner.Runtime.ActivateProjectAsync(otherContext).ConfigureAwait(false);
        var before = owner.Runtime.Current;

        var observation = await ((ILocomotiveTrainRuntimeUsageReader)fixture.Host)
            .ReadAsync(new(Guid.NewGuid(), project, project.Id)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(observation.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
            Assert.That(observation.LoadedDefinitionCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.ContextMismatch));
            Assert.That(owner.Runtime.Current, Is.SameAs(before), "The query must not reactivate the requested editor project.");
        }
    }

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

    [TestCase(false)]
    [TestCase(true)]
    public async Task ProjectHandoff_WaitsForAnInFlightConnectionBeforeLoggingOff(bool automatic)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var z21 = CreateZ21Mock();
        z21.Setup(value => value.ConnectAsync(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() => { entered.TrySetResult(); return release.Task; });
        using var runtime = CreateRuntime(z21.Object);
        await runtime.ActivateProjectAsync(new Project { Z21 = { IpAddress = "192.168.0.111" } }).ConfigureAwait(false);

        var connection = automatic ? runtime.StartAsync() : runtime.ConnectAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var handoff = runtime.DisconnectForProjectSwitchAsync();
        var disconnectedDuringConnect = handoff.IsCompleted;
        release.TrySetResult();
        await Task.WhenAll(connection, handoff).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        Assert.That(disconnectedDuringConnect, Is.False, "The old attempt must finish before the handoff logs off.");
        z21.Verify(value => value.DisconnectAsync(), Times.Once);
        Assert.That(runtime.Current.StatusText, Is.EqualTo("Disconnected"));
    }

    [Test]
    public async Task UpdateProjectAsync_WaitsForActivationToFinish()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interlocking = new Mock<IInterlockingRuntime>();
        using var runtime = CreateRuntime(CreateZ21Mock().Object, interlockingRuntime: interlocking.Object);
        var project = new Project();
        await runtime.ActivateProjectAsync(project).ConfigureAwait(false);
        interlocking.Setup(value => value.ActivateAsync(It.IsAny<InterlockingDefinition>(), It.IsAny<CancellationToken>()))
            .Returns(() => { entered.TrySetResult(); return release.Task; });

        var activation = runtime.ActivateProjectAsync(project);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var updated = new Project { Id = project.Id, Journeys = [new Journey { Name = "Updated" }] };
        var update = runtime.UpdateProjectAsync(updated);
        var updatedDuringActivation = update.IsCompleted;
        release.TrySetResult();
        await Task.WhenAll(activation, update).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updatedDuringActivation, Is.False);
            Assert.That(runtime.Current.JourneyStates, Does.ContainKey(updated.Journeys[0].Id));
        }
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
    public async Task ActivateProjectAsync_CancelsWorkflowFromReplacedProject()
    {
        var z21Mock = CreateZ21Mock();
        var (workflowService, started, cancelled) = CreateBlockingWorkflowService();
        using var runtime = CreateRuntime(z21Mock.Object, workflowService.Object);
        var project = CreateWorkflowProject(1);
        await runtime.ActivateProjectAsync(project);
        z21Mock.Raise(value => value.Received += null, new FeedbackResult(BuildFeedbackPacketForInPort(1)));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await runtime.ActivateProjectAsync(new Project { Name = "Replacement" });

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.That(cancelled.Task.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task UpdateProjectAsync_KeepsRunningWorkflowAndAppliesSignalChanges()
    {
        var z21Mock = CreateZ21Mock();
        var (workflowService, started, cancelled) = CreateBlockingWorkflowService();
        using var runtime = CreateRuntime(z21Mock.Object, workflowService.Object);
        var project = CreateWorkflowProject(1);
        project.SignalBoxPlan = new SignalBoxPlan();
        await runtime.ActivateProjectAsync(project).ConfigureAwait(false);
        z21Mock.Raise(value => value.Received += null, new FeedbackResult(BuildFeedbackPacketForInPort(1)));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        var signal = new SbSignal { Name = "A1" };
        project.SignalBoxPlan.Elements.Add(signal);

        await runtime.UpdateProjectAsync(project).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cancelled.Task.IsCompleted, Is.False, "A project update must not cancel running workflows.");
            Assert.That(runtime.Current.SignalBoxElements.Select(element => element.ElementId), Does.Contain(signal.Id));
        }
    }

    [Test]
    public async Task UpdateProjectAsync_KeepsCurrentSignalAspects()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);
        var signal = new SbSignal { Name = "A1" };
        var project = new Project { Name = "Editor", SignalBoxPlan = new SignalBoxPlan { Elements = [signal] } };
        var runtimeAspect = Enum.GetValues<SignalAspect>()[^1];
        await runtime.ActivateProjectAsync(project).ConfigureAwait(false);
        await runtime.SetSignalAspectAsync(signal.Id, runtimeAspect).ConfigureAwait(false);
        signal.Name = "A1 renamed";

        await runtime.UpdateProjectAsync(project).ConfigureAwait(false);

        var element = runtime.Current.SignalBoxElements.Single(item => item.ElementId == signal.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(element.Name, Is.EqualTo("A1 renamed"));
            Assert.That(element.SignalAspect, Is.EqualTo(runtimeAspect));
        }
    }

    [Test]
    public async Task UpdateProjectAsync_KeepsJourneyRunsAndFollowsEditedJourneys()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);
        var kept = new Journey { Name = "Kept", Stations = [new Station { Name = "A" }, new Station { Name = "B" }] };
        var removed = new Journey { Name = "Removed" };
        var project = new Project { Name = "Editor", Journeys = [kept, removed] };
        await runtime.ActivateProjectAsync(project).ConfigureAwait(false);
        var runBefore = runtime.Current.JourneyStates[kept.Id].JourneyRunId;

        kept.Stations[0].Name = "A renamed";
        kept.Stations.Insert(0, new Station { Name = "New first stop" });
        project.Journeys.Remove(removed);
        var added = new Journey { Name = "Added", Stations = [new Station { Name = "C" }] };
        project.Journeys.Add(added);
        await runtime.UpdateProjectAsync(project).ConfigureAwait(false);

        var states = runtime.Current.JourneyStates;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(states[kept.Id].JourneyRunId, Is.EqualTo(runBefore), "An update must not restart a journey.");
            Assert.That(states[kept.Id].CurrentStationName, Is.EqualTo("A renamed"), "Progress follows the stop by id.");
            Assert.That(states[kept.Id].CurrentPos, Is.EqualTo(1));
            Assert.That(states.ContainsKey(removed.Id), Is.False);
            Assert.That(states[added.Id].CurrentStationName, Is.EqualTo("C"));
        }
    }

    [Test]
    public async Task UpdateProjectAsync_IgnoresAnotherProject()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);
        var project = new Project { Name = "Active", Journeys = [new Journey { Name = "J1" }] };
        await runtime.ActivateProjectAsync(project).ConfigureAwait(false);

        await runtime.UpdateProjectAsync(new Project { Name = "Other", Journeys = [new Journey(), new Journey()] }).ConfigureAwait(false);

        Assert.That(runtime.Current.JourneyStates, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DisconnectAsync_CancelsActiveProjectWorkflow()
    {
        var z21Mock = CreateZ21Mock();
        var (workflowService, started, cancelled) = CreateBlockingWorkflowService();
        using var runtime = CreateRuntime(z21Mock.Object, workflowService.Object);
        var project = CreateWorkflowProject(2);
        await runtime.ActivateProjectAsync(project);
        z21Mock.Raise(value => value.Received += null, new FeedbackResult(BuildFeedbackPacketForInPort(2)));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await runtime.DisconnectAsync();

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.That(cancelled.Task.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task Dispose_CancelsActiveProjectWorkflow()
    {
        var z21Mock = CreateZ21Mock();
        var (workflowService, started, cancelled) = CreateBlockingWorkflowService();
        var runtime = CreateRuntime(z21Mock.Object, workflowService.Object);
        var project = CreateWorkflowProject(3);
        await runtime.ActivateProjectAsync(project);
        z21Mock.Raise(value => value.Received += null, new FeedbackResult(BuildFeedbackPacketForInPort(3)));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        runtime.Dispose();

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.That(cancelled.Task.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task ActivateProjectAsync_ActivatesClonedInterlockingDefinition()
    {
        var z21Mock = CreateZ21Mock();
        var interlockingRuntime = new Mock<IInterlockingRuntime>();
        interlockingRuntime
            .Setup(runtime => runtime.ActivateAsync(It.IsAny<InterlockingDefinition>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        using var runtime = CreateRuntime(
            z21Mock.Object,
            interlockingRuntime: interlockingRuntime.Object);
        var project = new Project { Name = "Editor" };
        project.Interlocking.Turnouts.Add(new TurnoutDefinition { Name = "W1" });

        await runtime.ActivateProjectAsync(project);

        interlockingRuntime.Verify(item => item.ActivateAsync(
            It.Is<InterlockingDefinition>(definition =>
                !ReferenceEquals(definition, project.Interlocking)
                && definition.Turnouts.Count == 1
                && definition.Turnouts[0].Name == "W1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StationTransition_Should_PublishReachedEventOnlyAfterFeedbackAppliesTransition()
    {
        // Arrange
        var z21Mock = CreateZ21Mock();
        var eventBus = new EventBus(Mock.Of<ILogger<EventBus>>());
        var first = new Station { Id = Guid.NewGuid(), Name = "First" };
        var reached = new Station { Id = Guid.NewGuid(), Name = "Reached" };
        var stopAction = new WorkflowAction
        {
            Type = ActionType.ChangeJourneyStop,
            ChangeJourneyStop = new ChangeJourneyStopActionPayload { MoveToNextStop = false, TargetStationId = reached.Id }
        };
        var workflow = new Workflow { Name = "Reach stop", Actions = [stopAction] };
        var workflowService = new Mock<IWorkflowService>();
        workflowService.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
            {
                await TestWorkflowServices.ChangeJourneyStop().ExecuteAsync(stopAction, request.Context, cancellationToken).ConfigureAwait(false);
                return new WorkflowExecutionResult
                {
                    ExecutionId = Guid.NewGuid(),
                    WorkflowId = request.Workflow.Id,
                    SourceCorrelationId = request.SourceCorrelationId,
                    Status = WorkflowExecutionStatus.Succeeded
                };
            });
        using var runtime = CreateRuntime(z21Mock.Object, workflowService.Object, eventBus: eventBus);
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Stations = [first, reached],
            EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { InPort = 1, Count = 1, WorkflowId = workflow.Id }] }
        };
        var project = new Project { Id = Guid.NewGuid(), Name = "Runtime event", Journeys = [journey], Workflows = [workflow] };
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

    private static MobaRuntimeService CreateRuntime(
        IZ21 z21,
        IWorkflowService? workflowService = null,
        IEventBus? eventBus = null,
        IInterlockingRuntime? interlockingRuntime = null)
    {
        var loggerMock = new Mock<ILogger<MobaRuntimeService>>();

        return new MobaRuntimeService(
            z21,
            workflowService ?? Mock.Of<IWorkflowService>(),
            new ActionExecutionContext { Z21 = z21 },
            new AppSettings
            {
                // Disable auto-connect during tests to keep behavior deterministic.
                Counter = new CounterSettings { CountOfFeedbackPoints = 3, UseTimerFilter = false }
            },
            loggerMock.Object,
            eventBus,
            interlockingRuntime: interlockingRuntime);
    }

    private static (Mock<IWorkflowService> Service, TaskCompletionSource Started, TaskCompletionSource Cancelled)
        CreateBlockingWorkflowService()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new Mock<IWorkflowService>();
        service
            .Setup(value => value.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowExecutionRequest request, CancellationToken cancellationToken) =>
            {
                started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return new WorkflowExecutionResult
                    {
                        ExecutionId = Guid.NewGuid(),
                        WorkflowId = request.Workflow.Id,
                        SourceCorrelationId = request.SourceCorrelationId,
                        Status = WorkflowExecutionStatus.Succeeded
                    };
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult();
                    throw;
                }
            });
        return (service, started, cancelled);
    }

    private static Project CreateWorkflowProject(int inPort)
    {
        var workflow = new Workflow { Name = "Blocking workflow" };
        return new Project
        {
            Name = "Runtime project",
            Workflows = [workflow],
            Journeys =
            [
                new Journey
                {
                    IsActive = true,
                    EventPlan = new JourneyEventPlan
                    {
                        Events = [new JourneyEvent { InPort = (uint)inPort, Count = 1, WorkflowId = workflow.Id }]
                    }
                }
            ]
        };
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

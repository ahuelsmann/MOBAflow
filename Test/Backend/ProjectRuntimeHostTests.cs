// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.ProjectRuntimes;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;

using Moq;

/// <summary>
/// One runtime per project with two fake Z21 endpoints: independence, Z21 conflicts, connection takeover and
/// which runtime the UI sees.
/// </summary>
[TestFixture]
internal sealed class ProjectRuntimeHostTests
{
    private static readonly string[] YardOnly = ["yard"];

    private EventBus _applicationBus = null!;
    private List<Mock<IZ21>> _createdZ21s = null!;
    private Z21ConnectionRegistry _registry = null!;
    private ProjectRuntimeHost _host = null!;

    [SetUp]
    public void SetUp()
    {
        _applicationBus = new EventBus(NullLogger<EventBus>.Instance);
        _createdZ21s = [];
        _registry = new Z21ConnectionRegistry(
            () => new ForwardingEventBus(new EventBus(NullLogger<EventBus>.Instance), _applicationBus),
            _ =>
            {
                var z21 = new Mock<IZ21>();
                _createdZ21s.Add(z21);
                return z21.Object;
            });
        _host = new ProjectRuntimeHost(
            new ProjectRuntimeFactory(_registry, CreateServices()),
            NullLogger<ProjectRuntimeHost>.Instance);
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _host.DisposeAsync().ConfigureAwait(false);
        await _registry.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task Load_CreatesOneRuntimePerProject_AndFeedbackReachesOnlyItsProject()
    {
        var station = Project("Station", "192.168.0.111");
        var yard = Project("Yard", "192.168.0.112");

        await _host.LoadAsync([station, yard]).ConfigureAwait(false);
        InPortCounterServiceTests.Raise(_createdZ21s[0], 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_host.Runtimes, Has.Count.EqualTo(2));
            Assert.That(_createdZ21s, Has.Count.EqualTo(2), "Each project gets its own Z21 connection.");
            Assert.That(Count(station, 1), Is.EqualTo(1UL));
            Assert.That(Count(yard, 1), Is.Zero, "Feedback of one Z21 never reaches the other project.");
        }
    }

    [Test]
    public async Task Load_WithTwoProjectsOnOneZ21_LetsOnlyTheEarlierProjectConnect()
    {
        var station = Project("Station", "192.168.0.111");
        var yard = Project("Yard", "192.168.0.111");

        await _host.LoadAsync([station, yard]).ConfigureAwait(false);

        var yardRuntime = _host.Get(yard.Id)!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(yardRuntime.Connection.ConflictProjectName, Is.EqualTo("Station"));
            Assert.That(yardRuntime.Runtime.Current.StatusText, Does.Contain("already used by project 'Station'"));
            Assert.That(yardRuntime.Connection.Z21, Is.Not.SameAs(_host.Get(station.Id)!.Connection.Z21));
        }

        _createdZ21s[1].Verify(z21 => z21.ConnectAsync(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Load_ProjectWithoutZ21_StaysDisconnectedAndSaysWhy()
    {
        var station = Project("Station", string.Empty);

        await _host.LoadAsync([station]).ConfigureAwait(false);

        Assert.That(_host.Get(station.Id)!.Runtime.Current.StatusText, Is.EqualTo("No Z21 assigned to project 'Station'"));
    }

    [Test]
    public async Task LoadingTheSameSolutionAgain_TakesOverTheOpenConnections()
    {
        var station = Project("Station", "192.168.0.111");
        await _host.LoadAsync([station]).ConfigureAwait(false);
        var firstZ21 = _host.Get(station.Id)!.Connection.Z21;

        await _host.LoadAsync([station]).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_host.Get(station.Id)!.Connection.Z21, Is.SameAs(firstZ21));
            Assert.That(_createdZ21s, Has.Count.EqualTo(1));
        }

        _createdZ21s[0].Verify(z21 => z21.DisconnectAsync(), Times.Never);
    }

    [Test]
    public async Task RemovingAProject_ClosesItsZ21Connection()
    {
        var station = Project("Station", "192.168.0.111");
        await _host.LoadAsync([station]).ConfigureAwait(false);

        await _host.RemoveAsync(station.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_host.Get(station.Id), Is.Null);
            Assert.That(_host.Runtimes, Is.Empty);
        }

        _createdZ21s[0].Verify(z21 => z21.DisconnectAsync(), Times.Once);
        _createdZ21s[0].Verify(z21 => z21.Dispose(), Times.Once);
    }

    [Test]
    public async Task Selecting_ForwardsOnlyTheSelectedRuntimeEventsToTheApplicationBus()
    {
        var station = Project("Station", "192.168.0.111");
        var yard = Project("Yard", "192.168.0.112");
        await _host.LoadAsync([station, yard]).ConfigureAwait(false);
        var received = new List<string>();
        _applicationBus.Subscribe<TestEvent>(e => received.Add(e.Name));

        _host.SelectProject(yard.Id);
        _host.Get(station.Id)!.Connection.EventBus.Publish(new TestEvent("station"));
        _host.Get(yard.Id)!.Connection.EventBus.Publish(new TestEvent("yard"));

        Assert.That(received, Is.EqualTo(YardOnly));
    }

    [Test]
    public async Task SelectedProjectRuntime_SendsCommandsToTheSelectedProjectOnly()
    {
        var station = Project("Station", "192.168.0.111");
        var yard = Project("Yard", "192.168.0.112");
        await _host.LoadAsync([station, yard]).ConfigureAwait(false);
        var selected = new SelectedProjectRuntime(_host, _applicationBus);

        _host.SelectProject(yard.Id);
        await selected.SetTrackPowerAsync(true).ConfigureAwait(false);

        _createdZ21s[1].Verify(z21 => z21.SetTrackPowerOnAsync(It.IsAny<CancellationToken>()), Times.Once);
        _createdZ21s[0].Verify(z21 => z21.SetTrackPowerOnAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AssigningTheZ21OfALaterProject_GivesItToTheEarlierProject()
    {
        var station = Project("Station", string.Empty);
        var yard = Project("Yard", "192.168.0.112");
        await _host.LoadAsync([station, yard]).ConfigureAwait(false);

        station.Z21.IpAddress = "192.168.0.112";
        await _host.ReplaceAsync(station).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_host.Get(station.Id)!.Connection.ConflictProjectName, Is.Null);
            Assert.That(_host.Get(yard.Id)!.Connection.ConflictProjectName, Is.EqualTo("Station"));
            Assert.That(_host.Get(station.Id)!.Connection.Z21, Is.SameAs(_createdZ21s[1].Object), "The open connection is taken over.");
        }
    }

    [Test]
    public async Task RemovingTheOwnerOfAZ21_LetsTheNextProjectConnect()
    {
        var station = Project("Station", "192.168.0.111");
        var yard = Project("Yard", "192.168.0.111");
        await _host.LoadAsync([station, yard]).ConfigureAwait(false);

        await _host.RemoveAsync(station.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_host.Get(yard.Id)!.Connection.ConflictProjectName, Is.Null);
            Assert.That(_host.Get(yard.Id)!.Connection.Z21, Is.SameAs(_createdZ21s[0].Object));
        }

        _createdZ21s[0].Verify(z21 => z21.DisconnectAsync(), Times.Never);
    }

    [Test]
    public async Task SelectionRequestedBeforeTheRuntimeExists_AppliesOnceItIsCreated()
    {
        var station = Project("Station", "192.168.0.111");

        _host.SelectProject(station.Id);
        await _host.LoadAsync([station]).ConfigureAwait(false);

        Assert.That(_host.Selected?.ProjectId, Is.EqualTo(station.Id));
    }

    private ulong Count(Project project, uint inPort) =>
        _host.Get(project.Id)!.Runtime.Current.InPortCounters.Single(counter => counter.InPort == inPort).Count;

    [TestCase(false)]
    [TestCase(true)]
    public async Task SavedProjectUpdate_RefreshesWhistleDefinitions(bool throughSelectedRuntime)
    {
        var z21 = new Mock<IZ21>();
        z21.SetupGet(connection => connection.IsConnected).Returns(true);
        var functionOff = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        z21.Setup(connection => connection.SetLocoFunctionAsync(24, 31, false, It.IsAny<CancellationToken>()))
            .Callback(() => functionOff.TrySetResult())
            .Returns(Task.CompletedTask);
        var registry = new Z21ConnectionRegistry(
            () => new ForwardingEventBus(new EventBus(NullLogger<EventBus>.Instance), _applicationBus),
            _ => z21.Object);
        await using var registryLifetime = registry.ConfigureAwait(false);
        var host = new ProjectRuntimeHost(
            new ProjectRuntimeFactory(registry, CreateServices()), NullLogger<ProjectRuntimeHost>.Instance);
        await using var hostLifetime = host.ConfigureAwait(false);
        var project = Project("Station", "192.0.2.42");
        var locomotive = new Locomotive { DigitalAddress = 12 };
        var rule = new LocomotiveWhistleRule
        {
            LocomotiveId = locomotive.Id, InPort = 1, FunctionIndex = 2, ActiveDurationMilliseconds = 1
        };
        project.Locomotives.Add(locomotive);
        project.LocomotiveWhistleRules.Add(rule);
        await host.LoadAsync([project]).ConfigureAwait(false);
        locomotive.DigitalAddress = 24;
        rule.FunctionIndex = 31;

        if (throughSelectedRuntime)
            await new SelectedProjectRuntime(host, _applicationBus).UpdateProjectAsync(project).ConfigureAwait(false);
        else
            await host.UpdateAsync(project).ConfigureAwait(false);
        host.Get(project.Id)!.Connection.EventBus.Publish(new FeedbackReceivedEvent(1));
        await functionOff.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        z21.Verify(connection => connection.SetLocoFunctionAsync(24, 31, true, It.IsAny<CancellationToken>()), Times.Once);
        z21.Verify(connection => connection.SetLocoFunctionAsync(12, 2, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Project Project(string name, string z21Address) =>
        new() { Name = name, Z21 = { IpAddress = z21Address } };

    private static ProjectRuntimeServices CreateServices() => new()
    {
        Settings = new AppSettings { Counter = { CountOfFeedbackPoints = 2, UseTimerFilter = false } },
        ActionExecutor = new Mock<IActionExecutor>().Object,
        WorkflowDependencies = new WorkflowServiceDependencies
        {
            Validator = new Mock<IWorkflowValidator>().Object,
            EffectPlanner = new Mock<IWorkflowEffectPlanner>().Object,
            TraceStore = new Mock<IWorkflowTraceStore>().Object,
            TimeProvider = TimeProvider.System
        },
        SharedExecutionContext = new ActionExecutionContext { Z21 = new Mock<IZ21>().Object },
        StopTransitionService = new JourneyStopTransitionService(),
        RuntimeStateStore = new Mock<IJourneyRuntimeStateStore>().Object,
        TimeProvider = TimeProvider.System,
        LoggerFactory = NullLoggerFactory.Instance
    };

    private sealed record TestEvent(string Name) : EventBase;
}

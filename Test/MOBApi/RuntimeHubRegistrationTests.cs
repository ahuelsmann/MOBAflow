// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.AspNetCore.SignalR;

using Moba.Contracts.Runtime;
using Moba.MOBApi.Hubs;
using Moba.MOBApi.Service;

using Moq;

using System.Globalization;

/// <summary>
/// Verifies host and remote registration, project groups and disconnect handling of the runtime hub: a remote must
/// only receive its own project's runtime, and a lost host must mark every project as not operational.
/// </summary>
[TestFixture]
internal sealed class RuntimeHubRegistrationTests
{
    private static readonly Guid ProjectA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Test]
    public void ProjectGroup_IsDistinctPerProjectAndSeparateFromTheRemoteGroup()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(RuntimeHub.ProjectGroup(ProjectA), Is.Not.EqualTo(RuntimeHub.ProjectGroup(ProjectB)));
            Assert.That(RuntimeHub.ProjectGroup(ProjectA), Is.Not.EqualTo(RuntimeHub.RuntimeRemoteGroup));
        }
    }

    [Test]
    public async Task RegisterHost_JoinsTheHostGroupAndReportsEachCachedProjectAsOperational()
    {
        var fixture = new HubFixture("host-1");
        fixture.SnapshotCache.Set(Snapshot(ProjectA, isConnected: true));
        fixture.SnapshotCache.Set(Snapshot(ProjectB, isConnected: false));

        await fixture.Hub.RegisterHost().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.HostRegistry.HostConnectionId, Is.EqualTo("host-1"));
            fixture.Groups.Verify(groups => groups.AddToGroupAsync("host-1", "runtime-host", It.IsAny<CancellationToken>()), Times.Once);
            fixture.GroupProxy(ProjectA).Verify(proxy => proxy.SessionStateChanged(true), Times.Once);
            fixture.GroupProxy(ProjectB).Verify(proxy => proxy.SessionStateChanged(false), Times.Once);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task RegisterRemote_WithoutClientId_IsRejectedAndNotRegistered(string? clientId)
    {
        var fixture = new HubFixture("remote-1");

        await Assert.ThatAsync(() => fixture.Hub.RegisterRemote(clientId!, ProjectA.ToString()),
            Throws.TypeOf<HubException>().With.Message.EqualTo("ClientId is required.")).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.RemoteRegistry.Count, Is.Zero);
            Assert.That(fixture.Groups.Invocations, Is.Empty);
        }
    }

    [Test]
    public async Task RegisterRemote_WithProject_JoinsBothGroupsAndReceivesOnlyThatProjectsRuntime()
    {
        var fixture = new HubFixture("remote-1");
        fixture.HostRegistry.SetHost("host-1");
        var snapshotA = fixture.SnapshotCache.Set(Snapshot(ProjectA, isConnected: true));
        fixture.SnapshotCache.Set(Snapshot(ProjectB, isConnected: true));

        await fixture.Hub.RegisterRemote(" phone-1 ", ProjectA.ToString()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.RemoteRegistry.GetAll().Single().ClientId, Is.EqualTo("phone-1"));
            Assert.That(fixture.RemoteRegistry.GetProject("remote-1"), Is.EqualTo(ProjectA));
            Assert.That(fixture.JoinedGroups, Is.EqualTo(new[] { RuntimeHub.RuntimeRemoteGroup, RuntimeHub.ProjectGroup(ProjectA) }));
            fixture.Caller.Verify(caller => caller.SnapshotUpdated(snapshotA.Json), Times.Once);
            fixture.Caller.Verify(caller => caller.SnapshotUpdated(It.IsAny<string>()), Times.Once);
            fixture.Caller.Verify(caller => caller.SessionStateChanged(true), Times.Once);
        }
    }

    [Test]
    public async Task RegisterRemote_WithoutHost_ReportsTheProjectAsNotOperational()
    {
        var fixture = new HubFixture("remote-1");
        fixture.SnapshotCache.Set(Snapshot(ProjectA, isConnected: true));

        await fixture.Hub.RegisterRemote("phone-1", ProjectA.ToString()).ConfigureAwait(false);

        fixture.Caller.Verify(caller => caller.SessionStateChanged(false), Times.Once);
    }

    [Test]
    public async Task RegisterRemote_WithoutProject_OnlyJoinsTheRemoteGroupForSolutionUpdates()
    {
        var fixture = new HubFixture("remote-1");
        fixture.HostRegistry.SetHost("host-1");

        await fixture.Hub.RegisterRemote("phone-1", "").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.RemoteRegistry.GetProject("remote-1"), Is.EqualTo(Guid.Empty));
            Assert.That(fixture.JoinedGroups, Is.EqualTo(new[] { RuntimeHub.RuntimeRemoteGroup }));
            fixture.Caller.Verify(caller => caller.SnapshotUpdated(It.IsAny<string>()), Times.Never);
            fixture.Caller.Verify(caller => caller.SessionStateChanged(false), Times.Once);
        }
    }

    [Test]
    public async Task RegisterRemote_WithAnInvalidProject_IsRejected()
    {
        var fixture = new HubFixture("remote-1");

        await Assert.ThatAsync(() => fixture.Hub.RegisterRemote("phone-1", "not-a-project"),
            Throws.TypeOf<HubException>().With.Message.EqualTo("ProjectId is required.")).ConfigureAwait(false);

        Assert.That(fixture.RemoteRegistry.Count, Is.Zero);
    }

    [Test]
    public async Task RegisterRemote_AgainForAnotherProject_LeavesThePreviousProjectGroup()
    {
        var fixture = new HubFixture("remote-1");
        await fixture.Hub.RegisterRemote("phone-1", ProjectA.ToString()).ConfigureAwait(false);

        await fixture.Hub.RegisterRemote("phone-1", ProjectB.ToString()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.LeftGroups, Is.EqualTo(new[] { RuntimeHub.ProjectGroup(ProjectA) }));
            Assert.That(fixture.RemoteRegistry.GetProject("remote-1"), Is.EqualTo(ProjectB));
            Assert.That(fixture.JoinedGroups, Does.Contain(RuntimeHub.ProjectGroup(ProjectB)));
        }
    }

    [Test]
    public async Task RegisterRemote_AgainForTheSameProject_StaysInItsGroup()
    {
        var fixture = new HubFixture("remote-1");
        await fixture.Hub.RegisterRemote("phone-1", ProjectA.ToString()).ConfigureAwait(false);

        await fixture.Hub.RegisterRemote("phone-1", ProjectA.ToString()).ConfigureAwait(false);

        Assert.That(fixture.LeftGroups, Is.Empty);
    }

    [Test]
    public async Task RegisterRemote_FirstSelectingAProject_LeavesNoProjectGroup()
    {
        var fixture = new HubFixture("remote-1");
        await fixture.Hub.RegisterRemote("phone-1", "").ConfigureAwait(false);

        await fixture.Hub.RegisterRemote("phone-1", ProjectA.ToString()).ConfigureAwait(false);

        Assert.That(fixture.LeftGroups, Is.Empty);
    }

    [Test]
    public async Task RegisterRemote_WithASavedSolution_ReportsItsUpdateTime()
    {
        var fixture = new HubFixture("remote-1");
        fixture.SolutionCache.Set("""{"projects":[]}""", sourcePath: "solution.json");
        fixture.SolutionCache.TryGet(out var solution);

        await fixture.Hub.RegisterRemote("phone-1", "").ConfigureAwait(false);

        fixture.Caller.Verify(
            caller => caller.SolutionUpdated(solution.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
            Times.Once);
    }

    [Test]
    public async Task RegisterRemote_WithASolutionWithoutSourcePath_ReportsNoSolutionUpdate()
    {
        var fixture = new HubFixture("remote-1");
        fixture.SolutionCache.Set("""{"projects":[]}""", sourcePath: null);

        await fixture.Hub.RegisterRemote("phone-1", "").ConfigureAwait(false);

        fixture.Caller.Verify(caller => caller.SolutionUpdated(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task PushSnapshot_SendsTheSnapshotOnlyToItsProjectGroup()
    {
        var fixture = new HubFixture("host-1");
        await fixture.Hub.RegisterHost().ConfigureAwait(false);

        await fixture.Hub.PushSnapshot(Snapshot(ProjectA, isConnected: true)).ConfigureAwait(false);

        Assert.That(fixture.SnapshotCache.TryGet(ProjectA, out var cached), Is.True);
        using (Assert.EnterMultipleScope())
        {
            fixture.GroupProxy(ProjectA).Verify(proxy => proxy.SnapshotUpdated(cached.Json), Times.Once);
            fixture.GroupProxy(ProjectA).Verify(proxy => proxy.SessionStateChanged(true), Times.Once);
            fixture.GroupProxy(ProjectB).Verify(proxy => proxy.SnapshotUpdated(It.IsAny<string>()), Times.Never);
        }
    }

    [Test]
    public async Task PushSnapshot_WithInvalidJson_IsRejectedAsHubError()
    {
        var fixture = new HubFixture("host-1");
        await fixture.Hub.RegisterHost().ConfigureAwait(false);

        await Assert.ThatAsync(() => fixture.Hub.PushSnapshot(Snapshot(Guid.Empty, isConnected: true)),
            Throws.TypeOf<HubException>().With.Message.Contains("names no project")).ConfigureAwait(false);

        Assert.That(fixture.SnapshotCache.GetAll(), Is.Empty);
    }

    [Test]
    public async Task OnDisconnected_OfTheHost_ClearsItAndReportsEveryProjectAsNotOperational()
    {
        var fixture = new HubFixture("host-1");
        await fixture.Hub.RegisterHost().ConfigureAwait(false);
        fixture.SnapshotCache.Set(Snapshot(ProjectA, isConnected: true));
        fixture.SnapshotCache.Set(Snapshot(ProjectB, isConnected: true));

        await fixture.Hub.OnDisconnectedAsync(exception: null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.HostRegistry.HasHost, Is.False);
            fixture.GroupProxy(ProjectA).Verify(proxy => proxy.SessionStateChanged(false), Times.Once);
            fixture.GroupProxy(ProjectB).Verify(proxy => proxy.SessionStateChanged(false), Times.Once);
        }
    }

    [Test]
    public async Task OnDisconnected_OfARemote_UnregistersItAndKeepsTheHost()
    {
        var fixture = new HubFixture("remote-1");
        fixture.HostRegistry.SetHost("host-1");
        fixture.SnapshotCache.Set(Snapshot(ProjectA, isConnected: true));
        await fixture.Hub.RegisterRemote("phone-1", ProjectA.ToString()).ConfigureAwait(false);

        await fixture.Hub.OnDisconnectedAsync(exception: null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.RemoteRegistry.Count, Is.Zero);
            Assert.That(fixture.HostRegistry.HostConnectionId, Is.EqualTo("host-1"));
            fixture.GroupProxy(ProjectA).Verify(proxy => proxy.SessionStateChanged(It.IsAny<bool>()), Times.Never);
        }
    }

    private static string Snapshot(Guid projectId, bool isConnected) =>
        RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = projectId, IsConnected = isConnected });

    /// <summary>A hub on real registries and caches; only SignalR transport (groups and clients) is mocked.</summary>
    private sealed class HubFixture
    {
        private readonly Dictionary<string, Mock<IRuntimeHubCallbacks>> _groupProxies = new(StringComparer.Ordinal);

        public HubFixture(string connectionId)
        {
            var context = new Mock<HubCallerContext>();
            context.SetupGet(value => value.ConnectionId).Returns(connectionId);
            var clients = new Mock<IHubCallerClients<IRuntimeHubCallbacks>>();
            clients.SetupGet(value => value.Caller).Returns(Caller.Object);
            clients.Setup(value => value.Group(It.IsAny<string>())).Returns<string>(name => GroupProxy(name).Object);
            Groups.Setup(groups => groups.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((_, group, _) => JoinedGroups.Add(group))
                .Returns(Task.CompletedTask);
            Groups.Setup(groups => groups.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((_, group, _) => LeftGroups.Add(group))
                .Returns(Task.CompletedTask);

            Hub = new RuntimeHub(
                SnapshotCache,
                SolutionCache,
                HostRegistry,
                new Mock<IRuntimeBroadcastMetrics>().Object,
                new Mock<IRuntimeCommandAdmission>().Object,
                RemoteRegistry)
            {
                Clients = clients.Object,
                Context = context.Object,
                Groups = Groups.Object
            };
        }

        public RuntimeHub Hub { get; }

        public RuntimeSnapshotCache SnapshotCache { get; } = new();

        public SolutionCache SolutionCache { get; } = new();

        public RuntimeHostRegistry HostRegistry { get; } = new();

        public RuntimeRemoteRegistry RemoteRegistry { get; } = new();

        public Mock<IGroupManager> Groups { get; } = new();

        public Mock<IRuntimeHubCallbacks> Caller { get; } = new();

        public List<string> JoinedGroups { get; } = [];

        public List<string> LeftGroups { get; } = [];

        public Mock<IRuntimeHubCallbacks> GroupProxy(Guid projectId) => GroupProxy(RuntimeHub.ProjectGroup(projectId));

        private Mock<IRuntimeHubCallbacks> GroupProxy(string name)
        {
            if (!_groupProxies.TryGetValue(name, out var proxy))
            {
                proxy = new Mock<IRuntimeHubCallbacks>();
                _groupProxies[name] = proxy;
            }

            return proxy;
        }
    }
}

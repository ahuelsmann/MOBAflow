#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.Test.Helpers;
using Moba.WinUI.Service;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

[TestFixture]
internal sealed class RestApiRuntimeHubServiceTests
{
    [Test]
    public async Task DisposeAsync_ShouldCancelPendingPushAndDisconnectOnlyOnce()
    {
        // Arrange
        await using var projectRuntimes = new TestProjectRuntimeHost();
        var station = TestProjectRuntimeHost.Project("Station", "192.168.0.111");
        await projectRuntimes.Host.LoadAsync([station]).ConfigureAwait(false);
        var runtimeHubHostClient = ConnectedHostClient();
        using var mobApiClient = new LocalMobApiClient(new AppSettings());
        var service = new RestApiRuntimeHubService(
            runtimeHubHostClient.Object,
            projectRuntimes.Host,
            NullLogger<RestApiRuntimeHubService>.Instance,
            mobApiClient);

        PublishSnapshot(projectRuntimes, station);

        // Act
        await service.DisposeAsync();
        await service.DisposeAsync();
        PublishSnapshot(projectRuntimes, station);

        // Assert
        runtimeHubHostClient.Verify(client => client.DisconnectAsync(), Times.Once);
        runtimeHubHostClient.Verify(
            client => client.PushSnapshotAsync(It.IsAny<MobaRuntimeSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [CancelAfter(10_000)]
    public async Task SnapshotChanges_ShouldPushTheLatestSnapshotOfEveryProject()
    {
        // Arrange
        await using var projectRuntimes = new TestProjectRuntimeHost();
        var station = TestProjectRuntimeHost.Project("Station", "192.168.0.111");
        var yard = TestProjectRuntimeHost.Project("Yard", "192.168.0.112");
        await projectRuntimes.Host.LoadAsync([station, yard]).ConfigureAwait(false);
        var pushed = new List<MobaRuntimeSnapshot>();
        var bothPushed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtimeHubHostClient = ConnectedHostClient();
        runtimeHubHostClient
            .Setup(client => client.PushSnapshotAsync(It.IsAny<MobaRuntimeSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<MobaRuntimeSnapshot, CancellationToken>((snapshot, _) =>
            {
                lock (pushed)
                {
                    pushed.Add(snapshot);
                    if (pushed.Count == 2)
                    {
                        bothPushed.TrySetResult();
                    }
                }
            })
            .Returns(Task.CompletedTask);
        using var mobApiClient = new LocalMobApiClient(new AppSettings());
        await using var service = new RestApiRuntimeHubService(
            runtimeHubHostClient.Object,
            projectRuntimes.Host,
            NullLogger<RestApiRuntimeHubService>.Instance,
            mobApiClient);

        // Act
        PublishSnapshot(projectRuntimes, station, statusText: "old");
        PublishSnapshot(projectRuntimes, yard, statusText: "yard");
        PublishSnapshot(projectRuntimes, station, statusText: "new");
        await bothPushed.Task.ConfigureAwait(false);

        // Assert
        lock (pushed)
        {
            Assert.That(
                pushed.Select(snapshot => (snapshot.ProjectId, snapshot.StatusText)),
                Is.EquivalentTo(new[] { (station.Id, "new"), (yard.Id, "yard") }));
        }
    }

    private static Mock<IRuntimeHubHostClient> ConnectedHostClient()
    {
        var runtimeHubHostClient = new Mock<IRuntimeHubHostClient>();
        runtimeHubHostClient.SetupGet(client => client.IsConnected).Returns(true);
        runtimeHubHostClient
            .Setup(client => client.DisconnectAsync())
            .Returns(Task.CompletedTask);
        return runtimeHubHostClient;
    }

    /// <summary>Publishes a snapshot on the project's own runtime bus, as its runtime does on every change.</summary>
    private static void PublishSnapshot(TestProjectRuntimeHost projectRuntimes, Project project, string statusText = "") =>
        projectRuntimes.Host.Get(project.Id)!.Connection.EventBus.Publish(
            new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot { ProjectId = project.Id, StatusText = statusText }));

    [Test]
    public async Task DisposeServicesAsync_ShouldPreferAsyncDisposal()
    {
        // Arrange
        var services = new TrackingServiceProvider();

        // Act
        await WinUiAppStartupService.DisposeServicesAsync(services);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(services.AsyncDisposeCount, Is.EqualTo(1));
            Assert.That(services.SyncDisposeCount, Is.Zero);
        });
    }

    private sealed class TrackingServiceProvider : IServiceProvider, IDisposable, IAsyncDisposable
    {
        public int AsyncDisposeCount { get; private set; }

        public int SyncDisposeCount { get; private set; }

        public object? GetService(Type serviceType)
        {
            return null;
        }

        public void Dispose()
        {
            SyncDisposeCount++;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
#endif

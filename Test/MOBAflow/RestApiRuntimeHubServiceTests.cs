#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.SharedUI.Interface;
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
        var runtimeHubHostClient = new Mock<IRuntimeHubHostClient>();
        runtimeHubHostClient
            .Setup(client => client.DisconnectAsync())
            .Returns(Task.CompletedTask);

        var mobaRuntime = new Mock<IMobaRuntime>();
        mobaRuntime
            .SetupGet(runtime => runtime.Current)
            .Returns(MobaRuntimeSnapshot.Empty);

        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var service = new RestApiRuntimeHubService(
            runtimeHubHostClient.Object,
            mobaRuntime.Object,
            eventBus,
            NullLogger<RestApiRuntimeHubService>.Instance);

        eventBus.Publish(new RuntimeSnapshotChangedEvent(MobaRuntimeSnapshot.Empty));

        // Act
        await service.DisposeAsync();
        await service.DisposeAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(eventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.Zero);
        });
        runtimeHubHostClient.Verify(client => client.DisconnectAsync(), Times.Once);
    }

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

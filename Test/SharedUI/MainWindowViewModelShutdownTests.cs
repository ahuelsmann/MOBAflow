// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Events;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;

/// <summary>
/// Regression tests for MainWindowViewModel shutdown behavior.
/// </summary>
[TestFixture]
internal partial class MainWindowViewModelShutdownTests
{
    [Test]
    public async Task PrepareForShutdownAsync_UnsubscribesFromRuntimeSnapshots()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(mobaRuntimeMock, eventBus);

        eventBus.Publish(
            new RuntimeSnapshotChangedEvent(
                new MobaRuntimeSnapshot
                {
                    IsConnected = true,
                    IsTrackPowerOn = true,
                    IsZ21Connecting = false,
                    HasSeenSuccessfulConnection = true,
                    StatusText = "Connected"
                }));

        await viewModel.PrepareForShutdownAsync();

        Assert.That(eventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.EqualTo(0));

        eventBus.Publish(
            new RuntimeSnapshotChangedEvent(
                new MobaRuntimeSnapshot
                {
                    IsConnected = false,
                    IsTrackPowerOn = false,
                    IsZ21Connecting = false,
                    HasSeenSuccessfulConnection = true,
                    StatusText = "Disconnected"
                }));

        Assert.That(viewModel.IsConnected, Is.True);
    }

    [Test]
    public async Task PrepareForShutdownAsync_UnsubscribesFromTrafficPackets()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(mobaRuntimeMock, eventBus);

        eventBus.Publish(new Z21TrafficPacketLoggedEvent(new Z21TrafficPacket()));
        Assert.That(viewModel.TrafficPackets, Has.Count.EqualTo(1));

        await viewModel.PrepareForShutdownAsync();

        Assert.That(eventBus.GetSubscriberCount<Z21TrafficPacketLoggedEvent>(), Is.EqualTo(0));

        eventBus.Publish(new Z21TrafficPacketLoggedEvent(new Z21TrafficPacket()));
        Assert.That(viewModel.TrafficPackets, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task PrepareForShutdownAsync_DisconnectsOnlyOnce()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock();
        var viewModel = CreateViewModel(mobaRuntimeMock);

        await viewModel.PrepareForShutdownAsync();
        await viewModel.PrepareForShutdownAsync();

        mobaRuntimeMock.Verify(client => client.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UsageCheckpointEvent_SynchronizesAuthoritativeCountersIntoEditorProject()
    {
        var locomotive = new Locomotive();
        var project = new Project { Locomotives = [locomotive] };
        var runtimeSnapshot = new MobaRuntimeSnapshot
        {
            VehicleUsage = new Dictionary<Guid, VehicleUsageRuntimeSnapshot>
            {
                [locomotive.Id] = new()
                {
                    VehicleId = locomotive.Id,
                    VehicleKind = TrainVehicleKind.Locomotive,
                    TrackedOperatingSeconds = 42,
                    TrackedCompletedTrips = 3
                }
            }
        };
        var mobaRuntimeMock = CreateMobaRuntimeMock();
        mobaRuntimeMock.SetupGet(runtime => runtime.Current).Returns(runtimeSnapshot);
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(mobaRuntimeMock, eventBus);
        viewModel.SelectedProject = new ProjectViewModel(project);

        eventBus.Publish(new VehicleUsageCheckpointCommittedEvent(
            project.Id,
            DateTimeOffset.UtcNow,
            runtimeSnapshot.VehicleUsage));

        Assert.Multiple(() =>
        {
            Assert.That(locomotive.Usage, Is.Not.Null);
            Assert.That(locomotive.Usage!.TrackedOperatingSeconds, Is.EqualTo(42));
            Assert.That(locomotive.Usage.TrackedCompletedTrips, Is.EqualTo(3));
        });
    }

    private static Mock<IMobaRuntime> CreateMobaRuntimeMock()
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaRuntimeMock.Setup(client => client.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.CheckpointUsageAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.SetActiveTrainAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return mobaRuntimeMock;
    }

    private static MainWindowViewModel CreateViewModel(Mock<IMobaRuntime> mobaRuntimeMock, IEventBus? eventBus = null)
    {
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntimeMock.Object,
            eventBus ?? new Mock<IEventBus>().Object,
            uiDispatcherMock.Object,
            new AppSettings(),
            new Solution(),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            loggerMock.Object);
    }
}

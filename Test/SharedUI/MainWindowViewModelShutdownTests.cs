// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Regression tests for MainWindowViewModel shutdown behavior.
/// </summary>
[TestFixture]
internal class MainWindowViewModelShutdownTests
{
    [Test]
    public async Task PrepareForShutdownAsync_UnsubscribesFromRuntimeSnapshots()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock();
        var viewModel = CreateViewModel(mobaRuntimeMock);

        mobaRuntimeMock.Raise(
            client => client.SnapshotChanged += null,
            mobaRuntimeMock.Object,
            new MobaRuntimeSnapshot
            {
                IsConnected = true,
                IsTrackPowerOn = true,
                IsZ21Connecting = false,
                HasSeenSuccessfulConnection = true,
                StatusText = "Connected"
            });

        await viewModel.PrepareForShutdownAsync();

        mobaRuntimeMock.Raise(
            client => client.SnapshotChanged += null,
            mobaRuntimeMock.Object,
            new MobaRuntimeSnapshot
            {
                IsConnected = false,
                IsTrackPowerOn = false,
                IsZ21Connecting = false,
                HasSeenSuccessfulConnection = true,
                StatusText = "Disconnected"
            });

        Assert.That(viewModel.IsConnected, Is.True);
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

    private static Mock<IMobaRuntime> CreateMobaRuntimeMock()
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaRuntimeMock.Setup(client => client.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return mobaRuntimeMock;
    }

    private static MainWindowViewModel CreateViewModel(Mock<IMobaRuntime> mobaRuntimeMock)
    {
        var eventBusMock = new Mock<IEventBus>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntimeMock.Object,
            eventBusMock.Object,
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

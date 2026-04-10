// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;

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
        var mobaClientMock = CreateMobaClientMock();
        var viewModel = CreateViewModel(mobaClientMock);

        mobaClientMock.Raise(
            client => client.SnapshotChanged += null,
            mobaClientMock.Object,
            new MobaRuntimeSnapshot
            {
                IsConnected = true,
                IsTrackPowerOn = true,
                IsZ21Connecting = false,
                HasSeenSuccessfulConnection = true,
                StatusText = "Connected"
            });

        await viewModel.PrepareForShutdownAsync();

        mobaClientMock.Raise(
            client => client.SnapshotChanged += null,
            mobaClientMock.Object,
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
        var mobaClientMock = CreateMobaClientMock();
        var viewModel = CreateViewModel(mobaClientMock);

        await viewModel.PrepareForShutdownAsync();
        await viewModel.PrepareForShutdownAsync();

        mobaClientMock.Verify(client => client.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IMobaClient> CreateMobaClientMock()
    {
        var mobaClientMock = new Mock<IMobaClient>();
        mobaClientMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaClientMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaClientMock.Setup(client => client.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return mobaClientMock;
    }

    private static MainWindowViewModel CreateViewModel(Mock<IMobaClient> mobaClientMock)
    {
        var eventBusMock = new Mock<IEventBus>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaClientMock.Object,
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

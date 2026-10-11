// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Test.Helpers;
using Moba.Backend.Events;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
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
    public async Task PrepareForShutdownAsync_StopsAllProjectRuntimesOnlyOnce()
    {
        var host = new Mock<IProjectRuntimeHost>();
        host.SetupGet(value => value.ConnectedProjectIds).Returns([]);
        var viewModel = CreateViewModel(CreateMobaRuntimeMock(), host: host);

        await viewModel.PrepareForShutdownAsync();
        await viewModel.PrepareForShutdownAsync();

        // An empty project list stops the trains of every runtime and discards them.
        host.Verify(value => value.LoadAsync(
            It.Is<IReadOnlyList<Project>>(projects => projects.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task PrepareForShutdownAsync_WithConnectedRuntimeAndCancel_KeepsEverythingRunning()
    {
        var solution = new Solution { Projects = [new Project { Name = "Station" }] };
        var host = new Mock<IProjectRuntimeHost>();
        host.SetupGet(value => value.ConnectedProjectIds).Returns([solution.Projects[0].Id]);
        var dialog = new Mock<IDialogService>();
        dialog.Setup(value => value.ShowConfirmationAsync(
                "Stop all trains?", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(false);
        var viewModel = CreateViewModel(CreateMobaRuntimeMock(), host: host, dialog: dialog, solution: solution);

        var closed = await viewModel.PrepareForShutdownAsync().ConfigureAwait(false);

        Assert.That(closed, Is.False);
        dialog.Verify(value => value.ShowConfirmationAsync(
            "Stop all trains?",
            It.Is<string>(message => message.Contains("'Station'", StringComparison.Ordinal)),
            "Stop trains", "Cancel", true), Times.Once);
        host.Verify(value => value.LoadAsync(It.IsAny<IReadOnlyList<Project>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task NewSolution_WithConnectedRuntime_AsksBeforeStoppingTheTrains(bool confirm)
    {
        var solution = new Solution { Projects = [new Project { Name = "Station" }] };
        var host = new Mock<IProjectRuntimeHost>();
        host.SetupGet(value => value.ConnectedProjectIds).Returns([solution.Projects[0].Id]);
        var dialog = new Mock<IDialogService>();
        dialog.Setup(value => value.ShowConfirmationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(confirm);
        var viewModel = CreateViewModel(CreateMobaRuntimeMock(), host: host, dialog: dialog, solution: solution);
        host.Invocations.Clear();

        await viewModel.NewSolutionCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(solution.Projects.Single().Name, Is.EqualTo(confirm ? "New Project" : "Station"));
            host.Verify(
                value => value.LoadAsync(It.IsAny<IReadOnlyList<Project>>(), It.IsAny<CancellationToken>()),
                confirm ? Times.Once() : Times.Never());
        }
    }

    private static Mock<IMobaRuntime> CreateMobaRuntimeMock()
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaRuntimeMock.Setup(client => client.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return mobaRuntimeMock;
    }

    private static MainWindowViewModel CreateViewModel(
        Mock<IMobaRuntime> mobaRuntimeMock,
        IEventBus? eventBus = null,
        Mock<IProjectRuntimeHost>? host = null,
        Mock<IDialogService>? dialog = null,
        Solution? solution = null)
    {
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntimeMock.Object,
            mobaRuntimeMock.Object,
            mobaRuntimeMock.Object,
            new LocalRuntimeCommandGateway(mobaRuntimeMock.Object),
            eventBus ?? new Mock<IEventBus>().Object,
            uiDispatcherMock.Object,
            new AppSettings(),
            host is null
                ? TestSolutionSessions.Create(solution ?? new Solution(), uiDispatcherMock.Object, mobaRuntimeMock.Object)
                : new SolutionSession(
                    solution ?? new Solution(),
                    new NullIoService(),
                    uiDispatcherMock.Object,
                    host.Object,
                    NullLogger<SolutionSession>.Instance),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            loggerMock.Object,
            dialogService: dialog?.Object);
    }
}

// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Test.Helpers;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;

/// <summary>
/// Regression tests for MainWindowViewModel shutdown behavior.
/// </summary>
[TestFixture]
internal sealed partial class MainWindowViewModelShutdownTests : IDisposable
{
    private readonly TestRollingStockPages _rollingStockPages = new();

    [TearDown]
    public void Dispose()
    {
        _rollingStockPages.Dispose();
        GC.SuppressFinalize(this);
    }

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
    public async Task PrepareForShutdownAsync_ReleasesAllProjectRuntimesOnlyOnce()
    {
        var host = new Mock<IProjectRuntimeHost>();
        host.SetupGet(value => value.ConnectedProjectIds).Returns([]);
        var viewModel = CreateViewModel(CreateMobaRuntimeMock(), host: host);

        await viewModel.PrepareForShutdownAsync();
        await viewModel.PrepareForShutdownAsync();

        host.Verify(value => value.ReleaseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task PrepareForShutdownAsync_WithConnectedRuntime_ClosesWithoutAskingAndWithoutStoppingTheTrains()
    {
        var solution = new Solution { Projects = [new Project { Name = "Station" }] };
        var host = new Mock<IProjectRuntimeHost>();
        host.SetupGet(value => value.ConnectedProjectIds).Returns((Guid[])[solution.Projects[0].Id]);
        var dialog = new Mock<IDialogService>();
        var viewModel = CreateViewModel(CreateMobaRuntimeMock(), host: host, dialog: dialog, solution: solution);
        host.Invocations.Clear();

        var released = false;

        var closed = await viewModel.PrepareForShutdownAsync(() => released = true).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(closed, Is.True);
            Assert.That(released, Is.True);
        }
        dialog.Verify(value => value.ShowConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        host.Verify(value => value.ReleaseAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Every other way to discard a runtime sets its locomotives to speed 0 first.
        host.Verify(value => value.LoadAsync(It.IsAny<IReadOnlyList<Project>>(), It.IsAny<CancellationToken>()), Times.Never);
        host.Verify(value => value.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        host.Verify(value => value.ReplaceAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PrepareForShutdownAsync_ReleasesRuntimeObserversOnceBeforeReleasingRuntimes()
    {
        var host = new Mock<IProjectRuntimeHost>();
        host.SetupGet(value => value.ConnectedProjectIds).Returns([]);
        var viewModel = CreateViewModel(CreateMobaRuntimeMock(), host: host);
        var steps = new List<string>();
        host.Setup(value => value.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("release runtimes"))
            .Returns(Task.CompletedTask);

        await viewModel.PrepareForShutdownAsync(() => steps.Add("release observers")).ConfigureAwait(false);
        await viewModel.PrepareForShutdownAsync(() => steps.Add("release observers")).ConfigureAwait(false);

        // The disconnect's last packets and snapshots must not reach the counter and traffic monitor.
        Assert.That(steps, Is.EqualTo((string[])["release observers", "release runtimes"]));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task NewSolution_WithConnectedRuntime_AsksBeforeStoppingTheTrains(bool confirm)
    {
        var solution = new Solution { Projects = [new Project { Name = "Station" }] };
        var host = new Mock<IProjectRuntimeHost>();
        host.SetupGet(value => value.ConnectedProjectIds).Returns((Guid[])[solution.Projects[0].Id]);
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

    [Test]
    public async Task PrepareForShutdownAsync_DisposesTheSharedWorkflowLibrary()
    {
        var solution = new Solution { Projects = [new Project { Workflows = [new Workflow { Name = "Departure" }] }] };
        var viewModel = CreateViewModel(CreateMobaRuntimeMock(), solution: solution);
        var library = viewModel.StationsPage.WorkflowLibrary;
        Assert.That(library.SelectedWorkflow, Is.SameAs(viewModel.SelectedProject!.Workflows[0]));

        await viewModel.PrepareForShutdownAsync().ConfigureAwait(false);

        // A disposed library no longer follows the session's project selection.
        viewModel.SelectedProject = new ProjectViewModel(new Project { Workflows = [new Workflow { Name = "Late" }] }, TestViewModelServices.Default);
        Assert.That(library.SelectedWorkflow, Is.Null);
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

    private MainWindowViewModel CreateViewModel(
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

        var locomotiveSession1 = host is null
                ? TestSolutionSessions.Create(solution ?? new Solution(), uiDispatcherMock.Object, mobaRuntimeMock.Object)
                : new SolutionSession(
                    solution ?? new Solution(),
                    new NullSolutionIoService(),
                    uiDispatcherMock.Object,
                    host.Object,
                    NullLogger<SolutionSession>.Instance, TestViewModelServices.Default);
        var stationEventBus1 = eventBus ?? new Mock<IEventBus>().Object;
        var settingsPage1 = TestSettingsPages.Create(new AppSettings(), uiDispatcherMock.Object);
        var trainsPage1 = _rollingStockPages.GetTrainsPage(locomotiveSession1, mobaRuntimeMock.Object, null);
        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntimeMock.Object,
            mobaRuntimeMock.Object,
            new LocalRuntimeCommandGateway(mobaRuntimeMock.Object),
            stationEventBus1,
            settingsPage1,
            trainsPage1,
            _rollingStockPages.GetStationsPage(locomotiveSession1, mobaRuntimeMock.Object, stationEventBus1, dialog?.Object),
            _rollingStockPages.GetJourneysPage(locomotiveSession1, new LocalRuntimeCommandGateway(mobaRuntimeMock.Object), uiDispatcherMock.Object),
            _rollingStockPages.GetProjectDiagnostics(locomotiveSession1, trainsPage1),
            _rollingStockPages.GetSystemStatus(stationEventBus1, locomotiveSession1, settingsPage1),
            locomotiveSession1,
            loggerMock.Object,
            dialogService: dialog?.Object);
    }
}

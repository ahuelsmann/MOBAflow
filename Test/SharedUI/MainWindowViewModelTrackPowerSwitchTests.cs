// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;

/// <summary>
/// The status bar's track power switch follows the runtime snapshots and forwards operator input through the shell.
/// </summary>
[TestFixture]
internal sealed partial class MainWindowViewModelTrackPowerSwitchTests : IDisposable
{
    private readonly TestRollingStockPages _rollingStockPages = new();

    [TearDown]
    public void Dispose()
    {
        _rollingStockPages.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ASwitchPositionThatDiffersFromTheRuntimeStateSendsTheTrackPowerCommand(bool trackPowerOn)
    {
        var runtime = CreateRuntimeMock();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(runtime, eventBus);
        eventBus.Publish(new RuntimeSnapshotChangedEvent(CreateSnapshot(isConnected: true, trackPowerOn)));

        viewModel.ApplyTrackPowerSwitchCommand.Execute(!trackPowerOn);

        runtime.Verify(value => value.SetTrackPowerAsync(!trackPowerOn, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ASwitchPositionThatMatchesTheRuntimeStateIsSynchronizationAndSendsNothing(bool trackPowerOn)
    {
        var runtime = CreateRuntimeMock();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(runtime, eventBus);
        eventBus.Publish(new RuntimeSnapshotChangedEvent(CreateSnapshot(isConnected: true, trackPowerOn)));

        viewModel.ApplyTrackPowerSwitchCommand.Execute(trackPowerOn);

        runtime.Verify(value => value.SetTrackPowerAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ASwitchPositionSendsNothingWhileOperationalControlIsDisabled()
    {
        var runtime = CreateRuntimeMock();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(runtime, eventBus);
        eventBus.Publish(new RuntimeSnapshotChangedEvent(CreateSnapshot(isConnected: false, trackPowerOn: false)));

        viewModel.ApplyTrackPowerSwitchCommand.Execute(true);

        Assert.That(viewModel.IsOperationalControlEnabled, Is.False);
        runtime.Verify(value => value.SetTrackPowerAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static MobaRuntimeSnapshot CreateSnapshot(bool isConnected, bool trackPowerOn) => new()
    {
        IsConnected = isConnected,
        IsTrackPowerOn = trackPowerOn,
        IsZ21Connecting = false,
        HasSeenSuccessfulConnection = isConnected,
        StatusText = isConnected ? "Connected" : "Disconnected"
    };

    private static Mock<IMobaRuntime> CreateRuntimeMock()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(value => value.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        runtime.Setup(value => value.SetTrackPowerAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return runtime;
    }

    private MainWindowViewModel CreateViewModel(Mock<IMobaRuntime> runtime, EventBus eventBus)
    {
        var dispatcher = ImmediateUiDispatcher.Instance;
        var session = TestSolutionSessions.Create(new Solution(), dispatcher, runtime.Object);
        var settingsPage = TestSettingsPages.Create(new AppSettings(), dispatcher);
        var trainsPage = _rollingStockPages.GetTrainsPage(session, runtime.Object, null);
        var gateway = new LocalRuntimeCommandGateway(runtime.Object);
        return new MainWindowViewModel(
            runtime.Object,
            runtime.Object,
            gateway,
            eventBus,
            settingsPage,
            trainsPage,
            _rollingStockPages.GetStationsPage(session, runtime.Object, eventBus),
            _rollingStockPages.GetJourneysPage(session, gateway, dispatcher),
            _rollingStockPages.GetProjectDiagnostics(session, trainsPage),
            _rollingStockPages.GetSystemStatus(eventBus, session, settingsPage),
            session,
            NullLogger<MainWindowViewModel>.Instance,
            new NullDialogService());
    }
}

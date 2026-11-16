// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Events;
using Moba.Contracts.Runtime;

using Moq;

/// <summary>
/// Z21 operator commands of MOBAsmart: connecting needs the project's Z21, failures are shown, and switching track
/// power off hides telemetry until the Z21 confirms it instead of showing stale currents.
/// </summary>
internal sealed partial class MauiViewModelInitializationTests
{
    [TestCase("")]
    [TestCase("   ")]
    public async Task Connect_WithoutAZ21ForTheProject_ExplainsAndDoesNotConnect(string address)
    {
        var dependencies = CreateDependencies();
        var viewModel = CreateViewModel(dependencies);
        viewModel.Z21IpAddress = address;

        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Z21ConnectionStatus, Is.EqualTo("No Z21 assigned to the selected project"));
            dependencies.MobaRuntimeMock.Verify(runtime => runtime.ConnectAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public async Task Connect_WithTheProjectsZ21_ConnectsTheRuntime()
    {
        var dependencies = CreateDependencies();
        var viewModel = CreateViewModel(dependencies);
        viewModel.Z21IpAddress = "192.168.0.111";

        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Z21ConnectionStatus, Is.EqualTo("Connecting..."));
            dependencies.MobaRuntimeMock.Verify(runtime => runtime.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task Connect_WhenTheRuntimeFails_ShowsTheReason()
    {
        var dependencies = CreateDependencies();
        dependencies.MobaRuntimeMock.Setup(runtime => runtime.ConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Z21 unreachable"));
        var viewModel = CreateViewModel(dependencies);
        viewModel.Z21IpAddress = "192.168.0.111";

        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.That(viewModel.Z21ConnectionStatus, Is.EqualTo("Connection failed: Z21 unreachable"));
    }

    [Test]
    public async Task Disconnect_DisconnectsTheRuntimeAndClearsTheStatus()
    {
        var dependencies = CreateDependencies();
        var viewModel = CreateViewModel(dependencies);
        viewModel.Z21IpAddress = "192.168.0.111";
        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        await viewModel.DisconnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Z21ConnectionStatus, Is.Null);
            dependencies.MobaRuntimeMock.Verify(runtime => runtime.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SetTrackPower_SendsTheRequestedState(bool turnOn)
    {
        var dependencies = CreateDependencies();
        var viewModel = CreateViewModel(dependencies);

        await viewModel.SetTrackPowerCommand.ExecuteAsync(turnOn).ConfigureAwait(false);

        dependencies.MobaRuntimeMock.Verify(runtime => runtime.SetTrackPowerAsync(turnOn, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TrackPowerOff_HidesTelemetryUntilTheZ21ConfirmsIt()
    {
        var dependencies = CreateDependencies();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(dependencies, eventBus);
        eventBus.Publish(new RuntimeSnapshotChangedEvent(PoweredSnapshot(mainCurrent: 500)));
        var shownWhilePowered = viewModel.MainCurrent;

        await viewModel.SetTrackPowerCommand.ExecuteAsync(false).ConfigureAwait(false);
        var clearedOnRequest = (viewModel.MainCurrent, viewModel.Temperature, viewModel.SupplyVoltage, viewModel.VccVoltage);
        eventBus.Publish(new RuntimeSnapshotChangedEvent(PoweredSnapshot(mainCurrent: 480)));
        var hiddenBeforeConfirmation = viewModel.MainCurrent;
        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot { IsConnected = true, IsTrackPowerOn = false }));
        eventBus.Publish(new RuntimeSnapshotChangedEvent(PoweredSnapshot(mainCurrent: 300)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(shownWhilePowered, Is.EqualTo(500));
            Assert.That(clearedOnRequest, Is.EqualTo((0, 0, 0, 0)));
            Assert.That(hiddenBeforeConfirmation, Is.Zero);
            Assert.That(viewModel.MainCurrent, Is.EqualTo(300));
            Assert.That(viewModel.Temperature, Is.EqualTo(35));
        }
    }

    [Test]
    public async Task TrackPowerOn_ShowsTelemetryAgainImmediately()
    {
        var dependencies = CreateDependencies();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(dependencies, eventBus);
        await viewModel.SetTrackPowerCommand.ExecuteAsync(false).ConfigureAwait(false);

        await viewModel.SetTrackPowerCommand.ExecuteAsync(true).ConfigureAwait(false);
        eventBus.Publish(new RuntimeSnapshotChangedEvent(PoweredSnapshot(mainCurrent: 250)));

        Assert.That(viewModel.MainCurrent, Is.EqualTo(250));
    }

    private static MobaRuntimeSnapshot PoweredSnapshot(int mainCurrent) => new()
    {
        IsConnected = true,
        IsTrackPowerOn = true,
        MainCurrent = mainCurrent,
        Temperature = 35,
        SupplyVoltage = 18000,
        VccVoltage = 17000
    };
}

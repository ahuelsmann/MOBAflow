// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
//
// Manual Android profiling checklist (device/emulator):
// 1. Cold load: Counter tab -> Control tab, measure time until slider responds.
// 2. Slider: drag 0->126, confirm no visible frame drops and drive commands debounce after release.
// 3. Z21 connected: switch away from Control tab, confirm no UI jank every 5s on Counter tab.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.TrainControl;
using Moba.SharedUI.ViewModel;

using Moq;

[TestFixture]
internal sealed class TrainControlViewModelPerformanceTests
{
    [Test]
    public void PauseUpdates_IgnoresRuntimeSnapshotEvents()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus: eventBus, initialSnapshot: new MobaRuntimeSnapshot { IsConnected = true });

        Assert.That(viewModel.IsConnected, Is.True);

        viewModel.PauseUpdates();
        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot { IsConnected = false }));

        Assert.That(viewModel.IsConnected, Is.True);
    }

    [Test]
    public void ResumeUpdates_AppliesCurrentSnapshotAfterPause()
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupSequence(runtime => runtime.Current)
            .Returns(new MobaRuntimeSnapshot { IsConnected = true })
            .Returns(new MobaRuntimeSnapshot { IsConnected = false });

        var viewModel = CreateViewModel(runtimeMock.Object);

        viewModel.PauseUpdates();
        viewModel.ResumeUpdates();

        Assert.That(viewModel.IsConnected, Is.False);
    }

    [Test]
    public void SetFunctionState_DoesNotRaisePropertyChanged_WhenStateUnchanged()
    {
        var viewModel = CreateViewModel();
        var function = viewModel.Functions[0];
        var changeCount = 0;
        function.PropertyChanged += (_, _) => changeCount++;

        viewModel.Functions[0].IsOn = false;
        changeCount = 0;

        viewModel.Functions[0].IsOn = false;

        Assert.That(changeCount, Is.Zero);
    }

    [Test]
    public async Task SpeedChange_DebouncesZ21DriveCommands()
    {
        var runtimeMock = CreateConnectedRuntimeMock();
        var viewModel = CreateViewModel(runtimeMock.Object);

        viewModel.Speed = 10;
        viewModel.Speed = 20;
        viewModel.Speed = 30;

        await Task.Delay(150);

        runtimeMock.Verify(
            runtime => runtime.SetLocomotiveDriveAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task EmergencyStop_AfterSelectionChange_UsesCurrentTargetAndPreservesFunctions(bool forward)
    {
        var runtime = CreateConnectedRuntimeMock();
        var moving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Setup(service => service.SetLocomotiveDriveAsync(
                42, 30, forward, It.IsAny<CancellationToken>()))
            .Callback(() => moving.TrySetResult())
            .Returns(Task.CompletedTask);
        using var viewModel = CreateViewModel(runtime.Object);
        var locomotiveA = new LocomotiveViewModel(new Locomotive { Name = "Harbor Pilot A", DigitalAddress = 7 });
        var locomotiveB = new LocomotiveViewModel(new Locomotive { Name = "Harbor Pilot B", DigitalAddress = 42 });
        viewModel.SelectedLocomotiveFromProject = locomotiveA;
        viewModel.SelectedLocomotiveFromProject = locomotiveB;
        viewModel.IsForward = forward;
        viewModel.Functions[0].IsOn = true;
        viewModel.Functions[31].IsOn = true;
        var functionStates = viewModel.Functions.Select(function => function.IsOn).ToArray();
        viewModel.Speed = 30;
        await moving.Task.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        runtime.Invocations.Clear();

        await viewModel.EmergencyStopCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.LocomotiveTitle, Is.EqualTo(locomotiveB.Name));
            Assert.That(viewModel.LocoAddress, Is.EqualTo(42));
            Assert.That(viewModel.Speed, Is.Zero);
            Assert.That(viewModel.IsForward, Is.EqualTo(forward));
            Assert.That(viewModel.Functions.Select(function => function.IsOn), Is.EqualTo(functionStates));
        }
        // This verifies command target/state, not exactly-once transport: the existing speed debounce is unchanged.
        runtime.Verify(service => service.SetLocomotiveDriveAsync(42, 0, forward, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        runtime.Verify(service => service.SetLocomotiveDriveAsync(7, It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
        runtime.Verify(service => service.SetLocomotiveFunctionAsync(It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(0)]
    [TestCase(10000)]
    public void EmergencyStop_InvalidAddress_IsUnavailable(int address)
    {
        using var viewModel = CreateViewModel();
        viewModel.LocoAddress = address;

        Assert.That(viewModel.EmergencyStopCommand.CanExecute(null), Is.False);
    }

    private static TrainControlViewModel CreateViewModel(
        IMobaRuntime? mobaRuntime = null,
        IEventBus? eventBus = null,
        MobaRuntimeSnapshot? initialSnapshot = null)
    {
        var runtime = mobaRuntime ?? CreateConnectedRuntimeMock(initialSnapshot).Object;
        return new TrainControlViewModel(
            runtime,
            new LocalRuntimeCommandGateway(runtime),
            CreateSettingsServiceMock().Object,
            eventBus: eventBus ?? new EventBus(NullLogger<EventBus>.Instance));
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock()
    {
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        return settingsServiceMock;
    }

    private static Mock<IMobaRuntime> CreateConnectedRuntimeMock(MobaRuntimeSnapshot? snapshot = null)
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current)
            .Returns(snapshot ?? new MobaRuntimeSnapshot { IsConnected = true });
        runtimeMock.Setup(runtime => runtime.RequestLocomotiveInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        runtimeMock.Setup(runtime => runtime.SetAllLocomotiveFunctionsOffAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        runtimeMock.Setup(runtime => runtime.SetLocomotiveDriveAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return runtimeMock;
    }
}

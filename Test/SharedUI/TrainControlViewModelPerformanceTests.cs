// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
//
// Manual Android profiling checklist (device/emulator):
// 1. Cold load: Counter tab -> Control tab, measure time until slider responds.
// 2. Slider: drag 0->126, confirm no visible frame drops and settings save only after release.
// 3. Z21 connected: switch away from Control tab, confirm no UI jank every 5s on Counter tab.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.SharedUI.Interface;
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
    public async Task SpeedChange_DebouncesSettingsPersistence()
    {
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);

        var runtimeMock = CreateConnectedRuntimeMock();
        var viewModel = new TrainControlViewModel(
            runtimeMock.Object,
            settingsServiceMock.Object,
            eventBus: new EventBus(NullLogger<EventBus>.Instance));

        await Task.Delay(600);
        settingsServiceMock.Invocations.Clear();

        viewModel.Speed = 10;
        viewModel.Speed = 20;
        viewModel.Speed = 30;

        await Task.Delay(500);

        settingsServiceMock.Verify(
            service => service.SaveSettingsAsync(It.IsAny<AppSettings>()),
            Times.Once);
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

    private static TrainControlViewModel CreateViewModel(
        IMobaRuntime? mobaRuntime = null,
        IEventBus? eventBus = null,
        MobaRuntimeSnapshot? initialSnapshot = null)
    {
        return new TrainControlViewModel(
            mobaRuntime ?? CreateConnectedRuntimeMock(initialSnapshot).Object,
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
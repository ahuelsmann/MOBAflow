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
/// Characterization tests for high-risk SharedUI ViewModel behavior.
/// </summary>
[TestFixture]
internal class ViewModelCharacterizationTests
{
    [Test]
    public void TrainControlViewModel_Constructor_ProjectsConnectionStateFromCurrentSnapshot()
    {
        var mobaClientMock = CreateMobaClientMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();

        var viewModel = new TrainControlViewModel(mobaClientMock.Object, settingsServiceMock.Object);

        Assert.That(viewModel.IsConnected, Is.True);
    }

    [Test]
    public async Task TrainControlViewModel_SpeedChange_SendsDriveCommand()
    {
        var mobaClientMock = CreateMobaClientMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaClientMock.Object, settingsServiceMock.Object);

        viewModel.Speed = 12;
        await Task.Delay(100);

        mobaClientMock.Verify(client => client.SetLocomotiveDriveAsync(3, 12, true, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task TrainControlViewModel_ToggleFunctionAsync_UpdatesStateAndCallsRuntime()
    {
        var mobaClientMock = CreateMobaClientMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaClientMock.Object, settingsServiceMock.Object);

        await viewModel.ToggleFunctionAsync(1);

        Assert.That(viewModel.IsF1On, Is.True);
        mobaClientMock.Verify(client => client.SetLocomotiveFunctionAsync(3, 1, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void MainWindowViewModel_AutoStartWebApp_SetterPersistsSettings()
    {
        var mobaClientMock = new Mock<IMobaClient>();
        mobaClientMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaClientMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());

        var settings = new AppSettings();
        var settingsServiceMock = CreateSettingsServiceMock(settings);

        var viewModel = CreateMainWindowViewModel(mobaClientMock.Object, settingsServiceMock.Object, settings);
        viewModel.AutoStartWebApp = false;

        Assert.That(settings.Application.AutoStartWebApp, Is.False);
        settingsServiceMock.Verify(service => service.SaveSettingsAsync(settings), Times.AtLeastOnce);
    }

    private static Mock<IMobaClient> CreateMobaClientMock(MobaRuntimeSnapshot snapshot)
    {
        var mobaClientMock = new Mock<IMobaClient>();
        mobaClientMock.SetupGet(client => client.Current).Returns(snapshot);
        mobaClientMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaClientMock.Setup(client => client.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaClientMock.Setup(client => client.SetLocomotiveFunctionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaClientMock.Setup(client => client.RequestLocomotiveInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mobaClientMock;
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock(AppSettings? settings = null)
    {
        var currentSettings = settings ?? new AppSettings();
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(currentSettings);
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.LoadSettingsAsync()).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.ResetToDefaultsAsync()).Returns(Task.CompletedTask);
        return settingsServiceMock;
    }

    private static MainWindowViewModel CreateMainWindowViewModel(
        IMobaClient mobaClient,
        ISettingsService settingsService,
        AppSettings settings)
    {
        var eventBusMock = new Mock<IEventBus>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaClient,
            eventBusMock.Object,
            uiDispatcherMock.Object,
            settings,
            new Solution(),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            loggerMock.Object,
            settingsService: settingsService);
    }
}

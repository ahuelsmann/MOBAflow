// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
//
// Manual Android profiling checklist (device/emulator) — record before/after each optimization phase:
// 1. Cold start: app launch until Counter tab is interactive (stopwatch).
// 2. Tab switch: Counter -> Control, time until slider responds.
// 3. Slider: drag 0->126, confirm no visible frame drops.
// 4. Z21 connected: switch away from Control tab, confirm no UI jank every 5s on Counter.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Discovery;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

using Moq;

[TestFixture]
internal sealed class MauiViewModelPerformanceTests
{
    private readonly List<MauiViewModel> _createdViewModels = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var viewModel in _createdViewModels)
        {
            viewModel.NotifyApplicationStopping();
        }

        _createdViewModels.Clear();
    }

    [Test]
    public void PauseHeavyUpdates_SkipsSignalBoxRefresh_OnRuntimeSnapshot()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();
        PublishSignalBoxSnapshot(eventBus, elementId);

        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

        viewModel.PauseHeavyUpdates();
        PublishSignalBoxSnapshot(eventBus, elementId, name: "Renamed");

        Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Signal"));
    }

    [Test]
    public void ResumeHeavyUpdates_AppliesPendingSignalBoxSnapshot()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        var elementId = Guid.NewGuid();

        viewModel.PauseHeavyUpdates();
        PublishSignalBoxSnapshot(eventBus, elementId, name: "Signal A");

        viewModel.SetSignalBoxTabActive(true);
        viewModel.ResumeHeavyUpdates();

        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));
        Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Signal A"));
    }

    [Test]
    public void RefreshSignalBoxElements_SameSnapshot_DoesNotReplaceCollectionItems()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, name: "Signal A");
        var firstReference = viewModel.SignalBoxElements[0];

        PublishSignalBoxSnapshot(eventBus, elementId, name: "Signal A");

        Assert.That(ReferenceEquals(firstReference, viewModel.SignalBoxElements[0]), Is.True);
    }

    [Test]
    public void RefreshSignalBoxElements_UpdatesExistingItem_WhenAspectChanges()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Hp0);
        var firstReference = viewModel.SignalBoxElements[0];

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);

        Assert.Multiple(() =>
        {
            Assert.That(ReferenceEquals(firstReference, viewModel.SignalBoxElements[0]), Is.True);
            Assert.That(viewModel.SignalBoxElements[0].SelectedSignalAspect, Is.EqualTo(SignalAspect.Ks1));
        });
    }

    [Test]
    public void ResumeHeavyUpdates_AppliesCachedRemoteSignalBox_AfterPauseHeavyUpdates()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var viewModel = CreateViewModel(eventBus, runtimeHubRemoteClient: hubMock.Object);
        var elementId = Guid.NewGuid();

        viewModel.PauseHeavyUpdates();
        PublishRemoteSignalBoxSnapshot(eventBus, elementId, name: "Remote Signal");
        viewModel.SetSignalBoxTabActive(true);
        viewModel.ResumeHeavyUpdates();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Remote Signal"));
        });
    }

    [Test]
    public void RefreshSignalBoxElements_KeepsPendingAspect_WhenStaleSnapshotArrives()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var gatewayMock = new Mock<IRuntimeCommandGateway>();
        gatewayMock
            .Setup(gateway => gateway.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = CreateViewModel(eventBus, runtimeCommandGateway: gatewayMock.Object);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);
        viewModel.SignalBoxElements[0].SelectSignalAspectCommand.Execute(SignalAspect.Ks2);

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);

        Assert.That(viewModel.SignalBoxElements[0].SelectedSignalAspect, Is.EqualTo(SignalAspect.Ks2));
    }

    [Test]
    public void RefreshSignalBoxElements_ClearsPendingAspect_WhenRemoteConfirmsChange()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var gatewayMock = new Mock<IRuntimeCommandGateway>();
        gatewayMock
            .Setup(gateway => gateway.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = CreateViewModel(eventBus, runtimeCommandGateway: gatewayMock.Object);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);
        viewModel.SignalBoxElements[0].SelectSignalAspectCommand.Execute(SignalAspect.Ks2);
        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks2);

        Assert.That(viewModel.SignalBoxElements[0].SelectedSignalAspect, Is.EqualTo(SignalAspect.Ks2));
    }

    [Test]
    public void RemoteSnapshot_AppliesMobaflowAspect_WhenPendingDiffersAndRemoteSessionActive()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        hubMock.SetupGet(hub => hub.IsConnected).Returns(true);
        hubMock
            .Setup(hub => hub.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var coordinator = new MobileRuntimeCoordinator(new Mock<IMobaRuntime>().Object, hubMock.Object);

        var settings = new AppSettings
        {
            RestApi =
            {
                CurrentIpAddress = "192.168.0.100",
                Port = 5001
            }
        };

        var viewModel = CreateViewModel(
            eventBus,
            settings: settings,
            runtimeHubRemoteClient: hubMock.Object,
            runtimeCommandGateway: coordinator,
            mobileRuntimeCoordinator: coordinator);
        coordinator.SetMobaflowSessionActive(true);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishRemoteSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Hp0);
        viewModel.SignalBoxElements[0].SelectSignalAspectCommand.Execute(SignalAspect.Ks2);
        PublishRemoteSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);

        Assert.That(viewModel.SignalBoxElements[0].SelectedSignalAspect, Is.EqualTo(SignalAspect.Ks1));
    }

    [Test]
    public void RemoteSnapshot_UpdatesExistingItem_WhenAspectChanges()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var viewModel = CreateViewModel(eventBus, runtimeHubRemoteClient: hubMock.Object);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishRemoteSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Hp0);
        PublishRemoteSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);

        Assert.That(viewModel.SignalBoxElements[0].SelectedSignalAspect, Is.EqualTo(SignalAspect.Ks1));
    }

    [Test]
    public void SelectSignalAspect_SendsHp0_WhenChangingFromKs1()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var gatewayMock = new Mock<IRuntimeCommandGateway>();
        gatewayMock
            .Setup(gateway => gateway.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = CreateViewModel(eventBus, runtimeCommandGateway: gatewayMock.Object);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);
        viewModel.SignalBoxElements[0].SelectSignalAspectCommand.Execute(SignalAspect.Hp0);

        gatewayMock.Verify(
            gateway => gateway.SetSignalAspectAsync(elementId, SignalAspect.Hp0, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.That(viewModel.SignalBoxElements[0].SelectedSignalAspect, Is.EqualTo(SignalAspect.Hp0));
    }

    [Test]
    public void SelectSignalAspect_SendsHp0_WhenAspectAlreadyHp0()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var gatewayMock = new Mock<IRuntimeCommandGateway>();
        gatewayMock
            .Setup(gateway => gateway.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = CreateViewModel(eventBus, runtimeCommandGateway: gatewayMock.Object);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Hp0);
        viewModel.SignalBoxElements[0].SelectSignalAspectCommand.Execute(SignalAspect.Hp0);

        gatewayMock.Verify(
            gateway => gateway.SetSignalAspectAsync(elementId, SignalAspect.Hp0, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void RefreshSignalBoxElements_KeepsList_WhenEmptySnapshotArrives()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, name: "Signal A");
        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

        PublishSignalBoxSnapshot(eventBus, elementId, name: "Signal A", aspect: SignalAspect.Hp0, includeElements: false);

        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));
        Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Signal A"));
    }

    [Test]
    public void ResumeHeavyUpdates_KeepsRemoteSignalBox_WhenPendingSnapshotWasEmpty()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var viewModel = CreateViewModel(eventBus, runtimeHubRemoteClient: hubMock.Object);
        var elementId = Guid.NewGuid();

        viewModel.SetSignalBoxTabActive(true);
        PublishRemoteSignalBoxSnapshot(eventBus, elementId, name: "Remote Signal");
        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

        viewModel.PauseHeavyUpdates();
        viewModel.SetSignalBoxTabActive(false);
        PublishSignalBoxSnapshot(eventBus, elementId, includeElements: false);

        viewModel.SetSignalBoxTabActive(true);
        viewModel.ResumeHeavyUpdates();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Remote Signal"));
        });
    }

    [Test]
    public void RemoteSnapshot_WithEmptySignalBoxElements_DoesNotClearExistingList()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var viewModel = CreateViewModel(eventBus, runtimeHubRemoteClient: hubMock.Object);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishRemoteSignalBoxSnapshot(eventBus, elementId, name: "Remote Signal");
        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            SignalBoxElements = []
        }));

        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));
        Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Remote Signal"));
    }

    [Test]
    public void RefreshSignalBoxElements_KeepsList_WhenEmptyLocalSnapshotArrivesWithMobaflowEnabled()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var viewModel = CreateViewModel(eventBus, runtimeHubRemoteClient: hubMock.Object);
        viewModel.IsMobaflowConnectionEnabled = true;
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishRemoteSignalBoxSnapshot(eventBus, elementId, name: "Remote Signal");
        PublishSignalBoxSnapshot(eventBus, elementId, includeElements: false);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Remote Signal"));
        });
    }

    [Test]
    public async Task SetSignalBoxTabActive_WhilePaused_RequestsSnapshotWhenEmpty()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var snapshotRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        hubMock.SetupGet(client => client.IsConnected).Returns(true);
        hubMock
            .Setup(client => client.RequestLatestSnapshotAsync(It.IsAny<CancellationToken>()))
            .Callback(() => snapshotRequested.TrySetResult())
            .Returns(Task.CompletedTask);

        var settings = new AppSettings
        {
            RestApi = { CurrentIpAddress = "192.168.0.79", Port = 5001, IsConnectionEnabled = true }
        };
        var viewModel = CreateViewModel(eventBus, settings, hubMock.Object);
        viewModel.RestApiIpAddress = settings.RestApi.CurrentIpAddress;
        viewModel.RestApiPort = settings.RestApi.Port;
        viewModel.IsRestApiReachable = true;
        viewModel.IsMobaflowConnectionEnabled = true;

        viewModel.PauseHeavyUpdates();
        viewModel.SetSignalBoxTabActive(true);

        await snapshotRequested.Task.WaitAsync(TimeSpan.FromSeconds(2));

        hubMock.Verify(
            client => client.RequestLatestSnapshotAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    private static void PublishRemoteSignalBoxSnapshot(
        EventBus eventBus,
        Guid elementId,
        string name = "Signal",
        SignalAspect aspect = SignalAspect.Hp0)
    {
        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = elementId,
                    Name = name,
                    Kind = SignalBoxElementKind.Signal,
                    X = 1,
                    Y = 2,
                    SignalAspect = aspect
                }
            ]
        }));
    }

    private static void PublishSignalBoxSnapshot(
        EventBus eventBus,
        Guid elementId,
        string name = "Signal",
        SignalAspect aspect = SignalAspect.Hp0,
        bool includeElements = true)
    {
        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            SignalBoxElements = includeElements
                ?
                [
                    new SignalBoxElementRuntimeSnapshot
                    {
                        ElementId = elementId,
                        Name = name,
                        Kind = SignalBoxElementKind.Signal,
                        X = 1,
                        Y = 2,
                        SignalAspect = aspect
                    }
                ]
                : []
        }));
    }

    private MauiViewModel CreateViewModel(
        IEventBus eventBus,
        AppSettings? settings = null,
        IRuntimeHubRemoteClient? runtimeHubRemoteClient = null,
        IRuntimeCommandGateway? runtimeCommandGateway = null,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null)
    {
        var appSettings = settings ?? new AppSettings();
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);

        var uiDispatcherMock = new Mock<IUiDispatcher>();
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUiLowPriority(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUiAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(asyncAction => asyncAction());

        var photoUploadMock = new Mock<IPhotoUploadService>();
        photoUploadMock
            .Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        var viewModel = new MauiViewModel(
            mobaRuntimeMock.Object,
            uiDispatcherMock.Object,
            appSettings,
            CreateSettingsServiceMock(appSettings).Object,
            new Mock<IRestDiscoveryService>().Object,
            new Mock<IZ21DiscoveryService>().Object,
            photoUploadMock.Object,
            new Mock<IPhotoCaptureService>().Object,
            new Mock<INetworkProfileChangeNotifier>().Object,
            NullLogger<MauiViewModel>.Instance,
            eventBus,
            runtimeHubRemoteClient: runtimeHubRemoteClient,
            runtimeCommandGateway: runtimeCommandGateway,
            mobileRuntimeCoordinator: mobileRuntimeCoordinator);

        _createdViewModels.Add(viewModel);
        viewModel.NotifySignalBoxPageLoaded();
        return viewModel;
    }

    private MauiViewModel CreateViewModel(IEventBus eventBus)
    {
        return CreateViewModel(eventBus, settings: null, runtimeHubRemoteClient: null);
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock(AppSettings? settings = null)
    {
        var appSettings = settings ?? new AppSettings();
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(appSettings);
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        return settingsServiceMock;
    }
}
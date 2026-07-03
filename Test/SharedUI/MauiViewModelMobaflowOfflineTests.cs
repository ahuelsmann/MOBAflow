// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

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

namespace Moba.Test.SharedUI;

[TestFixture]

internal sealed class MauiViewModelMobaflowOfflineTests

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

    public async Task DisablingMobaflowConnection_KeepsSignalBoxElements()

    {

        var eventBus = new EventBus(NullLogger<EventBus>.Instance);

        var hubMock = new Mock<IRuntimeHubRemoteClient>();

        var viewModel = CreateViewModel(eventBus, hubMock.Object);

        viewModel.SetSignalBoxTabActive(true);

        var elementId = Guid.NewGuid();

        PublishRemoteSignalBoxSnapshot(eventBus, elementId, "Remote Signal");

        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

        viewModel.IsMobaflowConnectionEnabled = false;

        await Task.Delay(200);

        Assert.Multiple(() =>

        {

            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Remote Signal"));

        });

    }

    [Test]
    public async Task MobaflowSessionEnd_ActivatesCachedProject()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtimeMock
            .Setup(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var settings = new AppSettings
        {
            RestApi =
            {
                CurrentIpAddress = "192.168.0.100",
                Port = 5001
            }
        };

        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        hubMock.SetupGet(hub => hub.IsConnected).Returns(true);
        hubMock
            .Setup(hub => hub.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var photoUploadMock = new Mock<IPhotoUploadService>();
        photoUploadMock
            .Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        var projectContext = new MobileSolutionContext();
        projectContext.ApplySolution(new Solution
        {
            Name = "Cached",
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives =
                    [
                        new Locomotive
                        {
                            Id = Guid.NewGuid(),
                            Name = "BR 110",
                            DigitalAddress = 7
                        }
                    ]
                }
            ]
        });

        var coordinator = new MobileRuntimeCoordinator(runtimeMock.Object, hubMock.Object);
        coordinator.SetMobaflowSessionActive(true);

        var viewModel = CreateViewModel(
            eventBus,
            runtimeHubRemoteClient: hubMock.Object,
            runtimeMock: runtimeMock,
            projectContext: projectContext,
            mobileRuntimeCoordinator: coordinator,
            settings: settings,
            photoUploadService: photoUploadMock.Object);

        viewModel.IsMobaflowConnectionEnabled = true;
        viewModel.IsRestApiReachable = true;
        viewModel.SetRuntimeHubConnected(true);
        viewModel.SetRemoteZ21Connected(true);
        await Task.Delay(100);

        Assert.That(viewModel.IsMobaflowConnectionEnabled, Is.True);

        viewModel.IsRestApiReachable = false;
        await Task.Delay(200);

        runtimeMock.Verify(
            runtime => runtime.ActivateProjectAsync(
                It.Is<Project>(project => project.Name == "myMOBA"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]

    public void RestoreCachedSignalBoxElements_PopulatesSignalBoxTab()

    {

        var eventBus = new EventBus(NullLogger<EventBus>.Instance);

        var viewModel = CreateViewModel(eventBus);

        viewModel.SetSignalBoxTabActive(true);

        var elementId = Guid.NewGuid();

        viewModel.RestoreCachedSignalBoxElements(

        [

            new SignalBoxElementRuntimeSnapshot

            {

                ElementId = elementId,

                Name = "Cached Signal",

                Kind = SignalBoxElementKind.Signal,

                X = 1,

                Y = 2,

                SignalAspect = SignalAspect.Hp0

            }

        ]);

        viewModel.ApplyRestoredMobileCacheToUi();

        Assert.Multiple(() =>

        {

            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Cached Signal"));

        });

    }

    [Test]

    public void StaleSignalCache_DoesNotReplaceProjectPlan_WhenRemoteSessionActive()

    {

        var signalId = Guid.Parse("3d6c0ace-dde2-4329-95d5-8e474b65828f");

        var projectContext = new MobileSolutionContext();

        projectContext.ApplySolution(new Solution

        {

            Projects =

            [

                new Project

                {

                    Name = "myMOBA",

                    SignalBoxPlan = new SignalBoxPlan

                    {

                        Elements =

                        [

                            new SbSignal

                            {

                                Id = signalId,

                                Name = "G2HBFA1",

                                X = 6,

                                Y = 4,

                                SignalAspect = SignalAspect.Hp0

                            }

                        ]

                    }
                }

            ]

        }, "myMOBA");

        var coordinator = new MobileRuntimeCoordinator(

            new Mock<IMobaRuntime>().Object,

            new Mock<IRuntimeHubRemoteClient>().Object);

        coordinator.SetMobaflowSessionActive(true);

        var eventBus = new EventBus(NullLogger<EventBus>.Instance);

        var viewModel = CreateViewModel(

            eventBus,

            projectContext: projectContext,

            mobileRuntimeCoordinator: coordinator);

        viewModel.SetSignalBoxTabActive(true);

        viewModel.RestoreCachedSignalBoxElements(

        [

            new SignalBoxElementRuntimeSnapshot

            {

                ElementId = Guid.NewGuid(),

                Name = "Cached Signal",

                Kind = SignalBoxElementKind.Signal,

                X = 0,

                Y = 0

            },

            new SignalBoxElementRuntimeSnapshot

            {

                ElementId = Guid.NewGuid(),

                Name = "E2E Signal",

                Kind = SignalBoxElementKind.Signal,

                X = 0,

                Y = 0

            }

        ]);

        viewModel.ApplyRestoredMobileCacheToUi();

        Assert.Multiple(() =>

        {

            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("G2HBFA1"));

            Assert.That(viewModel.SignalBoxElements[0].X, Is.EqualTo(6));

            Assert.That(viewModel.SignalBoxElements[0].Y, Is.EqualTo(4));

        });

    }

    [Test]

    public void ApplyRestoredMobileCacheToUi_PopulatesSignalBoxWithoutActiveTab()

    {

        var eventBus = new EventBus(NullLogger<EventBus>.Instance);

        var viewModel = CreateViewModel(eventBus);

        var elementId = Guid.NewGuid();
        var locomotiveId = Guid.NewGuid();

        viewModel.RestoreCachedMobileSnapshot(new MobileSolutionCacheEntry(

            new Solution

            {

                Name = "Cached",

                Projects =
                [
                    new Project
                    {
                        Name = "myMOBA",
                        Locomotives =
                        [
                            new Locomotive
                            {
                                Id = locomotiveId,
                                Name = "BR 110",
                                DigitalAddress = 7
                            }
                        ]
                    }
                ]

            },

            new SolutionSyncMeta(DateTimeOffset.UtcNow, "Cached", "myMOBA"),

            [

                new SignalBoxElementRuntimeSnapshot

                {

                    ElementId = elementId,

                    Name = "Offline Signal",

                    Kind = SignalBoxElementKind.Signal,

                    X = 2,

                    Y = 3,

                    SignalAspect = SignalAspect.Ks1

                }

            ],

            [

                new LocomotiveFleetSnapshot

                {

                    LocomotiveId = locomotiveId,

                    Name = "BR 110",

                    DigitalAddress = 7

                }

            ]));

        viewModel.ApplyRestoredMobileCacheToUi();

        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(0));

        viewModel.SetSignalBoxTabActive(true);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Offline Signal"));
            Assert.That(viewModel.GetStartupLocomotiveFleet(), Has.Count.EqualTo(1));
        });

    }

    private static void PublishLocalSignalBoxSnapshot(
        EventBus eventBus,
        Guid elementId,
        string name = "Signal",
        SignalAspect aspect = SignalAspect.Hp0)
    {
        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
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

    [Test]
    public void MobaflowHubReachableWithoutRemoteHost_RoutesCommandsToLocalZ21()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtimeMock
            .Setup(runtime => runtime.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(runtimeMock.Object, hubMock.Object);
        var viewModel = CreateViewModel(eventBus, hubMock.Object, runtimeMock: runtimeMock, mobileRuntimeCoordinator: coordinator);

        viewModel.IsMobaflowConnectionEnabled = true;
        viewModel.IsRestApiReachable = true;
        viewModel.SetRuntimeHubConnected(true);
        viewModel.SetRemoteZ21Connected(false);

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            StatusText = "Connected"
        }));

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.PreferRemoteRuntime, Is.False);
            Assert.That(coordinator.CanExecuteCommands, Is.True);
            Assert.That(coordinator.IsLocalZ21Connected, Is.True);
        });
    }

    [Test]
    public async Task MobaflowDisabled_LocalZ21Connected_RoutesSignalAspectToLocalRuntime()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtimeMock
            .Setup(runtime => runtime.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(runtimeMock.Object, hubMock.Object);
        var viewModel = CreateViewModel(
            eventBus,
            hubMock.Object,
            runtimeMock: runtimeMock,
            mobileRuntimeCoordinator: coordinator);

        viewModel.IsMobaflowConnectionEnabled = false;
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishLocalSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            StatusText = "Connected"
        }));

        viewModel.SignalBoxElements[0].SelectSignalAspectCommand.Execute(SignalAspect.Hp0);
        await Task.Delay(100);

        runtimeMock.Verify(
            runtime => runtime.SetSignalAspectAsync(elementId, SignalAspect.Hp0, It.IsAny<CancellationToken>()),
            Times.Once);
        hubMock.Verify(
            client => client.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task MobaflowHubReachableWithoutRemoteHost_RoutesSignalAspectToLocalZ21()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtimeMock
            .Setup(runtime => runtime.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(runtimeMock.Object, hubMock.Object);
        var viewModel = CreateViewModel(
            eventBus,
            hubMock.Object,
            runtimeMock: runtimeMock,
            mobileRuntimeCoordinator: coordinator);

        viewModel.IsMobaflowConnectionEnabled = true;
        viewModel.IsRestApiReachable = true;
        viewModel.SetRuntimeHubConnected(true);
        viewModel.SetRemoteZ21Connected(false);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishLocalSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            StatusText = "Connected"
        }));

        viewModel.SignalBoxElements[0].SelectSignalAspectCommand.Execute(SignalAspect.Ks2);
        await Task.Delay(100);

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.PreferRemoteRuntime, Is.False);
            Assert.That(coordinator.CanExecuteCommands, Is.True);
        });

        runtimeMock.Verify(
            runtime => runtime.SetSignalAspectAsync(elementId, SignalAspect.Ks2, It.IsAny<CancellationToken>()),
            Times.Once);
        hubMock.Verify(
            client => client.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task MobaflowDisabled_LocalZ21Connected_EnablesTrainControlCommands()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtimeMock
            .Setup(runtime => runtime.SetLocomotiveFunctionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(runtimeMock.Object, hubMock.Object);
        var viewModel = CreateViewModel(eventBus, hubMock.Object, runtimeMock: runtimeMock, mobileRuntimeCoordinator: coordinator);

        viewModel.IsMobaflowConnectionEnabled = false;

        var trainControlViewModel = new TrainControlViewModel(
            runtimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            StatusText = "Connected"
        }));

        await trainControlViewModel.ToggleFunctionAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(trainControlViewModel.IsSpeedControlEnabled, Is.True);
            Assert.That(trainControlViewModel.Functions[1].IsOn, Is.True);
        });

        runtimeMock.Verify(
            runtime => runtime.SetLocomotiveFunctionAsync(3, 1, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void MobaflowOperationalSession_PrefersRemoteRuntime()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var coordinator = new MobileRuntimeCoordinator(new Mock<IMobaRuntime>().Object, new Mock<IRuntimeHubRemoteClient>().Object);
        var viewModel = CreateViewModel(eventBus, mobileRuntimeCoordinator: coordinator);

        viewModel.IsMobaflowConnectionEnabled = true;
        viewModel.IsRestApiReachable = true;
        viewModel.SetRuntimeHubConnected(true);
        viewModel.SetRemoteZ21Connected(true);

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.PreferRemoteRuntime, Is.True);
            Assert.That(coordinator.CanExecuteCommands, Is.True);
        });
    }

    private MauiViewModel CreateViewModel(
        EventBus eventBus,
        IRuntimeHubRemoteClient? runtimeHubRemoteClient = null,
        Mock<IMobaRuntime>? runtimeMock = null,
        IProjectContext? projectContext = null,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null,
        IRuntimeCommandGateway? runtimeCommandGateway = null,
        AppSettings? settings = null,
        IRestDiscoveryService? restDiscoveryService = null,
        IPhotoUploadService? photoUploadService = null)
    {
        runtimeMock ??= new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtimeMock.Setup(runtime => runtime.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

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

        var viewModel = new MauiViewModel(
            runtimeMock.Object,
            uiDispatcherMock.Object,
            settings ?? new AppSettings(),
            CreateSettingsServiceMock().Object,
            restDiscoveryService ?? new Mock<IRestDiscoveryService>().Object,
            new Mock<IZ21DiscoveryService>().Object,
            photoUploadService ?? new Mock<IPhotoUploadService>().Object,
            new Mock<IPhotoCaptureService>().Object,
            new Mock<INetworkProfileChangeNotifier>().Object,
            NullLogger<MauiViewModel>.Instance,
            eventBus,
            runtimeHubRemoteClient: runtimeHubRemoteClient,
            runtimeCommandGateway: runtimeCommandGateway ?? mobileRuntimeCoordinator,
            mobileRuntimeCoordinator: mobileRuntimeCoordinator,
            projectContext: projectContext);

        _createdViewModels.Add(viewModel);
        viewModel.NotifySignalBoxPageLoaded();
        return viewModel;
    }

    [Test]
    public async Task InitializeAsync_StartsWithMobaflowConnectionDisabled_WhenNoEndpointEvenIfSettingsEnabled()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var settings = new AppSettings();
        settings.RestApi.IsConnectionEnabled = true;

        var viewModel = CreateViewModel(eventBus, settings: settings);

        await viewModel.InitializeAsync();

        Assert.That(viewModel.IsMobaflowConnectionEnabled, Is.False);
    }

    [Test]
    public async Task InitializeAsync_RestoresStoredSession_WhenEndpointAndEnabled()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var settings = new AppSettings
        {
            RestApi =
            {
                CurrentIpAddress = "192.168.0.42",
                Port = 5001,
                IsConnectionEnabled = true
            }
        };

        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        hubMock.SetupGet(hub => hub.IsConnected).Returns(true);
        hubMock
            .Setup(hub => hub.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var photoUploadMock = new Mock<IPhotoUploadService>();
        photoUploadMock
            .Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        var restDiscoveryMock = new Mock<IRestDiscoveryService>();
        restDiscoveryMock
            .Setup(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        var viewModel = CreateViewModel(
            eventBus,
            runtimeHubRemoteClient: hubMock.Object,
            settings: settings,
            restDiscoveryService: restDiscoveryMock.Object,
            photoUploadService: photoUploadMock.Object);

        await viewModel.InitializeAsync();
        await Task.Delay(300);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsMobaflowConnectionEnabled, Is.True);
            Assert.That(viewModel.IsRestApiReachable, Is.True);
        });

        photoUploadMock.Verify(
            service => service.HealthCheckAsync("192.168.0.42", 5001, It.IsAny<TimeSpan?>()),
            Times.AtLeastOnce);
        hubMock.Verify(
            hub => hub.ConnectAsync(
                "192.168.0.42",
                5001,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()),
            Times.AtLeastOnce);
        restDiscoveryMock.Verify(
            service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task MobaflowConnectionEnabled_WhenUnreachable_KeepsToggleAndRetriesDiscovery()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var settings = new AppSettings
        {
            RestApi =
            {
                CurrentIpAddress = "192.168.0.100",
                Port = 5001
            }
        };

        var restDiscoveryMock = new Mock<IRestDiscoveryService>();
        restDiscoveryMock
            .Setup(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));
        restDiscoveryMock
            .Setup(service => service.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        var photoUploadMock = new Mock<IPhotoUploadService>();
        photoUploadMock
            .Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel(
            eventBus,
            settings: settings,
            restDiscoveryService: restDiscoveryMock.Object,
            photoUploadService: photoUploadMock.Object);

        viewModel.IsMobaflowConnectionEnabled = true;
        await Task.Delay(3500);

        Assert.That(viewModel.IsMobaflowConnectionEnabled, Is.True);
        restDiscoveryMock.Verify(
            service => service.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        photoUploadMock.Verify(
            service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()),
            Times.AtLeast(2));
    }

    [Test]
    public async Task MobaflowConnectionEnabled_WhenReachableButHubFails_KeepsToggleEnabled()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var settings = new AppSettings
        {
            RestApi =
            {
                CurrentIpAddress = "192.168.0.100",
                Port = 5001
            }
        };

        var restDiscoveryMock = new Mock<IRestDiscoveryService>();
        restDiscoveryMock
            .Setup(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        var photoUploadMock = new Mock<IPhotoUploadService>();
        photoUploadMock
            .Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        hubMock.SetupGet(hub => hub.IsConnected).Returns(false);
        hubMock
            .Setup(hub => hub.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("SignalR connect failed"));

        var viewModel = CreateViewModel(
            eventBus,
            runtimeHubRemoteClient: hubMock.Object,
            settings: settings,
            restDiscoveryService: restDiscoveryMock.Object,
            photoUploadService: photoUploadMock.Object);

        viewModel.IsMobaflowConnectionEnabled = true;
        await Task.Delay(3500);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsMobaflowConnectionEnabled, Is.True);
            Assert.That(viewModel.IsRestApiReachable, Is.True);
        });
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock()
    {
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        return settingsServiceMock;
    }
}


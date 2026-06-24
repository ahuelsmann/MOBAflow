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
        await Task.Delay(300);

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



        Assert.Multiple(() =>

        {

            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Cached Signal"));

        });

    }



    [Test]

    public void ApplyRestoredMobileCacheToUi_PopulatesSignalBoxWithoutActiveTab()

    {

        var eventBus = new EventBus(NullLogger<EventBus>.Instance);

        var viewModel = CreateViewModel(eventBus);

        var elementId = Guid.NewGuid();



        viewModel.RestoreCachedMobileSnapshot(new MobileSolutionCacheEntry(

            new Solution

            {

                Name = "Cached",

                Projects = [new Project { Name = "myMOBA" }]

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

                    LocomotiveId = Guid.NewGuid(),

                    Name = "BR 110",

                    DigitalAddress = 7

                }

            ]));



        viewModel.ApplyRestoredMobileCacheToUi();



        Assert.Multiple(() =>

        {

            Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

            Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Offline Signal"));

            Assert.That(viewModel.GetStartupLocomotiveFleet(), Has.Count.EqualTo(1));

        });

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



    private MauiViewModel CreateViewModel(
        EventBus eventBus,
        IRuntimeHubRemoteClient? runtimeHubRemoteClient = null,
        Mock<IMobaRuntime>? runtimeMock = null,
        IProjectContext? projectContext = null,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null,
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
            mobileRuntimeCoordinator: mobileRuntimeCoordinator,
            projectContext: projectContext);

        _createdViewModels.Add(viewModel);
        return viewModel;
    }



    [Test]
    public async Task InitializeAsync_StartsWithMobaflowConnectionDisabled_WhenNotPairedEvenIfSettingsEnabled()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var settings = new AppSettings();
        settings.RestApi.IsConnectionEnabled = true;

        var viewModel = CreateViewModel(eventBus, settings: settings);

        await viewModel.InitializeAsync();

        Assert.That(viewModel.IsMobaflowConnectionEnabled, Is.False);
    }

    [Test]
    public async Task InitializeAsync_RestoresPairedSession_WhenCredentialsAndEnabled()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var settings = new AppSettings
        {
            RestApi =
            {
                CurrentIpAddress = "192.168.0.42",
                Port = 5001,
                ApiKey = "pair-key-123",
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
    public async Task MobaflowConnectionEnabled_WhenUnreachable_DisablesToggleAfterSingleAttempt()
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
        await Task.Delay(500);

        Assert.That(viewModel.IsMobaflowConnectionEnabled, Is.False);
        restDiscoveryMock.Verify(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        restDiscoveryMock.Verify(service => service.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock()
    {
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        return settingsServiceMock;
    }
}


// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.Test.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service.Recording;
using Moba.Common.Recording;
using Moba.Common.Configuration;
using Moba.Common.Events;

using Moba.Common.Runtime;

using Moba.Domain;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;

using Moba.SharedUI.Service;

using Moba.SharedUI.TrainControl;
using Moba.SharedUI.ViewModel;

using Moq;

namespace Moba.Test.SharedUI;

[TestFixture]

internal sealed class MauiViewModelMobaflowOfflineTests

{

    private readonly List<MauiViewModel> _createdViewModels = [];
    private static readonly string[] ProjectSwitchCalls = ["disconnect", "activate", "connect"];

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

        var projectContext = CreateProjectContext(new Solution
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

        var projectContext = CreateProjectContext(new Solution

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
            coordinator,
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

    [TestCase(false)]
    [TestCase(true)]
    public async Task MobileSignalAspect_RecordsCommandWhilePreservingLocalOrRemoteRouting(bool remoteSession)
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = new Mock<IMobaRuntime>();
        runtime.Setup(value => value.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var hub = new Mock<IRuntimeHubRemoteClient>();
        hub.Setup(value => value.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var coordinator = new MobileRuntimeCoordinator(runtime.Object, hub.Object);
        var session = new RecordingSessionService(TimeProvider.System);
        await using var sessionLifetime = session.ConfigureAwait(false);
        session.Start(new RecordingSessionStartRequest("Mobile signal", "1.0"));
        var gateway = new RecordingRuntimeCommandGateway(coordinator, session);
        var viewModel = CreateViewModel(eventBus, hub.Object, runtimeMock: runtime,
            mobileRuntimeCoordinator: coordinator, runtimeCommandGateway: gateway);
        viewModel.IsMobaflowConnectionEnabled = remoteSession;
        viewModel.IsRestApiReachable = remoteSession;
        viewModel.SetRuntimeHubConnected(remoteSession);
        viewModel.SetRemoteZ21Connected(remoteSession);
        viewModel.SetSignalBoxTabActive(true);
        var signalId = Guid.NewGuid();
        PublishRemoteSignalBoxSnapshot(eventBus, signalId, "Signal");
        if (!remoteSession)
        {
            eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
            {
                IsConnected = true,
                StatusText = "Connected"
            }));
        }

        viewModel.SignalBoxElements[0].SelectSignalAspectCommand.Execute(SignalAspect.Ks2);
        await viewModel.SetSignalBoxAspectCommand.ExecutionTask!.ConfigureAwait(false);
        var entries = (await session.StopAsync().ConfigureAwait(false)).Artifact!.Entries
            .Where(entry => entry.Source == "runtime-command-gateway").ToArray();
        Assert.That(entries, Has.Length.EqualTo(2));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries[0].TypeKey, Is.EqualTo("command.signal-aspect.request"));
            Assert.That(entries[0].Payload.GetProperty("signalId").GetGuid(), Is.EqualTo(signalId));
            Assert.That(entries[1].TypeKey, Is.EqualTo("command.signal-aspect.result"));
        }
        runtime.Verify(value => value.SetSignalAspectAsync(signalId, SignalAspect.Ks2, It.IsAny<CancellationToken>()),
            remoteSession ? Times.Never() : Times.Once());
        hub.Verify(value => value.SetSignalAspectAsync(signalId, SignalAspect.Ks2, It.IsAny<CancellationToken>()),
            remoteSession ? Times.Once() : Times.Never());
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

    /// <summary>The phone's view of a synchronized solution.</summary>
    private static MobileSolutionContext CreateProjectContext(Solution solution, string? activeProjectName = null)
    {
        var context = new MobileSolutionContext();
        context.ApplySolution(solution, activeProjectName);
        return context;
    }

    private MauiViewModel CreateViewModel(
        EventBus eventBus,
        IRuntimeHubRemoteClient? runtimeHubRemoteClient = null,
        Mock<IMobaRuntime>? runtimeMock = null,
        MobileSolutionContext? projectContext = null,
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

        var appSettings = settings ?? new AppSettings();
        var settingsService = CreateSettingsServiceMock().Object;
        var endpointLocator = TestEndpointLocators.Create(appSettings, settingsService, restDiscoveryService);
        var healthService = photoUploadService ?? new Mock<IPhotoUploadService>().Object;
        var viewModel = new MauiViewModel(
            runtimeMock.Object,
            runtimeMock.Object,
            runtimeMock.Object,
            runtimeCommandGateway ?? mobileRuntimeCoordinator ?? (IRuntimeCommandGateway)new LocalRuntimeCommandGateway(runtimeMock.Object),
            uiDispatcherMock.Object,
            appSettings,
            settingsService,
            endpointLocator,
            TestHubSessions.Create(appSettings, endpointLocator, healthService, runtimeHubRemoteClient),
            new MobileRuntimeProjection(runtimeMock.Object, projectContext, mobileRuntimeCoordinator),
            TestSolutionSynchronizers.Create(),
            new MobilePhotoUploader(new Mock<IPhotoCaptureService>().Object, healthService, endpointLocator),
            new Mock<INetworkProfileChangeNotifier>().Object,
            NullLogger<MauiViewModel>.Instance,
            eventBus,
            runtimeHubRemoteClient: runtimeHubRemoteClient,
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
    public async Task RemoteControl_FollowsTheSelectedProject()
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
        var photoUploadMock = new Mock<IPhotoUploadService>();
        photoUploadMock
            .Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);
        var station = new Project { Name = "Station" };
        var yard = new Project { Name = "Yard" };
        var projectContext = CreateProjectContext(new Solution { Name = "Layout", Projects = [station, yard] });

        var stationSelected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var yardSelected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hubMock.Setup(value => value.SelectProjectAsync(station.Id, It.IsAny<CancellationToken>()))
            .Callback(() => stationSelected.TrySetResult()).Returns(Task.CompletedTask);
        hubMock.Setup(value => value.SelectProjectAsync(yard.Id, It.IsAny<CancellationToken>()))
            .Callback(() => yardSelected.TrySetResult()).Returns(Task.CompletedTask);

        var viewModel = CreateViewModel(
            eventBus,
            runtimeHubRemoteClient: hubMock.Object,
            projectContext: projectContext,
            settings: settings,
            photoUploadService: photoUploadMock.Object);

        await viewModel.InitializeAsync().ConfigureAwait(false);
        await stationSelected.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        hubMock.Verify(hub => hub.SelectProjectAsync(station.Id, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        projectContext.SelectedProject = projectContext.SolutionViewModel!.Projects.Single(project => project.Model.Id == yard.Id);

        await yardSelected.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        hubMock.Verify(hub => hub.SelectProjectAsync(yard.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static readonly string[] StationAndYard = ["Station", "Yard"];

    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    public async Task ProjectPicker_SelectingAnotherProject_ConnectsToItsZ21(
        bool remoteSession, bool sameAddress, bool connecting)
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var connected = false;
        var calls = new List<string>();
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.Setup(runtime => runtime.ConnectAsync(It.IsAny<CancellationToken>()))
            .Callback(() => { connected = true; calls.Add("connect"); })
            .Returns(Task.CompletedTask);
        runtimeMock.Setup(runtime => runtime.DisconnectForProjectSwitchAsync(It.IsAny<CancellationToken>()))
            .Callback(() => { connected = false; calls.Add("disconnect"); })
            .Returns(Task.CompletedTask);
        runtimeMock.Setup(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("activate"))
            .Returns(Task.CompletedTask);
        var station = new Project { Name = "Station", Z21 = { IpAddress = "192.168.0.111" } };
        var yard = new Project
        {
            Name = "Yard",
            Z21 = { IpAddress = sameAddress ? "192.168.0.111" : "192.168.0.112", Port = 21106 }
        };
        var projectContext = CreateProjectContext(new Solution { Name = "Layout", Projects = [station, yard] });
        var hubMock = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(runtimeMock.Object, hubMock.Object);
        coordinator.SetMobaflowSessionActive(remoteSession);
        var viewModel = CreateViewModel(eventBus, runtimeMock: runtimeMock,
            projectContext: projectContext, mobileRuntimeCoordinator: coordinator);
        // Connected to the Z21 of the project selected first.
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(() => new MobaRuntimeSnapshot
        {
            IsConnected = connected, IsZ21Connecting = connecting
        });
        connected = !connecting;

        viewModel.SelectedProjectIndex = 1;
        await Task.Delay(200).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ProjectNames, Is.EqualTo(StationAndYard));
            Assert.That(projectContext.SelectedProject?.Model.Id, Is.EqualTo(yard.Id));
            Assert.That(viewModel.Z21IpAddress, Is.EqualTo(yard.Z21.IpAddress));
            Assert.That(calls, Is.EqualTo(ProjectSwitchCalls));
        }

        runtimeMock.Verify(runtime => runtime.ActivateProjectAsync(yard, It.IsAny<CancellationToken>()), Times.Once);
        runtimeMock.Verify(runtime => runtime.DisconnectForProjectSwitchAsync(It.IsAny<CancellationToken>()), Times.Once);
        runtimeMock.Verify(runtime => runtime.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Never);
        runtimeMock.Verify(runtime => runtime.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RapidProjectSelection_SerializesRemoteAndLocalHandoffAndSkipsStaleActivation(bool delayRemote)
    {
        var station = new Project { Name = "Station", Z21 = { IpAddress = "192.168.0.111" } };
        var yard = new Project { Name = "Yard", Z21 = { IpAddress = "192.168.0.112" } };
        var depot = new Project { Name = "Depot", Z21 = { IpAddress = "192.168.0.113" } };
        var context = CreateProjectContext(new Solution { Projects = [station, yard, depot] });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new Mock<IMobaRuntime>();
        var hub = new Mock<IRuntimeHubRemoteClient>();
        hub.Setup(value => value.SelectProjectAsync(yard.Id, It.IsAny<CancellationToken>()))
            .Returns(() => { if (delayRemote) { started.TrySetResult(); return release.Task; } return Task.CompletedTask; });
        var disconnects = 0;
        runtime.Setup(value => value.DisconnectForProjectSwitchAsync(It.IsAny<CancellationToken>()))
            .Returns(() => { if (!delayRemote && ++disconnects == 1) { started.TrySetResult(); return release.Task; } return Task.CompletedTask; });
        runtime.Setup(value => value.ConnectAsync(It.IsAny<CancellationToken>()))
            .Callback(() => connected.TrySetResult()).Returns(Task.CompletedTask);
        var viewModel = CreateViewModel(new EventBus(NullLogger<EventBus>.Instance), hub.Object,
            runtimeMock: runtime, projectContext: context);

        viewModel.SelectedProjectIndex = 1;
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        viewModel.SelectedProjectIndex = 2;
        hub.Verify(value => value.SelectProjectAsync(depot.Id, It.IsAny<CancellationToken>()), Times.Never);
        release.TrySetResult();
        await connected.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SelectedProject?.Model.Id, Is.EqualTo(depot.Id));
            Assert.That(viewModel.Z21IpAddress, Is.EqualTo(depot.Z21.IpAddress));
        }
        runtime.Verify(value => value.ActivateProjectAsync(yard, It.IsAny<CancellationToken>()), Times.Never);
        runtime.Verify(value => value.ActivateProjectAsync(depot, It.IsAny<CancellationToken>()), Times.Once);
        hub.Verify(value => value.SelectProjectAsync(depot.Id, It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(value => value.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SameProjectSync_UpdatesDefinitionsWithoutRestartingItsRuntime(bool changeEndpoint)
    {
        var station = new Project { Name = "Station", Z21 = { IpAddress = "192.168.0.111" } };
        var yard = new Project { Name = "Yard", Z21 = { IpAddress = "192.168.0.112" } };
        var context = CreateProjectContext(new Solution { Projects = [station, yard] });
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var updated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new Mock<IMobaRuntime>();
        runtime.Setup(value => value.ConnectAsync(It.IsAny<CancellationToken>()))
            .Callback(() => connected.TrySetResult()).Returns(Task.CompletedTask);
        runtime.Setup(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((project, _) => { if (project.Journeys.Count == 1) updated.TrySetResult(); })
            .Returns(Task.CompletedTask);
        var coordinator = new MobileRuntimeCoordinator(runtime.Object, Mock.Of<IRuntimeHubRemoteClient>());
        coordinator.SetMobaflowSessionActive(true);
        var viewModel = CreateViewModel(new EventBus(NullLogger<EventBus>.Instance),
            runtimeMock: runtime, projectContext: context, mobileRuntimeCoordinator: coordinator);
        viewModel.SelectedProjectIndex = 1;
        await connected.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var freshYard = new Project
        {
            Id = yard.Id, Name = yard.Name,
            Z21 = { IpAddress = changeEndpoint ? "192.168.0.113" : yard.Z21.IpAddress },
            Journeys = [new Journey { Name = "New definition" }]
        };

        context.ApplySolution(new Solution { Projects = [station, freshYard] });
        await updated.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        runtime.Verify(value => value.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(value => value.UpdateProjectAsync(freshYard, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task ProjectPicker_RemoteRegistrationFailureStillActivatesAndConnectsLocalProject()
    {
        var station = new Project { Z21 = { IpAddress = "192.168.0.111" } };
        var yard = new Project { Z21 = { IpAddress = "192.168.0.112" } };
        var context = CreateProjectContext(new Solution { Projects = [station, yard] });
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new Mock<IMobaRuntime>();
        runtime.Setup(value => value.ConnectAsync(It.IsAny<CancellationToken>()))
            .Callback(() => connected.TrySetResult()).Returns(Task.CompletedTask);
        var hub = new Mock<IRuntimeHubRemoteClient>();
        hub.Setup(value => value.SelectProjectAsync(yard.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Remote registration interrupted"));
        var viewModel = CreateViewModel(new EventBus(NullLogger<EventBus>.Instance), hub.Object,
            runtimeMock: runtime, projectContext: context);

        viewModel.SelectedProjectIndex = 1;
        await connected.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        runtime.Verify(value => value.ActivateProjectAsync(yard, It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(value => value.DisconnectForProjectSwitchAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(viewModel.Z21IpAddress, Is.EqualTo(yard.Z21.IpAddress));
    }

    [Test]
    public void SwitchingProjects_DropsTheLocomotivesOfThePreviousProject()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var station = new Project { Name = "Station" };
        var yard = new Project { Name = "Yard" };
        var projectContext = CreateProjectContext(new Solution { Name = "Layout", Projects = [station, yard] });
        var viewModel = CreateViewModel(eventBus, projectContext: projectContext);
        viewModel.RestoreCachedLocomotiveFleet(
        [
            new LocomotiveFleetSnapshot { LocomotiveId = Guid.NewGuid(), Name = "BR 110 of Station", DigitalAddress = 3 }
        ]);

        projectContext.SelectedProject = projectContext.SolutionViewModel!.Projects.Single(project => project.Model.Id == yard.Id);

        Assert.That(viewModel.GetStartupLocomotiveFleet(), Is.Empty);
    }

    [Test]
    public void SwitchingProjects_WithRemoteSession_DropsCachedAndRuntimeFleetOfThePreviousProject()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var locomotive = new Locomotive { Name = "Station locomotive", DigitalAddress = 3 };
        var station = new Project { Name = "Station", Locomotives = [locomotive] };
        var yard = new Project { Name = "Yard" };
        var context = CreateProjectContext(new Solution { Projects = [station, yard] });
        var runtime = new Mock<IMobaRuntime>();
        var hub = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(runtime.Object, hub.Object);
        coordinator.SetMobaflowSessionActive(true);
        var viewModel = CreateViewModel(eventBus, runtimeMock: runtime,
            projectContext: context, mobileRuntimeCoordinator: coordinator);
        viewModel.SetControlTabActive(true);
        IReadOnlyList<LocomotiveFleetSnapshot> oldFleet =
        [
            new() { LocomotiveId = locomotive.Id, Name = locomotive.Name, DigitalAddress = 3 }
        ];
        runtime.SetupGet(value => value.Current).Returns(new MobaRuntimeSnapshot { LocomotiveFleet = oldFleet });
        viewModel.ApplyLiveRemoteLocomotiveFleet(oldFleet);
        IReadOnlyList<LocomotiveFleetSnapshot>? publishedFleet = null;
        eventBus.Subscribe<LocomotiveFleetUpdatedEvent>(update => publishedFleet = update.Fleet);

        viewModel.SelectedProjectIndex = 1;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.GetStartupLocomotiveFleet(), Is.Empty);
            Assert.That(publishedFleet, Is.Empty);
            Assert.That(coordinator.PreferRemoteRuntime, Is.True);
        }
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


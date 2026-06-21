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



        var coordinator = new MobileRuntimeCoordinator(runtimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);

        coordinator.SetMobaflowSessionActive(true);



        var viewModel = CreateViewModel(

            eventBus,

            runtimeMock: runtimeMock,

            projectContext: projectContext,

            mobileRuntimeCoordinator: coordinator);



        viewModel.IsMobaflowConnectionEnabled = true;

        viewModel.IsRestApiReachable = true;

        viewModel.SetRuntimeHubConnected(true);



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

        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null)

    {

        runtimeMock ??= new Mock<IMobaRuntime>();

        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);



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

            new AppSettings(),

            CreateSettingsServiceMock().Object,

            new Mock<IRestDiscoveryService>().Object,

            new Mock<IZ21DiscoveryService>().Object,

            new Mock<IPhotoUploadService>().Object,

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
    public async Task InitializeAsync_StartsWithMobaflowConnectionDisabled_EvenWhenSettingsEnabled()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var settings = new AppSettings();
        settings.RestApi.IsConnectionEnabled = true;

        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(settings);
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);

        var runtimeMock = new Mock<IMobaRuntime>();
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

        var networkMock = new Mock<INetworkProfileChangeNotifier>();

        var viewModel = new MauiViewModel(
            runtimeMock.Object,
            uiDispatcherMock.Object,
            settings,
            settingsServiceMock.Object,
            new Mock<IRestDiscoveryService>().Object,
            new Mock<IZ21DiscoveryService>().Object,
            new Mock<IPhotoUploadService>().Object,
            new Mock<IPhotoCaptureService>().Object,
            networkMock.Object,
            NullLogger<MauiViewModel>.Instance,
            eventBus);

        _createdViewModels.Add(viewModel);

        await viewModel.InitializeAsync();

        Assert.That(viewModel.IsMobaflowConnectionEnabled, Is.False);
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock()

    {

        var settingsServiceMock = new Mock<ISettingsService>();

        settingsServiceMock.Setup(service => service.GetSettings()).Returns(new AppSettings());

        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);

        return settingsServiceMock;

    }

}


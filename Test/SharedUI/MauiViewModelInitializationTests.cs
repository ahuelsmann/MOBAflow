// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Discovery;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Common.Security;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Verifies deferred MAUI view-model startup behavior.
/// </summary>
[TestFixture]
internal sealed class MauiViewModelInitializationTests
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
    public void Constructor_DoesNotStartDeferredStartupWork()
    {
        var dependencies = CreateDependencies();

        _ = CreateViewModel(dependencies);

        dependencies.NetworkNotifierMock.Verify(notifier => notifier.StartListening(), Times.Never);
        dependencies.MobaRuntimeMock.Verify(client => client.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
        dependencies.MobaRuntimeMock.Verify(client => client.SetSystemStatePollingInterval(It.IsAny<int>()), Times.Never);
        dependencies.RestDiscoveryMock.Verify(service => service.DiscoverServerAsync(It.IsAny<string?>()), Times.Never);
        dependencies.RestDiscoveryMock.Verify(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.Z21DiscoveryMock.Verify(service => service.DiscoverZ21Async(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task InitializeAsync_StartsDeferredStartupWorkOnce_AndClampsFeedbackPointCount()
    {
        var settings = new AppSettings();
        settings.Counter.CountOfFeedbackPoints = 0;

        var dependencies = CreateDependencies(settings);
        dependencies.RestDiscoveryMock
            .Setup(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("192.168.0.79", 5001));
        dependencies.RestDiscoveryMock
            .Setup(service => service.DiscoverServerAsync(It.IsAny<string?>()))
            .ReturnsAsync(("192.168.0.79", 5001));
        dependencies.Z21DiscoveryMock
            .Setup(service => service.DiscoverZ21Async(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        dependencies.PhotoUploadMock
            .Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel(dependencies);

        await viewModel.InitializeAsync();
        await viewModel.InitializeAsync();
        await Task.Delay(2200);

        Assert.That(viewModel.CountOfFeedbackPoints, Is.EqualTo(1));
        Assert.That(viewModel.Statistics, Has.Count.EqualTo(1));
        Assert.That(settings.Counter.CountOfFeedbackPoints, Is.EqualTo(1));
        dependencies.NetworkNotifierMock.Verify(notifier => notifier.StartListening(), Times.Once);
        dependencies.MobaRuntimeMock.Verify(client => client.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        dependencies.MobaRuntimeMock.Verify(client => client.SetSystemStatePollingInterval(5), Times.Once);
        dependencies.RestDiscoveryMock.Verify(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.RestDiscoveryMock.Verify(service => service.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.Z21DiscoveryMock.Verify(service => service.DiscoverZ21Async(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        dependencies.SettingsServiceMock.Verify(service => service.SaveSettingsAsync(settings), Times.AtLeastOnce);
    }

    [Test]
    public async Task IncrementFeedbackPoints_ReplacesStatisticsCollectionAndKeepsListInSync()
    {
        var settings = new AppSettings();
        settings.Counter.CountOfFeedbackPoints = 1;

        var dependencies = CreateDependencies(settings);
        var viewModel = CreateViewModel(dependencies);

        await viewModel.InitializeAsync();

        var previousStatisticsReference = viewModel.Statistics;
        viewModel.IncrementFeedbackPointsCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.CountOfFeedbackPoints, Is.EqualTo(2));
            Assert.That(viewModel.Statistics, Has.Count.EqualTo(2));
            Assert.That(ReferenceEquals(previousStatisticsReference, viewModel.Statistics), Is.False);
        });
    }

    [Test]
    public async Task InitializeAsync_Should_ClearLegacyReadOnlyCredentialBeforeRuntimeStarts()
    {
        var dependencies = CreateDependencies();
        var credentialStore = new Mock<IRemoteControlCredentialStore>();
        credentialStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteControlCredential(
                Guid.Parse("11111111-2222-3333-4444-555555555555").ToString("N"),
                "192.168.0.27",
                5002,
                new string('A', 64),
                "credential-1",
                "legacy-refresh-token",
                RemoteControlRole.ReadOnly,
                1));
        credentialStore
            .Setup(store => store.ClearAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var transport = new Mock<IRemoteControlTransport>();
        var sessionService = new RemoteControlSessionService(credentialStore.Object, transport.Object);
        var viewModel = CreateViewModel(
            dependencies,
            remoteControlSessionService: sessionService);

        await viewModel.InitializeAsync().ConfigureAwait(true);

        credentialStore.Verify(
            store => store.ClearAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        transport.Verify(
            service => service.RefreshAsync(
                It.IsAny<RemoteControlCredential>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        dependencies.MobaRuntimeMock.Verify(
            runtime => runtime.StartAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void NotifyApplicationStopping_UnsubscribesFromEventBusSnapshots()
    {
        var dependencies = CreateDependencies();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(dependencies, eventBus);

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot { IsConnected = true, StatusText = "Connected" }));
        Assert.That(viewModel.IsConnected, Is.True);

        viewModel.NotifyApplicationStopping();

        Assert.That(eventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.EqualTo(0));
        Assert.That(eventBus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.EqualTo(0));

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot { IsConnected = false, StatusText = "Disconnected" }));
        Assert.That(viewModel.IsConnected, Is.True);
        dependencies.NetworkNotifierMock.Verify(notifier => notifier.StopListening(), Times.Once);
    }

    [Test]
    public void CounterSnapshotsUseOnlyLocalRuntimeAndSurviveCollectionRecreation()
    {
        var dependencies = CreateDependencies();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(dependencies, eventBus, runtimeHubRemoteClient: Mock.Of<IRuntimeHubRemoteClient>());
        viewModel.CountOfFeedbackPoints = 1;
        var lastFeedback = DateTimeOffset.UtcNow;
        var local = new MobaRuntimeSnapshot
        {
            InPortCounters = [new InPortCounterSnapshot(1, 5, lastFeedback, TimeSpan.FromSeconds(20))]
        };
        eventBus.Publish(new RuntimeSnapshotChangedEvent(local));
        eventBus.Publish(new RuntimeSnapshotChangedEvent(local));
        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            InPortCounters = [new InPortCounterSnapshot(1, 99, lastFeedback, null)]
        }));
        viewModel.IncrementFeedbackPointsCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Statistics[0].Count, Is.EqualTo(5));
            Assert.That(viewModel.Statistics[0].LastFeedbackTime, Is.EqualTo(lastFeedback.UtcDateTime));
            Assert.That(viewModel.Statistics[0].LastLapTime, Is.EqualTo(TimeSpan.FromSeconds(20)));
            Assert.That(viewModel.Statistics[1].Count, Is.Zero);
            Assert.That(eventBus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.Zero);
        }
        eventBus.Publish(new RuntimeSnapshotChangedEvent(MobaRuntimeSnapshot.Empty));
        Assert.That(viewModel.Statistics.All(stat => stat.Count == 0 && !stat.HasReceivedFirstLap), Is.True);
    }

    [Test]
    public async Task CounterResetWaitsForTheGatewayAndProjectsTheConfirmedLocalCount()
    {
        var dependencies = CreateDependencies();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var gateway = new Mock<IRuntimeCommandGateway>();
        var resetCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.Setup(value => value.ResetInPortCountersAsync(It.IsAny<CancellationToken>())).Returns(resetCompleted.Task);
        var viewModel = CreateViewModel(dependencies, eventBus, runtimeCommandGateway: gateway.Object);
        viewModel.CountOfFeedbackPoints = 1;
        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            InPortCounters = [new InPortCounterSnapshot(1, 7, null, null)]
        }));

        var reset = viewModel.ResetCountersCommand.ExecuteAsync(null);
        try
        {
            Assert.That(viewModel.Statistics.Single().Count, Is.EqualTo(7));
        }
        finally
        {
            resetCompleted.TrySetResult();
            await reset.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
        }

        Assert.That(viewModel.Statistics.Single().Count, Is.Zero);
        gateway.Verify(value => value.ResetInPortCountersAsync(It.IsAny<CancellationToken>()), Times.Once);
        dependencies.MobaRuntimeMock.Verify(value => value.ResetInPortCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FailedCounterResetPreservesCountsAndReportsTheError(bool unexpected)
    {
        var dependencies = CreateDependencies();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        dependencies.MobaRuntimeMock.Setup(value => value.ResetInPortCountersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(unexpected ? new IOException("Reset unavailable") : new InvalidOperationException("Reset unavailable"));
        var viewModel = CreateViewModel(dependencies, eventBus);
        viewModel.CountOfFeedbackPoints = 1;
        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            InPortCounters = [new InPortCounterSnapshot(1, 7, null, null)]
        }));

        await viewModel.ResetCountersCommand.ExecuteAsync(null).ConfigureAwait(true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Statistics.Single().Count, Is.EqualTo(7));
            Assert.That(viewModel.CounterResetError, Is.EqualTo("Reset unavailable"));
        }
    }

    private MauiViewModel CreateViewModel(
        TestDependencies dependencies,
        IEventBus? eventBus = null,
        RemoteControlSessionService? remoteControlSessionService = null,
        IRuntimeCommandGateway? runtimeCommandGateway = null,
        IRuntimeHubRemoteClient? runtimeHubRemoteClient = null)
    {
        var viewModel = new MauiViewModel(
            dependencies.MobaRuntimeMock.Object,
            dependencies.UiDispatcherMock.Object,
            dependencies.Settings,
            dependencies.SettingsServiceMock.Object,
            dependencies.RestDiscoveryMock.Object,
            dependencies.Z21DiscoveryMock.Object,
            dependencies.PhotoUploadMock.Object,
            dependencies.PhotoCaptureMock.Object,
            dependencies.NetworkNotifierMock.Object,
            NullLogger<MauiViewModel>.Instance,
            eventBus ?? new EventBus(NullLogger<EventBus>.Instance),
            dependencies.RestApiClientRegistrationMock.Object,
            runtimeHubRemoteClient: runtimeHubRemoteClient,
            runtimeCommandGateway: runtimeCommandGateway,
            remoteControlSessionService: remoteControlSessionService);

        _createdViewModels.Add(viewModel);
        return viewModel;
    }

    private static TestDependencies CreateDependencies(AppSettings? settings = null)
    {
        var currentSettings = settings ?? new AppSettings();
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var settingsServiceMock = new Mock<ISettingsService>();
        var restDiscoveryMock = new Mock<IRestDiscoveryService>();
        var z21DiscoveryMock = new Mock<IZ21DiscoveryService>();
        var photoUploadMock = new Mock<IPhotoUploadService>();
        var photoCaptureMock = new Mock<IPhotoCaptureService>();
        var networkNotifierMock = new Mock<INetworkProfileChangeNotifier>();
        var restApiClientRegistrationMock = new Mock<IRestApiClientRegistration>();

        mobaRuntimeMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntimeMock.Setup(client => client.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.SetSystemStatePollingInterval(It.IsAny<int>()));
        mobaRuntimeMock.Setup(client => client.ConnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.SetTrackPowerAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUiLowPriority(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUiAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(asyncAction => asyncAction());

        settingsServiceMock.Setup(service => service.GetSettings()).Returns(currentSettings);
        settingsServiceMock.Setup(service => service.LoadSettingsAsync()).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.ResetToDefaultsAsync()).Returns(Task.CompletedTask);

        restDiscoveryMock
            .Setup(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateNoDiscoveredEndpoint());
        restDiscoveryMock
            .Setup(service => service.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateNoDiscoveredEndpoint());
        z21DiscoveryMock.Setup(service => service.DiscoverZ21Async(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        photoUploadMock.Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).ReturnsAsync(false);
        photoUploadMock
            .Setup(service => service.UploadPhotoAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync((false, null, "not configured"));
        photoCaptureMock.Setup(service => service.CapturePhotoAsync()).ReturnsAsync((string?)null);
        restApiClientRegistrationMock.SetupGet(service => service.ClientId).Returns("test-client");
        restApiClientRegistrationMock.Setup(service => service.RegisterAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);

        return new TestDependencies(
            currentSettings,
            mobaRuntimeMock,
            uiDispatcherMock,
            settingsServiceMock,
            restDiscoveryMock,
            z21DiscoveryMock,
            photoUploadMock,
            photoCaptureMock,
            networkNotifierMock,
            restApiClientRegistrationMock);
    }

    private static (string?, int?) CreateNoDiscoveredEndpoint() => (null, null);

    private sealed record TestDependencies(
        AppSettings Settings,
        Mock<IMobaRuntime> MobaRuntimeMock,
        Mock<IUiDispatcher> UiDispatcherMock,
        Mock<ISettingsService> SettingsServiceMock,
        Mock<IRestDiscoveryService> RestDiscoveryMock,
        Mock<IZ21DiscoveryService> Z21DiscoveryMock,
        Mock<IPhotoUploadService> PhotoUploadMock,
        Mock<IPhotoCaptureService> PhotoCaptureMock,
        Mock<INetworkProfileChangeNotifier> NetworkNotifierMock,
        Mock<IRestApiClientRegistration> RestApiClientRegistrationMock);
}
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
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
        dependencies.RestDiscoveryMock.Verify(service => service.DiscoverServerAsync(), Times.Never);
        dependencies.Z21DiscoveryMock.Verify(service => service.DiscoverZ21Async(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task InitializeAsync_StartsDeferredStartupWorkOnce_AndClampsFeedbackPointCount()
    {
        var settings = new AppSettings();
        settings.Counter.CountOfFeedbackPoints = 0;

        var dependencies = CreateDependencies(settings);
        dependencies.RestDiscoveryMock
            .Setup(service => service.DiscoverServerAsync())
            .ReturnsAsync(("192.168.0.79", 5001));
        dependencies.Z21DiscoveryMock
            .Setup(service => service.DiscoverZ21Async(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        dependencies.PhotoUploadMock
            .Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel(dependencies);

        await viewModel.InitializeAsync();
        await viewModel.InitializeAsync();
        await Task.Delay(700);

        Assert.That(viewModel.CountOfFeedbackPoints, Is.EqualTo(1));
        Assert.That(viewModel.Statistics, Has.Count.EqualTo(1));
        Assert.That(settings.Counter.CountOfFeedbackPoints, Is.EqualTo(1));
        dependencies.NetworkNotifierMock.Verify(notifier => notifier.StartListening(), Times.Once);
        dependencies.MobaRuntimeMock.Verify(client => client.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        dependencies.MobaRuntimeMock.Verify(client => client.SetSystemStatePollingInterval(5), Times.Once);
        dependencies.RestDiscoveryMock.Verify(service => service.DiscoverServerAsync(), Times.Once);
        dependencies.Z21DiscoveryMock.Verify(service => service.DiscoverZ21Async(It.IsAny<CancellationToken>()), Times.Once);
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
    public void Constructor_WithEventBus_DoesNotProcessLegacyRuntimeSnapshotEvents()
    {
        var dependencies = CreateDependencies();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(dependencies, eventBus);

        dependencies.MobaRuntimeMock.Raise(
            runtime => runtime.SnapshotChanged += null,
            dependencies.MobaRuntimeMock.Object,
            new MobaRuntimeSnapshot { IsConnected = true, StatusText = "Connected" });

        Assert.That(viewModel.IsConnected, Is.False);
    }

    private MauiViewModel CreateViewModel(TestDependencies dependencies, IEventBus? eventBus = null)
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
            dependencies.RestApiClientRegistrationMock.Object,
            eventBus);

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
            .Setup(dispatcher => dispatcher.InvokeOnUiAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(asyncAction => asyncAction());

        settingsServiceMock.Setup(service => service.GetSettings()).Returns(currentSettings);
        settingsServiceMock.Setup(service => service.LoadSettingsAsync()).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.ResetToDefaultsAsync()).Returns(Task.CompletedTask);

        restDiscoveryMock
            .Setup(service => service.DiscoverServerAsync())
            .ReturnsAsync(CreateNoDiscoveredEndpoint());
        z21DiscoveryMock.Setup(service => service.DiscoverZ21Async(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        photoUploadMock.Setup(service => service.HealthCheckAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
        photoUploadMock
            .Setup(service => service.UploadPhotoAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync((false, null, "not configured"));
        photoCaptureMock.Setup(service => service.CapturePhotoAsync()).ReturnsAsync((string?)null);
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

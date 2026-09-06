#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Discovery;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Common.Security;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Service;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

using Moq;
using System.Net;
using System.Net.Http.Json;

[TestFixture]
internal sealed partial class RestApiStatusServiceTests
{
    private const string PairingFingerprint =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Test]
    public void GetToggleablePages_Should_CoverEveryRegisteredFeaturePage_WhenNavigationIsRegistered()
    {
        var pages = NavigationRegistration.RegisterPages(new ServiceCollection());
        var provider = new FeatureTogglePageProvider(pages, new AppSettings());
        string[] alwaysAvailablePageTags = ["help", "info", "settings"];

        var pagesWithoutToggle = pages
            .Where(page => string.IsNullOrWhiteSpace(page.FeatureToggleKey))
            .Select(page => page.Tag)
            .ToArray();
        var toggleablePages = provider.GetToggleablePages();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pagesWithoutToggle, Is.EquivalentTo(alwaysAvailablePageTags));
            Assert.That(toggleablePages.Select(page => page.Title), Does.Contain("Stations"));
            Assert.That(toggleablePages.Select(page => page.Title), Does.Contain("Recorder"));
        }
    }

    [Test]
    public async Task OpenAdminPairingAsync_Should_CreateValidatedQrPayload_WhenEndpointMatches()
    {
        using var response = CreateOpenPairingResponse(PairingFingerprint);
        string? lastRequestUri = null;
        var client = RestApiPairingTestClientFactory.Create(
            response,
            uri => lastRequestUri = uri);
        var host = new RestApiPairingHost(
            client,
            new FakeEndpointProvider(CreatePairingEndpoint(PairingFingerprint)));

        var invitation = await host.OpenAdminPairingAsync().ConfigureAwait(true);
        var decoded = RemotePairingQrCode.Decode(invitation.EncodedQrPayload);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lastRequestUri, Is.EqualTo("api/control-plane/security/pairing/open"));
            Assert.That(decoded.IsSuccess, Is.True);
            Assert.That(decoded.Invitation?.IpAddress, Is.EqualTo("192.168.0.27"));
            Assert.That(decoded.Invitation?.HttpPort, Is.EqualTo(5001));
            Assert.That(decoded.Invitation?.HttpsPort, Is.EqualTo(5002));
            Assert.That(invitation.ToString(), Does.Not.Contain(new string('B', 43)));
        }
    }

    [Test]
    public void OpenAdminPairingAsync_Should_RejectFingerprintMismatch()
    {
        using var response = CreateOpenPairingResponse(new string('C', 64));
        var client = RestApiPairingTestClientFactory.Create(response, _ => { });
        var host = new RestApiPairingHost(
            client,
            new FakeEndpointProvider(CreatePairingEndpoint(PairingFingerprint)));

        Assert.That(
            async () => await host.OpenAdminPairingAsync().ConfigureAwait(true),
            Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public async Task ApproveAsync_Should_SendHostOnlyDecisionRoute()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        string? lastRequestUri = null;
        var client = RestApiPairingTestClientFactory.Create(response, uri => lastRequestUri = uri);
        var host = new RestApiPairingHost(
            client,
            new FakeEndpointProvider(CreatePairingEndpoint(PairingFingerprint)));
        var requestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee").ToString("N");

        await host.ApproveAsync(requestId).ConfigureAwait(true);

        Assert.That(
            lastRequestUri,
            Is.EqualTo($"api/control-plane/security/pairing/requests/{requestId}/approve"));
    }
    [Test]
    public async Task RefreshAsync_Should_DisplayEffectivePort_WhenAnonymousStatusOmitsPort()
    {
        // Arrange
        const string statusJson = """
            {
              "status": "running"
            }
            """;
        using var statusHandler = new JsonHttpMessageHandler(statusJson);
        var dependencies = CreateDependencies(statusHandler);
        RestApiStatusChangedEvent? publishedStatus = null;
        dependencies.EventBus.Subscribe<RestApiStatusChangedEvent>(status => publishedStatus = status);

        // Act
        await dependencies.StatusService.RefreshAsync().ConfigureAwait(true);

        // Assert
        Assert.That(publishedStatus, Is.Not.Null);
        Assert.That(publishedStatus!.Status, Is.EqualTo("Running on port 5001"));
        await dependencies.DisposeAsync().ConfigureAwait(true);
    }

    [Test]
    public async Task DisposeAsync_ShouldDisconnectClientsOnlyOnce_WithoutDisposingDependencies()
    {
        // Arrange
        using var statusHandler = new ImmediateHttpMessageHandler();
        var dependencies = CreateDependencies(statusHandler);
        dependencies.PhotoHubClient
            .SetupGet(client => client.IsConnected)
            .Returns(true);
        dependencies.RuntimeHubHostClient
            .SetupGet(client => client.IsConnected)
            .Returns(true);

        // Act
        var firstDisposal = dependencies.StatusService.DisposeAsync().AsTask();
        var secondDisposal = dependencies.StatusService.DisposeAsync().AsTask();
        await Task.WhenAll(firstDisposal, secondDisposal).ConfigureAwait(true);

        // Assert
        dependencies.PhotoHubClient.Verify(client => client.DisconnectAsync(), Times.Once);
        dependencies.PhotoHubClient.Verify(client => client.DisposeAsync(), Times.Never);
        dependencies.RuntimeHubHostClient.Verify(client => client.DisconnectAsync(), Times.Once);
        await dependencies.DisposeAsync().ConfigureAwait(true);
    }

    [Test]
    public async Task DisposeAsync_ShouldCancelAndAwaitInFlightRefresh()
    {
        // Arrange
        using var requestHandler = new BlockingHttpMessageHandler();
        var dependencies = CreateDependencies(requestHandler);
        var refreshTask = dependencies.StatusService.RefreshAsync();
        await requestHandler.RequestStarted.Task.ConfigureAwait(true);

        // Act
        await dependencies.StatusService.DisposeAsync().ConfigureAwait(true);

        // Assert
        Assert.That(refreshTask.IsCompletedSuccessfully, Is.True);
        await dependencies.DisposeAsync().ConfigureAwait(true);
    }

    private static TestDependencies CreateDependencies(
        HttpMessageHandler statusHandler,
        IPhotoHubClient? actualPhotoHubClient = null,
        int port = 5001)
    {
        var appSettings = new AppSettings();
        appSettings.RestApi.Port = port;
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntime = new Mock<IMobaRuntime>();
        mobaRuntime.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntime.Setup(runtime => runtime.GetTrafficPackets()).Returns([]);

        var runtimeHubHostClient = new Mock<IRuntimeHubHostClient>();
        runtimeHubHostClient
            .Setup(client => client.DisconnectAsync())
            .Returns(Task.CompletedTask);

        var photoHubClient = new Mock<IPhotoHubClient>();
        photoHubClient
            .Setup(client => client.DisconnectAsync())
            .Returns(Task.CompletedTask);

        var restApiProcessService = new RestApiProcessService(
            appSettings,
            NullLogger<RestApiProcessService>.Instance,
            NullLogger<UdpDiscoveryResponder>.Instance);
        var runtimeHubService = new RestApiRuntimeHubService(
            runtimeHubHostClient.Object,
            mobaRuntime.Object,
            eventBus,
            NullLogger<RestApiRuntimeHubService>.Instance);

        var mainWindowViewModel = CreateMainWindowViewModel(appSettings, mobaRuntime, eventBus);
        var solutionSyncService = new RestApiSolutionSyncService(
            new Solution(),
            appSettings,
            mainWindowViewModel,
            restApiProcessService,
            eventBus,
            NullLogger<RestApiSolutionSyncService>.Instance);

        var statusHttpClient = new HttpClient(statusHandler);
        var statusService = new RestApiStatusService(
            statusHttpClient,
            appSettings,
            restApiProcessService,
            actualPhotoHubClient ?? photoHubClient.Object,
            runtimeHubService,
            solutionSyncService,
            runtimeHubHostClient.Object,
            mobaRuntime.Object,
            eventBus,
            NullLogger<RestApiStatusService>.Instance);

        return new TestDependencies(
            statusService,
            runtimeHubService,
            solutionSyncService,
            restApiProcessService,
            statusHttpClient,
            eventBus,
            photoHubClient,
            runtimeHubHostClient);
    }

    private static HttpResponseMessage CreateOpenPairingResponse(string fingerprint) =>
        new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                pairingSecret = new string('B', 43),
                serverPublicKeyFingerprint = fingerprint,
                expiresAt = DateTimeOffset.UtcNow.AddMinutes(2)
            })
        };

    private static MobApiDiscoveryEndpoint CreatePairingEndpoint(string fingerprint) => new(
        "192.168.0.27",
        5001,
        5002,
        Guid.Parse("11111111-2222-3333-4444-555555555555").ToString("N"),
        fingerprint,
        DiscoveryResponseParser.CurrentProtocolVersion);

    private static MainWindowViewModel CreateMainWindowViewModel(
        AppSettings appSettings,
        Mock<IMobaRuntime> mobaRuntime,
        IEventBus eventBus)
    {
        var uiDispatcher = new Mock<IUiDispatcher>();
        uiDispatcher
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntime.Object,
            eventBus,
            uiDispatcher.Object,
            appSettings,
            new Solution(),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            NullLogger<MainWindowViewModel>.Instance);
    }

    private sealed partial class ImmediateHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
        }
    }

    private sealed partial class JsonHttpMessageHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed partial class BlockingHttpMessageHandler : HttpMessageHandler
    {
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("The cancellation token should stop the request.");
        }
    }

    private sealed class FakeEndpointProvider(MobApiDiscoveryEndpoint endpoint)
        : IRestApiPairingEndpointProvider
    {
        public MobApiDiscoveryEndpoint? GetAuthenticatedPairingEndpoint() => endpoint;
    }

    private sealed record TestDependencies(
        RestApiStatusService StatusService,
        RestApiRuntimeHubService RuntimeHubService,
        RestApiSolutionSyncService SolutionSyncService,
        RestApiProcessService RestApiProcessService,
        HttpClient StatusHttpClient,
        IEventBus EventBus,
        Mock<IPhotoHubClient> PhotoHubClient,
        Mock<IRuntimeHubHostClient> RuntimeHubHostClient) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await StatusService.DisposeAsync().ConfigureAwait(false);
            await RuntimeHubService.DisposeAsync().ConfigureAwait(false);
            SolutionSyncService.Dispose();
            RestApiProcessService.Dispose();
            StatusHttpClient.Dispose();
        }
    }
}

internal static class RestApiPairingTestClientFactory
{
    internal static IHostControlPlaneClient Create(
        HttpResponseMessage response,
        Action<string?> capture)
    {
        return new RestApiPairingTestClient(response, capture);
    }
}

internal sealed class RestApiPairingTestClient(
    HttpResponseMessage response,
    Action<string?> capture) : IHostControlPlaneClient
{
    public bool IsEnrolled => true;

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        capture(request.RequestUri?.ToString());
        return Task.FromResult(response);
    }
}
#endif
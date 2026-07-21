#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Service;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

[TestFixture]
internal sealed class RestApiStatusServiceTests
{
    [Test]
    public async Task DisposeAsync_ShouldDisconnectClientsOnlyOnce_WithoutDisposingDependencies()
    {
        // Arrange
        await using var dependencies = CreateDependencies(new ImmediateHttpMessageHandler());
        dependencies.PhotoHubClient
            .SetupGet(client => client.IsConnected)
            .Returns(true);
        dependencies.RuntimeHubHostClient
            .SetupGet(client => client.IsConnected)
            .Returns(true);

        // Act
        var firstDisposal = dependencies.StatusService.DisposeAsync().AsTask();
        var secondDisposal = dependencies.StatusService.DisposeAsync().AsTask();
        await Task.WhenAll(firstDisposal, secondDisposal);

        // Assert
        dependencies.PhotoHubClient.Verify(client => client.DisconnectAsync(), Times.Once);
        dependencies.PhotoHubClient.Verify(client => client.DisposeAsync(), Times.Never);
        dependencies.RuntimeHubHostClient.Verify(client => client.DisconnectAsync(), Times.Once);
    }

    [Test]
    public async Task DisposeAsync_ShouldCancelAndAwaitInFlightRefresh()
    {
        // Arrange
        var requestHandler = new BlockingHttpMessageHandler();
        await using var dependencies = CreateDependencies(requestHandler);
        var refreshTask = dependencies.StatusService.RefreshAsync();
        await requestHandler.RequestStarted.Task;

        // Act
        await dependencies.StatusService.DisposeAsync();

        // Assert
        Assert.That(refreshTask.IsCompletedSuccessfully, Is.True);
    }

    private static TestDependencies CreateDependencies(HttpMessageHandler statusHandler)
    {
        var appSettings = new AppSettings();
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
            photoHubClient.Object,
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
            photoHubClient,
            runtimeHubHostClient);
    }

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

    private sealed class ImmediateHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
        }
    }

    private sealed class BlockingHttpMessageHandler : HttpMessageHandler
    {
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("The cancellation token should stop the request.");
        }
    }

    private sealed record TestDependencies(
        RestApiStatusService StatusService,
        RestApiRuntimeHubService RuntimeHubService,
        RestApiSolutionSyncService SolutionSyncService,
        RestApiProcessService RestApiProcessService,
        HttpClient StatusHttpClient,
        Mock<IPhotoHubClient> PhotoHubClient,
        Mock<IRuntimeHubHostClient> RuntimeHubHostClient) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await StatusService.DisposeAsync();
            await RuntimeHubService.DisposeAsync();
            SolutionSyncService.Dispose();
            RestApiProcessService.Dispose();
            StatusHttpClient.Dispose();
        }
    }
}
#endif

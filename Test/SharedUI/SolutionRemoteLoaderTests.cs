// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Events;
using Moba.SharedUI.Service;
using Moq;
using System.Net;
using System.Text;

namespace Moba.Test.SharedUI;

[TestFixture]
internal sealed class SolutionRemoteLoaderTests
{
    private const string ValidSolutionJson =
        """
        {
          "name": "Remote Solution",
          "schemaVersion": 1,
          "projects": [
            {
              "name": "Remote Project",
              "locomotives": [],
              "journeys": [],
              "workflows": [],
              "trains": []
            }
          ]
        }
        """;

    [Test]
    public async Task SyncIfNeededAsync_ActivatesProject_WhenRemoteSolutionIsNewer()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var handler = new FakeSolutionHttpHandler(updatedAt, ValidSolutionJson);
        var httpClient = new HttpClient(handler);
        var runtimeMock = new Mock<IMobaRuntime>();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobileContext = new MobileSolutionContext();
        var synced = false;
        eventBus.Subscribe<SolutionSyncedEvent>(_ => synced = true);
        var loader = new SolutionRemoteLoader(
            runtimeMock.Object,
            mobileContext,
            eventBus,
            NullLogger<SolutionRemoteLoader>.Instance,
            httpClient);

        await loader.SyncIfNeededAsync("192.168.0.10", 5001);

        runtimeMock.Verify(
            runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.That(loader.LastSyncedAt, Is.EqualTo(updatedAt));
        Assert.That(mobileContext.SelectedProject, Is.Not.Null);
        Assert.That(mobileContext.SelectedProject!.Name, Is.EqualTo("Remote Project"));
        Assert.That(synced, Is.True);
    }

    [Test]
    public async Task SyncIfNeededAsync_SkipsSecondCall_WhenUpdatedAtUnchanged()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var handler = new FakeSolutionHttpHandler(updatedAt, ValidSolutionJson);
        var httpClient = new HttpClient(handler);
        var runtimeMock = new Mock<IMobaRuntime>();
        var loader = new SolutionRemoteLoader(
            runtimeMock.Object,
            new MobileSolutionContext(),
            new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<SolutionRemoteLoader>.Instance,
            httpClient);

        await loader.SyncIfNeededAsync("192.168.0.10", 5001);
        await loader.SyncIfNeededAsync("192.168.0.10", 5001);

        runtimeMock.Verify(
            runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class FakeSolutionHttpHandler(DateTimeOffset updatedAt, string solutionJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith("/api/solution/meta", StringComparison.Ordinal))
            {
                var metaJson = $$"""{"updatedAt":"{{updatedAt:O}}","name":"Remote Solution","schemaVersion":1,"firstProjectName":"Remote Project"}""";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(metaJson, Encoding.UTF8, "application/json")
                });
            }

            if (path.EndsWith("/api/solution", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(solutionJson, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
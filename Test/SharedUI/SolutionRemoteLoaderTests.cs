// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Moba.Test.SharedUI;

[TestFixture]
internal sealed class SolutionRemoteLoaderTests
{
    private const string ValidSolutionJson =
        """
        {
          "name": "Remote Solution",
          "schemaVersion": 4,
          "projects": [
            {
              "name": "Other Project",
              "locomotives": [],
              "journeys": [],
              "workflows": [],
              "trains": []
            },
            {
              "name": "myMOBA",
              "locomotives": [
                {
                  "id": "bb15c10a-5b78-451f-8f2f-2d4e3efa74af",
                  "name": "BR 110 Verkehrsrot",
                  "digitalAddress": 7
                }
              ],
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
        var handler = new FakeSolutionHttpHandler(updatedAt, ValidSolutionJson, "C:/demo/solution.json");
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
        Assert.That(mobileContext.SelectedProject!.Name, Is.EqualTo("Other Project"));
        Assert.That(mobileContext.SelectedProject.Locomotives, Is.Empty);
        Assert.That(synced, Is.True);
    }

    [Test]
    public async Task SyncIfNeededAsync_SelectsActiveProjectLocomotives_WhenMetaIncludesActiveProjectName()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var handler = new FakeSolutionHttpHandler(
            updatedAt,
            ValidSolutionJson,
            "C:/demo/solution.json",
            activeProjectName: "myMOBA");
        var httpClient = new HttpClient(handler);
        var runtimeMock = new Mock<IMobaRuntime>();
        var mobileContext = new MobileSolutionContext();
        var loader = new SolutionRemoteLoader(
            runtimeMock.Object,
            mobileContext,
            new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<SolutionRemoteLoader>.Instance,
            httpClient);

        await loader.SyncIfNeededAsync("192.168.0.10", 5001);

        Assert.That(mobileContext.SelectedProject?.Name, Is.EqualTo("myMOBA"));
        Assert.That(mobileContext.SelectedProject?.Locomotives, Has.Count.EqualTo(1));
        Assert.That(mobileContext.SelectedProject?.Locomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
        runtimeMock.Verify(
            runtime => runtime.ActivateProjectAsync(
                It.Is<Project>(project => project.Name == "myMOBA"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SyncIfNeededAsync_SkipsSecondCall_WhenUpdatedAtUnchanged()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var handler = new FakeSolutionHttpHandler(updatedAt, ValidSolutionJson, "C:/demo/solution.json");
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

    [Test]
    public async Task SyncIfNeededAsync_ActivatesProject_WhenSourcePathMissing()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var handler = new FakeSolutionHttpHandler(updatedAt, ValidSolutionJson, sourcePath: null);
        var httpClient = new HttpClient(handler);
        var runtimeMock = new Mock<IMobaRuntime>();
        var loader = new SolutionRemoteLoader(
            runtimeMock.Object,
            new MobileSolutionContext(),
            new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<SolutionRemoteLoader>.Instance,
            httpClient);

        await loader.SyncIfNeededAsync("192.168.0.10", 5001);

        runtimeMock.Verify(
            runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.That(loader.LastSyncedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public async Task SyncIfNeededAsync_PersistsSolutionToMobileStore()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var handler = new FakeSolutionHttpHandler(updatedAt, ValidSolutionJson, "C:/demo/solution.json", "myMOBA");
        var httpClient = new HttpClient(handler);
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mobasmart-loader-" + Guid.NewGuid().ToString("N"));
        var store = new MobileSolutionStore(tempDirectory, NullLogger<MobileSolutionStore>.Instance);
        var loader = new SolutionRemoteLoader(
            new Mock<IMobaRuntime>().Object,
            new MobileSolutionContext(),
            new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<SolutionRemoteLoader>.Instance,
            httpClient,
            mobileSolutionStore: store);

        try
        {
            await loader.SyncIfNeededAsync("192.168.0.10", 5001);

            var cached = await store.TryLoadAsync();
            Assert.That(cached, Is.Not.Null);
            Assert.That(cached!.Meta.ActiveProjectName, Is.EqualTo("myMOBA"));
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Test]
    public async Task TryLoadFromCacheAsync_AppliesCachedSolutionAndActivatesProject()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mobasmart-loader-" + Guid.NewGuid().ToString("N"));
        var store = new MobileSolutionStore(tempDirectory, NullLogger<MobileSolutionStore>.Instance);
        var solution = JsonSerializer.Deserialize<Solution>(ValidSolutionJson, JsonOptions.Default)!;
        var meta = new SolutionSyncMeta(DateTimeOffset.UtcNow, solution.Name, "myMOBA");
        await store.SaveAsync(solution, meta);

        var runtimeMock = new Mock<IMobaRuntime>();
        var mobileContext = new MobileSolutionContext();
        var loader = new SolutionRemoteLoader(
            runtimeMock.Object,
            mobileContext,
            new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<SolutionRemoteLoader>.Instance,
            new HttpClient(new FakeSolutionHttpHandler(DateTimeOffset.UtcNow, ValidSolutionJson, null)),
            mobileSolutionStore: store);

        try
        {
            var loaded = await loader.TryLoadFromCacheAsync();

            Assert.That(loaded, Is.True);
            Assert.That(mobileContext.SelectedProject?.Name, Is.EqualTo("myMOBA"));
            runtimeMock.Verify(
                runtime => runtime.ActivateProjectAsync(
                    It.Is<Project>(project => project.Name == "myMOBA"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Test]
    public async Task TryLoadFromCacheAsync_RefreshesTrainControlProjectLocomotives()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mobasmart-loader-" + Guid.NewGuid().ToString("N"));
        var store = new MobileSolutionStore(tempDirectory, NullLogger<MobileSolutionStore>.Instance);
        var solution = JsonSerializer.Deserialize<Solution>(ValidSolutionJson, JsonOptions.Default)!;
        var meta = new SolutionSyncMeta(DateTimeOffset.UtcNow, solution.Name, "myMOBA");
        await store.SaveAsync(solution, meta);

        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        var mobileContext = new MobileSolutionContext();
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        var loader = new SolutionRemoteLoader(
            runtimeMock.Object,
            mobileContext,
            new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<SolutionRemoteLoader>.Instance,
            new HttpClient(new FakeSolutionHttpHandler(DateTimeOffset.UtcNow, ValidSolutionJson, null, "myMOBA")),
            mobileSolutionStore: store);

        var trainControl = new TrainControlViewModel(
            runtimeMock.Object,
            settingsMock.Object,
            mobileContext,
            NullLogger<TrainControlViewModel>.Instance,
            eventBus: new EventBus(NullLogger<EventBus>.Instance),
            options: new TrainControlViewModelOptions());

        try
        {
            var loaded = await loader.TryLoadFromCacheAsync();
            trainControl.RefreshLocomotiveList();

            Assert.That(loaded, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(trainControl.HasProjectLocomotives, Is.True);
                Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
                Assert.That(trainControl.ProjectLocomotives[0].DigitalAddress, Is.EqualTo(7u));
            });
        }
        finally
        {
            trainControl.Dispose();
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Test]
    public async Task SyncIfNeededAsync_SkipsRuntimeActivation_WhenMobaflowSessionIsActive()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var handler = new FakeSolutionHttpHandler(updatedAt, ValidSolutionJson, "C:/demo/solution.json");
        var httpClient = new HttpClient(handler);
        var runtimeMock = new Mock<IMobaRuntime>();
        var coordinatorMock = new Mock<IMobileRuntimeCoordinator>();
        coordinatorMock.SetupGet(coordinator => coordinator.PreferRemoteRuntime).Returns(true);
        var loader = new SolutionRemoteLoader(
            runtimeMock.Object,
            new MobileSolutionContext(),
            new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<SolutionRemoteLoader>.Instance,
            httpClient,
            coordinatorMock.Object);

        await loader.SyncIfNeededAsync("192.168.0.10", 5001);

        runtimeMock.Verify(
            runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SyncIfNeededAsync_RejectsSolutionJson_WithoutSchemaVersion()
    {
        const string invalidSolutionJson =
            """
            {
              "name": "Invalid Solution",
              "projects": [
                {
                  "name": "myMOBA",
                  "locomotives": [
                    {
                      "id": "bb15c10a-5b78-451f-8f2f-2d4e3efa74af",
                      "name": "BR 110 Verkehrsrot",
                      "digitalAddress": 7
                    }
                  ],
                  "journeys": [],
                  "workflows": [],
                  "trains": []
                }
              ]
            }
            """;

        var updatedAt = DateTimeOffset.UtcNow;
        var handler = new FakeSolutionHttpHandler(updatedAt, invalidSolutionJson, "C:/demo/solution.json", "myMOBA");
        var httpClient = new HttpClient(handler);
        var mobileContext = new MobileSolutionContext();
        var loader = new SolutionRemoteLoader(
            new Mock<IMobaRuntime>().Object,
            mobileContext,
            new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<SolutionRemoteLoader>.Instance,
            httpClient);

        await loader.ForceSyncAsync("192.168.0.10", 5001);

        Assert.That(mobileContext.SelectedProject, Is.Null);
    }

    private sealed class FakeSolutionHttpHandler(
        DateTimeOffset updatedAt,
        string solutionJson,
        string? sourcePath,
        string? activeProjectName = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith("/api/solution/meta", StringComparison.Ordinal))
            {
                var sourcePathJson = sourcePath == null
                    ? "null"
                    : $"\"{sourcePath.Replace("\\", "\\\\")}\"";
                var activeProjectJson = activeProjectName == null
                    ? "null"
                    : $"\"{activeProjectName}\"";
                var metaJson =
                    $$"""{"updatedAt":"{{updatedAt:O}}","sourcePath":{{sourcePathJson}},"activeProjectName":{{activeProjectJson}},"name":"Remote Solution","schemaVersion":1,"firstProjectName":"Other Project"}""";
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

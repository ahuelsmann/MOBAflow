// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;
using Moba.Test.Helpers;

using Moq;

/// <summary>
/// Catalog import, push, periodic and on-demand syncs of MOBAflow's solution on the phone.
/// </summary>
[TestFixture]
internal sealed class MobileSolutionSynchronizerTests
{
    private static readonly MobaflowEndpoint Endpoint = new("192.168.0.20", 5001);
    private static readonly bool[] ImportedOnceThenAgain = [true, false, true];
    private static readonly string[] TwoImports = ["sync", "snapshot", "sync", "snapshot"];
    private static readonly string[] TwoSnapshots = ["snapshot", "snapshot"];

    private Mock<ISolutionRemoteLoader> _loader = null!;
    private MobileSolutionSynchronizer _synchronizer = null!;
    private List<string> _calls = null!;

    [SetUp]
    public void SetUp()
    {
        _calls = [];
        _loader = new Mock<ISolutionRemoteLoader>();
        _loader.Setup(loader => loader.ForceSyncAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => _calls.Add("sync"))
            .Returns(Task.CompletedTask);
        _synchronizer = TestSolutionSynchronizers.Create(_loader.Object);
    }

    [Test]
    public async Task ImportInitialCatalogAsync_SyncsThenRequestsASnapshotOncePerSession()
    {
        var first = await ImportAsync(enabled: true, Endpoint).ConfigureAwait(false);
        _synchronizer.MarkInitialCatalogApplied();
        var second = await ImportAsync(enabled: true, Endpoint).ConfigureAwait(false);
        _synchronizer.ResetInitialCatalog();
        var afterReset = await ImportAsync(enabled: true, Endpoint).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new[] { first, second, afterReset }, Is.EqualTo(ImportedOnceThenAgain));
            Assert.That(_calls, Is.EqualTo(TwoImports));
        }
    }

    [Test]
    public async Task ImportInitialCatalogAsync_RunsConcurrentImportsOneAfterAnother()
    {
        var firstSync = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _loader.SetupSequence(loader => loader.ForceSyncAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(firstSync.Task)
            .Returns(Task.CompletedTask);

        var first = ImportAsync(enabled: true, Endpoint);
        var second = ImportAsync(enabled: true, Endpoint);
        var secondStartedEarly = _loader.Invocations.Count(invocation =>
            invocation.Method.Name == nameof(ISolutionRemoteLoader.ForceSyncAsync)) > 1;
        firstSync.SetResult();
        await Task.WhenAll(first, second).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondStartedEarly, Is.False);
            Assert.That(_calls, Is.EqualTo(TwoSnapshots));
        }

        _loader.Verify(
            loader => loader.ForceSyncAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task ImportInitialCatalogAsync_DoesNothingWhileTheConnectionIsOff()
    {
        var imported = await ImportAsync(enabled: false, Endpoint).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(imported, Is.False);
            Assert.That(_calls, Is.Empty);
        }
    }

    [Test]
    public async Task ImportInitialCatalogAsync_StillRequestsTheSnapshotWhenTheSyncFailsOrHasNoEndpoint()
    {
        _loader.Setup(loader => loader.ForceSyncAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Expected test failure"));

        var failedSync = await ImportAsync(enabled: true, Endpoint).ConfigureAwait(false);
        var withoutEndpoint = await ImportAsync(enabled: true, endpoint: null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failedSync, Is.True);
            Assert.That(withoutEndpoint, Is.True);
            Assert.That(_calls, Is.EqualTo(TwoSnapshots));
        }

        _loader.Verify(
            loader => loader.ForceSyncAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task PushAndPeriodicSync_SplitAtTheCatalogImport()
    {
        await _synchronizer.SyncAfterPushAsync(Endpoint, CancellationToken.None).ConfigureAwait(false);
        await _synchronizer.SyncIfNeededAsync(Endpoint, CancellationToken.None).ConfigureAwait(false);
        _synchronizer.MarkInitialCatalogApplied();
        await _synchronizer.SyncAfterPushAsync(Endpoint, CancellationToken.None).ConfigureAwait(false);
        await _synchronizer.SyncIfNeededAsync(Endpoint, CancellationToken.None).ConfigureAwait(false);

        _loader.Verify(loader => loader.ForceSyncAsync("192.168.0.20", 5001, It.IsAny<CancellationToken>()), Times.Once);
        _loader.Verify(loader => loader.SyncIfNeededAsync("192.168.0.20", 5001, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RequestSyncAsync_LoadsTheCacheForAnEmptyProjectAndNeedsAnEndpointForTheRemoteSync()
    {
        var withoutEndpoint = await _synchronizer
            .RequestSyncAsync(selectedProjectHasLocomotives: false, endpoint: null, CancellationToken.None)
            .ConfigureAwait(false);
        var withEndpoint = await _synchronizer
            .RequestSyncAsync(selectedProjectHasLocomotives: true, Endpoint, CancellationToken.None)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutEndpoint, Is.False);
            Assert.That(withEndpoint, Is.True);
        }

        _loader.Verify(loader => loader.TryLoadFromCacheAsync(It.IsAny<MobileSolutionCacheEntry?>(), It.IsAny<CancellationToken>()), Times.Once);
        _loader.Verify(loader => loader.ForceSyncAsync("192.168.0.20", 5001, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RequestSyncAsync_SkipsTheForcedSyncOnceTheCatalogIsShown()
    {
        _synchronizer.MarkInitialCatalogApplied();

        var synced = await _synchronizer
            .RequestSyncAsync(selectedProjectHasLocomotives: true, Endpoint, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.That(synced, Is.True);
        _loader.Verify(
            loader => loader.ForceSyncAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task WithoutALoader_NoSyncIsPossible()
    {
        var synchronizer = TestSolutionSynchronizers.Create();

        var synced = await synchronizer
            .RequestSyncAsync(selectedProjectHasLocomotives: false, Endpoint, CancellationToken.None)
            .ConfigureAwait(false);
        await synchronizer.SyncAfterPushAsync(Endpoint, CancellationToken.None).ConfigureAwait(false);

        Assert.That(synced, Is.False);
    }

    private Task<bool> ImportAsync(bool enabled, MobaflowEndpoint? endpoint) =>
        _synchronizer.ImportInitialCatalogAsync(
            () => enabled,
            () => endpoint,
            () =>
            {
                _calls.Add("snapshot");
                return Task.CompletedTask;
            },
            CancellationToken.None);
}

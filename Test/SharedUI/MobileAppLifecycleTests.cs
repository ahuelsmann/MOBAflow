// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;

using Moq;

/// <summary>
/// Foreground-service keep-alive, network-change debounce and stopping of the MOBAsmart app lifecycle.
/// </summary>
[TestFixture]
internal sealed class MobileAppLifecycleTests
{
    private static readonly MobileKeepAliveState Z21Only = new(true, false, false, false);
    private static readonly MobileKeepAliveState Idle = new(false, true, true, false);
    private static readonly MobileKeepAliveState Both = new(true, true, true, true);
    private static readonly string[] SecondChangeOnly = ["second"];

    private Mock<IBackgroundService> _backgroundService = null!;

    [SetUp]
    public void SetUp()
    {
        _backgroundService = new Mock<IBackgroundService>();
    }

    [Test]
    public async Task SyncBackgroundServiceAsync_StartsTheServiceWithTheActiveConnections()
    {
        using var lifecycle = CreateLifecycle();
        await lifecycle.SyncBackgroundServiceAsync(() => Z21Only, CancellationToken.None).ConfigureAwait(false);
        await lifecycle.SyncBackgroundServiceAsync(() => Both, CancellationToken.None).ConfigureAwait(false);

        _backgroundService.Verify(service => service.StartAsync("MOBAsmart Active", "Z21 connection maintained"), Times.Once);
        _backgroundService.Verify(service => service.StartAsync("MOBAsmart Active", "Z21 and MOBAflow session active"), Times.Once);
    }

    [Test]
    public async Task SyncBackgroundServiceAsync_StopsARunningServiceWithoutActiveConnections()
    {
        using var lifecycle = CreateLifecycle();
        _backgroundService.SetupGet(service => service.IsRunning).Returns(true);

        await lifecycle.SyncBackgroundServiceAsync(() => Idle, CancellationToken.None).ConfigureAwait(false);

        _backgroundService.Verify(service => service.StopAsync(), Times.Once);
        _backgroundService.Verify(service => service.StartAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task SyncBackgroundServiceAsync_ReadsTheStateAfterThePreviousSync()
    {
        using var lifecycle = CreateLifecycle();
        var firstStart = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _backgroundService
            .SetupSequence(service => service.StartAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(firstStart.Task)
            .Returns(Task.CompletedTask);
        var state = Z21Only;

        var first = lifecycle.SyncBackgroundServiceAsync(() => state, CancellationToken.None);
        var second = lifecycle.SyncBackgroundServiceAsync(() => state, CancellationToken.None);
        state = Both;
        firstStart.SetResult();
        await Task.WhenAll(first, second).ConfigureAwait(false);

        _backgroundService.Verify(service => service.StartAsync("MOBAsmart Active", "Z21 connection maintained"), Times.Once);
        _backgroundService.Verify(service => service.StartAsync("MOBAsmart Active", "Z21 and MOBAflow session active"), Times.Once);
    }

    [Test]
    public async Task SyncBackgroundServiceAsync_LogsAFailureAndDoesNothingWhileStopping()
    {
        using var lifecycle = CreateLifecycle();
        _backgroundService
            .Setup(service => service.StartAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Expected test failure"));

        await lifecycle.SyncBackgroundServiceAsync(() => Z21Only, CancellationToken.None).ConfigureAwait(false);
        lifecycle.BeginStopping();
        await lifecycle.SyncBackgroundServiceAsync(() => Z21Only, CancellationToken.None).ConfigureAwait(false);

        _backgroundService.Verify(service => service.StartAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task OnNetworkChangedAsync_RunsOnlyTheLastOfQuickChanges()
    {
        using var lifecycle = CreateLifecycle();
        var handled = new List<string>();

        var first = lifecycle.OnNetworkChangedAsync(_ =>
        {
            handled.Add("first");
            return Task.CompletedTask;
        });
        var second = lifecycle.OnNetworkChangedAsync(_ =>
        {
            handled.Add("second");
            return Task.CompletedTask;
        });
        await Task.WhenAll(first, second).ConfigureAwait(false);

        Assert.That(handled, Is.EqualTo(SecondChangeOnly));
    }

    [Test]
    public async Task BeginStopping_CancelsAPendingNetworkChangeOnce()
    {
        using var lifecycle = CreateLifecycle();
        var handled = false;
        var pending = lifecycle.OnNetworkChangedAsync(_ =>
        {
            handled = true;
            return Task.CompletedTask;
        });

        var first = lifecycle.BeginStopping();
        var second = lifecycle.BeginStopping();
        await pending.ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(lifecycle.IsStopping, Is.True);
            Assert.That(handled, Is.False);
        }
    }

    [Test]
    public async Task StopBackgroundServiceAsync_StopsARunningServiceAndIgnoresFailures()
    {
        using var lifecycle = CreateLifecycle();
        _backgroundService.SetupGet(service => service.IsRunning).Returns(true);
        _backgroundService.Setup(service => service.StopAsync()).ThrowsAsync(new InvalidOperationException("Expected test failure"));

        await lifecycle.StopBackgroundServiceAsync().ConfigureAwait(false);

        _backgroundService.Verify(service => service.StopAsync(), Times.Once);
    }

    private MobileAppLifecycle CreateLifecycle() =>
        new(_backgroundService.Object, TimeProvider.System, NullLogger.Instance);
}

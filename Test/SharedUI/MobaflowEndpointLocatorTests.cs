// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;
using Moba.Test.Helpers;

using Moq;

/// <summary>
/// Discovery, cadence and remembered endpoints of the MOBApi endpoint MOBAsmart connects to.
/// </summary>
[TestFixture]
internal sealed class MobaflowEndpointLocatorTests
{
    private static readonly (string? ip, int? port) NotFound = (null, null);

    private AppSettings _settings = null!;
    private Mock<ISettingsService> _settingsService = null!;
    private Mock<IRestDiscoveryService> _discovery = null!;
    private ManualClock _clock = null!;
    private MobaflowEndpointLocator _locator = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new AppSettings();
        _settings.RestApi.CurrentIpAddress = string.Empty;
        _settingsService = new Mock<ISettingsService>();
        _settingsService.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        _discovery = new Mock<IRestDiscoveryService>();
        _discovery.Setup(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotFound);
        _discovery.Setup(service => service.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotFound);
        _clock = new ManualClock(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
        _locator = TestEndpointLocators.Create(_settings, _settingsService.Object, _discovery.Object, _clock);
    }

    [Test]
    public async Task DiscoverAsync_RemembersAFoundEndpointInTheSettings()
    {
        _discovery.Setup(service => service.DiscoverServerFastAsync("192.168.0.111", It.IsAny<CancellationToken>()))
            .ReturnsAsync((" 192.168.0.20 ", 5001));

        var found = await _locator.DiscoverAsync("192.168.0.111", fullScan: false, CancellationToken.None).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.EqualTo(new MobaflowEndpoint("192.168.0.20", 5001)));
            Assert.That(_locator.StoredEndpoint, Is.EqualTo(found));
            Assert.That(_settings.RestApi.RecentIpAddresses, Does.Contain("192.168.0.20"));
        }

        _settingsService.Verify(service => service.SaveSettingsAsync(_settings), Times.Once);
        _discovery.Verify(service => service.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DiscoverAsync_ScansTheSubnetOnlyWhenRequestedAndFastDiscoveryFoundNothing()
    {
        _discovery.Setup(service => service.DiscoverServerAsync("192.168.0.111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("192.168.0.20", 5001));

        var withoutFullScan = await _locator.DiscoverAsync("192.168.0.111", fullScan: false, CancellationToken.None).ConfigureAwait(false);
        var withFullScan = await _locator.DiscoverAsync("192.168.0.111", fullScan: true, CancellationToken.None).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutFullScan, Is.Null);
            Assert.That(withFullScan, Is.EqualTo(new MobaflowEndpoint("192.168.0.20", 5001)));
        }
    }

    [Test]
    public async Task DiscoverAsync_ReportsAFailureAsNotFoundButKeepsCancellation()
    {
        _discovery.Setup(service => service.DiscoverServerFastAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Expected test failure"));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync().ConfigureAwait(false);

        var failed = await _locator.DiscoverAsync(null, fullScan: true, CancellationToken.None).ConfigureAwait(false);

        Assert.That(failed, Is.Null);
        await Assert.CatchAsync<OperationCanceledException>(() => _locator.DiscoverAsync(null, fullScan: true, cancelled.Token)).ConfigureAwait(false);
    }

    [Test]
    public async Task DiscoverWhenDueAsync_RunsEveryTenSecondsAtStartAndEvery25SecondsLater()
    {
        var first = await DiscoverWhenDueCountAsync().ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(5));
        var tooEarly = await DiscoverWhenDueCountAsync().ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(5));
        var afterTenSeconds = await DiscoverWhenDueCountAsync().ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(90));
        var afterStartWindow = await DiscoverWhenDueCountAsync().ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(20));
        var notYet25Seconds = await DiscoverWhenDueCountAsync().ConfigureAwait(false);

        Assert.That(new[] { first, tooEarly, afterTenSeconds, afterStartWindow, notYet25Seconds }, Is.EqualTo(DueSequence));
    }

    [Test]
    public async Task ResetDiscoveryCadence_LetsTheNextRediscoveryRunAtOnce()
    {
        await DiscoverWhenDueCountAsync().ConfigureAwait(false);

        _locator.ResetDiscoveryCadence();

        Assert.That(await DiscoverWhenDueCountAsync().ConfigureAwait(false), Is.EqualTo(1));
    }

    [Test]
    public void GetHealthCheckDelay_FollowsTheSessionAndTheStartWindow()
    {
        var disabled = _locator.GetHealthCheckDelay(connectionEnabled: false, sessionHealthy: false);
        var healthy = _locator.GetHealthCheckDelay(connectionEnabled: true, sessionHealthy: true);
        var startingUp = _locator.GetHealthCheckDelay(connectionEnabled: true, sessionHealthy: false);
        _clock.Advance(TimeSpan.FromSeconds(90));
        var later = _locator.GetHealthCheckDelay(connectionEnabled: true, sessionHealthy: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(disabled, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(healthy, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(startingUp, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(later, Is.EqualTo(TimeSpan.FromSeconds(15)));
        }
    }

    [Test]
    public async Task RememberReachableAddressAsync_SavesOnlyWhenTheHistoryChanged()
    {
        await _locator.RememberReachableAddressAsync("192.168.0.20").ConfigureAwait(false);
        await _locator.RememberReachableAddressAsync(" 192.168.0.20 ").ConfigureAwait(false);

        _settingsService.Verify(service => service.SaveSettingsAsync(_settings), Times.Once);
    }

    [Test]
    public async Task FindAsync_DoesNotRememberTheEndpoint()
    {
        _discovery.Setup(service => service.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("192.168.0.20", 5001));

        var found = await _locator.FindAsync(null, CancellationToken.None).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.EqualTo(new MobaflowEndpoint("192.168.0.20", 5001)));
            Assert.That(_locator.StoredEndpoint, Is.Null);
        }
    }

    private static readonly int[] DueSequence = [1, 0, 1, 1, 0];

    /// <summary>Runs a periodic rediscovery and returns how many fast discoveries it started (0 or 1).</summary>
    private async Task<int> DiscoverWhenDueCountAsync()
    {
        _discovery.Invocations.Clear();
        await _locator.DiscoverWhenDueAsync(null, CancellationToken.None).ConfigureAwait(false);
        return _discovery.Invocations.Count(invocation => invocation.Method.Name == nameof(IRestDiscoveryService.DiscoverServerFastAsync));
    }

    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}

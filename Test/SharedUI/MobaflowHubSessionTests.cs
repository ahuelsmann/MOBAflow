// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;
using Moba.Test.Helpers;

using Moq;

/// <summary>
/// Health check, client registration cadence and runtime hub connection of the MOBAsmart session with MOBApi.
/// </summary>
[TestFixture]
internal sealed class MobaflowHubSessionTests
{
    private AppSettings _settings = null!;
    private Mock<IPhotoUploadService> _healthService = null!;
    private Mock<IRuntimeHubRemoteClient> _hubClient = null!;
    private Mock<IRestApiClientRegistration> _registration = null!;
    private RecordingClock _clock = null!;
    private MobaflowHubSession _session = null!;
    private int _projectSelections;

    [SetUp]
    public void SetUp()
    {
        _settings = new AppSettings();
        _settings.RestApi.IsConnectionEnabled = true;
        _settings.RestApi.CurrentIpAddress = "192.168.0.20";
        _settings.RestApi.Port = 5001;
        var settingsService = new Mock<ISettingsService>();
        _healthService = new Mock<IPhotoUploadService>();
        _hubClient = new Mock<IRuntimeHubRemoteClient>();
        _hubClient.SetupGet(client => client.HasActiveHost).Returns(true);
        _registration = new Mock<IRestApiClientRegistration>();
        _registration.SetupGet(registration => registration.ClientId).Returns("phone-1");
        _registration.Setup(registration => registration.RegisterAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);
        _clock = new RecordingClock(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
        _projectSelections = 0;
        var locator = TestEndpointLocators.Create(_settings, settingsService.Object);
        _session = TestHubSessions.Create(
            _settings,
            locator,
            _healthService.Object,
            _hubClient.Object,
            _registration.Object,
            _clock);
    }

    [Test]
    public async Task EnsureConnectedAsync_SelectsTheProjectThenConnectsWithTheRegisteredClientId()
    {
        var outcome = await EnsureConnectedAsync(reachable: true).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(HubConnectionOutcome.Connected(hasActiveHost: true)));
            Assert.That(_projectSelections, Is.EqualTo(1));
        }

        _hubClient.Verify(
            client => client.ConnectAsync("192.168.0.20", 5001, "phone-1", It.IsAny<CancellationToken>(), false),
            Times.Once);
    }

    [Test]
    public async Task EnsureConnectedAsync_RetriesAfterFailuresWithGrowingDelays()
    {
        _hubClient
            .SetupSequence(client => client.ConnectAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("Expected test failure"))
            .ThrowsAsync(new InvalidOperationException("Expected test failure"))
            .Returns(Task.CompletedTask);

        var outcome = await EnsureConnectedAsync(reachable: true).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.IsConnected, Is.True);
            Assert.That(_clock.Delays, Is.EqualTo(new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1500) }));
            Assert.That(_projectSelections, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task EnsureConnectedAsync_ReportsAFailedConnectButKeepsTheRemoteZ21State()
    {
        _hubClient
            .Setup(client => client.ConnectAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("Expected test failure"));

        var outcome = await EnsureConnectedAsync(reachable: true).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(HubConnectionOutcome.ConnectFailed));
            Assert.That(_projectSelections, Is.EqualTo(4));
        }
    }

    [Test]
    public async Task EnsureConnectedAsync_KeepsAConnectedSessionWhenTheRestCheckFails()
    {
        _hubClient.SetupGet(client => client.IsConnected).Returns(true);
        _hubClient.SetupGet(client => client.HasActiveHost).Returns(false);

        var connected = await EnsureConnectedAsync(reachable: false).ConfigureAwait(false);
        _hubClient.SetupGet(client => client.IsConnected).Returns(false);
        var notConnected = await EnsureConnectedAsync(reachable: false).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(connected, Is.EqualTo(HubConnectionOutcome.Connected(hasActiveHost: false)));
            Assert.That(notConnected, Is.EqualTo(HubConnectionOutcome.Unchanged));
        }

        VerifyNoConnectAttempt();
    }

    [Test]
    public async Task EnsureConnectedAsync_RebuildsAConnectedSessionOnlyWhenForced()
    {
        _hubClient.SetupGet(client => client.IsConnected).Returns(true);

        await EnsureConnectedAsync(reachable: true).ConfigureAwait(false);
        VerifyNoConnectAttempt();

        await EnsureConnectedAsync(reachable: true, forceReconnect: true).ConfigureAwait(false);

        _hubClient.Verify(client => client.DisconnectAsync(), Times.Once);
        _hubClient.Verify(
            client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), true),
            Times.Once);
    }

    [Test]
    public async Task EnsureConnectedAsync_DisconnectsWhenTheMobaflowConnectionIsOff()
    {
        _settings.RestApi.IsConnectionEnabled = false;
        _hubClient.SetupGet(client => client.IsConnected).Returns(true);
        _hubClient.Setup(client => client.DisconnectAsync()).ThrowsAsync(new InvalidOperationException("Expected test failure"));

        var outcome = await EnsureConnectedAsync(reachable: true).ConfigureAwait(false);

        Assert.That(outcome, Is.EqualTo(HubConnectionOutcome.Disconnected));
        _hubClient.Verify(client => client.DisconnectAsync(), Times.Once);
        VerifyNoConnectAttempt();
    }

    [Test]
    public async Task EnsureConnectedAsync_NeedsAStoredEndpoint()
    {
        _settings.RestApi.CurrentIpAddress = " ";

        var outcome = await EnsureConnectedAsync(reachable: true).ConfigureAwait(false);

        Assert.That(outcome, Is.EqualTo(HubConnectionOutcome.Disconnected));
        VerifyNoConnectAttempt();
    }

    [Test]
    public async Task RegisterClientWhenDueAsync_RegistersFirstAndThenEveryTwoMinutes()
    {
        var endpoint = new MobaflowEndpoint("192.168.0.20", 5001);

        await _session.RegisterClientWhenDueAsync(endpoint).ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(119));
        await _session.RegisterClientWhenDueAsync(endpoint).ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _session.RegisterClientWhenDueAsync(endpoint).ConfigureAwait(false);

        _registration.Verify(registration => registration.RegisterAsync("192.168.0.20", 5001), Times.Exactly(2));
    }

    [Test]
    public async Task CheckHealthAsync_UsesTheShortTimeoutOnlyWhileConnecting()
    {
        var endpoint = new MobaflowEndpoint("192.168.0.20", 5001);

        await _session.CheckHealthAsync(endpoint, connecting: true).ConfigureAwait(false);
        await _session.CheckHealthAsync(endpoint, connecting: false).ConfigureAwait(false);

        _healthService.Verify(service => service.HealthCheckAsync("192.168.0.20", 5001, TimeSpan.FromSeconds(4)), Times.Once);
        _healthService.Verify(service => service.HealthCheckAsync("192.168.0.20", 5001, null), Times.Once);
    }

    private Task<HubConnectionOutcome> EnsureConnectedAsync(bool reachable, bool forceReconnect = false) =>
        _session.EnsureConnectedAsync(
            reachable,
            forceReconnect,
            () =>
            {
                _projectSelections++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

    private void VerifyNoConnectAttempt() =>
        _hubClient.Verify(
            client => client.ConnectAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()),
            Times.Never);

    /// <summary>A clock whose delays complete at once and are recorded.</summary>
    private sealed class RecordingClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public List<TimeSpan> Delays { get; } = [];

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Delays.Add(dueTime);
            return System.CreateTimer(callback, state, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }
    }
}

#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Common.Events;
using Moba.Test.Helpers;

using Moq;

/// <summary>
/// Issue 428: the first reachable status was read before the RuntimeHub host connected, and the next check came
/// 30 s later, so the MOBAsmart sync rows looked stuck. These tests drive the poll timer with fake time.
/// </summary>
internal sealed partial class RestApiStatusServiceTests
{
    [Test]
    public async Task Start_ShouldConfirmTheConnectedHostAfterOneSecond_InsteadOfAfterTheThirtySecondInterval()
    {
        // Arrange
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var probe = new SyncStartProbe(clock);
        var hostConnected = false;
        using var statusHandler = new SyncStatusHttpMessageHandler(probe, () => hostConnected);
        var dependencies = CreateDependencies(statusHandler, timeProvider: clock);
        await using var dependenciesLifetime = dependencies.ConfigureAwait(false);
        dependencies.RuntimeHubHostClient.SetupGet(client => client.IsConnected).Returns(() => hostConnected);
        dependencies.RuntimeHubHostClient
            .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                probe.RecordConnectAttempt();
                hostConnected = true;
                return Task.CompletedTask;
            });
        dependencies.EventBus.Subscribe<MobaflowSyncDiagnosticsChangedEvent>(e => probe.RecordDiagnostics(e.Diagnostics));

        // Act: the first refresh connects the host after it read the status.
        dependencies.StatusService.Start();
        var afterConnect = await probe.WaitForDiagnosticsAsync(diagnostics => diagnostics.HostClientConnected).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterConnect.ServerHasHost, Is.False, "The status was read before the host connected.");
            Assert.That(afterConnect.SyncStarting, Is.True, "The rows keep waiting instead of reporting a problem.");
        }

        clock.Advance(TimeSpan.FromSeconds(1));
        var confirmed = await probe.WaitForDiagnosticsAsync(diagnostics => diagnostics.ServerHasHost).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(confirmed.SyncStarting, Is.False);
            Assert.That(confirmed.HostConnectPending, Is.False);
            Assert.That(probe.RequestTimes, Is.EqualTo(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1) }));
        }

        clock.Advance(TimeSpan.FromSeconds(29));
        Assert.That(probe.RequestTimes, Has.Length.EqualTo(2), "Once set up, the status is checked every 30 s again.");
        clock.Advance(TimeSpan.FromSeconds(1));
        await probe.WaitUntilAsync(() => probe.RequestTimes.Length == 3).ConfigureAwait(false);
        Assert.That(probe.RequestTimes[^1], Is.EqualTo(TimeSpan.FromSeconds(31)));
    }

    [Test]
    public async Task Start_ShouldRetryAFailedHostConnectWithinSeconds_ThenFallBackToTheThirtySecondInterval()
    {
        // Arrange
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var probe = new SyncStartProbe(clock);
        using var statusHandler = new SyncStatusHttpMessageHandler(probe, () => false);
        var dependencies = CreateDependencies(statusHandler, timeProvider: clock);
        await using var dependenciesLifetime = dependencies.ConfigureAwait(false);
        dependencies.RuntimeHubHostClient
            .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                probe.RecordConnectAttempt();
                return Task.FromException(new HttpRequestException("RuntimeHub refused the connection"));
            });
        dependencies.EventBus.Subscribe<MobaflowSyncDiagnosticsChangedEvent>(e => probe.RecordDiagnostics(e.Diagnostics));

        // Act: run 70 s of fake time in one-second steps. Every refresh attempts the connect once, so the refresh
        // started by a step has finished once the diagnostics after its attempt are published.
        dependencies.StatusService.Start();
        await probe.WaitForRefreshAfterAttemptAsync(1).ConfigureAwait(false);
        for (var second = 1; second <= 70; second++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            await probe.WaitForRefreshAfterAttemptAsync(probe.RequestTimes.Length).ConfigureAwait(false);
        }

        // Assert: retries after 1, 2, 5, 10, 10 and 10 s, then the normal 30 s interval.
        var expectedSeconds = new[] { 0, 1, 3, 8, 18, 28, 38, 68 };
        var lastStarting = await probe.WaitForRefreshAfterAttemptAsync(7).ConfigureAwait(false);
        var startPhase = await probe.WaitForRefreshAfterAttemptAsync(6).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(probe.ConnectAttemptTimes, Is.EqualTo(expectedSeconds.Select(second => TimeSpan.FromSeconds(second))));
            Assert.That(startPhase.SyncStarting, Is.True, "The sixth attempt still belongs to the start phase.");
            Assert.That(lastStarting.SyncStarting, Is.False, "After the start phase the row reports the problem.");
            Assert.That(lastStarting.HostConnectPending, Is.True, "The host connect is still retried every 30 s.");
        }
    }

    [Test]
    public async Task Start_ShouldStillRecheckPromptly_WhenTheHostConnectTakesLongerThanTheWholeStartPhase()
    {
        // Arrange: the connect hangs for 45 s, longer than all re-check delays together (38 s).
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var probe = new SyncStartProbe(clock);
        var hostConnected = false;
        var connect = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var statusHandler = new SyncStatusHttpMessageHandler(probe, () => hostConnected);
        var dependencies = CreateDependencies(statusHandler, timeProvider: clock);
        await using var dependenciesLifetime = dependencies.ConfigureAwait(false);
        dependencies.RuntimeHubHostClient.SetupGet(client => client.IsConnected).Returns(() => hostConnected);
        dependencies.RuntimeHubHostClient
            .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                probe.RecordConnectAttempt();
                return connect.Task;
            });
        dependencies.EventBus.Subscribe<MobaflowSyncDiagnosticsChangedEvent>(e => probe.RecordDiagnostics(e.Diagnostics));

        // Act: every refresh during the connect publishes once and leaves the connect to the first refresh.
        dependencies.StatusService.Start();
        for (var second = 1; second <= 45; second++)
        {
            await probe.WaitUntilAsync(() => probe.DiagnosticsCount == probe.RequestTimes.Length).ConfigureAwait(false);
            clock.Advance(TimeSpan.FromSeconds(1));
        }

        await probe.WaitUntilAsync(() => probe.DiagnosticsCount == probe.RequestTimes.Length).ConfigureAwait(false);
        var whileConnecting = probe.LastDiagnostics;
        var requestsDuringConnect = probe.RequestTimes.Length;
        hostConnected = true;
        connect.SetResult();
        var afterConnect = await probe.WaitForDiagnosticsAsync(diagnostics => diagnostics.HostClientConnected).ConfigureAwait(false);
        clock.Advance(TimeSpan.FromSeconds(1));
        var confirmed = await probe.WaitForDiagnosticsAsync(diagnostics => diagnostics.ServerHasHost).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(probe.ConnectAttemptTimes, Has.Length.EqualTo(1), "Overlapping refreshes do not connect again.");
            Assert.That(requestsDuringConnect, Is.GreaterThan(7), "More checks ran during the connect than there are re-check delays.");
            Assert.That(whileConnecting.SyncStarting, Is.True, "The rows keep waiting while the connect runs.");
            Assert.That(afterConnect.SyncStarting, Is.True, "The finished connect starts the prompt re-checks.");
            Assert.That(probe.RequestTimes[^1], Is.EqualTo(TimeSpan.FromSeconds(46)), "The server is asked again 1 s after the connect.");
            Assert.That(confirmed.SyncStarting, Is.False);
        }
    }

    /// <summary>Records status requests, connect attempts and published diagnostics against the fake clock.</summary>
    private sealed class SyncStartProbe(ManualTimeProvider clock)
    {
        private readonly Lock _gate = new();
        private readonly DateTimeOffset _start = clock.GetUtcNow();
        private readonly List<TimeSpan> _requestTimes = [];
        private readonly List<TimeSpan> _connectAttemptTimes = [];
        private readonly List<(int Attempts, MobaflowSyncDiagnostics Diagnostics)> _diagnostics = [];
        private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TimeSpan[] RequestTimes
        {
            get
            {
                lock (_gate)
                {
                    return _requestTimes.ToArray();
                }
            }
        }

        public TimeSpan[] ConnectAttemptTimes
        {
            get
            {
                lock (_gate)
                {
                    return _connectAttemptTimes.ToArray();
                }
            }
        }

        public int DiagnosticsCount
        {
            get
            {
                lock (_gate)
                {
                    return _diagnostics.Count;
                }
            }
        }

        public MobaflowSyncDiagnostics LastDiagnostics
        {
            get
            {
                lock (_gate)
                {
                    return _diagnostics[^1].Diagnostics;
                }
            }
        }

        public void RecordRequest() => Record(() => _requestTimes.Add(clock.GetUtcNow() - _start));

        public void RecordConnectAttempt() => Record(() => _connectAttemptTimes.Add(clock.GetUtcNow() - _start));

        public void RecordDiagnostics(MobaflowSyncDiagnostics diagnostics) =>
            Record(() => _diagnostics.Add((_connectAttemptTimes.Count, diagnostics)));

        public Task<MobaflowSyncDiagnostics> WaitForDiagnosticsAsync(Func<MobaflowSyncDiagnostics, bool> match) =>
            WaitForAsync(entry => match(entry.Diagnostics));

        /// <summary>Waits for the diagnostics that the refresh publishes after the given connect attempt.</summary>
        public Task<MobaflowSyncDiagnostics> WaitForRefreshAfterAttemptAsync(int attempt) =>
            WaitForAsync(entry => entry.Attempts == attempt);

        public async Task WaitUntilAsync(Func<bool> condition)
        {
            while (true)
            {
                Task changed;
                lock (_gate)
                {
                    changed = _changed.Task;
                }

                if (condition())
                {
                    return;
                }

                await changed.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }

        private async Task<MobaflowSyncDiagnostics> WaitForAsync(Func<(int Attempts, MobaflowSyncDiagnostics Diagnostics), bool> match)
        {
            while (true)
            {
                Task changed;
                lock (_gate)
                {
                    foreach (var entry in _diagnostics.Where(match))
                    {
                        return entry.Diagnostics;
                    }

                    changed = _changed.Task;
                }

                await changed.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }

        private void Record(Action add)
        {
            TaskCompletionSource changed;
            lock (_gate)
            {
                add();
                changed = _changed;
                _changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            changed.TrySetResult();
        }
    }

    /// <summary>Answers GET /api/status like MOBApi; the server lists the host once it registered.</summary>
    private sealed partial class SyncStatusHttpMessageHandler(SyncStartProbe probe, Func<bool> hostRegistered) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            probe.RecordRequest();
            var hasHost = hostRegistered() ? "true" : "false";
            var json = $$"""{ "status": "running", "port": 5001, "runtime": { "hasHost": {{hasHost}} } }""";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
#endif

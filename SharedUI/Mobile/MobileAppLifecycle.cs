// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Mobile;

using Common.Runtime;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// The connection state that decides whether MOBAsmart keeps running in the background.
/// </summary>
/// <param name="IsZ21Connected">Whether the phone is connected to its project's Z21.</param>
/// <param name="IsMobaflowConnectionEnabled">Whether the MOBAflow connection is switched on.</param>
/// <param name="IsRestApiReachable">Whether MOBApi answers.</param>
/// <param name="IsRuntimeHubConnected">Whether the runtime hub is connected.</param>
public sealed record MobileKeepAliveState(
    bool IsZ21Connected,
    bool IsMobaflowConnectionEnabled,
    bool IsRestApiReachable,
    bool IsRuntimeHubConnected)
{
    /// <summary>Gets whether the MOBAflow session is usable.</summary>
    public bool IsMobaflowSessionActive => IsMobaflowConnectionEnabled && IsRestApiReachable && IsRuntimeHubConnected;
}

/// <summary>
/// MOBAsmart's app lifecycle: the Android foreground service that keeps active connections alive in the
/// background, the debounce of network changes and the stopping state. The root ViewModel supplies the
/// connection state and the work to run; this class decides when to run it.
/// </summary>
public sealed partial class MobileAppLifecycle : IDisposable
{
    private const string NotificationTitle = "MOBAsmart Active";

    /// <summary>Coalesces rapid platform connectivity notifications before re-running REST discovery.</summary>
    private static readonly TimeSpan NetworkChangeDebounce = TimeSpan.FromMilliseconds(750);

    private readonly IBackgroundService? _backgroundService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;
    private readonly Lock _gate = new();
    private Task _lastBackgroundServiceSync = Task.CompletedTask;
    private CancellationTokenSource? _networkChangeDebounceCts;
    private volatile bool _isStopping;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileAppLifecycle"/> class.
    /// </summary>
    /// <param name="backgroundService">Optional: the Android foreground service.</param>
    /// <param name="timeProvider">Clock for the network-change debounce.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public MobileAppLifecycle(IBackgroundService? backgroundService, TimeProvider timeProvider, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _backgroundService = backgroundService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Gets whether the host app is shutting down.</summary>
    public bool IsStopping => _isStopping;

    /// <summary>
    /// Marks the app as stopping and cancels a pending network-change refresh; returns false when it already was.
    /// </summary>
    public bool BeginStopping()
    {
        if (_isStopping)
        {
            return false;
        }

        _isStopping = true;
        CancelPendingNetworkChange(replaceWithNew: false);
        return true;
    }

    /// <summary>
    /// Starts or stops the foreground service for the current state. Syncs run one after another, and
    /// <paramref name="currentState"/> is read once the previous sync has finished.
    /// </summary>
    public async Task SyncBackgroundServiceAsync(Func<MobileKeepAliveState> currentState, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentState);
        if (_backgroundService == null || _isStopping)
        {
            return;
        }

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task previousSync;
        lock (_gate)
        {
            previousSync = _lastBackgroundServiceSync;
            _lastBackgroundServiceSync = done.Task;
        }

        try
        {
            await previousSync.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            cancellationToken.ThrowIfCancellationRequested();
            await ApplyBackgroundServiceStateAsync(_backgroundService, currentState()).ConfigureAwait(false);
        }
        finally
        {
            done.SetResult();
        }
    }

    /// <summary>Stops the foreground service if it runs; a failing stop is logged and ignored.</summary>
    public async Task StopBackgroundServiceAsync()
    {
        if (_backgroundService == null || !_backgroundService.IsRunning)
        {
            return;
        }

        try
        {
            await _backgroundService.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogBackgroundServiceStopFailed(_logger, ex);
        }
    }

    /// <summary>
    /// Runs <paramref name="handler"/> once the network has been quiet for 750 ms. A newer change cancels the
    /// waiting or running handler of the previous one through its token.
    /// </summary>
    public async Task OnNetworkChangedAsync(Func<CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var cancellationToken = CancelPendingNetworkChange(replaceWithNew: true);
        try
        {
            await Task.Delay(NetworkChangeDebounce, _timeProvider, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await handler(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose() => CancelPendingNetworkChange(replaceWithNew: false);

    private async Task ApplyBackgroundServiceStateAsync(IBackgroundService backgroundService, MobileKeepAliveState state)
    {
        try
        {
            if (!MobileBackgroundKeepAlivePolicy.ShouldKeepAlive(
                state.IsZ21Connected,
                state.IsMobaflowConnectionEnabled,
                state.IsRestApiReachable,
                state.IsRuntimeHubConnected))
            {
                if (backgroundService.IsRunning)
                {
                    await backgroundService.StopAsync().ConfigureAwait(false);
                }

                return;
            }

            var message = MobileBackgroundKeepAlivePolicy.GetNotificationMessage(
                state.IsZ21Connected,
                state.IsMobaflowSessionActive);
            await backgroundService.StartAsync(NotificationTitle, message).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during application shutdown.
        }
        catch (Exception ex)
        {
            LogBackgroundServiceSyncFailed(_logger, ex);
        }
    }

    private CancellationToken CancelPendingNetworkChange(bool replaceWithNew)
    {
        lock (_gate)
        {
            _networkChangeDebounceCts?.Cancel();
            _networkChangeDebounceCts?.Dispose();
            _networkChangeDebounceCts = replaceWithNew ? new CancellationTokenSource() : null;
            return _networkChangeDebounceCts?.Token ?? CancellationToken.None;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Foreground background service sync failed")]
    private static partial void LogBackgroundServiceSyncFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Foreground background service stop failed")]
    private static partial void LogBackgroundServiceStopFailed(ILogger logger, Exception exception);
}

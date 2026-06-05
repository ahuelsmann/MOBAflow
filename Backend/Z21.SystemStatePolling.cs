// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Common.Extension;

using Microsoft.Extensions.Logging;

using Network;

using Protocol;

public partial class Z21
{
    #region SystemState Polling
    /// <summary>
    /// Sets the system state polling interval. Use 0 to disable polling.
    /// Changes take effect immediately.
    /// </summary>
    /// <param name="intervalSeconds">Polling interval in seconds (0 = disabled, 1-30 recommended).</param>
    public void SetSystemStatePollingInterval(int intervalSeconds)
    {
        _systemStatePollingIntervalSeconds = intervalSeconds;

        if (_isConnected)
        {
            // Restart timer with new interval if connected
            StopSystemStatePollingTimer();
            if (intervalSeconds > 0)
            {
                StartSystemStatePollingTimer();
            }
        }

        _logger?.LogInformation("System state polling interval set to {Interval}s", intervalSeconds);
    }

    /// <summary>
    /// Starts a timer that periodically requests system state (current, voltage, temperature) from Z21.
    /// </summary>
    private void StartSystemStatePollingTimer()
    {
        if (_systemStatePollingIntervalSeconds <= 0) return;

        StopSystemStatePollingTimer();

        var interval = TimeSpan.FromSeconds(_systemStatePollingIntervalSeconds);
        _systemStatePollingTimer = new Timer(
            state =>
            {
                _ = state;
                SendSystemStateRequestAsync().Observe(ex => _logger?.LogWarning(ex, "Send system state request failed"));
            },
            null,
            interval,  // First poll after interval
            interval); // Subsequent polls at interval

        _logger?.LogDebug("SystemState polling timer started ({Interval}s interval)", _systemStatePollingIntervalSeconds);
    }

    /// <summary>
    /// Stops the system state polling timer.
    /// </summary>
    private void StopSystemStatePollingTimer()
    {
        if (_systemStatePollingTimer != null)
        {
            _systemStatePollingTimer.Dispose();
            _systemStatePollingTimer = null;
            _logger?.LogDebug("SystemState polling timer stopped");
        }
    }

    /// <summary>
    /// Sends a system state request (LAN_SYSTEMSTATE_GETDATA) to Z21.
    /// </summary>
    private async Task SendSystemStateRequestAsync()
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
        {
            return;
        }

        // Check if UDP is still connected before attempting send
        if (!_udp.IsConnected)
        {
            _logger?.LogTrace("SystemState request skipped: UDP not connected");
            return;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token,
                timeoutCts.Token);

            await SendAsync(Z21Command.BuildHandshake(), linkedCts.Token).ConfigureAwait(false);
            _logger?.LogTrace("SystemState request sent");
        }
        catch (OperationCanceledException) when (_cancellationTokenSource?.Token.IsCancellationRequested == true)
        {
            // Normal during shutdown, don't log
        }
        catch (UdpNotConnectedException ex)
        {
            // UDP disconnected during timer callback - this is expected during shutdown
            _logger?.LogTrace(ex, "SystemState request skipped: UDP not connected");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("SystemState request failed: {Message}", ex.Message);
        }
    }
    #endregion
}

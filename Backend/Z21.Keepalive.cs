// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Common.Events;
using Common.Extension;

using Microsoft.Extensions.Logging;

using Network;

public partial class Z21
{
    #region Keepalive Management
    /// <summary>
    /// Starts a timer that sends periodic status requests to keep the Z21 connection alive.
    /// The Z21 expects regular communication; without it, the connection may timeout after 60 seconds
    /// and the Z21 may become unstable or crash when multiple inactive clients accumulate.
    /// </summary>
    private void StartKeepaliveTimer()
    {
        // Ensure any existing timer is stopped first
        StopKeepaliveTimer();

        _keepaliveTimer = new Timer(
            state =>
            {
                _ = state;
                SendKeepaliveAsync().Observe(ex => _logger?.LogWarning(ex, "Send keepalive failed"));
            },
            null,
            TimeSpan.FromSeconds(30),  // First keepalive after 30 seconds
            TimeSpan.FromSeconds(30)); // Subsequent keepalives every 30 seconds

        _logger?.LogDebug("Keepalive timer started (30s interval)");
    }

    /// <summary>
    /// Stops the keepalive timer.
    /// </summary>
    private void StopKeepaliveTimer()
    {
        if (_keepaliveTimer != null)
        {
            _keepaliveTimer.Dispose();
            _keepaliveTimer = null;
            _logger?.LogDebug("Keepalive timer stopped");
        }
    }

    /// <summary>
    /// Sets IsConnected to true if not already connected and fires OnConnectedChanged event.
    /// Called when Z21 sends any valid response (SystemState, XBusStatus, SerialNumber, etc.)
    /// Starts SystemState polling timer on first successful connection (if enabled).
    /// </summary>
    private void SetConnectedIfNotAlready()
    {
        if (_isConnected) return;

        _isConnected = true;
        _logger?.LogInformation("✅ Z21 is responding - connection confirmed");

        // Start SystemState polling timer ONLY if interval > 0
        // Note: Z21 also sends SystemState automatically via broadcast (flag 0x0100)
        // Polling is optional for additional redundancy or faster updates
        if (_systemStatePollingIntervalSeconds > 0)
        {
            StartSystemStatePollingTimer();
            _logger?.LogDebug("SystemState polling enabled ({Interval}s). Note: Z21 also broadcasts SystemState automatically.",
                _systemStatePollingIntervalSeconds);
        }
        else
        {
            _logger?.LogDebug("SystemState polling disabled. Using Z21 broadcast-only (flag 0x0100).");
        }

        OnConnectedChanged?.Invoke(true);
        PublishEventAsync(new Z21ConnectionEstablishedEvent());
    }

    /// <summary>
    /// Sends a keepalive message (LAN_X_GET_STATUS) to the Z21.
    /// This prevents the Z21 from timing out inactive connections.
    /// Tracks failures and triggers disconnect after MaxKeepaliveFailures consecutive failures.
    /// </summary>
    private async Task SendKeepaliveAsync()
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
        {
            return;
        }

        // Check if UDP is still connected before attempting send
        if (!_udp.IsConnected)
        {
            _logger?.LogTrace("Keep-Alive skipped: UDP not connected");
            return;
        }

        try
        {
            // Create timeout token (5 seconds)
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token,
                timeoutCts.Token);

            await GetStatusAsync(linkedCts.Token).ConfigureAwait(false);

            // Success - reset failure counter
            if (_keepAliveFailures > 0)
            {
                _logger?.LogInformation("Keep-Alive recovered after {Failures} failures", _keepAliveFailures);
            }
            _keepAliveFailures = 0;
            _logger?.LogTrace("Keep-Alive sent successfully");
        }
        catch (OperationCanceledException) when (_cancellationTokenSource?.Token.IsCancellationRequested == true)
        {
            // Normal during shutdown, don't log
        }
        catch (UdpNotConnectedException ex)
        {
            // UDP disconnected during timer callback - this is expected during shutdown
            _logger?.LogTrace(ex, "Keep-Alive skipped: UDP not connected");
        }
        catch (Exception ex)
        {
            _keepAliveFailures++;
            _logger?.LogWarning("Keep-Alive failed ({Failures}/{Max}): {Message}",
                _keepAliveFailures, MaxKeepaliveFailures, ex.Message);

            if (_keepAliveFailures >= MaxKeepaliveFailures)
            {
                _logger?.LogError("Z21 connection lost after {Max} failed Keep-Alives. Disconnecting...",
                    MaxKeepaliveFailures);

                // Trigger disconnect on background thread to avoid deadlock
                HandleConnectionLostAsync().Observe(innerEx => _logger?.LogWarning(innerEx, "Handle connection lost failed"));
            }
        }
    }

    /// <summary>
    /// Handles connection lost scenario - disconnects and raises event.
    /// </summary>
    private async Task HandleConnectionLostAsync()
    {
        try
        {
            await DisconnectAsync().ConfigureAwait(false);
            OnConnectionLost?.Invoke();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during connection lost handling");
        }
    }
    #endregion
}

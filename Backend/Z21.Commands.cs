// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Microsoft.Extensions.Logging;

using Model;

using Protocol;

using Service;

using System.Net;

public partial class Z21
{
    #region Basic Commands
    /// <summary>
    /// Thread-safe send method with SemaphoreSlim to prevent concurrent sends.
    /// </summary>
    private async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        // ✅ Validate data BEFORE acquiring lock to fail fast
        ArgumentNullException.ThrowIfNull(data);

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // ✅ Only call ParsePacketType when actually logging (deferred execution)
            // This prevents NullReferenceException if ParsePacketType throws
            _trafficMonitor?.LogSentPacket(
                    data,
                    Z21Monitor.ParsePacketType(data),
                    $"Length: {data.Length} bytes"
                );

            await _udp.SendAsync(data).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task SendHandshakeAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(Z21Command.BuildHandshake(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets broadcast flags to receive only essential events (R-Bus feedback + System State).
    /// This reduces Z21 traffic by ~90% compared to subscribing to all events.
    /// </summary>
    private async Task SetBroadcastFlagsAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(Z21Command.BuildBroadcastFlagsBasic(), cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region Track Power Control
    /// <summary>
    /// Turns the track power ON.
    /// LAN_X_SET_TRACK_POWER_ON (X-Header: 0x21, DB0: 0x81)
    /// </summary>
    public async Task SetTrackPowerOnAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(Z21Command.BuildTrackPowerOn(), cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Track power ON command sent");
    }

    /// <summary>
    /// Turns the track power OFF.
    /// LAN_X_SET_TRACK_POWER_OFF (X-Header: 0x21, DB0: 0x80)
    /// </summary>
    public async Task SetTrackPowerOffAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(Z21Command.BuildTrackPowerOff(), cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Track power OFF command sent");
    }

    /// <summary>
    /// Triggers an emergency stop (locomotives stop but track power remains on).
    /// LAN_X_SET_STOP (X-Header: 0x80)
    /// </summary>
    public async Task SetEmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(Z21Command.BuildEmergencyStop(), cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Emergency stop command sent");
    }

    /// <summary>
    /// Requests the current Z21 status.
    /// LAN_X_GET_STATUS (X-Header: 0x21, DB0: 0x24)
    /// </summary>
    public async Task GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(Z21Command.BuildGetStatus(), cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Status request sent");
    }

    /// <summary>
    /// Attempts to recover a non-responsive Z21 by sending a recovery byte sequence.
    /// This can help when the Z21 stops responding without requiring a hardware restart.
    /// 
    /// If the UDP connection is closed, it will establish a new connection to the specified address.
    /// 
    /// Recovery Sequence (based on Z21 protocol behavior):
    /// 1. Check if UDP client is connected, if not connect to the specified address
    /// 2. Emergency Stop (0x06 0x00 0x40 0x00 0x80 0x80) - "Kicks" the Z21 awake
    /// 3. LAN_LOGOFF (0x04 0x00 0x30 0x00) - Clears any hanging client session
    /// 4. LAN_GET_SERIAL_NUMBER (0x04 0x00 0x10 0x00) - Simple command that always responds
    /// 5. LAN_SYSTEMSTATE_GETDATA (0x04 0x00 0x85 0x00) - Re-establish connection
    /// 6. LAN_SET_BROADCASTFLAGS (0x08 0x00 0x50 0x00 0xFF 0xFF 0xFF 0xFF) - Re-subscribe
    /// 7. LAN_X_GET_STATUS - Final status check
    /// </summary>
    /// <param name="address">IP address of Z21 (required if not already connected)</param>
    /// <param name="port">UDP port of Z21 (default: 21105)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RecoverConnectionAsync(IPAddress address, int port = Z21Protocol.DefaultPort, CancellationToken cancellationToken = default)
    {
        _logger?.LogWarning("🔄 Attempting Z21 recovery with byte sequence...");

        try
        {
            // Step 0: Check if UDP client is connected, if not establish connection
            if (!_udp.IsConnected)
            {
                _logger?.LogInformation("UDP client not connected - establishing connection to {Address}:{Port}", address, port);

                // Re-create cancellation token source
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Connect UDP (blind send capability)
                await _udp.ConnectAsync(address, port, _cancellationTokenSource.Token).ConfigureAwait(false);
                _logger?.LogInformation("UDP connection established for recovery");
            }
            else
            {
                // Re-create cancellation token source if needed
                if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _logger?.LogInformation("Re-creating cancellation token source for recovery");
                    _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                }
            }

            // Step 1: Emergency Stop - This often "wakes up" a stuck Z21
            await SendAsync(Z21Command.BuildEmergencyStop(), cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Recovery: Emergency Stop sent (wake-up kick)");
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Step 2: LOGOFF - Clear any hanging client session
            await SendAsync(Z21Command.BuildLogoff(), cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Recovery: LOGOFF sent (clear session)");
            await Task.Delay(150, cancellationToken).ConfigureAwait(false);

            // Step 3: GET_SERIAL_NUMBER - Simple command that Z21 should always respond to
            await SendAsync(Z21Command.BuildGetSerialNumber(), cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Recovery: GET_SERIAL_NUMBER sent (test response)");
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Step 4: Handshake - Re-establish connection
            await SendHandshakeAsync(cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Recovery: Handshake sent (re-establish)");
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Step 5: Re-subscribe to broadcasts
            await SetBroadcastFlagsAsync(cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Recovery: Broadcast flags set (re-subscribe)");
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Step 6: Request status - Verify connection is alive
            await GetStatusAsync(cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Recovery: Status requested (verify)");

            // Step 7: Restart keepalive timer if it was stopped
            if (_keepaliveTimer == null)
            {
                StartKeepaliveTimer();
                _logger?.LogDebug("Recovery: Keepalive timer restarted");
            }

            // Reset keepalive failure counter on successful recovery
            _keepAliveFailures = 0;

            _logger?.LogInformation("✅ Z21 recovery sequence completed - connection should be alive now");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Z21 recovery failed: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Requests version information from Z21 (serial number, hardware type, firmware version).
    /// </summary>
    private async Task RequestVersionInfoAsync(CancellationToken cancellationToken = default)
    {
        // Initialize VersionInfo if not already done
        VersionInfo ??= new Z21VersionInfo();

        // Request serial number
        await SendAsync(Z21Command.BuildGetSerialNumber(), cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("Serial number request sent");

        await Task.Delay(50, cancellationToken).ConfigureAwait(false);

        // Request hardware info (type + firmware version)
        await SendAsync(Z21Command.BuildGetHwInfo(), cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("Hardware info request sent");
    }
    #endregion

    #region Command Sending
    /// <summary>
    /// Sends a digital command to the Z21 (compat overload required by IZ21 interface).
    /// </summary>
    public Task SendCommandAsync(byte[] sendBytes)
    {
        return SendCommandAsync(sendBytes, CancellationToken.None);
    }

    /// <summary>
    /// Sends a digital command to the Z21 with optional cancellation support.
    /// </summary>
    /// <param name="sendBytes">The byte sequence containing the command for the Z21.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendCommandAsync(byte[] sendBytes, CancellationToken cancellationToken)
    {
        await SendAsync(sendBytes, cancellationToken).ConfigureAwait(false);
    }
    #endregion
}

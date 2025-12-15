// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Network;
using Moba.Backend.Protocol;
using Moba.Backend.Service;

using System.Net;

namespace Moba.Backend;

public class Z21 : IZ21
{
    public event Feedback? Received;
    public event SystemStateChanged? OnSystemStateChanged;
    public event XBusStatusChanged? OnXBusStatusChanged;
    public event VersionInfoChanged? OnVersionInfoChanged;
    public event Action? OnConnectionLost;

    private readonly IUdpClientWrapper _udp;
    private readonly ILogger<Z21>? _logger;
    private readonly Z21Monitor? _trafficMonitor;
    private CancellationTokenSource? _cancellationTokenSource;
    private Timer? _keepaliveTimer;
    private int _keepAliveFailures;
    private const int MAX_KEEPALIVE_FAILURES = 3;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    public Z21Monitor? TrafficMonitor => _trafficMonitor;

    /// <summary>
    /// Current version information of the Z21 (serial number, firmware, hardware).
    /// </summary>
    public Z21VersionInfo? VersionInfo { get; private set; }

    public Z21(IUdpClientWrapper udp, ILogger<Z21>? logger = null, Z21Monitor? trafficMonitor = null)
    {
        _udp = udp;
        _logger = logger;
        _trafficMonitor = trafficMonitor;
        _udp.Received += OnUdpReceived;
        _logger = logger;
    }

    /// <summary>
    /// Current system state of the Z21 (voltage, current, temperature, etc.)
    /// </summary>
    public SystemState CurrentSystemState { get; private set; } = new SystemState();

    /// <summary>
    /// Indicates whether the Z21 is currently connected.
    /// </summary>
    public bool IsConnected => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

    /// <summary>
    /// Connect to Z21.
    /// Sets broadcast flags to receive all events, which keeps the connection alive automatically.
    /// Also starts a keepalive timer that sends periodic status requests every 30 seconds.
    /// </summary>
    /// <param name="address">IP address of the Z21.</param>
    /// <param name="port">UDP port of the Z21 (default: 21105).</param>
    /// <param name="cancellationToken">Enables the controlled cancellation of long-running operations.</param>
    public async Task ConnectAsync(IPAddress address, int port = Z21Protocol.DefaultPort, CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await _udp.ConnectAsync(address, port, _cancellationTokenSource.Token).ConfigureAwait(false);
        
        // Small delay between commands to prevent Z21 overload
        await SendHandshakeAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        await Task.Delay(50, _cancellationTokenSource.Token).ConfigureAwait(false);
        
        await SetBroadcastFlagsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        await Task.Delay(50, _cancellationTokenSource.Token).ConfigureAwait(false);
        
        // Request initial status
        await GetStatusAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        
        // Request version information (serial number, hardware type, firmware version)
        await Task.Delay(50, _cancellationTokenSource.Token).ConfigureAwait(false);
        await RequestVersionInfoAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        
        // Start keepalive timer to prevent Z21 timeout/crash
        StartKeepaliveTimer();
        
        _logger?.LogInformation("Z21 connected to {Address}:{Port}. Keepalive timer started (30s interval)", address, port);
    }

    /// <summary>
    /// Disconnect from Z21.
    /// Sends LAN_LOGOFF to immediately free the client slot on the Z21.
    /// Without this, the Z21 waits 60 seconds before removing inactive clients.
    /// </summary>
    public async Task DisconnectAsync()
    {
        // Stop keepalive timer first
        StopKeepaliveTimer();

        // Only send LAN_LOGOFF if UDP is connected
        if (_udp.IsConnected)
        {
            // Send LAN_LOGOFF to immediately free client slot on Z21
            // This is critical for development: without it, the Z21 keeps "zombie clients"
            // and can hit the 20-client limit after many debug sessions
            try
            {
                await _udp.SendAsync(Z21Command.BuildLogoff()).ConfigureAwait(false);
                _logger?.LogInformation("LAN_LOGOFF sent to Z21");
            }
            catch (Exception ex)
            {
                // Don't fail disconnect if logoff fails (e.g., network already down)
                _logger?.LogWarning("Failed to send LAN_LOGOFF: {Message}", ex.Message);
            }
        }
        
        if (_cancellationTokenSource != null)
        {
            // Cancel and dispose safely
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
        
        // Reset failure counter
        _keepAliveFailures = 0;
        
        await _udp.StopAsync().ConfigureAwait(false);
        _logger?.LogInformation("Z21 disconnected successfully");
    }

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
            async _ => await SendKeepaliveAsync().ConfigureAwait(false),
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
    /// Sends a keepalive message (LAN_X_GET_STATUS) to the Z21.
    /// This prevents the Z21 from timing out inactive connections.
    /// Tracks failures and triggers disconnect after MAX_KEEPALIVE_FAILURES consecutive failures.
    /// </summary>
    private async Task SendKeepaliveAsync()
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
        {
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
        catch (Exception ex)
        {
            _keepAliveFailures++;
            _logger?.LogWarning("Keep-Alive failed ({Failures}/{Max}): {Message}", 
                _keepAliveFailures, MAX_KEEPALIVE_FAILURES, ex.Message);

            if (_keepAliveFailures >= MAX_KEEPALIVE_FAILURES)
            {
                _logger?.LogError("Z21 connection lost after {Max} failed Keep-Alives. Disconnecting...", 
                    MAX_KEEPALIVE_FAILURES);
                
                // Trigger disconnect on background thread to avoid deadlock
                _ = Task.Run(async () => await HandleConnectionLostAsync().ConfigureAwait(false));
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

    #region Basic Commands
    /// <summary>
    /// Thread-safe send method with SemaphoreSlim to prevent concurrent sends.
    /// </summary>
    private async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Log sent packet to traffic monitor
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

    private async Task SetBroadcastFlagsAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(Z21Command.BuildBroadcastFlagsAll(), cancellationToken).ConfigureAwait(false);
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

    #region Message Receiving & Parsing
    private void OnUdpReceived(object? sender, UdpReceivedEventArgs e)
    {
        var content = e.Buffer;
        if (content.Length < 4)
        {
            _logger?.LogWarning("Short UDP packet received {Length} bytes: {Payload}", content.Length, Z21Protocol.ToHex(content));
            return;
        }

        // Log received packet to traffic monitor
        _trafficMonitor?.LogReceivedPacket(
            content,
            Z21Monitor.ParsePacketType(content),
            $"Length: {content.Length} bytes"
        );

        // Log all received packets for debugging
        _logger?.LogDebug("UDP received {Length} bytes: {Payload}", content.Length, Z21Protocol.ToHex(content));

        if (Z21MessageParser.IsLanXHeader(content))
        {
            var xStatus = Z21MessageParser.TryParseXBusStatus(content);
            if (xStatus != null)
            {
                OnXBusStatusChanged?.Invoke(xStatus);
                _logger?.LogDebug("XBus Status: EmergencyStop={EmergencyStop}, TrackOff={TrackOff}, ShortCircuit={ShortCircuit}, Programming={Programming}", xStatus.EmergencyStop, xStatus.TrackOff, xStatus.ShortCircuit, xStatus.Programming);
            }
            return;
        }

        if (Z21MessageParser.IsRBusFeedback(content))
        {
            Received?.Invoke(new FeedbackResult(content));
            _logger?.LogDebug("RBus Feedback received");
            return;
        }

        if (Z21MessageParser.IsSystemState(content))
        {
            _logger?.LogInformation("📊 System State packet received, subscribers={Count}", OnSystemStateChanged?.GetInvocationList().Length ?? 0);

            if (Z21MessageParser.TryParseSystemState(content, out var mainCurrent, out var progCurrent, out var filteredMainCurrent, out var temperature, out var supplyVoltage, out var vccVoltage, out var centralState, out var centralStateEx))
            {
                CurrentSystemState = new SystemState
                {
                    MainCurrent = mainCurrent,
                    ProgCurrent = progCurrent,
                    FilteredMainCurrent = filteredMainCurrent,
                    Temperature = temperature,
                    SupplyVoltage = supplyVoltage,
                    VccVoltage = vccVoltage,
                    CentralState = centralState,
                    CentralStateEx = centralStateEx
                };

                _logger?.LogInformation("📊 Invoking OnSystemStateChanged: MainCurrent={MainCurrent}mA, Temp={Temp}C", mainCurrent, temperature);
                OnSystemStateChanged?.Invoke(CurrentSystemState);
                _logger?.LogDebug("System state event invoked");
            }
            else
            {
                _logger?.LogWarning("Failed to parse system state packet");
            }
            return;
        }

        // Parse serial number response
        if (Z21MessageParser.IsSerialNumber(content))
        {
            if (Z21MessageParser.TryParseSerialNumber(content, out var serialNumber))
            {
                VersionInfo ??= new Z21VersionInfo();
                VersionInfo.SerialNumber = serialNumber;
                _logger?.LogInformation("📌 Z21 Serial Number: {SerialNumber}", serialNumber);
                OnVersionInfoChanged?.Invoke(VersionInfo);
            }
            return;
        }

        // Parse hardware info response
        if (Z21MessageParser.IsHwInfo(content))
        {
            if (Z21MessageParser.TryParseHwInfo(content, out var hwType, out var fwVersion))
            {
                VersionInfo ??= new Z21VersionInfo();
                VersionInfo.HardwareTypeCode = hwType;
                VersionInfo.FirmwareVersionCode = fwVersion;
                _logger?.LogInformation("📌 Z21 Hardware: {HwType}, Firmware: {FwVersion}", VersionInfo.HardwareType, VersionInfo.FirmwareVersion);
                OnVersionInfoChanged?.Invoke(VersionInfo);
            }
            return;
        }

        _logger?.LogWarning("Unknown message: {Payload}", Z21Protocol.ToHex(content));
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
    /// Sends a digital command to the Z21.
    /// </summary>
    /// <param name="sendBytes">The byte sequence containing the command for the Z21.</param>
    public async Task SendCommandAsync(byte[] sendBytes, CancellationToken cancellationToken = default)
    {
        await SendAsync(sendBytes, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region Testing & Simulation
    /// <summary>
    /// Simulates a feedback event for testing purposes without requiring actual Z21 hardware.
    /// This triggers the same Received event as a real Z21 feedback message would.
    /// Only for testing in WinUI - not used in MOBAsmart.
    /// </summary>
    /// <param name="inPort">The InPort number (0-255) to simulate feedback for.</param>
    public void SimulateFeedback(int inPort)
    {
        if (inPort < 0 || inPort > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(inPort), "InPort must be between 0 and 255");
        }

        byte[] simulatedContent = [
            0x0F, 0x00, 0x80, 0x00,     // Header
            0x00,                       // GroupIndex
            (byte)inPort,               // InPort
            0x01,                       // Status (occupied)
            0x00, 0x00, 0x00, 0x00,     // Additional bytes
            0x00, 0x00, 0x00, 0x00      // Padding
        ];

        _logger?.LogInformation("🔔 SimulateFeedback: InPort={InPort}, Subscribers={Count}", inPort, Received?.GetInvocationList().Length ?? 0);
        System.Diagnostics.Debug.WriteLine($"🔔 SimulateFeedback: InPort={inPort}, Subscribers={Received?.GetInvocationList().Length ?? 0}");

        Received?.Invoke(new FeedbackResult(simulatedContent));

        System.Diagnostics.Debug.WriteLine($"✅ SimulateFeedback: Event invoked for InPort={inPort}");
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            StopKeepaliveTimer();
            _sendLock?.Dispose();
            try { _udp?.Dispose(); } catch { /* ignore */ }
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        _disposed = true;
    }
    #endregion
}

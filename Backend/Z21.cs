// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using CommunityToolkit.Mvvm.Messaging;
using Domain.Message;
using Interface;
using Microsoft.Extensions.Logging;
using Model;
using Network;
using Protocol;
using Service;
using System.Diagnostics;
using System.Net;

public class Z21 : IZ21

{
    public event Feedback? Received;
    public event SystemStateChanged? OnSystemStateChanged;
    public event XBusStatusChanged? OnXBusStatusChanged;
    public event VersionInfoChanged? OnVersionInfoChanged;
    public event Action? OnConnectionLost;
    public event Action<bool>? OnConnectedChanged;

    private readonly IUdpClientWrapper _udp;
    private readonly ILogger<Z21>? _logger;
    private readonly Z21Monitor? _trafficMonitor;
    private CancellationTokenSource? _cancellationTokenSource;
    private Timer? _keepaliveTimer;
    private Timer? _systemStatePollingTimer;
    private int _systemStatePollingIntervalSeconds = 5;
    private int _keepAliveFailures;
    private const int MAX_KEEPALIVE_FAILURES = 3;
    
    // Lock hierarchy (acquire from top to bottom to prevent deadlock):
    // 1. _connectionLock (protects Connect/Disconnect state)
    // 2. _sendLock (protects individual UDP send operations)
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    
    private bool _disposed;
    private bool _isConnected;

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
    }

    /// <summary>
    /// Current system state of the Z21 (voltage, current, temperature, etc.)
    /// </summary>
    public SystemState CurrentSystemState { get; private set; } = new();

    /// <summary>
    /// Indicates whether the Z21 is currently connected AND has responded.
    /// Only returns true after Z21 has sent a valid response (SystemState, XBusStatus, or VersionInfo).
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Connect to Z21.
    /// Sets broadcast flags to receive all events, which keeps the connection alive automatically.
    /// Also starts a keepalive timer that sends periodic status requests every 30 seconds.
    /// 
    /// Connection is non-blocking: method returns immediately after sending initial commands.
    /// IsConnected becomes true only when Z21 responds (via OnConnectedChanged event).
    /// This matches the behavior of the official Roco Z21 app.
    /// </summary>
    /// <param name="address">IP address of the Z21.</param>
    /// <param name="port">UDP port of the Z21 (default: 21105).</param>
    /// <param name="cancellationToken">Enables the controlled cancellation of long-running operations.</param>
    public async Task ConnectAsync(IPAddress address, int port = Z21Protocol.DefaultPort, CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Reset connection state
            _isConnected = false;
            
            await _udp.ConnectAsync(address, port, _cancellationTokenSource.Token).ConfigureAwait(false);
            
            // Small delay between commands to prevent Z21 overload
            await SendHandshakeAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await Task.Delay(50, _cancellationTokenSource.Token).ConfigureAwait(false);
            
            await SetBroadcastFlagsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await Task.Delay(50, _cancellationTokenSource.Token).ConfigureAwait(false);
            
            // Request initial status - this should trigger a response
            await GetStatusAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            
            // Request version information (serial number, hardware type, firmware version)
            await Task.Delay(50, _cancellationTokenSource.Token).ConfigureAwait(false);
            await RequestVersionInfoAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

            // Start keepalive timer immediately (will also serve as connection check)
            StartKeepaliveTimer();

            // Start system state polling timer for frequent updates (current, voltage, temperature)
            StartSystemStatePollingTimer();

            _logger?.LogInformation("🔄 Z21 connection initiated to {Address}:{Port}. Waiting for response...", address, port);
            
            // Note: IsConnected will be set to true when Z21 responds (in OnUdpReceived)
            // This is handled by the _connectionTcs logic in the message handlers
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Disconnect from Z21.
    /// Sends LAN_LOGOFF to immediately free the client slot on the Z21.
    /// Without this, the Z21 waits 60 seconds before removing inactive clients.
    /// </summary>
    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Step 1: Stop timers FIRST to prevent new callbacks from starting
            StopKeepaliveTimer();
            StopSystemStatePollingTimer();
            
            // Step 2: Small delay to allow any in-flight timer callbacks to complete
            // This prevents race condition where timer callback starts just before timer.Dispose()
            await Task.Delay(100).ConfigureAwait(false);

            // Step 3: Only send LAN_LOGOFF if UDP is connected
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
            
            // Step 4: Cancel token source (this will cancel any pending async operations)
            if (_cancellationTokenSource != null)
            {
                // Cancel and dispose safely
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
            
            // Step 5: Reset connection state and failure counter
            var wasConnected = _isConnected;
            _isConnected = false;
            _keepAliveFailures = 0;
            
            if (wasConnected)
            {
                OnConnectedChanged?.Invoke(false);
            }
            
            // Step 6: Stop UDP (this sets _client = null)
            await _udp.StopAsync().ConfigureAwait(false);
            _logger?.LogInformation("Z21 disconnected successfully");
        }
        finally
        {
            _connectionLock.Release();
        }
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
            state => { _ = state; _ = SendKeepaliveAsync(); },
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
    /// </summary>
    private void SetConnectedIfNotAlready()
    {
        if (_isConnected) return;
        
        _isConnected = true;
        _logger?.LogInformation("✅ Z21 is responding - connection confirmed");
        OnConnectedChanged?.Invoke(true);
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            // UDP disconnected during timer callback - this is expected during shutdown
            _logger?.LogTrace("Keep-Alive skipped: {Message}", ex.Message);
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
            state => { _ = state; _ = SendSystemStateRequestAsync(); },
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            // UDP disconnected during timer callback - this is expected during shutdown
            _logger?.LogTrace("SystemState request skipped: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("SystemState request failed: {Message}", ex.Message);
        }
    }
    #endregion

    #region Basic Commands
    /// <summary>
    /// Thread-safe send method with SemaphoreSlim to prevent concurrent sends.
    /// </summary>
    private async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        // ✅ Validate data BEFORE acquiring lock to fail fast
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // ✅ Only call ParsePacketType when actually logging (deferred execution)
            // This prevents NullReferenceException if ParsePacketType throws
            if (_trafficMonitor != null)
            {
                _trafficMonitor.LogSentPacket(
                    data,
                    Z21Monitor.ParsePacketType(data),
                    $"Length: {data.Length} bytes"
                );
            }

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
        // ✅ Only call ParsePacketType when actually logging (deferred execution)
        if (_trafficMonitor != null)
        {
            _trafficMonitor.LogReceivedPacket(
                content,
                Z21Monitor.ParsePacketType(content),
                $"Length: {content.Length} bytes"
            );
        }

        // Log all received packets for debugging
        _logger?.LogDebug("UDP received {Length} bytes: {Payload}", content.Length, Z21Protocol.ToHex(content));

        if (Z21MessageParser.IsLanXHeader(content))
        {
            var xStatus = Z21MessageParser.TryParseXBusStatus(content);
            if (xStatus != null)
            {
                // Z21 is responding - mark as connected
                SetConnectedIfNotAlready();
                
                OnXBusStatusChanged?.Invoke(xStatus);
                _logger?.LogDebug("XBus Status: EmergencyStop={EmergencyStop}, TrackOff={TrackOff}, ShortCircuit={ShortCircuit}, Programming={Programming}", xStatus.EmergencyStop, xStatus.TrackOff, xStatus.ShortCircuit, xStatus.Programming);
            }
            return;
        }

        if (Z21MessageParser.IsRBusFeedback(content))
        {
            // Parse feedback to get InPort
            var feedback = new FeedbackResult(content);
            
            // ✅ Publish via Messenger (Feedback Event deprecated in favor of Messenger)
            WeakReferenceMessenger.Default.Send(
                new FeedbackReceivedMessage((uint)feedback.InPort, content)
            );
            
            // Keep legacy event for backward compatibility (optional, can be removed later)
            Received?.Invoke(feedback);
            
            _logger?.LogDebug("RBus Feedback received: InPort={InPort}", feedback.InPort);
            return;
        }

        if (Z21MessageParser.IsSystemState(content))
        {
            _logger?.LogInformation("📊 System State packet received, subscribers={Count}", OnSystemStateChanged?.GetInvocationList().Length ?? 0);

            if (Z21MessageParser.TryParseSystemState(content, out var mainCurrent, out var progCurrent, out var filteredMainCurrent, out var temperature, out var supplyVoltage, out var vccVoltage, out var centralState, out var centralStateEx))
            {
                // Z21 is responding - mark as connected
                SetConnectedIfNotAlready();
                
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
                // Z21 is responding - mark as connected
                SetConnectedIfNotAlready();
                
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
    /// <param name="cancellationToken"></param>
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
        Debug.WriteLine($"🔔 SimulateFeedback: InPort={inPort}, Subscribers={Received?.GetInvocationList().Length ?? 0}");

        Received?.Invoke(new FeedbackResult(simulatedContent));

        Debug.WriteLine($"✅ SimulateFeedback: Event invoked for InPort={inPort}");
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
            StopSystemStatePollingTimer();
            _sendLock.Dispose();
            _connectionLock.Dispose();
            try { _udp.Dispose(); } catch { /* ignore */ }
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        _disposed = true;
    }
    #endregion
}

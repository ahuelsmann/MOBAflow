// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Common.Events;

using Interface;

using Microsoft.Extensions.Logging;

using Model;

using Network;

using Protocol;

using Service;

using System.Net;

/// <summary>
/// Default implementation of <see cref="IZ21"/> for communicating with a Z21 command station.
/// Handles UDP transport, protocol encoding/decoding, keep‑alive, system state polling
/// and raises high‑level events for feedback, system state and version information.
///
/// The implementation is split across multiple partial files by responsibility:
/// <list type="bullet">
/// <item><description><c>Z21.cs</c> — connection lifecycle, fields, events and disposal.</description></item>
/// <item><description><c>Z21.Keepalive.cs</c> — keepalive timer and connection-confirmation logic.</description></item>
/// <item><description><c>Z21.SystemStatePolling.cs</c> — periodic system state polling.</description></item>
/// <item><description><c>Z21.Commands.cs</c> — low-level send, track power and command sending.</description></item>
/// <item><description><c>Z21.Receive.cs</c> — UDP message reception and parsing.</description></item>
/// <item><description><c>Z21.Drive.cs</c> — locomotive drive and accessory/switching commands.</description></item>
/// <item><description><c>Z21.Diagnostics.cs</c> — simulation, debugging and RailCom helpers.</description></item>
/// </list>
/// </summary>
public partial class Z21 : IZ21
{
    /// <summary>
    /// Raised when a feedback packet has been parsed into a <see cref="FeedbackResult"/>.
    /// </summary>
    public event Feedback? Received;

    /// <summary>
    /// Raised when the Z21 reports a new system state snapshot.
    /// </summary>
    public event SystemStateChanged? OnSystemStateChanged;

    /// <summary>
    /// Raised when the X‑Bus status flags change (emergency stop, track off, etc.).
    /// </summary>
    public event XBusStatusChanged? OnXBusStatusChanged;

    /// <summary>
    /// Raised when version information (serial number, firmware, hardware) changes.
    /// </summary>
    public event VersionInfoChanged? OnVersionInfoChanged;

    /// <summary>
    /// Raised when the connection is lost unexpectedly.
    /// </summary>
    public event Action? OnConnectionLost;

    /// <summary>
    /// Raised when the logical connection state changes (true = connected and responding).
    /// </summary>
    public event Action<bool>? OnConnectedChanged;

    private readonly IUdpClientWrapper _udp;
    private readonly IEventBus _eventBus;
    private readonly ILogger<Z21>? _logger;
    private readonly Z21Monitor? _trafficMonitor;
    private CancellationTokenSource? _cancellationTokenSource;
    private Timer? _keepaliveTimer;
    private Timer? _systemStatePollingTimer;
    private int _systemStatePollingIntervalSeconds;
    private int _keepAliveFailures;
    private const int MaxKeepaliveFailures = 3;

    // Lock hierarchy (acquire from top to bottom to prevent deadlock):
    // 1. _connectionLock (protects Connect/Disconnect state)
    // 2. _sendLock (protects individual UDP send operations)
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private bool _disposed;
    private bool _isConnected;

    /// <summary>
    /// Gets the optional traffic monitor that records raw Z21 packets.
    /// </summary>
    public Z21Monitor? TrafficMonitor => _trafficMonitor;

    /// <summary>
    /// Current version information of the Z21 (serial number, firmware, hardware).
    /// </summary>
    public Z21VersionInfo? VersionInfo { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Z21"/> class.
    /// </summary>
    /// <param name="udp">UDP wrapper used for low-level packet transport.</param>
    /// <param name="eventBus">Event bus for publishing domain events.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="trafficMonitor">Optional traffic monitor for packet logging.</param>
    public Z21(IUdpClientWrapper udp, IEventBus eventBus, ILogger<Z21>? logger = null, Z21Monitor? trafficMonitor = null)
    {
        ArgumentNullException.ThrowIfNull(udp);
        ArgumentNullException.ThrowIfNull(eventBus);
        _udp = udp;
        _eventBus = eventBus;
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

            // SystemState polling is started AUTOMATICALLY when Z21 first responds
            // This prevents overloading the Z21 before connection is established
            // See: SetConnectedIfNotAlready()

            _logger?.LogInformation("Z21 connection initiated to {Address}:{Port}. Waiting for response", address, port);

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
                await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
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
                PublishEventAsync(new Z21ConnectionLostEvent());
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

    #region Dispose
    /// <summary>
    /// Disposes timers, cancellation tokens and synchronization primitives.
    /// Does not own the underlying UDP wrapper or event bus.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        StopKeepaliveTimer();
        StopSystemStatePollingTimer();

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        _sendLock.Dispose();
        _connectionLock.Dispose();

        _disposed = true;
    }

    #endregion
}
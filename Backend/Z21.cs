using System.Net;
using Moba.Backend.Interface;
using Moba.Backend.Network;
using Moba.Backend.Protocol;
using Microsoft.Extensions.Logging;

namespace Moba.Backend;

public class Z21 : IZ21
{
    public event Feedback? Received;
    public event SystemStateChanged? OnSystemStateChanged;
    public event XBusStatusChanged? OnXBusStatusChanged;

    private readonly IUdpClientWrapper _udp;
    private readonly ILogger<Z21>? _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    public Z21(IUdpClientWrapper udp, ILogger<Z21>? logger = null)
    {
        _udp = udp;
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
    /// </summary>
    /// <param name="address">IP address of the Z21.</param>
    /// <param name="cancellationToken">Enables the controlled cancellation of long-running operations.</param>
    public async Task ConnectAsync(IPAddress address, CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await _udp.ConnectAsync(address, Z21Protocol.DefaultPort, _cancellationTokenSource.Token).ConfigureAwait(false);
        await SendHandshakeAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        await SetBroadcastFlagsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        _logger?.LogInformation("Z21 connected. Broadcast events will keep connection alive");
    }

    /// <summary>
    /// Disconnect from Z21.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_cancellationTokenSource != null)
        {
            // Cancel and dispose safely
            _cancellationTokenSource.Cancel();
        }
        await _udp.StopAsync().ConfigureAwait(false);
        _logger?.LogInformation("Z21 disconnected successfully");
    }

    #region Basic Commands
    private async Task SendHandshakeAsync(CancellationToken cancellationToken = default)
    {
        await _udp.SendAsync(Z21Command.BuildHandshake(), cancellationToken).ConfigureAwait(false);
    }

    private async Task SetBroadcastFlagsAsync(CancellationToken cancellationToken = default)
    {
        await _udp.SendAsync(Z21Command.BuildBroadcastFlagsAll(), cancellationToken).ConfigureAwait(false);
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
                
                _logger?.LogInformation("📊 Invoking OnSystemStateChanged: MainCurrent={MainCurrent}mA, Temp={Temp}°C", mainCurrent, temperature);
                OnSystemStateChanged?.Invoke(CurrentSystemState);
                _logger?.LogDebug("System state event invoked");
            }
            else
            {
                _logger?.LogWarning("Failed to parse system state packet");
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
        await _udp.SendAsync(sendBytes, cancellationToken).ConfigureAwait(false);
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

        byte[] simulatedContent = new byte[] {
            0x0F, 0x00, 0x80, 0x00,     // Header
            0x00,                       // GroupIndex
            (byte)inPort,               // InPort
            0x01,                       // Status (occupied)
            0x00, 0x00, 0x00, 0x00,     // Additional bytes
            0x00, 0x00, 0x00, 0x00      // Padding
        };

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
            try { _udp?.Dispose(); } catch { /* ignore */ }
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        _disposed = true;
    }

    #endregion
}
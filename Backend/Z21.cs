using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Moba.Backend.Interface;

namespace Moba.Backend;

/// <summary>
/// This class enables bidirectional communication via UDP with a Z21 digital control center in your network.
/// Implements Z21 LAN Protocol Specification V1.13.
/// Connection is kept alive by broadcast events (no explicit keep-alive ping required).
/// </summary>
public class Z21 : IZ21
{
    public event Feedback? Received;
    public event SystemStateChanged? OnSystemStateChanged;

    private UdpClient? _client;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiverTask;
    private bool _disposed;

    /// <summary>
    /// Current system state of the Z21 (voltage, current, temperature, etc.)
    /// </summary>
    public SystemState CurrentSystemState { get; private set; } = new SystemState();

    /// <summary>
    /// Indicates whether the Z21 is currently connected.
    /// </summary>
    public bool IsConnected => _client != null && _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

    /// <summary>
    /// Connect to Z21.
    /// Sets broadcast flags to receive all events, which keeps the connection alive automatically.
    /// </summary>
    /// <param name="address">IP address of the Z21.</param>
    /// <param name="cancellationToken">Enables the controlled cancellation of long-running operations.</param>
    public async Task ConnectAsync(IPAddress address, CancellationToken cancellationToken = default)
    {
        _client = new UdpClient();
        _client.Connect(address, 21105);
        _client.DontFragment = false;
        _client.EnableBroadcast = false;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _receiverTask = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        await SendHandshakeAsync();
        await SetBroadcastFlagsAsync();
        
        Debug.WriteLine("✅ Z21 connected (broadcast events will keep connection alive)");
    }

    /// <summary>
    /// Disconnect from Z21.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
        }

        // Wait for receiver task, but ignore OperationCanceledException
        if (_receiverTask != null)
        {
            try
            {
                await _receiverTask;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("📡 Receiver task cancelled");
            }
        }

        _client?.Close();
        _client = null;

        Debug.WriteLine("✅ Z21 disconnected successfully");
    }

    #region Basic Commands
    private async Task SendHandshakeAsync()
    {
        // LAN_SYSTEMSTATE_GETDATA (0x85)
        byte[] sendBytes = new byte[] { 0x04, 0x00, 0x85, 0x00 };
        if (_client != null)
        {
            await _client.SendAsync(sendBytes, sendBytes.Length);
        }
    }

    private async Task SetBroadcastFlagsAsync()
    {
        // LAN_SET_BROADCASTFLAGS (0x50) - Subscribe to all events
        // These events keep the connection alive automatically - no explicit ping needed!
        byte[] sendBytes = new byte[] { 0x08, 0x00, 0x50, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };
        if (_client != null)
        {
            await _client.SendAsync(sendBytes, sendBytes.Length);
        }
    }
    #endregion

    #region Track Power Control
    /// <summary>
    /// Turns the track power ON.
    /// LAN_X_SET_TRACK_POWER_ON (X-Header: 0x21, DB0: 0x81)
    /// </summary>
    public async Task SetTrackPowerOnAsync()
    {
        byte[] sendBytes = new byte[] { 0x07, 0x00, 0x40, 0x00, 0x21, 0x81, 0xA0 };
        await SendCommandAsync(sendBytes);
        Debug.WriteLine("🔌 Track power ON command sent");
    }

    /// <summary>
    /// Turns the track power OFF.
    /// LAN_X_SET_TRACK_POWER_OFF (X-Header: 0x21, DB0: 0x80)
    /// </summary>
    public async Task SetTrackPowerOffAsync()
    {
        byte[] sendBytes = new byte[] { 0x07, 0x00, 0x40, 0x00, 0x21, 0x80, 0xA1 };
        await SendCommandAsync(sendBytes);
        Debug.WriteLine("🔌 Track power OFF command sent");
    }

    /// <summary>
    /// Triggers an emergency stop (locomotives stop but track power remains on).
    /// LAN_X_SET_STOP (X-Header: 0x80)
    /// </summary>
    public async Task SetEmergencyStopAsync()
    {
        byte[] sendBytes = new byte[] { 0x06, 0x00, 0x40, 0x00, 0x80, 0x80 };
        await SendCommandAsync(sendBytes);
        Debug.WriteLine("🔴 Emergency stop command sent");
    }

    /// <summary>
    /// Requests the current Z21 status.
    /// LAN_X_GET_STATUS (X-Header: 0x21, DB0: 0x24)
    /// </summary>
    public async Task GetStatusAsync()
    {
        byte[] sendBytes = new byte[] { 0x07, 0x00, 0x40, 0x00, 0x21, 0x24, 0x05 };
        await SendCommandAsync(sendBytes);
        Debug.WriteLine("📊 Status request sent");
    }
    #endregion

    #region Message Receiving & Parsing

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _client != null)
        {
            UdpReceiveResult result = await _client.ReceiveAsync(cancellationToken);
            byte[] content = result.Buffer;

            if (content.Length < 4)
            {
                Debug.WriteLine($"⚠ Short UDP packet received ({content.Length} bytes): {BitConverter.ToString(content)}");
                continue;
            }

            string header = BitConverter.ToString(content, 0, 4);

            Debug.WriteLine($"📨 Received: {header}");

            switch (header)
            {
                case "07-00-40-00":
                    ParseXBusMessage(content);
                    break;
                case "08-00-50-00":
                    Debug.WriteLine("🛠 Broadcast flags set");
                    break;
                case "0F-00-80-00":
                    // R-BUS Feedback event
                    Received?.Invoke(new FeedbackResult(content));
                    break;
                case "14-00-84-00":
                    Debug.WriteLine("🔄 System state changed");
                    ParseSystemStateChange(content);
                    break;
                default:
                    Debug.WriteLine($"⚠ Unknown message: {BitConverter.ToString(content)}");
                    break;
            }
        }
    }

    private static void ParseXBusMessage(byte[] data)
    {
        if (data.Length < 6) return;

        byte xHeader = data[4];
        byte db0 = data[5];

        switch (xHeader)
        {
            case 0x61: // Status messages
                switch (db0)
                {
                    case 0x00:
                        Debug.WriteLine("🔴 Track power OFF");
                        break;
                    case 0x01:
                        Debug.WriteLine("✅ Track power ON");
                        break;
                    case 0x02:
                        Debug.WriteLine("🛠 Programming mode active");
                        break;
                    case 0x08:
                        Debug.WriteLine("⚡ Short circuit detected!");
                        break;
                    case 0x82:
                        Debug.WriteLine("❓ Unknown X-BUS command");
                        break;
                }
                break;

            case 0x81: // Emergency stop
                Debug.WriteLine("🔴 Emergency stop active");
                break;

            case 0x62: // Status changed
                if (data.Length >= 7)
                {
                    byte status = data[6];
                    bool emergencyStop = (status & 0x01) != 0;
                    bool trackOff = (status & 0x02) != 0;
                    bool shortCircuit = (status & 0x04) != 0;
                    bool programming = (status & 0x20) != 0;

                    Debug.WriteLine($"📊 Status: EmergencyStop={emergencyStop}, TrackOff={trackOff}, ShortCircuit={shortCircuit}, Programming={programming}");
                }
                break;
        }
    }

    private void ParseSystemStateChange(byte[] data)
    {
        if (data.Length < 20) return;

        int mainCurrent = BitConverter.ToInt16(data, 4);
        int progCurrent = BitConverter.ToInt16(data, 6);
        int filteredMainCurrent = BitConverter.ToInt16(data, 8);
        int temperature = BitConverter.ToInt16(data, 10);
        int supplyVoltage = BitConverter.ToUInt16(data, 12);
        int vccVoltage = BitConverter.ToUInt16(data, 14);
        byte centralState = data[16];
        byte centralStateEx = data[17];

        Debug.WriteLine($"📊 SystemState:");
        Debug.WriteLine($"   Main Current: {mainCurrent} mA");
        Debug.WriteLine($"   Temperature: {temperature} °C");
        Debug.WriteLine($"   Supply Voltage: {supplyVoltage} mV");
        Debug.WriteLine($"   VCC Voltage: {vccVoltage} mV");
        Debug.WriteLine($"   State: 0x{centralState:X2}, StateEx: 0x{centralStateEx:X2}");

        // Update CurrentSystemState and raise event
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

        OnSystemStateChanged?.Invoke(CurrentSystemState);
    }

    #endregion

    #region Command Sending

    /// <summary>
    /// Sends a digital command to the Z21.
    /// </summary>
    /// <param name="sendBytes">The byte sequence containing the command for the Z21.</param>
    public async Task SendCommandAsync(byte[] sendBytes)
    {
        if (_client != null)
        {
            await _client.SendAsync(sendBytes, sendBytes.Length);
        }
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

        Debug.WriteLine($"⚡ Simulating feedback for InPort {inPort}");
        Received?.Invoke(new FeedbackResult(simulatedContent));
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
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _client?.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
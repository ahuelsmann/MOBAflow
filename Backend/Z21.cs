using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Moba.Backend;

public delegate void Feedback(FeedbackResult feedbackContent);

public class Z21 : IDisposable
{
    public event Feedback? Received;
    
    private UdpClient? _client;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiverTask;
    private Task? _pingTask;
    private bool _disposed;

    public async Task ConnectAsync(IPAddress address, CancellationToken cancellationToken = default)
    {
        _client = new UdpClient();
        _client.Connect(address, 21105);
        _client.DontFragment = false;
        _client.EnableBroadcast = false;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _receiverTask = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        await SendHandshakeAsync();

        _pingTask = Task.Run(() => SendPingAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        await SetBroadcastFlagsAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
        }

        if (_receiverTask != null)
        {
            await _receiverTask;
        }

        if (_pingTask != null)
        {
            await _pingTask;
        }

        _client?.Close();
        _client = null;
    }

    private async Task SendHandshakeAsync()
    {
        byte[] sendBytes = [0x04, 0x00, 0x85, 0x00];
        if (_client != null)
        {
            await _client.SendAsync(sendBytes, sendBytes.Length);
        }
    }

    private async Task SetBroadcastFlagsAsync()
    {
        byte[] sendBytes = [0x08, 0x00, 0x50, 0x00, 0xFF, 0xFF, 0xFF, 0xFF];
        if (_client != null)
        {
            await _client.SendAsync(sendBytes, sendBytes.Length);
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _client != null)
        {
            try
            {
                UdpReceiveResult result = await _client.ReceiveAsync(cancellationToken);
                byte[] content = result.Buffer;

                string valuesHexadecimal = string.Join(",", content.Select(b => b.ToString("X2")));
                string valuesDecimal = string.Join(",", content);
                string header = BitConverter.ToString(content, 0, 4);
                
                Debug.WriteLine($"Empfangene Nachricht: {header}");
                Debug.WriteLine(valuesHexadecimal);
                Debug.WriteLine(valuesDecimal);
                Debug.WriteLine("");

                switch (header)
                {
                    case "07-00-40-00":
                        Debug.WriteLine("🔴 Z21 Notstopp erkannt.");
                        ParseStopMessage(content);
                        break;
                    case "08-00-50-00":
                        Debug.WriteLine("🛠 Z21 Broadcast Flags wurden gesetzt.");
                        break;
                    case "04-00-51-00":
                        Debug.WriteLine("📡 Z21 Broadcast Flags wurden abgefragt.");
                        break;
                    case "08-00-61-00":
                        Debug.WriteLine("⚡ Kurzschluss erkannt!");
                        break;
                    case "08-00-62-00":
                        Debug.WriteLine("🔌 Gleisspannung und Stromwerte empfangen.");
                        ParseVoltageAndCurrent(content);
                        break;
                    case "0C-00-30-00":
                        Debug.WriteLine("🚂 Lok-Informationen empfangen.");
                        ParseLocoInfo(content);
                        break;
                    case "0F-00-80-00":
                        Received?.Invoke(new FeedbackResult(content));
                        break;
                    case "14-00-84-00":
                        Debug.WriteLine("🔄 Z21 Systemstatus geändert!");
                        ParseSystemStateChange(content);
                        break;
                    default:
                        Debug.WriteLine($"⚠ Unbekannte Nachricht: {BitConverter.ToString(content)}");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Fehler beim Empfangen: {ex.Message}");
            }
        }
    }

    private static void ParseStopMessage(byte[] data)
    {
        if (data.Length > 7)
        {
            Debug.WriteLine($"Zusätzliche Statusdaten beim Notstopp: {BitConverter.ToString(data, 7)}");
        }
    }

    private static void ParseVoltageAndCurrent(byte[] data)
    {
        if (data.Length >= 10)
        {
            int voltage = data[4] + (data[5] << 8);
            int current = data[6] + (data[7] << 8);
            Debug.WriteLine($"⚡ Spannung: {voltage} mV, Strom: {current} mA");
        }
    }

    private static void ParseLocoInfo(byte[] data)
    {
        if (data.Length >= 12)
        {
            int locoAddress = data[4] + (data[5] << 8);
            int speed = data[6];
            bool direction = (data[7] & 0x80) != 0;
            Debug.WriteLine($"🚂 Lok-Adresse: {locoAddress}, Geschwindigkeit: {speed}, Richtung: {(direction ? "Vorwärts" : "Rückwärts")}");
        }
    }

    private static void ParseSystemStateChange(byte[] data)
    {
        if (data.Length < 20) return;

        int trackPowerStatus = data[4];
        int shortCircuitStatus = data[5];
        int voltage = data[6] + (data[7] << 8);
        int current = data[8] + (data[9] << 8);

        Debug.WriteLine($"🔌 Gleisspannung: {(trackPowerStatus == 1 ? "An" : "Aus")}");
        Debug.WriteLine($"⚡ Kurzschluss: {(shortCircuitStatus != 0 ? "Ja" : "Nein")}");
        Debug.WriteLine($"🔋 Spannung: {voltage} mV");
        Debug.WriteLine($"🔋 Strom: {current} mA");
    }

    private async Task SendPingAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _client != null)
        {
            try
            {
                await Task.Delay(60000, cancellationToken);
                
                byte[] sendBytes = [0x04, 0x00, 0x1A, 0x00];
                await _client.SendAsync(sendBytes, sendBytes.Length);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Fehler beim Ping: {ex.Message}");
            }
        }
    }

    public async Task SendCommandAsync(byte[] sendBytes)
    {
        if (_client != null)
        {
            await _client.SendAsync(sendBytes, sendBytes.Length);
        }
    }

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
}
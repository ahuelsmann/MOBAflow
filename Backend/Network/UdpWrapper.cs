using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Moba.Backend.Network;

public class UdpReceivedEventArgs : EventArgs
{
    public byte[] Buffer { get; }
    public IPEndPoint RemoteEndPoint { get; }

    public UdpReceivedEventArgs(byte[] buffer, IPEndPoint remote)
    {
        Buffer = buffer;
        RemoteEndPoint = remote;
    }
}

public class UdpWrapper : IUdpClientWrapper
{
    private UdpClient? _client;
    private CancellationTokenSource? _cts;
    private Task? _receiverTask;
    private bool _disposed;

    /// <summary>
    /// Raised for each received UDP datagram.
    /// </summary>
    public event EventHandler<UdpReceivedEventArgs>? Received;

    /// <summary>
    /// Connects the wrapper to the remote endpoint and starts the receiver loop.
    /// </summary>
    public Task ConnectAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default)
    {
        _client = new UdpClient();
        _client.Connect(address, port);
        _client.DontFragment = false;
        _client.EnableBroadcast = false;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiverTask = Task.Run(() => ReceiverLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    private async Task ReceiverLoopAsync(CancellationToken cancellationToken)
    {
        Debug.WriteLine("UdpWrapper: Receiver loop started");

        try
        {
            while (!cancellationToken.IsCancellationRequested && _client != null)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Deliver data to subscribers
                try
                {
                    var buffer = result.Buffer;
                    Received?.Invoke(this, new UdpReceivedEventArgs(buffer, result.RemoteEndPoint));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UdpWrapper: Exception delivering received datagram: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UdpWrapper: Receiver loop failed: {ex.Message}");
        }

        Debug.WriteLine("UdpWrapper: Receiver loop stopped");
    }

    /// <summary>
    /// Sends a datagram with simple retry/backoff.
    /// </summary>
    public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default, int maxRetries = 3)
    {
        if (_client == null) throw new InvalidOperationException("UdpWrapper is not connected");

        int attempt = 0;
        int delayMs = 50;
        while (true)
        {
            try
            {
                await _client.SendAsync(data, data.Length).ConfigureAwait(false);
                return;
            }
            catch (SocketException ex) when (attempt < maxRetries)
            {
                attempt++;
                Debug.WriteLine($"UdpWrapper: Send attempt {attempt} failed: {ex.Message}. Retrying in {delayMs}ms");
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                delayMs *= 2;
            }
        }
    }

    public async Task StopAsync()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            try
            {
                if (_receiverTask != null)
                {
                    await _receiverTask.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"UdpWrapper: StopAsync receiver error: {ex.Message}");
            }
        }

        _client?.Close();
        _client = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cts?.Cancel();
        _cts?.Dispose();
        _client?.Dispose();
        _disposed = true;
    }
}

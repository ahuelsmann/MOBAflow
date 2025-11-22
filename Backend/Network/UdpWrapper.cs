// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Moba.Backend.Network;

/// <summary>
/// Platform-independent UDP client wrapper for Z21 communication.
/// 
/// Purpose:
/// - Abstracts UDP socket complexity (connect, send, receive, disconnect)
/// - Runs a background receiver loop that fires events for incoming datagrams
/// - Implements retry logic for sending with exponential backoff
/// 
/// Architecture:
/// - Thread-safe: receiver loop runs on background thread
/// - Events are delivered on background thread (caller must dispatch to UI thread if needed)
/// - Backend must remain platform-independent (no MainThread, DispatcherQueue, etc.)
/// 
/// Usage Pattern:
/// 1. Subscribe to Received event
/// 2. Call ConnectAsync(ipAddress, port)
/// 3. Handle Received events (on background thread!)
/// 4. Platform-specific ViewModels dispatch to UI thread if needed
/// 5. Call StopAsync() or Dispose() to cleanup
/// 
/// Z21 Communication Flow:
/// UDP Socket ← ReceiverLoopAsync → Received Event → Z21.OnUdpReceived() → Parse Protocol → Fire Domain Events
/// </summary>
public class UdpWrapper : IUdpClientWrapper
{
    private UdpClient? _client;
    private CancellationTokenSource? _cts;
    private Task? _receiverTask;
    private bool _disposed;

    /// <summary>
    /// Raised for each received UDP datagram.
    /// IMPORTANT: This event is raised on a background thread!
    /// Platform-specific code must dispatch to UI thread if updating UI properties.
    /// </summary>
    public event EventHandler<UdpReceivedEventArgs>? Received;

    /// <summary>
    /// Connects the wrapper to the remote endpoint and starts the receiver loop.
    /// </summary>
    /// <param name="address">Z21 IP address</param>
    /// <param name="port">Z21 UDP port (default: 21105)</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
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

    /// <summary>
    /// Background loop that continuously receives UDP datagrams and raises Received events.
    /// Runs on a background thread until cancellation is requested or an unrecoverable error occurs.
    /// </summary>
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

                // Deliver data to subscribers (on background thread)
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
    /// Sends a UDP datagram with retry logic and exponential backoff.
    /// Retries up to maxRetries times with delays: 50ms, 100ms, 200ms, etc.
    /// </summary>
    /// <param name="data">Byte array to send to Z21</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <exception cref="InvalidOperationException">Thrown if not connected</exception>
    /// <exception cref="SocketException">Thrown if all retry attempts fail</exception>
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

    /// <summary>
    /// Stops the receiver loop and closes the UDP client.
    /// Waits for the receiver task to complete gracefully.
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            await _cts.CancelAsync();
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

    /// <summary>
    /// Disposes resources and cancels the receiver loop.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _cts?.Cancel();
        _cts?.Dispose();
        _client?.Dispose();
        _disposed = true;
    }
}
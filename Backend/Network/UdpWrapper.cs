// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Network;

using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

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
    /// <summary>
    /// Raised when a UDP datagram has been received from the Z21.
    /// </summary>
    public event EventHandler<UdpReceivedEventArgs>? Received;

    private UdpClient? _client;
    private CancellationTokenSource? _cts;
    private Task? _receiverTask;
    private bool _disposed;
    private readonly ILogger<UdpWrapper>? _logger;

    // Performance tracking
    private int _totalSendCount;
    private int _totalRetryCount;
    private int _totalReceiveCount;
    private readonly Stopwatch _performanceTimer = Stopwatch.StartNew();
    private readonly Lock _statsLock = new();

    private void IncrementReceiveCount()
    {
        lock (_statsLock)
        {
            _totalReceiveCount++;
        }
    }

    private void IncrementSendAndLogStatsIfNeeded()
    {
        lock (_statsLock)
        {
            _totalSendCount++;
            if (_totalSendCount % 10 != 0)
            {
                return;
            }

            var elapsedSeconds = _performanceTimer.Elapsed.TotalSeconds;
            var sendsPerSecond = _totalSendCount / elapsedSeconds;
            var retriesPerSecond = _totalRetryCount / elapsedSeconds;

            _logger?.LogInformation(
                "📊 UDP Performance: {SendCount} total sends, {RetryCount} retries, {SendRate:F2} sends/sec, {RetryRate:F2} retries/sec, {ReceiveCount} receives",
                _totalSendCount, _totalRetryCount, sendsPerSecond, retriesPerSecond, _totalReceiveCount);
        }
    }

    private void IncrementRetryCount()
    {
        lock (_statsLock)
        {
            _totalRetryCount++;
        }
    }

    /// <summary>
    /// Indicates whether the UDP wrapper is connected and ready to send/receive.
    /// </summary>
    public bool IsConnected => _client != null && !_disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpWrapper"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics and performance statistics.</param>
    public UdpWrapper(ILogger<UdpWrapper>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("UdpWrapper initialized");
    }

    /// <summary>
    /// Connects the wrapper to the remote endpoint and starts the receiver loop.
    /// If already connected, closes the existing connection first.
    /// </summary>
    /// <param name="address">Z21 IP address</param>
    /// <param name="port">Z21 UDP port (default: 21105)</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public async Task ConnectAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default)
    {
        // Check if already disposed
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UdpWrapper), "Cannot connect after disposal");
        }

        // If already connected, stop the existing connection first
        if (_client != null)
        {
            _logger?.LogInformation("Closing existing connection before reconnecting...");
            await StopAsync().ConfigureAwait(false);
        }

        _logger?.LogInformation("Connecting to {Address}:{Port}", address, port);

        _client = new UdpClient();
        _client.Connect(address, port);
        _client.DontFragment = false;
        _client.EnableBroadcast = false;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiverTask = Task.Run(() => ReceiverLoopAsync(_cts.Token), _cts.Token);

        _logger?.LogInformation("UDP client connected and receiver loop started");
    }

    /// <summary>
    /// Background loop that continuously receives UDP datagrams and raises Received events.
    /// Runs on a background thread until cancellation is requested or an unrecoverable error occurs.
    /// </summary>
    private async Task ReceiverLoopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("🔄 UDP Receiver loop started");

        try
        {
            while (!cancellationToken.IsCancellationRequested && _client != null)
            {
                UdpReceiveResult result;
                try
                {
                    // Check cancellation before blocking receive
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger?.LogDebug("Receiver loop cancelled before receive");
                        break;
                    }

                    result = await _client.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                    IncrementReceiveCount();

                    _logger?.LogDebug("📥 Received {Length} bytes from {Endpoint}: {Data}",
                        result.Buffer.Length,
                        result.RemoteEndPoint,
                        BitConverter.ToString(result.Buffer).Replace("-", " "));
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug("Receiver loop cancelled during receive");
                    break;
                }
                catch (SocketException ex)
                {
                    _logger?.LogError("❌ Socket error in receiver loop: {Error}", ex.Message);
                    break;
                }

                Received?.Invoke(this, new UdpReceivedEventArgs(result.Buffer, result.RemoteEndPoint));
            }
        }
        finally
        {
            _logger?.LogInformation("🛑 UDP Receiver loop stopped. Stats: {SendCount} sends, {RetryCount} retries, {ReceiveCount} receives",
                _totalSendCount, _totalRetryCount, _totalReceiveCount);
        }
    }

    /// <summary>
    /// Sends a UDP datagram with retry logic and exponential backoff.
    /// Retries up to maxRetries times with delays: 50ms, 100ms, 200ms, etc.
    /// </summary>
    /// <param name="data">Byte array to send to Z21</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <exception cref="UdpNotConnectedException">Thrown if not connected</exception>
    /// <exception cref="SocketException">Thrown if all retry attempts fail</exception>
    public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default, int maxRetries = 3)
    {
        // Thread-safe check: Capture client reference to prevent null reference during disconnect
        var client = _client;
        if (client == null || _disposed)
        {
            throw new UdpNotConnectedException();
        }

        var sendStartTime = Stopwatch.StartNew();
        int attempt = 0;
        int delayMs = 50;

        IncrementSendAndLogStatsIfNeeded();

        _logger?.LogDebug("📤 Sending {Length} bytes (attempt 1/{MaxRetries}): {Data}",
            data.Length, maxRetries, BitConverter.ToString(data).Replace("-", " "));

        while (true)
        {
            try
            {
                // Use captured client reference for thread-safety
                await client.SendAsync(data, cancellationToken).ConfigureAwait(false);

                sendStartTime.Stop();
                _logger?.LogDebug("✅ Send successful in {ElapsedMs}ms", sendStartTime.ElapsedMilliseconds);

                return;
            }
            catch (SocketException ex) when (attempt < maxRetries)
            {
                attempt++;

                IncrementRetryCount();

                _logger?.LogWarning("⚠️ Send attempt {Attempt}/{MaxRetries} failed: {Error}. Retrying in {DelayMs}ms",
                    attempt, maxRetries, ex.Message, delayMs);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                delayMs *= 2;
            }
        }
    }

    /// <summary>
    /// Stops the receiver loop and closes the UDP client.
    /// Waits for the receiver task to complete gracefully.
    /// Allows reconnection after stop (unlike Dispose which is final).
    /// </summary>
    public async Task StopAsync()
    {
        _logger?.LogDebug("StopAsync called - stopping UDP client");

        // Cancel the receiver task first
        if (_cts is { IsCancellationRequested: false })
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            try
            {
                await WaitForReceiverTaskAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogWarning("StopAsync receiver error: {Message}", ex.Message);
            }
        }

        DisposeClientResources();

        _logger?.LogInformation("UDP client stopped successfully");
    }

    private async Task WaitForReceiverTaskAsync()
    {
        if (_receiverTask == null)
        {
            return;
        }

        var completedTask = await Task.WhenAny(_receiverTask, Task.Delay(2000)).ConfigureAwait(false);
        if (completedTask != _receiverTask)
        {
            _logger?.LogWarning("Receiver task did not complete within timeout");
        }
    }

    /// <summary>
    /// Disposes resources and cancels the receiver loop.
    /// After Dispose, the wrapper cannot be reused.
    /// Calls StopAsync() synchronously to ensure proper cleanup.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose must remain synchronous. Perform a best-effort, non-blocking shutdown.
        try
        {
            if (_cts is { IsCancellationRequested: false })
            {
                _cts.Cancel();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during dispose");
        }

        DisposeClientResources();
        _logger?.LogInformation("UdpWrapper disposed");
    }

    private void DisposeClientResources()
    {
        _cts?.Dispose();
        _cts = null;
        _receiverTask = null;

        if (_client == null)
        {
            return;
        }

        try
        {
            _client.Close();
            _client.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Error closing UDP client: {Message}", ex.Message);
        }

        _client = null;
    }
}

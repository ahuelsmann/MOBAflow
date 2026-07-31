// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// Provides one connected UDP boundary for the versioned display protocol.
/// </summary>
public sealed class UdpDisplayDatagramTransport : IDisplayDatagramTransport, IDisposable
{
    private readonly UdpClient _client;
    private readonly CancellationTokenSource _receiveCancellation = new();
    private readonly Task _receiveTask;
    private bool _disposed;

    /// <summary>
    /// Initializes and starts a connected UDP receive boundary.
    /// </summary>
    /// <param name="address">Configured display IP address.</param>
    /// <param name="port">Configured display UDP port.</param>
    public UdpDisplayDatagramTransport(IPAddress address, int port)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentOutOfRangeException.ThrowIfLessThan(port, IPEndPoint.MinPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, IPEndPoint.MaxPort);

        _client = new UdpClient(address.AddressFamily);
        _client.Connect(new IPEndPoint(address, port));
        _receiveTask = RunReceiveLoopAsync(
            ReceiveDatagramAsync,
            RaiseDatagramReceived,
            _receiveCancellation.Token);
    }

    /// <inheritdoc />
    public event EventHandler<DisplayDatagramReceivedEventArgs>? DatagramReceived;

    /// <inheritdoc />
    public async ValueTask SendAsync(
        ReadOnlyMemory<byte> datagram,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _client.SendAsync(datagram, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stops receiving and releases the UDP socket.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _receiveCancellation.Cancel();
        _client.Dispose();
        _receiveCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    internal static async Task RunReceiveLoopAsync(
        Func<CancellationToken, ValueTask<ReadOnlyMemory<byte>>> receiveAsync,
        Action<ReadOnlyMemory<byte>> onDatagram,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receiveAsync);
        ArgumentNullException.ThrowIfNull(onDatagram);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var datagram = await receiveAsync(cancellationToken).ConfigureAwait(false);
                onDatagram(datagram);
            }
            catch (SocketException) when (!cancellationToken.IsCancellationRequested)
            {
                // Connected UDP reports remote ICMP failures here. Keep receiving for endpoint recovery.
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async ValueTask<ReadOnlyMemory<byte>> ReceiveDatagramAsync(
        CancellationToken cancellationToken)
    {
        var result = await _client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        return result.Buffer;
    }

    private void RaiseDatagramReceived(ReadOnlyMemory<byte> datagram)
    {
        var eventArgs = new DisplayDatagramReceivedEventArgs(datagram);
        DatagramReceived?.Invoke(this, eventArgs);
    }
}
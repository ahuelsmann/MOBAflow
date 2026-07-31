// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using Moba.Display.Runtime;

using System.Net;

/// <summary>
/// Sends frames through the single production protocol v1.0 UDP path.
/// </summary>
public sealed class UdpDisplayFrameSender : IFrameSender, IDisposable
{
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private UdpDisplayDatagramTransport? _transport;
    private DisplayProtocolClient? _client;
    private DisplayProtocolFrameSession? _session;
    private string? _endpointKey;
    private bool _disposed;

    /// <inheritdoc />
    public async Task SendFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        FrameLoopOptions options,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);
        if (!IPAddress.TryParse(options.IpAddress, out var address))
        {
            throw new ArgumentException("Invalid display IP address format.", nameof(options));
        }

        if (options.Width is <= 0 or > ushort.MaxValue
            || options.Height is <= 0 or > ushort.MaxValue)
        {
            throw new ArgumentException("Display dimensions are outside the protocol range.", nameof(options));
        }

        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureSession(address, options.Port);
            await _session!.SendFrameAsync(
                rgb565Frame,
                (ushort)options.Width,
                (ushort)options.Height,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    /// <summary>Releases the active protocol session and UDP transport.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeSession();
        _sendGate.Dispose();
        GC.SuppressFinalize(this);
    }

    private void EnsureSession(IPAddress address, int port)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(port, IPEndPoint.MinPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, IPEndPoint.MaxPort);
        var endpointKey = $"{address}:{port}";
        if (string.Equals(_endpointKey, endpointKey, StringComparison.Ordinal))
        {
            return;
        }

        DisposeSession();
        _transport = new UdpDisplayDatagramTransport(address, port);
        _client = new DisplayProtocolClient(_transport);
        _session = new DisplayProtocolFrameSession(_client);
        _endpointKey = endpointKey;
    }

    private void DisposeSession()
    {
        _client?.Dispose();
        _transport?.Dispose();
        _session = null;
        _client = null;
        _transport = null;
        _endpointKey = null;
    }
}
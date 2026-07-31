// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using Moba.Display.Runtime;

using System.Net;

internal interface IDisplayFrameSessionConnection : IDisposable
{
    Task SendFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        ushort width,
        ushort height,
        CancellationToken cancellationToken);
}

/// <summary>
/// Sends frames through the single production protocol v1.0 UDP path.
/// </summary>
public sealed class UdpDisplayFrameSender : IFrameSender, IDisposable
{
    private readonly Func<IPEndPoint, IDisplayFrameSessionConnection> _connectionFactory;
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private IDisplayFrameSessionConnection? _connection;
    private IPEndPoint? _endpoint;
    private bool _disposed;

    /// <summary>Initializes the production UDP display sender.</summary>
    public UdpDisplayFrameSender()
        : this(CreateConnection)
    {
    }

    internal UdpDisplayFrameSender(
        Func<IPEndPoint, IDisplayFrameSessionConnection> connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

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
            var connection = _connection
                ?? throw new InvalidOperationException("Display protocol session initialization failed.");
            await connection.SendFrameAsync(
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
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, IPEndPoint.MaxPort);
        var endpoint = new IPEndPoint(address, port);
        if (Equals(_endpoint, endpoint))
        {
            return;
        }

        DisposeSession();
        _connection = _connectionFactory(endpoint);
        _endpoint = endpoint;
    }

    private void DisposeSession()
    {
        _connection?.Dispose();
        _connection = null;
        _endpoint = null;
    }

    private static IDisplayFrameSessionConnection CreateConnection(IPEndPoint endpoint) =>
        new UdpDisplayFrameSessionConnection(endpoint);

    private sealed class UdpDisplayFrameSessionConnection : IDisplayFrameSessionConnection
    {
        private readonly UdpDisplayDatagramTransport _transport;
        private readonly DisplayProtocolClient _client;
        private readonly DisplayProtocolFrameSession _session;

        public UdpDisplayFrameSessionConnection(IPEndPoint endpoint)
        {
            _transport = new UdpDisplayDatagramTransport(endpoint.Address, endpoint.Port);
            _client = new DisplayProtocolClient(_transport);
            _session = new DisplayProtocolFrameSession(_client);
        }

        public Task SendFrameAsync(
            ReadOnlyMemory<byte> rgb565Frame,
            ushort width,
            ushort height,
            CancellationToken cancellationToken) =>
            _session.SendFrameAsync(rgb565Frame, width, height, cancellationToken);

        public void Dispose()
        {
            _client.Dispose();
            _transport.Dispose();
        }
    }
}
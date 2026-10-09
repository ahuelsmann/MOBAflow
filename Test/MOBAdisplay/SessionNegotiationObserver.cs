// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;

/// <summary>
/// Wraps the frame sender's datagram transport and counts protocol renegotiations for the sustained-refresh run.
/// <see cref="DisplayProtocolFrameSession"/> negotiates once and negotiates again only after the device rejected
/// its session, whichever request revealed that. Every hello sent after the first successful negotiation is
/// therefore one session loss; retries of the same hello count once.
/// </summary>
internal sealed class SessionNegotiationObserver : IDisplayDatagramTransport, IDisposable
{
    private readonly Lock _syncRoot = new();
    private readonly IDisplayDatagramTransport _inner;
    private readonly HashSet<uint> _renegotiationRequestIds = [];
    private bool _negotiated;

    public SessionNegotiationObserver(IDisplayDatagramTransport inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _inner.DatagramReceived += OnDatagramReceived;
    }

    public event EventHandler<DisplayDatagramReceivedEventArgs>? DatagramReceived;

    /// <summary>Negotiations after the first successful one.</summary>
    public int Renegotiations
    {
        get
        {
            lock (_syncRoot)
            {
                return _renegotiationRequestIds.Count;
            }
        }
    }

    public ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default)
    {
        if (TryReadHeader(datagram, out var header) && header.MessageType == DisplayMessageType.HelloRequest)
        {
            lock (_syncRoot)
            {
                if (_negotiated)
                {
                    _renegotiationRequestIds.Add(header.RequestId);
                }
            }
        }

        return _inner.SendAsync(datagram, cancellationToken);
    }

    public void Dispose() => _inner.DatagramReceived -= OnDatagramReceived;

    private void OnDatagramReceived(object? sender, DisplayDatagramReceivedEventArgs e)
    {
        if (TryReadHeader(e.Datagram, out var header) && header.MessageType == DisplayMessageType.CapabilitiesResponse)
        {
            lock (_syncRoot)
            {
                _negotiated = true;
            }
        }

        DatagramReceived?.Invoke(this, e);
    }

    private static bool TryReadHeader(ReadOnlyMemory<byte> datagram, out DisplayPacketHeader header)
    {
        var decoded = DisplayPacketCodec.TryDecode(datagram.Span, out var packet, out _);
        header = decoded && packet is not null ? packet.Header : default;
        return decoded && packet is not null;
    }
}

// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;

/// <summary>
/// Wraps the frame sender's datagram transport and counts lost protocol sessions for the sustained-refresh run.
/// A session is lost when the device answers any request of a negotiated session with
/// <see cref="DisplayResultCode.WrongSession"/>, whether that was a frame request or the abort after another failure.
/// Further rejections count again only after a new negotiation succeeded.
/// </summary>
internal sealed class SessionLossObserver : IDisplayDatagramTransport, IDisposable
{
    private readonly Lock _syncRoot = new();
    private readonly IDisplayDatagramTransport _inner;
    private bool _sessionActive;
    private int _sessionLosses;

    public SessionLossObserver(IDisplayDatagramTransport inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _inner.DatagramReceived += OnDatagramReceived;
    }

    public event EventHandler<DisplayDatagramReceivedEventArgs>? DatagramReceived;

    /// <summary>Negotiated sessions the device rejected.</summary>
    public int SessionLosses
    {
        get
        {
            lock (_syncRoot)
            {
                return _sessionLosses;
            }
        }
    }

    public ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default) =>
        _inner.SendAsync(datagram, cancellationToken);

    public void Dispose() => _inner.DatagramReceived -= OnDatagramReceived;

    private void OnDatagramReceived(object? sender, DisplayDatagramReceivedEventArgs e)
    {
        Observe(e.Datagram);
        DatagramReceived?.Invoke(this, e);
    }

    private void Observe(ReadOnlyMemory<byte> datagram)
    {
        if (!DisplayPacketCodec.TryDecode(datagram.Span, out var packet, out _) || packet is null)
        {
            return;
        }

        var negotiated = packet.Header.MessageType == DisplayMessageType.CapabilitiesResponse;
        var rejected = packet.Header.MessageType == DisplayMessageType.Result
            && DisplayPayloadCodec.TryDecode(packet.Header.MessageType, packet.Payload.Span, out var payload, out _)
            && payload is ResultPayload { ResultCode: DisplayResultCode.WrongSession };
        lock (_syncRoot)
        {
            if (negotiated)
            {
                _sessionActive = true;
            }
            else if (rejected && _sessionActive)
            {
                _sessionActive = false;
                _sessionLosses++;
            }
        }
    }
}

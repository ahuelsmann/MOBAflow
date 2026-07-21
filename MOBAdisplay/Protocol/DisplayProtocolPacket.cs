// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

/// <summary>
/// Represents one validated display protocol datagram.
/// </summary>
public sealed class DisplayProtocolPacket
{
    /// <summary>
    /// Initializes a protocol packet and takes an immutable copy of its payload.
    /// </summary>
    public DisplayProtocolPacket(DisplayPacketHeader header, ReadOnlyMemory<byte> payload)
    {
        Header = header;
        Payload = payload.ToArray();
    }

    /// <summary>
    /// Gets the caller-controlled protocol header.
    /// </summary>
    public DisplayPacketHeader Header { get; }

    /// <summary>
    /// Gets the immutable packet payload.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }
}

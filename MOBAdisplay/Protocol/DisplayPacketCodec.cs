// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

using System.Buffers.Binary;

/// <summary>
/// Encodes and decodes the fixed display protocol envelope in network byte order.
/// </summary>
public static class DisplayPacketCodec
{
    /// <summary>
    /// Encodes a packet and derives its payload length and CRC32 integrity field.
    /// </summary>
    public static byte[] Encode(DisplayProtocolPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        if (!TryValidateHeader(packet.Header, out var error))
        {
            throw new ArgumentException($"Invalid display packet header: {error}.", nameof(packet));
        }

        if (packet.Payload.Length > DisplayProtocol.MaxPayloadLength)
        {
            throw new ArgumentException("Display packet payload exceeds the protocol limit.", nameof(packet));
        }

        var datagram = new byte[DisplayProtocol.HeaderLength + packet.Payload.Length];
        WriteHeader(datagram, packet.Header, (ushort)packet.Payload.Length, DisplayCrc32.Compute(packet.Payload.Span));
        packet.Payload.Span.CopyTo(datagram.AsSpan(DisplayProtocol.HeaderLength));
        return datagram;
    }

    /// <summary>
    /// Decodes exactly one datagram without accepting truncated or trailing data.
    /// </summary>
    public static bool TryDecode(
        ReadOnlySpan<byte> datagram,
        out DisplayProtocolPacket? packet,
        out DisplayPacketDecodeError error)
    {
        packet = null;
        if (!TryReadHeader(datagram, out var header, out var payloadLength, out var payloadCrc32, out error))
        {
            return false;
        }

        if (!TryValidateHeader(header, out error))
        {
            return false;
        }

        var payload = datagram.Slice(DisplayProtocol.HeaderLength, payloadLength);
        if (DisplayCrc32.Compute(payload) != payloadCrc32)
        {
            error = DisplayPacketDecodeError.PayloadChecksumMismatch;
            return false;
        }

        packet = new DisplayProtocolPacket(header, payload.ToArray());
        return true;
    }

    private static bool TryReadHeader(
        ReadOnlySpan<byte> datagram,
        out DisplayPacketHeader header,
        out ushort payloadLength,
        out uint payloadCrc32,
        out DisplayPacketDecodeError error)
    {
        header = default;
        payloadLength = 0;
        payloadCrc32 = 0;
        error = DisplayPacketDecodeError.None;
        if (datagram.Length < DisplayProtocol.HeaderLength)
        {
            error = DisplayPacketDecodeError.PacketTooShort;
            return false;
        }

        if (BinaryPrimitives.ReadUInt32BigEndian(datagram) != DisplayProtocol.Magic)
        {
            error = DisplayPacketDecodeError.InvalidMagic;
            return false;
        }

        if (BinaryPrimitives.ReadUInt16BigEndian(datagram[8..]) != DisplayProtocol.HeaderLength)
        {
            error = DisplayPacketDecodeError.InvalidHeaderLength;
            return false;
        }

        payloadLength = BinaryPrimitives.ReadUInt16BigEndian(datagram[10..]);
        if (datagram.Length != DisplayProtocol.HeaderLength + payloadLength)
        {
            error = DisplayPacketDecodeError.PayloadLengthMismatch;
            return false;
        }

        header = ReadHeaderFields(datagram);
        payloadCrc32 = BinaryPrimitives.ReadUInt32BigEndian(datagram[28..]);
        return true;
    }

    private static DisplayPacketHeader ReadHeaderFields(ReadOnlySpan<byte> datagram) =>
        new(
            new DisplayProtocolVersion(datagram[4], datagram[5]),
            (DisplayMessageType)datagram[6],
            (DisplayProtocolFlags)datagram[7],
            BinaryPrimitives.ReadUInt32BigEndian(datagram[12..]),
            BinaryPrimitives.ReadUInt32BigEndian(datagram[16..]),
            BinaryPrimitives.ReadUInt32BigEndian(datagram[20..]),
            BinaryPrimitives.ReadUInt16BigEndian(datagram[24..]),
            BinaryPrimitives.ReadUInt16BigEndian(datagram[26..]));

    private static bool TryValidateHeader(DisplayPacketHeader header, out DisplayPacketDecodeError error)
    {
        if (header.Version.Major == 0)
        {
            error = DisplayPacketDecodeError.InvalidVersion;
            return false;
        }

        if (header.RequestId == 0)
        {
            error = DisplayPacketDecodeError.InvalidRequestId;
            return false;
        }

        if (!Enum.IsDefined(typeof(DisplayMessageType), header.MessageType))
        {
            error = DisplayPacketDecodeError.UnknownMessageType;
            return false;
        }

        if ((header.Flags & ~DisplayProtocol.SupportedFlags) != 0)
        {
            error = DisplayPacketDecodeError.UnsupportedFlags;
            return false;
        }

        if (header.PacketCount == 0 || header.PacketIndex >= header.PacketCount)
        {
            error = DisplayPacketDecodeError.InvalidPacketSequence;
            return false;
        }

        error = DisplayPacketDecodeError.None;
        return true;
    }

    private static void WriteHeader(
        Span<byte> destination,
        DisplayPacketHeader header,
        ushort payloadLength,
        uint payloadCrc32)
    {
        BinaryPrimitives.WriteUInt32BigEndian(destination, DisplayProtocol.Magic);
        destination[4] = header.Version.Major;
        destination[5] = header.Version.Minor;
        destination[6] = (byte)header.MessageType;
        destination[7] = (byte)header.Flags;
        BinaryPrimitives.WriteUInt16BigEndian(destination[8..], DisplayProtocol.HeaderLength);
        BinaryPrimitives.WriteUInt16BigEndian(destination[10..], payloadLength);
        BinaryPrimitives.WriteUInt32BigEndian(destination[12..], header.RequestId);
        BinaryPrimitives.WriteUInt32BigEndian(destination[16..], header.FrameId);
        BinaryPrimitives.WriteUInt32BigEndian(destination[20..], header.SessionId);
        BinaryPrimitives.WriteUInt16BigEndian(destination[24..], header.PacketIndex);
        BinaryPrimitives.WriteUInt16BigEndian(destination[26..], header.PacketCount);
        BinaryPrimitives.WriteUInt32BigEndian(destination[28..], payloadCrc32);
    }
}

internal static class DisplayCrc32
{
    private const uint Polynomial = 0xEDB88320;

    public static uint Compute(ReadOnlySpan<byte> payload)
    {
        var crc = uint.MaxValue;
        foreach (var value in payload)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ Polynomial : crc >> 1;
            }
        }

        return ~crc;
    }
}

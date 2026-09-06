// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

/// <summary>
/// Contains the caller-controlled fields in a display protocol envelope.
/// </summary>
/// <param name="Version">Protocol version used by the datagram.</param>
/// <param name="MessageType">Message carried by the datagram.</param>
/// <param name="Flags">Transport behavior flags.</param>
/// <param name="RequestId">Correlates a request and its responses.</param>
/// <param name="FrameId">Identifies a frame transaction, or zero for non-frame messages.</param>
/// <param name="SessionId">Identifies the current device session, or zero before negotiation.</param>
/// <param name="PacketIndex">Zero-based packet position in a logical message sequence.</param>
/// <param name="PacketCount">Total packet count in the logical message sequence.</param>
public readonly record struct DisplayPacketHeader(
    DisplayProtocolVersion Version,
    DisplayMessageType MessageType,
    DisplayProtocolFlags Flags,
    uint RequestId,
    uint FrameId,
    uint SessionId,
    ushort PacketIndex = 0,
    ushort PacketCount = 1);

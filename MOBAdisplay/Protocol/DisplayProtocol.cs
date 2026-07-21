// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

/// <summary>
/// Defines the stable envelope constants for the versioned ESP32 display protocol.
/// </summary>
public static class DisplayProtocol
{
    public const uint MAGIC = 0x4D4F4241;
    public const ushort HEADER_LENGTH = 32;
    public const int DEFAULT_MAX_DATAGRAM_LENGTH = 1232;
    public const int DEFAULT_MAX_PAYLOAD_LENGTH = DEFAULT_MAX_DATAGRAM_LENGTH - HEADER_LENGTH;
    public const int MAX_PAYLOAD_LENGTH = ushort.MaxValue;
    public const byte CURRENT_MAJOR_VERSION = 1;
    public const byte CURRENT_MINOR_VERSION = 0;
    public const DisplayProtocolFlags SUPPORTED_FLAGS =
        DisplayProtocolFlags.Response
        | DisplayProtocolFlags.AcknowledgementRequired
        | DisplayProtocolFlags.Retry
        | DisplayProtocolFlags.FinalPacket;

    /// <summary>
    /// Gets the protocol version emitted by this host implementation.
    /// </summary>
    public static DisplayProtocolVersion CurrentVersion =>
        new(CURRENT_MAJOR_VERSION, CURRENT_MINOR_VERSION);
}

/// <summary>
/// Identifies a protocol message carried by a display datagram.
/// </summary>
public enum DisplayMessageType : byte
{
    HelloRequest = 0x01,
    CapabilitiesResponse = 0x02,
    HealthRequest = 0x03,
    HealthResponse = 0x04,
    BeginFrame = 0x10,
    FrameRegion = 0x11,
    CompleteFrame = 0x12,
    AbortFrame = 0x13,
    Clear = 0x20,
    SetBrightness = 0x21,
    RenderTestPattern = 0x22,
    Result = 0x7F
}

/// <summary>
/// Describes transport behavior that applies to one protocol datagram.
/// </summary>
[Flags]
public enum DisplayProtocolFlags : byte
{
    None = 0,
    Response = 1 << 0,
    AcknowledgementRequired = 1 << 1,
    Retry = 1 << 2,
    FinalPacket = 1 << 3
}

/// <summary>
/// Describes why a protocol datagram could not be decoded.
/// </summary>
public enum DisplayPacketDecodeError
{
    None,
    PacketTooShort,
    InvalidMagic,
    InvalidHeaderLength,
    PayloadLengthMismatch,
    InvalidVersion,
    InvalidRequestId,
    UnknownMessageType,
    UnsupportedFlags,
    InvalidPacketSequence,
    PayloadChecksumMismatch
}
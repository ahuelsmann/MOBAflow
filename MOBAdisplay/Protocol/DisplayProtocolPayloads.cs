// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

/// <summary>
/// Identifies a typed payload that can be serialized by the display protocol.
/// </summary>
public interface IDisplayProtocolPayload
{
    /// <summary>
    /// Gets the envelope message type associated with this payload.
    /// </summary>
    DisplayMessageType MessageType { get; }
}

/// <summary>
/// Describes why a typed display payload could not be decoded.
/// </summary>
public enum DisplayPayloadDecodeError
{
    None,
    UnsupportedMessageType,
    InvalidLength,
    ReservedFieldNotZero,
    InvalidVersionRange,
    InvalidValue,
    UnknownEnumValue,
    UnsupportedFlags,
    InvalidUtf8
}

/// <summary>
/// Identifies a pixel format used by frame metadata.
/// </summary>
public enum DisplayPixelFormat : byte
{
    Rgb565BigEndian = 1
}

/// <summary>
/// Identifies pixel formats advertised by a device.
/// </summary>
[Flags]
public enum DisplayPixelFormatFlags : ushort
{
    None = 0,
    Rgb565BigEndian = 1 << 0
}

/// <summary>
/// Identifies a clockwise display rotation.
/// </summary>
public enum DisplayRotation : byte
{
    Degrees0 = 0,
    Degrees90 = 1,
    Degrees180 = 2,
    Degrees270 = 3
}

/// <summary>
/// Identifies rotations advertised by a device.
/// </summary>
[Flags]
public enum DisplayRotationFlags : byte
{
    None = 0,
    Degrees0 = 1 << 0,
    Degrees90 = 1 << 1,
    Degrees180 = 1 << 2,
    Degrees270 = 1 << 3
}

/// <summary>
/// Identifies optional commands advertised by a device.
/// </summary>
[Flags]
public enum DisplayOptionalCommandFlags : byte
{
    None = 0,
    Clear = 1 << 0,
    SetBrightness = 1 << 1,
    RenderTestPattern = 1 << 2
}

/// <summary>
/// Identifies frame-transfer guarantees advertised by a device.
/// </summary>
[Flags]
public enum DisplayFrameCapabilityFlags : byte
{
    None = 0,
    FullFrameStaging = 1 << 0,
    RegionTransfer = 1 << 1,
    AtomicPresentation = 1 << 2
}

/// <summary>
/// Identifies the negotiated acknowledgement strategy.
/// </summary>
public enum DisplayAcknowledgementMode : byte
{
    ControlAndCompletion = 0
}

/// <summary>
/// Identifies a device health state.
/// </summary>
public enum DisplayHealthState : byte
{
    Ready = 0,
    Busy = 1,
    Degraded = 2
}

/// <summary>
/// Identifies why a frame was aborted.
/// </summary>
public enum DisplayAbortReason : byte
{
    HostCancellation = 0,
    Replacement = 1,
    Shutdown = 2
}

/// <summary>
/// Identifies a built-in test pattern.
/// </summary>
public enum DisplayTestPattern : byte
{
    Conformance = 1
}

/// <summary>
/// Identifies the outcome of a display protocol operation.
/// </summary>
public enum DisplayResultCode : byte
{
    Ok = 0x00,
    Invalid = 0x01,
    Unsupported = 0x02,
    UnsupportedVersion = 0x03,
    Busy = 0x04,
    Incomplete = 0x05,
    ChecksumMismatch = 0x06,
    Timeout = 0x07,
    HardwareFailure = 0x08,
    WrongSession = 0x09,
    Conflict = 0x0A
}

/// <summary>
/// Describes additional properties of a structured result.
/// </summary>
[Flags]
public enum DisplayResultFlags : byte
{
    None = 0,
    Presented = 1 << 0,
    Duplicate = 1 << 1,
    Retryable = 1 << 2
}

/// <summary>
/// Requests protocol negotiation with a device.
/// </summary>
/// <param name="MinimumVersion">Lowest protocol version accepted by the host.</param>
/// <param name="MaximumVersion">Highest protocol version accepted by the host.</param>
/// <param name="MaximumDatagramLength">Largest UDP payload accepted by the host.</param>
public readonly record struct HelloRequestPayload(
    DisplayProtocolVersion MinimumVersion,
    DisplayProtocolVersion MaximumVersion,
    ushort MaximumDatagramLength) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.HelloRequest;
}

/// <summary>
/// Reports the capabilities selected for a new device session.
/// </summary>
/// <param name="SelectedVersion">Negotiated protocol version.</param>
/// <param name="Width">Native display width in pixels.</param>
/// <param name="Height">Native display height in pixels.</param>
/// <param name="MaximumDatagramLength">Largest UDP payload accepted by the device.</param>
/// <param name="MaximumRegionPayloadLength">Largest region pixel payload accepted by the device.</param>
/// <param name="PixelFormats">Supported pixel formats.</param>
/// <param name="Rotations">Supported clockwise rotations.</param>
/// <param name="OptionalCommands">Supported optional control commands.</param>
/// <param name="FrameCapabilities">Supported frame-transfer guarantees.</param>
/// <param name="AcknowledgementMode">Negotiated acknowledgement strategy.</param>
/// <param name="SessionId">New non-zero device session identifier.</param>
/// <param name="DeviceIdentity">Safe diagnostic device identity.</param>
/// <param name="FirmwareVersion">Safe diagnostic firmware version.</param>
/// <param name="AdapterIdentity">Safe diagnostic display-adapter identity.</param>
public sealed record CapabilitiesResponsePayload(
    DisplayProtocolVersion SelectedVersion,
    ushort Width,
    ushort Height,
    ushort MaximumDatagramLength,
    ushort MaximumRegionPayloadLength,
    DisplayPixelFormatFlags PixelFormats,
    DisplayRotationFlags Rotations,
    DisplayOptionalCommandFlags OptionalCommands,
    DisplayFrameCapabilityFlags FrameCapabilities,
    DisplayAcknowledgementMode AcknowledgementMode,
    uint SessionId,
    string DeviceIdentity,
    string FirmwareVersion,
    string AdapterIdentity) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.CapabilitiesResponse;
}

/// <summary>
/// Requests current device health information.
/// </summary>
public readonly record struct HealthRequestPayload : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.HealthRequest;
}

/// <summary>
/// Reports safe device health counters and state.
/// </summary>
/// <param name="HealthState">Current device health state.</param>
/// <param name="LastResultCode">Most recent structured result code.</param>
/// <param name="UptimeSeconds">Device uptime in seconds.</param>
/// <param name="FreeHeapBytes">Current free heap in bytes.</param>
/// <param name="AcceptedFrameCount">Number of accepted frames.</param>
/// <param name="RejectedFrameCount">Number of rejected frames.</param>
/// <param name="LastCompletedFrameId">Most recently completed frame identifier, or zero.</param>
public readonly record struct HealthResponsePayload(
    DisplayHealthState HealthState,
    DisplayResultCode LastResultCode,
    uint UptimeSeconds,
    uint FreeHeapBytes,
    uint AcceptedFrameCount,
    uint RejectedFrameCount,
    uint LastCompletedFrameId) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.HealthResponse;
}

/// <summary>
/// Declares metadata and integrity information for a frame transaction.
/// </summary>
/// <param name="Width">Frame width in pixels.</param>
/// <param name="Height">Frame height in pixels.</param>
/// <param name="PixelFormat">Frame pixel format.</param>
/// <param name="Rotation">Clockwise frame rotation.</param>
/// <param name="ExpectedPixelByteCount">Exact complete-frame byte count.</param>
/// <param name="FrameCrc32">CRC32 of the complete frame pixel stream.</param>
public readonly record struct BeginFramePayload(
    ushort Width,
    ushort Height,
    DisplayPixelFormat PixelFormat,
    DisplayRotation Rotation,
    uint ExpectedPixelByteCount,
    uint FrameCrc32) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.BeginFrame;
}

/// <summary>
/// Carries one immutable rectangular region of frame pixel data.
/// </summary>
public sealed class FrameRegionPayload : IDisplayProtocolPayload
{
    /// <summary>
    /// Initializes a frame region and takes an immutable copy of its pixel data.
    /// </summary>
    /// <param name="x">Region X origin.</param>
    /// <param name="y">Region Y origin.</param>
    /// <param name="width">Region width in pixels.</param>
    /// <param name="height">Region height in pixels.</param>
    /// <param name="frameByteOffset">Byte offset in the complete frame stream.</param>
    /// <param name="pixelBytes">RGB565 big-endian region pixels.</param>
    public FrameRegionPayload(
        ushort x,
        ushort y,
        ushort width,
        ushort height,
        uint frameByteOffset,
        ReadOnlyMemory<byte> pixelBytes)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        FrameByteOffset = frameByteOffset;
        PixelBytes = pixelBytes.ToArray();
    }

    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.FrameRegion;

    /// <summary>
    /// Gets the region X origin.
    /// </summary>
    public ushort X { get; }

    /// <summary>
    /// Gets the region Y origin.
    /// </summary>
    public ushort Y { get; }

    /// <summary>
    /// Gets the region width in pixels.
    /// </summary>
    public ushort Width { get; }

    /// <summary>
    /// Gets the region height in pixels.
    /// </summary>
    public ushort Height { get; }

    /// <summary>
    /// Gets the byte offset in the complete frame stream.
    /// </summary>
    public uint FrameByteOffset { get; }

    /// <summary>
    /// Gets an immutable copy of the RGB565 big-endian region pixels.
    /// </summary>
    public ReadOnlyMemory<byte> PixelBytes { get; }
}

/// <summary>
/// Requests validation and atomic presentation of a complete staged frame.
/// </summary>
/// <param name="FrameCrc32">Expected CRC32 of the complete frame pixel stream.</param>
public readonly record struct CompleteFramePayload(uint FrameCrc32) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.CompleteFrame;
}

/// <summary>
/// Requests idempotent disposal of staged frame data.
/// </summary>
/// <param name="Reason">Reason for discarding the staged frame.</param>
public readonly record struct AbortFramePayload(DisplayAbortReason Reason) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.AbortFrame;
}

/// <summary>
/// Requests that the display be cleared to one RGB565 color.
/// </summary>
/// <param name="Rgb565Color">RGB565 clear color.</param>
public readonly record struct ClearPayload(ushort Rgb565Color) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.Clear;
}

/// <summary>
/// Requests a brightness percentage from zero through one hundred.
/// </summary>
/// <param name="Percentage">Brightness percentage from zero through one hundred.</param>
public readonly record struct SetBrightnessPayload(byte Percentage) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.SetBrightness;
}

/// <summary>
/// Requests a built-in deterministic device test pattern.
/// </summary>
/// <param name="Pattern">Built-in pattern identifier.</param>
public readonly record struct RenderTestPatternPayload(DisplayTestPattern Pattern) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.RenderTestPattern;
}

/// <summary>
/// Reports a structured protocol operation result.
/// </summary>
/// <param name="ResultCode">Operation result code.</param>
/// <param name="Flags">Additional result properties.</param>
/// <param name="DetailCode">Message-specific detail code, or zero.</param>
/// <param name="RetryAfterMilliseconds">Suggested retry delay, or zero.</param>
/// <param name="FirstMissingByteOffset">First missing frame-byte offset, or zero.</param>
/// <param name="MissingByteCount">Contiguous missing frame-byte count, or zero.</param>
public readonly record struct ResultPayload(
    DisplayResultCode ResultCode,
    DisplayResultFlags Flags,
    ushort DetailCode,
    uint RetryAfterMilliseconds,
    uint FirstMissingByteOffset,
    uint MissingByteCount) : IDisplayProtocolPayload
{
    /// <inheritdoc />
    public DisplayMessageType MessageType => DisplayMessageType.Result;
}
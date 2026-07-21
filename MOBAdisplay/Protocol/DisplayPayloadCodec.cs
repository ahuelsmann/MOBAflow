// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

using System.Buffers.Binary;
using System.Text;

/// <summary>
/// Encodes and decodes typed v1.0 display message payloads in network byte order.
/// </summary>
public static class DisplayPayloadCodec
{
    private const int HelloLength = 8;
    private const int CapabilitiesFixedLength = 20;
    private const int HealthLength = 24;
    private const int BeginFrameLength = 16;
    private const int FrameRegionMetadataLength = 16;
    private const int CompleteFrameLength = 4;
    private const int AbortFrameLength = 4;
    private const int ClearLength = 2;
    private const int BrightnessLength = 1;
    private const int TestPatternLength = 4;
    private const int ResultLength = 16;

    private const DisplayPixelFormatFlags SupportedPixelFormats =
        DisplayPixelFormatFlags.Rgb565BigEndian;
    private const DisplayRotationFlags SupportedRotations =
        DisplayRotationFlags.Degrees0
        | DisplayRotationFlags.Degrees90
        | DisplayRotationFlags.Degrees180
        | DisplayRotationFlags.Degrees270;
    private const DisplayOptionalCommandFlags SupportedOptionalCommands =
        DisplayOptionalCommandFlags.Clear
        | DisplayOptionalCommandFlags.SetBrightness
        | DisplayOptionalCommandFlags.RenderTestPattern;
    private const DisplayFrameCapabilityFlags SupportedFrameCapabilities =
        DisplayFrameCapabilityFlags.FullFrameStaging
        | DisplayFrameCapabilityFlags.RegionTransfer
        | DisplayFrameCapabilityFlags.AtomicPresentation;
    private const DisplayResultFlags SupportedResultFlags =
        DisplayResultFlags.Presented
        | DisplayResultFlags.Duplicate
        | DisplayResultFlags.Retryable;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>
    /// Encodes one typed payload and rejects values that violate the v1.0 contract.
    /// </summary>
    public static byte[] Encode(IDisplayProtocolPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payload switch
        {
            HelloRequestPayload value => EncodeHello(value),
            CapabilitiesResponsePayload value => EncodeCapabilities(value),
            HealthRequestPayload => [],
            HealthResponsePayload value => EncodeHealth(value),
            BeginFramePayload value => EncodeBeginFrame(value),
            FrameRegionPayload value => EncodeFrameRegion(value),
            CompleteFramePayload value => EncodeCompleteFrame(value),
            AbortFramePayload value => EncodeAbortFrame(value),
            ClearPayload value => EncodeClear(value),
            SetBrightnessPayload value => EncodeBrightness(value),
            RenderTestPatternPayload value => EncodeTestPattern(value),
            ResultPayload value => EncodeResult(value),
            _ => throw new ArgumentException("Unsupported display payload type.", nameof(payload))
        };
    }

    /// <summary>
    /// Decodes exactly one typed payload for the supplied envelope message type.
    /// </summary>
    public static bool TryDecode(
        DisplayMessageType messageType,
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        payload = null;
        return messageType switch
        {
            DisplayMessageType.HelloRequest => TryDecodeHello(source, out payload, out error),
            DisplayMessageType.CapabilitiesResponse => TryDecodeCapabilities(source, out payload, out error),
            DisplayMessageType.HealthRequest => TryDecodeHealthRequest(source, out payload, out error),
            DisplayMessageType.HealthResponse => TryDecodeHealth(source, out payload, out error),
            DisplayMessageType.BeginFrame => TryDecodeBeginFrame(source, out payload, out error),
            DisplayMessageType.FrameRegion => TryDecodeFrameRegion(source, out payload, out error),
            DisplayMessageType.CompleteFrame => TryDecodeCompleteFrame(source, out payload, out error),
            DisplayMessageType.AbortFrame => TryDecodeAbortFrame(source, out payload, out error),
            DisplayMessageType.Clear => TryDecodeClear(source, out payload, out error),
            DisplayMessageType.SetBrightness => TryDecodeBrightness(source, out payload, out error),
            DisplayMessageType.RenderTestPattern => TryDecodeTestPattern(source, out payload, out error),
            DisplayMessageType.Result => TryDecodeResult(source, out payload, out error),
            _ => Fail(DisplayPayloadDecodeError.UnsupportedMessageType, out payload, out error)
        };
    }

    private static byte[] EncodeHello(HelloRequestPayload payload)
    {
        ThrowIfInvalid(TryValidateHello(payload, out var error), error);
        var result = new byte[HelloLength];
        result[0] = payload.MinimumVersion.Major;
        result[1] = payload.MinimumVersion.Minor;
        result[2] = payload.MaximumVersion.Major;
        result[3] = payload.MaximumVersion.Minor;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(4), payload.MaximumDatagramLength);
        return result;
    }

    private static byte[] EncodeCapabilities(CapabilitiesResponsePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ThrowIfInvalid(TryValidateCapabilities(payload, out var error), error);
        var deviceIdentity = EncodeString(payload.DeviceIdentity);
        var firmwareVersion = EncodeString(payload.FirmwareVersion);
        var adapterIdentity = EncodeString(payload.AdapterIdentity);
        var result = new byte[CapabilitiesFixedLength + 3 + deviceIdentity.Length + firmwareVersion.Length + adapterIdentity.Length];
        WriteCapabilitiesFixed(result, payload);
        var offset = CapabilitiesFixedLength;
        offset = WriteString(result, offset, deviceIdentity);
        offset = WriteString(result, offset, firmwareVersion);
        WriteString(result, offset, adapterIdentity);
        return result;
    }

    private static void WriteCapabilitiesFixed(Span<byte> destination, CapabilitiesResponsePayload payload)
    {
        destination[0] = payload.SelectedVersion.Major;
        destination[1] = payload.SelectedVersion.Minor;
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], payload.Width);
        BinaryPrimitives.WriteUInt16BigEndian(destination[4..], payload.Height);
        BinaryPrimitives.WriteUInt16BigEndian(destination[6..], payload.MaximumDatagramLength);
        BinaryPrimitives.WriteUInt16BigEndian(destination[8..], payload.MaximumRegionPayloadLength);
        BinaryPrimitives.WriteUInt16BigEndian(destination[10..], (ushort)payload.PixelFormats);
        destination[12] = (byte)payload.Rotations;
        destination[13] = (byte)payload.OptionalCommands;
        destination[14] = (byte)payload.FrameCapabilities;
        destination[15] = (byte)payload.AcknowledgementMode;
        BinaryPrimitives.WriteUInt32BigEndian(destination[16..], payload.SessionId);
    }

    private static byte[] EncodeHealth(HealthResponsePayload payload)
    {
        ThrowIfInvalid(TryValidateHealth(payload, out var error), error);
        var result = new byte[HealthLength];
        result[0] = (byte)payload.HealthState;
        result[1] = (byte)payload.LastResultCode;
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4), payload.UptimeSeconds);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8), payload.FreeHeapBytes);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(12), payload.AcceptedFrameCount);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(16), payload.RejectedFrameCount);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(20), payload.LastCompletedFrameId);
        return result;
    }

    private static byte[] EncodeBeginFrame(BeginFramePayload payload)
    {
        ThrowIfInvalid(TryValidateBeginFrame(payload, out var error), error);
        var result = new byte[BeginFrameLength];
        BinaryPrimitives.WriteUInt16BigEndian(result, payload.Width);
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(2), payload.Height);
        result[4] = (byte)payload.PixelFormat;
        result[5] = (byte)payload.Rotation;
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8), payload.ExpectedPixelByteCount);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(12), payload.FrameCrc32);
        return result;
    }

    private static byte[] EncodeFrameRegion(FrameRegionPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ThrowIfInvalid(TryValidateFrameRegion(payload, out var error), error);
        var result = new byte[FrameRegionMetadataLength + payload.PixelBytes.Length];
        BinaryPrimitives.WriteUInt16BigEndian(result, payload.X);
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(2), payload.Y);
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(4), payload.Width);
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(6), payload.Height);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8), payload.FrameByteOffset);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(12), (uint)payload.PixelBytes.Length);
        payload.PixelBytes.Span.CopyTo(result.AsSpan(FrameRegionMetadataLength));
        return result;
    }

    private static byte[] EncodeCompleteFrame(CompleteFramePayload payload)
    {
        var result = new byte[CompleteFrameLength];
        BinaryPrimitives.WriteUInt32BigEndian(result, payload.FrameCrc32);
        return result;
    }

    private static byte[] EncodeAbortFrame(AbortFramePayload payload)
    {
        ThrowIfInvalid(IsDefined(payload.Reason), DisplayPayloadDecodeError.UnknownEnumValue);
        var result = new byte[AbortFrameLength];
        result[0] = (byte)payload.Reason;
        return result;
    }

    private static byte[] EncodeClear(ClearPayload payload)
    {
        var result = new byte[ClearLength];
        BinaryPrimitives.WriteUInt16BigEndian(result, payload.Rgb565Color);
        return result;
    }

    private static byte[] EncodeBrightness(SetBrightnessPayload payload)
    {
        ThrowIfInvalid(payload.Percentage <= 100, DisplayPayloadDecodeError.InvalidValue);
        return [payload.Percentage];
    }

    private static byte[] EncodeTestPattern(RenderTestPatternPayload payload)
    {
        ThrowIfInvalid(IsDefined(payload.Pattern), DisplayPayloadDecodeError.UnknownEnumValue);
        var result = new byte[TestPatternLength];
        result[0] = (byte)payload.Pattern;
        return result;
    }

    private static byte[] EncodeResult(ResultPayload payload)
    {
        ThrowIfInvalid(TryValidateResult(payload, out var error), error);
        var result = new byte[ResultLength];
        result[0] = (byte)payload.ResultCode;
        result[1] = (byte)payload.Flags;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(2), payload.DetailCode);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4), payload.RetryAfterMilliseconds);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8), payload.FirstMissingByteOffset);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(12), payload.MissingByteCount);
        return result;
    }

    private static bool TryDecodeHello(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, HelloLength, out payload, out error)
            || !ReservedBytesAreZero(source[6..8], out payload, out error))
        {
            return false;
        }

        var value = new HelloRequestPayload(
            new DisplayProtocolVersion(source[0], source[1]),
            new DisplayProtocolVersion(source[2], source[3]),
            BinaryPrimitives.ReadUInt16BigEndian(source[4..]));
        return TryValidateAndSet(value, TryValidateHello(value, out error), out payload, ref error);
    }

    private static bool TryDecodeCapabilities(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        payload = null;
        if (source.Length < CapabilitiesFixedLength + 3)
        {
            error = DisplayPayloadDecodeError.InvalidLength;
            return false;
        }

        var offset = CapabilitiesFixedLength;
        if (!TryReadString(source, ref offset, out var deviceIdentity, out error)
            || !TryReadString(source, ref offset, out var firmwareVersion, out error)
            || !TryReadString(source, ref offset, out var adapterIdentity, out error)
            || offset != source.Length)
        {
            error = error == DisplayPayloadDecodeError.None ? DisplayPayloadDecodeError.InvalidLength : error;
            return false;
        }

        var value = ReadCapabilitiesFixed(source, deviceIdentity, firmwareVersion, adapterIdentity);
        return TryValidateAndSet(value, TryValidateCapabilities(value, out error), out payload, ref error);
    }

    private static CapabilitiesResponsePayload ReadCapabilitiesFixed(
        ReadOnlySpan<byte> source,
        string deviceIdentity,
        string firmwareVersion,
        string adapterIdentity) =>
        new(
            new DisplayProtocolVersion(source[0], source[1]),
            BinaryPrimitives.ReadUInt16BigEndian(source[2..]),
            BinaryPrimitives.ReadUInt16BigEndian(source[4..]),
            BinaryPrimitives.ReadUInt16BigEndian(source[6..]),
            BinaryPrimitives.ReadUInt16BigEndian(source[8..]),
            (DisplayPixelFormatFlags)BinaryPrimitives.ReadUInt16BigEndian(source[10..]),
            (DisplayRotationFlags)source[12],
            (DisplayOptionalCommandFlags)source[13],
            (DisplayFrameCapabilityFlags)source[14],
            (DisplayAcknowledgementMode)source[15],
            BinaryPrimitives.ReadUInt32BigEndian(source[16..]),
            deviceIdentity,
            firmwareVersion,
            adapterIdentity);

    private static bool TryDecodeHealthRequest(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (source.Length != 0)
        {
            return Fail(DisplayPayloadDecodeError.InvalidLength, out payload, out error);
        }

        payload = new HealthRequestPayload();
        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryDecodeHealth(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, HealthLength, out payload, out error)
            || !ReservedBytesAreZero(source[2..4], out payload, out error))
        {
            return false;
        }

        var value = new HealthResponsePayload(
            (DisplayHealthState)source[0],
            (DisplayResultCode)source[1],
            BinaryPrimitives.ReadUInt32BigEndian(source[4..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[8..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[12..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[16..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[20..]));
        return TryValidateAndSet(value, TryValidateHealth(value, out error), out payload, ref error);
    }

    private static bool TryDecodeBeginFrame(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, BeginFrameLength, out payload, out error)
            || !ReservedBytesAreZero(source[6..8], out payload, out error))
        {
            return false;
        }

        var value = new BeginFramePayload(
            BinaryPrimitives.ReadUInt16BigEndian(source),
            BinaryPrimitives.ReadUInt16BigEndian(source[2..]),
            (DisplayPixelFormat)source[4],
            (DisplayRotation)source[5],
            BinaryPrimitives.ReadUInt32BigEndian(source[8..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[12..]));
        return TryValidateAndSet(value, TryValidateBeginFrame(value, out error), out payload, ref error);
    }

    private static bool TryDecodeFrameRegion(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        payload = null;
        if (source.Length < FrameRegionMetadataLength)
        {
            error = DisplayPayloadDecodeError.InvalidLength;
            return false;
        }

        var pixelLength = BinaryPrimitives.ReadUInt32BigEndian(source[12..]);
        var availablePixelBytes = source.Length - FrameRegionMetadataLength;
        if (pixelLength > availablePixelBytes || pixelLength != availablePixelBytes)
        {
            error = DisplayPayloadDecodeError.InvalidLength;
            return false;
        }

        var value = new FrameRegionPayload(
            BinaryPrimitives.ReadUInt16BigEndian(source),
            BinaryPrimitives.ReadUInt16BigEndian(source[2..]),
            BinaryPrimitives.ReadUInt16BigEndian(source[4..]),
            BinaryPrimitives.ReadUInt16BigEndian(source[6..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[8..]),
            source[FrameRegionMetadataLength..].ToArray());
        return TryValidateAndSet(value, TryValidateFrameRegion(value, out error), out payload, ref error);
    }

    private static bool TryDecodeCompleteFrame(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, CompleteFrameLength, out payload, out error))
        {
            return false;
        }

        payload = new CompleteFramePayload(BinaryPrimitives.ReadUInt32BigEndian(source));
        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryDecodeAbortFrame(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, AbortFrameLength, out payload, out error)
            || !ReservedBytesAreZero(source[1..], out payload, out error))
        {
            return false;
        }

        if (!IsDefined((DisplayAbortReason)source[0]))
        {
            return Fail(DisplayPayloadDecodeError.UnknownEnumValue, out payload, out error);
        }

        payload = new AbortFramePayload((DisplayAbortReason)source[0]);
        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryDecodeClear(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, ClearLength, out payload, out error))
        {
            return false;
        }

        payload = new ClearPayload(BinaryPrimitives.ReadUInt16BigEndian(source));
        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryDecodeBrightness(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, BrightnessLength, out payload, out error))
        {
            return false;
        }

        if (source[0] > 100)
        {
            return Fail(DisplayPayloadDecodeError.InvalidValue, out payload, out error);
        }

        payload = new SetBrightnessPayload(source[0]);
        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryDecodeTestPattern(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, TestPatternLength, out payload, out error)
            || !ReservedBytesAreZero(source[1..], out payload, out error))
        {
            return false;
        }

        if (!IsDefined((DisplayTestPattern)source[0]))
        {
            return Fail(DisplayPayloadDecodeError.UnknownEnumValue, out payload, out error);
        }

        payload = new RenderTestPatternPayload((DisplayTestPattern)source[0]);
        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryDecodeResult(
        ReadOnlySpan<byte> source,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (!HasExactLength(source, ResultLength, out payload, out error))
        {
            return false;
        }

        var value = new ResultPayload(
            (DisplayResultCode)source[0],
            (DisplayResultFlags)source[1],
            BinaryPrimitives.ReadUInt16BigEndian(source[2..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[4..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[8..]),
            BinaryPrimitives.ReadUInt32BigEndian(source[12..]));
        return TryValidateAndSet(value, TryValidateResult(value, out error), out payload, ref error);
    }

    private static bool TryValidateHello(HelloRequestPayload payload, out DisplayPayloadDecodeError error)
    {
        if (payload.MinimumVersion.Major == 0
            || !payload.MinimumVersion.HasCompatibleMajorVersion(payload.MaximumVersion)
            || payload.MinimumVersion.CompareTo(payload.MaximumVersion) > 0)
        {
            error = DisplayPayloadDecodeError.InvalidVersionRange;
            return false;
        }

        error = payload.MaximumDatagramLength > DisplayProtocol.HEADER_LENGTH
            ? DisplayPayloadDecodeError.None
            : DisplayPayloadDecodeError.InvalidValue;
        return error == DisplayPayloadDecodeError.None;
    }

    private static bool TryValidateCapabilities(
        CapabilitiesResponsePayload payload,
        out DisplayPayloadDecodeError error)
    {
        if (payload.SelectedVersion.Major == 0
            || payload.Width == 0
            || payload.Height == 0
            || payload.SessionId == 0
            || payload.MaximumRegionPayloadLength == 0
            || payload.MaximumRegionPayloadLength + DisplayProtocol.HEADER_LENGTH + FrameRegionMetadataLength
                > payload.MaximumDatagramLength
            || string.IsNullOrEmpty(payload.DeviceIdentity)
            || string.IsNullOrEmpty(payload.FirmwareVersion)
            || string.IsNullOrEmpty(payload.AdapterIdentity))
        {
            error = DisplayPayloadDecodeError.InvalidValue;
            return false;
        }

        if (!HasOnlyFlags(payload.PixelFormats, SupportedPixelFormats) || payload.PixelFormats == DisplayPixelFormatFlags.None
            || !HasOnlyFlags(payload.Rotations, SupportedRotations) || payload.Rotations == DisplayRotationFlags.None
            || !HasOnlyFlags(payload.OptionalCommands, SupportedOptionalCommands)
            || !HasOnlyFlags(payload.FrameCapabilities, SupportedFrameCapabilities))
        {
            error = DisplayPayloadDecodeError.UnsupportedFlags;
            return false;
        }

        if (!IsDefined(payload.AcknowledgementMode) || !StringsFit(payload))
        {
            error = DisplayPayloadDecodeError.InvalidValue;
            return false;
        }

        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryValidateHealth(HealthResponsePayload payload, out DisplayPayloadDecodeError error)
    {
        if (!IsDefined(payload.HealthState) || !IsDefined(payload.LastResultCode))
        {
            error = DisplayPayloadDecodeError.UnknownEnumValue;
            return false;
        }

        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryValidateBeginFrame(BeginFramePayload payload, out DisplayPayloadDecodeError error)
    {
        if (!IsDefined(payload.PixelFormat) || !IsDefined(payload.Rotation))
        {
            error = DisplayPayloadDecodeError.UnknownEnumValue;
            return false;
        }

        var expectedLength = (ulong)payload.Width * payload.Height * 2;
        if (payload.Width == 0 || payload.Height == 0 || expectedLength > uint.MaxValue
            || payload.ExpectedPixelByteCount != expectedLength)
        {
            error = DisplayPayloadDecodeError.InvalidValue;
            return false;
        }

        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryValidateFrameRegion(FrameRegionPayload payload, out DisplayPayloadDecodeError error)
    {
        var expectedLength = (ulong)payload.Width * payload.Height * 2;
        if (payload.Width == 0 || payload.Height == 0 || expectedLength > uint.MaxValue
            || payload.PixelBytes.Length != (long)expectedLength || (payload.FrameByteOffset & 1) != 0)
        {
            error = DisplayPayloadDecodeError.InvalidValue;
            return false;
        }

        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryValidateResult(ResultPayload payload, out DisplayPayloadDecodeError error)
    {
        if (!IsDefined(payload.ResultCode))
        {
            error = DisplayPayloadDecodeError.UnknownEnumValue;
            return false;
        }

        if (!HasOnlyFlags(payload.Flags, SupportedResultFlags))
        {
            error = DisplayPayloadDecodeError.UnsupportedFlags;
            return false;
        }

        var hasMissingRange = payload.MissingByteCount != 0;
        var retryDelayApplicable = payload.ResultCode == DisplayResultCode.Busy
            || payload.Flags.HasFlag(DisplayResultFlags.Retryable);
        if (payload.Flags.HasFlag(DisplayResultFlags.Presented) && payload.ResultCode != DisplayResultCode.Ok
            || payload.ResultCode == DisplayResultCode.Incomplete != hasMissingRange
            || payload.ResultCode != DisplayResultCode.Incomplete && payload.FirstMissingByteOffset != 0
            || !retryDelayApplicable && payload.RetryAfterMilliseconds != 0)
        {
            error = DisplayPayloadDecodeError.InvalidValue;
            return false;
        }

        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryReadString(
        ReadOnlySpan<byte> source,
        ref int offset,
        out string value,
        out DisplayPayloadDecodeError error)
    {
        value = string.Empty;
        if (offset >= source.Length || source.Length - offset - 1 < source[offset])
        {
            error = DisplayPayloadDecodeError.InvalidLength;
            return false;
        }

        var length = source[offset++];
        try
        {
            value = StrictUtf8.GetString(source.Slice(offset, length));
        }
        catch (DecoderFallbackException)
        {
            error = DisplayPayloadDecodeError.InvalidUtf8;
            return false;
        }

        offset += length;
        error = value.Length == 0 ? DisplayPayloadDecodeError.InvalidValue : DisplayPayloadDecodeError.None;
        return error == DisplayPayloadDecodeError.None;
    }

    private static byte[] EncodeString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var encoded = StrictUtf8.GetBytes(value);
        if (encoded.Length is 0 or > byte.MaxValue)
        {
            throw new ArgumentException("Display protocol strings must contain 1 through 255 UTF-8 bytes.", nameof(value));
        }

        return encoded;
    }

    private static int WriteString(Span<byte> destination, int offset, ReadOnlySpan<byte> encoded)
    {
        destination[offset++] = (byte)encoded.Length;
        encoded.CopyTo(destination[offset..]);
        return offset + encoded.Length;
    }

    private static bool StringsFit(CapabilitiesResponsePayload payload)
    {
        try
        {
            return StrictUtf8.GetByteCount(payload.DeviceIdentity) is > 0 and <= byte.MaxValue
                && StrictUtf8.GetByteCount(payload.FirmwareVersion) is > 0 and <= byte.MaxValue
                && StrictUtf8.GetByteCount(payload.AdapterIdentity) is > 0 and <= byte.MaxValue;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    private static bool HasExactLength(
        ReadOnlySpan<byte> source,
        int expectedLength,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        if (source.Length == expectedLength)
        {
            payload = null;
            error = DisplayPayloadDecodeError.None;
            return true;
        }

        return Fail(DisplayPayloadDecodeError.InvalidLength, out payload, out error);
    }

    private static bool ReservedBytesAreZero(
        ReadOnlySpan<byte> reserved,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        foreach (var value in reserved)
        {
            if (value != 0)
            {
                return Fail(DisplayPayloadDecodeError.ReservedFieldNotZero, out payload, out error);
            }
        }

        payload = null;
        error = DisplayPayloadDecodeError.None;
        return true;
    }

    private static bool TryValidateAndSet<TPayload>(
        TPayload value,
        bool isValid,
        out IDisplayProtocolPayload? payload,
        ref DisplayPayloadDecodeError error)
        where TPayload : IDisplayProtocolPayload
    {
        payload = isValid ? value : null;
        return isValid;
    }

    private static bool Fail(
        DisplayPayloadDecodeError failure,
        out IDisplayProtocolPayload? payload,
        out DisplayPayloadDecodeError error)
    {
        payload = null;
        error = failure;
        return false;
    }

    private static void ThrowIfInvalid(bool isValid, DisplayPayloadDecodeError error)
    {
        if (!isValid)
        {
            throw new ArgumentException($"Invalid display payload: {error}.", "payload");
        }
    }

    private static bool HasOnlyFlags(DisplayPixelFormatFlags value, DisplayPixelFormatFlags supported) =>
        (value & ~supported) == 0;

    private static bool HasOnlyFlags(DisplayRotationFlags value, DisplayRotationFlags supported) =>
        (value & ~supported) == 0;

    private static bool HasOnlyFlags(DisplayOptionalCommandFlags value, DisplayOptionalCommandFlags supported) =>
        (value & ~supported) == 0;

    private static bool HasOnlyFlags(DisplayFrameCapabilityFlags value, DisplayFrameCapabilityFlags supported) =>
        (value & ~supported) == 0;

    private static bool HasOnlyFlags(DisplayResultFlags value, DisplayResultFlags supported) =>
        (value & ~supported) == 0;

    private static bool IsDefined<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Enum.IsDefined(typeof(TEnum), value);
}
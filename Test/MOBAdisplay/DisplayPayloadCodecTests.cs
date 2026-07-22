// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;

using System.Security.Cryptography;

[TestFixture]
[Category("Unit")]
internal sealed class DisplayPayloadCodecTests
{
    private const uint SessionId = 0x0A0B0C0D;
    private const uint FrameId = 0x11223344;
    private const uint FrameCrc32 = 0xD521D143;

    private static IEnumerable<TestCaseData> GoldenVectors()
    {
        yield return Golden(
            DisplayMessageType.HelloRequest,
            "0100010004D00000",
            "4D4F4241010001020020000801020301000000000000000000000001603FAE350100010004D00000");
        yield return Golden(
            DisplayMessageType.CapabilitiesResponse,
            "010000F0011804D004A000010F0707000A0B0C0D0865737033322D733305312E322E3306737437373839",
            "4D4F4241010002010020002A01020301000000000000000000000001CDDF16EF"
            + "010000F0011804D004A000010F0707000A0B0C0D0865737033322D733305312E322E3306737437373839");
        yield return Golden(
            DisplayMessageType.HealthRequest,
            string.Empty,
            "4D4F4241010003020020000001020302000000000A0B0C0D0000000100000000");
        yield return Golden(
            DisplayMessageType.HealthResponse,
            "000000000000003C000200000000000A0000000201020304",
            "4D4F4241010004010020001801020302000000000A0B0C0D00000001DF3B4548"
            + "000000000000003C000200000000000A0000000201020304");
        yield return Golden(
            DisplayMessageType.BeginFrame,
            "000200020100000000000008D521D143",
            "4D4F4241010010020020001001020303112233440A0B0C0D00000001D047C929"
            + "000200020100000000000008D521D143");
        yield return Golden(
            DisplayMessageType.FrameRegion,
            "00000000000200010000000000000004F80007E0",
            "4D4F4241010011080020001401020304112233440A0B0C0D0000000123DC1FAE"
            + "00000000000200010000000000000004F80007E0");
        yield return Golden(
            DisplayMessageType.CompleteFrame,
            "D521D143",
            "4D4F4241010012020020000401020305112233440A0B0C0D000000017EB75E14D521D143");
        yield return Golden(
            DisplayMessageType.AbortFrame,
            "01000000",
            "4D4F4241010013020020000401020306112233440A0B0C0D0000000199F8B87901000000");
        yield return Golden(
            DisplayMessageType.Clear,
            "001F",
            "4D4F4241010020020020000201020307000000000A0B0C0D00000001CCD11F0A001F");
        yield return Golden(
            DisplayMessageType.SetBrightness,
            "64",
            "4D4F4241010021020020000101020308000000000A0B0C0D0000000198DD4ACC64");
        yield return Golden(
            DisplayMessageType.RenderTestPattern,
            "01000000",
            "4D4F4241010022020020000401020309000000000A0B0C0D0000000199F8B87901000000");
        yield return Golden(
            DisplayMessageType.Result,
            "05040001000000320000000400000004",
            "4D4F424101007F010020001001020305112233440A0B0C0D000000013322A028"
            + "05040001000000320000000400000004");
    }

    private static IEnumerable<DisplayMessageType> MessageTypes() =>
        Enum.GetValues<DisplayMessageType>();

    private static IEnumerable<TestCaseData> InvalidGoldenVectors()
    {
        yield return InvalidGolden(
            DisplayMessageType.HelloRequest,
            "0100010004D00001",
            DisplayPayloadDecodeError.ReservedFieldNotZero);
        yield return InvalidGolden(
            DisplayMessageType.CapabilitiesResponse,
            "010000F0011804D004A080010F0707000A0B0C0D0865737033322D733305312E322E3306737437373839",
            DisplayPayloadDecodeError.UnsupportedFlags);
        yield return InvalidGolden(DisplayMessageType.HealthRequest, "00", DisplayPayloadDecodeError.InvalidLength);
        yield return InvalidGolden(
            DisplayMessageType.HealthResponse,
            "FF0000000000003C000200000000000A0000000201020304",
            DisplayPayloadDecodeError.UnknownEnumValue);
        yield return InvalidGolden(
            DisplayMessageType.BeginFrame,
            "000200020100000000000006D521D143",
            DisplayPayloadDecodeError.InvalidValue);
        yield return InvalidGolden(
            DisplayMessageType.FrameRegion,
            "00000000000200010000000000000006F80007E0",
            DisplayPayloadDecodeError.InvalidLength);
        yield return InvalidGolden(DisplayMessageType.CompleteFrame, "D521D1", DisplayPayloadDecodeError.InvalidLength);
        yield return InvalidGolden(
            DisplayMessageType.AbortFrame,
            "01000001",
            DisplayPayloadDecodeError.ReservedFieldNotZero);
        yield return InvalidGolden(DisplayMessageType.Clear, "00", DisplayPayloadDecodeError.InvalidLength);
        yield return InvalidGolden(DisplayMessageType.SetBrightness, "65", DisplayPayloadDecodeError.InvalidValue);
        yield return InvalidGolden(
            DisplayMessageType.RenderTestPattern,
            "01000001",
            DisplayPayloadDecodeError.ReservedFieldNotZero);
        yield return InvalidGolden(
            DisplayMessageType.Result,
            "05040001000000320000000400000000",
            DisplayPayloadDecodeError.InvalidValue);
    }

    [TestCaseSource(nameof(GoldenVectors))]
    public void Encode_Should_MatchGoldenPayloadAndDatagram(
        DisplayMessageType messageType,
        string expectedPayloadHex,
        string expectedDatagramHex)
    {
        // Arrange
        var payload = CreatePayload(messageType);
        var expectedPayload = Convert.FromHexString(expectedPayloadHex);

        // Act
        var encodedPayload = DisplayPayloadCodec.Encode(payload);
        var datagram = DisplayPacketCodec.Encode(new DisplayProtocolPacket(CreateHeader(messageType), encodedPayload));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(payload.MessageType, Is.EqualTo(messageType));
            Assert.That(encodedPayload, Is.EqualTo(expectedPayload));
            Assert.That(datagram, Is.EqualTo(Convert.FromHexString(expectedDatagramHex)));
        }
    }

    [TestCaseSource(nameof(GoldenVectors))]
    public void TryDecode_Should_RoundTripGoldenPayload(
        DisplayMessageType messageType,
        string payloadHex,
        string _)
    {
        // Arrange
        var encoded = Convert.FromHexString(payloadHex);

        // Act
        var success = DisplayPayloadCodec.TryDecode(messageType, encoded, out var payload, out var error);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.EqualTo(DisplayPayloadDecodeError.None));
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload!.MessageType, Is.EqualTo(messageType));
            Assert.That(DisplayPayloadCodec.Encode(payload), Is.EqualTo(encoded));
        }
    }

    [TestCaseSource(nameof(InvalidGoldenVectors))]
    public void TryDecode_Should_RejectInvalidGoldenPayload(
        DisplayMessageType messageType,
        string payloadHex,
        DisplayPayloadDecodeError expectedError)
    {
        AssertDecodeError(messageType, Convert.FromHexString(payloadHex), expectedError);
    }

    [Test]
    public void TryDecode_Should_RejectPayload_When_MessageTypeIsUnknown()
    {
        AssertDecodeError((DisplayMessageType)0x55, [], DisplayPayloadDecodeError.UnsupportedMessageType);
    }

    [TestCaseSource(nameof(MessageTypes))]
    public void TryDecode_Should_NotThrow_When_PayloadBytesAreArbitrary(DisplayMessageType messageType)
    {
        var random = new Random((int)messageType);

        for (var length = 0; length <= 80; length++)
        {
            var bytes = new byte[length];
            random.NextBytes(bytes);

            Assert.DoesNotThrow(() => DisplayPayloadCodec.TryDecode(messageType, bytes, out _, out _));
        }
    }

    [Test]
    public void TryDecode_Should_RejectPayload_When_FixedLengthDoesNotMatch()
    {
        AssertDecodeError(DisplayMessageType.Clear, [0], DisplayPayloadDecodeError.InvalidLength);
    }

    [Test]
    public void TryDecode_Should_RejectPayload_When_ReservedFieldIsNotZero()
    {
        var payload = GoldenPayload(DisplayMessageType.HelloRequest);
        payload[7] = 1;

        AssertDecodeError(DisplayMessageType.HelloRequest, payload, DisplayPayloadDecodeError.ReservedFieldNotZero);
    }

    [Test]
    public void TryDecode_Should_RejectCapabilities_When_StringContainsInvalidUtf8()
    {
        var payload = GoldenPayload(DisplayMessageType.CapabilitiesResponse);
        payload[21] = 0xFF;

        AssertDecodeError(DisplayMessageType.CapabilitiesResponse, payload, DisplayPayloadDecodeError.InvalidUtf8);
    }

    [Test]
    public void TryDecode_Should_RejectCapabilities_When_FlagIsUnsupported()
    {
        var payload = GoldenPayload(DisplayMessageType.CapabilitiesResponse);
        payload[10] = 0x80;

        AssertDecodeError(DisplayMessageType.CapabilitiesResponse, payload, DisplayPayloadDecodeError.UnsupportedFlags);
    }

    [Test]
    public void TryDecode_Should_RejectBeginFrame_When_ByteCountIsInconsistent()
    {
        var payload = GoldenPayload(DisplayMessageType.BeginFrame);
        payload[11] = 6;

        AssertDecodeError(DisplayMessageType.BeginFrame, payload, DisplayPayloadDecodeError.InvalidValue);
    }

    [Test]
    public void TryDecode_Should_RejectFrameRegion_When_DeclaredLengthDoesNotMatch()
    {
        var payload = GoldenPayload(DisplayMessageType.FrameRegion);
        payload[15] = 6;

        AssertDecodeError(DisplayMessageType.FrameRegion, payload, DisplayPayloadDecodeError.InvalidLength);
    }

    [Test]
    public void TryDecode_Should_RejectBrightness_When_PercentageExceedsOneHundred()
    {
        AssertDecodeError(DisplayMessageType.SetBrightness, [101], DisplayPayloadDecodeError.InvalidValue);
    }

    [Test]
    public void TryDecode_Should_RejectResult_When_IncompleteRangeIsMissing()
    {
        var payload = GoldenPayload(DisplayMessageType.Result);
        Array.Clear(payload, 12, sizeof(uint));

        AssertDecodeError(DisplayMessageType.Result, payload, DisplayPayloadDecodeError.InvalidValue);
    }

    [Test]
    public void Encode_Should_RejectCapabilities_When_StringExceedsWireLimit()
    {
        var payload = CreateCapabilities() with { DeviceIdentity = new string('x', byte.MaxValue + 1) };

        var exception = Assert.Throws<ArgumentException>(() => DisplayPayloadCodec.Encode(payload));

        Assert.That(exception!.ParamName, Is.EqualTo("payload"));
    }

    [Test]
    public void FrameRegionPayload_Should_CopyPixelBytes()
    {
        var pixelBytes = Convert.FromHexString("F80007E0");
        var payload = new FrameRegionPayload(0, 0, 2, 1, 0, pixelBytes);

        pixelBytes[0] = 0;

        Assert.That(payload.PixelBytes.ToArray(), Is.EqualTo(Convert.FromHexString("F80007E0")));
    }

    [Test]
    public void ComputeCrc32_Should_MatchConformanceFrame()
    {
        var frame = Convert.FromHexString("F80007E0001FFFFF");

        var crc32 = DisplayPacketCodec.ComputeCrc32(frame);

        Assert.That(crc32, Is.EqualTo(FrameCrc32));
    }

    [Test]
    public void CreateRgb565_Should_MatchFiveByFourConformanceVector()
    {
        const string expectedHex =
            "F800F80007E007E0001FF800F80007E007E0001F"
            + "FFFFFFFFFFFF00000000FFFFFFFFFFFF00000000";
        var expectedSha256 = Convert.FromHexString(
            "C66E9742B685AE94F7914BEF06AEFFB648DA2BF85380E1A46B42A64922EC445A");

        var frame = DisplayConformancePattern.CreateRgb565(width: 5, height: 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(frame, Is.EqualTo(Convert.FromHexString(expectedHex)));
            Assert.That(DisplayPacketCodec.ComputeCrc32(frame), Is.EqualTo(0x6491200A));
            Assert.That(SHA256.HashData(frame), Is.EqualTo(expectedSha256));
        }
    }

    [TestCase(0, 1, "width")]
    [TestCase(1, 0, "height")]
    public void CreateRgb565_Should_RejectZeroDimension(
        int width,
        int height,
        string expectedParameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => DisplayConformancePattern.CreateRgb565((ushort)width, (ushort)height));

        Assert.That(exception!.ParamName, Is.EqualTo(expectedParameter));
    }

    private static TestCaseData Golden(
        DisplayMessageType messageType,
        string payloadHex,
        string datagramHex) =>
        new(messageType, payloadHex, datagramHex);

    private static TestCaseData InvalidGolden(
        DisplayMessageType messageType,
        string payloadHex,
        DisplayPayloadDecodeError error) =>
        new(messageType, payloadHex, error);

    private static byte[] GoldenPayload(DisplayMessageType messageType) =>
        Convert.FromHexString(messageType switch
        {
            DisplayMessageType.HelloRequest => "0100010004D00000",
            DisplayMessageType.CapabilitiesResponse =>
                "010000F0011804D004A000010F0707000A0B0C0D0865737033322D733305312E322E3306737437373839",
            DisplayMessageType.BeginFrame => "000200020100000000000008D521D143",
            DisplayMessageType.FrameRegion => "00000000000200010000000000000004F80007E0",
            DisplayMessageType.Result => "05040001000000320000000400000004",
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
        });

    private static IDisplayProtocolPayload CreatePayload(DisplayMessageType messageType) =>
        messageType switch
        {
            DisplayMessageType.HelloRequest => new HelloRequestPayload(
                DisplayProtocol.CurrentVersion,
                DisplayProtocol.CurrentVersion,
                DisplayProtocol.DEFAULT_MAX_DATAGRAM_LENGTH),
            DisplayMessageType.CapabilitiesResponse => CreateCapabilities(),
            DisplayMessageType.HealthRequest => new HealthRequestPayload(),
            DisplayMessageType.HealthResponse => new HealthResponsePayload(
                DisplayHealthState.Ready,
                DisplayResultCode.Ok,
                UptimeSeconds: 60,
                FreeHeapBytes: 0x00020000,
                AcceptedFrameCount: 10,
                RejectedFrameCount: 2,
                LastCompletedFrameId: 0x01020304),
            DisplayMessageType.BeginFrame => new BeginFramePayload(
                Width: 2,
                Height: 2,
                DisplayPixelFormat.Rgb565BigEndian,
                DisplayRotation.Degrees0,
                ExpectedPixelByteCount: 8,
                FrameCrc32),
            DisplayMessageType.FrameRegion => new FrameRegionPayload(
                x: 0,
                y: 0,
                width: 2,
                height: 1,
                frameByteOffset: 0,
                Convert.FromHexString("F80007E0")),
            DisplayMessageType.CompleteFrame => new CompleteFramePayload(FrameCrc32),
            DisplayMessageType.AbortFrame => new AbortFramePayload(DisplayAbortReason.Replacement),
            DisplayMessageType.Clear => new ClearPayload(0x001F),
            DisplayMessageType.SetBrightness => new SetBrightnessPayload(100),
            DisplayMessageType.RenderTestPattern => new RenderTestPatternPayload(DisplayTestPattern.Conformance),
            DisplayMessageType.Result => new ResultPayload(
                DisplayResultCode.Incomplete,
                DisplayResultFlags.Retryable,
                DetailCode: 1,
                RetryAfterMilliseconds: 50,
                FirstMissingByteOffset: 4,
                MissingByteCount: 4),
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
        };

    private static CapabilitiesResponsePayload CreateCapabilities() =>
        new(
            DisplayProtocol.CurrentVersion,
            Width: 240,
            Height: 280,
            MaximumDatagramLength: DisplayProtocol.DEFAULT_MAX_DATAGRAM_LENGTH,
            MaximumRegionPayloadLength: 1184,
            DisplayPixelFormatFlags.Rgb565BigEndian,
            DisplayRotationFlags.Degrees0
            | DisplayRotationFlags.Degrees90
            | DisplayRotationFlags.Degrees180
            | DisplayRotationFlags.Degrees270,
            DisplayOptionalCommandFlags.Clear
            | DisplayOptionalCommandFlags.SetBrightness
            | DisplayOptionalCommandFlags.RenderTestPattern,
            DisplayFrameCapabilityFlags.FullFrameStaging
            | DisplayFrameCapabilityFlags.RegionTransfer
            | DisplayFrameCapabilityFlags.AtomicPresentation,
            DisplayAcknowledgementMode.ControlAndCompletion,
            SessionId,
            DeviceIdentity: "esp32-s3",
            FirmwareVersion: "1.2.3",
            AdapterIdentity: "st7789");

    private static DisplayPacketHeader CreateHeader(DisplayMessageType messageType) =>
        messageType switch
        {
            DisplayMessageType.HelloRequest => Header(messageType, DisplayProtocolFlags.AcknowledgementRequired, 0x01020301, 0, 0),
            DisplayMessageType.CapabilitiesResponse => Header(messageType, DisplayProtocolFlags.Response, 0x01020301, 0, 0),
            DisplayMessageType.HealthRequest => Header(messageType, DisplayProtocolFlags.AcknowledgementRequired, 0x01020302, 0, SessionId),
            DisplayMessageType.HealthResponse => Header(messageType, DisplayProtocolFlags.Response, 0x01020302, 0, SessionId),
            DisplayMessageType.BeginFrame => Header(
                messageType,
                DisplayProtocolFlags.AcknowledgementRequired,
                0x01020303,
                FrameId,
                SessionId),
            DisplayMessageType.FrameRegion => Header(messageType, DisplayProtocolFlags.FinalPacket, 0x01020304, FrameId, SessionId),
            DisplayMessageType.CompleteFrame => Header(
                messageType,
                DisplayProtocolFlags.AcknowledgementRequired,
                0x01020305,
                FrameId,
                SessionId),
            DisplayMessageType.AbortFrame => Header(
                messageType,
                DisplayProtocolFlags.AcknowledgementRequired,
                0x01020306,
                FrameId,
                SessionId),
            DisplayMessageType.Clear => Header(messageType, DisplayProtocolFlags.AcknowledgementRequired, 0x01020307, 0, SessionId),
            DisplayMessageType.SetBrightness => Header(messageType, DisplayProtocolFlags.AcknowledgementRequired, 0x01020308, 0, SessionId),
            DisplayMessageType.RenderTestPattern => Header(
                messageType,
                DisplayProtocolFlags.AcknowledgementRequired,
                0x01020309,
                0,
                SessionId),
            DisplayMessageType.Result => Header(messageType, DisplayProtocolFlags.Response, 0x01020305, FrameId, SessionId),
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
        };

    private static DisplayPacketHeader Header(
        DisplayMessageType messageType,
        DisplayProtocolFlags flags,
        uint requestId,
        uint frameId,
        uint sessionId) =>
        new(DisplayProtocol.CurrentVersion, messageType, flags, requestId, frameId, sessionId);

    private static void AssertDecodeError(
        DisplayMessageType messageType,
        ReadOnlySpan<byte> payloadBytes,
        DisplayPayloadDecodeError expectedError)
    {
        var success = DisplayPayloadCodec.TryDecode(messageType, payloadBytes, out var payload, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(payload, Is.Null);
            Assert.That(error, Is.EqualTo(expectedError));
        }
    }
}
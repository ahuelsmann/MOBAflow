// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;

[TestFixture]
[Category("Unit")]
internal sealed class DisplayPacketCodecTests
{
    private static readonly byte[] HelloPayload = Convert.FromHexString("0100010004D00000");
    private static readonly byte[] HelloGoldenDatagram = Convert.FromHexString(
        "4D4F4241010001020020000801020304000000000000000000000001603FAE350100010004D00000");

    [Test]
    public void Encode_Should_MatchHelloGoldenVector()
    {
        // Arrange
        var packet = CreateHelloPacket();

        // Act
        var datagram = DisplayPacketCodec.Encode(packet);

        // Assert
        Assert.That(datagram, Is.EqualTo(HelloGoldenDatagram));
    }

    [Test]
    public void TryDecode_Should_DecodeHelloGoldenVector()
    {
        // Act
        var success = DisplayPacketCodec.TryDecode(HelloGoldenDatagram, out var packet, out var error);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.EqualTo(DisplayPacketDecodeError.None));
            Assert.That(packet, Is.Not.Null);
            Assert.That(packet!.Header, Is.EqualTo(CreateHelloPacket().Header));
            Assert.That(packet.Payload.ToArray(), Is.EqualTo(HelloPayload));
        }
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_PacketIsTooShort()
    {
        AssertDecodeError(HelloGoldenDatagram[..20], DisplayPacketDecodeError.PacketTooShort);
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_MagicIsInvalid()
    {
        var datagram = HelloGoldenDatagram.ToArray();
        datagram[0] = 0;

        AssertDecodeError(datagram, DisplayPacketDecodeError.InvalidMagic);
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_HeaderLengthIsInvalid()
    {
        var datagram = HelloGoldenDatagram.ToArray();
        datagram[9] = 31;

        AssertDecodeError(datagram, DisplayPacketDecodeError.InvalidHeaderLength);
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_PayloadLengthDoesNotMatch()
    {
        var datagram = HelloGoldenDatagram.ToArray();
        datagram[11] = 9;

        AssertDecodeError(datagram, DisplayPacketDecodeError.PayloadLengthMismatch);
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_PayloadChecksumIsInvalid()
    {
        var datagram = HelloGoldenDatagram.ToArray();
        datagram[^1] ^= 0x01;

        AssertDecodeError(datagram, DisplayPacketDecodeError.PayloadChecksumMismatch);
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_ProtocolMajorVersionIsZero()
    {
        var datagram = HelloGoldenDatagram.ToArray();
        datagram[4] = 0;

        AssertDecodeError(datagram, DisplayPacketDecodeError.InvalidVersion);
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_RequestIdIsZero()
    {
        var datagram = HelloGoldenDatagram.ToArray();
        Array.Clear(datagram, 12, sizeof(uint));

        AssertDecodeError(datagram, DisplayPacketDecodeError.InvalidRequestId);
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_MessageTypeIsUnknown()
    {
        var datagram = HelloGoldenDatagram.ToArray();
        datagram[6] = 0x55;

        AssertDecodeError(datagram, DisplayPacketDecodeError.UnknownMessageType);
    }

    [Test]
    public void TryDecode_Should_RejectPacket_When_FlagsAreUnsupported()
    {
        var datagram = HelloGoldenDatagram.ToArray();
        datagram[7] = 0x80;

        AssertDecodeError(datagram, DisplayPacketDecodeError.UnsupportedFlags);
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    public void TryDecode_Should_RejectPacket_When_PacketSequenceIsInvalid(
        int packetIndex,
        int packetCount)
    {
        var datagram = HelloGoldenDatagram.ToArray();
        datagram[24] = (byte)(packetIndex >> 8);
        datagram[25] = (byte)packetIndex;
        datagram[26] = (byte)(packetCount >> 8);
        datagram[27] = (byte)packetCount;

        AssertDecodeError(datagram, DisplayPacketDecodeError.InvalidPacketSequence);
    }

    [Test]
    public void Encode_Should_RejectPacket_When_PayloadExceedsProtocolLimit()
    {
        var packet = new DisplayProtocolPacket(
            CreateHelloPacket().Header,
            new byte[DisplayProtocol.MaxPayloadLength + 1]);

        var exception = Assert.Throws<ArgumentException>(() => DisplayPacketCodec.Encode(packet));

        Assert.That(exception!.ParamName, Is.EqualTo("packet"));
    }

    private static DisplayProtocolPacket CreateHelloPacket() =>
        new(
            new DisplayPacketHeader(
                DisplayProtocol.CurrentVersion,
                DisplayMessageType.HelloRequest,
                DisplayProtocolFlags.AcknowledgementRequired,
                RequestId: 0x01020304,
                FrameId: 0,
                SessionId: 0),
            HelloPayload);

    private static void AssertDecodeError(
        ReadOnlySpan<byte> datagram,
        DisplayPacketDecodeError expectedError)
    {
        var success = DisplayPacketCodec.TryDecode(datagram, out var packet, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(packet, Is.Null);
            Assert.That(error, Is.EqualTo(expectedError));
        }
    }
}

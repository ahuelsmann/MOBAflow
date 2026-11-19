// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Protocol;

/// <summary>
/// Unit tests for Z21FeedbackParser (LAN_RMBUS_DATACHANGED parsing).
/// </summary>
[TestFixture]
internal class Z21FeedbackParserTests
{
    /// <summary>
    /// Builds a minimal LAN_RMBUS_DATACHANGED packet: DataLen(2) + Header(2) + Group(1) + Data(8) + XOR(1) = 14 bytes.
    /// </summary>
    private static byte[] MakePacket(int groupNumber, byte[] dataBytes)
    {
        var packet = new byte[14];
        packet[0] = 0x0F;
        packet[1] = 0x00;
        packet[2] = 0x80;
        packet[3] = 0x00;
        packet[4] = (byte)groupNumber;
        Array.Copy(dataBytes, 0, packet, 5, Math.Min(8, dataBytes.Length));
        return packet;
    }

    [Test]
    public void ExtractFirstInPort_ShortPacket_ReturnsZero()
    {
        var data = new byte[] { 0x0F, 0x00, 0x80, 0x00 };

        var result = Z21FeedbackParser.ExtractFirstInPort(data);

        Assert.That(result, Is.Zero);
    }

    [Test]
    public void ExtractFirstInPort_NoBitsSet_ReturnsZero()
    {
        var packet = MakePacket(0, [0, 0, 0, 0, 0, 0, 0, 0]);

        var result = Z21FeedbackParser.ExtractFirstInPort(packet);

        Assert.That(result, Is.Zero);
    }

    [Test]
    public void ExtractFirstInPort_FirstBitSet_ReturnsInPort1()
    {
        // Group 0, first byte, LSB set => InPort 1
        var packet = MakePacket(0, [0x01, 0, 0, 0, 0, 0, 0, 0]);

        var result = Z21FeedbackParser.ExtractFirstInPort(packet);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ExtractFirstInPort_SecondBitSet_ReturnsInPort2()
    {
        var packet = MakePacket(0, [0x02, 0, 0, 0, 0, 0, 0, 0]);

        var result = Z21FeedbackParser.ExtractFirstInPort(packet);

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void ExtractFirstInPort_LastBitOfFirstByte_ReturnsInPort8()
    {
        var packet = MakePacket(0, [0x80, 0, 0, 0, 0, 0, 0, 0]);

        var result = Z21FeedbackParser.ExtractFirstInPort(packet);

        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void ExtractFirstInPort_Group1_FirstBit_Returns65()
    {
        // InPort = (1 * 64) + 0 + 0 + 1 = 65
        var packet = MakePacket(1, [0x01, 0, 0, 0, 0, 0, 0, 0]);

        var result = Z21FeedbackParser.ExtractFirstInPort(packet);

        Assert.That(result, Is.EqualTo(65));
    }

    [Test]
    public void ExtractAllInPorts_EmptyPacket_ReturnsEmptyList()
    {
        var data = new byte[] { 0x0F, 0x00, 0x80, 0x00 };

        var result = Z21FeedbackParser.ExtractAllInPorts(data);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ExtractAllInPorts_TwoBitsSet_ReturnsBothInPorts()
    {
        var packet = MakePacket(0, [0x03, 0, 0, 0, 0, 0, 0, 0]); // bits 0 and 1

        var result = Z21FeedbackParser.ExtractAllInPorts(packet);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Does.Contain(1));
        Assert.That(result, Does.Contain(2));
    }

    [Test]
    public void ExtractAllInPorts_MultipleBytes_ReturnsAllActiveInPorts()
    {
        var packet = MakePacket(0, [0x01, 0x01, 0, 0, 0, 0, 0, 0]); // InPort 1 and 9

        var result = Z21FeedbackParser.ExtractAllInPorts(packet);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Does.Contain(1));
        Assert.That(result, Does.Contain(9));
    }

    [Test]
    public void GetGroupNumber_ValidPacket_ReturnsGroup()
    {
        var packet = MakePacket(2, [0, 0, 0, 0, 0, 0, 0, 0]);

        var result = Z21FeedbackParser.GetGroupNumber(packet);

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void GetGroupNumber_ShortPacket_ReturnsMinusOne()
    {
        var data = new byte[] { 0x0F, 0x00, 0x80, 0x00 };

        var result = Z21FeedbackParser.GetGroupNumber(data);

        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void GetFeedbackState_ValidPacket_Returns8Bytes()
    {
        var packet = MakePacket(0, [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88]);

        var result = Z21FeedbackParser.GetFeedbackState(packet);

        Assert.That(result, Has.Length.EqualTo(8));
        Assert.That(result[0], Is.EqualTo(0x11));
        Assert.That(result[7], Is.EqualTo(0x88));
    }

    [Test]
    public void GetFeedbackState_ShortPacket_ReturnsEmptyArray()
    {
        var data = new byte[] { 0x0F, 0x00, 0x80, 0x00, 0x00 };

        var result = Z21FeedbackParser.GetFeedbackState(data);

        Assert.That(result, Is.Empty);
    }
}

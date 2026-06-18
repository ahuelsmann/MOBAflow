// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Protocol;

/// <summary>
/// Tests for <see cref="Z21Command"/> byte-level packet builders.
/// These verify the protocol-critical encoding: packet length prefixes, X-BUS headers,
/// DCC long/short address encoding and the XOR checksum over the X-BUS payload.
/// A regression here would silently send malformed packets to the Z21 central station.
/// </summary>
[TestFixture]
internal sealed class Z21CommandTests
{
    /// <summary>
    /// Computes the XOR checksum the same way the Z21 expects it: XOR over all
    /// X-BUS bytes (everything after the 4-byte LAN header up to, but excluding, the checksum).
    /// </summary>
    private static byte XorOverXBus(byte[] packet)
    {
        byte xor = 0;
        for (var i = 4; i < packet.Length - 1; i++)
            xor ^= packet[i];
        return xor;
    }

    private static void AssertLengthPrefixMatchesPacket(byte[] packet)
    {
        var declaredLength = packet[0] | (packet[1] << 8);
        Assert.That(declaredLength, Is.EqualTo(packet.Length),
            "DataLen prefix must equal the actual packet length.");
    }

    [Test]
    public void BuildGetSerialNumber_ReturnsFourByteRequest()
    {
        var packet = Z21Command.BuildGetSerialNumber();

        Assert.That(packet, Is.EqualTo(new byte[]
        {
            0x04, 0x00, Z21Protocol.Header.LAN_GET_SERIAL_NUMBER, 0x00
        }));
        AssertLengthPrefixMatchesPacket(packet);
    }

    [Test]
    public void BuildGetHwInfo_ReturnsFourByteRequest()
    {
        var packet = Z21Command.BuildGetHwInfo();

        Assert.That(packet[2], Is.EqualTo(Z21Protocol.Header.LAN_GET_HWINFO));
        AssertLengthPrefixMatchesPacket(packet);
    }

    [Test]
    public void BuildBroadcastFlagsBasic_EncodesFlagsLittleEndian()
    {
        var packet = Z21Command.BuildBroadcastFlagsBasic();
        var flags = Z21Protocol.BroadcastFlags.Basic;

        Assert.Multiple(() =>
        {
            AssertLengthPrefixMatchesPacket(packet);
            Assert.That(packet[2], Is.EqualTo(Z21Protocol.Header.LAN_SET_BROADCASTFLAGS));
            Assert.That(packet[4], Is.EqualTo((byte)(flags & 0xFF)));
            Assert.That(packet[5], Is.EqualTo((byte)((flags >> 8) & 0xFF)));
            Assert.That(packet[6], Is.EqualTo((byte)((flags >> 16) & 0xFF)));
            Assert.That(packet[7], Is.EqualTo((byte)((flags >> 24) & 0xFF)));
        });
    }

    [Test]
    public void BuildBroadcastFlagsAll_SetsEveryFlagByte()
    {
        var packet = Z21Command.BuildBroadcastFlagsAll();

        Assert.That(packet[4..8], Is.EqualTo(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }));
    }

    [Test]
    public void BuildTrackPowerOn_And_Off_DifferOnlyInPayloadAndChecksum()
    {
        var on = Z21Command.BuildTrackPowerOn();
        var off = Z21Command.BuildTrackPowerOff();

        Assert.Multiple(() =>
        {
            Assert.That(on[5], Is.EqualTo(Z21Protocol.TrackPowerDb0.ON));
            Assert.That(off[5], Is.EqualTo(Z21Protocol.TrackPowerDb0.OFF));
            Assert.That(on[4], Is.EqualTo(Z21Protocol.XHeader.X_TRACK_POWER));
            Assert.That(off[4], Is.EqualTo(Z21Protocol.XHeader.X_TRACK_POWER));
        });
    }

    [Test]
    public void BuildSetLocoDrive_ShortAddress_EncodesAddressWithoutLongFlag()
    {
        var packet = Z21Command.BuildSetLocoDrive(address: 3, speed: 50, forward: true);

        Assert.Multiple(() =>
        {
            AssertLengthPrefixMatchesPacket(packet);
            Assert.That(packet[4], Is.EqualTo(Z21Protocol.XHeader.X_SET_LOCO_DRIVE));
            Assert.That(packet[5], Is.EqualTo(0x13), "DB0 must select 128 speed steps.");
            Assert.That(packet[6], Is.EqualTo(0x00), "Short address MSB stays 0.");
            Assert.That(packet[7], Is.EqualTo(0x03));
            Assert.That(packet[^1], Is.EqualTo(XorOverXBus(packet)));
        });
    }

    [Test]
    public void BuildSetLocoDrive_LongAddress_SetsHighAddressBits()
    {
        var packet = Z21Command.BuildSetLocoDrive(address: 1000, speed: 0, forward: true);

        Assert.Multiple(() =>
        {
            Assert.That(packet[6] & 0xC0, Is.EqualTo(0xC0), "Long addresses must OR 0xC0 into the MSB.");
            Assert.That(packet[6], Is.EqualTo((byte)(0xC0 | ((1000 >> 8) & 0x3F))));
            Assert.That(packet[7], Is.EqualTo((byte)(1000 & 0xFF)));
            Assert.That(packet[^1], Is.EqualTo(XorOverXBus(packet)));
        });
    }

    [Test]
    public void BuildSetLocoDrive_ForwardFlag_SetsHighBitOfSpeedByte()
    {
        var forward = Z21Command.BuildSetLocoDrive(address: 3, speed: 10, forward: true);
        var backward = Z21Command.BuildSetLocoDrive(address: 3, speed: 10, forward: false);

        Assert.Multiple(() =>
        {
            Assert.That(forward[8] & 0x80, Is.EqualTo(0x80), "Forward sets bit 7.");
            Assert.That(backward[8] & 0x80, Is.EqualTo(0x00), "Reverse clears bit 7.");
            Assert.That(forward[8] & 0x7F, Is.EqualTo(10), "Lower 7 bits carry the speed.");
        });
    }

    [Test]
    public void BuildSetLocoFunction_On_SetsFunctionTypeBitsAndIndex()
    {
        var packet = Z21Command.BuildSetLocoFunction(address: 3, functionIndex: 5, on: true);

        Assert.Multiple(() =>
        {
            Assert.That(packet[5], Is.EqualTo(0xF8), "DB0 selects the function command.");
            Assert.That(packet[8] & 0x40, Is.EqualTo(0x40), "On state sets the TT=01 bits.");
            Assert.That(packet[8] & 0x3F, Is.EqualTo(5), "Lower 6 bits carry the function index.");
            Assert.That(packet[^1], Is.EqualTo(XorOverXBus(packet)));
        });
    }

    [Test]
    public void BuildSetLocoFunction_Off_ClearsFunctionTypeBits()
    {
        var packet = Z21Command.BuildSetLocoFunction(address: 3, functionIndex: 1, on: false);

        Assert.That(packet[8] & 0x40, Is.EqualTo(0x00));
    }

    [Test]
    public void BuildGetLocoInfo_HasValidChecksum()
    {
        var packet = Z21Command.BuildGetLocoInfo(address: 42);

        Assert.Multiple(() =>
        {
            AssertLengthPrefixMatchesPacket(packet);
            Assert.That(packet[4], Is.EqualTo(Z21Protocol.XHeader.X_GET_LOCO_INFO));
            Assert.That(packet[^1], Is.EqualTo(XorOverXBus(packet)));
        });
    }

    [Test]
    public void BuildSetTurnout_EncodesFAdrAsDccAddressMinusOne()
    {
        var packet = Z21Command.BuildSetTurnout(decoderAddress: 201, output: 0, activate: true);
        const int expectedFAdr = 201 - 1;

        Assert.Multiple(() =>
        {
            Assert.That(packet[5], Is.EqualTo((byte)((expectedFAdr >> 8) & 0xFF)));
            Assert.That(packet[6], Is.EqualTo((byte)(expectedFAdr & 0xFF)));
            Assert.That(packet[^1], Is.EqualTo(XorOverXBus(packet)));
        });
    }

    [Test]
    public void BuildSetTurnout_CommandByte_ReflectsQueueActivateAndOutputFlags()
    {
        var queuedActivateOutput2 = Z21Command.BuildSetTurnout(10, output: 1, activate: true, queue: true);
        var immediateDeactivateOutput1 = Z21Command.BuildSetTurnout(10, output: 0, activate: false, queue: false);

        Assert.Multiple(() =>
        {
            Assert.That(queuedActivateOutput2[7] & 0x80, Is.EqualTo(0x80), "Bits 7-6 are always 10.");
            Assert.That(queuedActivateOutput2[7] & 0x20, Is.EqualTo(0x20), "Queue flag set.");
            Assert.That(queuedActivateOutput2[7] & 0x08, Is.EqualTo(0x08), "Activate flag set.");
            Assert.That(queuedActivateOutput2[7] & 0x01, Is.EqualTo(0x01), "Output 2 selected.");

            Assert.That(immediateDeactivateOutput1[7] & 0x20, Is.EqualTo(0x00), "Queue flag clear.");
            Assert.That(immediateDeactivateOutput1[7] & 0x08, Is.EqualTo(0x00), "Activate flag clear.");
            Assert.That(immediateDeactivateOutput1[7] & 0x01, Is.EqualTo(0x00), "Output 1 selected.");
        });
    }

    [Test]
    public void BuildSetExtAccessory_EncodesAddressValueAndChecksum()
    {
        var packet = Z21Command.BuildSetExtAccessory(extAccessoryAddress: 5, commandValue: 200);

        Assert.Multiple(() =>
        {
            AssertLengthPrefixMatchesPacket(packet);
            Assert.That(packet[4], Is.EqualTo(Z21Protocol.XHeader.X_SET_EXT_ACCESSORY));
            Assert.That(packet[6], Is.EqualTo(5));
            Assert.That(packet[7], Is.EqualTo(200));
            Assert.That(packet[8], Is.EqualTo(0x00), "Reserved byte stays zero.");
            Assert.That(packet[^1], Is.EqualTo(XorOverXBus(packet)));
        });
    }

    [Test]
    public void BuildGetTurnoutInfo_EncodesFAdrAndChecksum()
    {
        var packet = Z21Command.BuildGetTurnoutInfo(decoderAddress: 50);

        Assert.Multiple(() =>
        {
            Assert.That(packet[4], Is.EqualTo(Z21Protocol.XHeader.X_GET_TURNOUT_INFO));
            Assert.That((packet[5] << 8) | packet[6], Is.EqualTo(50 - 1));
            Assert.That(packet[^1], Is.EqualTo(XorOverXBus(packet)));
        });
    }

    [TestCase(1)]
    [TestCase(127)]
    [TestCase(128)]
    [TestCase(9999)]
    public void BuildSetLocoDrive_AnyAddress_ProducesSelfConsistentChecksum(int address)
    {
        var packet = Z21Command.BuildSetLocoDrive(address, speed: 60, forward: true);

        Assert.That(packet[^1], Is.EqualTo(XorOverXBus(packet)));
    }
}
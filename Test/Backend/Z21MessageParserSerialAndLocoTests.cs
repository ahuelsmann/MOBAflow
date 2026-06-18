// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Protocol;

/// <summary>
/// Extended tests for <see cref="Z21MessageParser"/> beyond X-Bus status and system state:
/// packet type detection, serial/HW info, LAN_X_GET_VERSION and loco info decoding.
/// </summary>
[TestFixture]
internal sealed class Z21MessageParserSerialAndLocoTests
{
    [Test]
    public void IsLanXHeader_ReturnsFalseForNonXBusPacket()
    {
        var data = new byte[] { 0x04, 0x00, 0x10, 0x00 };

        Assert.That(Z21MessageParser.IsLanXHeader(data), Is.False);
    }

    [Test]
    public void IsRBusFeedback_RecognizesFeedbackHeader()
    {
        var data = new byte[] { 0x0F, 0x00, Z21Protocol.Header.LAN_RMBUS_DATACHANGED, 0x00 };

        Assert.That(Z21MessageParser.IsRBusFeedback(data), Is.True);
    }

    [Test]
    public void TryParseSerialNumber_ReadsLittleEndianValue()
    {
        var data = new byte[] { 0x08, 0x00, Z21Protocol.Header.LAN_GET_SERIAL_NUMBER, 0x00, 0x39, 0x30, 0x01, 0x00 };

        var ok = Z21MessageParser.TryParseSerialNumber(data, out var serial);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(serial, Is.EqualTo(0x00013039u));
        });
    }

    [Test]
    public void TryParseSerialNumber_ShortPacket_ReturnsFalse()
    {
        var ok = Z21MessageParser.TryParseSerialNumber([0x04, 0x00, 0x10, 0x00], out _);

        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryParseHwInfo_ReadsHardwareAndFirmwareCodes()
    {
        var data = new byte[]
        {
            0x0C, 0x00, Z21Protocol.Header.LAN_GET_HWINFO, 0x00,
            0x02, 0x02, 0x00, 0x00,
            0x43, 0x01, 0x00, 0x00
        };

        var ok = Z21MessageParser.TryParseHwInfo(data, out var hw, out var fw);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(hw, Is.EqualTo(0x00000202u));
            Assert.That(fw, Is.EqualTo(0x00000143u));
        });
    }

    [Test]
    public void TryParseLanXGetVersionResponse_ParsesXbusAndCommandStationId()
    {
        var data = new byte[] { 0x09, 0x00, 0x40, 0x00, 0x63, 0x21, 0x05, 0x00, 0x12 };

        var ok = Z21MessageParser.TryParseLanXGetVersionResponse(data, out var xbusVer, out var cmdstId);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(xbusVer, Is.EqualTo(0x05));
            Assert.That(cmdstId, Is.EqualTo(0x0012));
        });
    }

    [Test]
    public void TryParseXBusStatus_UnknownXHeader_ReturnsNull()
    {
        var data = new byte[] { 0x07, 0x00, 0x40, 0x00, 0x99, 0x00, 0x00 };

        Assert.That(Z21MessageParser.TryParseXBusStatus(data), Is.Null);
    }

    [Test]
    public void TryParseLocoInfo_DecodesAddressSpeedDirectionAndFunctions()
    {
        var data = new byte[]
        {
            0x0E, 0x00, 0x40, 0x00,
            Z21Protocol.XHeader.X_LOCO_INFO,
            0x00, 0x03,
            0x03,
            0x8B,
            0x10,
            0x00,
            0x00,
            0x00,
            0x00
        };

        var ok = Z21MessageParser.TryParseLocoInfo(data, out var loco);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(loco, Is.Not.Null);
            Assert.That(loco!.Address, Is.EqualTo(3));
            Assert.That(loco.Speed, Is.EqualTo(10));
            Assert.That(loco.IsForward, Is.True);
            Assert.That(loco.SpeedSteps, Is.EqualTo(128));
            Assert.That(loco.IsF0On, Is.True);
            Assert.That(loco.IsF1On, Is.False);
        });
    }

    [Test]
    public void TryParseLocoInfo_NonLocoPacket_ReturnsFalse()
    {
        var data = Z21Command.BuildTrackPowerOn();

        var ok = Z21MessageParser.TryParseLocoInfo(data, out var loco);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(loco, Is.Null);
        });
    }
}
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Model;

/// <summary>
/// Tests for <see cref="Z21TrafficPacket"/> display helpers used by the traffic monitor.
/// </summary>
[TestFixture]
internal sealed class Z21TrafficPacketTests
{
    [Test]
    public void DataHex_FormatsBytesWithSpaces()
    {
        var packet = new Z21TrafficPacket { Data = [0x0F, 0x00, 0x80, 0x00] };

        Assert.That(packet.DataHex, Is.EqualTo("0F 00 80 00"));
    }

    [TestCase(true, "↑")]
    [TestCase(false, "↓")]
    public void DirectionIcon_ReflectsPacketDirection(bool isSent, string expectedIcon)
    {
        var packet = new Z21TrafficPacket { IsSent = isSent };

        Assert.That(packet.DirectionIcon, Is.EqualTo(expectedIcon));
    }

    [Test]
    public void InPortFormatted_WithMultipleActivePorts_JoinsAllValues()
    {
        var packet = new Z21TrafficPacket
        {
            InPort = 1,
            AllInPorts = [1, 4, 8]
        };

        Assert.That(packet.InPortFormatted, Is.EqualTo("1,4,8"));
    }

    [Test]
    public void InPortFormatted_WithSinglePort_ReturnsSingleValue()
    {
        var packet = new Z21TrafficPacket
        {
            InPort = 5,
            AllInPorts = [5]
        };

        Assert.That(packet.InPortFormatted, Is.EqualTo("5"));
    }
}
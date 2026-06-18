// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Model;
using Moba.Backend.Protocol;
using Moba.Backend.Service;

/// <summary>
/// Tests for <see cref="Z21Monitor"/> traffic logging and static packet-type parsing.
/// Covers the circular buffer, direction metadata and R-Bus feedback enrichment used by the traffic monitor.
/// </summary>
[TestFixture]
internal sealed class Z21MonitorTests
{
    private Z21Monitor _monitor = null!;

    [SetUp]
    public void SetUp()
    {
        _monitor = new Z21Monitor(NullLogger<Z21Monitor>.Instance);
    }

    [Test]
    public void ParsePacketType_TooShort_ReturnsUnknown()
    {
        Assert.That(Z21Monitor.ParsePacketType([0x01, 0x00]), Is.EqualTo("Unknown (too short)"));
    }

    [Test]
    public void ParsePacketType_LanXTrackPower_ResolvesNestedCommand()
    {
        var packet = Z21Command.BuildTrackPowerOn();

        Assert.That(Z21Monitor.ParsePacketType(packet), Is.EqualTo("LAN_X_SET_TRACK_POWER"));
    }

    [Test]
    public void ParsePacketType_RBusFeedback_ReturnsDatChangedHeader()
    {
        var packet = new byte[] { 0x0F, 0x00, 0x80, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        Assert.That(Z21Monitor.ParsePacketType(packet), Is.EqualTo("LAN_RMBUS_DATACHANGED"));
    }

    [Test]
    public void ParsePacketType_UnknownHeader_IncludesHexCode()
    {
        var packet = new byte[] { 0x04, 0x00, 0xFF, 0x00 };

        Assert.That(Z21Monitor.ParsePacketType(packet), Is.EqualTo("Unknown (0x00FF)"));
    }

    [Test]
    public void LogSentPacket_StoresSentDirectionAndDetails()
    {
        var data = Z21Command.BuildHandshake();

        _monitor.LogSentPacket(data, "Handshake", "startup");

        var packet = _monitor.GetPackets().Single();

        Assert.Multiple(() =>
        {
            Assert.That(packet.IsSent, Is.True);
            Assert.That(packet.PacketType, Is.EqualTo("Handshake"));
            Assert.That(packet.Details, Is.EqualTo("startup"));
            Assert.That(packet.IsFeedbackRelated, Is.False);
            Assert.That(packet.Data, Is.EqualTo(data));
        });
    }

    [Test]
    public void LogReceivedPacket_WithActiveFeedback_EnrichesInPortMetadata()
    {
        var packetBytes = BuildFeedbackPacket(group: 0, dataBytes: [0x04, 0, 0, 0, 0, 0, 0, 0]);
        Z21TrafficPacket? logged = null;
        _monitor.PacketLogged += (_, p) => logged = p;

        _monitor.LogReceivedPacket(packetBytes, "Feedback");

        Assert.That(logged, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(logged!.IsSent, Is.False);
            Assert.That(logged.IsFeedbackRelated, Is.True);
            Assert.That(logged.InPort, Is.EqualTo(3));
            Assert.That(logged.AllInPorts, Is.EqualTo(new[] { 3 }));
        });
    }

    [Test]
    public void LogReceivedPacket_WithZeroFeedbackBits_IsNotMarkedFeedbackRelated()
    {
        var packetBytes = BuildFeedbackPacket(group: 0, dataBytes: [0, 0, 0, 0, 0, 0, 0, 0]);

        _monitor.LogReceivedPacket(packetBytes);

        var packet = _monitor.GetPackets().Single();
        Assert.That(packet.IsFeedbackRelated, Is.False);
    }

    [Test]
    public void GetPackets_ReturnsNewestFirst()
    {
        _monitor.LogSentPacket([0x01], "first");
        _monitor.LogSentPacket([0x02], "second");

        var types = _monitor.GetPackets().Select(p => p.PacketType).ToList();

        Assert.That(types, Is.EqualTo(new[] { "second", "first" }));
    }

    [Test]
    public void Clear_RemovesAllPackets()
    {
        _monitor.LogSentPacket([0x01]);
        _monitor.Clear();

        Assert.That(_monitor.GetPackets(), Is.Empty);
    }

    private static byte[] BuildFeedbackPacket(int group, byte[] dataBytes)
    {
        var packet = new byte[14];
        packet[0] = 0x0F;
        packet[1] = 0x00;
        packet[2] = Z21Protocol.Header.LAN_RMBUS_DATACHANGED;
        packet[3] = 0x00;
        packet[4] = (byte)group;
        Array.Copy(dataBytes, 0, packet, 5, Math.Min(8, dataBytes.Length));
        return packet;
    }
}
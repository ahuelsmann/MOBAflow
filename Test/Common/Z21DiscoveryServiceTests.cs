// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Backend.Discovery;
using Moba.Backend.Protocol;

using System.Net;

/// <summary>
/// Unit tests for Z21 LAN response recognition used during subnet discovery.
/// </summary>
[TestFixture]
internal sealed class Z21DiscoveryServiceTests
{
    [Test]
    public void IsZ21Response_ReturnsFalse_WhenPacketTooShort()
    {
        Assert.That(Z21DiscoveryService.IsZ21Response([0x04, 0x00]), Is.False);
    }

    [Test]
    public void IsZ21Response_ReturnsTrue_ForLanSystemStateHeader()
    {
        var packet = new byte[] { 0x04, 0x00, Z21Protocol.Header.LAN_SYSTEMSTATE, 0x00 };
        Assert.That(Z21DiscoveryService.IsZ21Response(packet), Is.True);
    }

    [Test]
    public void IsZ21Response_ReturnsTrue_ForLanXHeader()
    {
        var packet = new byte[] { 0x04, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00 };
        Assert.That(Z21DiscoveryService.IsZ21Response(packet), Is.True);
    }

    [Test]
    public void IsZ21Response_ReturnsFalse_WhenThirdHeaderByteIsNonZero()
    {
        var packet = new byte[] { 0x04, 0x00, Z21Protocol.Header.LAN_SYSTEMSTATE, 0x01 };
        Assert.That(Z21DiscoveryService.IsZ21Response(packet), Is.False);
    }

    [Test]
    public void TryReadSerialNumberResponse_ReadsAddressPortAndSerialNumber()
    {
        var packet = new byte[] { 0x08, 0x00, Z21Protocol.Header.LAN_GET_SERIAL_NUMBER, 0x00, 0x39, 0x30, 0x00, 0x00 };
        var sender = new IPEndPoint(IPAddress.Parse("192.168.0.111"), 21105);

        var read = Z21DiscoveryService.TryReadSerialNumberResponse(packet, sender, out var z21);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(read, Is.True);
            Assert.That(z21, Is.EqualTo(new Moba.Common.Discovery.DiscoveredZ21("192.168.0.111", 21105, 12345)));
        }
    }

    [Test]
    public void TryReadSerialNumberResponse_IgnoresOtherPackets()
    {
        var packet = new byte[] { 0x04, 0x00, Z21Protocol.Header.LAN_SYSTEMSTATE, 0x00 };
        var sender = new IPEndPoint(IPAddress.Parse("192.168.0.111"), 21105);

        Assert.That(Z21DiscoveryService.TryReadSerialNumberResponse(packet, sender, out _), Is.False);
    }
}

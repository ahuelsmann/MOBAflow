// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Backend.Discovery;
using Moba.Backend.Protocol;

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
}

// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Protocol;

/// <summary>
/// Unit tests for Z21Protocol static class (constants and helper methods).
/// </summary>
[TestFixture]
internal class Z21ProtocolTests
{
    [Test]
    public void ToHex_FormatsByteArrayCorrectly()
    {
        var data = new byte[] { 0x04, 0x00, 0x10, 0x00 };

        var result = Z21Protocol.ToHex(data);

        Assert.That(result, Is.EqualTo("04-00-10-00"));
    }

    [Test]
    public void ToHex_EmptyArray_ReturnsEmptyString()
    {
        var result = Z21Protocol.ToHex([]);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ToHexSpaced_ReplacesDashesWithSpaces()
    {
        var data = new byte[] { 0x0F, 0x00, 0x80, 0x00 };

        var result = Z21Protocol.ToHexSpaced(data);

        Assert.That(result, Is.EqualTo("0F 00 80 00"));
    }

    [Test]
    public void DefaultPort_Is21105()
    {
        Assert.That(Z21Protocol.DefaultPort, Is.EqualTo(21105));
    }

    [Test]
    public void AlternativePort_Is21106()
    {
        Assert.That(Z21Protocol.AlternativePort, Is.EqualTo(21106));
    }

    [Test]
    public void Header_LAN_GET_SERIAL_NUMBER_Is0x10()
    {
        Assert.That(Z21Protocol.Header.LAN_GET_SERIAL_NUMBER, Is.EqualTo(0x10));
    }

    [Test]
    public void Header_LAN_X_HEADER_Is0x40()
    {
        Assert.That(Z21Protocol.Header.LAN_X_HEADER, Is.EqualTo(0x40));
    }

    [Test]
    public void BroadcastFlags_Basic_IncludesDrivingRbusSystemState()
    {
        Assert.That(Z21Protocol.BroadcastFlags.Driving, Is.EqualTo(0x0000_0001u));
        Assert.That(Z21Protocol.BroadcastFlags.Rbus, Is.EqualTo(0x0000_0002u));
        Assert.That(Z21Protocol.BroadcastFlags.SystemState, Is.EqualTo(0x0000_0100u));
    }

    [Test]
    public void TrackPowerDb0_OffAndOn_HaveExpectedValues()
    {
        Assert.That(Z21Protocol.TrackPowerDb0.OFF, Is.EqualTo(0x80));
        Assert.That(Z21Protocol.TrackPowerDb0.ON, Is.EqualTo(0x81));
    }

    [Test]
    public void CentralState_EmergencyStop_Is0x01()
    {
        Assert.That(Z21Protocol.CentralState.EmergencyStop, Is.EqualTo(0x01));
    }

    [Test]
    public void HardwareType_Z21Xl_Is0x00000211()
    {
        Assert.That(Z21Protocol.HardwareType.Z21Xl, Is.EqualTo(0x0000_0211u));
    }
}

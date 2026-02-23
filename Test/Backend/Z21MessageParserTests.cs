// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Protocol;

[TestFixture]
internal class Z21MessageParserTests
{
    [Test]
    public void TryParseXBusStatus_ParsesFlags()
    {
        // 07-00-40-00 62 00 07 -> status changed with flags 0x07 (emergency + trackoff + short)
        var data = new byte[] { 0x07,0x00,0x40,0x00, 0x62, 0x00, 0x07 };
        var status = Z21MessageParser.TryParseXBusStatus(data);
        Assert.That(status, Is.Not.Null);
        Assert.That(status!.EmergencyStop, Is.True);
        Assert.That(status.TrackOff, Is.True);
        Assert.That(status.ShortCircuit, Is.True);
        Assert.That(status.Programming, Is.False);
    }

    [Test]
    public void TryParseSystemState_Works()
    {
        // 14-00-84-00 + 16 bytes payload (fake values)
        var data = new byte[]
        {
            0x14,0x00,0x84,0x00,
            0x10,0x00, // mainCurrent 16
            0x20,0x00, // progCurrent 32
            0x30,0x00, // filteredMainCurrent 48
            0x40,0x00, // temperature 64
            0x50,0x00, // supplyVoltage 80
            0x60,0x00, // vccVoltage 96
            0xAA,      // centralState
            0xBB       // centralStateEx
        };
        var ok = Z21MessageParser.TryParseSystemState(data, out var main, out var prog, out var filt, out var temp, out var supply, out var vcc, out var st, out var stex);
        Assert.That(ok, Is.True);
        Assert.That(main, Is.EqualTo(16));
        Assert.That(prog, Is.EqualTo(32));
        Assert.That(filt, Is.EqualTo(48));
        Assert.That(temp, Is.EqualTo(64));
        Assert.That(supply, Is.EqualTo(80));
        Assert.That(vcc, Is.EqualTo(96));
        Assert.That(st, Is.EqualTo(0xAA));
        Assert.That(stex, Is.EqualTo(0xBB));
    }
}

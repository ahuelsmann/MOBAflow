// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Model;
using Moba.Backend.Protocol;

/// <summary>
/// Additional <see cref="Z21MessageParser"/> loco-info tests for speed-step modes,
/// direction decoding and extended function bits beyond F0/F1.
/// </summary>
[TestFixture]
internal sealed class Z21MessageParserLocoFunctionsTests
{
    [Test]
    public void TryParseLocoInfo_14SpeedSteps_Uses14StepMode()
    {
        var data = BuildLocoPacket(speedStepsNibble: 0x00, speedByte: 0x00, functions: [0, 0, 0, 0, 0]);

        var ok = Z21MessageParser.TryParseLocoInfo(data, out var loco);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(loco!.SpeedSteps, Is.EqualTo(14));
            Assert.That(loco.IsForward, Is.False);
            Assert.That(loco.Speed, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryParseLocoInfo_28SpeedSteps_Uses28StepMode()
    {
        var data = BuildLocoPacket(speedStepsNibble: 0x02, speedByte: 0x03, functions: [0, 0, 0, 0, 0]);

        var ok = Z21MessageParser.TryParseLocoInfo(data, out var loco);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(loco!.SpeedSteps, Is.EqualTo(28));
            Assert.That(loco.Speed, Is.EqualTo(2), "Encoded speed 3 maps to step 2 after e-stop offset.");
        });
    }

    [Test]
    public void TryParseLocoInfo_HigherFunctionBits_DecodeThroughF20()
    {
        var data = BuildLocoPacket(
            speedStepsNibble: 0x03,
            speedByte: 0x00,
            functions: [0x00, 0x01, 0x08, 0xC8, 0x00]);

        var ok = Z21MessageParser.TryParseLocoInfo(data, out var loco);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(loco!.GetFunction(5), Is.True);
            Assert.That(loco.GetFunction(12), Is.True);
            Assert.That(loco.GetFunction(16), Is.True);
            Assert.That(loco.GetFunction(19), Is.True);
            Assert.That(loco.GetFunction(20), Is.True);
        });
    }

    [Test]
    public void LocoInfo_GetFunction_ReturnsFalseForUnsetBits()
    {
        var loco = new LocoInfo { Functions = 0x04 };

        Assert.Multiple(() =>
        {
            Assert.That(loco.GetFunction(2), Is.True);
            Assert.That(loco.GetFunction(3), Is.False);
            Assert.That(loco.IsF0On, Is.False);
            Assert.That(loco.IsF1On, Is.False);
        });
    }

    private static byte[] BuildLocoPacket(byte speedStepsNibble, byte speedByte, byte[] functions)
    {
        return
        [
            0x0E, 0x00, 0x40, 0x00,
            Z21Protocol.XHeader.X_LOCO_INFO,
            0x00, 0x03,
            speedStepsNibble,
            speedByte,
            functions[0],
            functions[1],
            functions[2],
            functions[3],
            functions[4]
        ];
    }
}
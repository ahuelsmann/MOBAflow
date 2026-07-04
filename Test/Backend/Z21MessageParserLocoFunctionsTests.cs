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
        // DB5=0x81 (F5 + F12), DB6=0x42 (F14 + F19), DB7=0x01 (F21)
        var data = BuildLocoPacket(
            speedStepsNibble: 0x03,
            speedByte: 0x00,
            functions: [0x00, 0x81, 0x42, 0x01, 0x00]);

        var ok = Z21MessageParser.TryParseLocoInfo(data, out var loco);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(loco!.GetFunction(5), Is.True);
            Assert.That(loco.GetFunction(12), Is.True);
            Assert.That(loco.GetFunction(14), Is.True);
            Assert.That(loco.GetFunction(19), Is.True);
            Assert.That(loco.GetFunction(21), Is.True);
            Assert.That(loco.GetFunction(10), Is.False, "F10 must stay off when DB6 carries F14");
        });
    }

    [Test]
    public void TryParseLocoInfo_F14On_DoesNotActivateF10()
    {
        // DB6 bit1 = F14 per Z21 spec section 4.4
        var data = BuildLocoPacket(
            speedStepsNibble: 0x04,
            speedByte: 0x00,
            functions: [0x00, 0x00, 0x02, 0x00, 0x00]);

        var ok = Z21MessageParser.TryParseLocoInfo(data, out var loco);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(loco!.GetFunction(14), Is.True);
            Assert.That(loco.GetFunction(10), Is.False);
            Assert.That(loco.GetFunction(15), Is.False);
        });
    }

    [Test]
    public void TryParseLocoInfo_F15OffAfterF14On_ReflectsIndependentBits()
    {
        var data = BuildLocoPacket(
            speedStepsNibble: 0x04,
            speedByte: 0x00,
            functions: [0x00, 0x00, 0x02, 0x00, 0x00]);

        Z21MessageParser.TryParseLocoInfo(data, out var withF14);

        data = BuildLocoPacket(
            speedStepsNibble: 0x04,
            speedByte: 0x00,
            functions: [0x00, 0x00, 0x00, 0x00, 0x00]);

        Z21MessageParser.TryParseLocoInfo(data, out var allOff);

        Assert.Multiple(() =>
        {
            Assert.That(withF14!.GetFunction(14), Is.True);
            Assert.That(withF14.GetFunction(15), Is.False);
            Assert.That(allOff!.GetFunction(14), Is.False);
            Assert.That(allOff.GetFunction(15), Is.False);
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
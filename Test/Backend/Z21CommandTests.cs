// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Protocol;

[TestFixture]
public class Z21CommandTests
{
    [TestCase(203, 0, false, false, "0A-00-40-00-53-03-2C-80-FC")]
    [TestCase(203, 0, true, false, "0A-00-40-00-53-03-2C-88-F4")]
    public void BuildSetTurnout_EncodesFAdr(
        int decoderAddress,
        int output,
        bool activate,
        bool queue,
        string expected)
    {
        var command = Z21Command.BuildSetTurnout(decoderAddress, output, activate, queue);

        Assert.That(BitConverter.ToString(command), Is.EqualTo(expected));
    }

    [TestCase(203, "09-00-40-00-43-03-2C-6C")]
    public void BuildGetTurnoutInfo_EncodesFAdr(int decoderAddress, string expected)
    {
        var command = Z21Command.BuildGetTurnoutInfo(decoderAddress);

        Assert.That(BitConverter.ToString(command), Is.EqualTo(expected));
    }
}

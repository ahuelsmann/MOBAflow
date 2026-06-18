// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Model;

/// <summary>
/// Tests for <see cref="Z21VersionInfo"/> display helpers that translate raw Z21 codes
/// into human-readable hardware and firmware strings shown in connection diagnostics.
/// </summary>
[TestFixture]
internal sealed class Z21VersionInfoTests
{
    [TestCase(0x00000201u, "z21start")]
    [TestCase(0x00000202u, "Z21")]
    [TestCase(0x00000206u, "Z21a")]
    public void HardwareType_MapsKnownCodes(uint code, string expectedName)
    {
        var info = new Z21VersionInfo { HardwareTypeCode = code };

        Assert.That(info.HardwareType, Is.EqualTo(expectedName));
    }

    [Test]
    public void HardwareType_UnknownCode_IncludesHexValue()
    {
        var info = new Z21VersionInfo { HardwareTypeCode = 0xDEADBEEF };

        Assert.That(info.HardwareType, Is.EqualTo("Unknown (0xDEADBEEF)"));
    }

    [Test]
    public void FirmwareVersion_DecodesBcdMajorMinor()
    {
        var info = new Z21VersionInfo { FirmwareVersionCode = 0x0143 };

        Assert.That(info.FirmwareVersion, Is.EqualTo("V1.43"));
    }

    [Test]
    public void ToString_IncludesSerialHardwareAndFirmware()
    {
        var info = new Z21VersionInfo
        {
            SerialNumber = 101953,
            HardwareTypeCode = 0x00000206,
            FirmwareVersionCode = 0x0143
        };

        Assert.That(info.ToString(), Is.EqualTo("S/N: 101953, HW: Z21a, FW: V1.43"));
    }
}
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;

[TestFixture]
[Category("Unit")]
internal sealed class DisplayStandardPatternRequirementsTests
{
    [Test]
    public void EvaluateFrameTransfer_Should_AllowCapabilities_WhenOnlyPatternExceedsSafetyLimit()
    {
        var capabilities = new CapabilitiesResponsePayload(
            DisplayProtocol.CurrentVersion,
            ushort.MaxValue,
            ushort.MaxValue,
            DisplayProtocol.DEFAULT_MAX_DATAGRAM_LENGTH,
            DisplayProtocol.DEFAULT_MAX_PAYLOAD_LENGTH,
            DisplayPixelFormatFlags.Rgb565BigEndian,
            DisplayRotationFlags.Degrees0,
            DisplayOptionalCommandFlags.None,
            DisplayFrameCapabilityFlags.FullFrameStaging
                | DisplayFrameCapabilityFlags.RegionTransfer
                | DisplayFrameCapabilityFlags.AtomicPresentation,
            DisplayAcknowledgementMode.ControlAndCompletion,
            42,
            "test-device",
            "1.0.0",
            "test-adapter");

        var transferResult = DisplayStandardPatternRequirements.EvaluateFrameTransfer(capabilities);
        var patternResult = DisplayStandardPatternRequirements.EvaluateStandardPattern(capabilities);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transferResult, Is.EqualTo(DisplayStandardPatternIncompatibility.None));
            Assert.That(
                patternResult,
                Is.EqualTo(DisplayStandardPatternIncompatibility.FrameExceedsHostSafetyLimit));
        }
    }
}

// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

/// <summary>Identifies why negotiated capabilities cannot present the host-rendered standard pattern.</summary>
public enum DisplayStandardPatternIncompatibility
{
    /// <summary>The negotiated capabilities support the standard pattern.</summary>
    None,
    /// <summary>The device does not support RGB565 big-endian pixels.</summary>
    MissingRgb565BigEndian,
    /// <summary>The device does not support zero-degree rotation.</summary>
    MissingZeroDegreeRotation,
    /// <summary>The device does not provide all required atomic frame-transfer guarantees.</summary>
    MissingAtomicFrameTransfer,
    /// <summary>The native frame would exceed the host allocation safety limit.</summary>
    FrameExceedsHostSafetyLimit
}

/// <summary>Evaluates the negotiated capabilities required by the host-rendered standard pattern.</summary>
public static class DisplayStandardPatternRequirements
{
    /// <summary>Maximum host-rendered RGB565 frame size accepted for one standard pattern.</summary>
    public const long MaximumHostFrameByteCount = 4L * 1024 * 1024;

    private const DisplayFrameCapabilityFlags RequiredFrameCapabilities =
        DisplayFrameCapabilityFlags.FullFrameStaging
        | DisplayFrameCapabilityFlags.RegionTransfer
        | DisplayFrameCapabilityFlags.AtomicPresentation;

    /// <summary>
    /// Returns the structured reason why capabilities cannot transfer the required RGB565 frame.
    /// </summary>
    /// <param name="capabilities">Negotiated device capabilities.</param>
    /// <returns>The incompatibility reason, or <see cref="DisplayStandardPatternIncompatibility.None"/>.</returns>
    public static DisplayStandardPatternIncompatibility EvaluateFrameTransfer(
        CapabilitiesResponsePayload capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        if (!capabilities.PixelFormats.HasFlag(DisplayPixelFormatFlags.Rgb565BigEndian))
        {
            return DisplayStandardPatternIncompatibility.MissingRgb565BigEndian;
        }

        if (!capabilities.Rotations.HasFlag(DisplayRotationFlags.Degrees0))
        {
            return DisplayStandardPatternIncompatibility.MissingZeroDegreeRotation;
        }

        if ((capabilities.FrameCapabilities & RequiredFrameCapabilities) != RequiredFrameCapabilities)
        {
            return DisplayStandardPatternIncompatibility.MissingAtomicFrameTransfer;
        }

        return DisplayStandardPatternIncompatibility.None;
    }

    /// <summary>
    /// Returns the structured reason why capabilities cannot present the host-rendered standard pattern.
    /// </summary>
    /// <param name="capabilities">Negotiated device capabilities.</param>
    /// <returns>The incompatibility reason, or <see cref="DisplayStandardPatternIncompatibility.None"/>.</returns>
    public static DisplayStandardPatternIncompatibility EvaluateStandardPattern(
        CapabilitiesResponsePayload capabilities)
    {
        var transferIncompatibility = EvaluateFrameTransfer(capabilities);
        if (transferIncompatibility != DisplayStandardPatternIncompatibility.None)
        {
            return transferIncompatibility;
        }

        var frameByteCount = (long)capabilities.Width * capabilities.Height * 2;
        if (frameByteCount > MaximumHostFrameByteCount)
        {
            return DisplayStandardPatternIncompatibility.FrameExceedsHostSafetyLimit;
        }

        return DisplayStandardPatternIncompatibility.None;
    }
}

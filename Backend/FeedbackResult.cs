// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend;

using Protocol;

/// <summary>
/// Represents a Z21 feedback event with the active InPort number.
/// Extracts the InPort from a LAN_RMBUS_DATACHANGED packet using bit-level analysis.
/// 
/// Historical Note: Previously used content[5] directly, which was incorrect.
/// The data bytes represent feedback STATE (bit pattern), not InPort number.
/// Now uses Z21FeedbackParser.ExtractFirstInPort() for correct bit-to-InPort conversion.
/// </summary>
public class FeedbackResult
{
    /// <summary>
    /// Feedback point from the feedback module (R-BUS).
    /// 1-based InPort number extracted from the first set bit in the feedback data.
    /// Returns 0 if no feedback is active (should not occur in normal operation).
    /// </summary>
    public int InPort { get; }

    /// <summary>
    /// Raw feedback packet data (for debugging/logging).
    /// </summary>
    public byte[] RawData { get; }

    public FeedbackResult(byte[] content)
    {
        if (content.Length < 6)
            throw new ArgumentException("Invalid feedback packet: must be at least 6 bytes", nameof(content));

        RawData = content;
        InPort = Z21FeedbackParser.ExtractFirstInPort(content);

        if (InPort == 0)
        {
            // This should not happen in normal operation - Z21 only sends feedback when bits are set
            // Log this as a warning if it occurs
            System.Diagnostics.Debug.WriteLine($"⚠️ FeedbackResult: No active InPort detected in packet: {BitConverter.ToString(content)}");
        }
    }
}


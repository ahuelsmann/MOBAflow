// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend;

using System;

/// <summary>
/// Extracts the feedback ID from the byte array to set the value for the InPort property.
/// </summary>
public class FeedbackResult(byte[] content)
{
    /// <summary>
    /// Feedback point from the feedback module (R-BUS).
    /// </summary>
    public int InPort { get; } = content.Length >= 6 ? content[5] : throw new ArgumentException("Invalid content.");
}

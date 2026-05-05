// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Runtime;

/// <summary>
/// Raised after each attempt to push a rendered frame to the UDP or serial transport.
/// </summary>
public sealed class FrameTransmissionCompletedEventArgs : EventArgs
{
    public FrameTransmissionCompletedEventArgs(DateTime timestamp, bool success, string? failureMessage)
    {
        Timestamp = timestamp;
        Success = success;
        FailureMessage = failureMessage;
    }

    public DateTime Timestamp { get; }
    public bool Success { get; }
    /// <summary>Non-null only when <see cref="Success"/> is false.</summary>
    public string? FailureMessage { get; }
}
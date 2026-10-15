// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>A newer announcement replaced this playback before it finished, so it was only partly heard.</summary>
public sealed class WavePlaybackInterruptedException : Exception
{
    /// <summary>Creates the exception with a display-safe default message.</summary>
    public WavePlaybackInterruptedException() : base("Playback was replaced by a newer announcement.") { }

    /// <summary>Creates the exception with a display-safe message.</summary>
    public WavePlaybackInterruptedException(string message) : base(message) { }

    /// <summary>Creates the exception with a display-safe message and its cause.</summary>
    public WavePlaybackInterruptedException(string message, Exception innerException) : base(message, innerException) { }
}

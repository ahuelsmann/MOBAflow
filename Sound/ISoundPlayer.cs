// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

/// <summary>
/// Interface for playing sound files.
/// Allows for platform-specific implementations and testability.
/// </summary>
public interface ISoundPlayer
{
    /// <summary>
    /// Plays a wave file from the specified path asynchronously.
    /// </summary>
    /// <param name="waveFile">Full path to the .wav file to play</param>
    /// <param name="cancellationToken">Cancellation token to stop playback</param>
    Task PlayAsync(string waveFile, CancellationToken cancellationToken = default);
}
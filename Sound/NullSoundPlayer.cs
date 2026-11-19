// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

/// <summary>
/// Null Object implementation of ISoundPlayer.
/// Used on platforms without audio support (e.g., WebApp, headless servers).
/// Silently ignores all audio playback requests.
/// </summary>
public class NullSoundPlayer : ISoundPlayer
{
    /// <summary>
    /// Does nothing. Audio playback is not supported on this platform.
    /// </summary>
    public Task PlayAsync(string waveFile, CancellationToken cancellationToken = default)
    {
        // Silent no-op - platform doesn't support audio
        return Task.CompletedTask;
    }
}

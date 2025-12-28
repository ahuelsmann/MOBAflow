// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using Microsoft.Extensions.Logging;

using System.Media;
using System.Runtime.Versioning;
using System.Threading.Tasks;

/// <summary>
/// Windows-specific implementation of ISoundPlayer using System.Media.SoundPlayer.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsSoundPlayer(ILogger<WindowsSoundPlayer> logger) : ISoundPlayer
{
    /// <summary>
    /// Plays a wave file from the specified path asynchronously.
    /// Waits for 1.5 seconds to allow sound to complete (async, cancelable).
    /// </summary>
    /// <param name="waveFile">Full path to the .wav file to play</param>
    /// <param name="cancellationToken">Cancellation token to stop playback</param>
    public async Task PlayAsync(string waveFile, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔊 Playing sound file: {waveFile}");
            logger.LogInformation("Playing sound file: {WaveFile}", waveFile);
            
            // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
            using var player = new SoundPlayer { SoundLocation = waveFile };
            player.Play();
            
            // Async delay instead of blocking Thread.Sleep
            await Task.Delay(1500, cancellationToken).ConfigureAwait(false);
            
            Console.WriteLine($"✅ Sound file played successfully: {waveFile}");
            logger.LogDebug("Sound file played successfully: {WaveFile}", waveFile);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"⏸ Sound playback cancelled: {waveFile}");
            logger.LogInformation("Sound playback cancelled: {WaveFile}", waveFile);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to play sound file: {waveFile} - Error: {ex.Message}");
            logger.LogError(ex, "Failed to play sound file: {WaveFile}", waveFile);
            throw new InvalidOperationException($"Failed to play sound file: {waveFile}", ex);
        }
    }

    /// <summary>
    /// Plays a wave file from the specified path (synchronous wrapper for backward compatibility).
    /// </summary>
    /// <param name="waveFile">Full path to the .wav file to play</param>
    [Obsolete("Use PlayAsync instead for non-blocking I/O and cancellation support")]
    public void Play(string waveFile) => PlayAsync(waveFile).GetAwaiter().GetResult();
}
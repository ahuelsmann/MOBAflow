// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using Microsoft.Extensions.Logging;
using System.Media;
using System.Runtime.Versioning;

/// <summary>
/// Windows-specific implementation of ISoundPlayer using System.Media.SoundPlayer.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsSoundPlayer(ILogger<WindowsSoundPlayer> logger) : ISoundPlayer
{
    /// <summary>
    /// Plays a wave file from the specified path asynchronously.
    /// Waits for the sound to complete playback before returning (blocking on background thread).
    /// </summary>
    /// <param name="waveFile">Full path to the .wav file to play</param>
    /// <param name="cancellationToken">Cancellation token to stop playback</param>
    public async Task PlayAsync(string waveFile, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Playing sound file: {WaveFile}", waveFile);
            
            // Run on background thread to avoid blocking UI
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
                using var player = new SoundPlayer();
                player.SoundLocation = waveFile;

                // PlaySync() blocks until sound completes - perfect for sequential execution!
                player.PlaySync();
            }, cancellationToken).ConfigureAwait(false);
            
            logger.LogDebug("Sound file played successfully: {WaveFile}", waveFile);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation("Sound playback cancelled: {WaveFile}", waveFile);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to play sound file: {WaveFile}", waveFile);
                        throw new InvalidOperationException($"Failed to play sound file: {waveFile}", ex);
                    }
                }
            }
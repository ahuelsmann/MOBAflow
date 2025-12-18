// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

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
    /// Plays a wave file from the specified path.
    /// Blocks for 1.5 seconds to allow sound to complete.
    /// </summary>
    /// <param name="waveFile">Full path to the .wav file to play</param>
    public void Play(string waveFile)
    {
        try
        {
            Console.WriteLine($"🔊 Playing sound file: {waveFile}");
            logger.LogInformation("Playing sound file: {WaveFile}", waveFile);
            
            using var player = new SoundPlayer
            {
                SoundLocation = waveFile
            };
            player.Play();
            Thread.Sleep(1500);
            
            Console.WriteLine($"✅ Sound file played successfully: {waveFile}");
            logger.LogDebug("Sound file played successfully: {WaveFile}", waveFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to play sound file: {waveFile} - Error: {ex.Message}");
            logger.LogError(ex, "Failed to play sound file: {WaveFile}", waveFile);
            throw new InvalidOperationException($"Failed to play sound file: {waveFile}", ex);
        }
    }
}

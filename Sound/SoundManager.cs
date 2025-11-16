using System.Media;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Moba.Sound;

/// <summary>
/// Windows-specific implementation of ISoundPlayer using System.Media.SoundPlayer.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsSoundPlayer : ISoundPlayer
{
    private readonly ILogger<WindowsSoundPlayer> _logger;

    public WindowsSoundPlayer(ILogger<WindowsSoundPlayer> logger)
    {
        _logger = logger;
    }

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
            _logger.LogInformation("Playing sound file: {WaveFile}", waveFile);
            
            using var player = new SoundPlayer
            {
                SoundLocation = waveFile
            };
            player.Play();
            Thread.Sleep(1500);
            
            Console.WriteLine($"✅ Sound file played successfully: {waveFile}");
            _logger.LogDebug("Sound file played successfully: {WaveFile}", waveFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to play sound file: {waveFile} - Error: {ex.Message}");
            _logger.LogError(ex, "Failed to play sound file: {WaveFile}", waveFile);
            throw;
        }
    }
}
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using Microsoft.Extensions.Logging;
using Moba.Common.Extensions;

namespace Moba.Sound;

/// <summary>
/// Windows-native Text-to-Speech implementation using System.Speech (SAPI).
/// Only works on Windows platforms. Provides offline TTS without cloud dependencies.
/// </summary>
[SupportedOSPlatform("windows")]
public class SystemSpeechEngine : ISpeakerEngine
{
    private readonly ILogger<SystemSpeechEngine> _logger;

    public SystemSpeechEngine(ILogger<SystemSpeechEngine> logger)
    {
        _logger = logger;
    }

    public string Name { get; set; } = "System.Speech (Windows SAPI)";

    /// <summary>
    /// Performs a text-to-speech announcement using Windows SAPI.
    /// </summary>
    /// <param name="message">The text to be spoken</param>
    /// <param name="voiceName">Optional: Name of the voice to use (can be full name like "Microsoft Hedda Desktop" or partial name like "Hedda")</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when message is null or empty</exception>
    public async Task AnnouncementAsync(string message, string? voiceName)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        await Task.Run(() =>
        {
            using var synthesizer = new SpeechSynthesizer();
            
            // Configure output to default audio device
            synthesizer.SetOutputToDefaultAudioDevice();

            // Select voice if specified
            if (!string.IsNullOrEmpty(voiceName) && !TrySelectVoice(synthesizer, voiceName))
            {
                this.LogWarning($"Voice '{voiceName}' not found. Using default voice.", _logger);
                
                this.Log("Available voices:", _logger);
                
                // Use Select() instead of foreach for better performance
                var voiceDescriptions = synthesizer.GetInstalledVoices()
                    .Select(voice => voice.VoiceInfo)
                    .Select(info => $"  - {info.Name} ({info.Culture.Name}, {info.Gender}, {info.Age})");
                
                foreach (var description in voiceDescriptions)
                {
                    this.Log(description, _logger);
                }
            }

            // Configure speech rate (slower speech: -15% similar to Azure config)
            // Range: -10 (slowest) to 10 (fastest), 0 is normal
            synthesizer.Rate = -2; // Approximately -15% to -20% slower

            // Configure volume
            synthesizer.Volume = 100; // Range: 0 to 100

            try
            {
                // Synthesize speech synchronously
                this.Log($"🔊 Synthesizing speech: [{message}]", _logger);
                
                synthesizer.Speak(message);
                
                this.Log($"✅ Speech synthesized successfully for text: [{message}]", _logger);
            }
            catch (Exception ex)
            {
                this.LogError($"ERROR during speech synthesis for message: [{message}]", ex, _logger);
                throw;
            }
        });
    }

    /// <summary>
    /// Tries to select a voice by name. Supports both full names and partial matches.
    /// </summary>
    /// <param name="synthesizer">The speech synthesizer</param>
    /// <param name="voiceName">The voice name to search for</param>
    /// <returns>True if voice was found and selected, false otherwise</returns>
    private bool TrySelectVoice(SpeechSynthesizer synthesizer, string voiceName)
    {
        // Try exact match first
        try
        {
            synthesizer.SelectVoice(voiceName);
            this.Log($"✅ Using voice (exact match): {voiceName}", _logger);
            return true;
        }
        catch (ArgumentException)
        {
            // Exact match failed, try partial match
        }

        // Try to find a voice that contains the given name
        var installedVoices = synthesizer.GetInstalledVoices();
        var matchingVoice = installedVoices.FirstOrDefault(v => 
            v.VoiceInfo.Name.Contains(voiceName, StringComparison.OrdinalIgnoreCase));

        if (matchingVoice != null)
        {
            synthesizer.SelectVoice(matchingVoice.VoiceInfo.Name);
            this.Log($"✅ Using voice (partial match): {matchingVoice.VoiceInfo.Name}", _logger);
            return true;
        }

        // Try to select by gender and culture if the name suggests German
        if (voiceName.Contains("German", StringComparison.OrdinalIgnoreCase) || 
            voiceName.Contains("de-", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                synthesizer.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo("de-DE"));
                this.Log("✅ Using German voice (by culture)", _logger);
                return true;
            }
            catch
            {
                // Culture-based selection failed
            }
        }

        return false;
    }
}

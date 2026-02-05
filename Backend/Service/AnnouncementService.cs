// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;
using Microsoft.Extensions.Logging;
using Sound;

/// <summary>
/// Service for generating station announcements from templates.
/// 
/// Purpose:
/// - Generate announcement text from journey template
/// - Replace placeholders with station data
/// - Delegate audio output to ISpeakerEngine (CognitiveSpeechEngine or SystemSpeechEngine)
/// 
/// Architecture:
/// - Template rendering: Pure backend logic (platform-independent)
/// - Audio output: Delegated to ISpeakerEngine (can be any implementation)
/// - Integrates with existing Sound.csproj infrastructure
/// 
/// Usage:
/// 1. Inject ISpeakerEngine (configured in DI)
/// 2. Call GenerateAnnouncementText() for text generation only
/// 3. Call GenerateAndSpeakAnnouncementAsync() for text + audio
/// 
/// Template Format:
/// Journey.Text = "Nächster Halt {StationName}. {StationIsExitOnLeft}."
/// 
/// Placeholders:
/// - {StationName} → Station.Name
/// - {StationIsExitOnLeft} → "Ausstieg in Fahrtrichtung links" or "Ausstieg in Fahrtrichtung rechts"
/// - {ExitDirection} → "links" or "rechts" (based on Station.IsExitOnLeft)
/// - {StationNumber} → Station ordinal position in journey
/// - {TrackNumber} → Station.Track
/// </summary>
public class AnnouncementService
{
    private readonly ISpeakerEngine? _speakerEngine;
    private readonly ILogger<AnnouncementService>? _logger;

    /// <summary>
    /// Gets whether a speaker engine is available for speech synthesis.
    /// </summary>
    public bool IsSpeakerEngineAvailable => _speakerEngine != null;

    /// <summary>
    /// Initializes announcement service with optional speaker engine.
    /// If no engine is supplied, announcements are generated but not spoken.
    /// </summary>
    /// <param name="speakerEngine">Speaker engine for audio output (optional)</param>
    /// <param name="logger">Optional logger for debugging</param>
    public AnnouncementService(ISpeakerEngine? speakerEngine = null, ILogger<AnnouncementService>? logger = null)
    {
        _speakerEngine = speakerEngine;
        _logger = logger;
        _logger?.LogInformation("AnnouncementService initialized (Speaker Engine: {EngineType})", 
            _speakerEngine?.Name ?? "None");
    }

    /// <summary>
    /// Generates announcement text by replacing template placeholders with station data.
    /// </summary>
    /// <param name="journey">Journey containing template text</param>
    /// <param name="station">Station with data to substitute</param>
    /// <param name="stationIndex">Ordinal position of station in journey (1-based)</param>
    /// <returns>Generated announcement text ready to speak</returns>
    public string GenerateAnnouncementText(Journey journey, Station station, int stationIndex)
    {
        if (string.IsNullOrEmpty(journey.Text))
        {
            _logger?.LogWarning("Journey '{JourneyName}' has no announcement template", journey.Name);
            return string.Empty;
        }

        var text = journey.Text;

        // Replace {StationName}
        text = ReplaceToken(text, "StationName", station.Name);

        // Replace {StationIsExitOnLeft} - full German phrase
        var exitPhrase = station.IsExitOnLeft 
            ? "links" 
            : "rechts";
        text = ReplaceToken(text, "StationIsExitOnLeft", exitPhrase);

        // Replace {ExitDirection} - "links" or "rechts" (for custom templates)
        var exitDirection = station.IsExitOnLeft ? "links" : "rechts";
        text = ReplaceToken(text, "ExitDirection", exitDirection);

        // Replace {StationNumber} - ordinal position (1-based)
        text = ReplaceToken(text, "StationNumber", stationIndex.ToString());

        // Replace {TrackNumber} if available
        if (station.Track.HasValue)
        {
            text = ReplaceToken(text, "TrackNumber", station.Track.Value.ToString());
        }

        _logger?.LogInformation("Generated announcement: \"{AnnouncementText}\"", text);
        return text;
    }

    /// <summary>
    /// Generates announcement text and speaks it via speaker engine.
    /// Safe to call even if no speaker engine is configured (logs and returns gracefully).
    /// </summary>
    /// <param name="journey">Journey containing template</param>
    /// <param name="station">Station with data</param>
    /// <param name="stationIndex">Station position (1-based)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    public async Task GenerateAndSpeakAnnouncementAsync(
        Journey journey, 
        Station station, 
        int stationIndex,
        CancellationToken cancellationToken = default)
    {
        // Generate text
        var announcementText = GenerateAnnouncementText(journey, station, stationIndex);

        if (string.IsNullOrEmpty(announcementText))
        {
            _logger?.LogWarning("No announcement text to speak for station '{StationName}'", station.Name);
            return;
        }

        // Speak via speaker engine if available
        if (_speakerEngine != null)
        {
            try
            {
                _logger?.LogInformation("🔊 Speaking announcement via {SpeakerEngine} for station '{StationName}'", 
                    _speakerEngine.Name, station.Name);
                await _speakerEngine.AnnouncementAsync(announcementText, voiceName: null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Failed to speak announcement: {Error}", ex.Message);
                // Don't throw - TTS failure shouldn't break journey execution
            }
        }
        else
        {
            _logger?.LogInformation("⚠️ No speaker engine configured. Announcement text: \"{Text}\"", announcementText);
        }
    }

    /// <summary>
    /// Replaces a single placeholder token with a value.
    /// </summary>
    /// <param name="text">Text containing {Token} placeholder</param>
    /// <param name="token">Token name (without braces)</param>
    /// <param name="value">Value to substitute</param>
    /// <returns>Text with token replaced</returns>
    private string ReplaceToken(string text, string token, string value)
    {
        var pattern = $"{{{token}}}";
        var replaced = text.Replace(pattern, value);
        
        if (replaced != text)
        {
            _logger?.LogDebug("Replaced {{{Token}}} with '{Value}'", token, value);
        }

        return replaced;
    }
}

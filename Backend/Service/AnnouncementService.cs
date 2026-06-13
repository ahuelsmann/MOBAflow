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
/// - Delegate audio output to ISpeakerEngine (PiperSpeechEngine or SystemSpeechEngine)
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
/// Journey.Text = "Next stop {StationName}. {StationIsExitOnLeft}." (or localized equivalent)
/// 
/// Placeholders:
/// - {StationName} → Station.Name
/// - {StationIsExitOnLeft} → "Ausstieg in Fahrtrichtung links" or "Ausstieg in Fahrtrichtung rechts"
/// - {ExitDirection} → "links" or "rechts" (based on Station.IsExitOnLeft)
/// - {StationNumber} → Station ordinal position in journey
/// - {TrackNumber} → Station.Platforms/Station.PlatformId
/// </summary>
public interface IAnnouncementService
{
    bool IsSpeakerEngineAvailable { get; }

    string GenerateAnnouncementText(Journey journey, Station station, int stationIndex);

    string GenerateAnnouncementText(string? templateText, Station station, int stationIndex, string? templateName = null);

    Task GenerateAndSpeakAnnouncementAsync(
        Journey journey,
        Station station,
        int stationIndex,
        CancellationToken cancellationToken = default);

    Task GenerateAndSpeakAnnouncementAsync(
        string? templateText,
        Station station,
        int stationIndex,
        CancellationToken cancellationToken = default,
        string? templateName = null,
        bool suppressSpeechErrors = true);
}

public class AnnouncementService : IAnnouncementService
{
    private readonly ISpeakerEngineFactory? _speakerEngineFactory;
    private readonly ILogger<AnnouncementService>? _logger;

    /// <summary>
    /// Gets whether a speaker engine factory is available for speech synthesis.
    /// </summary>
    public bool IsSpeakerEngineAvailable => _speakerEngineFactory != null;

    /// <summary>
    /// Initializes announcement service with optional speaker engine factory.
    /// If no factory is supplied, announcements are generated but not spoken.
    /// </summary>
    /// <param name="speakerEngineFactory">Speaker engine factory for creating engines (optional)</param>
    /// <param name="logger">Optional logger for debugging</param>
    public AnnouncementService(ISpeakerEngineFactory? speakerEngineFactory = null, ILogger<AnnouncementService>? logger = null)
    {
        _speakerEngineFactory = speakerEngineFactory;
        _logger = logger;
        _logger?.LogInformation("AnnouncementService initialized (Speaker Engine Factory: {FactoryAvailable})",
            _speakerEngineFactory != null ? "Available" : "None");
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
        return GenerateAnnouncementText(journey.Text, station, stationIndex, journey.Name);
    }

    /// <summary>
    /// Generates announcement text by replacing template placeholders with station data.
    /// </summary>
    /// <param name="templateText">Raw announcement template text.</param>
    /// <param name="station">Station with data to substitute.</param>
    /// <param name="stationIndex">Ordinal position of station in journey (1-based).</param>
    /// <param name="templateName">Optional template name used for logging context.</param>
    /// <returns>Generated announcement text ready to speak.</returns>
    public string GenerateAnnouncementText(string? templateText, Station station, int stationIndex, string? templateName = null)
    {
        if (string.IsNullOrEmpty(templateText))
        {
            _logger?.LogWarning("Announcement template '{TemplateName}' is empty", templateName ?? "<unknown>");
            return string.Empty;
        }

        var text = templateText;

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
        var platformNumber = ResolvePlatformNumber(station);
        if (platformNumber.HasValue)
        {
            text = ReplaceToken(text, "TrackNumber", platformNumber.Value.ToString());
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
        await GenerateAndSpeakAnnouncementAsync(journey.Text, station, stationIndex, cancellationToken, journey.Name).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates announcement text and speaks it via speaker engine.
    /// </summary>
    /// <param name="templateText">Raw announcement template text.</param>
    /// <param name="station">Station with data.</param>
    /// <param name="stationIndex">Station position (1-based).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <param name="templateName">Optional template name used for logging context.</param>
    public async Task GenerateAndSpeakAnnouncementAsync(
        string? templateText,
        Station station,
        int stationIndex,
        CancellationToken cancellationToken = default,
        string? templateName = null,
        bool suppressSpeechErrors = true)
    {
        // Generate text
        var announcementText = GenerateAnnouncementText(templateText, station, stationIndex, templateName);

        if (string.IsNullOrEmpty(announcementText))
        {
            _logger?.LogWarning("No announcement text to speak for station '{StationName}'", station.Name);
            return;
        }

        // Speak via speaker engine if factory is available
        if (_speakerEngineFactory != null)
        {
            try
            {
                // Create engine dynamically based on current settings
                var speakerEngine = _speakerEngineFactory.CreateEngineFromOptions();

                _logger?.LogInformation("Speaking announcement via {SpeakerEngine} for station '{StationName}'",
                    speakerEngine.Name, station.Name);
                await speakerEngine.AnnouncementAsync(announcementText, voiceName: null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to speak announcement: {Error}", ex.Message);
                if (!suppressSpeechErrors)
                {
                    throw;
                }
            }
        }
        else
        {
            _logger?.LogInformation("No speaker engine configured. Announcement text: \"{Text}\"", announcementText);
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

    private static uint? ResolvePlatformNumber(Station station)
    {
        if (station.PlatformId.HasValue)
        {
            var platform = station.Platforms.FirstOrDefault(platform => platform.Id == station.PlatformId.Value);
            if (platform != null)
            {
                return platform.Number;
            }
        }

        return station.Platforms.FirstOrDefault()?.Number;
    }
}
